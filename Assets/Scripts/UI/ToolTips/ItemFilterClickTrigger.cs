using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemFilterClickTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("Open Offset (Screen Pixels)")]
    public Vector2 screenOffset = new Vector2(0, -40f);

    private void Awake()
    {
        EnsureCollider();
    }

    public void OnPointerClick(PointerEventData e)
    {
        var panel = ItemFilter.Instance;
        if (panel == null) return;
        panel.OpenAtScreen(e.position, new Vector2(16,16), 8);
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