using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionCostPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private ConstructionCostRow rowPrefab;

    private BuildingData _data;
    private readonly List<ConstructionCostRow> _rows = new();

    private void OnEnable()
    {
        Refresh();
    }

    public void SetData(BuildingData data)
    {
        _data = data;
        RebuildRows();
        Refresh();
    }

    /// <summary>重建行（更换建筑时调用）</summary>
    private void RebuildRows()
    {
        // 清空旧行
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        if (_data == null || _data.constructionCost == null || _data.constructionCost.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            gameObject.SetActive(true);
        }

        // 生成新行
        foreach (var cost in _data.constructionCost)
        {
            if (cost.item == null || cost.amount <= 0) continue;

            var row = Instantiate(rowPrefab, listContainer);
            int have = GetInventoryCount(cost.item);
            row.Bind(cost.item, cost.amount, have);
            _rows.Add(row);
        }
    }

    public void Refresh()
    {
        if (!gameObject.activeInHierarchy || _data == null) return;
        
        int idx = 0;
        foreach (var cost in _data.constructionCost)
        {
            if (cost.item == null || cost.amount <= 0) continue;
            if (idx >= _rows.Count) break;

            int have = GetInventoryCount(cost.item);
            _rows[idx].UpdateAmount(have);
            idx++;
        }
    }

    private int GetInventoryCount(ItemDef item)
    {
        var inv = InventoryManager.Instance;
        if (inv == null || item == null) return 0;
        return inv.GetItemCount(item);
    }
}
