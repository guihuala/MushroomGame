using System.Collections.Generic;
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
    
    public bool CanProvide => false;  // Hub 只吃不出
    public bool CanReceive => true;

    public bool TryReceive(in ItemPayload payload)
    {
        return _hub.ReceiveItem(payload);
    }

    public bool TryProvide(ref ItemPayload payload) => false;
}
