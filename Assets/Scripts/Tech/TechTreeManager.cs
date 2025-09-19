using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class TechNode
{
    public BuildingData building;
    public List<ItemStack> unlockCost;
    public List<TechNode> prerequisites;
    public bool isUnlocked;
    public bool isUsable;

    public TechNode(BuildingData building, List<ItemStack> cost, bool initialUnlock = false)
    {
        this.building = building;
        unlockCost = cost ?? new List<ItemStack>();
        prerequisites = new List<TechNode>();
        isUnlocked = initialUnlock;
    }

    public bool CanUnlock()
    {
        if (isUnlocked) return false;
        
        foreach (var prereq in prerequisites)
        {
            if (!prereq.isUnlocked) return false;
        }
        
        foreach (var cost in unlockCost)
        {
            if (cost.item == null || !InventoryManager.Instance.HasEnoughItemStack(cost))
                return false;
        }
        
        return true;
    }

    public bool TryUnlock()
    {
        if (!CanUnlock()) return false;
        
        foreach (var cost in unlockCost)
        {
            InventoryManager.Instance.RemoveItemStack(cost);
        }
        
        isUnlocked = true;
        return true;
    }
    
    public bool HasParent()
    {
        return prerequisites.Count > 0;
    }
    
    public TechNode Parent()
    {
        return prerequisites.FirstOrDefault();
    }
}

public class TechTreeManager : Singleton<TechTreeManager>, IManager
{
    [SerializeField] private TechTreeConfig techTreeConfig;
    
    private Dictionary<BuildingData, TechNode> techTree = new Dictionary<BuildingData, TechNode>();
    private HashSet<BuildingData> unlockedBuildings = new HashSet<BuildingData>();
    
    private Dictionary<BuildingData, int> _overrideLevels = new Dictionary<BuildingData, int>();

    public void Initialize()
    {
        InitializeTechTree();
        UnlockInitialBuildings();
        BuildingSelectionUI.Instance.InitializeUI();
    }

    private void InitializeTechTree()
    {
        techTree.Clear();
        unlockedBuildings.Clear();

        // 首先创建所有节点
        foreach (var nodeConfig in techTreeConfig.nodes)
        {
            var node = new TechNode(
                nodeConfig.building, 
                nodeConfig.unlockCost,
                techTreeConfig.initialUnlocks.Contains(nodeConfig.building)
            );
            techTree[nodeConfig.building] = node;
        }

        // 然后设置前置关系
        SetupPrerequisites();
    }

    private void SetupPrerequisites()
    {
        foreach (var nodeConfig in techTreeConfig.nodes)
        {
            var currentNode = techTree[nodeConfig.building];
            
            foreach (var prereqBuilding in nodeConfig.prerequisites)
            {
                if (techTree.ContainsKey(prereqBuilding))
                {
                    currentNode.prerequisites.Add(techTree[prereqBuilding]);
                }
                else
                {
                    Debug.LogWarning($"前置建筑 {prereqBuilding.buildingName} 未在科技树中定义");
                }
            }
        }
    }
    
    public int GetNodeLevel(BuildingData building)
    {
        if (_overrideLevels.TryGetValue(building, out var ov) && ov >= 0)
            return ov;
        return GetNodeDepth(building);
    }
    
    public int GetOrderInLevel(BuildingData building)
    {
        var cfg = techTreeConfig.nodes.FirstOrDefault(n => n.building == building);
        return cfg != null ? cfg.orderInLevel : 0;
    }

    private void UnlockInitialBuildings()
    {
        foreach (var building in techTreeConfig.initialUnlocks)
        {
            if (techTree.ContainsKey(building))
            {
                techTree[building].isUnlocked = true;
                unlockedBuildings.Add(building);
            }
        }
    }

    public TechNode GetTechNode(BuildingData building)
    {
        return techTree.ContainsKey(building) ? techTree[building] : null;
    }

    public bool UnlockBuilding(BuildingData building)
    {
        var node = GetTechNode(building);
        if (node == null || !node.TryUnlock()) return false;
        
        unlockedBuildings.Add(building);
        
        MsgCenter.SendMsg(MsgConst.BUILDING_UNLOCKED, building);
        return true;
    }
    
    public List<BuildingData> GetUnlockedBuildings()
    {
        return unlockedBuildings.ToList();
    }

    public List<BuildingData> GetUnlockedBuildingsByCategory(BuildingCategory category)
    {
        return unlockedBuildings
            .Where(b => b.category == category)
            .ToList();
    }
    
    public Dictionary<BuildingData, TechNode> GetTechTreeStructure()
    {
        return new Dictionary<BuildingData, TechNode>(techTree);
    }
    
    // 获取节点的层级深度（用于UI布局）
    public int GetNodeDepth(BuildingData building)
    {
        var node = GetTechNode(building);
        if (node == null || node.prerequisites.Count == 0) return 0;

        int maxDepth = 0;
        foreach (var prereq in node.prerequisites)
        {
            maxDepth = Mathf.Max(maxDepth, GetNodeDepth(prereq.building) + 1);
        }
        return maxDepth;
    }
}