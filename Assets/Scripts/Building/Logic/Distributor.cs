using System.Collections.Generic;
using UnityEngine;

public class Distributor : Building, IItemPort, IOrientable, ITickable
{
    [Header("Direction")]
    public Vector2Int outDir = Vector2Int.up;
    public Vector2Int inDir  = Vector2Int.down;

    [Header("Capacity / Behavior")]
    public int bufferCap = 8;            // 内部缓存上限（件）
    public bool strictDirCheck = true;   // 推送前是否做方向兼容检查

    [Header("Throughput")]
    public float pushesPerSecond = 4f;   // 每秒最多推送件数
    private float _pushBudget;           // 累积预算（dt 叠加）

    private readonly Queue<ItemPayload> _queue = new();
    private int _rrIndex = 0;            // 轮询起点（均匀分配）
    
    public bool CanReceive => _queue.Count < bufferCap;
    public bool CanProvide => false;     // 分配器不被动提供拉取
    
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
        // 简单做法：让物体“右向”对齐 outDir
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }
    
    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (!CanReceive) return false;

        var p = payloadIn;
        p.worldPos = grid != null ? grid.CellToWorld(cell) : transform.position;
        _queue.Enqueue(p);
        return true;
    }
    
    public bool TryProvide(ref ItemPayload payload) => false;
    
    public void Tick(float dt)
    {
        if (_queue.Count == 0 || grid == null) return;

        // 累积推送预算（节流）
        _pushBudget += Mathf.Max(0f, pushesPerSecond) * dt;
        int quota = Mathf.FloorToInt(_pushBudget);
        if (quota <= 0) return;

        int sent = 0;
        while (sent < quota && _queue.Count > 0)
        {
            if (!TryFanOutOne()) break; // 三个出口都堵塞 → 暂停
            sent++;
        }

        _pushBudget -= sent; // 扣除已用预算（保留小数）
    }

    // 将队首物品尝试按轮询分发到可用出口
    private bool TryFanOutOne()
    {
        // 轮询顺序：前 → 右 → 左（可改为前→左→右）
        var dirs = new Vector2Int[3];
        dirs[0] = outDir;
        dirs[1] = RotCW(outDir);
        dirs[2] = RotCCW(outDir);

        for (int i = 0; i < dirs.Length; i++)
        {
            int idx = (_rrIndex + i) % dirs.Length;
            var dir = dirs[idx];
            if (dir == inDir) continue; // 不从入口吐出

            var targetCell = cell + dir;
            var port = grid.GetPortAt(targetCell);
            if (port == null || !port.CanReceive) continue;

            // 与传送带的方向兼容检查（若启用）
            if (strictDirCheck && !TransportCompat.DownAccepts(dir, port)) continue;

            var payload = _queue.Peek();
            payload.worldPos = grid.CellToWorld(targetCell);

            if (port.TryReceive(in payload))
            {
                _queue.Dequeue();
                _rrIndex = (idx + 1) % dirs.Length; // 成功后下次从下一个出口开始
                return true;
            }
        }
        return false; // 三个方向都满/不接受
    }
}
