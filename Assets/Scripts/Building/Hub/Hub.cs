using System;
using System.Collections.Generic;
using UnityEngine;

public class Hub : MonoBehaviour
{
    public TileGridService grid;
    
    private Vector2Int centerCell;
    private readonly List<HubPort> _ports = new();
    private readonly List<ItemPayload> _storage = new();

    [Header("Storage")]
    public int maxStorage = 999;
    
    public event Action<ItemPayload> OnItemReceived;

    void Start()
    {
        grid = FindObjectOfType<TileGridService>();
        centerCell = grid.WorldToCell(transform.position);

        RegisterInitialPorts();
    }
    
    private void RegisterInitialPorts()
    {
        AddPort(Vector2Int.zero);
        AddPort(Vector2Int.left);
        AddPort(Vector2Int.right);
    }
    
    private void OnDestroy()
    {
        foreach (var port in _ports)
        {
            grid.UnregisterPort(port.Cell, port);
        }
    }

    public void AddPort(Vector2Int offset)
    {
        var cell = centerCell + offset;
        var port = new HubPort(cell, this);
        _ports.Add(port);
        grid.RegisterPort(cell, port);
    }

    public bool ReceiveItem(in ItemPayload payload)
    {
        if (_storage.Count >= maxStorage)
        {
            DebugManager.LogWarning("Hub storage full!", this);
            return false;
        }

        _storage.Add(payload);
        DebugManager.Log($"Hub received {payload.amount}x {payload.item?.name}", this);
        
        OnItemReceived?.Invoke(payload);

        return true;
    }

    // 查询
    public int GetItemCount(ItemDef item)
    {
        int total = 0;
        foreach (var p in _storage)
        {
            if (p.item == item) total += p.amount;
        }
        return total;
    }
}