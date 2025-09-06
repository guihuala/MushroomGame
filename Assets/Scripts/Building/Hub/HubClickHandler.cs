using UnityEngine;
using UnityEngine.EventSystems;

public class HubClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Hub _hub;
    
    private void Start()
    {
        _hub = GetComponent<Hub>();
        if (_hub == null)
        {
            return;
        }
        
        // 确保有碰撞器用于点击检测
        EnsureCollider();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && _hub != null)
        {
            MsgCenter.SendMsg(MsgConst.MSG_HUB_CLICKED, _hub);
        }
    }
    
    // 确保有碰撞器用于点击检测
    private void EnsureCollider()
    {
        // 检查是否已有2D碰撞器
        var existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            // 添加BoxCollider2D，大小匹配Sprite
            var collider = gameObject.AddComponent<BoxCollider2D>();
            
            // 获取Sprite的尺寸来自动设置碰撞器大小
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
            }
            else
            {
                // 默认大小
                collider.size = new Vector2(1f, 1f);
            }
        }
    }
    
    public void UpdateColliderSize()
    {
        var collider = GetComponent<BoxCollider2D>();
        var spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (collider != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            collider.size = spriteRenderer.sprite.bounds.size;
        }
    }
}