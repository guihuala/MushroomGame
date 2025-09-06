using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
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
    }

    private void Start()
    {
        MsgCenter.RegisterMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        
        if (hub != null)
        {
            hub.OnItemReceived += HandleItemReceived;
            hub.OnStageCompleted += HandleStageCompleted;
            InitializeHudInventory();
        }

        // 初始化任务面板
        if (taskPanel != null)
        {
            taskPanel.Initialize(hub);
        }

        MsgCenter.RegisterMsg(MsgConst.MSG_HUB_CLICKED, OnHubClicked);
    }

    private void OnDestroy()
    {
        if (hub != null)
        {
            hub.OnItemReceived -= HandleItemReceived;
            hub.OnStageCompleted -= HandleStageCompleted;
        }
        
        MsgCenter.UnregisterMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        MsgCenter.UnregisterMsg(MsgConst.MSG_HUB_CLICKED, OnHubClicked);
    }

    private void InitializeHudInventory()
    {
        if (inventoryContainer == null || inventoryItemPrefab == null) return;

        ClearHudItems();

        var allItems = GetAllItemsInHub();
        var sortedItems = allItems.OrderByDescending(item => hub.GetItemCount(item)).ToList();

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
            int currentAmount = hub.GetItemCount(item);
            _hudItems[item].UpdateDisplay(currentAmount);
        }
    }

    private void ShowMushroomSelectionPanel(params object[] args)
    {
        Vector2Int targetCell = (Vector2Int)args[0];
        List<Building> mushrooms = GetUnlockedMushrooms();
        mushroomSelectionPanel.ShowMushroomPanel(mushrooms, targetCell);
    }

    private List<Building> GetUnlockedMushrooms()
    {
        return TechTreeManager.Instance.GetUnlockedBuildings().Where(b => b is MushroomBuilding).ToList();
    }

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }

    private void HandleItemReceived(ItemPayload payload)
    {
        if (payload.item != null)
        {
            if (_hudItems.ContainsKey(payload.item))
            {
                UpdateHudItemDisplay(payload.item);
            }
            else
            {
                CreateHudItemSlot(payload.item);
            }

            // 更新任务面板
            taskPanel?.UpdateTaskItemDisplay(payload.item);
        }
    }

    private void HandleStageCompleted(int stageIndex)
    {
        InitializeHudInventory();
        taskPanel?.UpdateTaskPanelProgress();
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
        if (Time.frameCount % 60 == 0 && _isInitialized)
        {
            UpdateAllHudItems();
        }
    }
}