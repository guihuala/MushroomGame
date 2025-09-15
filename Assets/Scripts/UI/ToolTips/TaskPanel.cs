using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour, IHoverPanel
{
    [Header("UI References")]
    [SerializeField] private Text taskStageText;
    [SerializeField] private Transform taskContent;
    [SerializeField] private GameObject taskItemPrefab;
    
    [SerializeField] private bool smartOffsetBySize = true;         // 智能偏移：跟随面板尺寸并自动翻转
    [SerializeField] private Vector2 cursorPadding = new Vector2(16f, 16f); // 鼠标与面板的最小间距（像素/Canvas单位）
    [SerializeField] private float edgePadding = 8f;                // 贴边留白
    
    private Hub _hub;
    private Dictionary<ItemDef, TaskPanelItem> _taskItems = new Dictionary<ItemDef, TaskPanelItem>();
    private DraggablePanel _draggablePanel;
    
    // === 悬浮定位相关 ===
    [SerializeField] private Vector2 hoverOffset = new Vector2(18f, -18f); // 面板相对鼠标的偏移
    [SerializeField] private bool clampToCanvas = true;                    // 贴边防出界

    private Canvas _rootCanvas;
    private RectTransform _rt;

    private void OnEnable()
    {
        CacheRTAndCanvas();
    }

    private void CacheRTAndCanvas()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>();
    }
    
    // ==== IHoverPanel 实现 ====
    public void ShowAtScreenPosition(Vector2 screenPos)
    {
        Initialize(FindObjectOfType<Hub>()); 
        ShowPanel();
        Reposition(screenPos);
    }

    public void FollowMouse(Vector2 screenPos)
    {
        if (!gameObject.activeSelf) return;
        Reposition(screenPos);
    }
    
    public void SetContext(object context)
    {
        if (context is Hub hub) Initialize(hub);
    }

private void Reposition(Vector2 screenPos)
{
    CacheRTAndCanvas();
    if (_rt == null) return;

    if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.WorldSpace)
    {
        var canvasRT = (RectTransform)_rootCanvas.transform;

        // 鼠标的 Canvas 局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, _rootCanvas.worldCamera, out var mouseLocal);

        var size  = _rt.rect.size;   // 面板当前尺寸（受 Layout 变化）
        var pivot = _rt.pivot;       // 面板当前 pivot

        Vector2 pos; // 目标 anchoredPosition

        if (!smartOffsetBySize)
        {
            // 兼容旧逻辑：固定偏移
            pos = mouseLocal + hoverOffset;
        }
        else
        {
            // 默认放在鼠标的“右下角”（不遮住鼠标）
            // 右侧：left(minX) = mouseX + paddingX
            // 下方：top (maxY) = mouseY - paddingY
            pos = new Vector2(
                mouseLocal.x + cursorPadding.x + pivot.x * size.x,
                mouseLocal.y - cursorPadding.y - (1f - pivot.y) * size.y
            );

            // 画布可见范围（留出边距）
            Vector2 minEdge = canvasRT.rect.min + new Vector2(edgePadding, edgePadding);
            Vector2 maxEdge = canvasRT.rect.max - new Vector2(edgePadding, edgePadding);

            // 计算当前面板在画布内的包围（以 anchoredPosition + pivot 求 min/max）
            Vector2 min = pos - pivot * size;
            Vector2 max = pos + (Vector2.one - pivot) * size;

            // 水平翻转：若右侧溢出，则放到鼠标左侧（right=maxX = mouseX - paddingX）
            if (max.x > maxEdge.x)
            {
                pos.x = mouseLocal.x - cursorPadding.x - (1f - pivot.x) * size.x;
            }

            // 重新算一次左右边界并夹紧（面板比画布还大等极端情况）
            min = pos - pivot * size;
            max = pos + (Vector2.one - pivot) * size;
            if (min.x < minEdge.x)
                pos.x += (minEdge.x - min.x);
            else if (max.x > maxEdge.x)
                pos.x -= (max.x - maxEdge.x);

            // 垂直翻转：若下方溢出，则放到鼠标上方（bottom=minY = mouseY + paddingY）
            min = pos - pivot * size;
            max = pos + (Vector2.one - pivot) * size;
            if (min.y < minEdge.y)
            {
                pos.y = mouseLocal.y + cursorPadding.y + pivot.y * size.y;
            }

            // 再次夹紧上下边界
            min = pos - pivot * size;
            max = pos + (Vector2.one - pivot) * size;
            if (min.y < minEdge.y)
                pos.y += (minEdge.y - min.y);
            else if (max.y > maxEdge.y)
                pos.y -= (max.y - maxEdge.y);
        }

        _rt.anchoredPosition = pos;

        // 兼容你的原有“贴边防出界”开关（智能模式已做过一次，保留这层保险）
        if (clampToCanvas)
        {
            Vector2 minEdge = canvasRT.rect.min + new Vector2(edgePadding, edgePadding);
            Vector2 maxEdge = canvasRT.rect.max - new Vector2(edgePadding, edgePadding);
            Vector2 min = _rt.anchoredPosition - pivot * size;
            Vector2 max = _rt.anchoredPosition + (Vector2.one - pivot) * size;

            Vector2 corrected = _rt.anchoredPosition;
            if (min.x < minEdge.x) corrected.x += (minEdge.x - min.x);
            if (max.x > maxEdge.x) corrected.x -= (max.x - maxEdge.x);
            if (min.y < minEdge.y) corrected.y += (minEdge.y - min.y);
            if (max.y > maxEdge.y) corrected.y -= (max.y - maxEdge.y);
            _rt.anchoredPosition = corrected;
        }
    }
    else
    {
        // World Space Canvas：简单偏移
        _rt.position = (Vector2)_rt.position + hoverOffset;
    }
}

    private void Awake()
    {
        _draggablePanel = GetComponent<DraggablePanel>();
        if (_draggablePanel == null)
        {
            _draggablePanel = gameObject.AddComponent<DraggablePanel>();
        }
    }

    public void Initialize(Hub hub)
    {
        _hub = hub;
        
        ClosePanel();
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        InitializeTaskPanel();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void InitializeTaskPanel()
    {
        if (taskContent == null || taskItemPrefab == null) return;

        ClearTaskItems();

        var currentStage = _hub.GetCurrentStage();
        if (currentStage != null && currentStage.requirements != null)
        {
            foreach (var requirement in currentStage.requirements)
            {
                if (requirement.item != null)
                {
                    CreateTaskItemSlot(requirement.item, requirement.requiredAmount);
                }
            }
        }

        UpdateTaskPanelProgress();
    }

    private void ClearTaskItems()
    {
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }
        _taskItems.Clear();
    }

    private void CreateTaskItemSlot(ItemDef item, int requiredAmount)
    {
        GameObject slotObj = Instantiate(taskItemPrefab, taskContent);
        TaskPanelItem taskItem = slotObj.GetComponent<TaskPanelItem>();
        
        if (taskItem != null)
        {
            taskItem.Initialize(item, requiredAmount);
            _taskItems[item] = taskItem;
            UpdateTaskItemDisplay(item);
        }
    }

    public void UpdateTaskItemDisplay(ItemDef item)
    {
        if (_taskItems.ContainsKey(item))
        {
            int currentAmount = _hub.GetItemCount(item);
            float progress = _hub.GetItemProgress(item);
            _taskItems[item].UpdateDisplay(currentAmount, progress);
        }
    }

    public void UpdateTaskPanelProgress()
    {
        if (taskStageText != null)
        {
            var currentStage = _hub.GetCurrentStage();
            if (currentStage != null)
            {
                taskStageText.text = $"Stage {_hub.currentStageIndex + 1}/{_hub.stages.Count}: {currentStage.stageName}";
            }
        }
    }
}