using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ItemPortIconRenderer : MonoBehaviour
{
    public MushroomBuilding building;
    public PortSpriteSet sprites;

    [Tooltip("用于决定图标的排序层与序号；不指定则自动从子物体中查找一个SpriteRenderer。")]
    public SpriteRenderer baseRendererForSorting;

    [Tooltip("仅在运行时显示")]
    public bool runtimeOnly = true;

    readonly List<PortWorldInfo> _ports = new();
    readonly List<SpriteRenderer> _icons = new();

    int _lastRotationSteps = int.MinValue;
    Vector2Int _lastCell;

    void Reset()
    {
        if (!building) building = GetComponent<MushroomBuilding>();
        if (!baseRendererForSorting) baseRendererForSorting = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (!baseRendererForSorting) baseRendererForSorting = GetComponentInChildren<SpriteRenderer>();
        RebuildIcons();
    }

    void OnDisable()
    {
        ClearIcons();
    }

    void LateUpdate()
    {
        if (runtimeOnly && !Application.isPlaying) { HideIcons(); return; }

        if (!building || !sprites) { HideIcons(); return; }

        if (_lastRotationSteps != building.rotationSteps || _lastCell != building.cell)
        {
            RebuildIcons();
        }

        UpdateIcons();
    }

    void RebuildIcons()
    {
        ClearIcons();
        if (!building || !sprites) return;

        building.GetPortWorldInfos(_ports);
        for (int i = 0; i < _ports.Count; i++)
        {
            var go = new GameObject($"PortIcon_{i}");
            go.transform.SetParent(transform, worldPositionStays: false);

            var sr = go.AddComponent<SpriteRenderer>();
            _icons.Add(sr);
        }

        _lastRotationSteps = building.rotationSteps;
        _lastCell = building.cell;

        UpdateIcons();
    }

    void UpdateIcons()
    {
        if (!building || !sprites) return;

        // 确保参考渲染器可用（避免在不同Prefab层级下找不到）
        if (!baseRendererForSorting) baseRendererForSorting = GetComponentInChildren<SpriteRenderer>(); // NEW

        building.GetPortWorldInfos(_ports);

        for (int i = 0; i < _icons.Count && i < _ports.Count; i++)
        {
            var info = _ports[i];
            var sr = _icons[i];
            if (!sr) continue;

            // 选择精灵
            sr.sprite = (info.type == PortType.Input) ? sprites.inputSprite : sprites.outputSprite;

            // 位置
            Vector3 p = info.worldPos + (Vector3)sprites.worldOffset;
            sr.transform.position = p;

            sr.transform.rotation = Quaternion.Euler(0, 0, 0);
            
            if (!string.IsNullOrEmpty(sprites.sortingLayer))
            {
                sr.sortingLayerName = sprites.sortingLayer;
            }
            else if (baseRendererForSorting)
            {
                sr.sortingLayerID = baseRendererForSorting.sortingLayerID;
            }
            
            int baseOrder = baseRendererForSorting ? baseRendererForSorting.sortingOrder : 0;
            sr.sortingOrder = baseOrder + sprites.orderInLayerOffset;
        }

        // 多余图标隐藏
        for (int i = _ports.Count; i < _icons.Count; i++)
        {
            if (_icons[i]) _icons[i].enabled = false;
        }
    }

    void ClearIcons()
    {
        for (int i = 0; i < _icons.Count; i++)
        {
            if (_icons[i]) DestroyImmediate(_icons[i].gameObject);
        }
        _icons.Clear();
    }

    void HideIcons()
    {
        foreach (var sr in _icons) if (sr) sr.enabled = false;
    }
}
