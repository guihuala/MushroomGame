using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemFilterClickTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("Open Offset (Screen Pixels)")]
    public Vector2 screenOffset = new Vector2(0, -40f);

    private Collider2D _col;
    private bool _blockedByErase;

    private void Awake()
    {
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

    public void OnPointerClick(PointerEventData e)
    {
        if (_blockedByErase) return;
        var panel = ItemFilter.Instance;
        if (panel == null) return;
        panel.OpenAtScreen(e.position, new Vector2(16,16), 8);
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