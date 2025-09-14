using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 科技树面板
/// </summary>
public class TechTreePanel : BasePanel, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    [Header("科技树设置")]
    [SerializeField] private RectTransform techTreeContainer; // 科技树容器（内容）
    [SerializeField] private GameObject techNodePrefab;       // 科技节点预制体
    [SerializeField] private GameObject connectionLinePrefab; // 连接线预制体
    [SerializeField] private float nodeSpacing = 200f;        // 节点间距
    [SerializeField] private float levelSpacing = 150f;       // 层级间距

    [Header("缩放设置")]
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;
    [SerializeField] private float zoomSpeed = 0.1f;          // 点击按钮缩放步长
    [SerializeField] private float wheelZoomSpeed = 0.12f;    // 滚轮缩放强度
    [SerializeField] private float zoomTweenTime = 0.12f;     // DOTween 缩放时长

    [Header("拖拽/惯性/边界")]
    [SerializeField] private bool enableInertia = true;
    [SerializeField] private float inertiaDamping = 10f;      // 越大越快停
    [SerializeField] private bool enableRubberBand = true;
    [SerializeField] private float rubberStrength = 0.15f;    // 越大越“紧”
    [SerializeField] private float boundsPadding = 120f;      // 内容边界以外允许的橡皮筋范围

    [Header("UI组件")]
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup zoomControls;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button resetZoomButton;
    
    [Header("连线层级")]
    [SerializeField] private Transform connectionLineRoot;

    private Dictionary<BuildingData, TechNodeUI> nodeUIs = new Dictionary<BuildingData, TechNodeUI>();
    private List<GameObject> connectionLines = new List<GameObject>();

    // === 交互状态 ===
    private RectTransform _viewport;             // 容器父级视口（一般是 ScrollRect.viewport 或 techTreeContainer.parent）
    private Vector2 _dragStartPosView;           // 开始拖拽时指针在视口局部坐标
    private Vector2 _contentStartPos;            // 开始拖拽时内容 anchoredPosition
    private Vector2 _velocity;                   // 惯性速度（view 空间）
    private bool _isDragging;

    // Tween 缓存
    private Tweener _zoomTween;
    private Tweener _panTween;

    protected override void Awake()
    {
        base.Awake();

        closeButton.onClick.AddListener(OnCloseClick);
        zoomInButton.onClick.AddListener(ZoomInButton);
        zoomOutButton.onClick.AddListener(ZoomOutButton);
        resetZoomButton.onClick.AddListener(ResetZoomAnimated);

        _viewport = techTreeContainer.parent as RectTransform;

        // 自动创建一个 Connections 容器并放在最底层，这样连线永远在节点下方
        if (connectionLineRoot == null)
        {
            var go = new GameObject("Connections", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(techTreeContainer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            connectionLineRoot = rt;
        }
        (connectionLineRoot as RectTransform)?.SetSiblingIndex(0);
    }
    
    void OnDestroy()
    {
        closeButton.onClick.RemoveAllListeners();
        zoomInButton.onClick.RemoveAllListeners();
        zoomOutButton.onClick.RemoveAllListeners();
        resetZoomButton.onClick.RemoveAllListeners();
    }

    public override void OpenPanel(string name)
    {
        base.OpenPanel(name);
        InitializeTechTree();
        
        GameManager.Instance.PauseGame();
    }

    /// <summary>初始化科技树</summary>
    private void InitializeTechTree()
    {
        ClearTechTree();

        var techTree = TechTreeManager.Instance.GetTechTreeStructure();
        var unlockedBuildings = TechTreeManager.Instance.GetUnlockedBuildings();

        // 1) 分层：优先使用 overrideLevel
        Dictionary<int, List<BuildingData>> levels = new Dictionary<int, List<BuildingData>>();
        foreach (var kvp in techTree)
        {
            var building = kvp.Key;
            int level = TechTreeManager.Instance.GetNodeLevel(building);
            if (!levels.ContainsKey(level)) levels[level] = new List<BuildingData>();
            levels[level].Add(building);
        }

        // 2) 同层稳定排序：orderInLevel -> buildingName
        foreach (var kv in levels)
        {
            kv.Value.Sort((a, b) =>
            {
                int oa = TechTreeManager.Instance.GetOrderInLevel(a);
                int ob = TechTreeManager.Instance.GetOrderInLevel(b);
                int cmp = oa.CompareTo(ob);
                if (cmp != 0) return cmp;
                string na = techTree[a].building.buildingName;
                string nb = techTree[b].building.buildingName;
                return string.Compare(na, nb, System.StringComparison.Ordinal);
            });
        }

        float totalWidth = 0f;
        float totalHeight = 0f;

        // 3) 逐层摆放
        foreach (var level in levels.OrderBy(kv => kv.Key))
        {
            int nodeCount = level.Value.Count;
            float startX = -((nodeCount - 1) * nodeSpacing) / 2f;
            float levelHeight = level.Key * levelSpacing;

            for (int i = 0; i < nodeCount; i++)
            {
                var building = level.Value[i];
                var node = techTree[building];

                var nodeObj = Object.Instantiate(techNodePrefab, techTreeContainer);
                var nodeUI = nodeObj.GetComponent<TechNodeUI>();

                float parentXPos = 0f;
                float parentYPos = 0f;

                if (node.HasParent())
                {
                    var parentNode = node.Parent();
                    var parentNodeUI = nodeUIs[parentNode.building];
                    var parentRect = parentNodeUI.GetComponent<RectTransform>();
                    parentXPos = parentRect.anchoredPosition.x;
                    parentYPos = parentRect.anchoredPosition.y;
                }

                float xPos = parentXPos + startX + i * nodeSpacing;
                float yPos = parentYPos + levelHeight;

                nodeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

                bool isUnlocked = unlockedBuildings.Contains(building);
                bool canUnlock = node.CanUnlock();
                nodeUI.Initialize(node, isUnlocked, canUnlock);
                nodeUI.OnNodeClick += OnNodeClick;

                nodeUIs[building] = nodeUI;
                
                totalWidth = Mathf.Max(totalWidth, Mathf.Abs(xPos) + nodeSpacing);
                totalHeight = Mathf.Max(totalHeight, Mathf.Abs(yPos) + levelSpacing);
            }
        }

        // 4) 连线 & 视图复位
        CreateConnectionLines(techTree);
        ResetViewImmediate();
    }

    /// <summary>创建连接线</summary>
    private void CreateConnectionLines(Dictionary<BuildingData, TechNode> techTree)
    {
        foreach (var kvp in techTree)
        {
            TechNode node = kvp.Value;
            foreach (var prereq in node.prerequisites)
            {
                if (nodeUIs.ContainsKey(node.building) && nodeUIs.ContainsKey(prereq.building))
                {
                    CreateConnectionLine(nodeUIs[prereq.building], nodeUIs[node.building]);
                }
            }
        }
    }

    private void CreateConnectionLine(TechNodeUI fromNode, TechNodeUI toNode)
    {
        Transform parent = connectionLineRoot != null ? connectionLineRoot : techTreeContainer;
        GameObject lineObj = Instantiate(connectionLinePrefab, parent);
        ConnectionLine line = lineObj.GetComponent<ConnectionLine>();
        line.Initialize(fromNode.GetComponent<RectTransform>(), toNode.GetComponent<RectTransform>());
        connectionLines.Add(lineObj);
    }
    
    private void ClearTechTree()
    {
        foreach (var node in nodeUIs.Values)
            if (node != null) Object.Destroy(node.gameObject);
        nodeUIs.Clear();

        foreach (var line in connectionLines)
            if (line != null) Object.Destroy(line);
        connectionLines.Clear();
    }

    private void OnNodeClick(TechNode node)
    {
        if (node.isUnlocked)
        {
            DebugManager.Log($"建筑 {node.building.buildingName} 已解锁");
            return;
        }

        if (node.CanUnlock())
        {
            bool success = TechTreeManager.Instance.UnlockBuilding(node.building);
            if (success)
            {
                if (nodeUIs.ContainsKey(node.building))
                {
                    AudioManager.Instance.PlaySfx("Unlock");
                    nodeUIs[node.building].SetUnlocked(true);
                }
            }
        }
        else
        {
            DebugManager.Log($"无法解锁 {node.building.buildingName}，需要满足前置条件或资源不足");
        }
    }

    /// <summary>立即复位</summary>
    private void ResetViewImmediate()
    {
        KillTweens();
        techTreeContainer.localScale = Vector3.one;
        techTreeContainer.anchoredPosition = Vector2.zero;
        _velocity = Vector2.zero;
        (connectionLineRoot as RectTransform)?.SetSiblingIndex(0);
    }

    private void ResetZoomAnimated()
    {
        KillTweens();
        _zoomTween = techTreeContainer.DOScale(Vector3.one, 0.25f);
        _panTween  = techTreeContainer.DOAnchorPos(Vector2.zero, 0.25f);
        _velocity = Vector2.zero;
    }

    private void ZoomInButton()  => ZoomButtonStep(+zoomSpeed);
    private void ZoomOutButton() => ZoomButtonStep(-zoomSpeed);

    private void ZoomButtonStep(float step)
    {
        // 按钮缩放以视口中心为基准
        var centerScreen = RectTransformUtility.WorldToScreenPoint(null, _viewport.position);
        ZoomAroundScreenPoint(centerScreen, step, zoomTweenTime);
    }

    /// <summary>滚轮缩放（围绕鼠标）</summary>
    public void OnScroll(PointerEventData eventData)
    {
        if (Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;

        Vector2 mouseScreen = eventData.position;
        float delta = eventData.scrollDelta.y * wheelZoomSpeed;
        
        ZoomAroundScreenPoint(mouseScreen, delta, zoomTweenTime);
    }

    private void ZoomAroundScreenPoint(Vector2 screenPoint, float delta, float tweenTime)
    {
        KillTweens();

        float oldScale = techTreeContainer.localScale.x;
        float newScale = Mathf.Clamp(oldScale + delta, minZoom, maxZoom);
        if (Mathf.Approximately(oldScale, newScale)) return;

        // 转换鼠标到视口的局部坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, screenPoint, null, out var viewPt))
            viewPt = Vector2.zero;

        // 计算目标位置
        Vector2 contentPos = techTreeContainer.anchoredPosition;
        Vector2 contentLocalPoint = (viewPt - contentPos) / oldScale;
        Vector2 targetPos = viewPt - contentLocalPoint * newScale;
        
        _zoomTween = techTreeContainer.DOScale(newScale, tweenTime).SetEase(Ease.OutQuad).SetUpdate(true);
        _panTween  = techTreeContainer.DOAnchorPos(targetPos, tweenTime).SetEase(Ease.OutQuad).SetUpdate(true);

        // 重置速度
        _velocity = Vector2.zero;
    }

    /// <summary>开始拖拽</summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _velocity = Vector2.zero;
        KillTweens();

        // 记录指针在视口的局部位置，以及当前内容位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, null, out _dragStartPosView);
        _contentStartPos = techTreeContainer.anchoredPosition;
    }

    /// <summary>拖拽中</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, null, out var viewPt))
            return;

        Vector2 deltaView = viewPt - _dragStartPosView;
        // 拖拽位移按“当前缩放”衰减，保证手感稳定
        Vector2 targetPos = _contentStartPos + deltaView;

        // 软边界：越越界，位移越被压缩（橡皮筋）
        targetPos = ApplyRubberBand(targetPos);

        // 赋值并计算速度（供惯性使用）
        Vector2 oldPos = techTreeContainer.anchoredPosition;
        techTreeContainer.anchoredPosition = targetPos;
        _velocity = (targetPos - oldPos) / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
    }

    /// <summary>结束拖拽</summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    private void Update()
    {
        // 惯性衰减
        if (!_isDragging && enableInertia && _velocity.sqrMagnitude > 1f)
        {
            // 先按速度推进
            Vector2 pos = techTreeContainer.anchoredPosition + _velocity * Time.unscaledDeltaTime;

            // 软边界
            pos = ApplyRubberBand(pos);

            techTreeContainer.anchoredPosition = pos;

            // 指数衰减
            float damp = Mathf.Clamp01(inertiaDamping * Time.unscaledDeltaTime);
            _velocity = Vector2.Lerp(_velocity, Vector2.zero, damp);
        }
    }
    
    private Vector2 ApplyRubberBand(Vector2 targetPos)
    {
        if (!enableRubberBand || _viewport == null) return targetPos;

        // 估算内容边界
        float scale = techTreeContainer.localScale.x;
        Vector2 contentSize = techTreeContainer.rect.size * scale;
        Vector2 viewSize = _viewport.rect.size;

        // 允许的移动范围
        Vector2 halfDiff = new Vector2(
            Mathf.Max(0f, (contentSize.x - viewSize.x) * 0.5f),
            Mathf.Max(0f, (contentSize.y - viewSize.y) * 0.5f)
        );
        
        Vector2 min = -halfDiff - Vector2.one * boundsPadding;
        Vector2 max = halfDiff + Vector2.one * boundsPadding;

        Vector2 pos = targetPos;

        // 对每个轴：若超出 min/max，则按距离做平滑压缩
        pos.x = RubberClamp(pos.x, min.x, max.x);
        pos.y = RubberClamp(pos.y, min.y, max.y);

        return pos;
    }

    private float RubberClamp(float v, float min, float max)
    {
        if (v < min)
        {
            float d = min - v;
            return min - d * (1f - rubberStrength);
        }
        if (v > max)
        {
            float d = v - max;
            return max + d * (1f - rubberStrength);
        }
        return v;
    }

    private void KillTweens()
    {
        _zoomTween?.Kill(false);
        _panTween?.Kill(false);
        _zoomTween = _panTween = null;
    }

    private void OnCloseClick()
    {
        GameManager.Instance.ResumeGame();
        UIManager.Instance.ClosePanel(panelName);
    }
}
