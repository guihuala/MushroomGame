using System.Collections.Generic;
using UnityEngine;

public partial class Conveyer
{
    [Header("Path Line (Mycelium) - Global")]
    [SerializeField] private bool  pathLineEnabled = true;
    [SerializeField] private float pathLineWidth   = 0.04f;
    [SerializeField] private Color pathLineColor   = new(0.90f, 1.00f, 0.90f, 0.90f);
    [SerializeField] private Material pathLineMaterial;

    [Header("Rounded Corners")]
    [SerializeField] private int cornerVertices = 6;
    [SerializeField] private int capVertices    = 2;

    [Header("Powered Highlight")]
    [SerializeField] private Color highlightColor      = Color.white;
    [SerializeField] private float highlightFadePerSec = 2.5f;

    // —— 全局渲染共享 —— //
    private static bool     s_pathInit = false;
    private static bool     s_enabled  = true;
    private static float    s_width    = 0.04f;
    private static Color    s_color    = Color.white;
    private static Color    s_highlightColor = Color.white;
    private static float    s_glowFade = 2.5f;
    private static int      s_cornerV  = 6, s_capV = 2;
    private static Material s_material;
    private static Transform s_lineRoot;

    private struct PathLine { public LineRenderer lr; public List<Conveyer> members; public float glow; }
    private static readonly List<PathLine> s_lines = new();
    private static readonly Dictionary<Conveyer, int> s_beltToLine = new();
    private static readonly HashSet<Conveyer> s_all = new();
    private static readonly Dictionary<Conveyer, Vector4> s_dirCache = new();

    private static bool  s_dirty = false;
    private static float s_lastBuildTime = -999f;
    private const  float REBUILD_DEBOUNCE = 0.02f;

    #region 注册/更新/高亮
    protected virtual void Awake()
    {
        s_all.Add(this);
        EnsurePathSystemInitialized();
        CacheDir(this);
        MarkPathDirty();
    }

    protected virtual void OnDestroy()
    {
        s_all.Remove(this);
        s_dirCache.Remove(this);
        MarkPathDirty();
    }

    protected virtual void LateUpdate()
    {
        bool changed = false;
        foreach (var c in s_all)
            if (UpdateDirCacheIfChanged(c)) changed = true;
        if (changed) s_dirty = true;

        if (s_dirty && Time.unscaledTime - s_lastBuildTime >= REBUILD_DEBOUNCE)
        {
            s_lastBuildTime = Time.unscaledTime;
            RebuildAllPathLines();
        }

        if (s_lines.Count > 0 && s_glowFade > 0f)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < s_lines.Count; i++)
            {
                var pl = s_lines[i];
                if (pl.lr == null) continue;
                if (pl.glow > 0f) { pl.glow = Mathf.Max(0f, pl.glow - s_glowFade * dt); s_lines[i] = pl; }
                Color c = Color.Lerp(s_color, s_highlightColor, pl.glow);
                pl.lr.startColor = pl.lr.endColor = c;
            }
        }
    }

    private void EnsurePathSystemInitialized()
    {
        if (s_pathInit) return;
        s_pathInit = true;

        s_enabled  = pathLineEnabled;
        s_width    = pathLineWidth;
        s_color    = pathLineColor;
        s_highlightColor = highlightColor;
        s_glowFade = Mathf.Max(0.01f, highlightFadePerSec);
        s_cornerV  = Mathf.Max(0, cornerVertices);
        s_capV     = Mathf.Max(0, capVertices);

        s_material = pathLineMaterial != null
            ? pathLineMaterial
            : new Material(Shader.Find("Sprites/Default")) { name = "[Shared] MyceliumPathLine", hideFlags = HideFlags.DontSave };

        var root = GameObject.Find("[Conveyor Path Lines]") ?? new GameObject("[Conveyor Path Lines]");
        s_lineRoot = root.transform;

        MarkPathDirty();
    }

    private static void MarkPathDirty() => s_dirty = true;
    private static void CacheDir(Conveyer c) => s_dirCache[c] = new Vector4(c.inDir.x, c.inDir.y, c.outDir.x, c.outDir.y);
    private static bool UpdateDirCacheIfChanged(Conveyer c)
    {
        var now = new Vector4(c.inDir.x, c.inDir.y, c.outDir.x, c.outDir.y);
        if (!s_dirCache.TryGetValue(c, out var old) || old != now) { s_dirCache[c] = now; return true; }
        return false;
    }

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

    #region 构图（只连 Out→In 的物流连通）
    private static bool IsFlowConnected(Conveyer a, Conveyer b)
    {
        Vector2Int d = b.cell - a.cell;
        if (Mathf.Abs(d.x) + Mathf.Abs(d.y) != 1) return false;
        bool forward  = (a.outDir == d) && (b.inDir == -d);
        bool backward = (a.inDir  == d) && (b.outDir == -d);
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
    #endregion

    #region 生成直线（保留拐角 + 圆角 + 简化）
    private static void RebuildAllPathLines()
    {
        s_dirty = false;
        ClearAllPathLines();
        s_beltToLine.Clear();
        if (!s_enabled || s_all.Count == 0) return;

        var neighbors = new Dictionary<Conveyer, List<Conveyer>>(s_all.Count);
        foreach (var c in s_all)
        {
            if (c == null || c.grid == null) continue;
            var list = GetFlowNeighbors(c);
            if (list.Count > 0) neighbors[c] = list;
        }

        var visited = new HashSet<Conveyer>();
        foreach (var kv in neighbors)
            if (!visited.Contains(kv.Key) && kv.Value.Count == 1)
                BuildLineFromEndpoint(kv.Key, neighbors, visited);

        foreach (var kv in neighbors)
            if (!visited.Contains(kv.Key))
                BuildLoopLine(kv.Key, neighbors, visited);
    }

    private static void BuildLineFromEndpoint(Conveyer start,
        Dictionary<Conveyer, List<Conveyer>> nbr, HashSet<Conveyer> visited)
    {
        if (start == null || !nbr.ContainsKey(start)) return;

        var pts = new List<Vector3>(16);
        var members = new List<Conveyer>(16);
        Conveyer cur = start, prev = null;
        Vector2Int? lastDir = null;

        while (cur != null && !visited.Contains(cur))
        {
            visited.Add(cur);
            Vector3 curPos = cur.GetWorldPosition();

            AddPointUnique(pts, curPos);      // 入点
            members.Add(cur);

            if (prev != null)
            {
                Vector2Int dir = cur.cell - prev.cell;
                if (!lastDir.HasValue || dir != lastDir.Value)
                    AddPointUnique(pts, curPos); // 拐角双写
                lastDir = dir;
            }

            Conveyer next = null;
            if (nbr.TryGetValue(cur, out var list))
                foreach (var n in list) { if (n != prev) { next = n; break; } }

            prev = cur; cur = next;
        }

        if (pts.Count >= 1) AddPointUnique(pts, pts[pts.Count - 1]);
        CreatePathLine(pts, start, members);
    }

    private static void BuildLoopLine(Conveyer start,
        Dictionary<Conveyer, List<Conveyer>> nbr, HashSet<Conveyer> visited)
    {
        if (start == null || !nbr.ContainsKey(start)) return;

        var pts = new List<Vector3>(16);
        var members = new List<Conveyer>(16);
        Conveyer cur = start, prev = null;
        Vector2Int? lastDir = null;

        while (cur != null && !visited.Contains(cur))
        {
            visited.Add(cur);
            Vector3 curPos = cur.GetWorldPosition();

            AddPointUnique(pts, curPos);
            members.Add(cur);

            if (prev != null)
            {
                Vector2Int dir = cur.cell - prev.cell;
                if (!lastDir.HasValue || dir != lastDir.Value)
                    AddPointUnique(pts, curPos);
                lastDir = dir;
            }

            Conveyer next = null;
            if (nbr.TryGetValue(cur, out var list))
            {
                foreach (var n in list) { if (n != prev && !visited.Contains(n)) { next = n; break; } }
                if (next == null && list.Count == 2)
                {
                    Vector2Int closeDir = start.cell - cur.cell;
                    if (!lastDir.HasValue || closeDir != lastDir.Value)
                        AddPointUnique(pts, start.GetWorldPosition());
                }
            }

            prev = cur; cur = next;
        }

        if (pts.Count >= 1) AddPointUnique(pts, pts[pts.Count - 1]);
        CreatePathLine(pts, start, members);
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
        lr.widthMultiplier   = s_width;
        lr.material          = s_material;
        lr.numCornerVertices = s_cornerV;
        lr.numCapVertices    = s_capV;
        lr.alignment         = LineAlignment.View;
        lr.startColor = lr.endColor = s_color;

        var sr = sample.GetComponent<SpriteRenderer>();
        if (sr != null) { lr.sortingLayerID = sr.sortingLayerID; lr.sortingOrder = sr.sortingOrder + 1; }

        var pl = new PathLine { lr = lr, members = members, glow = 0f };
        int idx = s_lines.Count;
        s_lines.Add(pl);
        foreach (var m in members) s_beltToLine[m] = idx;
    }

    private static void ClearAllPathLines()
    {
        for (int i = 0; i < s_lines.Count; i++)
            if (s_lines[i].lr != null) Object.Destroy(s_lines[i].lr.gameObject);
        s_lines.Clear();
        s_beltToLine.Clear();
    }

    private static void SimplifyColinear(List<Vector3> pts)
    {
        if (pts.Count < 3) return;
        const float lenEpsSqr = 1e-8f;
        const float crossEps  = 1e-6f;

        var outPts = new List<Vector3>(pts.Count);
        outPts.Add(pts[0]);

        for (int i = 1; i < pts.Count - 1; i++)
        {
            var a = outPts[outPts.Count - 1];
            var b = pts[i];
            var c = pts[i + 1];
            var ab = b - a; ab.z = 0;
            var bc = c - b; bc.z = 0;

            if (ab.sqrMagnitude < lenEpsSqr || bc.sqrMagnitude < lenEpsSqr)
            { AddPointUnique(outPts, b); continue; }

            var cross = Vector3.Cross(ab.normalized, bc.normalized);
            if (cross.sqrMagnitude < crossEps) continue;

            AddPointUnique(outPts, b);
        }
        AddPointUnique(outPts, pts[^1]);

        pts.Clear(); pts.AddRange(outPts);
    }

    private static void AddPointUnique(List<Vector3> pts, Vector3 p, float epsSqr = 1e-8f)
    {
        if (pts.Count == 0 || (pts[^1] - p).sqrMagnitude > epsSqr) pts.Add(p);
    }
    #endregion
}
