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

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        g.RegisterPort(cell, this);
        PowerManager.Instance.AddPowerSource(this);
        TickManager.Instance?.Register(this);
    }

    public override void OnRemoved()
    {
        PowerManager.Instance.RemovePowerSource(this);
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }

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
        else
        {
            Debug.LogWarning("Not enough cached resources to generate power.");
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
            extraText   = $"发电：+{powerGenerated}/每{productionInterval:F1}s；消耗：{resourceConsumed} x {(resourceType ? resourceType.itemId : "资源")}"
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
