using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Building, ITickable, IItemPort, IOrientable, IAutoTiler
{
    public Vector2Int inDir  = Vector2Int.left;    // 默认从左边吃
    public Vector2Int outDir = Vector2Int.right;   // 默认向右吐

    private readonly Queue<ItemPayload> _buffer = new();
    private const int BUFFER_CAPACITY = 3;

    // 下游连接缓存（用于快速推送）
    private IItemPort _connectedOutputPort = null;
    private Vector2Int _connectedDirection = Vector2Int.zero;

    // 给物品图标动画的路径点
    private readonly List<Vector3> _conveyorPath = new();

    // 防抖：一帧只跑一次 AutoTile
    private int _lastAutoTileFrame = -1;
    
    private static Vector2Int RotCW  (Vector2Int v) => new Vector2Int(v.y, -v.x);
    private static Vector2Int RotCCW (Vector2Int v) => new Vector2Int(-v.y, v.x);
    private static bool       IsOpp  (Vector2Int a, Vector2Int b) => a + b == Vector2Int.zero;

    public bool CanProvide => _buffer.Count > 0;               // 我能提供物品
    public bool CanReceive => _buffer.Count < BUFFER_CAPACITY; // 我能接收物品

    #region 放置/移除/邻居消息

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid = g;
        cell = c;

        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
        MsgCenter.RegisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        
        AutoTile();
    }

    public override void OnRemoved()
    {
        MsgCenter.UnregisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        
        _connectedOutputPort = null;
        base.OnRemoved();
    }

    public override void OnNeighborChanged()
    {
        AutoTile();
    }

    private void OnNeighborChangedMsg(params object[] args)
    {
        if (args.Length > 0 && args[0] is Vector2Int changed)
        {
            // 只关心四邻
            if ((changed - cell).sqrMagnitude == 1)
            { 
                OnNeighborChanged();
            }
        }
    }

    #endregion

    #region IOrientable / IAutoTiler

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir  = -dir;
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
        BuildLocalPath();
        FindBestOutputConnection_ByRouter();
        Debug.Log($"[SetDirection] {cell} out:{outDir} in:{inDir}");
    }

    /// <summary>只触发邻居自检，并在末尾重连下游+刷新路径</summary>
    public void AutoTile()
    {
        if (_lastAutoTileFrame == Time.frameCount) return;
        _lastAutoTileFrame = Time.frameCount;

        AutoTileSystem.RewireAround(grid, this);
    }

    /// <summary>系统层设置方向后统一收尾</summary>
    public void ApplyDirAndRebuild()
    {
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
        BuildLocalPath();
        FindBestOutputConnection_ByRouter();
    }

    #endregion

    #region IItemPort / ITickable

    public bool TryReceive(in ItemPayload payload)
    {
        if (_buffer.Count >= BUFFER_CAPACITY) return false;

        // 入列时生成图标动画一次
        _buffer.Enqueue(payload);

        if (payload.item != null)
        {
            BuildLocalPath(); // 确保路径最新
            float half = grid.cellSize * 0.5f;
            Vector3 start = grid.CellToWorld(cell) - new Vector3(inDir.x, inDir.y, 0f) * half;
            var icon = ItemIconManager.Instance.CreateItemIcon(payload.item, start);
            var anim = icon.AddComponent<ItemFlowAnimation>();
            anim.Init(start, _conveyorPath);
        }

        return true;
    }

    public bool TryProvide(ref ItemPayload payload) // 改为 ref
    {
        if (_buffer.Count == 0) return false;
    
        payload = _buffer.Dequeue(); // 提供物品给调用者
        return true;
    }

    public void Tick(float dt)
    {
        if (_buffer.Count == 0) return;

        if (!IsCurrentConnectionValid())
        {
            FindBestOutputConnection_ByRouter();
        }

        if (_connectedOutputPort != null && _connectedOutputPort.CanReceive) // 下游能接收
        {
            var pkg = _buffer.Peek();
            if (_connectedOutputPort.TryProvide(ref pkg)) // 下游从我这里拉取
            {
                _buffer.Dequeue(); // 直接弹出，不需要再调用TryProvide
                Debug.Log($"[Tick] {cell} -> {cell + _connectedDirection}");
            }
        }
    }

    #endregion

    #region 连接选择 / 校验 / 路径
    
    private bool IsCurrentConnectionValid()
    {
        if (_connectedOutputPort == null) return false;
        var targetCell = cell + _connectedDirection;
        var current = grid.GetPortAt(targetCell);
        return ReferenceEquals(current, _connectedOutputPort) &&
               TransportCompat.DownAccepts(_connectedDirection, current);
    }

    /// <summary>
    /// 由“路由器”规则选择下游：前、右、左；跳过回头；用集中兼容判断
    /// </summary>
    public void FindBestOutputConnection_ByRouter()
    {
        _connectedOutputPort = null;
        _connectedDirection = Vector2Int.zero;

        var forward = outDir;
        var right   = RotCW(outDir);
        var left    = RotCCW(outDir);
        var candidates = new[] { forward, right, left };

        foreach (var d in candidates)
        {
            if (IsOpp(d, inDir)) continue; // 禁止回头

            var target = cell + d;
            var port = grid.GetPortAt(target);
            
            outDir = d;
            _connectedDirection = d;
            _connectedOutputPort = port;
            transform.right = new Vector3(outDir.x, outDir.y, 0f);
            BuildLocalPath();
            Debug.Log($"[FindBest] {cell} connected -> {target} dir:{d}");
            return;
        }

        // 没接上也刷新可视（防止路径旧态）
        BuildLocalPath();
    }

    public void BuildLocalPath()
    {
        _conveyorPath.Clear();

        Vector3 center = grid.CellToWorld(cell);
        float half = grid.cellSize * 0.5f;

        Vector3 inPoint  = center - new Vector3(inDir.x,  inDir.y,  0f) * half;
        Vector3 outPoint = center + new Vector3(outDir.x, outDir.y, 0f) * half;

        _conveyorPath.Add(inPoint);

        // 拐弯时插入中心点；直线（inDir == -outDir）不插入
        if (!(inDir == -outDir))
            _conveyorPath.Add(center);

        _conveyorPath.Add(outPoint);
    }

    #endregion
}