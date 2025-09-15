using UnityEngine;

[DisallowMultipleComponent]
public class PreviewDirectionArrow : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Vector2 _localOffset = new(0f, 0.3f);

    public void Initialize(
        Sprite sprite,
        SpriteRenderer baseRendererForSorting,
        Vector2 localOffset,
        float scale = 1f,
        string sortingLayer = "",
        int orderInLayerOffset = 100
    )
    {
        _sr = gameObject.GetComponent<SpriteRenderer>();
        if (!_sr) _sr = gameObject.AddComponent<SpriteRenderer>();

        _sr.sprite = sprite;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        _localOffset = localOffset;

        // 排序层：优先使用配置名，否则跟随主体
        if (!string.IsNullOrEmpty(sortingLayer))
            _sr.sortingLayerName = sortingLayer;
        else if (baseRendererForSorting)
            _sr.sortingLayerID = baseRendererForSorting.sortingLayerID;

        _sr.sortingOrder = (baseRendererForSorting ? baseRendererForSorting.sortingOrder : 0) + orderInLayerOffset;
        
        transform.localPosition = _localOffset;
        transform.localRotation = Quaternion.identity;
    }
}