using System.Collections.Generic;
using UnityEngine;


public class Trashroom : Building, IItemPort, ITickable, IProductionInfoProvider
{
    [Header("Direction")]
    public Vector2Int outDir = Vector2Int.right; // 输出方向
    
    [Header("Conversion Rate")]
    public ItemDef outputItem;         // 目标产物
    public float processRate = 1f;     // 每秒处理的物品数量
    public int inputPerBatch  = 1;     // 每批消耗多少输入物品
    public int outputPerBatch = 1;     // 每批生成多少目标产物

    private float _processBudget = 0f; // 每次 Tick 后按速率消耗物品并转化为产物

    // 存储累计的输入物品
    private readonly Dictionary<ItemDef, int> _inputs = new();

    // 用于控制动画的组件
    private MushroomAnimator _mushroomAnimator;

    // IItemPort
    public bool CanReceive => true;    // 始终可以接收物品
    public bool CanProvide => false;   // 不提供任何物品

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        g.RegisterPort(cell, this);
        UpdateVisual();
        TickManager.Instance?.Register(this);

        // 获取动画组件
        _mushroomAnimator = GetComponent<MushroomAnimator>();
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

        // 累积物品
        AddCount(_inputs, payloadIn.item, payloadIn.amount);
        return true;
    }

    // 不支持被动提供
    public bool TryProvide(ref ItemPayload payload) => false;
    
    // 累积物品，增加物品数量
    private void AddCount(Dictionary<ItemDef, int> dict, ItemDef item, int amount)
    {
        if (item == null || amount <= 0) return;

        // 如果物品已经在字典中，增加它的数量
        if (dict.ContainsKey(item))
        {
            dict[item] += amount;
        }
        else
        {
            // 如果物品不在字典中，添加新的物品及其数量
            dict[item] = amount;
        }
    }
    
    public void Tick(float dt)
    {
        if (grid == null || outputItem == null) return;

        // 累计产物生成预算
        _processBudget += Mathf.Max(0f, processRate) * dt;

        int quota = Mathf.FloorToInt(_processBudget); 

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
        
        _processBudget -= quota;
    }

    // 检查是否有足够输入物品
    private bool HasAtLeastInput(int neededAmount)
    {
        return CountTotal(_inputs) >= neededAmount;
    }

    // 消耗指定数量的输入物品
    private void ConsumeInput(int amount)
    {
        if (amount <= 0) return;

        foreach (var kv in _inputs)
        {
            if (kv.Value <= amount)
            {
                amount -= kv.Value;
                _inputs.Remove(kv.Key);
                if (amount <= 0) break;
            }
            else
            {
                // 扣除部分
                _inputs[kv.Key] -= amount;
                break;
            }
        }
    }

    // 生成指定数量的产物并存入库存
    private void ProduceOutput(int amount)
    {
        if (outputItem == null || amount <= 0) return;

        _mushroomAnimator.PlaySquashAndStretch();
        InventoryManager.Instance.AddItem(outputItem, amount);
    }
    
    private static int CountTotal(Dictionary<ItemDef, int> dict)
    {
        int total = 0;
        foreach (var v in dict.Values) total += v;
        return total;
    }
    
    private void UpdateVisual()
    {
        transform.right = new Vector3(outDir.x, outDir.y, 0f);
    }

    #region toolkit

    public ProductionInfo GetProductionInfo()
    {
        var info = new ProductionInfo
        {
            displayName = gameObject.name,
            recipe = null,
            isProducing = CountTotal(_inputs) > 0,
            progress01 = 0f,
            extraText = $"Rate: {processRate}/s, conversion: everything → {outputPerBatch} x {outputItem}"
        };
        
        return info;
    }

    #endregion
}
