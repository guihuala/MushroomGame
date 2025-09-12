using UnityEngine;
using UnityEngine.EventSystems;

public class MinerHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Miner _miner;

    private void Awake()
    {
        _miner = GetComponent<Miner>();
        if (_miner == null)
        {
            Debug.LogError("Miner component not found on " + gameObject.name);
        }
        
        EnsureCollider();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_miner == null) return;
        BuildingSelectionUI.Instance.ShowMinerTooltip(miner: _miner);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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