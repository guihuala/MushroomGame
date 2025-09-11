using System.Collections.Generic;
using UnityEngine;

public class Conveyer : Building, IItemPort, IOrientable, IBeltNode
{
    [Header("传送带设置")] public float beltSpeed = 1.0f; // 每秒移动的格子数
    public Vector2Int inDir = Vector2Int.left;
    public Vector2Int outDir = Vector2Int.right;

    [Header("物品间距设置")] public float itemSpacing = 0.3f; // 物品之间的最小间距（0-1之间）

    [Header("容量限制")] public int maxItems = 3;
    
    private readonly List<BeltItem> _items = new();
    public IReadOnlyList<BeltItem> Items => _items;
    
    private IItemPort _connectedOutputPort;
    private Vector2Int _connectedDirection;

    // 自动铺路相关
    private int _lastAutoTileFrame = -1;
    private static Vector2Int RotCW(Vector2Int v) => new(v.y, -v.x);
    private static Vector2Int RotCCW(Vector2Int v) => new(-v.y, v.x);

    public bool CanProvide => _items.Count > 0 && _items[0].pos >= 0.95f;
    public bool CanReceive => _items.Count < maxItems;

    #region 菌丝可视化参数

    // === 菌丝直线（全体传送带共享的渲染配置） ===
    [Header("Path Line (Mycelium) - Global for all conveyors")]
    [SerializeField] private bool pathLineEnabled = true;
    [SerializeField] private float pathLineWidth = 0.04f;
    [SerializeField] private Color pathLineColor = new Color(0.90f, 1.00f, 0.90f, 0.90f);
    [SerializeField] private Material pathLineMaterial; // 为空则用 Sprites/Default

    // 静态共享
    private static bool s_pathInit = false;
    private static bool s_enabled = true;
    private static float s_width = 0.04f;
    private static Color s_color = new Color(0.90f, 1.00f, 0.90f, 0.90f);
    private static Material s_material;
    private static Transform s_lineRoot;
    private static readonly List<PathLine> s_lines = new();
    private static readonly HashSet<Conveyer> s_all = new();
    private static bool s_dirty = false;
    private static float s_lastBuildTime = -999f;
    private const float REBUILD_DEBOUNCE = 0.02f; // 20ms 合并抖动，避免频繁拆装抖动
    
    [Header("Rounded Corners")]
    [SerializeField] private int cornerVertices = 6;   // 圆角细腻程度（2~8）
    [SerializeField] private int capVertices    = 2;   // 两端圆帽

    [Header("Powered Highlight")]
    [SerializeField] private Color highlightColor    = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    [SerializeField] private float highlightFadePerSec = 2.5f;   // 高亮衰减速度（每秒）
    
    private static Color    s_highlightColor = Color.white;
    private static float    s_glowFade = 2.5f;
    private static int      s_cornerV  = 6, s_capV = 2;
    
    // 每条线的对象与状态
    private struct PathLine {
        public LineRenderer lr;
        public List<Conveyer> members;  // 参与这条线的带子
        public float glow;               // 0..1 高亮强度（逐帧衰减）
    }
    private static readonly Dictionary<Conveyer, int> s_beltToLine = new();
    
    #endregion

    public Vector3 GetWorldPosition()
    {
        var p = grid.CellToWorld(cell);
        return p;
    }

    #region IBeltNode

    public Vector2Int Cell => cell;
    public Vector2Int InDir => inDir;
    public Vector2Int OutDir => outDir;

    public virtual void StepMove(float dt)
    {
        UpdateItemPositions(dt);
    }

    public virtual void StepTransfer()
    {
        TryTransferFirstItem();
        ClampItemPositions();
    }

    #endregion

    #region 生命周期
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);

        MsgCenter.RegisterMsg(MsgConst.NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        MsgCenter.SendMsg(MsgConst.CONVEYOR_PLACED, this);
        AutoTile();
        BeltScheduler.Instance.RebuildAllPaths();
        MarkPathDirty();
    }

    public override void OnRemoved()
    {
        MsgCenter.SendMsg(MsgConst.CONVEYOR_REMOVED, this);
        MsgCenter.UnregisterMsg(MsgConst.NEIGHBOR_CHANGED, OnNeighborChangedMsg);

        grid.UnregisterPort(cell, this);
        BeltScheduler.Instance.RebuildAllPaths();
        _connectedOutputPort = null;
        MarkPathDirty();
        base.OnRemoved();
    }
    
    protected virtual void Awake()
    {
        s_all.Add(this);
        EnsurePathSystemInitialized();
    }

    protected virtual void OnDestroy()
    {
        s_all.Remove(this);
        MarkPathDirty();
    }
    
    protected virtual void LateUpdate()
    {
        // 1) 路径重建
        if (s_dirty && Time.unscaledTime - s_lastBuildTime >= REBUILD_DEBOUNCE)
        {
            s_lastBuildTime = Time.unscaledTime;
            RebuildAllPathLines();
        }

        // 2) 高亮衰减 & 颜色更新
        if (s_lines.Count > 0 && s_glowFade > 0f)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < s_lines.Count; i++)
            {
                var pl = s_lines[i];
                if (pl.lr == null) continue;

                if (pl.glow > 0f)
                {
                    pl.glow = Mathf.Max(0f, pl.glow - s_glowFade * dt);
                    s_lines[i] = pl;
                }

                Color c = Color.Lerp(s_color, s_highlightColor, pl.glow);
                pl.lr.startColor = pl.lr.endColor = c;
            }
        }
    }

    private void EnsurePathSystemInitialized()
    {
        if (s_pathInit) return;
        s_pathInit = true;

        s_enabled = pathLineEnabled;
        s_width = pathLineWidth;
        s_color = pathLineColor;
        s_highlightColor = highlightColor;
        s_glowFade = Mathf.Max(0.01f, highlightFadePerSec);
        s_cornerV = Mathf.Max(0, cornerVertices);
        s_capV = Mathf.Max(0, capVertices);

        s_material = pathLineMaterial != null
            ? pathLineMaterial
            : new Material(Shader.Find("Sprites/Default"))
                { name = "[Shared] MyceliumPathLine", hideFlags = HideFlags.DontSave };

        var root = GameObject.Find("[Conveyor Path Lines]");
        if (root == null) root = new GameObject("[Conveyor Path Lines]");
        s_lineRoot = root.transform;

        MarkPathDirty();
    }

    private static void MarkPathDirty() => s_dirty = true;

    #endregion

    #region 邻居处理

    public override void OnNeighborChanged() => AutoTile();

    private void OnNeighborChangedMsg(params object[] args)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (args.Length > 0 && args[0] is Vector2Int changed && (changed - cell).sqrMagnitude == 1)
        {
            OnNeighborChanged();
        }
    }

    #endregion

    #region 方向与自动铺路

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = -dir;
        UpdateVisualDirection();
        BeltScheduler.Instance.RebuildAllPaths();
    }

    public void AutoTile()
    {
        if (_lastAutoTileFrame == Time.frameCount) return;
        _lastAutoTileFrame = Time.frameCount;
        AutoTileSystem.RewireAround(grid, this);
    }

    public void ApplyDirAndRebuild()
    {
        UpdateVisualDirection();
        FindBestOutputConnection();
    }

    private void UpdateVisualDirection() => transform.right = new Vector3(outDir.x, outDir.y, 0f);

    #endregion

    #region 物品传输接口

    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (!CanReceive) return false;

        var payload = payloadIn;
        payload.worldPos = GetWorldPosition();

        _items.Add(new BeltItem(payload) { pos = 0f });
        return true;
    }

    public bool TryProvide(ref ItemPayload payload)
    {
        if (!CanProvide) return false;
        payload = _items[0].payload;
        _items.RemoveAt(0);
        return true;
    }

    #endregion

    #region Tick逻辑

    private void UpdateItemPositions(float dt)
    {
        float moveDistance = dt * beltSpeed;

        var nextPort = grid.GetPortAt(cell + outDir);

        bool hasValidOutput = false;

        if (nextPort is Conveyer nextBelt)
        {
            // 带→带：只要下一带“头部留出 itemSpacing”，就视为可输出
            if (nextBelt.Items.Count == 0) hasValidOutput = true;
            else hasValidOutput = nextBelt.Items[0].pos >= itemSpacing;
        }
        else
        {
            // 带→建筑
            hasValidOutput = IsCurrentConnectionValid() && _connectedOutputPort != null &&
                             _connectedOutputPort.CanReceive;
        }

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];

            float maxAllowed = i == 0 ? float.MaxValue : _items[i - 1].pos - itemSpacing;

            bool isLast = i == _items.Count - 1;
            if (isLast && !hasValidOutput)
                maxAllowed = Mathf.Min(maxAllowed, 1f - itemSpacing);

            item.pos = Mathf.Min(item.pos + moveDistance, maxAllowed);

            item.payload.worldPos = GetWorldPosition();
            _items[i] = item;
        }
    }

    private void TryTransferFirstItem()
    {
        if (_items.Count == 0) return;

        Vector2Int nextCell = cell + outDir;
        var nextPort = grid.GetPortAt(nextCell);

        // ① 带 -> 带（几何 + 容量；不走端口协议）
        if (nextPort is Conveyer nextBelt)
        {
            // 先把头部的目标点设为“下一带的格中心”
            var head = _items[0];
            head.pos = Mathf.Min(1f, head.pos);
            head.payload.worldPos = nextBelt.GetWorldPosition();
            _items[0] = head;

            if (nextBelt.TryAcceptFromNeighbour(this))
            {
                _items.RemoveAt(0);
            }
            else
            {
                // 对方满：把位置限制在出口前，并把目标点回滚为“本格中心”（视觉不提前跳）
                head = _items[0];
                head.pos = Mathf.Min(head.pos, 1f - itemSpacing);
                head.payload.worldPos = GetWorldPosition();
                _items[0] = head;
            }

            return;
        }

        // ② 带 -> 建筑（端口协议）
        ValidateConnection();
        if (_connectedOutputPort == null || !_connectedOutputPort.CanReceive)
        {
            if (_items[0].pos > 1f - itemSpacing)
            {
                var item = _items[0];
                item.pos = 1f - itemSpacing;
                item.payload.worldPos = GetWorldPosition();
                _items[0] = item;
            }

            return;
        }

        var payloadOut = _items[0].payload;
        if (_connectedOutputPort.TryReceive(in payloadOut))
        {
            _items.RemoveAt(0);
        }
    }

    private void ClampItemPositions()
    {
        bool hasValidOutput = IsCurrentConnectionValid() && _connectedOutputPort != null &&
                              _connectedOutputPort.CanReceive;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].pos > 1f)
            {
                var item = _items[i];
                bool isLast = i == _items.Count - 1;
                item.pos = (isLast && !hasValidOutput) ? 0.99f : 1f;
                _items[i] = item;
            }
        }
    }

    #endregion

    public bool TryAcceptFromNeighbour(Conveyer from)
    {
        if (!HasSpaceForIncoming()) return false;

        var incoming = from._items[0];
        incoming.pos = 0f;
        // 接收瞬间：把视觉目标设为我方“格中心”
        incoming.payload.worldPos = GetWorldPosition();
        _items.Insert(0, incoming);
        return true;
    }

    private bool HasSpaceForIncoming()
    {
        if (_items.Count == 0) return true;
        return _items[0].pos >= itemSpacing;
    }

    #region 连接管理

    private void ValidateConnection()
    {
        if (!IsCurrentConnectionValid())
            FindBestOutputConnection();
    }

    private bool IsCurrentConnectionValid()
    {
        if (_connectedOutputPort == null) return false;
        var targetCell = cell + _connectedDirection;
        var currentPort = grid.GetPortAt(targetCell);
        return ReferenceEquals(currentPort, _connectedOutputPort) &&
               TransportCompat.DownAccepts(_connectedDirection, currentPort);
    }

    public void FindBestOutputConnection()
    {
        _connectedOutputPort = null;
        _connectedDirection = Vector2Int.zero;

        var directions = new[] { outDir, RotCW(outDir), RotCCW(outDir) };
        foreach (var dir in directions)
        {
            if (dir == inDir) continue;
            var targetCell = cell + dir;
            var port = grid.GetPortAt(targetCell);
            if (port != null && TransportCompat.DownAccepts(dir, port))
            {
                outDir = dir;
                inDir = -dir;
                _connectedDirection = dir;
                _connectedOutputPort = port;
                UpdateVisualDirection();
                return;
            }
        }
    }

    #endregion

    #region 视觉效果

// —— 物流连通判定 —— //
    private static bool IsFlowConnected(Conveyer a, Conveyer b)
    {
        Vector2Int d = b.cell - a.cell;
        if (Mathf.Abs(d.x) + Mathf.Abs(d.y) != 1) return false;
        bool forward = (a.outDir == d) && (b.inDir == -d);
        bool backward = (a.inDir == d) && (b.outDir == -d);
        return forward || backward;
    }

    private static List<Conveyer> GetFlowNeighbors(Conveyer c)
    {
        var result = new List<Conveyer>(2);
        var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var d in dirs)
        {
            var nb = c.grid?.GetBuildingAt(c.cell + d) as Conveyer;
            if (nb != null && IsFlowConnected(c, nb)) result.Add(nb);
        }

        return result;
    }

    // —— 重建所有线段 —— //
    private static void RebuildAllPathLines()
    {
        s_dirty = false;
        ClearAllPathLines();
        s_beltToLine.Clear();
        if (!s_enabled || s_all.Count == 0) return;

        // 1) 构图（仅物流连通的邻居）
        var neighbors = new Dictionary<Conveyer, List<Conveyer>>(s_all.Count);
        foreach (var c in s_all)
        {
            if (c == null || c.grid == null) continue;
            var list = GetFlowNeighbors(c);
            if (list.Count > 0) neighbors[c] = list;
        }

        // 2) 端点优先
        var visited = new HashSet<Conveyer>();
        foreach (var kv in neighbors)
        {
            var node = kv.Key;
            if (visited.Contains(node)) continue;
            if (kv.Value.Count == 1)
                BuildLineFromEndpoint(node, neighbors, visited);
        }

        // 3) 闭环
        foreach (var kv in neighbors)
        {
            var node = kv.Key;
            if (!visited.Contains(node))
                BuildLoopLine(node, neighbors, visited);
        }
    }

    private static void BuildLineFromEndpoint(
        Conveyer start,
        Dictionary<Conveyer, List<Conveyer>> nbr,
        HashSet<Conveyer> visited)
    {
        if (start == null || !nbr.ContainsKey(start)) return;

        var pts     = new List<Vector3>(16);
        var members = new List<Conveyer>(16);

        Conveyer cur  = start;
        Conveyer prev = null;
        Vector2Int? lastDir = null;

        while (cur != null && !visited.Contains(cur))
        {
            visited.Add(cur);
            Vector3 curPos = cur.GetWorldPosition();

            // 1) 先把当前格心加入
            AddPointUnique(pts, curPos);
            members.Add(cur);

            // 2) 如与上一步方向不同，**再加入一次同一点**，确保 LineRenderer 产生圆角
            if (prev != null)
            {
                Vector2Int dir = cur.cell - prev.cell;
                if (!lastDir.HasValue || dir != lastDir.Value)
                    AddPointUnique(pts, curPos); // 双写角点
                lastDir = dir;
            }

            // 3) 选下一个（不回头）
            Conveyer next = null;
            if (nbr.TryGetValue(cur, out var list))
            {
                foreach (var n in list)
                {
                    if (n == prev) continue;
                    next = n; break;
                }
            }

            prev = cur;
            cur  = next;
        }

        // 4) 末尾保护：如果循环因为“到头”提前退出，最后一个点没重复，就再补一次
        if (pts.Count >= 1)
            AddPointUnique(pts, pts[pts.Count - 1]);

        CreatePathLine(pts, start, members);
    }

    private static void BuildLoopLine(
        Conveyer start,
        Dictionary<Conveyer, List<Conveyer>> nbr,
        HashSet<Conveyer> visited)
    {
        if (start == null || !nbr.ContainsKey(start)) return;

        var pts     = new List<Vector3>(16);
        var members = new List<Conveyer>(16);

        Conveyer cur  = start;
        Conveyer prev = null;
        Vector2Int? lastDir = null;

        while (cur != null && !visited.Contains(cur))
        {
            visited.Add(cur);
            Vector3 curPos = cur.GetWorldPosition();

            // 1) 先加当前位置
            AddPointUnique(pts, curPos);
            members.Add(cur);

            // 2) 方向变化 → 再加一次“同一点”，形成可靠的圆角
            if (prev != null)
            {
                Vector2Int dir = cur.cell - prev.cell;
                if (!lastDir.HasValue || dir != lastDir.Value)
                    AddPointUnique(pts, curPos);
                lastDir = dir;
            }

            // 3) 找下一个未访问的邻居（不回头）
            Conveyer next = null;
            if (nbr.TryGetValue(cur, out var list))
            {
                foreach (var n in list)
                {
                    if (n == prev) continue;
                    if (!visited.Contains(n)) { next = n; break; }
                }

                // 闭环收尾：如果没有未访问邻居了，检查“回到起点”的方向是否与最后一步不同；
                // 不同则把【起点】再加入一次，形成圆角闭合
                if (next == null && list.Count == 2)
                {
                    Vector2Int closeDir = start.cell - cur.cell;
                    if (!lastDir.HasValue || closeDir != lastDir.Value)
                        AddPointUnique(pts, start.GetWorldPosition());
                }
            }

            prev = cur;
            cur  = next;
        }

        // 4) 闭环不必再补终点，非闭环若最后一点没重复，补一次
        if (pts.Count >= 1)
            AddPointUnique(pts, pts[pts.Count - 1]);

        CreatePathLine(pts, start, members);
    }

    private static void SimplifyColinear(List<Vector3> pts)
    {
        if (pts.Count < 3) return;

        const float lenEpsSqr = 1e-8f;   // 极短段阈值（避免 0 向量归一化）
        const float crossEps  = 1e-6f;   // 共线阈值

        var outPts = new List<Vector3>(pts.Count);
        outPts.Add(pts[0]);

        for (int i = 1; i < pts.Count - 1; i++)
        {
            var a = outPts[outPts.Count - 1];
            var b = pts[i];
            var c = pts[i + 1];

            var ab = b - a; ab.z = 0;
            var bc = c - b; bc.z = 0;

            // 任一段接近 0 长度：保留中点（常见于我们“拐角双写”的点）
            if (ab.sqrMagnitude < lenEpsSqr || bc.sqrMagnitude < lenEpsSqr)
            {
                AddPointUnique(outPts, b);
                continue;
            }

            // 真正共线才跳过中点；否则保留作为折点
            var cross = Vector3.Cross(ab.normalized, bc.normalized);
            if (cross.sqrMagnitude < crossEps) continue;

            AddPointUnique(outPts, b);
        }

        AddPointUnique(outPts, pts[pts.Count - 1]);

        pts.Clear();
        pts.AddRange(outPts);
    }

    private static void CreatePathLine(List<Vector3> pts, Conveyer sample, List<Conveyer> members)
    {
        SimplifyColinear(pts);
        if (pts.Count < 2) return;

        var go = new GameObject("[Conveyor Path]");
        go.transform.SetParent(s_lineRoot, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = pts.Count;
        lr.SetPositions(pts.ToArray());
        lr.widthMultiplier = s_width;
        lr.material = s_material;
        lr.numCornerVertices = s_cornerV;
        lr.numCapVertices = s_capV;
        lr.alignment = LineAlignment.View;
        lr.startColor = lr.endColor = s_color;

        var sr = sample.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            lr.sortingLayerID = sr.sortingLayerID;
            lr.sortingOrder = sr.sortingOrder + 1;
        }

        // 记录线与成员关系 & 初始高亮
        var pl = new PathLine { lr = lr, members = members, glow = 0f };
        int lineIdx = s_lines.Count;
        s_lines.Add(pl);
        foreach (var m in members) s_beltToLine[m] = lineIdx;
    }

    private static void ClearAllPathLines()
    {
        for (int i = 0; i < s_lines.Count; i++)
            if (s_lines[i].lr != null)
                Object.Destroy(s_lines[i].lr.gameObject);
        s_lines.Clear();
        s_beltToLine.Clear();
    }

    // 只在与上一点距离足够大时加入，避免 [A,A,B] / [A,B,B]
    private static void AddPointUnique(List<Vector3> pts, Vector3 p, float epsSqr = 1e-8f)
    {
        if (pts.Count == 0 || (pts[pts.Count - 1] - p).sqrMagnitude > epsSqr)
            pts.Add(p);
    }
    
    #endregion

    #region 通电

    /// <summary>让包含该传送带的整条线高亮，强度 [0,1]，会按 highlightFadePerSec 衰减。</summary>
    public static void MarkPathActivated(Conveyer c, float strength = 1f)
    {
        if (c == null) return;
        if (s_beltToLine.TryGetValue(c, out int idx) && idx >= 0 && idx < s_lines.Count)
        {
            var pl = s_lines[idx];
            pl.glow = Mathf.Clamp01(Mathf.Max(pl.glow, strength));
            s_lines[idx] = pl;
        }
    }

    #endregion
}