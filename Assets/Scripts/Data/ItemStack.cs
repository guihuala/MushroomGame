using System;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    public ItemDef item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;
    public int CanTake(int want) => Mathf.Min(want, amount);
    public int CanGive(int want)
    {
        if (item == null) return 0;
        return Mathf.Clamp(want, 0, (int)item.stackLimit);
    }
}