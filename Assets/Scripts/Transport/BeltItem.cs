using UnityEngine;


public class BeltItem
{
    public ItemPayload payload;
    public float pos; // 位置

    public BeltItem(ItemPayload payload)
    {
        this.payload = payload;
        this.pos = 0f;
    }
}