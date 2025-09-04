using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Building, ITickable, IItemPort, IOrientable
{
    public Vector2Int inDir = Vector2Int.left;    // 输入方向
    public Vector2Int outDir = Vector2Int.right;  // 输出方向
    private readonly Queue<ItemPayload> _buffer = new();  // 物品缓冲区
    private const int BUFFER_CAPACITY = 3;
    
    private static Vector2Int RotateCW(Vector2Int d) => new(d.y, -d.x);
    private static Vector2Int RotateCCW(Vector2Int d) => new(-d.y, d.x);
    private static bool IsOpposite(Vector2Int a, Vector2Int b) => a + b == Vector2Int.zero;
    
    public bool CanPull => _buffer.Count < BUFFER_CAPACITY; // 能从上游拉取
    public bool CanPush => _buffer.Count > 0;      // 能推送到下游
    
    private List<Vector3> conveyorPath = new List<Vector3>();  // 用于存储传送带路径上的所有格子
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);

        // 先以当前 outDir 估个值，再尝试自动路由到最近下游
        BuildLocalPath();
        AutoRoute();
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

        // todo.在相邻格建筑变化的事件里触发
        AutoRoute();

        var payload = _buffer.Peek();
        
        if (payload.item != null)
        {
            // 入口位置：靠近入边
            Vector3 startPos = grid.CellToWorld(cell) - new Vector3(inDir.x, inDir.y, 0f) * (grid.cellSize * 0.5f);
            var iconObject = ItemIconManager.Instance.CreateItemIcon(payload.item, startPos);
            var flowAnimation = iconObject.AddComponent<ItemFlowAnimation>();
            flowAnimation.Init(startPos, conveyorPath);
        }

        var targetCell = cell + outDir;
        var outPort = grid.GetPortAt(targetCell);
        if (outPort != null && outPort.CanPull)
        {
            var pkg = _buffer.Peek();
            if (outPort.TryPull(ref pkg))
            {
                TryPush(pkg);
                DebugManager.Log($"Conveyor {cell} -> {targetCell}", this);
            }
        }
    }
    
    private void AutoRoute()
    {
        // 候选方向：优先直行，其次右转，再次左转；不允许掉头
        var forward = outDir;
        var right = RotateCW(outDir);
        var left  = RotateCCW(outDir);
        var candidates = new[] { forward, right, left };

        foreach (var d in candidates)
        {
            if (IsOpposite(d, inDir)) continue; // 禁止回头

            var targetCell = cell + d;
            var port = grid.GetPortAt(targetCell);
            if (port != null && port.CanPull)
            {
                outDir = d;
                transform.right = new Vector3(outDir.x, outDir.y, 0f);
                BuildLocalPath(); // 方向变化时重建本格路径
                return;
            }
        }

        // 传送带的尽头连接到地面时，触发弹出面板事件
        var targetCellAtEnd = cell + outDir;
        if (grid.IsFree(targetCellAtEnd, checkMushrooms: true))
        {
            // 发送消息以触发面板显示
            MsgCenter.SendMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, targetCellAtEnd);
        }

        // 没找到的话仍重建路径
        BuildLocalPath();
    }


    
    private void BuildLocalPath()
    {
        conveyorPath.Clear();

        Vector3 center = grid.CellToWorld(cell);
        // 让路径在“本格内部”流动（入边到出边）。edgeOffset 控制离格中心的偏移。
        float half = grid.cellSize * 0.5f;
        float edgeOffset = half; // 走到边缘

        // 入边世界坐标（从 inDir 进来，点在格子边缘）
        Vector3 inPoint  = center - new Vector3(inDir.x, inDir.y, 0f) * edgeOffset;
        // 出边世界坐标（朝 outDir 离开，点在另一条边缘）
        Vector3 outPoint = center + new Vector3(outDir.x, outDir.y, 0f) * edgeOffset;

        // 若直行：简单两点；若拐弯：加一个“角点”（格子中心附近的小转折）
        conveyorPath.Add(inPoint);

        if (! (inDir + outDir == Vector2Int.zero) && ! (inDir == -outDir))
        {
            // L 形：添加中心点做转折
            conveyorPath.Add(center);
        }

        conveyorPath.Add(outPoint);
    }
    
    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = -dir;
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }
}