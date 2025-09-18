using UnityEngine;

public class BeltItem
{
    public ItemPayload payload;
    public float pos;     // [0,1) 该段上的进度
    public int lane;      // -1=左 / 0=直 / +1=右   —— 用于双轨并道
    public readonly long id;

    private static long _nextId;

    public BeltItem(ItemPayload payload)
    {
        this.payload = payload;
        this.pos = 0f;
        this.lane = 0;
        this.id = ++_nextId;
    }
}