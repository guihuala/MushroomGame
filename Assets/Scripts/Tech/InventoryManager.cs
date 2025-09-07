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

public class InventoryManager : Singleton<InventoryManager>,IManager
{
    [Header("Storage Settings")]
    public int maxStorage = 99999;

    private Dictionary<ItemDef, int> _inventory = new Dictionary<ItemDef, int>();
    
    // 初始化方法
    public void Initialize()
    {
        DebugManager.Log("InventoryManager initialized");
        // 清空库存并重置
        ClearInventory();
    }
    
    // 添加物品到库存
    public bool AddItem(ItemDef item, int amount)
    {
        if (item == null || amount <= 0) return false;
        
        if (GetTotalItemCount() + amount > maxStorage)
        {
            DebugManager.LogWarning("Inventory storage full!", this);
            return false;
        }

        if (_inventory.ContainsKey(item))
        {
            _inventory[item] += amount;
        }
        else
        {
            _inventory[item] = amount;
        }

        // 发送消息到消息中心
        MsgCenter.SendMsg(MsgConst.INVENTORY_ITEM_ADDED, item, amount);
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED);
        
        return true;
    }

    // 添加 ItemStack
    public bool AddItemStack(ItemStack itemStack)
    {
        return AddItem(itemStack.item, itemStack.amount);
    }

    // 从库存移除物品
    public bool RemoveItem(ItemDef item, int amount)
    {
        if (item == null || amount <= 0) return false;
        
        if (!_inventory.ContainsKey(item) || _inventory[item] < amount)
        {
            return false;
        }

        _inventory[item] -= amount;
        if (_inventory[item] <= 0)
        {
            _inventory.Remove(item);
        }

        // 发送消息到消息中心
        MsgCenter.SendMsg(MsgConst.INVENTORY_ITEM_REMOVED, item, amount);
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED);
        
        return true;
    }

    // 移除 ItemStack
    public bool RemoveItemStack(ItemStack itemStack)
    {
        return RemoveItem(itemStack.item, itemStack.amount);
    }

    // 获取特定物品数量
    public int GetItemCount(ItemDef item)
    {
        return _inventory.ContainsKey(item) ? _inventory[item] : 0;
    }

    // 获取 ItemStack
    public ItemStack GetItemStack(ItemDef item)
    {
        return new ItemStack
        {
            item = item,
            amount = GetItemCount(item)
        };
    }

    // 获取所有物品总数量
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var count in _inventory.Values)
        {
            total += count;
        }
        return total;
    }

    // 检查是否有足够物品
    public bool HasEnoughItem(ItemDef item, int requiredAmount)
    {
        return GetItemCount(item) >= requiredAmount;
    }

    // 检查是否有足够的 ItemStack
    public bool HasEnoughItemStack(ItemStack requiredStack)
    {
        return HasEnoughItem(requiredStack.item, requiredStack.amount);
    }

    // 获取所有物品的 ItemStack 列表
    public List<ItemStack> GetAllItemStacks()
    {
        var items = new List<ItemStack>();
        foreach (var kvp in _inventory)
        {
            items.Add(new ItemStack { item = kvp.Key, amount = kvp.Value });
        }
        return items;
    }

    // 清空库存
    public void ClearInventory()
    {
        _inventory.Clear();
        MsgCenter.SendMsgAct(MsgConst.INVENTORY_CHANGED);
    }
    
    // 尝试从库存中取出指定数量的物品
    public bool TryTakeItems(ItemDef item, int amount, out ItemStack takenStack)
    {
        takenStack = new ItemStack { item = item, amount = 0 };
        
        if (!HasEnoughItem(item, amount))
            return false;

        if (RemoveItem(item, amount))
        {
            takenStack.amount = amount;
            return true;
        }
        
        return false;
    }

    // 尝试取出 ItemStack
    public bool TryTakeItemStack(ItemStack requiredStack, out ItemStack takenStack)
    {
        return TryTakeItems(requiredStack.item, requiredStack.amount, out takenStack);
    }
}