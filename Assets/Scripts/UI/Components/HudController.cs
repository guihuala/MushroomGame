using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button techTreeButton;
    [SerializeField] private Hub hub;

    [Header("HUD Inventory Display")]
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private GameObject inventoryItemPrefab;

    [Header("Task Panel Reference")]
    [SerializeField] private TaskPanel taskPanel;

    [Header("Mushroom Selection")]
    public MushroomSelectionPanel mushroomSelectionPanel;

    private Dictionary<ItemDef, InventoryHudItem> _hudItems = new Dictionary<ItemDef, InventoryHudItem>();
    private bool _isInitialized = false;

    void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
        techTreeButton.onClick.AddListener(OnTechTreeButtonClicked);
    }

    private void Start()
    {
        // 注册消息中心事件
        MsgCenter.RegisterMsg(MsgConst.SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        MsgCenter.RegisterMsg(MsgConst.HUB_CLICKED, OnHubClicked);
        MsgCenter.RegisterMsg(MsgConst.INVENTORY_ITEM_ADDED, HandleItemAdded);
        MsgCenter.RegisterMsgAct(MsgConst.INVENTORY_CHANGED, HandleInventoryChanged);
        MsgCenter.RegisterMsg(MsgConst.HUB_STAGE_COMPLETED, HandleStageCompleted);

        InitializeHudInventory();

        // 初始化任务面板
        if (taskPanel != null)
        {
            taskPanel.Initialize(hub);
        }
    }

    private void OnDestroy()
    {
        // 注销消息中心事件
        MsgCenter.UnregisterMsg(MsgConst.SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        MsgCenter.UnregisterMsg(MsgConst.HUB_CLICKED, OnHubClicked);
        MsgCenter.UnregisterMsg(MsgConst.INVENTORY_ITEM_ADDED, HandleItemAdded);
        MsgCenter.UnregisterMsgAct(MsgConst.INVENTORY_CHANGED, HandleInventoryChanged);
        MsgCenter.UnregisterMsg(MsgConst.HUB_STAGE_COMPLETED, HandleStageCompleted);
    }

    private void InitializeHudInventory()
    {
        if (inventoryContainer == null || inventoryItemPrefab == null) return;

        ClearHudItems();

        var allItems = GetAllItemsInHub();
        var sortedItems = allItems.OrderByDescending(item => InventoryManager.Instance.GetItemCount(item)).ToList();

        foreach (var item in sortedItems)
        {
            CreateHudItemSlot(item);
        }

        _isInitialized = true;
    }

    private void ClearHudItems()
    {
        foreach (Transform child in inventoryContainer)
        {
            Destroy(child.gameObject);
        }
        _hudItems.Clear();
    }

    private HashSet<ItemDef> GetAllItemsInHub()
    {
        HashSet<ItemDef> items = new HashSet<ItemDef>();
        
        var currentStage = hub.GetCurrentStage();
        if (currentStage != null && currentStage.requirements != null)
        {
            foreach (var requirement in currentStage.requirements)
            {
                if (requirement.item != null)
                {
                    items.Add(requirement.item);
                }
            }
        }
        return items;
    }

    private void CreateHudItemSlot(ItemDef item)
    {
        GameObject slotObj = Instantiate(inventoryItemPrefab, inventoryContainer);
        InventoryHudItem hudItem = slotObj.GetComponent<InventoryHudItem>();
        
        if (hudItem != null)
        {
            hudItem.Initialize(item);
            _hudItems[item] = hudItem;
            UpdateHudItemDisplay(item);
        }
    }

    private void UpdateAllHudItems()
    {
        foreach (var item in _hudItems.Keys.ToList())
        {
            UpdateHudItemDisplay(item);
        }
    }

    private void UpdateHudItemDisplay(ItemDef item)
    {
        if (_hudItems.ContainsKey(item))
        {
            int currentAmount = InventoryManager.Instance.GetItemCount(item);
            _hudItems[item].UpdateDisplay(currentAmount);
        }
    }

    private void ShowMushroomSelectionPanel(params object[] args)
    {
        Vector2Int targetCell = (Vector2Int)args[0];
        List<BuildingData> mushrooms = GetUnlockedMushrooms();
        mushroomSelectionPanel.ShowMushroomPanel(mushrooms, targetCell);
    }

    private List<BuildingData> GetUnlockedMushrooms()
    {
        return TechTreeManager.Instance.GetUnlockedBuildingsByCategory(BuildingCategory.Mushroom);
    }

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }

    private void OnTechTreeButtonClicked()
    {
        UIManager.Instance.OpenPanel("TechTreePanel");
    }
    

    private void HandleInventoryChanged()
    {
        UpdateAllHudItems();
        
        // 更新任务面板
        taskPanel?.UpdateTaskPanelProgress();
    }

    private void HandleItemAdded(params object[] args)
    {
        if (args.Length > 0 && args[0] is ItemDef item)
        {
            if (!_hudItems.ContainsKey(item))
            {
                // 检查这个物品是否在当前阶段的需求中
                var currentStage = hub.GetCurrentStage();
                if (currentStage != null && currentStage.requirements.Any(r => r.item == item))
                {
                    CreateHudItemSlot(item);
                }
            }
            else
            {
                UpdateHudItemDisplay(item);
            }

            // 更新任务面板的特定物品显示
            taskPanel?.UpdateTaskItemDisplay(item);
        }
    }

    private void HandleStageCompleted(params object[] args)
    {
        if (args.Length > 0 && args[0] is int stageIndex)
        {
            InitializeHudInventory();
            taskPanel?.UpdateTaskPanelProgress();
        }
    }

    private void OnHubClicked(params object[] args)
    {
        if (args.Length > 0 && args[0] is Hub clickedHub && clickedHub == hub)
        {
            taskPanel?.ShowPanel();
        }
    }

    private void Update()
    {
        // 减少更新频率，每60帧更新一次
        if (Time.frameCount % 60 == 0 && _isInitialized)
        {
            UpdateAllHudItems();
        }
    }
}