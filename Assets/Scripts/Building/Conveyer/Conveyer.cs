using System.Collections.Generic;
using UnityEngine;

public class Conveyer : Building, IItemPort, IOrientable, IBeltNode
{
    [Header("传送带设置")] public float beltSpeed = 1.0f; // 每秒移动的格子数
    public Vector2Int inDir = Vector2Int.left;
    public Vector2Int outDir = Vector2Int.right;

    [Header("物品间距设置")] public float itemSpacing = 0.3f; // 物品之间的最小间距（0-1之间）

    [Header("容量限制")] public int maxItems = 3;
    
    // 数据存储
    private readonly List<BeltItem> _items = new();
    // 属性
    public IReadOnlyList<BeltItem> Items => _items;
    
    private IItemPort _connectedOutputPort;
    private Vector2Int _connectedDirection;

    // 自动铺路相关
    private int _lastAutoTileFrame = -1;
    private static Vector2Int RotCW(Vector2Int v) => new(v.y, -v.x);
    private static Vector2Int RotCCW(Vector2Int v) => new(-v.y, v.x);
    
    public Vector2Int Direction => outDir;

    public bool CanProvide => _items.Count > 0 && _items[0].pos >= 0.95f;
    public bool CanReceive => _items.Count < maxItems;
    
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
    private static readonly List<LineRenderer> s_lines = new();
    private static readonly HashSet<Conveyer> s_all = new();
    private static bool s_dirty = false;
    private static float s_lastBuildTime = -999f;
    private const float REBUILD_DEBOUNCE = 0.02f; // 20ms 合并抖动，避免频繁拆装抖动
    
    public Vector3 GetWorldPosition()
    {
        return grid != null ? grid.CellToWorld(cell) : transform.position;
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
        // 注册全局集合
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
        if (!s_dirty) return;
        if (Time.unscaledTime - s_lastBuildTime < REBUILD_DEBOUNCE) return;
        s_lastBuildTime = Time.unscaledTime;
        RebuildAllPathLines();
    }
    
    private void EnsurePathSystemInitialized()
    {
        if (s_pathInit) return;
        s_pathInit = true;

        // 把“本实例”的 inspector 配置同步到全局（以后以静态为准）
        s_enabled = pathLineEnabled;
        s_width = pathLineWidth;
        s_color = pathLineColor;
        s_material = pathLineMaterial != null ? pathLineMaterial : new Material(Shader.Find("Sprites/Default"))
        {
            name = "[Shared] MyceliumPathLine",
            hideFlags = HideFlags.DontSave
        };

        var root = GameObject.Find("[Conveyor Path Lines]");
        if (root == null)
        {
            var go = new GameObject("[Conveyor Path Lines]");
            s_lineRoot = go.transform;
        }
        else s_lineRoot = root.transform;

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

    // —— 直线渲染：把所有 Conveyer 按 4 向邻接合并成线段 —— //
    private static void RebuildAllPathLines()
    {
        s_dirty = false;
        ClearAllPathLines();
        if (!s_enabled || s_all.Count == 0) return;

        // 1) 构建邻接（只考虑 4 向直邻，并且必须都处于已放置状态）
        var neighbors = new Dictionary<Conveyer, List<Conveyer>>(s_all.Count);
        foreach (var c in s_all)
        {
            if (c == null || c.grid == null) continue;
            var list = GetNeighbors4(c);
            if (list.Count > 0) neighbors[c] = list;
        }

        // 2) 找端点（度==1）优先生成路径；剩余（度==2的环）作为闭环处理
        var visited = new HashSet<Conveyer>();
        foreach (var kv in neighbors)
        {
            var node = kv.Key;
            if (visited.Contains(node)) continue;
            int deg = kv.Value.Count;
            if (deg == 1)
            {
                BuildLineFromEndpoint(node, neighbors, visited);
            }
        }

        // 闭环处理：未访问的、有邻居的，随便挑一点绕一圈
        foreach (var kv in neighbors)
        {
            var node = kv.Key;
            if (visited.Contains(node)) continue;
            BuildLoopLine(node, neighbors, visited);
        }
    }

// 收集 4 向直邻的 Conveyer（只连接同类基类/子类的传送带）
    private static List<Conveyer> GetNeighbors4(Conveyer c)
    {
        var result = new List<Conveyer>(4);
        var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var d in dirs)
        {
            var nb = c.grid?.GetBuildingAt(c.cell + d) as Conveyer;
            if (nb != null) result.Add(nb);
        }

        return result;
    }

// 从端点出发，沿链到另一个端/或中止，生成一根折线
    private static void BuildLineFromEndpoint(Conveyer start, Dictionary<Conveyer, List<Conveyer>> nbr,
        HashSet<Conveyer> visited)
    {
        var points = new List<Vector3>(16);
        Conveyer prev = null;
        var cur = start;

        while (cur != null && !visited.Contains(cur))
        {
            visited.Add(cur);
            points.Add(cur.GetWorldPosition());

            // 走向下一个未访问的邻居（不回头）
            Conveyer next = null;
            if (nbr.TryGetValue(cur, out var list))
            {
                foreach (var n in list)
                {
                    if (n == prev) continue;
                    next = n;
                    break;
                }
            }

            prev = cur;
            cur = next;
        }

        if (points.Count >= 2)
            CreatePathLine(points, start);
    }

// 闭环：从任一点出发，沿着未访问的链一圈
    private static void BuildLoopLine(Conveyer start, Dictionary<Conveyer, List<Conveyer>> nbr,
        HashSet<Conveyer> visited)
    {
        var points = new List<Vector3>(16);
        Conveyer prev = null;
        var cur = start;

        while (cur != null && !visited.Contains(cur))
        {
            visited.Add(cur);
            points.Add(cur.GetWorldPosition());

            // 选择任一未访问的邻居继续
            Conveyer next = null;
            if (nbr.TryGetValue(cur, out var list))
            {
                foreach (var n in list)
                {
                    if (n == prev)
                    {
                        continue;
                    }

                    if (!visited.Contains(n))
                    {
                        next = n;
                        break;
                    }
                }

                // 如果都访问过，闭环：把首点再加一遍，方便 LineRenderer 闭合视觉（非 loop）
                if (next == null && list.Count == 2 && points.Count >= 2)
                {
                    points.Add(points[0]);
                }
            }

            prev = cur;
            cur = next;
        }

        if (points.Count >= 2)
            CreatePathLine(points, start);
    }

// 创建一根 LineRenderer，放到共享容器下
    private static void CreatePathLine(List<Vector3> points, Conveyer sample)
    {
        var go = new GameObject("[Conveyor Path]");
        go.transform.SetParent(s_lineRoot, false);
        var lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        lr.widthMultiplier = s_width;
        lr.material = s_material;
        lr.startColor = lr.endColor = s_color;

        // 与带同层绘制，并提高一个 order，避免被遮住
        var sr = sample.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            lr.sortingLayerID = sr.sortingLayerID;
            lr.sortingOrder = sr.sortingOrder + 1;
        }

        s_lines.Add(lr);
    }

    private static void ClearAllPathLines()
    {
        for (int i = 0; i < s_lines.Count; i++)
            if (s_lines[i] != null)
                Object.Destroy(s_lines[i].gameObject);
        s_lines.Clear();
    }

    #endregion
}