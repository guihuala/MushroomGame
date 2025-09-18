using UnityEngine;
using UnityEngine.EventSystems;

public class HubHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Hover UI")]
    [SerializeField] private TaskPanel taskPanel;
    [SerializeField] private float hoverDelay = 0.1f;
    
    private Hub _hub;
    private bool _isPointerInside;
    private float _hoverTimer;

    private void Start()
    {
        _hub = GetComponent<Hub>();
        EnsureCollider();
    }

    private void Update()
    {
        if (_hub == null || taskPanel == null) return;

        if (_isPointerInside)
        {
            _hoverTimer += Time.unscaledDeltaTime;
            
            if (_hoverTimer >= hoverDelay)
            {
                Vector2 mouse = Input.mousePosition;

                if (!taskPanel.gameObject.activeSelf)
                {
                    taskPanel.Initialize(_hub);
                    taskPanel.ShowAtScreenPosition(mouse);
                }
                else
                {
                    taskPanel.FollowMouse(mouse);
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        _hoverTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        _hoverTimer = 0f;

        if (taskPanel != null)
        {
            taskPanel.ClosePanel();
        }
    }
    
    public void OnPointerMove(PointerEventData eventData)
    {
        if (_isPointerInside && taskPanel != null && taskPanel.gameObject.activeSelf)
        {
            taskPanel.FollowMouse(eventData.position);
        }
    }

    // 确保有碰撞器用于指针事件检测
    private void EnsureCollider()
    {
        var existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            var collider = gameObject.AddComponent<BoxCollider2D>();

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                collider.size = spriteRenderer.sprite.bounds.size;
            else
                collider.size = new Vector2(2f, 2f);
        }
    }
}
