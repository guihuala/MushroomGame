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

    #endregion
}