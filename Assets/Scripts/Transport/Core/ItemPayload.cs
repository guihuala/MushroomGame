using UnityEngine;

public struct ItemPayload
{
    public ItemDef item;
    public int amount;         // 货物数量
    public Vector3 worldPos;   // 可用于做小物体沿线移动的插值位置
}