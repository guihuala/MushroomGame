using System.Collections.Generic;
using UnityEngine;

public class MushroomBuilding : Building, IItemPort
{
    // 占地大小 2x1
    public Vector2Int size = new Vector2Int(2, 1);

    // 输入端口
    public IItemPort inputPort;

    // 输出端口
    public IItemPort outputPort;

    // 蘑菇的生产配方
    public RecipeDef recipe;  // 使用的配方
    private float currentCycleTime = 0f;  // 当前生产周期
    private List<ItemStack> inputItems = new List<ItemStack>();  // 存储输入材料
    private List<ItemStack> outputItems = new List<ItemStack>();  // 存储输出产品

    private void Start()
    {
        inputPort = GetComponentInChildren<IItemPort>();  // 假设输入端口是左侧
        outputPort = GetComponentInChildren<IItemPort>(); // 假设输出端口是右侧
    }

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        grid.RegisterPort(cell, this);
        // 如果需要初始化输入输出，可以在这里添加
    }

    public override void OnRemoved()
    {
        base.OnRemoved();
        grid.UnregisterPort(cell, this);
    }

    public void Tick(float dt)
    {
        // 累积生产时间
        currentCycleTime += dt;

        // 检查是否可以生产
        if (currentCycleTime >= recipe.productionTime)
        {
            // 检查是否满足配方的输入条件
            if (recipe.CanProduce(inputItems))
            {
                Produce();
                currentCycleTime = 0f;  // 重置生产周期
            }
        }
    }

    // 执行生产操作
    private void Produce()
    {
        // 消耗输入材料并产生输出产品
        recipe.Produce(inputItems, outputItems);

        // 通过输出端口将产品推送出去
        if (outputPort != null && outputItems.Count > 0)
        {
            foreach (var output in outputItems)
            {
                ItemPayload outputPayload = new ItemPayload
                {
                    item = output.item,
                    amount = output.amount,
                    worldPos = grid.CellToWorld(cell) // 可用于做物品的动画效果
                };
                outputPort.TryPush(outputPayload);
            }
        }

        // 记录输出
        DebugManager.Log($"Mushroom produced: {string.Join(", ", outputItems)}.");
    }

    // 作为输入端口的接口实现
    public bool TryPull(ref ItemPayload payload)
    {
        // 如果输入端口有空间，可以从外部拉取材料
        return false;
    }

    public bool TryPush(in ItemPayload payload)
    {
        // 如果输出端口有空间，可以向外推送产品
        return true;
    }

    public bool CanPush { get; }
    public bool CanPull { get; }
}


