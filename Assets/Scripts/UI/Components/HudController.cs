using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Hub hub;
    [SerializeField] private Transform inventoryPanel; // 库存显示面板
    [SerializeField] private GameObject itemSlotPrefab; // 物品槽预制体
    public MushroomSelectionPanel mushroomSelectionPanel;  // 引用蘑菇选择面板
    
    private Dictionary<ItemDef, Text> _itemTexts = new Dictionary<ItemDef, Text>();
    private Dictionary<ItemDef, Image> _itemImages = new Dictionary<ItemDef, Image>();

    void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    private void Start()
    {
        MsgCenter.RegisterMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
        InitializeInventoryUI();
    }

    // 初始化库存UI
    private void InitializeInventoryUI()
    {
        if (inventoryPanel == null || itemSlotPrefab == null) return;

        // 清空现有物品槽
        foreach (Transform child in inventoryPanel)
        {
            Destroy(child.gameObject);
        }
        _itemTexts.Clear();
        _itemImages.Clear();

        // 为Hub中所有可能的物品创建UI槽位
        if (hub != null && hub.stages != null)
        {
            // 收集所有阶段中出现的物品
            HashSet<ItemDef> allItems = new HashSet<ItemDef>();
            foreach (var stage in hub.stages)
            {
                if (stage.requirements != null)
                {
                    foreach (var requirement in stage.requirements)
                    {
                        if (requirement.item != null)
                        {
                            allItems.Add(requirement.item);
                        }
                    }
                }
            }

            // 为每个物品创建UI槽位
            foreach (var item in allItems)
            {
                CreateItemSlot(item);
            }
        }
    }

    // 创建物品槽位
    private void CreateItemSlot(ItemDef item)
    {
        if (inventoryPanel == null || itemSlotPrefab == null) return;

        GameObject slot = Instantiate(itemSlotPrefab, inventoryPanel);
        slot.name = $"{item.name}_Slot";

        // 获取文本和图像组件
        Text itemText = slot.GetComponentInChildren<Text>();
        Image itemImage = slot.transform.Find("Icon")?.GetComponent<Image>();

        if (itemText != null)
        {
            _itemTexts[item] = itemText;
            UpdateItemCount(item, hub.GetItemCount(item));
        }

        if (itemImage != null && item.icon != null)
        {
            itemImage.sprite = item.icon;
            _itemImages[item] = itemImage;
        }
    }

    // 更新物品数量显示
    private void UpdateItemCount(ItemDef item, int count)
    {
        if (_itemTexts.ContainsKey(item))
        {
            var currentStage = hub.GetCurrentStage();
            int requiredAmount = 0;

            // 获取当前阶段该物品的需求数量
            if (currentStage != null && currentStage.requirements != null)
            {
                foreach (var requirement in currentStage.requirements)
                {
                    if (requirement.item == item)
                    {
                        requiredAmount = requirement.requiredAmount;
                        break;
                    }
                }
            }

            // 显示格式：当前数量/需求数量
            _itemTexts[item].text = $"{count}/{requiredAmount}";

            // 根据是否满足需求改变颜色
            if (count >= requiredAmount && requiredAmount > 0)
            {
                _itemTexts[item].color = Color.green; // 满足需求显示绿色
            }
            else
            {
                _itemTexts[item].color = Color.white; // 未满足显示白色
            }
        }
    }

    // 显示蘑菇选择面板
    private void ShowMushroomSelectionPanel(params object[] args)
    {
        Vector2Int targetCell = (Vector2Int)args[0];
        
        // 获取已解锁的蘑菇建筑列表
        List<Building> mushrooms = GetUnlockedMushrooms();

        // 调用蘑菇选择面板显示
        mushroomSelectionPanel.ShowMushroomPanel(mushrooms, targetCell);
    }

    // 获取已解锁的蘑菇建筑列表
    private List<Building> GetUnlockedMushrooms()
    {
        // 获取已解锁的蘑菇建筑列表
        return TechTreeManager.Instance.GetUnlockedBuildings().Where(b => b is MushroomBuilding).ToList();
    }
    
    void OnEnable()
    {
        if (hub != null)
        {
            hub.OnItemReceived += HandleItemReceived;
            hub.OnStageCompleted += HandleStageCompleted;
        }
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnItemReceived -= HandleItemReceived;
            hub.OnStageCompleted -= HandleStageCompleted;
        }
    }

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }

    private void HandleItemReceived(ItemPayload payload)
    {
        if (payload.item != null)
        {
            // 更新特定物品的数量显示
            UpdateItemCount(payload.item, hub.GetItemCount(payload.item));
        }
    }

    private void HandleStageCompleted(int stageIndex)
    {
        // 阶段完成时更新所有物品显示（需求数量可能变化）
        RefreshAllItemDisplays();
    }

    // 刷新所有物品显示
    private void RefreshAllItemDisplays()
    {
        if (hub != null)
        {
            foreach (var itemPair in _itemTexts)
            {
                UpdateItemCount(itemPair.Key, hub.GetItemCount(itemPair.Key));
            }
        }
    }

    // 更新UI显示（每帧更新，确保实时性）
    private void Update()
    {
        // 可选：每帧更新进度显示，或者使用事件驱动
        // 如果性能有问题，可以改为定时更新或事件驱动
        RefreshAllItemDisplays();
    }

    // 添加调试按钮（可选）
    [ContextMenu("Refresh Inventory UI")]
    private void RefreshInventoryUI()
    {
        InitializeInventoryUI();
        RefreshAllItemDisplays();
    }
}