using UnityEngine;
using System.Collections.Generic;

public class MultiGridBuilding : Building
{
    protected List<ItemPayload> storage = new();  // 存储物品的容器

    // 物品接收方法
    public virtual bool ReceiveItem(in ItemPayload payload)
    {
        // 可以扩展逻辑，比如存储物品的数量限制等
        storage.Add(payload);
        return true;
    }

    // 物品输出方法
    public virtual bool ProvideItem(ref ItemPayload payload)
    {
        // 如果有物品可以提供，提供第一个物品
        if (storage.Count > 0)
        {
            payload = storage[0];  // 获取第一个物品
            storage.RemoveAt(0);    // 移除该物品
            return true;
        }
        return false;
    }
}