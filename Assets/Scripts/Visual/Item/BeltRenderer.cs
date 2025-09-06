using System.Collections.Generic;
using UnityEngine;

public class BeltRenderer : MonoBehaviour
{
    [Header("渲染设置")]
    public GameObject itemVisualPrefab;
    public float itemHeight = 0.2f;

    private TileGridService _grid;
    private readonly Dictionary<Conveyer, List<GameObject>> _itemVisuals = new();
    private readonly Queue<GameObject> _visualPool = new();
    
    // 注册所有活跃传送带的集合
    private readonly HashSet<Conveyer> _activeConveyors = new();

    void Start()
    {
        _grid = FindObjectOfType<TileGridService>();
        // 注册建筑放置和移除事件
        MsgCenter.RegisterMsg(MsgConst.MSG_CONVEYOR_PLACED, OnBuildingPlaced);
        MsgCenter.RegisterMsg(MsgConst.MSG_CONVEYOR_REMOVED, OnBuildingRemoved);
        
        // 初始化时查找场景中已有的传送带
        FindExistingConveyors();
    }

    void OnDestroy()
    {
        // 注销事件
        MsgCenter.UnregisterMsg(MsgConst.MSG_CONVEYOR_PLACED, OnBuildingPlaced);
        MsgCenter.UnregisterMsg(MsgConst.MSG_CONVEYOR_REMOVED, OnBuildingRemoved);
    }

    void LateUpdate()
    {
        // 只更新已注册的活跃传送带
        foreach (var conveyor in _activeConveyors)
        {
            // 检查传送带是否已被销毁但还未收到移除事件
            if (conveyor == null)
            {
                _activeConveyors.Remove(conveyor);
                continue;
            }
            UpdateConveyorVisuals(conveyor);
        }
    }

    private void FindExistingConveyors()
    {
        var existingConveyors = FindObjectsOfType<Conveyer>();
        DebugManager.Log($"Found {existingConveyors.Length} existing conveyors", this);
        
        foreach (var conveyor in existingConveyors)
        {
            if (conveyor != null && !_activeConveyors.Contains(conveyor))
            {
                _activeConveyors.Add(conveyor);
                DebugManager.Log($"Added existing conveyor: {conveyor.name}", this);
            }
        }
    }

    // 建筑放置事件处理
    private void OnBuildingPlaced(params object[] args)
    {
        if (args.Length > 0 && args[0] is Conveyer conveyer)
        {
            _activeConveyors.Add(conveyer);
            EnsureVisualsCapacity(conveyer); // 立即创建视觉对象
        }
    }

    // 建筑移除事件处理
    private void OnBuildingRemoved(params object[] args)
    {
        if (args.Length > 0 && args[0] is Conveyer conveyer)
        {
            _activeConveyors.Remove(conveyer);
            RemoveConveyorVisuals(conveyer);
        }
    }
    
    private Vector3 GetSafeWorldPosition(Conveyer conveyer)
    {
        if (conveyer == null)
        {
            DebugManager.LogWarning("GetSafeWorldPosition called with null conveyor", this);
            return Vector3.zero;
        }
        
        try
        {
            Vector3 worldPos = conveyer.GetWorldPosition();
            DebugManager.Log($"Got world position from conveyor: {worldPos}", this);
            return worldPos;
        }
        catch (System.NullReferenceException ex)
        {
            DebugManager.LogWarning($"Grid is null, using transform position: {ex.Message}", this);
            return conveyer.transform.position;
        }
    }
    
    private void UpdateConveyorVisuals(Conveyer conveyer)
    {
        if (conveyer == null)
        {
            return;
        }

        EnsureVisualsCapacity(conveyer);

        var visuals = _itemVisuals[conveyer];
        var items = conveyer.Items;
        var worldPos = GetSafeWorldPosition(conveyer);
        var direction = conveyer.Direction;
        
        for (int i = 0; i < visuals.Count; i++)
        {
            var visual = visuals[i];
            
            if (i < items.Count)
            {
                if (visual == null)
                {
                    continue;
                }

                visual.SetActive(true);
                UpdateItemVisual(visual, items[i], worldPos, direction);
            }
            else
            {
                if (visual != null)
                {
                    visual.SetActive(false);
                }
            }
        }
    }

    // 移除传送带视觉对象
    private void RemoveConveyorVisuals(Conveyer conveyer)
    {
        if (conveyer == null)
        { 
            return;
        }

        if (_itemVisuals.TryGetValue(conveyer, out var visuals))
        {
            DebugManager.Log($"Removing {visuals.Count} visuals for conveyor {conveyer.name}", this);
            foreach (var visual in visuals)
            {
                if (visual != null)
                {
                    ReturnVisualToPool(visual);
                }
            }
            _itemVisuals.Remove(conveyer);
        }
        else
        {
            DebugManager.Log($"No visuals found for conveyor {conveyer.name}", this);
        }
    }
    
    private void EnsureVisualsCapacity(Conveyer conveyer)
    {
        if (conveyer == null)
        {
            return;
        }

        if (!_itemVisuals.ContainsKey(conveyer))
        {
            _itemVisuals[conveyer] = new List<GameObject>();
        }

        var visuals = _itemVisuals[conveyer];
        int needed = conveyer.maxItems - visuals.Count;
        
        if (needed > 0)
        {
            for (int i = 0; i < needed; i++)
            {
                var visual = GetVisualFromPool();
                if (visual != null)
                {
                    visual.transform.SetParent(conveyer.transform);
                    visuals.Add(visual);
                }
            }
        }
    }

    private void UpdateItemVisual(GameObject visual, BeltItem item, Vector3 beltWorldPos, Vector2Int direction)
    {
        if (visual == null || _grid == null) return;

        // 设置图标
        var spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = item.payload.item.icon;
        }

        // 计算物品位置 - 确保不会超出传送带范围
        float progress = Mathf.Clamp01(item.position);
    
        // 根据传送带方向计算偏移
        Vector3 directionVector = new Vector3(direction.x, direction.y, 0).normalized;
    
        // 计算偏移量，确保不会超出传送带末端
        float maxOffset = _grid.cellSize * 0.95f; // 留一点边距
        float actualOffset = progress * maxOffset;
    
        Vector3 offset = directionVector * actualOffset;
    
        // 计算最终位置
        Vector3 position = beltWorldPos + offset + new Vector3(0, 0, -itemHeight);
        visual.transform.position = position;
    }
    
    private GameObject GetVisualFromPool()
    {
        GameObject visual = null;
    
        // 先从对象池获取
        if (_visualPool.Count > 0)
        {
            visual = _visualPool.Dequeue();
            if (visual != null)
            {
                visual.SetActive(true);
                return visual;
            }
        }
        
        if (itemVisualPrefab == null)
        {
            return null;
        }
    
        visual = Instantiate(itemVisualPrefab);
    
        return visual;
    }

    private void ReturnVisualToPool(GameObject visual)
    {
        if (visual == null)
        {
            return;
        }

        visual.SetActive(false);
        visual.transform.SetParent(null);
        _visualPool.Enqueue(visual);
    }
}