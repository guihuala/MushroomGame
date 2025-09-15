using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ItemPortIconRenderer : MonoBehaviour
{
    public MushroomBuilding building;      // 也可做成 MultiGridBuilding
    public PortSpriteSet sprites;          // 上一步创建的配置
    [Tooltip("仅在运行时显示")]
    public bool runtimeOnly = true;

    readonly List<PortWorldInfo> _ports = new();
    readonly List<SpriteRenderer> _icons = new();

    int _lastRotationSteps = int.MinValue;
    Vector2Int _lastCell;

    void Reset()
    {
        if (!building) building = GetComponent<MushroomBuilding>();
    }

    void OnEnable()
    {
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

        // 轮询“是否需要刷新”：旋转/位置变化
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

        UpdateIcons(); // 立即刷新一次
    }

    void UpdateIcons()
    {
        if (!building || !sprites) return;
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
            _icons[i].transform.position = p;

            // 朝向 → 旋转
            float z = info.side switch
            {
                CellSide.Up    => 0f,
                CellSide.Right => -90f,
                CellSide.Down  => 180f,
                CellSide.Left  => 90f,
                _ => 0f
            };
            _icons[i].transform.rotation = Quaternion.Euler(0, 0, z);

            // 尺寸
            _icons[i].transform.localScale = Vector3.one * Mathf.Max(0.01f, sprites.worldScale);

            // 排序层
            if (building.TryGetComponent<SpriteRenderer>(out var br))
            {
                _icons[i].sortingLayerID = string.IsNullOrEmpty(sprites.sortingLayer)
                    ? br.sortingLayerID
                    : SortingLayer.NameToID(sprites.sortingLayer);
                _icons[i].sortingOrder = br.sortingOrder + sprites.orderInLayerOffset;
            }
        }

        // 如果端口数量变了（理论上你这类建筑是固定的2个），也能自适应
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
