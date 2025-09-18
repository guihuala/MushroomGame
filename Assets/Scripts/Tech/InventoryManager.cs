using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    public ItemDef item;
    public int amount;

    public int CanTake(int want) => Mathf.Min(want, amount);
    public int CanGive(int want)
    {
        if (item == null) return 0;
        return Mathf.Clamp(want, 0, (int)item.stackLimit);
    }
}

public class InventoryManager : Singleton<InventoryManager>, IManager
{
    [Header("Storage Settings")]
    public int maxStorage = 99999;

    [Header("Catalog Settings")]
    [Tooltip("从 Resources 下的这个相对路径读取全部 ItemDef，例如：\"Items\" 或 \"Data/Items\"")]
    public string itemDefResourcesPath = "Items";

    // 运行期“库存数量”映射
    private readonly Dictionary<ItemDef, int> _inventory = new Dictionary<ItemDef, int>();

    // 运行期“全部物品目录”（不关心库存数量）
    private readonly List<ItemDef> _catalog = new List<ItemDef>();
    public IReadOnlyList<ItemDef> AllItemsCatalog => _catalog;

    // ========== 生命周期 ==========
    public void Initialize()
    {
        BuildCatalogFromResources(itemDefResourcesPath); // 先搭好“全部物品”目录
        ClearInventory();                                 // 清库存（按你原来的逻辑）
    }
    
    public void BuildCatalogFromResources(string resourcesPath)
    {
        _catalog.Clear();

        if (string.IsNullOrEmpty(resourcesPath))
        {
            Debug.LogWarning("[InventoryManager] itemDefResourcesPath 为空，跳过目录构建。");
            return;
        }

        var items = Resources.LoadAll<ItemDef>(resourcesPath);
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning($"[InventoryManager] 未在 Resources/{resourcesPath} 下找到任何 ItemDef。");
            return;
        }

        // 去重 + 保持稳定顺序
        var seen = new HashSet<ItemDef>();
        foreach (var def in items)
        {
            if (def == null || seen.Contains(def)) continue;
            _catalog.Add(def);
            seen.Add(def);
        }

        // （可选）按名称排序：_catalog.Sort((a,b)=>string.Compare(a.name,b.name,StringComparison.Ordinal));
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED); // 如果你有 UI 监听这个事件，可复用
    }

    // ========== 库存操作 ==========
    public bool AddItem(ItemDef item, int amount)
    {
        if (item == null || amount <= 0) return false;

        if (GetTotalItemCount() + amount > maxStorage)
        {
            DebugManager.LogWarning("Inventory storage full!", this);
            return false;
        }

        if (_inventory.ContainsKey(item)) _inventory[item] += amount;
        else _inventory[item] = amount;

        MsgCenter.SendMsg(MsgConst.INVENTORY_ITEM_ADDED, item, amount);
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED);
        return true;
    }

    public bool AddItemStack(ItemStack itemStack) => AddItem(itemStack.item, itemStack.amount);

    public bool RemoveItem(ItemDef item, int amount)
    {
        if (item == null || amount <= 0) return false;

        if (!_inventory.ContainsKey(item) || _inventory[item] < amount) return false;

        _inventory[item] -= amount;
        if (_inventory[item] <= 0) _inventory.Remove(item);

        MsgCenter.SendMsg(MsgConst.INVENTORY_ITEM_REMOVED, item, amount);
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED);
        return true;
    }

    public bool RemoveItemStack(ItemStack itemStack) => RemoveItem(itemStack.item, itemStack.amount);

    public int GetItemCount(ItemDef item) => _inventory.TryGetValue(item, out var n) ? n : 0;

    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var count in _inventory.Values) total += count;
        return total;
    }

    public bool HasEnoughItem(ItemDef item, int requiredAmount) => GetItemCount(item) >= requiredAmount;

    public bool HasEnoughItemStack(ItemStack requiredStack) => HasEnoughItem(requiredStack.item, requiredStack.amount);

    /// <summary>
    /// 获取“当前库存里有的物品”转为 ItemStack 列表（只包含库存>0 的物品）
    /// </summary>
    public List<ItemStack> GetAllItemStacks()
    {
        var items = new List<ItemStack>();
        foreach (var kvp in _inventory)
            items.Add(new ItemStack { item = kvp.Key, amount = kvp.Value });
        return items;
    }

    /// <summary>
    /// 基于“全部目录”返回 ItemStack 列表（可选择包含库存为 0 的物品）
    /// </summary>
    public List<ItemStack> GetAllItemsAsStacks(bool includeZeroAmount = true)
    {
        var list = new List<ItemStack>(_catalog.Count);
        foreach (var def in _catalog)
        {
            int amt = GetItemCount(def);
            if (!includeZeroAmount && amt <= 0) continue;
            list.Add(new ItemStack { item = def, amount = amt });
        }
        return list;
    }

    public void ClearInventory()
    {
        _inventory.Clear();
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED);
    }

    public bool TryTakeItems(ItemDef item, int amount, out ItemStack takenStack)
    {
        takenStack = new ItemStack { item = item, amount = 0 };

        if (!HasEnoughItem(item, amount)) return false;

        if (RemoveItem(item, amount))
        {
            takenStack.amount = amount;
            return true;
        }
        return false;
    }
}
