using DG.Tweening;
using UnityEngine;

public partial class PlacementSystem
{
    #region 预览
    
    private void ClearPreview()
    {
        if (_currentPreview == null) return;
        _currentMoveTween?.Kill();
        _currentRotateTween?.Kill();
        Destroy(_currentPreview.gameObject);
        _currentPreview = null;
    }

    private void UpdatePreview(Vector3 worldPos, Vector2Int cell)
    {
        if (_currentPrefab == null) return;

        if (_currentPreview == null) CreatePreview(worldPos);
        else SmoothMovePreview(worldPos);

        if (_currentPreview == null) return;

        ApplyPreviewPalette();
        bool ok = EvaluatePlacementOK(cell);

        TintPreview(ok);
    }

    private void CreatePreview(Vector3 position)
    {
        if (_currentPrefab == null || previewPrefab == null) return;

        if (_currentPreview == null)
        {
            var previewObject = Instantiate(previewPrefab, position, Quaternion.identity);
            _currentPreview = previewObject.GetComponent<GenericPreview>();
            if (_currentPreview == null) _currentPreview = previewObject.AddComponent<GenericPreview>();

            _currentPreview.validColor = previewValidColor;
            _currentPreview.invalidColor = previewInvalidColor;
            _origValid = _currentPreview.validColor;
            _origInvalid = _currentPreview.invalidColor;

            _currentPreview.SetDirection(_currentDir);
            _currentPreview.SetSize(_currentPrefab.size);
            SetPreviewIcon();
        }
    }
    
    private void SetPreviewIcon()
    {
        if (_currentPreview == null) return;
        if (_currentData != null && _currentData.icon != null)
            _currentPreview.SetIcon(_currentData.icon);
    }
    
    private bool EvaluatePlacementOK(Vector2Int cell)
    {
        if (_currentPrefab == null) return false;
        return grid.AreCellsFree(cell, _currentPrefab.size, _currentPrefab);
    }

    private void SmoothMovePreview(Vector3 targetPosition)
    {
        if (_currentPreview == null) return;
        _currentMoveTween?.Kill();
        _currentMoveTween = _currentPreview.transform
            .DOMove(targetPosition, previewMoveDuration)
            .SetEase(moveEase);
    }

    private void ApplyPreviewPalette()
    {
        if (_currentPreview == null) return;
        _currentPreview.validColor = _origValid;
        _currentPreview.invalidColor = _origInvalid;
    }

    private void TintPreview(bool ok)
    {
        if (_currentPreview == null) return;
        Color c = ok ? _currentPreview.validColor : _currentPreview.invalidColor;
        var renderers = _currentPreview.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers) r.color = c;
    }
    
    private void RotatePreview()
    {
        Vector2Int[] rotationCycle = { Vector2Int.up ,Vector2Int.right, Vector2Int.down, Vector2Int.left};
        int currentIndex = System.Array.IndexOf(rotationCycle, _currentDir);
        int nextIndex = (currentIndex + 1) % rotationCycle.Length;
        SetDirection(rotationCycle[nextIndex]);
    }

    private void SetDirection(Vector2Int direction)
    {
        _currentDir = direction;

        if (_currentPreview == null) return;
        _currentPreview.SetDirection(direction);
    }
    
    private Vector3 ComputeVisualOffsetWorld(Building prefab, Vector2Int cell, Vector2Int dir)
    {
        if (!prefab) return Vector3.zero;

        // 读取锚点设置（没有则不偏移）
        var anchor = prefab.GetComponent<VisualAnchor2D>();
        if (!anchor) return Vector3.zero;

        // 旋转步数（Up=0, Right=1, Down=2, Left=3）
        int steps = DirToSteps(dir);

        // 当前朝向下的 footprint 尺寸（90/270 时需交换）
        Vector2Int size = prefab.size;
        if ((steps & 1) == 1) size = new Vector2Int(size.y, size.x);

        // 以“格”为单位的偏移（从 footprint 左下角格中心出发）
        Vector2 offsetCells = new Vector2(
            (size.x - 1) * anchor.anchor01.x,
            (size.y - 1) * anchor.anchor01.y
        ) + anchor.extraOffsetCells;

        // 将 offsetCells 根据旋转步数旋转到世界网格坐标系（以格为单位）
        Vector2 offsetCellsRot = RotateGridVec(offsetCells, steps);

        // 把“每格向量”换算到世界单位
        var origin = grid.CellToWorld(cell);
        var toRight = grid.CellToWorld(cell + Vector2Int.right) - origin;
        var toUp    = grid.CellToWorld(cell + Vector2Int.up)    - origin;

        return toRight * offsetCellsRot.x + toUp * offsetCellsRot.y;
    }

    private static int DirToSteps(Vector2Int dir)
    {
        if (dir == Vector2Int.up)    return 0;
        if (dir == Vector2Int.right) return 1;
        if (dir == Vector2Int.down)  return 2;
        if (dir == Vector2Int.left)  return 3;
        return 0;
    }

    private static Vector2 RotateGridVec(Vector2 v, int steps)  // 以格为单位的 90° 旋转
    {
        steps = ((steps % 4) + 4) % 4;
        switch (steps)
        {
            case 0: return v;
            case 1: return new Vector2(v.y, -v.x);
            case 2: return new Vector2(-v.x, -v.y);
            case 3: return new Vector2(-v.y, v.x);
        }
        return v;
    }

    #endregion
}