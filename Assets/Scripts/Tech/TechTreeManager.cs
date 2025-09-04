using System.Collections.Generic;
using System.Linq;

public class TechTreeManager : Singleton<TechTreeManager>
{
    private HashSet<Building> unlockedBuildings = new HashSet<Building>();

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