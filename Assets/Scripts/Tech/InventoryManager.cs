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
    
    public string itemDefResourcesPath;
    
    private readonly Dictionary<ItemDef, int> _inventory = new Dictionary<ItemDef, int>();
    
    private readonly List<ItemDef> _catalog = new List<ItemDef>();
    
    public void Initialize()
    {
        BuildCatalogFromResources(itemDefResourcesPath);
        ClearInventory();
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
        
        var seen = new HashSet<ItemDef>();
        foreach (var def in items)
        {
            if (def == null || seen.Contains(def)) continue;
            _catalog.Add(def);
            seen.Add(def);
        }
    }
    
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
}
