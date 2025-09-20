using System;
using UnityEngine;
using UnityEngine.UI;

public class CostRow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private Text amountText;

    [Header("Colors")]
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color lackColor   = new Color(1f, 0.35f, 0.35f, 1f);

    private ItemDef _item;
    private int _need;
    
    private Action _onInventoryChangedAct;
    private MsgRecAction _onItemChangedMsg;

    private void OnEnable()
    {
        // 无参：任何库存变化都全量刷新一遍
        _onInventoryChangedAct = OnInventoryChanged_NoArgs;
        MsgCenter.RegisterMsgAct(MsgConst.INVENTORY_CHANGED, _onInventoryChangedAct);

        // 有参：只在此行对应物品变更时刷新
        _onItemChangedMsg = OnInventoryItemChanged_WithArgs;
        MsgCenter.RegisterMsg(MsgConst.INVENTORY_ITEM_ADDED, _onItemChangedMsg);
        MsgCenter.RegisterMsg(MsgConst.INVENTORY_ITEM_REMOVED, _onItemChangedMsg);
    }

    private void OnDisable()
    {
        if (_onInventoryChangedAct != null)
            MsgCenter.UnregisterMsgAct(MsgConst.INVENTORY_CHANGED, _onInventoryChangedAct);

        if (_onItemChangedMsg != null)
        {
            MsgCenter.UnregisterMsg(MsgConst.INVENTORY_ITEM_ADDED, _onItemChangedMsg);
            MsgCenter.UnregisterMsg(MsgConst.INVENTORY_ITEM_REMOVED, _onItemChangedMsg);
        }
    }
    
    public void Bind(ItemDef item, int need, int have)
    {
        _item = item;
        _need = Mathf.Max(0, need);

        if (icon != null)
            icon.sprite = item != null ? item.icon : null;

        UpdateAmount(have);
    }

    /// <summary>无参更新：从库存拉最新数量。</summary>
    private void OnInventoryChanged_NoArgs()
    {
        if (_item == null) return;
        int have = InventoryManager.Instance.GetItemCount(_item);
        UpdateAmount(have);
    }

    /// <summary>有参更新：仅此行物品变化时刷新。</summary>
    private void OnInventoryItemChanged_WithArgs(params object[] objs)
    {
        if (_item == null || objs == null || objs.Length < 2) return;
        
        var changedItem = objs[0] as ItemDef;
        if (changedItem == null || changedItem != _item) return;

        int have = InventoryManager.Instance.GetItemCount(_item);
        UpdateAmount(have);
    }

    /// <summary>渲染文本与颜色。</summary>
    public void UpdateAmount(int have)
    {
        have = Mathf.Max(0, have);
        if (amountText != null)
        {
            amountText.text = $"{have}/{_need}";
            amountText.color = (have >= _need) ? enoughColor : lackColor;
        }
    }
}