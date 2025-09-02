using UnityEngine;

public class Conveyor : Building, IItemPort, ITickable, IOrientable
{
    [SerializeField] private Vector2Int direction = Vector2Int.right;
    [SerializeField] private float moveSpeed = 3f; // 单位/秒


    private ItemPayload? _carried; // 当前在带上的一包
    private float _progress; // 0..1 沿着格子的进度


    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        transform.right = new Vector3(direction.x, direction.y, 0f);
        TickManager.Instance.Register(this);
    }


    public override void OnRemoved()
    {
        TickManager.Instance?.Unregister(this);
        grid.UnregisterPort(cell, this);
        base.OnRemoved();
    }
    
    public bool TryPull(ref ItemPayload payload)
    {
        return true;
    }

    public bool TryPush(in ItemPayload payload)
    {
        if (_carried.HasValue) return false;
        _carried = new ItemPayload
        {
            item = payload.item,
            amount = payload.amount,
            worldPos = grid.CellToWorld(cell)
        };
        _progress = 0f;
        return true;
    }

    public bool CanPull { get; }
    public bool CanPush => !_carried.HasValue;

    
    public void Tick(float dt)
    {
        if (!_carried.HasValue) return;


        float distPerCell = grid.cellSize;
        float v = moveSpeed * dt / distPerCell;
        _progress = Mathf.Min(1f, _progress + v);


        var start = grid.CellToWorld(cell);
        var end = grid.CellToWorld(cell + direction);
        var p = _carried.Value;
        p.worldPos = Vector3.Lerp(start, end, _progress);
        _carried = p;


        if (_progress >= 1f)
        {
            var downstream = grid.GetPortAt(cell + direction);
            if (downstream != null && downstream.CanPush)
            {
                if (downstream.TryPush(p))
                {
                    _carried = null;
                    _progress = 0f;
                }
            }
        }
    }


    public void SetDirection(Vector2Int dir)
    {
        direction = dir;
        transform.right = new Vector3(dir.x, dir.y, 0f);
    }
    
    public bool GetVisualState(out Sprite sprite, out Vector3 pos)
    {
        if (_carried.HasValue && _carried.Value.item != null)
        {
            sprite = _carried.Value.item.icon;
            pos = _carried.Value.worldPos;
            return true;
        }

        sprite = null;
        pos = default;
        return false;
    }
}