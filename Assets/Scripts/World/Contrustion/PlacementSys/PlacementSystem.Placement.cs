using UnityEngine;

public partial class PlacementSystem
{
    #region 放置/擦除
    private void PlaceOne(Vector2Int cell, Vector3 worldPos, Vector2Int dirForThis, bool adjustPrev)
    {
        // 1) 统一可行性：按“建筑类型 + 占地矩形”判断
        if (_currentPrefab == null || !grid.AreCellsFree(cell, _currentPrefab.size, _currentPrefab))
        {
            _dragLastCell = cell;
            return;
        }

        // 2) 实例化并设置朝向
        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
        if (building is IOrientable orientable) orientable.SetDirection(dirForThis);

        // 3) 占格 & 回调
        grid.OccupyCells(cell, building.size, building);
        building.OnPlaced(grid, cell);

        // 4) 拖动时让上一件顺着方向
        if (adjustPrev && _dragLastBuilding is IOrientable prevOrient)
        {
            var delta = NormalizeToCardinal(cell - _dragLastCell);
            if (delta != Vector2Int.zero) prevOrient.SetDirection(delta);
        }

        _dragLastCell = cell;
        _dragLastBuilding = building;
    }
    
    private void EraseOne(Vector2Int cell)
    {
        var building = grid.GetBuildingAt(cell);
        if (building != null && !_pendingEraseBuildings.Contains(building))
        {
            MarkBuildingForErase(building);
        }
        _dragLastCell = cell;
    }

    private void EraseArea(Vector2Int start, Vector2Int end)
    {
        Vector2Int min = Vector2Int.Min(start, end);
        Vector2Int max = Vector2Int.Max(start, end);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                EraseOne(cell);
            }
        }
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