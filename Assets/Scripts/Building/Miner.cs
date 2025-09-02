using System;
using System.Collections.Generic;
using UnityEngine;

public class Miner : Building, ITickable, IOrientable
{
    [Header("生产效率")]
    public float cycleTime = 1.0f;  // 每生产一包的时间
    public int packetAmount = 1;    // 每包数量
    public Vector2Int outDir = Vector2Int.right;

    [Header("资源检测")]
    public ResourceTilemapService tileService;
    private IResourceSource _source;
    public LayerMask nodeMask;
    private ResourceNode _node;

    private float _t;
    private readonly Queue<ItemPayload> _buffer = new(); // 简易缓冲
    private const int BUFFER_LIMIT = 3;
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        
        tileService = FindObjectOfType<ResourceTilemapService>();
        
        BindNodeUnder();
        TickManager.Instance.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        base.OnRemoved();
    }

    private void BindNodeUnder()
    {
        if (tileService)
        {
            _source = tileService.GetSourceAt(cell);
            if (_source != null) return;
        }
        else
        {
            Debug.LogWarning("tileService == null");
        }

        var pos = grid.CellToWorld(cell);
        var hit = Physics2D.OverlapPoint(pos, nodeMask);
        var node = hit ? hit.GetComponent<ResourceNode>() : null;
        _source = node;

        if (_source == null)
            Debug.LogWarning($"Miner at {cell} found no resource.");
    }

    public void Tick(float dt)
    {
        if (_source == null) return;
        
        // 先尝试把缓冲里的货物往外推
        TryFlushBuffer();
        
        if (_source.TryConsumeOnce())
        {
            var payload = new ItemPayload
            {
                item = _source.YieldItem,
                amount = packetAmount,
                worldPos = grid.CellToWorld(cell)
            };
            _buffer.Enqueue(payload);
            _t = 0f;
        }

        // 再试一次推（避免等到下帧）
        TryFlushBuffer();
    }

    private void TryFlushBuffer()
    {
        if (_buffer.Count == 0) return;

        var outPort = grid.GetPortAt(cell + outDir);
        if (outPort != null && outPort.CanPush)
        {
            // 只推一包，留点节奏
            var pkg = _buffer.Peek();
            if (outPort.TryPush(pkg)) _buffer.Dequeue();
        }
    }
    
    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        transform.right = new Vector3(dir.x, dir.y, 0f);
    }
}
