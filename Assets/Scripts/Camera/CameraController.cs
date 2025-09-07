using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float keyboardMoveSpeed = 10f;
    public bool edgeScrolling = true;
    public float edgeThreshold = 20f;
    
    [Header("缩放设置")]
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    public float zoomLerpSpeed = 10f;
    
    [Header("边界设置")]
    public bool enableBounds = true;
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);
    
    private Camera _camera;
    private Vector3 _targetPosition;
    private float _targetZoom;
    private Vector3 _dragOrigin;
    private bool _isDragging = false;

    private InputManager _input;

    private void Awake()
    {
        _input = InputManager.Instance;
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }
        
        _targetPosition = transform.position;
        _targetZoom = _camera.orthographic ? _camera.orthographicSize : _camera.fieldOfView;
    }

    private void Update()
    {
        if (IsInBuildMode()) return;
        
        HandleKeyboardMovement();
        HandleMouseDrag();
        HandleZoom();
        
        ApplyMovement();
        ApplyZoom();
        ClampPosition();
    }

    private bool IsInBuildMode()
    {
        var placementSystem = FindObjectOfType<PlacementSystem>();
        return placementSystem != null && placementSystem.IsInBuildMode;
    }

    private void HandleKeyboardMovement()
    {
        Vector2 movementInput = _input.GetCameraMovementInput();
        
        if (movementInput != Vector2.zero)
        {
            Vector3 movement = new Vector3(movementInput.x, movementInput.y, 0f) * keyboardMoveSpeed * Time.deltaTime;
            _targetPosition += movement;
        }
    }

    private void HandleMouseDrag()
    {
        if (_input.IsCameraDragStarted())
        {
            _dragOrigin = GetMouseWorldPosition();
            _isDragging = true;
        }
        
        if (_input.IsCameraDragEnded())
        {
            _isDragging = false;
        }
        
        if (_isDragging)
        {
            Vector3 difference = _dragOrigin - GetMouseWorldPosition();
            _targetPosition += difference;
        }
    }

    private void HandleZoom()
    {
        float scroll = _input.GetZoomInput();
        if (scroll != 0f)
        {
            if (_camera.orthographic)
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
            else
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }
    }

    private void ApplyMovement()
    {
        transform.position = Vector3.Lerp(transform.position, _targetPosition, moveSpeed * Time.deltaTime);
    }

    private void ApplyZoom()
    {
        if (_camera.orthographic)
        {
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetZoom, zoomLerpSpeed * Time.deltaTime);
        }
        else
        {
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetZoom, zoomLerpSpeed * Time.deltaTime);
        }
    }

    private void ClampPosition()
    {
        if (!enableBounds) return;
        
        float effectiveMinX = minBounds.x + GetCameraWidth() / 2f;
        float effectiveMaxX = maxBounds.x - GetCameraWidth() / 2f;
        float effectiveMinY = minBounds.y + GetCameraHeight() / 2f;
        float effectiveMaxY = maxBounds.y - GetCameraHeight() / 2f;
        
        _targetPosition.x = Mathf.Clamp(_targetPosition.x, effectiveMinX, effectiveMaxX);
        _targetPosition.y = Mathf.Clamp(_targetPosition.y, effectiveMinY, effectiveMaxY);
    }

    private float GetCameraWidth()
    {
        if (_camera.orthographic)
        {
            return _camera.orthographicSize * 2f * _camera.aspect;
        }
        else
        {
            float distance = Mathf.Abs(transform.position.z);
            return 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _camera.aspect;
        }
    }

    private float GetCameraHeight()
    {
        if (_camera.orthographic)
        {
            return _camera.orthographicSize * 2f;
        }
        else
        {
            float distance = Mathf.Abs(transform.position.z);
            return 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = _input.GetMousePosition();
        mousePosition.z = -transform.position.z;
        
        return _camera.ScreenToWorldPoint(mousePosition);
    }

    public void TeleportTo(Vector3 position)
    {
        _targetPosition = position;
        transform.position = position;
        ClampPosition();
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableBounds) return;
        
        Gizmos.color = Color.yellow;
        
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
        
        Gizmos.DrawWireCube(center, size);
    }
}