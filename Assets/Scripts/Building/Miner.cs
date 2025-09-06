using System.Collections.Generic;
using UnityEngine;

public class Miner : Building, ITickable, IOrientable, IItemPort
{
    [Header("生产效率")]
    public float cycleTime = 1.0f;  // 每生产一包的时间
    public int packetAmount = 1;    // 每包数量
    public Vector2Int outDir = Vector2Int.right;

    [Header("资源检测")]
    public ResourceTilemapService tileService;
    public LayerMask nodeMask;  // 用于检测资源节点的遮罩

    private IResourceSource _source;  // 绑定的资源节点
    private float _t;  // 用于计时的变量
    private readonly Queue<ItemPayload> _buffer = new(); // 简易缓冲
    private const int BUFFER_LIMIT = 3;

    // 属性
    public bool CanProvide => _buffer.Count > 0;
    public bool CanReceive => false;

    #region 生命周期管理

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        tileService = FindObjectOfType<ResourceTilemapService>();
        BindNodeUnder();
        grid.RegisterPort(cell, this);
        TickManager.Instance.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }

    #endregion

    #region 资源节点绑定

    private void BindNodeUnder()
    {
        if (tileService)
        {
            _source = tileService.GetSourceAt(cell);
            if (_source != null) return;
        }

        var pos = grid.CellToWorld(cell);
        var hit = Physics2D.OverlapPoint(pos, nodeMask);
        var node = hit ? hit.GetComponent<ResourceNode>() : null;
        _source = node;
    }

    #endregion

    #region 物品传输接口

    public bool TryReceive(in ItemPayload payload) => false;

    public bool TryProvide(ref ItemPayload payload)
    {
        if (_buffer.Count == 0) return false;
        payload = _buffer.Dequeue();
        return true;
    }

    #endregion

    #region Tick逻辑

    public void Tick(float dt)
    {
        if (_source == null) return;

        _t += dt;
        TryProduceItem();
        TryFlushBuffer();
    }

    private void TryProduceItem()
    {
        if (_buffer.Count >= BUFFER_LIMIT || _t < cycleTime) return;

        if (_source.TryConsumeOnce())
        {
            var payload = new ItemPayload
            {
                item = _source.YieldItem,
                amount = packetAmount
            };

            _buffer.Enqueue(payload);
        }

        _t = 0f;
    }

    private void TryFlushBuffer()
    {
        if (_buffer.Count == 0) return;

        var targetCell = cell + outDir;
        var outPort = grid.GetPortAt(targetCell);

        if (outPort != null && outPort.CanReceive)
        {
            ItemPayload payload = _buffer.Peek();
            if (outPort.TryReceive(in payload))
            {
                _buffer.Dequeue();
            }
        }
    }

    #endregion

    #region 方向设置

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        transform.right = new Vector3(dir.x, dir.y, 0f);
    }

    #endregion
}