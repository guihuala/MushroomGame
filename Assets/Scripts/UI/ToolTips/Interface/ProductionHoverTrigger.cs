using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class ProductionHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private IProductionInfoProvider _provider;
    private Collider2D _col;
    private bool _blockedByErase;

    private void Awake()
    {
        _provider = GetComponent<IProductionInfoProvider>();
        if (_provider == null) _provider = GetComponentInParent<IProductionInfoProvider>();
        _col = EnsureCollider();
    }

    private void OnEnable()
    {
        MsgCenter.RegisterMsgAct(MsgConst.ERASE_MODE_ENTER, OnEraseEnter);
        MsgCenter.RegisterMsgAct(MsgConst.ERASE_MODE_EXIT,  OnEraseExit);
    }
    private void OnDisable()
    {
        MsgCenter.UnregisterMsgAct(MsgConst.ERASE_MODE_ENTER, OnEraseEnter);
        MsgCenter.UnregisterMsgAct(MsgConst.ERASE_MODE_EXIT,  OnEraseExit);
    }

    private void OnEraseEnter() { _blockedByErase = true;  if (_col) _col.enabled = false; }
    private void OnEraseExit()  { _blockedByErase = false; if (_col) _col.enabled = true;  }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_blockedByErase) return;
        BuildingSelectionUI.Instance.ShowProductionTooltip(_provider);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_blockedByErase) return;
        BuildingSelectionUI.Instance.CloseAllTooltips();
    }

    private Collider2D EnsureCollider()
    {
        var col = GetComponent<Collider2D>();
        if (!col)
        {
            var sr = GetComponent<SpriteRenderer>();
            var bc = gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = (sr && sr.sprite) ? sr.sprite.bounds.size : new Vector2(1.2f, 1.2f);
            col = bc;
        }
        return col;
    }
}