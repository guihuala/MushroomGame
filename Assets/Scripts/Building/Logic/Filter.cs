using System.Collections.Generic;
using UnityEngine;

public class Filter : Building, IItemPort, IOrientable, ITickable
{
    [Header("Direction")]
    public Vector2Int outDir = Vector2Int.right;   // 输出朝向
    public Vector2Int inDir  = Vector2Int.left;    // 输入朝向

    [Header("Whitelist")]
    public List<ItemDef> allowedItems = new List<ItemDef>();

    [Tooltip("白名单为空时是否允许所有物品通过")]
    public bool allowAllWhenEmpty = false;

    [Header("Capacity / Behavior")]
    public int bufferCap = 16;                   // 内部缓冲上限
    public bool strictDirCheck = true;           // 推送前做方向兼容检查

    [Header("Throughput")]
    public float pushesPerSecond = 4f;           // 每秒最多推送件数
    private float _pushBudget = 0f;              // 推送预算

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
            ? Vector2Int.right
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
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }
    
    private bool IsAllowed(ItemDef item)
    {
        if (item == null) return false;
        if (allowedItems == null || allowedItems.Count == 0) return allowAllWhenEmpty;
        return allowedItems.Contains(item);
    }
    
    public bool TryReceive(in ItemPayload payloadIn)
    {
        // 只有“指定物品 + 有输入”才接收（AND）
        if (!CanReceive) return false;
        if (!IsAllowed(payloadIn.item)) return false;

        var p = payloadIn;
        p.worldPos = grid != null ? grid.CellToWorld(cell) : transform.position;
        _queue.Enqueue(p);
        return true;
    }
    
    public bool TryProvide(ref ItemPayload payload) => false;
    
    public void Tick(float dt)
    {
        if (grid == null || _queue.Count == 0) return;

        _pushBudget += Mathf.Max(0f, pushesPerSecond) * dt;
        int quota = Mathf.FloorToInt(_pushBudget);
        if (quota <= 0) return;

        int sent = 0;
        while (sent < quota && _queue.Count > 0)
        {
            if (!TryPushForward()) break; // 前方不可用 → 暂停
            sent++;
        }

        _pushBudget -= sent; // 扣预算
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
}
