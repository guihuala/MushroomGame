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
            MsgCenter.SendMsg(MsgConst.HUB_CLICKED, _hub);
        }
    }
    
    // 确保有碰撞器用于点击检测
    private void EnsureCollider()
    {
        var existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            var collider = gameObject.AddComponent<BoxCollider2D>();
            
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
}