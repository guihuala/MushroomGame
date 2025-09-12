using UnityEngine;

public class PowerPlant : Building, ITickable, IItemPort, IProductionInfoProvider
{
    private Vector2Int inDir = Vector2Int.down;
    
    [Header("电力生成设置")]
    public int powerGenerated = 10;  // 每次生产的电力
    public int resourceConsumed = 1; // 每次生产消耗的资源数量
    public ItemDef resourceType; // 用于消耗的资源类型

    private float _timer = 0f;
    private float productionInterval = 1f; // 每多少秒生产一次电力

    private int cachedResources = 0; // 存储的缓存资源数量

    public bool CanReceive => true;
    public bool CanProvide => false;
    
    [Header("电力覆盖")]
    public float coverageRange = 5f;

    [Header("范围可视化")]
    public bool showRingOnHover = true;
    public Color ringColor = new Color(1f, 0.85f, 0.2f, 0.5f);
    public int ringSegments = 64;
    private LineRenderer _ring;

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        g.RegisterPort(cell, this);
        PowerManager.Instance.AddPowerSource(this);
        TickManager.Instance?.Register(this);
        EnsureRing();
        SetRingVisible(false);
    }

    public override void OnRemoved()
    {
        PowerManager.Instance.RemovePowerSource(this);
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        DestroyRing();
        base.OnRemoved();
    }

    private void EnsureRing()
    {
        if (_ring != null) return;
        _ring = gameObject.AddComponent<LineRenderer>();
        _ring.loop = true;
        _ring.useWorldSpace = false;
        _ring.widthMultiplier = 0.05f;
        _ring.material = new Material(Shader.Find("Sprites/Default"));
        _ring.startColor = _ring.endColor = ringColor;
        _ring.positionCount = ringSegments;

        for (int i = 0; i < ringSegments; i++)
        {
            float t = i / (float)ringSegments * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * coverageRange);
        }
        _ring.sortingOrder = 5000; // 保证在上层
    }

    private void DestroyRing()
    {
        if (_ring != null) Destroy(_ring);
    }

    private void SetRingVisible(bool v)
    {
        if (_ring) _ring.enabled = v && showRingOnHover;
    }

    private void OnMouseEnter() => SetRingVisible(true);
    private void OnMouseExit()  => SetRingVisible(false);
    

    public void Tick(float dt)
    {
        _timer += Time.deltaTime;

        // 每过一定时间检查资源并生产电力
        if (_timer >= productionInterval)
        {
            _timer = 0f;
            
            TryGeneratePower();
        }
    }

    private void TryGeneratePower()
    {
        if (cachedResources >= resourceConsumed)
        {
            cachedResources -= resourceConsumed;
            PowerManager.Instance.AddPower(powerGenerated);
            Debug.Log("PowerGenerator generated " + powerGenerated + " power.");
        }
    }
    
    public bool TryReceive(in ItemPayload payload)
    {
        Debug.Log(payload.item);
        if (payload.item == resourceType)
        {
            cachedResources += payload.amount;
            Debug.Log("Received " + payload.amount + " resources, current cache: " + cachedResources);
            return true;
        }

        return false;
    }

    public bool TryProvide(ref ItemPayload payload) => false;

    #region tooltip
    
    public ProductionInfo GetProductionInfo()
    {
        var info = new ProductionInfo {
            displayName = gameObject.name,
            recipe      = null,
            isProducing = cachedResources >= resourceConsumed,
            progress01  = 0f,
            extraText   = $"Power generation: +{powerGenerated}/each{productionInterval:F1}s; Consumption: {resourceConsumed} x {(resourceType ? resourceType.itemId : "resource")}"
        };
        
        info.inputs.Add(new IOEntry {
            item = resourceType, have = cachedResources, cap = -1, want = resourceConsumed
        });
        
        info.outputs.Add(new IOEntry {
            item = null, have = 0, cap = -1, want = 0, resourceKey = "Electricity"
        });

        return info;
    }

    #endregion
}
