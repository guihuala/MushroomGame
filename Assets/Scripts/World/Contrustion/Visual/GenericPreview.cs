using UnityEngine;
using DG.Tweening;

public class GenericPreview : MonoBehaviour
{
    [Header("预览设置")]
    public Color validColor = new Color(0, 1, 0, 0.5f);    // 可放置颜色
    public Color invalidColor = new Color(1, 0, 0, 0.5f);  // 不可放置颜色
    private bool _isRotationEnabled = true; // 是否允许旋转
    private SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyPreviewColor(validColor);
    }

    public void SetRotationEnabled(bool enabled)
    {
        if (_spriteRenderer == null) return;
        _isRotationEnabled = enabled;
        transform.DORotate(new Vector3(0, 0, 0), 0.3f)
            .SetEase(Ease.OutBack);
    }
    
    public void SetDirection(Vector2Int direction)
    {
        if (!_isRotationEnabled) return;
        
        float targetAngle = GetRotationAngleFromDirection(direction);
        float currentAngle = transform.eulerAngles.z;

        float deltaAngle = Mathf.DeltaAngle(currentAngle, targetAngle);

        transform.DORotate(new Vector3(0, 0, targetAngle), 0.3f)
            .SetEase(Ease.OutBack);
    }

    private float GetRotationAngleFromDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.right) return 0f;
        if (direction == Vector2Int.up)    return 90f;
        if (direction == Vector2Int.left)  return 180f;
        if (direction == Vector2Int.down)  return 270f;
        return 0f;
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
        if (_spriteRenderer != null)
        {
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (_spriteRenderer != null && icon != null)
        {
            _spriteRenderer.sprite = icon;
        }
    }
}
