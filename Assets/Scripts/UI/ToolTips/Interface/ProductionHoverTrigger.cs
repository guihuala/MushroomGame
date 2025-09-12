using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class ProductionHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private IProductionInfoProvider _provider;
    private bool _inside;

    private void Awake()
    {
        _provider = GetComponent<IProductionInfoProvider>();
        if (_provider == null) _provider = GetComponentInParent<IProductionInfoProvider>();

        EnsureCollider();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _inside = true;
        BuildingSelectionUI.Instance.ShowProductionTooltip(_provider);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _inside = false;
        BuildingSelectionUI.Instance.CloseAllTooltips();
    }

    private void EnsureCollider()
    {
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var sr = GetComponent<SpriteRenderer>();
            var bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size : new Vector2(1.2f, 1.2f);
        }
    }
}