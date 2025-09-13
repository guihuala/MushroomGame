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
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();

        if (dragHandle == null && dragFromAnywhere)
        {
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
        return eventData.button == PointerEventData.InputButton.Left;
    }

    private void ClampToScreen()
    {
        if (_canvas == null || _panelRect == null) return;

        Vector3[] panelCorners = new Vector3[4];
        _panelRect.GetWorldCorners(panelCorners);

        Rect canvasRect = _canvas.pixelRect;
        
        Vector2 minPanelCorner = panelCorners[0];
        Vector2 maxPanelCorner = panelCorners[2];

        Vector2 panelSize = maxPanelCorner - minPanelCorner;
        
        float minX = 0;
        float maxX = canvasRect.width - panelSize.x;
        float minY = 0;
        float maxY = canvasRect.height - panelSize.y;
        
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, _panelRect.position); 
        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _panelRect.parent as RectTransform, screenPosition, null, out Vector2 localPoint))
        {
            _panelRect.anchoredPosition = localPoint;
        }
    }
}