using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private bool dragFromAnywhere = true;
    [SerializeField] private RectTransform dragHandle;
    [SerializeField] private bool clampToScreen = true;
    [SerializeField] private float dragSensitivity = 1f;

    private RectTransform _panelRect;
    private Canvas _canvas;
    private Vector2 _dragStartPosition;
    private Vector2 _panelStartPosition;
    private bool _isDragging;

    private void Awake()
    {
        _panelRect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        
        // 如果没有指定拖拽手柄，并且允许任意位置拖拽，则整个面板可拖拽
        if (dragHandle == null && dragFromAnywhere)
        {
            // 确保面板有Image或Graphic组件才能接收事件
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0); // 完全透明
            }
        }
    }

    private void Start()
    {
        // 设置拖拽手柄的事件
        if (dragHandle != null)
        {
            AddDragEventsToHandle(dragHandle);
        }
    }

    private void AddDragEventsToHandle(RectTransform handle)
    {
        var eventTrigger = handle.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = handle.gameObject.AddComponent<EventTrigger>();
        }

        // 开始拖拽
        var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDragEntry.callback.AddListener((data) => { OnBeginDrag((PointerEventData)data); });
        eventTrigger.triggers.Add(beginDragEntry);

        // 拖拽中
        var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        dragEntry.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
        eventTrigger.triggers.Add(dragEntry);

        // 结束拖拽
        var endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDragEntry.callback.AddListener((data) => { OnEndDrag((PointerEventData)data); });
        eventTrigger.triggers.Add(endDragEntry);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsValidDrag(eventData)) return;

        _isDragging = true;
        _dragStartPosition = eventData.position;
        _panelStartPosition = _panelRect.anchoredPosition;
        
        // 提升面板层级（可选）
        _panelRect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || !IsValidDrag(eventData)) return;

        Vector2 currentPosition = eventData.position;
        Vector2 delta = (currentPosition - _dragStartPosition) * dragSensitivity / _canvas.scaleFactor;

        _panelRect.anchoredPosition = _panelStartPosition + delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;
        
        if (clampToScreen)
        {
            ClampToScreen();
        }
    }

    private bool IsValidDrag(PointerEventData eventData)
    {
        // 只响应左键点击（或触摸）
        return eventData.button == PointerEventData.InputButton.Left;
    }

    private void ClampToScreen()
    {
        if (_canvas == null || _panelRect == null) return;

        Vector3[] panelCorners = new Vector3[4];
        _panelRect.GetWorldCorners(panelCorners);

        Rect canvasRect = _canvas.pixelRect;
        Vector2 minPanelCorner = _canvas.worldCamera.WorldToScreenPoint(panelCorners[0]);
        Vector2 maxPanelCorner = _canvas.worldCamera.WorldToScreenPoint(panelCorners[2]);

        Vector2 panelSize = maxPanelCorner - minPanelCorner;
        Vector2 currentPosition = _panelRect.anchoredPosition;

        // 计算边界限制
        float minX = 0;
        float maxX = canvasRect.width - panelSize.x;
        float minY = 0;
        float maxY = canvasRect.height - panelSize.y;

        // 转换为Canvas空间的位置
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, _panelRect.position);
        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);

        // 转换回anchoredPosition
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _panelRect.parent as RectTransform, screenPosition, _canvas.worldCamera, out Vector2 localPoint))
        {
            _panelRect.anchoredPosition = localPoint;
        }
    }

    // 公共方法用于外部控制
    public void StartDrag()
    {
        _isDragging = true;
        _dragStartPosition = Input.mousePosition;
        _panelStartPosition = _panelRect.anchoredPosition;
    }

    public void StopDrag()
    {
        _isDragging = false;
        if (clampToScreen)
        {
            ClampToScreen();
        }
    }

    // 设置拖拽手柄（运行时）
    public void SetDragHandle(RectTransform handle)
    {
        dragHandle = handle;
        if (handle != null)
        {
            AddDragEventsToHandle(handle);
        }
    }

    // 启用/禁用拖拽
    public void SetDraggable(bool draggable)
    {
        if (dragHandle != null)
        {
            dragHandle.gameObject.SetActive(draggable);
        }
        enabled = draggable;
    }
}