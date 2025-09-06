using UnityEngine;
using UnityEngine.UI;

public class InventoryHudItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Text quantityText;

    private ItemDef _item;

    public void Initialize(ItemDef item)
    {
        _item = item;
        
        if (itemIcon != null && item.icon != null)
        {
            itemIcon.sprite = item.icon;
        }
    }

    public void UpdateDisplay(int currentAmount)
    {
        if (quantityText != null)
        {
            quantityText.text = currentAmount.ToString();
        }
    }
}