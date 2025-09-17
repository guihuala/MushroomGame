using UnityEngine;
using DG.Tweening;

public class GenericPreview : MonoBehaviour
{
    public enum FitMode
    {
        FitWidth,   // 等比缩放，使宽度刚好覆盖 size.x * cellWorldSize
        FitHeight,  // 等比缩放，使高度刚好覆盖 size.y * cellWorldSize
        Contain,    // 等比缩放，整体“完全放入”目标宽高（不裁切，可能留空白）
        Cover       // 等比缩放，整体“完全覆盖”目标宽高（可能超出一边）
    }

    [Header("预览设置")]
    public Color validColor = new Color(0, 1, 0, 0.5f);
    public Color invalidColor = new Color(1, 0, 0, 0.5f);

    [Tooltip("单个格子的世界尺寸（单位：世界坐标）。Tile 一格=1时保持1即可。")]
    public float cellWorldSize = 1f;

    [Tooltip("等比缩放模式")]
    public FitMode fitMode = FitMode.FitWidth;
    
    [Header("Direction Arrow (Preview-time)")]
    [SerializeField] private Sprite directionArrowSprite;
    [SerializeField] private Vector2 arrowLocalOffset = new(0f, 0.6f);
    [SerializeField] private float arrowScale = 1f;
    [SerializeField] private string arrowSortingLayer = "Preview";
    [SerializeField] private int arrowOrderInLayerOffset = 100;

    private bool _isRotationEnabled = true;
    private SpriteRenderer _spriteRenderer;
    
    private Vector2 _spriteLocalSize = Vector2.one;
    
    private PreviewDirectionArrow _dirArrow;   // 运行时箭头实例
    private SpriteRenderer _baseRendererForSorting; // 参考排序的渲染器
    
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        CacheSpriteLocalSize();
        ApplyPreviewColor(validColor);
    }

    void OnValidate()
    {
        CacheSpriteLocalSize();
    }

    void CacheSpriteLocalSize()
    {
        if (_spriteRenderer && _spriteRenderer.sprite)
        {
            var b = _spriteRenderer.sprite.bounds;
            _spriteLocalSize = b.size;
            if (_spriteLocalSize.x <= 0f) _spriteLocalSize.x = 1f;
            if (_spriteLocalSize.y <= 0f) _spriteLocalSize.y = 1f;
        }
    }

    public void SetRotationEnabled(bool enabled)
    {
        if (_spriteRenderer == null) return;
        _isRotationEnabled = enabled;

        transform.DOKill(true);
        transform.DORotate(new Vector3(0, 0, 0), 0.3f).SetEase(Ease.OutBack);

        EnsureDirectionArrow();
    }
    
    public void SetDirection(Vector2Int direction)
    {
        if (!_isRotationEnabled) { RemoveDirectionArrow(); return; } // 不可旋转时，不显示箭头

        float targetAngle = GetRotationAngleFromDirection(direction);
        transform.DOKill(true);
        transform.DORotate(new Vector3(0, 0, targetAngle), 0.3f).SetEase(Ease.OutBack);
    }

    private float GetRotationAngleFromDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return 0f;
        if (direction == Vector2Int.right)    return 270f;
        if (direction == Vector2Int.down)  return 180f;
        if (direction == Vector2Int.left)  return 90f;
        return 0f;
    }
    
    private void ApplyPreviewColor(Color color)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = color;
    }

    /// <summary>
    /// 用等比缩放匹配目标占地尺寸（单位：格子数）。
    /// </summary>
    public void SetSize(Vector2Int size)
    {
        if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

        // 目标世界尺寸（希望预览覆盖的宽/高）
        float targetW = Mathf.Max(0.0001f, size.x) * Mathf.Max(0.0001f, cellWorldSize);
        float targetH = Mathf.Max(0.0001f, size.y) * Mathf.Max(0.0001f, cellWorldSize);

        // 精灵在 scale=1 时的世界尺寸
        float spriteW = Mathf.Max(0.0001f, _spriteLocalSize.x);
        float spriteH = Mathf.Max(0.0001f, _spriteLocalSize.y);

        // 比例因子：等比缩放 → X/Y 使用同一个 scale
        float scale;
        switch (fitMode)
        {
            case FitMode.FitWidth:
                scale = targetW / spriteW;
                break;
            case FitMode.FitHeight:
                scale = targetH / spriteH;
                break;
            case FitMode.Contain:
                scale = Mathf.Min(targetW / spriteW, targetH / spriteH);
                break;
            case FitMode.Cover:
            default:
                scale = Mathf.Max(targetW / spriteW, targetH / spriteH);
                break;
        }

        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void SetIcon(Sprite icon)
    {
        if (_spriteRenderer == null) return;
        if (icon == null) return;

        _spriteRenderer.sprite = icon;
        CacheSpriteLocalSize();
    }

    #region 箭头
        
    public void ConfigureDirectionArrow(
        Sprite sprite, SpriteRenderer baseRendererForSorting,
        Vector2 localOffset, float scale, string sortingLayer, int orderOffset)
    {
        directionArrowSprite = sprite;
        _baseRendererForSorting = baseRendererForSorting;
        arrowLocalOffset = localOffset;
        arrowScale = scale;
        arrowSortingLayer = sortingLayer;
        arrowOrderInLayerOffset = orderOffset;

        EnsureDirectionArrow(); // 根据当前是否可旋转决定创建/移除
    }
    
    private void EnsureDirectionArrow()
    {
        // 只有 可旋转 + 有配置的sprite 时才显示箭头
        if (!_isRotationEnabled || directionArrowSprite == null) { RemoveDirectionArrow(); return; }

        if (_dirArrow == null)
        {
            var go = new GameObject("PreviewDirectionArrow");
            go.transform.SetParent(transform, false);
            _dirArrow = go.AddComponent<PreviewDirectionArrow>();
            
            var refRenderer = _baseRendererForSorting != null ? _baseRendererForSorting : GetComponentInChildren<SpriteRenderer>();

            _dirArrow.Initialize(
                directionArrowSprite,
                refRenderer,
                arrowLocalOffset,
                arrowScale,
                arrowSortingLayer,
                arrowOrderInLayerOffset
            );
        }
    }

    private void RemoveDirectionArrow()
    {
        if (_dirArrow != null)
        {
            DestroyImmediate(_dirArrow.gameObject);
            _dirArrow = null;
        }
    }

    #endregion
}