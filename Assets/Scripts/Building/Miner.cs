using UnityEngine;

public class Miner : Building, IItemPort
{
    [SerializeField] private ItemDef oreItem;   // 产出物
    [SerializeField] private float cycleTime = 1.0f; // 多少秒产一包
    [SerializeField] private int packetAmount = 1;
    [SerializeField] private Vector2Int outDir = Vector2Int.right;

    [Header("采集条件")]
    public bool requiresLight;
    public bool requiresPower;

    private float _t;
    private IItemPort _outPort;

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        FindOutPort();
        ItemTickManager.Instance.Register(GetComponent<Conveyor>());
    }

    private void FindOutPort()
    {
        var nextCell = cell + outDir;
        var world = grid.CellToWorld(nextCell);
        var hit = Physics2D.OverlapPoint(world);
        _outPort = hit ? hit.GetComponentInParent<IItemPort>() : null;
    }

    void FixedUpdate()
    {
        // 条件检查
        if (requiresLight)
        {
            // TODO: 检测 LightField 
        }

        if (requiresPower)
        {
            // TODO: 检测 PowerField
        }

        _t += Time.fixedDeltaTime;
        if (_t >= cycleTime && _outPort != null && _outPort.CanPush)
        {
            var payload = new ItemPayload
            {
                item = oreItem,
                amount = packetAmount,
                worldPos = transform.position
            };
            if (_outPort.TryPush(payload)) _t = 0f;
        }
    }

    // 采集器不收货，只出货
    public bool TryPull(ref ItemPayload payload) => false;
    public bool TryPush(in ItemPayload payload) => false;
    public bool CanPull => false;
    public bool CanPush => false;
}