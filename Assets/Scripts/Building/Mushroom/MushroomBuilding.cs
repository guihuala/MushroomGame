using System.Collections.Generic;
using UnityEngine;

public class MushroomBuilding : Building, IItemPort, ITickable
{
    [Header("Building Settings")]
    public Vector2Int size = new Vector2Int(2, 1);
    
    [Header("Production Settings")]
    public RecipeDef recipe;  // 使用的配方
    public float productionTime = 5f;  // 生产时间（秒）
    
    [Header("Storage")]
    public int inputBufferSize = 10;  // 输入缓冲区大小
    public int outputBufferSize = 10; // 输出缓冲区大小

    // 输入输出缓冲区
    private readonly Dictionary<ItemDef, int> _inputBuffer = new();
    private readonly Queue<ItemPayload> _outputBuffer = new();
    
    private float _currentProductionTime = 0f;
    private bool _isProducing = false;

    public bool CanReceive => GetTotalInputCount() < inputBufferSize;
    public bool CanProvide => _outputBuffer.Count > 0;

    #region 生命周期

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
        Debug.Log($"[Mushroom] Placed at {cell}, recipe: {recipe?.name}");
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }

    #endregion

    #region IItemPort 实现

    public bool TryReceive(in ItemPayload payload)
    {
        if (!CanReceive) return false;
        
        // 检查是否是配方需要的材料
        if (!IsInputItem(payload.item))
        {
            Debug.Log($"[Mushroom] Rejected item: {payload.item?.name}, not in recipe");
            return false;
        }

        // 添加到输入缓冲区
        AddToInputBuffer(payload.item, payload.amount);
        Debug.Log($"[Mushroom] Received {payload.amount}x {payload.item.name}, total: {GetInputCount(payload.item)}");
        
        return true;
    }

    public bool TryProvide(ref ItemPayload payload)
    {
        if (!CanProvide) return false;
        
        payload = _outputBuffer.Dequeue();
        Debug.Log($"[Mushroom] Provided {payload.amount}x {payload.item.name}");
        return true;
    }

    #endregion

    #region 生产逻辑

    public void Tick(float dt)
    {
        // 检查是否可以开始生产
        if (!_isProducing && CanStartProduction())
        {
            StartProduction();
        }

        // 生产进行中
        if (_isProducing)
        {
            _currentProductionTime += dt;
            
            if (_currentProductionTime >= productionTime)
            {
                CompleteProduction();
            }
        }

        // 尝试输出产品
        TryOutputProducts();
    }

    private bool CanStartProduction()
    {
        if (recipe == null) return false;
        
        // 检查是否有足够的输入材料
        foreach (var input in recipe.inputItems)
        {
            if (GetInputCount(input.item) < input.amount)
                return false;
        }
        
        // 检查输出缓冲区是否有空间
        return _outputBuffer.Count + recipe.outputItems.Count <= outputBufferSize;
    }

    private void StartProduction()
    {
        _isProducing = true;
        _currentProductionTime = 0f;
        
        // 消耗输入材料
        foreach (var input in recipe.inputItems)
        {
            ConsumeInputItem(input.item, input.amount);
        }
        
        Debug.Log($"[Mushroom] Started production: {recipe.name}");
    }

    private void CompleteProduction()
    {
        _isProducing = false;
        _currentProductionTime = 0f;
        
        // 生成输出产品
        foreach (var output in recipe.outputItems)
        {
            var payload = new ItemPayload
            {
                item = output.item,
                amount = output.amount,
                worldPos = grid.CellToWorld(cell)
            };
            
            _outputBuffer.Enqueue(payload);
        }
        
        Debug.Log($"[Mushroom] Completed production: {recipe.name}");
    }

    private void TryOutputProducts()
    {
        if (_outputBuffer.Count == 0) return;

        // 尝试向各个方向输出产品
        foreach (var direction in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
        {
            var targetCell = cell + direction;
            var port = grid.GetPortAt(targetCell);
            
            if (port != null && port.CanReceive)
            {
                var payload = _outputBuffer.Peek();
                if (port.TryReceive(payload))
                {
                    _outputBuffer.Dequeue();
                    Debug.Log($"[Mushroom] Output to {targetCell}");
                    break;
                }
            }
        }
    }

    #endregion

    #region 缓冲区管理

    private bool IsInputItem(ItemDef item)
    {
        if (recipe == null) return false;
        
        foreach (var input in recipe.inputItems)
        {
            if (input.item == item) return true;
        }
        return false;
    }

    private void AddToInputBuffer(ItemDef item, int amount)
    {
        if (_inputBuffer.ContainsKey(item))
            _inputBuffer[item] += amount;
        else
            _inputBuffer[item] = amount;
    }

    private int GetInputCount(ItemDef item)
    {
        return _inputBuffer.ContainsKey(item) ? _inputBuffer[item] : 0;
    }

    private void ConsumeInputItem(ItemDef item, int amount)
    {
        if (_inputBuffer.ContainsKey(item))
        {
            _inputBuffer[item] -= amount;
            if (_inputBuffer[item] <= 0)
                _inputBuffer.Remove(item);
        }
    }

    private int GetTotalInputCount()
    {
        int total = 0;
        foreach (var count in _inputBuffer.Values)
            total += count;
        return total;
    }

    #endregion
}

