using System.Collections.Generic;
using UnityEngine;

public class Conveyer : Building, ITickable, IItemPort, IOrientable, IAutoTiler
{
    [Header("传送带设置")]
    public float beltSpeed = 1.0f; // 每秒移动的格子数
    public Vector2Int inDir = Vector2Int.left;
    public Vector2Int outDir = Vector2Int.right;

    [Header("容量限制")]
    public int maxItems = 3;

    // 数据存储
    private readonly List<BeltItem> _items = new();
    private IItemPort _connectedOutputPort;
    private Vector2Int _connectedDirection;

    // 自动铺路相关
    private int _lastAutoTileFrame = -1;
    private static Vector2Int RotCW(Vector2Int v) => new(v.y, -v.x);
    private static Vector2Int RotCCW(Vector2Int v) => new(-v.y, v.x);

    // 属性
    public bool CanProvide => _items.Count > 0 && _items[0].position >= 0.95f;
    public bool CanReceive => _items.Count < maxItems;
    public IReadOnlyList<BeltItem> Items => _items;
    public Vector2Int Direction => outDir;
    
    public Vector3 GetWorldPosition()
    {
        if (grid == null) 
        {
            Debug.LogWarning("Grid reference is null in Conveyor");
            return transform.position;
        }
        return grid.CellToWorld(cell);
    }

    #region 生命周期管理

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
        MsgCenter.RegisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        MsgCenter.SendMsg(MsgConst.MSG_CONVEYOR_PLACED,this);
        AutoTile();
    }

    public override void OnRemoved()
    {
        MsgCenter.SendMsg(MsgConst.MSG_CONVEYOR_REMOVED,this);
        MsgCenter.UnregisterMsg(MsgConst.MSG_NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        TickManager.Instance?.Unregister(this);
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

    #region 物品传输接口

    public bool TryReceive(in ItemPayload payload)
    {
        if (!CanReceive) return false;

        _items.Add(new BeltItem
        {
            payload = payload,
            position = 0f
        });

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

        // 检查输出连接是否有效
        bool hasValidOutput = IsCurrentConnectionValid() && _connectedOutputPort != null &&
                              _connectedOutputPort.CanReceive;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];

        // 只有前面的物品移动了，后面的才能移动（防重叠）
        float maxAllowedPosition = i == 0 ? float.MaxValue : _items[i - 1].position - 0.1f;

        // 如果是最后一个物品且没有有效输出，限制最大位置为0.99f（防止完全移出）
        bool isLastItem = i == _items.Count - 1;
        if (isLastItem && !hasValidOutput)
        {
            maxAllowedPosition = Mathf.Min(maxAllowedPosition, 0.99f);
        }

        float newPosition = item.position + moveDistance;
        item.position = Mathf.Min(newPosition, maxAllowedPosition);
        _items[i] = item;
    }
}

private void TryTransferFirstItem()
{
    if (_items.Count == 0 || _items[0].position < 0.95f) return;

    ValidateConnection();

    // 检查连接是否有效且可以接收
    if (_connectedOutputPort == null || !_connectedOutputPort.CanReceive)
    {
        // 如果没有有效输出，阻止物品继续前进
        if (_items[0].position > 0.99f)
        {
            var item = _items[0];
            item.position = 0.99f; // 卡在尽头位置
            _items[0] = item;
        }

        return;
    }

    var payload = _items[0].payload;
    if (_connectedOutputPort.TryReceive(in payload))
    {
        _items.RemoveAt(0);
    }
}

private void ClampItemPositions()
{
    bool hasValidOutput = IsCurrentConnectionValid() && _connectedOutputPort != null && _connectedOutputPort.CanReceive;

    for (int i = 0; i < _items.Count; i++)
    {
        if (_items[i].position > 1f)
        {
            var item = _items[i];

            // 如果是最后一个物品且没有有效输出，限制在0.99f
            bool isLastItem = i == _items.Count - 1;
            if (isLastItem && !hasValidOutput)
            {
                item.position = 0.99f;
            }
            else
            {
                item.position = 1f;
            }

            _items[i] = item;
        }
    }
}

#endregion

#region 连接管理

private void ValidateConnection()
{
    if (!IsCurrentConnectionValid())
    {
        FindBestOutputConnection();
    }
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