using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Recipe")]
public class RecipeDef : ScriptableObject
{
    public string recipeName;  // 配方名称
    public List<ItemStack> inputItems;  // 输入材料列表
    public List<ItemStack> outputItems;  // 输出产品列表
    public float productionTime;  // 生产时间（秒）

    // 检查是否满足输入材料条件
    public bool CanProduce(List<ItemStack> availableItems)
    {
        foreach (var input in inputItems)
        {
            bool found = false;
            foreach (var available in availableItems)
            {
                if (input.item == available.item && available.amount >= input.amount)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }

    // 消耗输入材料并返回输出
    public void Produce(List<ItemStack> availableItems, List<ItemStack> outputItems)
    {
        // 消耗输入材料
        foreach (var input in inputItems)
        {
            foreach (var available in availableItems)
            {
                if (input.item == available.item && available.amount >= input.amount)
                {
                    // 减少输入材料数量
                    break;
                }
            }
        }

        // 添加输出产品
        foreach (var output in outputItems)
        {
            bool found = false;
            foreach (var available in outputItems)
            {
                if (available.item == output.item)
                {
                    // 增加输出产品数量
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                outputItems.Add(new ItemStack { item = output.item, amount = output.amount });
            }
        }
    }
}