using System.Collections.Generic;
using UnityEngine;

public class Crosser : Building, IItemPort, ITickable, IOrientable
{
    [Header("方向")]
    public Vector2Int outDir = Vector2Int.right; // 主轴正向（另一条轴自动取垂直方向）
    public Vector2Int inDir  = Vector2Int.left;  // 主轴反向（= -outDir）

    [Header("容量与节流")]
    public int totalBufferCap = 16;       // 四个方向队列的总容量上限
    public float pushesPerSecond = 8f;    // 每秒最多推送件数（总配额，分方向轮询）
    public bool strictDirCheck = true;    // 推送前做方向兼容检查（建议 true）

    // ============ 端口协议 ============
    public bool CanReceive => _totalCount < totalBufferCap;
    public bool CanProvide => false; // 交叉器不被动被“拉取”，只主动往前推

    // ============ 内部状态 ============
    // 四个“出向队列”映射：key = 出向（从本格到目标格的方向）
    private readonly Dictionary<Vector2Int, Queue<ItemPayload>> _lanes = new();
    private readonly List<Vector2Int> _laneOrder = new(); // 轮询顺序
    private int _totalCount = 0;

    // 推送预算（节流）
    private float _pushBudget = 0f;
    private int _rrIndex = 0; // 方向轮询起点

    #region 生命周期
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        
        g.RegisterPort(cell, this);
        NormalizeDirs();
        BuildLanes();
        
        TickManager.Instance?.Register(this);

        UpdateVisual();
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }
    #endregion

    #region 方向/可视
    public void SetDirection(Vector2Int dir)
    {
        outDir = dir == Vector2Int.zero
            ? Vector2Int.up
            : new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
        inDir = -outDir;

        NormalizeDirs();
        BuildLanes();
        UpdateVisual();
    }

    private void NormalizeDirs()
    {
        outDir = ClampCardinal(outDir);
        inDir  = -outDir;
    }

    private void UpdateVisual()
    {
        transform.up = new Vector3(outDir.x, outDir.y, 0f);
    }
    #endregion

    #region 车道构建
    private static Vector2Int ClampCardinal(Vector2Int v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2Int(v.x > 0 ? 1 : -1, 0);
        if (Mathf.Abs(v.y) > Mathf.Abs(v.x)) return new Vector2Int(0, v.y > 0 ? 1 : -1);
        return (v == Vector2Int.zero) ? Vector2Int.right : v;
    }
    
    private void BuildLanes()
    {
        var aPos = outDir;
        var aNeg = -outDir;
        var bPos = RotCW(outDir);
        var bNeg = -bPos;

        EnsureLane(aPos);
        EnsureLane(aNeg);
        EnsureLane(bPos);
        EnsureLane(bNeg);

        _laneOrder.Clear();
        _laneOrder.Add(aPos); _laneOrder.Add(aNeg); _laneOrder.Add(bPos); _laneOrder.Add(bNeg);
    }

    private void EnsureLane(Vector2Int dir)
    {
        if (!_lanes.ContainsKey(dir)) _lanes[dir] = new Queue<ItemPayload>();
    }
    #endregion

    #region IItemPort
    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (!CanReceive) return false;
        if (grid == null) return false;

        // 通过 payload.worldPos 推断“来货源格”（上游在带->建时不会把 worldPos 改成目标格）
        var srcCell = grid.WorldToCell(payloadIn.worldPos);
        var dirIn = ClampCardinal(cell - srcCell);      // 来货方向（从源->我）
        if (dirIn == Vector2Int.zero) return false;     // 非法

        // 仅接受四向；将“出向”设为与来向相同（直行通过）
        var dstDir = dirIn;

        // 只允许走两条轴（主轴或垂直轴）
        var perp = RotCW(outDir);
        bool onMainAxis = (dstDir == outDir || dstDir == -outDir);
        bool onPerpAxis = (dstDir == perp   || dstDir == -perp);
        if (!onMainAxis && !onPerpAxis) return false;

        // 入队
        var p = payloadIn;
        p.worldPos = grid.CellToWorld(cell); // 进入本格，定位到中心
        _lanes[dstDir].Enqueue(p);
        _totalCount++;
        return true;
    }
    
    public bool TryProvide(ref ItemPayload payload) => false;
    #endregion

    #region ITickable：推动四向队首
    public void Tick(float dt)
    {
        if (grid == null || _totalCount == 0) return;

        _pushBudget += Mathf.Max(0f, pushesPerSecond) * dt;
        int quota = Mathf.FloorToInt(_pushBudget);
        if (quota <= 0) return;

        int sent = 0;
        int lanes = _laneOrder.Count;

        for (int step = 0; step < lanes && sent < quota; step++)
        {
            int idx = (_rrIndex + step) % lanes;
            var dir = _laneOrder[idx];

            if (TryPushLane(dir))
            {
                sent++;
                _rrIndex = (idx + 1) % lanes; // 成功后从下一方向开始
            }
        }

        _pushBudget -= sent; // 扣预算
    }

    private bool TryPushLane(Vector2Int dir)
    {
        var q = _lanes[dir];
        if (q.Count == 0) return false;

        var targetCell = cell + dir;
        var port = grid.GetPortAt(targetCell);
        if (port == null || !port.CanReceive) return false;

        if (strictDirCheck && !TransportCompat.DownAccepts(dir, port)) return false;

        var payload = q.Peek();
        payload.worldPos = grid.CellToWorld(targetCell);

        if (port.TryReceive(in payload))
        {
            q.Dequeue();
            _totalCount--;
            return true;
        }

        return false;
    }
    #endregion
}
