using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TechNodeUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private GameObject lockedOverlay;

    public System.Action<TechNode> OnNodeClick;
    private TechNode techNode;

    public void Initialize(TechNode node, bool isUnlocked, bool canUnlock)
    {
        techNode = node;
        
        iconImage.sprite = node.building.icon;
        nameText.text = node.building.buildingName;
        
        SetUnlocked(isUnlocked);
        
        // 设置成本文本
        string costString = "";
        foreach (var cost in node.unlockCost)
        {
            costString += $"{cost.item.displayName} x{cost.amount}\n";
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        lockedOverlay.SetActive(!unlocked);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnNodeClick?.Invoke(techNode);
    }
}