using UnityEngine;

public partial class PlacementSystem
{
    #region 放置/擦除
    private void PlaceOne(Vector2Int cell, Vector3 worldPos, Vector2Int dirForThis, bool adjustPrev)
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
        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
        if (building is IOrientable orientable) orientable.SetDirection(dirForThis);

        // 5) 占格 & 回调
        grid.OccupyCells(cell, building.size, building);
        building.OnPlaced(grid, cell);

        // 6) 拖线时让上一件顺向
        if (adjustPrev && _dragLastBuilding is IOrientable prevOrient)
        {
            var delta = NormalizeToCardinal(cell - _dragLastCell);
            if (delta != Vector2Int.zero) prevOrient.SetDirection(delta);
        }

        _dragLastCell = cell;
        _dragLastBuilding = building;
    }

    // 仅沿水平或垂直（锁定轴）步进放置
    private void StepAndPlaceAlongLockedAxis(Vector2Int last, Vector2Int target)
    {
        var cur = last;
        while (cur != target)
        {
            Vector2Int step = (target.x != cur.x)
                ? new Vector2Int((int)Mathf.Sign(target.x - cur.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(target.y - cur.y));

            var next = cur + step;
            PlaceOne(next, grid.CellToWorld(next), step, adjustPrev: true);
            cur = next;
        }
    }
    
    private static Vector2Int NormalizeToCardinal(Vector2Int delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int((int)Mathf.Sign(delta.x), 0);
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            return new Vector2Int(0, (int)Mathf.Sign(delta.y));
        return Vector2Int.zero;
    }
    #endregion
}