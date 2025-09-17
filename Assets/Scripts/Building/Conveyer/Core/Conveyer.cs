using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Conveyer : Building, IItemPort, IOrientable, IBeltNode
{
    #region 基础参数

    [Header("传送带")] [Tooltip("每秒移动的格子数")] public float beltSpeed = 1.0f;
    [Tooltip("输入方向")] public Vector2Int inDir;
    [Tooltip("输出方向")] public Vector2Int outDir;

    [Header("物流")] [Tooltip("物品之间的最小间距（0~1）")]
    public float itemSpacing = 0.30f;

    [Tooltip("带上最大物品数量")] public int maxItems = 3;

    #region 精灵图配置

    [Header("传送带精灵图")]
    [SerializeField] private BeltSpriteSet sprites;
    [SerializeField] private Sprite deadEndCap;
    [SerializeField] private Sprite none;

    private SpriteRenderer spriteRenderer;

    #endregion

    #endregion

    #region 内部数据

    private readonly List<BeltItem> _items = new();
    public IReadOnlyList<BeltItem> Items => _items;

    private IItemPort _connectedOutputPort;
    private Vector2Int _connectedDirection;

    private readonly List<IItemPort> _connectedInputPorts = new();
    private IBeltNode _beltNodeImplementation;

    #endregion

    #region IBeltNode

    public Vector2Int Cell => cell;
    
    public Vector2Int InDir => inDir;
    public Vector2Int OutDir => outDir;

    public virtual void StepMove(float dt) => UpdateItemPositions(dt);

    public virtual void StepTransfer()
    {
        TryReceiveFromInputs();// 尝试从所有输入源接收物品
        TryTransferFirstItem();// 尝试转移第一个物品
        ClampItemPositions();
    }

    private void TryReceiveFromInputs()
    {
        if (!CanReceive) return;

        // 遍历所有输入端口，并接收物品
        foreach (var inputPort in _connectedInputPorts)
        {
            if (inputPort.CanProvide)
            {
                ItemPayload payload = default;
                if (inputPort.TryProvide(ref payload))
                {
                    // 接收物品
                    TryReceive(in payload);
                }
            }
        }
    }

    #endregion

    #region 生命周期

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        AutoTile();
        
        MsgCenter.SendMsg(MsgConst.NEIGHBOR_CHANGED, Cell);
        MsgCenter.RegisterMsg(MsgConst.NEIGHBOR_CHANGED, OnNeighborChangedMsg);
        MsgCenter.SendMsg(MsgConst.CONVEYOR_PLACED, this);
        BeltScheduler.Instance?.RebuildAllPaths();
    }

    public override void OnRemoved()
    {
        MsgCenter.SendMsg(MsgConst.NEIGHBOR_CHANGED, Cell);
        MsgCenter.SendMsg(MsgConst.CONVEYOR_REMOVED, this);
        MsgCenter.UnregisterMsg(MsgConst.NEIGHBOR_CHANGED, OnNeighborChangedMsg);

        grid.UnregisterPort(cell, this);
        BeltScheduler.Instance?.RebuildAllPaths();
        base.OnRemoved();
    }

    #endregion

    #region 邻居/自动布线

    private void OnNeighborChangedMsg(params object[] args)
    {
        if (!this || !gameObject.activeInHierarchy) return;
        if (args.Length > 0 && args[0] is Vector2Int changed && (changed - cell).sqrMagnitude <= 2f)
            AutoTile(true);
    }

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        inDir = - dir;
        BeltScheduler.Instance?.RebuildAllPaths();
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
        _connectedDirection = outDir; // 只认当前朝向

        var port = grid.GetPortAt(cell + outDir) as IItemPort;
        if (port != null && TransportCompat.DownAccepts(outDir, port))
        {
            _connectedOutputPort = port;
        }
    }

    #endregion
    
    // 邻居是否能作为我 outDir 的接收者
    private static bool AcceptsTopo(Vector2Int dirToDown, IItemPort down)
    {
        if (down == null) return false;
        if (down is Miner) return false; // 不接收的建筑
        return true; // 传送带及其他默认可接
    }

    // 邻居是否把输出对准我
    private static bool FeedsTopo(Vector2Int dirToMe, IItemPort up)
    {
        if (up == null) return false;
        switch (up)
        {
            case Conveyer c: return c.outDir == dirToMe;
            case Miner m:    return m.outDir == dirToMe;
            default:         return false;
        }
    }
    
    private static readonly Vector2Int[] CARD = {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    private int ComputeConnectionMask()
    {
        int mask = 0;

        for (int i = 0; i < 4; i++)
        {
            Vector2Int dir = CARD[i];
            var neighbor = grid.GetPortAt(cell + dir) as IItemPort;
            if (neighbor == null) continue;

            bool connected;
            if (dir == outDir)
            {
                // 前方：我 -> 邻居
                connected = AcceptsTopo(outDir, neighbor);
                
            }
            else
            {
                // 侧/后：邻居 -> 我
                connected = FeedsTopo(-dir, neighbor);
            }

            if (connected) mask |= (1 << i);
        }

        return mask;
    }
    
    private (Sprite sprite, float zRotDeg) ChooseSpriteAndRotation(int mask)
    {
        // 便利函数：把某两位/三位的模式识别出来
        bool has(int bit) => (mask & bit) != 0;

        // 位意义：U=1, R=2, D=4, L=8
        int U = 1, R = 2, D = 4, L = 8;

        // 1) 四向全连：十字
        if (mask == (U | R | D | L)) return (sprites.cross, 0f);

        // 2) 三向连：T 字（缺的那一侧决定朝向）
        if (has(U) && has(R) && has(D) && !has(L)) return (sprites.tee, 0f); // 缺左：朝右的T（默认姿态）
        if (has(R) && has(D) && has(L) && !has(U)) return (sprites.tee, -90f); // 缺上：朝下
        if (has(D) && has(L) && has(U) && !has(R)) return (sprites.tee, -180f); // 缺右：朝左
        if (has(L) && has(U) && has(R) && !has(D)) return (sprites.tee, +90f); // 缺下：朝上

        // 3) 两向连：要么直线，要么转角
        if (has(U) && has(D) && !has(R) && !has(L)) return (sprites.straight, 0f); // 竖直（默认直线纵向）
        if (has(L) && has(R) && !has(U) && !has(D)) return (sprites.straight, 90f); // 水平

        // 转角（相邻两侧）
        if (has(U) && has(R)) return (sprites.corner, 0f); // Up->Right（默认角）
        if (has(R) && has(D)) return (sprites.corner, -90f); // Right->Down
        if (has(D) && has(L)) return (sprites.corner, -180f); // Down->Left
        if (has(L) && has(U)) return (sprites.corner, +90f); // Left->Up

        // 4) 单侧连：端帽
        if (deadEndCap != null)
        {
            if (mask == U) return (deadEndCap, 180f);
            if (mask == R) return (deadEndCap, 90f);
            if (mask == D) return (deadEndCap, 0f);
            if (mask == L) return (deadEndCap, -90f);
        }

        // 5) 无连接
        return (none, 0f);
    }

    public void UpdateVisualSprite()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer) return;

        int mask = ComputeConnectionMask(); // 获取当前连接掩码
        
        Debug.Log($"[BELT VIS] cell={cell} out={outDir} mask={Convert.ToString(mask,2).PadLeft(4,'0')}");

        var (spr, rot) = ChooseSpriteAndRotation(mask);

        spriteRenderer.sprite = spr;
        transform.localRotation = Quaternion.Euler(0, 0, rot);
    }
}

[System.Serializable]
public struct BeltSpriteSet
{
    [Tooltip("直线基底（默认纵向 Up<->Down），会旋转复用成横向")]
    public Sprite straight;

    [Tooltip("转角基底（默认 Up->Right），会旋转复用成四个拐角")]
    public Sprite corner;

    [Tooltip("T字基底（默认 缺Left，即连接 Up+Right+Down）")]
    public Sprite tee;

    [Tooltip("十字（四向全连）")] public Sprite cross;
}
