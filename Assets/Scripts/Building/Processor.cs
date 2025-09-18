using System.Collections.Generic;
using UnityEngine;

public class Processor : Building, ITickable, IItemPort, IOrientable, IProductionInfoProvider
{
    [Header("加工器设置")]
    public RecipeDef recipe;  // 生产配方
    public Vector2Int inDir = Vector2Int.down;  // 输入方向
    public Vector2Int outDir = Vector2Int.up;  // 输出方向
    
    [Header("生产设置")]
    public float productionProgress = 0f;  // 当前生产进度
    public bool isProducing = false;  // 是否正在生产
    
    [Header("缓冲区设置")]
    public int inputBufferSize = 10;  // 输入缓冲区大小
    public int outputBufferSize = 10;  // 输出缓冲区大小
    
    // 输入和输出缓冲区
    private readonly Dictionary<ItemDef, int> _inputBuffer = new();
    private readonly Dictionary<ItemDef, int> _outputBuffer = new();
    
    public bool CanProvide => GetTotalOutputCount() > 0;
    public bool CanReceive => HasSpaceInInputBuffer();
    
    public Vector3 GetWorldPosition()
    {
        return grid != null ? grid.CellToWorld(cell) : transform.position;
    }

    #region 生命周期管理

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }

    #endregion

    #region 物品传输接口

    public bool TryReceive(in ItemPayload payload)
    {
        if (!CanReceive || recipe == null) return false;
        
        // 检查是否是配方需要的材料
        bool isNeeded = false;
        foreach (var input in recipe.inputItems)
        {
            if (input.item == payload.item)
            {
                isNeeded = true;
                break;
            }
        }
        
        if (!isNeeded) return false;
        
        // 添加到输入缓冲区
        if (_inputBuffer.ContainsKey(payload.item))
        {
            _inputBuffer[payload.item] += payload.amount;
        }
        else
        {
            _inputBuffer[payload.item] = payload.amount;
        }
        
        return true;
    }

    public bool TryProvide(ref ItemPayload payload)
    {
        if (!CanProvide || recipe == null) return false;
        
        // 从输出缓冲区获取物品
        foreach (var output in recipe.outputItems)
        {
            if (_outputBuffer.ContainsKey(output.item) && _outputBuffer[output.item] > 0)
            {
                payload.item = output.item;
                payload.amount = Mathf.Min(output.amount, _outputBuffer[output.item]);
                _outputBuffer[output.item] -= payload.amount;
                
                if (_outputBuffer[output.item] <= 0)
                {
                    _outputBuffer.Remove(output.item);
                }
                
                payload.worldPos = GetWorldPosition();
                return true;
            }
        }
        
        return false;
    }

    #endregion

    #region Tick逻辑

    public void Tick(float dt)
    {
        TryPullInputItems();
        
        float mult = PowerManager.Instance.GetSpeedMultiplier(cell, grid);
        UpdateProduction(dt * mult);
        
        TryPushOutputItems();
    }

    private void TryPullInputItems()
    {
        if (recipe == null || !HasSpaceInInputBuffer()) return;
        
        // 尝试从输入端口拉取所需材料
        foreach (var input in recipe.inputItems)
        {
            if (GetInputCount(input.item) >= input.amount) continue;
            
            var targetCell = cell + inDir;
            var port = grid.GetPortAt(targetCell);
            
            if (port != null && port.CanProvide)
            {
                ItemPayload payload = new ItemPayload { item = input.item, amount = 1 };
                if (port.TryProvide(ref payload) && payload.amount > 0)
                {
                    if (_inputBuffer.ContainsKey(payload.item))
                    {
                        _inputBuffer[payload.item] += payload.amount;
                    }
                    else
                    {
                        _inputBuffer[payload.item] = payload.amount;
                    }
                }
            }
        }
    }

    private void UpdateProduction(float dt)
    {
        if (recipe == null) return;
        
        // 检查是否可以开始生产
        if (!isProducing && CanStartProduction())
        {
            StartProduction();
        }
        
        // 更新生产进度
        if (isProducing)
        {
            productionProgress += dt;
            
            if (productionProgress >= recipe.productionTime)
            {
                CompleteProduction();
            }
        }
    }

    private void TryPushOutputItems()
    {
        if (recipe == null || GetTotalOutputCount() == 0) return;
        
        var targetCell = cell + outDir;
        var port = grid.GetPortAt(targetCell);
        
        if (port != null && port.CanReceive)
        {
            foreach (var output in recipe.outputItems)
            {
                if (_outputBuffer.ContainsKey(output.item) && _outputBuffer[output.item] > 0)
                {
                    ItemPayload payload = new ItemPayload
                    {
                        item = output.item,
                        amount = Mathf.Min(output.amount, _outputBuffer[output.item]),
                        worldPos = GetWorldPosition()
                    };
                    
                    if (port.TryReceive(in payload))
                    {
                        _outputBuffer[output.item] -= payload.amount;
                        
                        if (_outputBuffer[output.item] <= 0)
                        {
                            _outputBuffer.Remove(output.item);
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region 生产逻辑

    private bool CanStartProduction()
    {
        if (recipe == null) return false;
        
        // 检查输入材料是否足够
        foreach (var input in recipe.inputItems)
        {
            if (GetInputCount(input.item) < input.amount)
            {
                return false;
            }
        }
        
        // 检查输出缓冲区是否有空间
        foreach (var output in recipe.outputItems)
        {
            if (GetOutputCount(output.item) + output.amount > outputBufferSize)
            {
                return false;
            }
        }
        
        return true;
    }

    private void StartProduction()
    {
        if (recipe == null) return;
        
        // 消耗输入材料
        foreach (var input in recipe.inputItems)
        {
            _inputBuffer[input.item] -= input.amount;
            if (_inputBuffer[input.item] <= 0)
            {
                _inputBuffer.Remove(input.item);
            }
        }
        
        productionProgress = 0f;
        isProducing = true;
    }

    private void CompleteProduction()
    {
        if (recipe == null) return;
        
        // 添加输出产品
        foreach (var output in recipe.outputItems)
        {
            if (_outputBuffer.ContainsKey(output.item))
            {
                _outputBuffer[output.item] += output.amount;
            }
            else
            {
                _outputBuffer[output.item] = output.amount;
            }
        }
        
        productionProgress = 0f;
        isProducing = false;
    }

    #endregion

    #region 辅助方法

    private int GetInputCount(ItemDef item)
    {
        return _inputBuffer.ContainsKey(item) ? _inputBuffer[item] : 0;
    }

    private int GetOutputCount(ItemDef item)
    {
        return _outputBuffer.ContainsKey(item) ? _outputBuffer[item] : 0;
    }

    private int GetTotalInputCount()
    {
        int total = 0;
        foreach (var count in _inputBuffer.Values)
        {
            total += count;
        }
        return total;
    }

    private int GetTotalOutputCount()
    {
        int total = 0;
        foreach (var count in _outputBuffer.Values)
        {
            total += count;
        }
        return total;
    }

    private bool HasSpaceInInputBuffer()
    {
        return GetTotalInputCount() < inputBufferSize;
    }
    
    #endregion

    #region 方向设置

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = -dir;
        transform.up = new Vector3(dir.x, dir.y, 0f);
    }

    #endregion

    #region toolkit

    public ProductionInfo GetProductionInfo()
    {
        var info = new ProductionInfo {
            displayName = gameObject.name,
            recipe      = recipe,
            isProducing = isProducing,
            progress01  = (recipe != null && recipe.productionTime > 0f)
                ? Mathf.Clamp01(productionProgress / recipe.productionTime)
                : 0f
        };

        if (recipe != null)
        {
            foreach (var input in recipe.inputItems)
            {
                int have = 0;
                if (input.item != null) _inputBuffer.TryGetValue(input.item, out have);
                info.inputs.Add(new IOEntry {
                    item = input.item, have = have, cap = inputBufferSize, want = input.amount
                });
            }
            foreach (var output in recipe.outputItems)
            {
                int have = 0;
                if (output.item != null) _outputBuffer.TryGetValue(output.item, out have);
                info.outputs.Add(new IOEntry {
                    item = output.item, have = have, cap = outputBufferSize, want = output.amount
                });
            }
        }
        return info;
    }

    #endregion
}