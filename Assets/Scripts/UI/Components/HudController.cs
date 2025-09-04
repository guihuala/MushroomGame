using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Hub hub;
    [SerializeField] private ItemDef watchItem;
    [SerializeField] private Text label;

    void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    public MushroomSelectionPanel mushroomSelectionPanel;  // 引用蘑菇选择面板

    private void Start()
    {
        MsgCenter.RegisterMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, ShowMushroomSelectionPanel);
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
        }
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnItemReceived -= HandleItemReceived;
        }
    }

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }

    private void HandleItemReceived(ItemPayload payload)
    {
        if (watchItem != null && payload.item == watchItem)
        {
            int count = hub.GetItemCount(watchItem);
            label.text = $"{watchItem.name}: {count}";
        }
    }
}