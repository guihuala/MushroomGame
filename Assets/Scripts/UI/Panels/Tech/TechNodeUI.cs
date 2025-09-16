using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TechNodeUI : MonoBehaviour, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("UI组件")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private GameObject lockedOverlay;

    public System.Action<TechNode> OnNodeClick;
    private TechNode techNode;
    private bool _hovering;

    public void Initialize(TechNode node, bool isUnlocked, bool canUnlock)
    {
        techNode = node;
        iconImage.sprite = node.building.icon;
        nameText.text = node.building.buildingName;
        SetUnlocked(isUnlocked);
    }

    public void SetUnlocked(bool unlocked)
    {
        lockedOverlay.SetActive(!unlocked);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnNodeClick?.Invoke(techNode);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        TechCostTooltip.Instance?.Show(techNode, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        TechCostTooltip.Instance?.Hide();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_hovering)
            TechCostTooltip.Instance?.Show(techNode, eventData.position);
    }
}