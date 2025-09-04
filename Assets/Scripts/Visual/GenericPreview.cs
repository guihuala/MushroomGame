using UnityEngine;

public class GenericPreview : MonoBehaviour, IOrientable
{
    [Header("预览设置")]
    public Color validColor = new Color(0, 1, 0, 0.5f);    // 可放置颜色
    public Color invalidColor = new Color(1, 0, 0, 0.5f);  // 不可放置颜色
    
    private SpriteRenderer _spriteRenderer;
    private Vector2Int _currentDirection = Vector2Int.right;
    
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyPreviewColor(validColor);
    }
    
    public void SetDirection(Vector2Int direction)
    {
        _currentDirection = direction;
        transform.right = new Vector3(direction.x, direction.y, 0f);
    }

    public void SetPreviewState(bool canPlace)
    {
        ApplyPreviewColor(canPlace ? validColor : invalidColor);
    }
    
    private void ApplyPreviewColor(Color color)
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
        }
    }
    
    public void SetSize(Vector2Int size)
    {
        // 可以根据建筑尺寸调整预览大小（可选）
        if (_spriteRenderer != null)
        {
            // 这里可以根据需要调整预览的尺寸
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }
    
    /// <summary>
    /// 设置预览图标
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (_spriteRenderer != null && icon != null)
        {
            _spriteRenderer.sprite = icon;
        }
    }
    
    public Vector2Int GetCurrentDirection() => _currentDirection;
}