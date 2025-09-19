using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ItemFilterPanel : MonoBehaviour
{
    [Header("UI Components")]
    public Transform itemListContainer;

    [Header("Outside Click")]
    [Tooltip("点击空白处关闭的全屏遮罩（可留空，运行时自动创建）")]
    public Image outsideClickMask;
    
    [Header("Selection Frame (Child)")]
    [Tooltip("按钮预制件里边框子物体的名称")]
    public string frameChildName = "Frame";

    private readonly Dictionary<ItemDef, GameObject> _frameByItem = new();
    private readonly Dictionary<ItemDef, Image> _btnImageByItem = new();

    public GameObject itemButtonPrefab;

    private List<ItemDef> allItems;
    private ItemDef selectedItem;
    private CanvasGroup cg;

    private Filter _contextFilter;
    private ItemDef _selectedItem;  // 当前唯一选中

    public static ItemFilterPanel Instance { get; private set; }

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
            outsideClickMask.color = new Color(0, 0, 0, 0f);
            go.transform.SetSiblingIndex(transform.GetSiblingIndex());
            go.GetComponent<Button>().onClick.AddListener(Close);
            go.SetActive(false);
        }
    }

    private void Start()
    {
        FindAllItems();
    }

    public void Close() => SetVisible(false);

    public void OpenForFilter(Filter filter, Vector2 screenPos, Vector2 cursorPadding, float edgePadding)
    {
        _contextFilter = filter;
        _selectedItem = _contextFilter ? _contextFilter.allowedItem : null;

        OpenAtScreen(screenPos, cursorPadding, edgePadding);
    }

    private void SetVisible(bool show)  
    {
        FindAllItems();
        
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

        // 清空
        for (int i = itemListContainer.childCount - 1; i >= 0; i--)
            Destroy(itemListContainer.GetChild(i).gameObject);
        _btnImageByItem.Clear();
        _frameByItem.Clear();

        if (allItems == null) return;

        foreach (var item in allItems)
        {
            var go = Instantiate(itemButtonPrefab, itemListContainer);
            go.name = item.itemId;

            // 主图标
            var img = go.GetComponent<Image>();
            if (img) { img.sprite = item.icon; _btnImageByItem[item] = img; }

            // 找到边框子物体（默认隐藏）
            var frameGO = FindFrameChild(go.transform, frameChildName);
            if (frameGO != null)
            {
                frameGO.SetActive(false);
                _frameByItem[item] = frameGO;
            }

            // 点击回调
            var btn = go.GetComponent<Button>();
            if (btn)
            {
                var captured = item;
                btn.onClick.AddListener(() => OnItemSelected(captured));
            }
        }

        _selectedItem = _contextFilter ? _contextFilter.allowedItem : null;
        UpdateHighlightVisuals();
    }

    public void OpenAtScreen(Vector2 screenPos, Vector2 cursorPadding, float edgePadding)
    {
        var canvas = GetComponentInParent<Canvas>();
        var rt = (RectTransform)transform;

        SetVisible(true);
        BuildListUIIfNeeded();

        UIPosUtil.PlacePanelAtScreenPoint(rt, canvas, screenPos, cursorPadding, edgePadding, smartFlip: true);
    }

    private void OnItemSelected(ItemDef item)
    {
        _selectedItem = item;
        UpdateHighlightVisuals();

        if (_contextFilter != null)
            _contextFilter.allowedItem = item;

        Close();
    }

    private void UpdateHighlightVisuals()
    {
        // 只有选中项的边框启用，其它全部关闭
        foreach (var kv in _frameByItem)
        {
            var item = kv.Key;
            var frameGO = kv.Value;
            if (!frameGO) continue;
            frameGO.SetActive(item == _selectedItem);
        }
    }
    
    private GameObject FindFrameChild(Transform root, string childName)
    {
        var t = root.Find(childName);
        if (t != null) return t.gameObject;

        // 递归查找
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindFrameChild(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }

    private void FindAllItems()
    {
        if (allItems == null)
        {
            var stacks = InventoryManager.Instance.GetAllItemsAsStacks(true);
            allItems = stacks.ConvertAll(s => s.item);
        }
    }
}
