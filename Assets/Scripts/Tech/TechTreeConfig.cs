using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "TechTreeConfig", menuName = "Tech Tree/Tech Tree Configuration")]
public class TechTreeConfig : ScriptableObject
{
    public List<TechNodeConfig> nodes = new List<TechNodeConfig>();
    public List<BuildingData> initialUnlocks = new List<BuildingData>();
}

[System.Serializable]
public class TechNodeConfig
{
    public BuildingData building;
    public List<ItemStack> unlockCost = new List<ItemStack>();
    public List<BuildingData> prerequisites = new List<BuildingData>();
    
    [Header("UI Layout (optional)")]
    [Tooltip("手动指定该节点的层（0 开始）。-1 表示自动按依赖深度计算。")]
    public int overrideLevel = -1;

    [Tooltip("同层内的显示顺序，数值越小越靠左。默认 0。")]
    public int orderInLevel = 0;
}