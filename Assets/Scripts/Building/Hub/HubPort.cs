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

    public bool CanProvide => true;
    public bool CanReceive => false;

    public bool TryReceive(in ItemPayload payload)
    {
        return _hub.ReceiveItem(payload); // 将物品接收进hub
    }

    public bool TryProvide(ref ItemPayload payload)
    {
        return false;
    }
}
