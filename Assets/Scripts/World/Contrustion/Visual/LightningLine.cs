using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class LightningLine : MonoBehaviour
{
    [Header("Shape")]
    [Range(4, 64)] public int segments = 16;         // 主干段数
    public float jaggedness = 0.35f;                 // 折线幅度系数（相对于 AB 距离）
    public float noiseScale = 1f;
    public float taper = 0.1f;                       // 尾部收窄(0~1)

    [Header("Animation")]
    public float jitterSpeed = 3f;                   // 抖动速度（越大越乱）
    public float refreshInterval = 0.02f;            // 重建折线的间隔（秒）
    public float flickerMin = 0.7f;                  // 亮度闪烁下限
    public float flickerMax = 1.0f;                  // 亮度闪烁上限
    public float width = 0.06f;                      // 基础宽度

    [Header("Branch (optional)")]
    public bool enableBranches = true;
    [Range(0f, 0.5f)] public float branchChance = 0.05f;
    public int branchSegments = 3;
    public float branchLengthFactor = 0.25f;         // 分叉长度占主干 AB 的比例
    public float branchWidthFactor = 0.6f;           // 分叉相对主干宽度

    private LineRenderer lr;
    private Vector3 a, b;                            // 端点（世界坐标）
    private float timer;
    private System.Random rng;
    private int seed;
    public Transform ta, tb;
    public float zPlane = 0f;


    // 用于保存生成的分叉
    private readonly List<LineRenderer> branchPool = new();

    public void SetEndpoints(Vector3 A, Vector3 B)
    {
        a = A; b = B;
        seed = (A.GetHashCode() * 486187739) ^ B.GetHashCode();
        if (rng == null) rng = new System.Random(seed);
        RebuildNow(force:true);
    }

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        // 默认配置
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;

        // 宽度随长度稍微收尾
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, Mathf.Clamp01(1f - taper));
        lr.widthCurve = curve;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (lr == null) return;
        
        if (ta) { var p = ta.position; p.z = zPlane; a = p; }
        if (tb) { var p = tb.position; p.z = zPlane; b = p; }

        timer += Time.deltaTime;
        
        var cam = Camera.main;
        if (cam != null)
        {
            var mid = (a + b) * 0.5f;
            var vp = cam.WorldToViewportPoint(mid);
            bool visible = vp.z > 0 && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
            lr.enabled = visible;
            if (!visible) return;
        }

        // 闪烁
        float flicker = Mathf.Lerp(flickerMin, flickerMax, Mathf.PerlinNoise(Time.time * jitterSpeed, seed * 0.001f));
        var c = lr.startColor;
        c.a = flicker;
        lr.startColor = c;
        lr.endColor   = c;

        // 宽度（可微动态）
        lr.widthMultiplier = width * Mathf.Lerp(0.95f, 1.05f, Mathf.PerlinNoise(0.5f, Time.time * 2f));

        // 定时重建折线（比每帧重建省一点）
        if (timer >= refreshInterval)
        {
            RebuildNow();
            timer = 0f;
        }
        
        float dist = (a - cam.transform.position).magnitude;
        segments = dist > 30f ? 2 : 18;
        float targetInterval = dist > 30f ? 0.06f : 0.02f;

        timer += Time.deltaTime;
        if (timer >= targetInterval) { RebuildNow(); timer = 0f; }
    }

    private void RebuildNow(bool force = false)
    {
        if (!force && (a == b)) return;

        int count = Mathf.Max(2, segments);
        lr.positionCount = count;

        Vector3 ab = b - a;
        float len = ab.magnitude;
        Vector3 dir = (len > 0.0001f) ? ab / len : Vector3.right;
        // 找一个与 dir 垂直的 2D 法线（Z 朝外）
        Vector3 n = new Vector3(-dir.y, dir.x, 0f);

        float t = Time.time * jitterSpeed;
        float amp = jaggedness * len; // 幅度相对长度

        for (int i = 0; i < count; i++)
        {
            float u = (float)i / (count - 1);
            // 主干上某点
            Vector3 p = Vector3.Lerp(a, b, u);

            // 噪声偏移
            float phase = u * noiseScale + seed * 0.013f;
            float off = (Mathf.PerlinNoise(phase, t) - 0.5f) * 2f; // [-1,1]
            float falloff = Mathf.Sin(Mathf.PI * u);               // 中段更乱，端点更稳
            p += n * (off * amp * falloff);

            lr.SetPosition(i, p);
        }

        // 分叉
        BuildBranches(dir, n, len);
    }

    private void BuildBranches(Vector3 dir, Vector3 n, float len)
    {
        // 回收所有分叉（不销毁，禁用即可）
        for (int i = 0; i < branchPool.Count; i++)
            if (branchPool[i]) branchPool[i].enabled = false;

        if (!enableBranches || branchChance <= 0f) return;

        int active = 0;
        // 每个点有概率生成一个短分叉
        int mainCount = lr.positionCount;
        for (int i = 1; i < mainCount - 1; i++)
        {
            if (Random01(rng) > branchChance) continue;

            // 取主干某点作为起点
            Vector3 start = lr.GetPosition(i);

            // 分叉朝向：以主干方向为基准，随机 ±45°，并随机朝法线两侧
            float side = (Random01(rng) < 0.5f) ? -1f : 1f;
            float angle = Mathf.Lerp(-45f, 45f, Random01(rng)) * Mathf.Deg2Rad;
            Vector3 dir2 = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward) * dir;
            dir2 = (dir2 + side * 0.3f * n).normalized;

            float blen = len * branchLengthFactor * Mathf.Lerp(0.6f, 1.2f, Random01(rng));
            int bseg = Mathf.Clamp(branchSegments, 3, 32);

            // 拿一条可用的分叉线
            LineRenderer blr = null;
            if (active < branchPool.Count && branchPool[active] != null)
            {
                blr = branchPool[active];
                blr.enabled = true;
            }
            else
            {
                var go = new GameObject("LightningBranch");
                go.transform.SetParent(this.transform, worldPositionStays: false);
                blr = go.AddComponent<LineRenderer>();
                // 复制主干的材质/样式
                CopyLineStyle(lr, blr);
                branchPool.Add(blr);
            }
            active++;

            blr.positionCount = bseg;
            blr.widthMultiplier = lr.widthMultiplier * branchWidthFactor;

            // 生成分叉折线
            for (int k = 0; k < bseg; k++)
            {
                float u = (float)k / (bseg - 1);
                Vector3 p = start + dir2 * (u * blen);

                float phase = (i * 0.37f + k * 0.21f + seed * 0.019f);
                float off = (Mathf.PerlinNoise(phase, Time.time * jitterSpeed) - 0.5f) * 2f;
                float falloff = Mathf.Pow(1f - u, 1.5f); // 尾部更细更收敛
                p += Vector3.Cross(Vector3.forward, dir2).normalized * off * blen * 0.12f * falloff;

                blr.SetPosition(k, p);
            }
        }
    }

    private static void CopyLineStyle(LineRenderer src, LineRenderer dst)
    {
        dst.useWorldSpace = src.useWorldSpace;
        dst.textureMode = src.textureMode;
        dst.alignment = src.alignment;
        dst.numCornerVertices = src.numCornerVertices;
        dst.numCapVertices = src.numCapVertices;
        dst.material = src.material;
        dst.startColor = src.startColor;
        dst.endColor   = src.endColor;
        dst.widthCurve = src.widthCurve;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder   = src.sortingOrder + 1; // 分叉叠在主干上
    }

    private static float Random01(System.Random r) => (float)r.NextDouble();
    
    public Vector3 GetStartPoint() => lr.positionCount > 0 ? lr.GetPosition(0) : Vector3.zero;
    public Vector3 GetEndPoint() => lr.positionCount > 1 ? lr.GetPosition(1) : Vector3.zero;
    
    public void SetEndpoints(Transform A, Transform B, float z)
    {
        ta = A; tb = B;
        zPlane = z;
        // 初次立即同步一次世界端点，触发一次构建
        Vector3 Aw = (ta ? ta.position : Vector3.zero); Aw.z = zPlane;
        Vector3 Bw = (tb ? tb.position : Vector3.zero); Bw.z = zPlane;
        SetEndpoints(Aw, Bw);
    }
    
    public bool HasEndpoint(Transform t)
    {
        return t && (t == ta || t == tb);
    }
}
