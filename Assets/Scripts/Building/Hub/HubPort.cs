using UnityEngine;

public class HubPort : IItemPort
{
    public Vector2Int Cell { get; private set; }
    private readonly Hub _hub;

    public HubPort(Vector2Int cell, Hub hub)
    {
        Cell = cell;
        _hub = hub;
    }

    public bool CanPull => true; // 不支持拉取
    public bool CanPush => false;  // 只能接收

    public bool TryPull(ref ItemPayload payload)
    {
        return _hub.ReceiveItem(payload); // 将物品接收进hub
    }

    public bool TryPush(in ItemPayload payload)
    {
        return false; // 将物品接收进hub
    }
}
