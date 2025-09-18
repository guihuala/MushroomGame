using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ItemFilter : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("列表容器：用于显示可选物品按钮（必须是一个 RectTransform）")]
    public Transform itemListContainer;

    [Header("Outside Click")]
    [Tooltip("点击空白处关闭的全屏遮罩（可留空，运行时自动创建）")]
    public Image outsideClickMask;
    
    public GameObject itemButtonPrefab;

    private List<ItemDef> allItems;      // 所有物品（从你的库存系统取）
    private ItemDef selectedItem;        // 当前选中
    private CanvasGroup cg;              // 显隐控制

    // ===== Singleton =====
    public static ItemFilter Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);
        
        if (!outsideClickMask)
        {
            var go = new GameObject("OutsideClickMask", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform.parent ? transform.parent : transform, false);
            outsideClickMask = go.GetComponent<Image>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            outsideClickMask.color = new Color(0, 0, 0, 0f); // 透明遮罩
            go.transform.SetSiblingIndex(transform.GetSiblingIndex()); // 放在面板之下
            go.GetComponent<Button>().onClick.AddListener(Close);
            go.SetActive(false); // 默认不启用
        }
        
        if (allItems == null)
        {
            var stacks = InventoryManager.Instance.GetAllItemsAsStacks(true);
            allItems = stacks.ConvertAll(s => s.item);
        }
    }
    
    public void Close() => SetVisible(false);
    
    private void SetVisible(bool show)
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        cg.alpha = show ? 1f : 0f;
        cg.interactable = show;
        cg.blocksRaycasts = show;
        if (outsideClickMask) outsideClickMask.gameObject.SetActive(show);
        gameObject.SetActive(true);
    }

    private void BuildListUIIfNeeded()
    {
        if (!itemListContainer) return;
        
        for (int i = itemListContainer.childCount - 1; i >= 0; i--)
            Destroy(itemListContainer.GetChild(i).gameObject);

        if (allItems == null) return;

        // 创建物品按钮
        foreach (var item in allItems)
        {
            var go = Instantiate(itemButtonPrefab, itemListContainer);
            
            var img = go.GetComponent<Image>();
            if (img) img.sprite = item.icon;

            var btn = go.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => OnItemSelected(item));
        }
    }

    private void OnItemSelected(ItemDef item)
    {
        if (selectedItem != null)
            DeselectItem();
        
        selectedItem = item;
        HighlightSelectedItem(item);
        
        FilterItems(selectedItem);

        Close();
    }

    private void HighlightSelectedItem(ItemDef item)
    {
        var t = itemListContainer.Find(item.itemId);
        if (t)
        {
            var img = t.GetComponent<Image>();
            if (img) img.color = Color.yellow;
        }
    }

    private void DeselectItem()
    {
        var t = itemListContainer.Find(selectedItem.itemId);
        if (t)
        {
            var img = t.GetComponent<Image>();
            if (img) img.color = Color.white;
        }
    }

    private void FilterItems(ItemDef selected)
    {
        var filter = FindObjectOfType<Filter>();
        if (filter != null) filter.allowedItem = selected;
    }
    
    public void OpenAtScreen(Vector2 screenPos, Vector2 cursorPadding, float edgePadding)
    {
        var canvas = GetComponentInParent<Canvas>();
        var rt = (RectTransform)transform;
        
        SetVisible(true);
        
        BuildListUIIfNeeded();

        UIPosUtil.PlacePanelAtScreenPoint(rt, canvas, screenPos, cursorPadding, edgePadding, smartFlip: true);
    }
}
