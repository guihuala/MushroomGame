using System.Collections.Generic;
using UnityEngine;

public class Conveyor : Building, ITickable, IItemPort, IOrientable
{
    public Vector2Int inDir = Vector2Int.left;    // 输入方向（固定）
    public Vector2Int outDir = Vector2Int.right;  // 输出方向（自动调整）
    private readonly Queue<ItemPayload> _buffer = new();
    private const int BUFFER_CAPACITY = 3;
    
    // 缓存已经建立的连接，避免频繁重新路由
    private IItemPort _connectedOutputPort = null;
    private Vector2Int _connectedDirection = Vector2Int.zero;
    
    private static Vector2Int RotateCW(Vector2Int d) => new(d.y, -d.x);
    private static Vector2Int RotateCCW(Vector2Int d) => new(-d.y, d.x);
    private static bool IsOpposite(Vector2Int a, Vector2Int b) => a + b == Vector2Int.zero;
    
    public bool CanPull => _buffer.Count < BUFFER_CAPACITY;
    public bool CanPush => _buffer.Count > 0;
    
    private List<Vector3> conveyorPath = new List<Vector3>();
    
    #region 摆放和移除

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
        
        MsgCenter.RegisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        
        BuildLocalPath();
        FindBestOutputConnection();
    }
    
    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        
        MsgCenter.UnregisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        
        _connectedOutputPort = null;
        base.OnRemoved();
    }    

    #endregion

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

        // 检查当前连接是否仍然有效
        if (!IsCurrentConnectionValid())
        {
            FindBestOutputConnection(); // 连接失效，重新寻找
        }

        var payload = _buffer.Peek();
        
        if (payload.item != null)
        {
            Vector3 startPos = grid.CellToWorld(cell) - new Vector3(inDir.x, inDir.y, 0f) * (grid.cellSize * 0.5f);
            var iconObject = ItemIconManager.Instance.CreateItemIcon(payload.item, startPos);
            var flowAnimation = iconObject.AddComponent<ItemFlowAnimation>();
            flowAnimation.Init(startPos, conveyorPath);
        }

        // 尝试推送到当前连接的下游
        if (_connectedOutputPort != null && _connectedOutputPort.CanPull)
        {
            var pkg = _buffer.Peek();
            if (_connectedOutputPort.TryPull(ref pkg))
            {
                TryPush(pkg);
                DebugManager.Log($"Conveyor {cell} -> {cell + _connectedDirection}", this);
            }
        }
    }
    
    /// <summary>
    /// 检查当前连接是否仍然有效
    /// </summary>
    private bool IsCurrentConnectionValid()
    {
        if (_connectedOutputPort == null) return false;
        
        var targetCell = cell + _connectedDirection;
        var currentPort = grid.GetPortAt(targetCell);
        
        // 检查端口是否仍然是同一个，并且方向匹配
        return ReferenceEquals(currentPort, _connectedOutputPort) && 
               IsDirectionCompatible(_connectedDirection, currentPort);
    }
    
    /// <summary>
    /// 寻找最佳输出连接
    /// </summary>
    private void FindBestOutputConnection()
    {
        _connectedOutputPort = null;
        _connectedDirection = Vector2Int.zero;
        
        // 候选方向：直线、左侧、右侧（按优先级顺序）
        var forward = outDir;
        var right = RotateCW(outDir);
        var left = RotateCCW(outDir);
        var candidates = new[] { forward, right, left };

        foreach (var direction in candidates)
        {
            if (IsOpposite(direction, inDir)) continue; // 禁止回头
            
            var targetCell = cell + direction;
            var port = grid.GetPortAt(targetCell);
            
            if (port != null && port.CanPull && IsDirectionCompatible(direction, port))
            {
                // 找到合适的连接
                outDir = direction;
                _connectedDirection = direction;
                _connectedOutputPort = port;
                transform.right = new Vector3(outDir.x, outDir.y, 0f);
                BuildLocalPath();
                DebugManager.Log($"Conveyor {cell} connected to {targetCell} direction {direction}", this);
                return;
            }
        }

        // 没有找到下游连接，检查是否接触到地表
        var fallbackCell = cell + outDir;
        if (grid.IsTouchingSurface(fallbackCell))
        {
            MsgCenter.SendMsg(MsgConst.MSG_SHOW_MUSHROOM_PANEL, fallbackCell);
        }

        BuildLocalPath();
    }
    
    /// <summary>
    /// 检查方向是否与下游建筑兼容
    /// </summary>
    private bool IsDirectionCompatible(Vector2Int direction, IItemPort downstreamPort)
    {
        // 如果是另一个传送带，检查入口方向是否匹配
        if (downstreamPort is Conveyor downstreamConveyor)
        {
            // 当前出口方向应该是下游传送带的入口方向的相反方向
            return direction == -downstreamConveyor.inDir;
        }
        
        // 对于其他类型的建筑，默认兼容（可能需要根据具体建筑类型调整）
        return true;
    }
    
    private void BuildLocalPath()
    {
        conveyorPath.Clear();

        Vector3 center = grid.CellToWorld(cell);
        float half = grid.cellSize * 0.5f;
        float edgeOffset = half;

        Vector3 inPoint = center - new Vector3(inDir.x, inDir.y, 0f) * edgeOffset;
        Vector3 outPoint = center + new Vector3(outDir.x, outDir.y, 0f) * edgeOffset;

        conveyorPath.Add(inPoint);

        if (!(inDir + outDir == Vector2Int.zero) && !(inDir == -outDir))
        {
            conveyorPath.Add(center);
        }

        conveyorPath.Add(outPoint);
    }
    
    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = -dir;
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
        FindBestOutputConnection(); // 手动设置方向后重新寻找连接
    }
    
    /// <summary>
    /// 处理邻居变化消息
    /// </summary>
    private void OnNeighborChangedMsg(params object[] args)
    {
        if (args.Length > 0 && args[0] is Vector2Int changedCell)
        {
            // 检查变化的单元格是否是我们的邻居
            Vector2Int[] directions = { 
                Vector2Int.up, Vector2Int.right, 
                Vector2Int.down, Vector2Int.left 
            };
            
            foreach (var dir in directions)
            {
                if (cell + dir == changedCell)
                {
                    OnNeighborChanged();
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// 当邻居发生变化时重新评估连接
    /// </summary>
    public override void OnNeighborChanged()
    {
        FindBestOutputConnection();
        DebugManager.Log($"Conveyor at {cell} detected neighbor change, re-evaluating connections", this);
    }
}