using UnityEngine;

public class Conveyor : Building, IItemPort
{
    [SerializeField] private float moveSpeed = 3f; // 世界单位/秒
    [SerializeField] private Transform visual;     // 箭头或皮带动画
    [SerializeField] private Vector2Int direction = Vector2Int.right;

    private IItemPort _nextPort;
    private ItemPayload? _carried; // 皮带上的当前小包

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        // 根据 direction 设置朝向
        transform.right = new Vector3(direction.x, direction.y, 0);
        // 尝试在相邻格找到下游端口（放在 ItemTickManager 的注册阶段也行）
        FindNextPort();
        ItemTickManager.Instance.Register(this);
    }

    public override void OnRemoved()
    {
        ItemTickManager.Instance?.Unregister(this);
        base.OnRemoved();
    }

    private void FindNextPort()
    {
        var nextCell = cell + direction;
        // 简化：用 Physics2D Overlap 方式或在 GridService 里查占位并 GetComponent<IItemPort>()
        var world = grid.CellToWorld(nextCell);
        var hit = Physics2D.OverlapPoint(world);
        _nextPort = hit ? hit.GetComponentInParent<IItemPort>() : null;
    }

    // IItemPort：上游往我这推送
    public bool TryPush(in ItemPayload payload)
    {
        if (_carried.HasValue) return false;
        _carried = payload;
        _carried = new ItemPayload {
            item = payload.item,
            amount = payload.amount,
            worldPos = transform.position
        };
        return true;
    }

    public bool TryPull(ref ItemPayload payload)
    {
        // 传送带不对外“拉走”——由下游来推送/拉取都行，这里给 false
        return false;
    }

    public bool CanPull => false;
    public bool CanPush => !_carried.HasValue;

    // 由 ItemTickManager 调度
    public void Tick(float dt)
    {
        if (!_carried.HasValue) return;

        var p = _carried.Value;
        var target = grid.CellToWorld(cell + direction);

        // 朝向目标插值
        p.worldPos = Vector3.MoveTowards(p.worldPos, target, moveSpeed * dt);
        _carried = p;

        // 到达末端：尝试塞进下游
        if (Vector3.Distance(p.worldPos, target) < 0.01f && _nextPort != null && _nextPort.CanPush)
        {
            if (_nextPort.TryPush(p))
            {
                _carried = null;
            }
        }
    }
}
