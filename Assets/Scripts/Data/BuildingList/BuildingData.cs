using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Building System/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("基本信息")]
    public string buildingName;
    public string description;
    public Sprite icon;
    public Building prefab;
    
    [Header("分类")]
    public BuildingCategory category = BuildingCategory.Production;
}

public enum BuildingCategory
{
    Production,     // 生产建筑（矿机等）
    Logistics,      // 物流建筑（传送带等）
}