using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct IOEntry {
    public ItemDef item;
    public int have;   // 当前缓存数量
    public int cap;    // 缓存上限（未知填 -1）
    public int want;   // 单次配方需要/产出（未知填 0）
    public string resourceKey; // 当 item == null 时，用它从 Resources 加载图标
}

public class ProductionInfo {
    public string displayName;
    public RecipeDef recipe;         // 可为空（例如发电站/垃圾房）
    public List<IOEntry> inputs = new();
    public List<IOEntry> outputs = new();
    public bool  isProducing;
    public float progress01;         // 0..1
    public string extraText;         // 额外说明（功率/速率等）
}

public interface IProductionInfoProvider {
    /// <summary>用于悬浮面板显示的生产/缓存信息</summary>
    ProductionInfo GetProductionInfo();
}