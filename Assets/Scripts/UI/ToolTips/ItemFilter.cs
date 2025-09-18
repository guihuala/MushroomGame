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
        DontDestroyOnLoad(gameObject);

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();

        // 默认隐藏
        SetVisible(false);

        // 外部点击遮罩
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

        // 物品列表只取一次（也可以在 Open 时刷新）
        if (allItems == null)
        {
            var stacks = InventoryManager.Instance.GetAllItemsAsStacks(true);
            allItems = stacks.ConvertAll(s => s.item);
        }
    }

    // === Public API ===

    /// <summary>在世界坐标附近打开并定位（触发器调用）。</summary>
    public void OpenAtWorld(Vector3 worldPos, Vector2 screenOffset)
    {
        BuildListUIIfNeeded();

        // 面板显隐
        SetVisible(true);

        // 将世界坐标转到屏幕/本地 UI 坐标
        var canvas = GetComponentInParent<Canvas>();
        var panelRT = transform as RectTransform;

        Vector2 screen;
        var cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? Camera.main : null;
        screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        screen += screenOffset;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRT.parent as RectTransform, screen, cam, out local);
        panelRT.anchoredPosition = local;
    }
    
    public void Close() => SetVisible(false);

    // === Internal ===

    private void SetVisible(bool show)
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        cg.alpha = show ? 1f : 0f;
        cg.interactable = show;
        cg.blocksRaycasts = show;
        if (outsideClickMask) outsideClickMask.gameObject.SetActive(show);
        gameObject.SetActive(true); // 常驻但可交互开关由 CanvasGroup 控制
    }

    private void BuildListUIIfNeeded()
    {
        if (!itemListContainer) return;

        // 清空旧项
        for (int i = itemListContainer.childCount - 1; i >= 0; i--)
            Destroy(itemListContainer.GetChild(i).gameObject);

        if (allItems == null) return;

        // 创建物品按钮
        foreach (var item in allItems)
        {
            var go = new GameObject(item.itemId, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(itemListContainer, false);

            var img = go.GetComponent<Image>();
            img.sprite = item.icon;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => OnItemSelected(item));
        }
    }

    private void OnItemSelected(ItemDef item)
    {
        // 取消旧高亮
        if (selectedItem != null)
            DeselectItem();

        // 选择新物品
        selectedItem = item;
        HighlightSelectedItem(item);

        // 更新过滤器（你的游戏逻辑）
        FilterItems(selectedItem);

        // 关闭
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
}
