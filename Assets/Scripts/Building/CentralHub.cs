using UnityEngine;
using System.Collections.Generic;

public class CentralHub : Building, IItemPort
{
    private readonly Dictionary<ItemDef, int> _received = new();
    public System.Action<ItemDef, int> OnDelivered; // UI订阅

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
    }

    public override void OnRemoved()
    {
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }

    public bool TryPull(ref ItemPayload payload)
    {
        throw new System.NotImplementedException();
    }

    public bool TryPush(in ItemPayload payload)
    {
        if (payload.item == null || payload.amount <= 0) return false;
        if (!_received.ContainsKey(payload.item)) _received[payload.item] = 0;
        _received[payload.item] += payload.amount;
        OnDelivered?.Invoke(payload.item, _received[payload.item]);
        return true;
    }

    public bool CanPull { get; }

    public bool CanPush => true; // 永远能收

    public int GetDelivered(ItemDef item) =>
        _received.TryGetValue(item, out var n) ? n : 0;
}