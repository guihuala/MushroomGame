using System.Collections.Generic;
using UnityEngine;

public struct ItemPayload
{
    public ItemDef item;         // 物品类型
    public int amount;           // 物品数量
    public Vector3 worldPos;     // 物品在世界中的位置
}