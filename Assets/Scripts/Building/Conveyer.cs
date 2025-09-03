using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Building, ITickable, IItemPort, IOrientable
{
    public Vector2Int inDir = Vector2Int.left;
    public Vector2Int outDir = Vector2Int.right;
    
    private readonly Queue<ItemPayload> _buffer = new();
    private const int BUFFER_CAPACITY = 3;

    public bool CanPull => _buffer.Count < BUFFER_CAPACITY; // 能从上游拉取
    public bool CanPush => _buffer.Count > 0;      // 能推送到下游

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }
    
    public bool TryPull(ref ItemPayload payload)
    {
        if (_buffer.Count >= BUFFER_CAPACITY) return false;
        _buffer.Enqueue(payload);
        return true;
    }
    
    public bool TryPush(in ItemPayload payload)
    {
        if (_buffer.Count == 0) return false;
        _buffer.Dequeue();
        return true;
    }
    
    public void Tick(float dt)
    {
        if (_buffer.Count == 0) return;

        var targetCell = cell + outDir;
        var outPort = grid.GetPortAt(targetCell);
        
        if (outPort != null && outPort.CanPull)
        {
            var pkg = _buffer.Peek();
            
            if (outPort.TryPull(ref pkg)) // 如果目标端口可以拉取并接收物品
            {
                TryPush(pkg); // 自己推出
                DebugManager.Log($"Conveyor at {cell} pushing item {pkg.item?.name} ({pkg.amount}) to {targetCell}", this);
            }
        }
    }
    
    public void SetDirection(Vector2Int input)
    {
        inDir = input;
        outDir = cell - inDir;  // 反向计算输出方向
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }
}
