using System.Collections.Generic;
using System.Linq;

public class TechTreeManager : Singleton<TechTreeManager>,IManager
{
    private HashSet<Building> unlockedBuildings = new HashSet<Building>();

    // 初始化方法
    public void Initialize()
    {
        DebugManager.Log("TechTreeManager initialized");

        unlockedBuildings.Clear();

        // UnlockInitialBuildings();
    }
    
    public void UnlockBuilding(Building building)
    {
        if (!unlockedBuildings.Contains(building))
        {
            unlockedBuildings.Add(building);
            DebugManager.Log($"Building {building.name} unlocked!");
        }
    }

    public List<Building> GetUnlockedBuildings()
    {
        return unlockedBuildings.ToList();
    }
}