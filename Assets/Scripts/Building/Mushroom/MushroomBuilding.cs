using System.Collections.Generic;
using UnityEngine;

public class MushroomBuilding : MultiGridBuilding, ITickable ,IProductionInfoProvider
{
    [Header("Recipe")]
    public RecipeDef recipe;
    public float speedMultiplier = 1f;

    [Header("Buffers")]
    public int inputCap  = 64;
    public int outputCap = 64;
    
    [Header("Output Routing")]
    [Tooltip("若为 true，则产出物直接存入全局仓库（参考 Trashroom），不进入本地输出缓冲/不经输出端口。")]
    public bool sendOutputsToInventory = false;
    
    [Header("Loot Popup")]
    public bool showLootPopup = true;
    public float lootPopupHeight = 1.0f;
    public float lootPopupDuration = 0.9f;
    public Vector3 lootPopupOffset = new Vector3(0f, 1.1f, 0f);
    public int lootPopupMaxBurst = 3;     // 数量很大时最多同时弹几个
    public float lootPopupBurstSpread = 0.25f; // 多个图标的水平散开半径

    private float lootPopupItemSpacing = 0.8f; 
    
    private readonly Dictionary<ItemDef, int> _inputs  = new();
    private readonly Dictionary<ItemDef, int> _outputs = new();

    // 生产状态
    public float productionProgress = 0f;
    public bool  isProducing        = false;

    private readonly List<Vector2Int> _inputOuterCells  = new();
    private readonly List<Vector2Int> _outputOuterCells = new();
    
    private int _outputRR = 0;
 
    private readonly List<ItemPort> _registeredOuterPorts = new();
    
    private MushroomAnimator _mushroomAnimator;

    #region 生命周期
    
    private void Awake()
    {
        buildZone = BuildZone.SurfaceOnly;
        _mushroomAnimator = transform.GetChild(0).GetComponent<MushroomAnimator>();
    }
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        RebuildOuterCells();
        RegisterOuterPorts();

        TickManager.Instance?.Register(this);
    }
    
    public override void OnRemoved()
    {
        UnregisterOuterPorts();
        TickManager.Instance?.Unregister(this);
        base.OnRemoved();
    }
    
    #endregion

    #region 端口格计算
    
    private static Vector2Int SideToOffset(CellSide side) => side switch {
        CellSide.Up    => new Vector2Int(0, 1),
        CellSide.Right => new Vector2Int(1, 0),
        CellSide.Down  => new Vector2Int(0,-1),
        CellSide.Left  => new Vector2Int(-1,0),
        _ => Vector2Int.zero
    };

    private static Vector2Int RotateLocal(Vector2Int p, int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        return steps switch {
            0 => p,
            1 => new Vector2Int( p.y, -p.x),
            2 => new Vector2Int(-p.x, -p.y),
            3 => new Vector2Int(-p.y,  p.x),
            _ => p
        };
    }

    private static CellSide RotateSide(CellSide side, int steps)
    {
        int v = (((int)side) + steps) % 4;
        return (CellSide)v;
    }

    private void RebuildOuterCells()
    {
        _inputOuterCells.Clear();
        _outputOuterCells.Clear();

        foreach (var def in portDefs)
        {
            var local = RotateLocal(def.localCell, rotationSteps);
            var side = RotateSide(def.side, rotationSteps);

            var outer = cell + local + SideToOffset(side); // “端口外侧”的格
            if (def.type == PortType.Input) _inputOuterCells.Add(outer);
            else _outputOuterCells.Add(outer);
        }
    }
    
    // 把端口注册到“外侧格”
    private void RegisterOuterPorts()
    {
        UnregisterOuterPorts();

        foreach (var def in portDefs)
        {
            var local = RotateLocal(def.localCell, rotationSteps);
            var side = RotateSide(def.side, rotationSteps);
            var outer = cell + local ;

            // 在“外侧格”注册一个能把 I/O 转发到本建筑的端口
            var port = new ItemPort(outer, this, def.type);
            grid.RegisterPort(outer, port);
            _registeredOuterPorts.Add(port);
        }
    }

    private void UnregisterOuterPorts()
    {
        for (int i = 0; i < _registeredOuterPorts.Count; i++)
        {
            var p = _registeredOuterPorts[i];
            if (p != null) grid.UnregisterPort(p.Cell, p);
        }
        _registeredOuterPorts.Clear();
    }
    
    #endregion

    #region ITickable

    public void Tick(float dt)
    {
        if (recipe == null) return;

        TryPullInputItems();     // 从输入外侧格的邻居端口拉需要的料
        UpdateProduction(dt);    // 检查能否开工、推进进度、完成产出
        TryPushOutputItems();    // 向输出外侧格的邻居端口推产物
        
        // 生产时调用动画
        if (isProducing)
        {
            _mushroomAnimator.PlaySquashAndStretch();  // 每次生产时调用动画
        }
    }

    private void TryPullInputItems()
    {
        if (recipe == null || !HasSpaceInInputBuffer()) return;

        // 遍历所有输入外侧格，找邻居的“可提供”端口
        foreach (var neighborCell in _inputOuterCells)
        {
            var port = grid.GetPortAt(neighborCell);
            if (port == null || !port.CanProvide) continue;

            foreach (var need in recipe.inputItems)
            {
                if (need.item == null || need.amount <= 0) continue;
                if (GetInputCount(need.item) >= need.amount) continue;

                ItemPayload payload = new ItemPayload { item = need.item, amount = 1 };
                if (port.TryProvide(ref payload) && payload.amount > 0)
                {
                    AddCount(_inputs, payload.item, payload.amount);
                }
            }
        }
    }

    private void TryPushOutputItems()
    {
        if (recipe == null || GetTotal(_outputs) == 0) return;
        if (_outputOuterCells.Count == 0) return;

        int start = _outputRR % _outputOuterCells.Count;

        for (int k = 0; k < _outputOuterCells.Count; k++)
        {
            int idx = (start + k) % _outputOuterCells.Count;
            var port = grid.GetPortAt(_outputOuterCells[idx]);
            if (port == null || !port.CanReceive) continue;

            foreach (var outDef in recipe.outputItems)
            {
                var item = outDef.item;
                if (item == null) continue;
                if (!_outputs.TryGetValue(item, out int have) || have <= 0) continue;

                int send = Mathf.Min(outDef.amount, have);
                if (send <= 0) continue;

                ItemPayload payload = new ItemPayload { item = item, amount = send, worldPos = grid.CellToWorld(cell) };
                if (port.TryReceive(in payload))
                {
                    AddCount(_outputs, item, -send);
                    _outputRR = idx + 1; // 下次从下一个端口开始
                    return;
                }
            }
        }
    }

    private void UpdateProduction(float dt)
    {
        if (recipe == null) return;

        // 不能生产时先尝试开工
        if (!isProducing && CanStartProduction())
        {
            StartProduction();
        }

        if (isProducing)
        {
            productionProgress += dt * Mathf.Max(0.001f, speedMultiplier);

            if (productionProgress >= recipe.productionTime)
            {
                CompleteProduction();
            }
        }
    }
    
    #endregion

    #region 生产规则

    private bool CanStartProduction()
    {
        if (recipe == null) return false;

        // 输入是否足够
        foreach (var input in recipe.inputItems)
        {
            if (input.item == null || input.amount <= 0) continue;
            if (GetInputCount(input.item) < input.amount) return false;
        }

        // 输出是否有空间（只有当不直接入仓库时才判断本地输出缓冲）
        if (!sendOutputsToInventory)
        {
            int willAdd = 0;
            foreach (var output in recipe.outputItems)
            {
                willAdd += Mathf.Max(0, output.amount);
            }
            if (GetTotal(_outputs) + willAdd > outputCap) return false;
        }

        return true;
    }

    private void StartProduction()
    {
        if (recipe == null) return;

        // 扣材料
        foreach (var input in recipe.inputItems)
        {
            if (input.item == null || input.amount <= 0) continue;
            AddCount(_inputs, input.item, -input.amount);
        }

        productionProgress = 0f;
        isProducing = true;
    }
    
    private void CompleteProduction()
    {
        if (recipe == null) return;

        // 1) 本次产出汇总（用于一次性弹出多物品）
        var awarded = new List<(ItemDef item, int amount)>();

        foreach (var outDef in recipe.outputItems)
        {
            if (outDef.item == null || outDef.amount <= 0) continue;

            if (sendOutputsToInventory)
            {
                InventoryManager.Instance.AddItem(outDef.item, outDef.amount);
                awarded.Add((outDef.item, outDef.amount));
            }
            else
            {
                AddCount(_outputs, outDef.item, outDef.amount);
            }
        }

        // 2) 如果是直接入仓库，集中一次性弹出所有图标（并排显示）
        if (sendOutputsToInventory && showLootPopup && awarded.Count > 0)
        {
            SpawnLootPopupBatch(awarded);
        }

        productionProgress = 0f;
        isProducing = false;
    }

    #endregion

    #region I/O回调

    // 供料
    public override bool ReceiveItem(in ItemPayload payload)
    {
        if (payload.item == null || payload.amount <= 0) return false;
        if (!HasSpaceInInputBuffer()) return false;

        AddCount(_inputs, payload.item, payload.amount);
        return true;
    }

    // 取货
    public override bool ProvideItem(ref ItemPayload payload)
    {
        if (recipe == null) return false;

        foreach (var outDef in recipe.outputItems)
        {
            var item = outDef.item;
            if (item == null) continue;

            if (_outputs.TryGetValue(item, out int have) && have > 0)
            {
                int give = Mathf.Min(outDef.amount, have);
                payload.item     = item;
                payload.amount   = give;
                payload.worldPos = grid != null ? grid.CellToWorld(cell) : transform.position;

                AddCount(_outputs, item, -give);
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Buffer

    private int GetInputCount(ItemDef item)
        => (item != null && _inputs.TryGetValue(item, out int v)) ? v : 0;

    private static void AddCount(Dictionary<ItemDef, int> dict, ItemDef item, int delta)
    {
        if (item == null || delta == 0) return;
        dict.TryGetValue(item, out int v);
        v += delta;
        if (v <= 0) dict.Remove(item);
        else dict[item] = v;
    }

    private static int GetTotal(Dictionary<ItemDef, int> dict)
    {
        int total = 0;
        foreach (var v in dict.Values) total += v;
        return total;
    }

    private bool HasSpaceInInputBuffer() => GetTotal(_inputs) < inputCap;

    #endregion
    
    public void GetPortWorldInfos(List<PortWorldInfo> buffer)
    {
        buffer.Clear();
        if (grid == null) return;

        foreach (var def in portDefs)
        {
            var local = RotateLocal(def.localCell, rotationSteps);
            var side  = RotateSide(def.side, rotationSteps);
            var outer = cell + local + SideToOffset(side);   // 外侧格 = 实际端口位置
            var p     = grid.CellToWorld(outer);

            buffer.Add(new PortWorldInfo
            {
                worldPos = p,
                type = def.type,
                side = side
            });
        }
    }

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
                if (input.item != null) _inputs.TryGetValue(input.item, out have);
                info.inputs.Add(new IOEntry {
                    item = input.item, have = have, cap = inputCap, want = input.amount
                });
            }
            foreach (var output in recipe.outputItems)
            {
                int have = 0;
                if (output.item != null) _outputs.TryGetValue(output.item, out have);
                info.outputs.Add(new IOEntry {
                    item = output.item, have = have, cap = outputCap, want = output.amount
                });
            }
        }
        return info;
    }

    #endregion
    
    private void SpawnLootPopup(ItemDef item, int amount)
    {
        // 1) 取图标：假设 ItemDef 有 icon 字段（若你项目是 item.iconSprite 或 uiSprite，请改成对应字段）
        Sprite icon = item != null ? item.icon : null;
        if (icon == null) return;

        // 2) 世界位置（建筑头顶 + 自定义偏移）
        Vector3 startPos = (grid != null ? grid.CellToWorld(cell) : transform.position) + lootPopupOffset;

        // 3) 大量物品时做“burst”效果，最多弹出 lootPopupMaxBurst 个
        int burst = Mathf.Clamp(amount, 1, Mathf.Max(1, lootPopupMaxBurst));
        for (int i = 0; i < burst; i++)
        {
            // 让每个图标稍微左右散开一点
            float angle = (burst == 1) ? 0f : (i / (float)(burst - 1) - 0.5f) * Mathf.PI * 0.35f;
            Vector3 jitter = new Vector3(Mathf.Sin(angle) * lootPopupBurstSpread, 0f, 0f);

            FloatingLootIcon.Spawn(
                icon,
                startPos + jitter,
                lootPopupHeight,
                lootPopupDuration
            );
        }
    }
    
    private void SpawnLootPopupBatch(List<(ItemDef item, int amount)> awarded)
    {
        // 基准位置：建筑头顶
        Vector3 center = (grid != null ? grid.CellToWorld(cell) : transform.position) + lootPopupOffset;
        int n = awarded.Count;
        if (n <= 0) return;

        // 使一排图标居中摆放
        float totalWidth = lootPopupItemSpacing * (n - 1);
        float left = -totalWidth * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var (item, amt) = awarded[i];
            if (item == null || item.icon == null) continue;

            Vector3 pos = center + new Vector3(left + i * lootPopupItemSpacing, 0f, 0f);

            // 同时出现、各自向上飘 & 淡出，附带数量角标
            FloatingLootIcon.Spawn(
                icon: item.icon,
                worldPos: pos,
                height: lootPopupHeight,
                duration: lootPopupDuration,
                label: (amt > 1 ? $"×{amt}" : null)
            );
        }
    }
}

public struct PortWorldInfo
{
    public Vector3 worldPos;
    public PortType type;
    public CellSide side;
}