using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Building, ITickable, IItemPort, IOrientable, IAutoTiler
{
    public Vector2Int inDir = Vector2Int.left;    // 输入方向（固定）
    public Vector2Int outDir = Vector2Int.right;  // 输出方向（自动调整）
    private readonly Queue<ItemPayload> _buffer = new();
    private const int BUFFER_CAPACITY = 3;
    
    private IItemPort _connectedOutputPort = null;
    private Vector2Int _connectedDirection = Vector2Int.zero;
    
    private static Vector2Int RotateCW(Vector2Int d) => new(d.y, -d.x);
    private static Vector2Int RotateCCW(Vector2Int d) => new(-d.y, d.x);
    private static bool IsOpposite(Vector2Int a, Vector2Int b) => a + b == Vector2Int.zero;

    public bool CanPull => _buffer.Count < BUFFER_CAPACITY;
    public bool CanPush => _buffer.Count > 0;
    
    private List<Vector3> conveyorPath = new List<Vector3>();

    #region 摆放和移除

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);

        MsgCenter.RegisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);

        AutoTile(); // 初始化调用自检
        FindBestOutputConnection(); // 尝试建立下游连接
        BuildLocalPath(); // 构建本地路径
    }

    // 自检方法：仅触发与“我 outDir”正交、且已连通的邻居做自检
    public void AutoTile()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        foreach (var d in dirs)
        {
            var nCell = cell + d;
            var port = grid.GetPortAt(nCell);
            if (port is not Conveyor neighbor) continue;

            // 只有当邻居的 inDir/outDir 与“我”的 outDir 成 90°，且邻居本身已连通时，才触发它自检
            bool ortho = IsPerpendicular(neighbor.inDir, outDir) || IsPerpendicular(neighbor.outDir, outDir);
            if (ortho && HasDirectionalNeighbor(grid, neighbor))
            {
                Debug.Log($"[AutoTile] Neighbor at {nCell} is valid, performing self-check");
                NeighborSelfCheck(neighbor, d);
            }
        }

        // 自己：只评估下游与路径（不调整 in/out）
        FindBestOutputConnection();
        BuildLocalPath();
    }
    
// a = 被自检的邻居传送带
    private static bool HasDirectionalNeighbor(TileGridService grid, Conveyor a)
    {
        // 检查：a 的下游（a.outDir）
        var downCell = a.cell + a.outDir;
        var downPort = grid.GetPortAt(downCell);
        if (IsValidDownstream(a.outDir, downPort))
            return true;

        // 检查：a 的上游（在 a.cell - a.inDir）
        var upCell = a.cell - a.inDir;             // 注意：上游在 cell - inDir
        var upPort = grid.GetPortAt(upCell);
        if (IsValidUpstream(a.inDir, upPort))
            return true;

        return false;
    }

    // 下游是否“已连通/可连通”：我把货沿 dirFromA 给它
    private static bool IsValidDownstream(Vector2Int dirFromA, IItemPort down)
    {
        if (down == null || !down.CanPull) return false;

        switch (down)
        {
            case Conveyor c:
                // 我给它的方向 == 它的入口反向（它的 inDir 指向它的内部）
                return dirFromA == -c.inDir;

            case HubPort _:
                // HubPort 只接收，不做方向判断
                return true;

            // 将来其它只接收的建筑：只要 CanPull 就行；若你以后有带“输入方向”的处理器，
            // 可以在这里加一个分支检查它的 inDir 是否朝向我
            default:
                return true;
        }
    }

    // 上游是否“已连通/可连通”：它把货沿 dirToA 给我
    private static bool IsValidUpstream(Vector2Int dirToA, IItemPort up)
    {
        if (up == null || !up.CanPush) return false;

        switch (up)
        {
            case Conveyor c:
                // 上游传送带的 outDir 必须正好指向我
                return c.outDir == dirToA;

            case Miner m: // 只输出，不能拉
                return m.outDir == dirToA;  // 产出方向要指向我（你的 Miner 有 outDir）  

            // 若将来有“多输入单输出”的处理器作为上游：
            // 也检查它的 outDir 是否 == dirToA；若它没有方向概念，就只看 CanPush
            default:
                return true;
        }
    }

    
    // 正交方向判断
    private static bool IsPerpendicular(Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y == 0;

    // 判断方向是否兼容
    private bool IsDirectionCompatible(Vector2Int direction, IItemPort downstreamPort)
    {
        if (downstreamPort is Conveyor downstreamConveyor)
        {
            return direction == -downstreamConveyor.inDir;
        }
        return true;
    }
    
    private void NeighborSelfCheck(Conveyor neighbor, Vector2Int dirToNeighbor)
    {
        // dBA：我 -> 邻居；dAB：邻居 -> 我
        var dBA = dirToNeighbor;
        var dAB = -dirToNeighbor;
        
        // 1) 我吃邻居：我.in == dBA 且 邻.out == dAB
        bool alreadyMatch_in  = (inDir == dBA)  && (neighbor.outDir == dAB);
        // 2) 我喂邻居：我.out == dBA 且 邻.in  == dAB
        bool alreadyMatch_out = (outDir == dBA) && (neighbor.inDir  == dAB);

        Debug.Log($"[NeighborSelfCheck] me {cell} vs neighbor {neighbor.cell}, " +
                  $"me(in:{inDir},out:{outDir}), nb(in:{neighbor.inDir},out:{neighbor.outDir}), " +
                  $"dBA:{dBA}, dAB:{dAB}");

        if (alreadyMatch_in || alreadyMatch_out)
        {
            Debug.Log($"[NeighborSelfCheck] No change needed (already matched).");
            return;
        }

        // ——只改“邻居”的那一侧——
        // 情况A：我准备吃邻居（我.in == dBA），但“邻居.out != dAB” → 改邻居.out 让它朝我
        if (inDir == dBA && neighbor.outDir != dAB)
        {
            neighbor.outDir = dAB;
            // 邻居的 inDir 不强制与 outDir 相反（允许拐弯）；只改这一侧
            ApplyNeighborPostChange(neighbor, "Fix neighbor.out -> me");
            return;
        }

        // 情况B：我准备喂邻居（我.out == dBA），但“邻居.in != dAB” → 改邻居.in 让它从我这边进
        if (outDir == dBA && neighbor.inDir != dAB)
        {
            neighbor.inDir = dAB;
            ApplyNeighborPostChange(neighbor, "Fix neighbor.in <- me");
            return;
        }

        Debug.Log($"[NeighborSelfCheck] Nothing to change after checks.");
    }

    // 改完邻居后做收尾 & 通知
    private void ApplyNeighborPostChange(Conveyor neighbor, string reason)
    {
        neighbor.transform.right = new Vector3(neighbor.outDir.x, neighbor.outDir.y, 0f);
        neighbor.BuildLocalPath();
        neighbor.FindBestOutputConnection();
        Debug.Log($"[NeighborSelfCheck] {reason}. neighbor now in:{neighbor.inDir}, out:{neighbor.outDir}");

        // 允许连锁稳定（只触发邻居自己的一轮）
        neighbor.AutoTile();
        Debug.Log($"[NotifyNeighborChanged] Notified neighbor {neighbor.cell} to self-check once.");
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        
        MsgCenter.UnregisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        
        _connectedOutputPort = null;
        base.OnRemoved();
    }    

    #endregion

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = -dir; // 保证入口与出口是相对的
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }

    public override void OnNeighborChanged()
    {
        AutoTile();
    }

    public bool TryPull(ref ItemPayload payload)
    {
        if (_buffer.Count >= BUFFER_CAPACITY) return false;
        _buffer.Enqueue(payload);
        return true;
    }

    public bool TryPush(in ItemPayload payload)
    {
        if (_buffer.Count == 0) return false;
        _buffer.Dequeue();
        return true;
    }

    public void Tick(float dt)
    {
        if (_buffer.Count == 0) return;

        if (!IsCurrentConnectionValid())
        {
            FindBestOutputConnection();
        }

        var payload = _buffer.Peek();
        
        if (payload.item != null)
        {
            Vector3 startPos = grid.CellToWorld(cell) - new Vector3(inDir.x, inDir.y, 0f) * (grid.cellSize * 0.5f);
            var iconObject = ItemIconManager.Instance.CreateItemIcon(payload.item, startPos);
            var flowAnimation = iconObject.AddComponent<ItemFlowAnimation>();
            flowAnimation.Init(startPos, conveyorPath);
        }

        if (_connectedOutputPort != null && _connectedOutputPort.CanPull)
        {
            var pkg = _buffer.Peek();
            if (_connectedOutputPort.TryPull(ref pkg))
            {
                TryPush(pkg);
                Debug.Log($"[Tick] Conveyor {cell} -> {cell + _connectedDirection}");
            }
        }
    }
    
    private bool IsCurrentConnectionValid()
    {
        if (_connectedOutputPort == null) return false;
        
        var targetCell = cell + _connectedDirection;
        var currentPort = grid.GetPortAt(targetCell);
        
        return ReferenceEquals(currentPort, _connectedOutputPort) && 
               IsDirectionCompatible(_connectedDirection, currentPort);
    }
    
    private void FindBestOutputConnection()
    {
        _connectedOutputPort = null;
        _connectedDirection = Vector2Int.zero;

        var forward = outDir;
        var right = RotateCW(outDir);
        var left = RotateCCW(outDir);
        var candidates = new[] { forward, right, left };

        foreach (var direction in candidates)
        {
            if (IsOpposite(direction, inDir)) continue;

            var targetCell = cell + direction;
            var port = grid.GetPortAt(targetCell);

            if (port == null || !port.CanPull || !IsDirectionCompatible(direction, port)) continue;

            outDir = direction;
            _connectedDirection = direction;
            _connectedOutputPort = port;
            transform.right = new Vector3(outDir.x, outDir.y, 0f);
            BuildLocalPath();
            Debug.Log($"[FindBestOutputConnection] Connected to {targetCell} with direction {direction}");
            return;
        }

        var fallbackCell = cell + outDir;
        if (grid.IsTouchingSurface(fallbackCell))
        {
            MsgCenter.SendMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, fallbackCell);
        }

        BuildLocalPath();
    }

    private void BuildLocalPath()
    {
        conveyorPath.Clear();

        Vector3 center = grid.CellToWorld(cell);
        float half = grid.cellSize * 0.5f;
        float edgeOffset = half;

        Vector3 inPoint = center - new Vector3(inDir.x, inDir.y, 0f) * edgeOffset;
        Vector3 outPoint = center + new Vector3(outDir.x, outDir.y, 0f) * edgeOffset;

        conveyorPath.Add(inPoint);

        if (!(inDir + outDir == Vector2Int.zero) && !(inDir == -outDir))
        {
            conveyorPath.Add(center);
        }

        conveyorPath.Add(outPoint);
    }

    private void OnNeighborChangedMsg(params object[] args)
    {
        if (args.Length > 0 && args[0] is Vector2Int changedCell)
        {
            Vector2Int[] directions = { 
                Vector2Int.up, Vector2Int.right, 
                Vector2Int.down, Vector2Int.left 
            };
            
            foreach (var dir in directions)
            {
                if (cell + dir == changedCell)
                {
                    OnNeighborChanged();
                    return;
                }
            }
        }
    }
}
