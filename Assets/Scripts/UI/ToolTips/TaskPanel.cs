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
        if (_rt == null || _rootCanvas == null) return;

        UIPosUtil.PlacePanelAtScreenPoint(
            _rt, _rootCanvas, screenPos,
            cursorPadding, edgePadding, smartFlip: true);
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