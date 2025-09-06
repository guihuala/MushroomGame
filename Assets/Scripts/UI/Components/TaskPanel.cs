using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text taskStageText;
    [SerializeField] private Transform taskContent;
    [SerializeField] private GameObject taskItemPrefab;
    [SerializeField] private Button closeButton;

    private Hub _hub;
    private Dictionary<ItemDef, TaskPanelItem> _taskItems = new Dictionary<ItemDef, TaskPanelItem>();
    private DraggablePanel _draggablePanel;

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
        closeButton.onClick.AddListener(ClosePanel);
        
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