using UnityEngine;

public partial class PlacementSystem
{
    #region 拆除撤销系统
    
    // 标记待拆除建筑
    private void MarkBuildingForErase(Building building)
    {
        if (building == null || _pendingEraseBuildings.Contains(building)) return;

        _pendingEraseBuildings.Add(building);
        
        // 保存原始颜色并设置待拆除颜色
        var renderers = building.GetComponentsInChildren<SpriteRenderer>();
        Color[] originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].color = pendingEraseColor;
        }
        
        _originalBuildingColors.Add(originalColors);
    }

    // 取消拆除
    private void CancelErase()
    {
        if (_pendingEraseBuildings.Count == 0) return;

        _isEraseCancelled = true;
        
        // 恢复建筑颜色
        int colorIndex = 0;
        foreach (var building in _pendingEraseBuildings)
        {
            if (building != null)
            {
                var renderers = building.GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (colorIndex < _originalBuildingColors.Count && 
                        i < _originalBuildingColors[colorIndex].Length)
                    {
                        renderers[i].color = _originalBuildingColors[colorIndex][i];
                    }
                }
            }
            colorIndex++;
        }

        ClearPendingErase();
    }

    // 执行拆除
    private void ConfirmErase()
    {
        foreach (var building in _pendingEraseBuildings)
        {
            if (building != null)
            {
                building.OnRemoved();
            }
        }
        
        ClearPendingErase();
    }

    // 清空待拆除列表
    private void ClearPendingErase()
    {
        _pendingEraseBuildings.Clear();
        _originalBuildingColors.Clear();
        _isEraseCancelled = false;
    }

    #endregion
}