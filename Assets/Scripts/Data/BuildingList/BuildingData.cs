using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Building System/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("基本信息")]
    public string buildingName;
    [TextArea] public string description;
    public Sprite icon;
    public Building prefab;
    
    [Header("分类")]
    public BuildingCategory category = BuildingCategory.Production;
    
    [Header("建造费用")]
    public List<ItemStack> constructionCost = new List<ItemStack>();
    
    // 检查是否有足够的建造资源
    public bool HasEnoughResources()
    {
        foreach (var cost in constructionCost)
        {
            if (!InventoryManager.Instance.HasEnoughItem(cost.item, cost.amount))
            {
                return false;
            }
        }
        return true;
    }
    
    // 扣除建造资源
    public bool DeductConstructionCost()
    {
        if (!HasEnoughResources()) return false;
        
        foreach (var cost in constructionCost)
        {
            InventoryManager.Instance.RemoveItem(cost.item, cost.amount);
        }
        return true;
    }
}

public enum BuildingCategory
{
    Production,     // 生产建筑（矿机等）
    Logistics,      // 物流建筑（传送带等）
    Mushroom,       // 蘑菇
}