using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Filter : Building, IItemPort, IOrientable, ITickable
{
    [Header("Direction")]
    public Vector2Int outDir = Vector2Int.up;   // 输出朝向
    public Vector2Int inDir  = Vector2Int.down;    // 输入朝向

    [Header("Whitelist")]
    public ItemDef allowedItem;

    [Tooltip("白名单为空时是否允许所有物品通过")]
    public bool allowAllWhenEmpty = false;

    [Header("Capacity / Behavior")]
    public int bufferCap = 16;                   // 内部缓冲上限
    public bool strictDirCheck = true;           // 推送前做方向兼容检查

    [Header("Throughput")]
    public float pushesPerSecond = 4f;           // 每秒最多推送件数
    private float _pushBudget = 0f;              // 推送预算
    
    public enum MismatchPolicy { BounceBack, PassThrough, Drop }

    [Header("Mismatch Handling")]
    public bool nonBlockingMismatch = true;           // 入口不匹配时不阻塞上游（默认开）
    public MismatchPolicy mismatchPolicy = MismatchPolicy.BounceBack;
    public int rejectBufferCap = 16;                  // 拒收缓冲上限（防爆）

    private readonly Queue<ItemPayload> _rejectQ = new(); // 不匹配的物品队列


    // ==== 内部状态 ====
    private readonly Queue<ItemPayload> _queue = new();
    public bool CanReceive => _queue.Count < bufferCap;   // IItemPort
    public bool CanProvide => false;                      // 不被动提供
    public Vector2Int Cell   => cell;                     // IBeltNode
    public Vector2Int InDir  => inDir;
    public Vector2Int OutDir => outDir;

    // ==== 放置/移除 ====
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        
        g.RegisterPort(cell, this);
        UpdateVisual();
        TickManager.Instance?.Register(this);
    }

    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }
    
    public void SetDirection(Vector2Int dir)
    {
        outDir = dir == Vector2Int.zero
            ? Vector2Int.up
            : new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));

        inDir =
            outDir == Vector2Int.right ? Vector2Int.left  :
            outDir == Vector2Int.left  ? Vector2Int.right :
            outDir == Vector2Int.up    ? Vector2Int.down  :
                                         Vector2Int.up;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        transform.up = new Vector3(outDir.x, outDir.y, 0f);
    }
    
    private bool IsAllowed(ItemDef item)
    {
        if (item == null) return false;
        if (allowedItem == null) return allowAllWhenEmpty;
        return allowedItem == item;
    }
    
// ===== TryReceive：非阻塞接收 =====
    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (!CanReceive) return false;

        // 匹配：入白名单队列
        if (IsAllowed(payloadIn.item))
        {
            var p = payloadIn;
            p.worldPos = grid != null ? grid.CellToWorld(cell) : transform.position;
            _queue.Enqueue(p);
            return true;
        }

        // 不匹配：走“拒收策略”
        if (!nonBlockingMismatch) return false; // 保留旧行为（可关）

        // 有拒收队列容量 → 接收进入拒收队列（上游立刻成功返回）
        if (_rejectQ.Count < Mathf.Max(1, rejectBufferCap))
        {
            var p = payloadIn;
            p.worldPos = grid != null ? grid.CellToWorld(cell) : transform.position;
            _rejectQ.Enqueue(p);
            return true;
        }

        // 拒收队列已满：策略为 Drop 时丢弃；否则仍拒绝
        if (mismatchPolicy == MismatchPolicy.Drop)
            return true; // 直接吞掉，避免上游卡住（按需可加特效/日志）

        return false;
    }
    
    public bool TryProvide(ref ItemPayload payload) => false;
    
// ===== Tick：优先处理拒收物，再处理白名单物 =====
    public void Tick(float dt)
    {
        if (grid == null) return;
        if (_queue.Count == 0 && _rejectQ.Count == 0) return;

        _pushBudget += Mathf.Max(0f, pushesPerSecond) * dt;
        int quota = Mathf.FloorToInt(_pushBudget);
        if (quota <= 0) return;

        int sent = 0;
        while (sent < quota && (_queue.Count > 0 || _rejectQ.Count > 0))
        {
            bool moved = false;

            // 1) 先处理拒收物（更紧急，避免堆积）
            if (_rejectQ.Count > 0)
                moved |= TryPushMismatch();

            // 2) 再处理白名单物
            if (!moved && _queue.Count > 0)
                moved |= TryPushAllowed();

            if (!moved) break; // 两边都推不动就结束（等下一帧/下游腾位置）
            sent++;
        }

        _pushBudget -= sent;
    }

    private bool TryPushForward()
    {
        var targetCell = cell + outDir;
        var port = grid.GetPortAt(targetCell);
        if (port == null || !port.CanReceive) return false;

        if (strictDirCheck && !TransportCompat.DownAccepts(outDir, port))
            return false;

        var payload = _queue.Peek();
        payload.worldPos = grid.CellToWorld(targetCell);

        if (port.TryReceive(in payload))
        {
            _queue.Dequeue();
            return true;
        }
        return false;
    }
    
    private bool TryPushAllowed()
    {
        return TryPushTo(outDir, _queue, bypassAllowCheck: false);
    }
    
    private bool TryPushMismatch()
    {
        if (_rejectQ.Count == 0) return false;

        switch (mismatchPolicy)
        {
            case MismatchPolicy.BounceBack:
                return TryPushTo(-inDir, _rejectQ, bypassAllowCheck: true);
            case MismatchPolicy.PassThrough:
                return TryPushTo(outDir, _rejectQ, bypassAllowCheck: true);
            case MismatchPolicy.Drop:
                _rejectQ.Dequeue(); // 安静丢弃（可在此添加粒子/音效）
                return true;
            default:
                return false;
        }
    }
    
    private bool TryPushTo(Vector2Int dir, Queue<ItemPayload> srcQ, bool bypassAllowCheck)
    {
        var targetCell = cell + dir;
        var port = grid.GetPortAt(targetCell);
        if (port == null || !port.CanReceive) return false;
        
        if (strictDirCheck && !TransportCompat.DownAccepts(dir, port))
            return false;

        var payload = srcQ.Peek();
        
        if (!bypassAllowCheck && !IsAllowed(payload.item))
            return false;

        payload.worldPos = grid.CellToWorld(targetCell);

        if (port.TryReceive(in payload))
        {
            srcQ.Dequeue();
            return true;
        }
        return false;
    }
}
