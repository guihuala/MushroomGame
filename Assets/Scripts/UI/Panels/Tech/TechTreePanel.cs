using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 科技树面板
/// </summary>
public class TechTreePanel : BasePanel, IDragHandler, IScrollHandler
{
    [Header("科技树设置")]
    [SerializeField] private RectTransform techTreeContainer; // 科技树容器
    [SerializeField] private GameObject techNodePrefab; // 科技节点预制体
    [SerializeField] private GameObject connectionLinePrefab; // 连接线预制体
    [SerializeField] private float nodeSpacing = 200f; // 节点间距
    [SerializeField] private float levelSpacing = 150f; // 层级间距

    [Header("缩放设置")]
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;
    [SerializeField] private float zoomSpeed = 0.1f;

    [Header("UI组件")]
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private CanvasGroup zoomControls;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button resetZoomButton;

    private Dictionary<BuildingData, TechNodeUI> nodeUIs = new Dictionary<BuildingData, TechNodeUI>();
    private List<GameObject> connectionLines = new List<GameObject>();
    private Vector2 dragStartPosition;
    private Vector2 containerStartPosition;

    protected override void Awake()
    {
        base.Awake();
        
        // 绑定按钮事件
        closeButton.onClick.AddListener(OnCloseClick);
        zoomInButton.onClick.AddListener(ZoomIn);
        zoomOutButton.onClick.AddListener(ZoomOut);
        resetZoomButton.onClick.AddListener(ResetZoom);
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
    }

    /// <summary>
    /// 初始化科技树
    /// </summary>
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
            int level = TechTreeManager.Instance.GetNodeLevel(building); // <-- 新方法
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
                // 兜底：按名字
                string na = techTree[a].building.buildingName;
                string nb = techTree[b].building.buildingName;
                return string.Compare(na, nb, System.StringComparison.Ordinal);
            });
        }

        // 3) 逐层摆放
        foreach (var level in levels)
        {
            int nodeCount = level.Value.Count;
            float totalWidth = (nodeCount - 1) * nodeSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < nodeCount; i++)
            {
                var building = level.Value[i];
                var node = techTree[building];

                var nodeObj = Instantiate(techNodePrefab, techTreeContainer);
                var nodeUI = nodeObj.GetComponent<TechNodeUI>();

                float xPos = startX + i * nodeSpacing;
                float yPos = -level.Key * levelSpacing;
                nodeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

                bool isUnlocked = unlockedBuildings.Contains(building);
                bool canUnlock = node.CanUnlock();
                nodeUI.Initialize(node, isUnlocked, canUnlock);
                nodeUI.OnNodeClick += OnNodeClick;

                nodeUIs[building] = nodeUI;
            }
        }

        // 4) 连线 & 视图复位
        CreateConnectionLines(techTree);
        ResetView();
    }

    /// <summary>
    /// 计算节点层级
    /// </summary>
    private int CalculateNodeLevel(TechNode node, Dictionary<BuildingData, TechNode> techTree)
    {
        if (node.prerequisites.Count == 0)
            return 0;

        int maxLevel = 0;
        foreach (var prereq in node.prerequisites)
        {
            int prereqLevel = CalculateNodeLevel(prereq, techTree) + 1;
            maxLevel = Mathf.Max(maxLevel, prereqLevel);
        }

        return maxLevel;
    }

    /// <summary>
    /// 创建连接线
    /// </summary>
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

    /// <summary>
    /// 创建单个连接线
    /// </summary>
    private void CreateConnectionLine(TechNodeUI fromNode, TechNodeUI toNode)
    {
        GameObject lineObj = Instantiate(connectionLinePrefab, techTreeContainer);
        ConnectionLine line = lineObj.GetComponent<ConnectionLine>();
        
        line.Initialize(fromNode.GetComponent<RectTransform>(), toNode.GetComponent<RectTransform>());
        connectionLines.Add(lineObj);
    }

    /// <summary>
    /// 清空科技树
    /// </summary>
    private void ClearTechTree()
    {
        foreach (var node in nodeUIs.Values)
        {
            if (node != null)
                Destroy(node.gameObject);
        }
        nodeUIs.Clear();

        foreach (var line in connectionLines)
        {
            if (line != null)
                Destroy(line);
        }
        connectionLines.Clear();
    }

    /// <summary>
    /// 节点点击事件
    /// </summary>
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
                // 更新UI
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
    
    /// <summary>
    /// 重置视图
    /// </summary>
    private void ResetView()
    {
        techTreeContainer.localScale = Vector3.one;
        techTreeContainer.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 缩放视图
    /// </summary>
    private void Zoom(float delta)
    {
        float newScale = Mathf.Clamp(techTreeContainer.localScale.x + delta, minZoom, maxZoom);
        techTreeContainer.localScale = Vector3.one * newScale;
    }

    private void ZoomIn() => Zoom(zoomSpeed);
    private void ZoomOut() => Zoom(-zoomSpeed);
    private void ResetZoom() => techTreeContainer.DOScale(Vector3.one, 0.3f);

    /// <summary>
    /// 拖动事件
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        techTreeContainer.anchoredPosition += eventData.delta / techTreeContainer.localScale.x;
    }

    /// <summary>
    /// 滚轮缩放事件
    /// </summary>
    public void OnScroll(PointerEventData eventData)
    {
        Zoom(eventData.scrollDelta.y * zoomSpeed * 0.1f);
    }

    private void OnCloseClick()
    {
        UIManager.Instance.ClosePanel(panelName);
    }
}