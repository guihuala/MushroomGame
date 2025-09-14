using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemFilterClickTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Prefab")]
    public GameObject itemSelectionPanelPrefab; // 物品选择框的预制件

    private GameObject itemSelectionPanelInstance; // 实例化后的物品选择框

    private void Awake()
    {
        EnsureCollider();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ItemFilter.GetInstance() != null) return;
        
        itemSelectionPanelInstance = Instantiate(itemSelectionPanelPrefab);
        itemSelectionPanelInstance.SetActive(true);
        
        itemSelectionPanelInstance.transform.position = transform.position + new Vector3(0, -20f, 0);
        
        var itemFilter = itemSelectionPanelInstance.GetComponent<ItemFilter>();
        itemFilter.InitializeFilter();
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