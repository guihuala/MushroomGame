using DG.Tweening;
using UnityEngine;

public partial class PlacementSystem
{
    #region 放置/擦除
    private void PlaceOne(Vector2Int cell, Vector3 worldPos, Vector2Int dirForThis)
    {
        // 1) 必须有 Data 和 Prefab
        if (_currentData == null || _currentData.prefab == null)
        {
            _dragLastCell = cell;
            return;
        }
        _currentPrefab = _currentData.prefab; // 确保一致

        // 2) 建造区域可行性
        if (!grid.AreCellsFree(cell, _currentPrefab.size, _currentPrefab))
        {
            _dragLastCell = cell;
            return;
        }

        // 3) 检查/扣除建造费用——只看 Data
        if (!_currentData.HasEnoughResources())
        {
            Debug.Log("资源不足，无法建造");
            _dragLastCell = cell;
            return;
        }
        if (!_currentData.DeductConstructionCost())
        {
            Debug.Log("扣除资源失败");
            _dragLastCell = cell;
            return;
        }

        // 4) 实例化并朝向

        var building = Instantiate(_currentData.prefab, worldPos, Quaternion.identity);
        
        var meta = building.gameObject.AddComponent<PlacedBuildingMeta>();
        meta.sourceData = _currentData;   // _currentData 为你当前要放置的 SO
        
        var t = building.transform;
        t.localScale = Vector3.zero;
        t.DOScale(1f, placeScaleDuration).SetEase(placeScaleEase);

        if (building is IOrientable orientable) 
        {
            orientable.SetDirection(dirForThis);
        }

        // 5) 占格 & 回调
        grid.OccupyCells(cell, building.size, building);
        building.OnPlaced(grid, cell);
        
        AudioManager.Instance.PlaySfx("Place");

        _dragLastCell = cell;
        _dragLastBuilding = building;
    }

    // 仅沿水平或垂直步进放置
    private void StepAndPlaceAlongLockedAxis(Vector2Int last, Vector2Int target)
    {
        var cur = last;
        while (cur != target)
        {
            Vector2Int step = (target.x != cur.x)
                ? new Vector2Int((int)Mathf.Sign(target.x - cur.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(target.y - cur.y));

            var next = cur + step;
            PlaceOne(next, grid.CellToWorld(next), _currentDir);
            cur = next;
        }
    }
    #endregion
}