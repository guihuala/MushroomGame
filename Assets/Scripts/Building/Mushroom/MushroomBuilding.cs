using System.Collections.Generic;
using UnityEngine;

public class MushroomBuilding : MultiGridBuilding, ITickable
{
    [Header("Recipe")]
    public RecipeDef recipe;
    public float speedMultiplier = 1f;

    [Header("Buffers")]
    public int inputCap  = 64;
    public int outputCap = 64;
    
    private readonly Dictionary<ItemDef, int> _inputs  = new();
    private readonly Dictionary<ItemDef, int> _outputs = new();

    // 生产状态
    public float productionProgress = 0f;
    public bool  isProducing        = false;

    // 缓存两个端口所在外侧格
    private Vector2Int _inputPortCell;   // 第1格下方
    private Vector2Int _outputPortCell;  // 第2格下方
    
    private MushroomAnimator _mushroomAnimator;

    #region 生命周期

    private void Awake()
    {
        portDefs.Clear();
        portDefs.Add(new PortDefinition { localCell = new Vector2Int(0, 0), side = CellSide.Down, type = PortType.Input  });
        portDefs.Add(new PortDefinition { localCell = new Vector2Int(1, 0), side = CellSide.Down, type = PortType.Output });

        buildZone = BuildZone.SurfaceOnly;
        
        _mushroomAnimator = GetComponent<MushroomAnimator>();
    }

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);

        RecalculatePortCells();
        BuildAndRegisterPorts(cell);

        TickManager.Instance?.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        base.OnRemoved();
    }
    
    #endregion

    #region 端口格计算

    private static Vector2Int SideToOffset(CellSide side)
    {
        switch (side)
        {
            case CellSide.Up:    return new Vector2Int(0, 1);
            case CellSide.Right: return new Vector2Int(1, 0);
            case CellSide.Down:  return new Vector2Int(0, -1);
            case CellSide.Left:  return new Vector2Int(-1, 0);
            default:             return Vector2Int.zero;
        }
    }

    private static Vector2Int RotateLocal(Vector2Int p, int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        switch (steps)
        {
            case 0: return p;
            case 1: return new Vector2Int(p.y, -p.x);
            case 2: return new Vector2Int(-p.x, -p.y);
            case 3: return new Vector2Int(-p.y, p.x);
            default: return p;
        }
    }

    private static CellSide RotateSide(CellSide side, int steps)
    {
        int v = (((int)side) + steps) % 4;
        return (CellSide)v;
    }

    private void RecalculatePortCells()
    {
        var inLocal   = RotateLocal(new Vector2Int(0, 0), rotationSteps);
        var inSide    = RotateSide(CellSide.Down, rotationSteps);
        _inputPortCell  = cell + inLocal + SideToOffset(inSide);

        var outLocal  = RotateLocal(new Vector2Int(1, 0), rotationSteps);
        var outSide   = RotateSide(CellSide.Down, rotationSteps);
        _outputPortCell = cell + outLocal + SideToOffset(outSide);
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

        var port = grid.GetPortAt(_inputPortCell);
        if (port == null || !port.CanProvide) return;

        // 按配方逐项补齐
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

    private void TryPushOutputItems()
    {
        if (recipe == null || GetTotal(_outputs) == 0) return;

        var port = grid.GetPortAt(_outputPortCell);
        if (port == null || !port.CanReceive) return;

        // 逐项尝试推送
        foreach (var outDef in recipe.outputItems)
        {
            var item = outDef.item;
            if (item == null) continue;

            if (!_outputs.TryGetValue(item, out int have) || have <= 0) continue;

            var send = Mathf.Min(outDef.amount, have);
            if (send <= 0) continue;

            ItemPayload payload = new ItemPayload
            {
                item     = item,
                amount   = send,
                worldPos = grid != null ? grid.CellToWorld(cell) : transform.position
            };

            if (port.TryReceive(in payload))
            {
                AddCount(_outputs, item, -send);
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

        // 输出是否有空间
        int willAdd = 0;
        foreach (var output in recipe.outputItems)
        {
            willAdd += Mathf.Max(0, output.amount);
        }
        return GetTotal(_outputs) + willAdd <= outputCap;
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

        foreach (var outDef in recipe.outputItems)
        {
            if (outDef.item == null || outDef.amount <= 0) continue;
            AddCount(_outputs, outDef.item, outDef.amount);
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
}