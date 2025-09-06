using UnityEngine;
using UnityEngine.UI;

public class TaskPanelItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Text progressText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color completedColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private Color inProgressColor = new Color(0.8f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private Color notStartedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    private ItemDef _item;
    private int _requiredAmount;

    public void Initialize(ItemDef item, int requiredAmount)
    {
        _item = item;
        _requiredAmount = requiredAmount;
        
        if (itemIcon != null && item.icon != null)
        {
            itemIcon.sprite = item.icon;
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.name;
        }
    }

    public void UpdateDisplay(int currentAmount, float progress)
    {
        if (progressText != null)
        {
            progressText.text = $"{currentAmount}/{_requiredAmount}";
        }

        if (backgroundImage != null)
        {
            if (progress >= 1f)
            {
                backgroundImage.color = completedColor;
            }
            else if (progress > 0f)
            {
                backgroundImage.color = inProgressColor;
            }
            else
            {
                backgroundImage.color = notStartedColor;
            }
        }
    }
}