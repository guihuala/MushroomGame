using System.Collections.Generic;
using UnityEngine;

public class Conveyer : Building, ITickable, IItemPort, IOrientable
{
    [Header("传送带设置")] public float beltSpeed = 1.0f; // 每秒移动的格子数
    public Vector2Int inDir = Vector2Int.left;
    public Vector2Int outDir = Vector2Int.right;

    [Header("物品间距设置")] public float itemSpacing = 0.3f; // 物品之间的最小间距（0-1之间）
    public float transferThreshold = 0.95f; // 传输阈值

    [Header("容量限制")] public int maxItems = 3;
    
    // 数据存储
    private readonly List<BeltItem> _items = new();
    // 属性
    public IReadOnlyList<BeltItem> Items => _items;
    
    private IItemPort _connectedOutputPort;
    private Vector2Int _connectedDirection;

    // 自动铺路相关
    private int _lastAutoTileFrame = -1;
    private static Vector2Int RotCW(Vector2Int v) => new(v.y, -v.x);
    private static Vector2Int RotCCW(Vector2Int v) => new(-v.y, v.x);
    
    public Vector2Int Direction => outDir;

    public bool CanProvide => _items.Count > 0 && _items[0].pos >= 0.95f;
    public bool CanReceive => _items.Count < maxItems;
    
    public Vector3 GetWorldPosition()
    {
        return grid != null ? grid.CellToWorld(cell) : transform.position;
    }
    

    #region 生命周期
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
 
        MsgCenter.RegisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        MsgCenter.SendMsg(MsgConst.MSG_CONVEYOR_PLACED, this);
        AutoTile();
    }

    public override void OnRemoved()
    {
        MsgCenter.SendMsg(MsgConst.MSG_CONVEYOR_REMOVED, this);
        MsgCenter.UnregisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        
        grid.UnregisterPort(cell, this);
        _connectedOutputPort = null;
        base.OnRemoved();
    }

    #endregion

    #region 邻居处理

    public override void OnNeighborChanged() => AutoTile();

    private void OnNeighborChangedMsg(params object[] args)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (args.Length > 0 && args[0] is Vector2Int changed && (changed - cell).sqrMagnitude == 1)
        {
            OnNeighborChanged();
        }
    }

    #endregion

    #region 方向与自动铺路

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = -dir;
        UpdateVisualDirection();
    }

    public void AutoTile()
    {
        if (_lastAutoTileFrame == Time.frameCount) return;
        _lastAutoTileFrame = Time.frameCount;
        AutoTileSystem.RewireAround(grid, this);
    }

    public void ApplyDirAndRebuild()
    {
        UpdateVisualDirection();
        FindBestOutputConnection();
    }

    private void UpdateVisualDirection() => transform.right = new Vector3(outDir.x, outDir.y, 0f);

    #endregion

    #region 物品传输接口（格中心逻辑）

    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (!CanReceive) return false;

        var payload = payloadIn;
        payload.worldPos = GetWorldPosition();

        _items.Add(new BeltItem(payload) { pos = 0f });
        return true;
    }

    public bool TryProvide(ref ItemPayload payload)
    {
        if (!CanProvide) return false;
        payload = _items[0].payload;
        _items.RemoveAt(0);
        return true;
    }

    #endregion

    #region Tick逻辑

    public void Tick(float dt)
    {
        UpdateItemPositions(dt);
        TryTransferFirstItem();
        ClampItemPositions();
    }

    private void UpdateItemPositions(float dt)
    {
        float moveDistance = dt * beltSpeed;

        bool hasValidOutput = IsCurrentConnectionValid() && _connectedOutputPort != null &&
                              _connectedOutputPort.CanReceive;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];

            // 物品的最大移动限制
            float maxAllowed = i == 0 ? float.MaxValue : _items[i - 1].pos - itemSpacing;

            bool isLast = i == _items.Count - 1;
            if (isLast && !hasValidOutput)
                maxAllowed = Mathf.Min(maxAllowed, 1f - itemSpacing);

            // 平滑的增加位置，避免闪烁
            item.pos = Mathf.Min(item.pos + moveDistance, maxAllowed);

            item.payload.worldPos = GetWorldPosition();
            _items[i] = item;
        }
    }

    private void TryTransferFirstItem()
    {
        if (_items.Count == 0 || _items[0].pos < transferThreshold) return;

        Vector2Int nextCell = cell + outDir;
        var nextPort = grid.GetPortAt(nextCell);

        // ① 带 -> 带（几何 + 容量；不走端口协议）
        if (nextPort is Conveyer nextBelt)
        {
            // 先把头部的目标点设为“下一带的格中心”
            var head = _items[0];
            head.pos = Mathf.Min(1f, head.pos);
            head.payload.worldPos = nextBelt.GetWorldPosition();
            _items[0] = head;

            if (nextBelt.TryAcceptFromNeighbour(this))
            {
                _items.RemoveAt(0);
            }
            else
            {
                // 对方满：把位置限制在出口前，并把目标点回滚为“本格中心”（视觉不提前跳）
                head = _items[0];
                head.pos = Mathf.Min(head.pos, 1f - itemSpacing);
                head.payload.worldPos = GetWorldPosition();
                _items[0] = head;
            }
            return;
        }

        // ② 带 -> 建筑（端口协议）
        ValidateConnection();
        if (_connectedOutputPort == null || !_connectedOutputPort.CanReceive)
        {
            if (_items[0].pos > 1f - itemSpacing)
            {
                var item = _items[0];
                item.pos = 1f - itemSpacing;
                item.payload.worldPos = GetWorldPosition();
                _items[0] = item;
            }
            return;
        }

        var payloadOut = _items[0].payload;
        if (_connectedOutputPort.TryReceive(in payloadOut))
        {
            _items.RemoveAt(0);
        }
    }

    private void ClampItemPositions()
    {
        bool hasValidOutput = IsCurrentConnectionValid() && _connectedOutputPort != null &&
                              _connectedOutputPort.CanReceive;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].pos > 1f)
            {
                var item = _items[i];
                bool isLast = i == _items.Count - 1;
                item.pos = (isLast && !hasValidOutput) ? 0.99f : 1f;
                _items[i] = item;
            }
        }
    }

    #endregion

    // ========= 带->带 接收 =========
    public bool TryAcceptFromNeighbour(Conveyer from)
    {
        if (!HasSpaceForIncoming()) return false;

        var incoming = from._items[0];
        incoming.pos = 0f;
        // 接收瞬间：把视觉目标设为我方“格中心”
        incoming.payload.worldPos = GetWorldPosition();
        _items.Insert(0, incoming);
        return true;
    }

    private bool HasSpaceForIncoming()
    {
        if (_items.Count == 0) return true;
        return _items[0].pos >= itemSpacing;
    }

    #region 连接管理

    private void ValidateConnection()
    {
        if (!IsCurrentConnectionValid())
            FindBestOutputConnection();
    }

    private bool IsCurrentConnectionValid()
    {
        if (_connectedOutputPort == null) return false;
        var targetCell = cell + _connectedDirection;
        var currentPort = grid.GetPortAt(targetCell);
        return ReferenceEquals(currentPort, _connectedOutputPort) &&
               TransportCompat.DownAccepts(_connectedDirection, currentPort);
    }

    public void FindBestOutputConnection()
    {
        _connectedOutputPort = null;
        _connectedDirection = Vector2Int.zero;

        var directions = new[] { outDir, RotCW(outDir), RotCCW(outDir) };
        foreach (var dir in directions)
        {
            if (dir == inDir) continue;
            var targetCell = cell + dir;
            var port = grid.GetPortAt(targetCell);
            if (port != null && TransportCompat.DownAccepts(dir, port))
            {
                outDir = dir;
                inDir = -dir;
                _connectedDirection = dir;
                _connectedOutputPort = port;
                UpdateVisualDirection();
                return;
            }
        }
    }
    
    #endregion
}
