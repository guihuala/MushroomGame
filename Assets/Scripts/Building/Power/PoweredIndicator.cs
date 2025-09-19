using UnityEngine;

/// <summary>
/// 建筑通电提示
/// </summary>
[RequireComponent(typeof(Building))]
public class PoweredIndicator : MonoBehaviour
{
    [Header("Icon")]
    public Sprite iconSprite;                  // 要显示的图标
    public Color  iconColor = Color.white;
    public float  iconScale = .5f;              // 缩放
    public int    sortingOrder = 5000;

    [Header("Position")]
    public float yOffset = 1.0f;
    public float xOffset = 0.0f;             
    public float zOffset = 0.02f;             // 为了确保在前景/后景正确堆叠

    [Header("Optional Appear/Disappear")]
    public bool  useAppearAnim = true;         // 是否使用小弹出/消失
    public float appearTime = 0.12f;
    public float disappearTime = 0.10f;
    [Range(1.0f, 1.2f)] public float appearOvershoot = 1.05f;

    private Building _b;
    private Transform _iconTf;
    private SpriteRenderer _sr;
    private bool _lastPowered;

    private void Awake()
    {
        _b = GetComponent<Building>();
        EnsureIcon();
        SetIconVisible(false, instant: true);
    }

    private void OnEnable()
    {
        if (PowerManager.Instance != null)
            PowerManager.Instance.PowerCoverageChanged += RefreshNow;

        RefreshNow();                          // 立刻试一次
        StartCoroutine(WaitGridThenRefresh()); // 下一帧再兜底（避免放置早于赋 grid/cell）
    }

    private void OnDisable()
    {
        if (PowerManager.Instance != null)
            PowerManager.Instance.PowerCoverageChanged -= RefreshNow;
    }

    private System.Collections.IEnumerator WaitGridThenRefresh()
    {
        yield return null;
        RefreshNow();
    }

    private void EnsureIcon()
    {
        if (_iconTf != null) return;

        var go = new GameObject("PoweredIcon");
        _iconTf = go.transform;
        _iconTf.SetParent(transform, false);

        _sr = go.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = sortingOrder;
        _sr.sprite = iconSprite;
        _sr.color  = iconColor;

        ApplyIconTransform();  // 应用一次位置/缩放
    }

    private void ApplyIconTransform()
    {
        if (_iconTf == null) return;
        _iconTf.localPosition = new Vector3(xOffset, yOffset, zOffset);
        _iconTf.localScale    = Vector3.one * Mathf.Max(0.01f, iconScale);
    }

    /// <summary>外部（或参数被Inspector修改后）可手动调用，重应用位置/缩放/颜色。</summary>
    public void ReapplyStyle()
    {
        if (_sr != null)
        {
            _sr.sprite = iconSprite;
            _sr.color  = iconColor;
            _sr.sortingOrder = sortingOrder;
        }
        ApplyIconTransform();
    }

    private void RefreshNow()
    {
        if (_b == null || PowerManager.Instance == null || _b.grid == null) return;

        bool powered = PowerManager.Instance.IsCellPowered(_b.cell, _b.grid);
        if (powered != _lastPowered)
        {
            SetIconVisible(powered, instant: !useAppearAnim);
            _lastPowered = powered;
        }

        // 每次刷新都重应用一次位置/缩放（如在运行时被改动）
        ApplyIconTransform();
    }

    private void SetIconVisible(bool v, bool instant)
    {
        if (_sr == null || _iconTf == null) return;

        if (v)
        {
            _sr.enabled = true;
            if (instant || !useAppearAnim)
            {
                _iconTf.localScale = Vector3.one * Mathf.Max(0.01f, iconScale);
                ApplyIconAlpha(iconColor.a);
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(Co_Appear());
            }
        }
        else
        {
            if (instant || !useAppearAnim)
            {
                _sr.enabled = false;
                _iconTf.localScale = Vector3.one * Mathf.Max(0.01f, iconScale);
                ApplyIconAlpha(iconColor.a);
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(Co_Disappear());
            }
        }
    }

    private System.Collections.IEnumerator Co_Appear()
    {
        float t = 0f;
        _sr.enabled = true;

        float targetA = iconColor.a;
        Vector3 targetS = Vector3.one * Mathf.Max(0.01f, iconScale);
        Vector3 overshootS = targetS * appearOvershoot;

        // 0 -> overshoot
        while (t < appearTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / appearTime);
            _iconTf.localScale = Vector3.Lerp(Vector3.zero, overshootS, Mathf.SmoothStep(0f, 1f, u));
            ApplyIconAlpha(Mathf.Lerp(0f, targetA, u));
            yield return null;
        }
        // overshoot -> target
        float t2 = 0f;
        while (t2 < 0.06f)
        {
            t2 += Time.deltaTime;
            float u = Mathf.Clamp01(t2 / 0.06f);
            _iconTf.localScale = Vector3.Lerp(overshootS, targetS, u);
            yield return null;
        }
        _iconTf.localScale = targetS;
        ApplyIconAlpha(targetA);
    }

    private System.Collections.IEnumerator Co_Disappear()
    {
        float t = 0f;
        float startA = _sr.color.a;
        Vector3 startS = _iconTf.localScale;

        while (t < disappearTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / disappearTime);
            _iconTf.localScale = Vector3.Lerp(startS, Vector3.zero, u);
            ApplyIconAlpha(Mathf.Lerp(startA, 0f, u));
            yield return null;
        }
        _sr.enabled = false;
        _iconTf.localScale = Vector3.one * Mathf.Max(0.01f, iconScale);
        ApplyIconAlpha(iconColor.a);
    }

    private void ApplyIconAlpha(float a)
    {
        if (_sr == null) return;
        var c = _sr.color;
        c.a = a;
        _sr.color = c;
    }

#if UNITY_EDITOR
    // 在编辑器里改 Inspector 值时，实时预览位置/颜色/图标
    private void OnValidate()
    {
        if (_sr != null)
        {
            _sr.sprite = iconSprite;
            _sr.color  = iconColor;
            _sr.sortingOrder = sortingOrder;
        }
        if (_iconTf != null) ApplyIconTransform();
    }
#endif
}
