using System.Collections.Generic;
using UnityEngine;

public struct ItemPayload
{
    public ItemDef item;         // 物品类型
    public int amount;           // 物品数量
    public Vector3 worldPos;     // 物品在世界中的位置
    
    public bool IsValid => item != null;   // 只要内部引用字段不为空，就认为有效
    public static readonly ItemPayload Empty = new ItemPayload();
}