using System.Collections.Generic;
using UnityEngine;

public partial class Conveyer : Building, IItemPort, IOrientable, IBeltNode
{
    #region 基础参数
    [Header("传送带")]
    [Tooltip("每秒移动的格子数")] public float beltSpeed = 1.0f;
    [Tooltip("输入方向")] public Vector2Int inDir = Vector2Int.left;
    [Tooltip("输出方向")] public Vector2Int outDir = Vector2Int.right;

    [Header("物流")]
    [Tooltip("物品之间的最小间距（0~1）")] public float itemSpacing = 0.30f;
    [Tooltip("带上最大物品数量")] public int maxItems = 3;
    #endregion

    #region 内部数据
    private readonly List<BeltItem> _items = new();
    public IReadOnlyList<BeltItem> Items => _items;

    private IItemPort _connectedOutputPort;
    private Vector2Int _connectedDirection;
    #endregion

    #region IBeltNode
    public Vector2Int Cell => cell;
    public Vector2Int InDir => inDir;
    public Vector2Int OutDir => outDir;

    public virtual void StepMove(float dt)     => UpdateItemPositions(dt);
    public virtual void StepTransfer()          { TryTransferFirstItem(); ClampItemPositions(); }
    #endregion

    #region 生命周期（放置/移除）
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);

        MsgCenter.RegisterMsg(MsgConst.NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        MsgCenter.SendMsg(MsgConst.CONVEYOR_PLACED, this);

        AutoTile();
        BeltScheduler.Instance?.RebuildAllPaths();
        MarkPathDirty();   // PathLines 部分提供
    }

    public override void OnRemoved()
    {
        MsgCenter.SendMsg(MsgConst.CONVEYOR_REMOVED, this);
        MsgCenter.UnregisterMsg(MsgConst.NEIGHBOR_CHANGED, OnNeighborChangedMsg);

        grid.UnregisterPort(cell, this);
        BeltScheduler.Instance?.RebuildAllPaths();

        _connectedOutputPort = null;
        MarkPathDirty();
        base.OnRemoved();
    }
    #endregion

    #region 邻居/自动布线（实现见 AutoTile 部分）
    public override void OnNeighborChanged() => AutoTile();
    private void OnNeighborChangedMsg(params object[] args)
    {
        if (!this || !gameObject.activeInHierarchy) return;
        if (args.Length > 0 && args[0] is Vector2Int changed && (changed - cell).sqrMagnitude == 1)
            OnNeighborChanged();
    }

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir; inDir = -dir;
        UpdateVisualDirection();
        BeltScheduler.Instance?.RebuildAllPaths();
    }

    public void ApplyDirAndRebuild()
    {
        UpdateVisualDirection();
        FindBestOutputConnection();
        MarkPathDirty();
    }
    #endregion

    #region 物流推进/转移
    private void UpdateItemPositions(float dt)
    {
        if (_items.Count == 0) return;

        float move = Mathf.Max(0f, dt * beltSpeed);
        float headLimit = 1f;

        var nextPort = grid.GetPortAt(cell + outDir);
        if (nextPort is Conveyer nextBelt)
        {
            headLimit = (nextBelt.Items.Count == 0) ? 1f : Mathf.Min(1f, nextBelt.Items[0].pos - itemSpacing);
        }
        else
        {
            bool canOut = IsCurrentConnectionValid() && _connectedOutputPort != null && _connectedOutputPort.CanReceive;
            headLimit = canOut ? 1f : 1f - 0.0001f;
        }

        Vector3 a = grid.CellToWorld(cell);
        Vector3 b = grid.CellToWorld(cell + outDir);

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            float limit = (i == _items.Count - 1) ? headLimit : Mathf.Min(_items[i + 1].pos - itemSpacing, 1f);
            var it = _items[i];
            it.pos = Mathf.Min(it.pos + move, limit);
            it.payload.worldPos = Vector3.Lerp(a, b, Mathf.Clamp01(it.pos));
            _items[i] = it;
        }
    }

    private void TryTransferFirstItem()
    {
        if (_items.Count == 0) return;

        int headIdx = _items.Count - 1;
        var head = _items[headIdx];
        if (head.pos < 1f - 1e-4f) return;

        var nextPort = grid.GetPortAt(cell + outDir);
        if (nextPort is Conveyer nextBelt)
        {
            bool space = nextBelt.Items.Count == 0 || nextBelt.Items[0].pos >= itemSpacing;
            if (!space || nextBelt.Items.Count >= nextBelt.maxItems) return;

            _items.RemoveAt(headIdx);
            head.pos = 0f;
            nextBelt.InternalReceive(head);
            return;
        }

        ValidateConnection();
        if (_connectedOutputPort != null && _connectedOutputPort.CanReceive)
        {
            var payload = head.payload;
            if (_connectedOutputPort.TryReceive(in payload))
                _items.RemoveAt(headIdx);
        }
    }

    private void InternalReceive(BeltItem item)
    {
        item.pos = 0f;
        _items.Insert(0, item);
    }

    private void ClampItemPositions()
    {
        for (int i = 1; i < _items.Count; i++)
            if (_items[i].pos < _items[i - 1].pos + itemSpacing)
                _items[i].pos = _items[i - 1].pos + itemSpacing;

        if (_items.Count > 0)
            _items[_items.Count - 1].pos = Mathf.Min(_items[_items.Count - 1].pos, 1f);
    }
    #endregion

    #region IItemPort
    public bool CanReceive => _items.Count < maxItems && (_items.Count == 0 || _items[0].pos >= itemSpacing);
    public bool CanProvide => _items.Count > 0 && _items[_items.Count - 1].pos >= 1f - 1e-4f;

    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (!CanReceive) return false;
        var item = new BeltItem(payloadIn) { pos = 0f };
        _items.Insert(0, item);
        return true;
    }

    public bool TryProvide(ref ItemPayload payload)
    {
        if (!CanProvide) return false;
        int idx = _items.Count - 1;
        payload = _items[idx].payload;
        _items.RemoveAt(idx);
        return true;
    }

    public bool TryAcceptFromNeighbour(Conveyer from)
    {
        if (_items.Count > 0 && _items[0].pos < itemSpacing) return false;
        var incoming = from._items[from._items.Count - 1];
        incoming.pos = 0f;
        _items.Insert(0, incoming);
        return true;
    }
    #endregion

    #region 连接/朝向
    private void ValidateConnection()
    {
        if (!IsCurrentConnectionValid()) FindBestOutputConnection();
    }

    private bool IsCurrentConnectionValid()
    {
        if (_connectedOutputPort == null) return false;
        var target = cell + _connectedDirection;
        var currentPort = grid.GetPortAt(target);
        return ReferenceEquals(currentPort, _connectedOutputPort) &&
               TransportCompat.DownAccepts(_connectedDirection, currentPort);
    }

    public void FindBestOutputConnection()
    {
        _connectedOutputPort = null;
        _connectedDirection = Vector2Int.zero;

        var dirs = new[] { outDir, RotCW(outDir), RotCCW(outDir) };
        foreach (var dir in dirs)
        {
            if (dir == inDir) continue;
            var port = grid.GetPortAt(cell + dir);
            if (port != null && TransportCompat.DownAccepts(dir, port))
            {
                outDir = dir; inDir = -dir;
                _connectedDirection = dir; _connectedOutputPort = port;
                UpdateVisualDirection();
                return;
            }
        }
    }

    private void UpdateVisualDirection()
    {
        float ang = (outDir == Vector2Int.right) ? 0f :
                    (outDir == Vector2Int.up)    ? 90f :
                    (outDir == Vector2Int.left)  ? 180f : -90f;
        transform.localRotation = Quaternion.Euler(0, 0, ang);
    }

    public Vector3 GetWorldPosition() => grid.CellToWorld(cell);
    private static Vector2Int RotCW(Vector2Int v)  => new(v.y, -v.x);
    private static Vector2Int RotCCW(Vector2Int v) => new(-v.y, v.x);
    #endregion
}