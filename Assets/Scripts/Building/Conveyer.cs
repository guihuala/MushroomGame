using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Building, ITickable, IItemPort, IOrientable
{
    public Vector2Int inDir = Vector2Int.left;    // 输入方向
    public Vector2Int outDir = Vector2Int.right;  // 输出方向
    private readonly Queue<ItemPayload> _buffer = new();  // 物品缓冲区
    private const int BUFFER_CAPACITY = 3;
    
    public bool CanPull => _buffer.Count < BUFFER_CAPACITY; // 能从上游拉取
    public bool CanPush => _buffer.Count > 0;      // 能推送到下游
    
    private List<Vector3> conveyorPath = new List<Vector3>();  // 用于存储传送带路径上的所有格子

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
        
        // 计算传送带路径
        CalculateConveyorPath();
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

        var payload = _buffer.Peek();  // 取出当前物品

        // 创建物品图标并开始流动
        if (payload.item != null)
        {
            Vector3 startPos = payload.worldPos + new Vector3(inDir.x, inDir.y, 0f);
            
            var iconObject = ItemIconManager.Instance.CreateItemIcon(payload.item, startPos);
            var flowAnimation = iconObject.AddComponent<ItemFlowAnimation>();
            
            flowAnimation.Init(startPos, conveyorPath); // 设置物品的流动路径
        }

        // 物品开始流动，依次推送到下游
        var targetCell = cell + outDir;
        var outPort = grid.GetPortAt(targetCell);

        if (outPort != null && outPort.CanPull)
        {
            var pkg = _buffer.Peek();
            if (outPort.TryPull(ref pkg))
            {
                TryPush(pkg);  // 推送物品
                DebugManager.Log($"Conveyor at {cell} pushing item {pkg.item?.name} ({pkg.amount}) to {targetCell}", this);
            }
        }
    }

    private void CalculateConveyorPath()
    {
        Vector3 startPos = grid.CellToWorld(cell);
        Vector3 endPos = startPos;

        // 假设传送带路径为一条线，沿着传送带的格子顺序构建路径
        conveyorPath.Clear();
        conveyorPath.Add(startPos); // 起始点

        for (int i = 0; i < 10; i++)  // todo.这边目前是硬编码
        {
            endPos += new Vector3(outDir.x, outDir.y, 0); // 沿着传送带方向计算下一个格子的位置
            conveyorPath.Add(endPos);
        }
    }

    public void SetDirection(Vector2Int input)
    {
        inDir = input;
        outDir = cell - inDir; // 反向计算输出方向
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }
}