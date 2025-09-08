using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public abstract class MushroomBuilding : Building
{
    [Header("蘑菇特有属性")]
    public float growthTime = 60f; // 生长时间（秒）
    public int yieldAmount = 1; // 产量
    
    // 蘑菇特有的生长状态
    public enum GrowthStage { Seedling, Growing, Mature, Harvestable }
    public GrowthStage currentStage = GrowthStage.Seedling;
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        // 蘑菇使用特殊的建造检查
        
        base.OnPlaced(g, c);
        StartGrowthCycle();
    }
    
    protected virtual void StartGrowthCycle()
    {
        // 实现蘑菇生长逻辑
        Invoke(nameof(AdvanceToNextStage), growthTime / 3f);
    }
    
    protected virtual void AdvanceToNextStage()
    {
        // 生长阶段推进逻辑
        switch (currentStage)
        {
            case GrowthStage.Seedling:
                currentStage = GrowthStage.Growing;
                Invoke(nameof(AdvanceToNextStage), growthTime / 3f);
                break;
            case GrowthStage.Growing:
                currentStage = GrowthStage.Mature;
                Invoke(nameof(AdvanceToNextStage), growthTime / 3f);
                break;
            case GrowthStage.Mature:
                currentStage = GrowthStage.Harvestable;
                OnReadyForHarvest();
                break;
        }
        
        UpdateVisuals();
    }
    
    protected virtual void UpdateVisuals()
    {
        // 根据生长阶段更新外观
    }
    
    protected virtual void OnReadyForHarvest()
    {
        // 蘑菇成熟，可以收获
    }
    
    public virtual bool TryHarvest()
    {
        if (currentStage != GrowthStage.Harvestable) return false;
        
        // 收获逻辑
        currentStage = GrowthStage.Seedling;
        StartGrowthCycle();
        return true;
    }
}
