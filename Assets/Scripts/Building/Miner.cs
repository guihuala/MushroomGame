using System;
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
    private IResourceSource _source;
    public LayerMask nodeMask;
    private ResourceNode _node;

    private float _t;  // 用于计时的变量
    private readonly Queue<ItemPayload> _buffer = new(); // 简易缓冲
    private const int BUFFER_LIMIT = 3;
    
    public bool CanPull => false; // 矿机不能拉取，只能推送
    public bool CanPush => _buffer.Count > 0; // 有物品时可以推送
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        
        tileService = FindObjectOfType<ResourceTilemapService>();
        
        BindNodeUnder(); // 检查当前位置有没有资源
        grid.RegisterPort(cell, this); // 注册端口到网格
        TickManager.Instance.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this); // 注销端口
        base.OnRemoved();
    }

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

        if (_source == null)
            Debug.LogWarning($"Miner at {cell} found no resource.");
    }
    
    public bool TryPull(ref ItemPayload payload)
    {
        return false; // 矿机不拉取
    }

    public bool TryPush(in ItemPayload payload)
    {
        return false; // 矿机不能接收推送，只能推送出去
    }

    // 尝试将物品从缓冲区推送到目标
    private void TryFlushBuffer()
    {
        if (_buffer.Count == 0) return;

        var targetCell = cell + outDir;
        var outPort = grid.GetPortAt(targetCell);
        
        if (outPort != null && outPort.CanPull) // 目标位置尝试拉取
        {
            ItemPayload payload = _buffer.Peek();;
            if (outPort.TryPull(ref payload))
            {
                // 成功拉取物品后，减少一个缓存
                _buffer.Dequeue();
            }
        }
    }

    public void Tick(float dt)
    {
        if (_source == null) 
        {
            DebugManager.LogWarning($"Miner at {cell} has no resource source", this);
            return;
        }

        // 累积时间，达到指定的 cycleTime 时采集一包资源
        _t += dt;
        
        // 只有当缓冲区未满，且时间到了才会尝试采集
        if (_buffer.Count < BUFFER_LIMIT && _t >= cycleTime)
        {
            // 如果有资源
            if (_source.TryConsumeOnce())
            {
                var payload = new ItemPayload
                {
                    item = _source.YieldItem,
                    amount = packetAmount,
                    worldPos = grid.CellToWorld(cell)
                };

                _buffer.Enqueue(payload);  // 将物品加入缓冲区
                DebugManager.Log($"Miner at {cell} produced {payload.amount}x {payload.item?.name}", this);
            }
            else
            {
                DebugManager.LogWarning($"Miner at {cell} failed to produce, no resources available", this);
            }

            _t = 0f;  // 重置计时器
        }

        // 尝试将物品从缓冲区推送到目标
        TryFlushBuffer();
    }

    public void SetDirection(Vector2Int dir)
    {
        outDir = dir;
        transform.right = new Vector3(dir.x, dir.y, 0f);
    }
}

