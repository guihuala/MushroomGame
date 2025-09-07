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
    public Vector2 editorPosition; // 用于编辑器中的位置
}