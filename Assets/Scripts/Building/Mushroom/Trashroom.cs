using UnityEngine;

public class Trashroom : Building, IItemPort, ITickable
{
    [Header("Direction")]
    public Vector2Int outDir = Vector2Int.right; // 输出方向
    public Vector2Int inDir  = Vector2Int.left;  // 输入方向

    [Header("Conversion Rate")]
    public ItemDef outputItem;         // 目标产物
    public float processRate = 1f;     // 每秒处理的物品数量（即每秒转化多少物品为产物）
    public int inputPerBatch  = 1;     // 每批消耗多少输入物品
    public int outputPerBatch = 1;     // 每批生成多少目标产物
    
    private float _processBudget = 0f; // 每次 Tick 后按速率消耗物品并转化为产物

    // IItemPort
    public bool CanReceive => true;    // 始终可以接收物品
    public bool CanProvide => false;   // 不提供任何物品
    
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
    
    public bool TryReceive(in ItemPayload payloadIn)
    {
        if (payloadIn.item == null || payloadIn.amount <= 0) return false;

        // 直接把物品加入库存
        bool ok = InventoryManager.Instance.AddItem(payloadIn.item, payloadIn.amount);
        return ok; // 返回是否成功
    }

    // 不支持被动提供
    public bool TryProvide(ref ItemPayload payload) => false;

    // ==== 每Tick按速率处理物品并转化为目标产物 ====
    public void Tick(float dt)
    {
        if (grid == null || outputItem == null) return;

        // 累计产物生成预算
        _processBudget += Mathf.Max(0f, processRate) * dt;

        int quota = Mathf.FloorToInt(_processBudget); // 计算可生成的“批数”

        if (quota <= 0) return;

        // 逐批次转化物品
        for (int i = 0; i < quota; i++)
        {
            if (HasAtLeastInput(inputPerBatch)) // 确保有足够物品才能转化
            {
                ConsumeInput(inputPerBatch);  // 消耗输入物品
                ProduceOutput(outputPerBatch); // 生成产物
            }
        }

        // 扣除预算
        _processBudget -= quota;
    }

    // 检查是否有足够输入物品
    private bool HasAtLeastInput(int neededAmount)
    {
        return InventoryManager.Instance.GetItemCount(outputItem) >= neededAmount;
    }

    // 消耗指定数量的输入物品
    private void ConsumeInput(int amount)
    {
        if (amount <= 0) return;

        bool consumed = InventoryManager.Instance.RemoveItem(outputItem, amount);
        if (!consumed)
        {
            Debug.LogWarning("[Trashroom] Failed to consume required input!");
        }
    }

    // 生成指定数量的产物并存入库存
    private void ProduceOutput(int amount)
    {
        if (outputItem == null || amount <= 0) return;

        bool added = InventoryManager.Instance.AddItem(outputItem, amount);
        if (!added)
        {
            Debug.LogWarning("[Trashroom] Failed to add output item to inventory!");
        }
    }

    // ==== 方向可视化 ====
    private void UpdateVisual()
    {
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }
}
