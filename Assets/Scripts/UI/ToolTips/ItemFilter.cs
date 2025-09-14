using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ItemFilter : MonoBehaviour
{
    [Header("UI Components")]
    public Transform itemListContainer;  // 列表容器，用于显示物品

    private List<ItemDef> allItems;      // 所有物品列表
    private ItemDef selectedItem;        // 选中的物品（仅允许一个物品）

    // 静态变量确保只有一个 ItemFilter 实例
    private static ItemFilter instance;

    void Awake()
    {
        // 检查是否已经存在一个 ItemFilter 实例
        if (instance != null)
        {
            Destroy(gameObject);  // 如果已存在，销毁新创建的实例
            return;
        }

        instance = this;  // 设置当前为唯一实例
        DontDestroyOnLoad(gameObject); // 确保物品选择面板在切换场景时不会销毁

        // 获取所有物品
        allItems = InventoryManager.Instance.GetAllItemStacks().ConvertAll(itemStack => itemStack.item);

        // 初始化选中的物品为 null
        selectedItem = null;
    }

    public void InitializeFilter()
    {
        // 显示物品列表
        ShowItemSelectionPanel();

        // 监听外部点击关闭
        CloseOnOutsideClick();
    }

    private void ShowItemSelectionPanel()
    {
        // 清空列表
        foreach (Transform child in itemListContainer)
        {
            Destroy(child.gameObject);
        }

        // 创建物品按钮并添加到列表
        foreach (var item in allItems)
        {
            var itemButton = new GameObject(item.itemId);
            var button = itemButton.AddComponent<Button>();
            itemButton.AddComponent<Image>().sprite = item.icon;  // 设置物品图标

            button.onClick.AddListener(() => OnItemSelected(item));

            itemButton.transform.SetParent(itemListContainer);
        }
    }

    private void OnItemSelected(ItemDef item)
    {
        // 如果已有选择，取消高亮显示
        if (selectedItem != null)
        {
            // 取消高亮显示前一个物品
            DeselectItem();
        }

        // 选择新的物品并高亮显示
        selectedItem = item;
        HighlightSelectedItem(item);

        // 更新过滤器
        FilterItems(selectedItem);

        // 关闭面板
        OnClosePanel();
    }

    private void HighlightSelectedItem(ItemDef item)
    {
        // 在这里你可以添加物品高亮显示的逻辑，例如修改物品图标的颜色或者添加边框
        var itemButton = itemListContainer.Find(item.itemId)?.GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.GetComponent<Image>().color = Color.yellow;  // 高亮显示选中的物品
        }
    }

    private void DeselectItem()
    {
        // 取消物品高亮显示
        var itemButton = itemListContainer.Find(selectedItem.itemId)?.GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.GetComponent<Image>().color = Color.white;  // 恢复原来的颜色
        }
    }

    private void OnClosePanel()
    {
        Destroy(gameObject);  // 销毁当前的面板
    }

    private void FilterItems(ItemDef selectedItem)
    {
        var filter = FindObjectOfType<Filter>();
        if (filter != null)
        {
            filter.allowedItem = selectedItem; // 只允许选中的物品
        }
    }

    private void CloseOnOutsideClick()
    {
        // 监听点击外部关闭 UI
        var background = new GameObject("Background");
        background.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        background.transform.SetParent(transform);

        var button = background.AddComponent<Button>();
        button.onClick.AddListener(OnClosePanel);
    }

    public static ItemFilter GetInstance()
    {
        return instance;
    }
}
