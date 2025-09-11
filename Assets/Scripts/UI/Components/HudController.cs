using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button techTreeButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Hub hub;

    [SerializeField] private Transform inventoryPanel;
    [SerializeField] private Transform inventoryContainer;
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Button toggleInventoryButton;

    [SerializeField] private TaskPanel taskPanel;
    
    [Header("收缩设置")]
    public float slideSpeed = 10f;  // 控制收缩和展开的速度
    public float hiddenX = -600f;   // 收缩后的 X 坐标
    public float shownX = 0f;       // 展开后的 X 坐标

    private bool isInventoryOpen = true;  // 标记面板是否展开
    
    public MushroomSelectionPanel mushroomSelectionPanel;

    private Dictionary<ItemDef, InventoryHudItem> _hudItems = new Dictionary<ItemDef, InventoryHudItem>();
    private bool _isInitialized = false;

    #region 生命周期

    void Awake()
    {
        toggleInventoryButton.onClick.AddListener(ToggleInventoryPanel);
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
        techTreeButton.onClick.AddListener(OnTechTreeButtonClicked);
        guideButton.onClick.AddListener(OnGuideButtonClicked);
    }

    private void Start()
    {
        MsgCenter.RegisterMsg(MsgConst.SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        MsgCenter.RegisterMsg(MsgConst.HUB_CLICKED, OnHubClicked);
        MsgCenter.RegisterMsg(MsgConst.INVENTORY_ITEM_ADDED, HandleItemAdded);
        MsgCenter.RegisterMsgAct(MsgConst.INVENTORY_CHANGED, HandleInventoryChanged);
        MsgCenter.RegisterMsg(MsgConst.HUB_STAGE_COMPLETED, HandleStageCompleted);

        InitializeInventory();
        
        if (taskPanel != null)
        {
            taskPanel.Initialize(hub);
        }

        inventoryContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(shownX,
            inventoryContainer.GetComponent<RectTransform>().anchoredPosition.y);
    }

    private void OnDestroy()
    {
        MsgCenter.UnregisterMsg(MsgConst.SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        MsgCenter.UnregisterMsg(MsgConst.HUB_CLICKED, OnHubClicked);
        MsgCenter.UnregisterMsg(MsgConst.INVENTORY_ITEM_ADDED, HandleItemAdded);
        MsgCenter.UnregisterMsgAct(MsgConst.INVENTORY_CHANGED, HandleInventoryChanged);
        MsgCenter.UnregisterMsg(MsgConst.HUB_STAGE_COMPLETED, HandleStageCompleted);
    }

    #endregion

    #region 按钮

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }

    private void OnTechTreeButtonClicked()
    {
        UIManager.Instance.OpenPanel("TechTreePanel");
    }

    private void OnGuideButtonClicked()
    {
        UIManager.Instance.OpenPanel("GuidePanel");
    }

    #endregion

    #region 库存

    private void InitializeInventory()
    {
        if (inventoryContainer == null || inventoryItemPrefab == null) return;

        ClearItems();
        
        var allItems = InventoryManager.Instance.GetAllItemStacks();
        var sortedItems = allItems.OrderByDescending(item => item.amount).ToList(); // 按数量排序

        foreach (var itemStack in sortedItems)
        {
            CreateItemSlot(itemStack);
        }

        _isInitialized = true;
    }

    private void ClearItems()
    {
        // 清空所有现有物品槽
        foreach (Transform child in inventoryContainer)
        {
            Destroy(child.gameObject);
        }
        _hudItems.Clear();
    }

    private void CreateItemSlot(ItemStack itemStack)
    {
        // 创建物品槽
        GameObject slotObj = Instantiate(inventoryItemPrefab, inventoryContainer);
        InventoryHudItem hudItem = slotObj.GetComponent<InventoryHudItem>();

        if (hudItem != null)
        {
            hudItem.Initialize(itemStack.item);  // 初始化物品槽
            _hudItems[itemStack.item] = hudItem;  // 保存到 _hudItems 字典
            UpdateItemDisplay(itemStack.item);    // 更新物品显示
        }
    }

    private void UpdateAllItems()
    {
        // 更新所有物品显示
        foreach (var item in _hudItems.Keys.ToList())
        {
            UpdateItemDisplay(item);
        }
    }

    private void UpdateItemDisplay(ItemDef item)
    {
        if (_hudItems.ContainsKey(item))
        {
            // 获取库存中的物品数量
            int currentAmount = InventoryManager.Instance.GetItemCount(item);
            _hudItems[item].UpdateDisplay(currentAmount);  // 更新物品槽
        }
    }
    
    #region 收缩与展开
    
    public void ToggleInventoryPanel()
    {
        if (isInventoryOpen)
        {
            StartCoroutine(SlideInventoryPanel(hiddenX));  // 收缩
        }
        else
        {
            StartCoroutine(SlideInventoryPanel(shownX));  // 展开
        }

        // 切换状态
        isInventoryOpen = !isInventoryOpen;
    }
    
    private IEnumerator SlideInventoryPanel(float targetX)
    {
        RectTransform rt = inventoryPanel.GetComponent<RectTransform>();
        Vector2 currentPos = rt.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, currentPos.y);

        float timeElapsed = 0f;

        while (timeElapsed < 1f)
        {
            rt.anchoredPosition = Vector2.Lerp(currentPos, targetPos, timeElapsed);
            timeElapsed += Time.deltaTime * slideSpeed;
            yield return null;
        }

        rt.anchoredPosition = targetPos;  // 确保最终位置准确
    }

    #endregion

    #endregion

    #region 消息处理

    private void HandleInventoryChanged()
    {
        UpdateAllItems();
        
        taskPanel?.UpdateTaskPanelProgress();
    }

    private void HandleItemAdded(params object[] args)
    {
        if (args.Length > 0 && args[0] is ItemDef item)
        {
            int itemCount = InventoryManager.Instance.GetItemCount(item);

            ItemStack itemStack = new ItemStack
            {
                item = item,
                amount = itemCount
            };
            
            if (!_hudItems.ContainsKey(item))
            {
                CreateItemSlot(itemStack);  // 创建物品槽
            }
            else
            {
                UpdateItemDisplay(item);  // 更新物品显示
            }

            taskPanel?.UpdateTaskItemDisplay(item);
        }
    }


    private void HandleStageCompleted(params object[] args)
    {
        if (args.Length > 0 && args[0] is int stageIndex)
        {
            InitializeInventory();
            taskPanel?.UpdateTaskPanelProgress();
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

    private void OnHubClicked(params object[] args)
    {
        if (args.Length > 0 && args[0] is Hub clickedHub && clickedHub == hub)
        {
            taskPanel?.ShowPanel();
        }
    }

    #endregion
}
