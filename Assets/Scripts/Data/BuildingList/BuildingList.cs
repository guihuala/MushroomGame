using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingList", menuName = "Building System/Building List")]
public class BuildingList : ScriptableObject
{
    public List<BuildingData> allBuildings = new List<BuildingData>();
    
    // 按分类获取建筑
    public List<BuildingData> GetBuildingsByCategory(BuildingCategory category)
    {
        return allBuildings.FindAll(b => b.category == category);
    }
    
    // 获取所有分类
    public BuildingCategory[] GetAllCategories()
    {
        return (BuildingCategory[])System.Enum.GetValues(typeof(BuildingCategory));
    }
}