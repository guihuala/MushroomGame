using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class ProductionHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Panel Source")]
    [SerializeField] private ProductionTooltipPanel panel;       // 优先：直接引用
    [SerializeField] private ProductionTooltipPanel panelPrefab; // 备选：预制体实例化
    [SerializeField] private Canvas panelCanvasOverride;         // 可选：指定 Canvas

    [Header("Behavior")]
    [SerializeField] private float hoverDelay = 0.12f;

    private IProductionInfoProvider _provider;
    private bool _inside;
    private float _t;

    private static ProductionTooltipPanel sharedPanel; // 全场景复用

    private void Awake()
    {
        // 既查自己也查父物体，避免脚本挂在子节点时拿不到
        _provider = GetComponent<IProductionInfoProvider>();
        if (_provider == null) _provider = GetComponentInParent<IProductionInfoProvider>();

        EnsureCollider();
        EnsurePanelAvailable(); // 先尝试一遍
    }

    private void OnEnable()
    {
        if (panel == null && sharedPanel == null) EnsurePanelAvailable();
        if (_provider == null)
        {
            _provider = GetComponent<IProductionInfoProvider>();
            if (_provider == null) _provider = GetComponentInParent<IProductionInfoProvider>();
        }
    }

    private void Update()
    {
        var p = GetPanel();
        if (!_inside || p == null || _provider == null) return;

        _t += Time.unscaledDeltaTime;
        if (_t >= hoverDelay)
        {
            p.SetContext(_provider);
            Vector2 mouse = Input.mousePosition;

            if (!p.gameObject.activeSelf)
                p.ShowAtScreenPosition(mouse);
            else
                p.FollowMouse(mouse);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _inside = true;
        _t = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _inside = false;
        _t = 0f;

        var p = GetPanel();
        if (p != null && p.gameObject.activeSelf)
            p.ClosePanel();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        var p = GetPanel();
        if (_inside && p != null && p.gameObject.activeSelf)
            p.FollowMouse(eventData.position);
    }

    private void EnsureCollider()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var sr = GetComponent<SpriteRenderer>();
            var bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size : new Vector2(1.2f, 1.2f);
        }
    }

    private ProductionTooltipPanel GetPanel() => panel != null ? panel : sharedPanel;

    private void EnsurePanelAvailable()
    {
        // 1) 手动引用
        if (panel != null) return;

        // 2) 场景里找现成的（包含未激活）
        if (sharedPanel == null)
            sharedPanel = FindObjectOfType<ProductionTooltipPanel>(true);
        if (sharedPanel != null) return;

        // 3) 预制体
        var prefab = panelPrefab;
        if (prefab == null) return;

        Canvas targetCanvas = panelCanvasOverride != null ? panelCanvasOverride : FindBestCanvas();
        if (targetCanvas == null)
        {
            var go = new GameObject("HoverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas = c;
        }

        sharedPanel = Instantiate(prefab, targetCanvas.transform);
        sharedPanel.gameObject.name = "[Runtime] ProductionTooltipPanel";
        sharedPanel.gameObject.SetActive(false);
    }

    private Canvas FindBestCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas overlay = null;
        Canvas screenCam = null;

        foreach (var c in canvases)
        {
            if (!c.gameObject.activeInHierarchy) continue;
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { overlay = c; break; }
            if (c.renderMode == RenderMode.ScreenSpaceCamera && screenCam == null) screenCam = c;
        }
        return overlay != null ? overlay : (screenCam != null ? screenCam : (canvases.Length > 0 ? canvases[0] : null));
    }
}
