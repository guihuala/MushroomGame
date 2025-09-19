using DG.Tweening;
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

        // 第一次标记建筑时发送显示提示事件
        if (_pendingEraseBuildings.Count == 1)
        {
            MsgCenter.SendMsg(MsgConst.ERASE_CANCEL_SHOW_HINT, eraseConfirmDuration);
        }
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

        // 发送取消事件
        MsgCenter.SendMsg(MsgConst.ERASE_CANCELLED);

        ClearPendingErase();
    }

    // 执行拆除
    private void ConfirmErase()
    {
        foreach (var building in _pendingEraseBuildings)
        {
            if (!building) continue;
            StartCoroutine(EraseOneWithAnimAndRefund(building));
        }

        MsgCenter.SendMsg(MsgConst.ERASE_CONFIRMED);
        ClearPendingErase();
    }

    private System.Collections.IEnumerator EraseOneWithAnimAndRefund(Building building)
    {
        var t = building.transform;
        t.DOKill();
        yield return t.DOScale(0f, eraseScaleDuration).SetEase(eraseScaleEase).WaitForCompletion();

        TryRefundFor(building);
        building.OnRemoved();

        ParticleManager.Instance.PlayEffect("Smoke 2", building.transform.position);
    }

    private void TryRefundFor(Building building)
    {
        var meta = building.GetComponent<PlacedBuildingMeta>();
        var data = meta ? meta.sourceData : null;
        if (!data) return;
        
        foreach (var cost in data.constructionCost)
        {
            if (cost.Equals(null) || cost.item == null || cost.amount <= 0) continue;
            int refund = Mathf.RoundToInt(cost.amount * refundRatio); // 四舍五入
            if (refund > 0)
            {
                InventoryManager.Instance.AddItem(cost.item, refund);
            }
        }
    }
    
    // 清空待拆除列表
    private void ClearPendingErase()
    {
        // 发送隐藏提示事件
        MsgCenter.SendMsg(MsgConst.ERASE_CANCEL_HIDE_HINT);

        _pendingEraseBuildings.Clear();
        _originalBuildingColors.Clear();
        _isEraseCancelled = false;
    }

    #endregion
}