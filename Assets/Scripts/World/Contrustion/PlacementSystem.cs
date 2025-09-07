using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class PlacementSystem : MonoBehaviour
{
    [Header("通用预览设置")]
    public Color previewValidColor = new Color(0, 1, 0, 0.5f);
    public Color previewInvalidColor = new Color(1, 0, 0, 0.5f);

    [Header("擦除模式预览颜色")]
    public Color eraseValidColor = new Color(1, 0, 0, 0.35f);
    public Color eraseInvalidColor = new Color(1, 0, 0, 0.15f);

    [Header("动画设置")]
    public float previewMoveDuration = 0.2f;
    public float previewRotateDuration = 0.3f;
    public Ease moveEase = Ease.OutQuad;
    public Ease rotateEase = Ease.OutBack;

    [Header("组件")]
    public Camera mainCam;
    public TileGridService grid;
    public CameraController cameraController;
    [FormerlySerializedAs("buildingListManager")] public BuildingList buildingList;

    [Header("预览预制件")]
    public GameObject previewPrefab;

    // 预览
    private GenericPreview _currentPreview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;
    private Color _origValid, _origInvalid;
    private Tween _currentMoveTween;
    private Tween _currentRotateTween;

    public int SelectedIndex { get; private set; } = 1;
    public bool IsInBuildMode { get; private set; } = false;

    // 拖动连续绘制/擦除
    private bool _isDragging = false;
    private Vector2Int _dragLastCell;
    private Building _dragLastBuilding;

    // ===== 模式与热键 =====
    private enum BrushMode { Place, Erase }
    private BrushMode _mode = BrushMode.Place;

    private InputManager _input;

    [Header("HUD 设置")]
    public bool showHUD = true;
    public Vector2 hudAnchor = new Vector2(12, 12);
    public float hudLine = 18f;
    public float hudWidth = 360f;
    private GUIStyle _hudStyle, _hudHeader, _hudSmall;

    void Start()
    {
        _input = InputManager.Instance;
        
        if (!mainCam) mainCam = Camera.main;
        if (!cameraController) cameraController = FindObjectOfType<CameraController>();
        ExitBuildMode();
    }

    void Update()
    {
        if (!IsInBuildMode) return;

        var mouseWorld = mainCam.ScreenToWorldPoint(_input.GetMousePosition());
        var cell = grid.WorldToCell(mouseWorld);
        var worldPos = grid.CellToWorld(cell);

        UpdatePreview(worldPos, cell);
        HandleBuildModeInput(cell, worldPos);
    }

    // ========== 建造模式开关 ==========
    public void EnterBuildMode()
    {
        IsInBuildMode = true;
        if (cameraController) cameraController.enabled = false;
        Cursor.visible = true;
        SelectIndex(1);
    }

    public void ExitBuildMode()
    {
        IsInBuildMode = false;
        if (cameraController) cameraController.enabled = true;

        ClearPreview();
        _currentPrefab = null;

        _isDragging = false;
        _dragLastBuilding = null;
        _mode = BrushMode.Place;

        // 清理所有动画
        _currentMoveTween?.Kill();
        _currentRotateTween?.Kill();
    }

    private void ClearPreview()
    {
        if (_currentPreview != null)
        {
            _currentMoveTween?.Kill();
            _currentRotateTween?.Kill();
            Destroy(_currentPreview.gameObject);
            _currentPreview = null;
        }
    }

    // ========== 预览 ==========
    private void UpdatePreview(Vector3 worldPos, Vector2Int cell)
    {
        if (_currentPrefab == null) return;

        if (_currentPreview == null)
        {
            CreatePreview(worldPos);
        }
        else
        {
            SmoothMovePreview(worldPos);
        }

        if (_currentPreview != null)
        {
            // 擦除模式切换预览颜色
            if (EraseActive)
            {
                _currentPreview.validColor = eraseValidColor;
                _currentPreview.invalidColor = eraseInvalidColor;
            }
            else
            {
                _currentPreview.validColor = _origValid;
                _currentPreview.invalidColor = _origInvalid;
            }

            bool ok = !EraseActive ? grid.AreCellsFree(cell, _currentPrefab.size)
                : (grid.GetBuildingAt(cell) != null);
            _currentPreview.SetPreviewState(ok);
        }
    }

    private void SmoothMovePreview(Vector3 targetPosition)
    {
        if (_currentPreview == null) return;

        _currentMoveTween?.Kill();
        _currentMoveTween = _currentPreview.transform.DOMove(targetPosition, previewMoveDuration)
            .SetEase(moveEase);
    }

    private void CreatePreview(Vector3 position)
    {
        if (_currentPrefab == null || previewPrefab == null) return;

        var previewObject = Instantiate(previewPrefab, position, Quaternion.identity);
        _currentPreview = previewObject.GetComponent<GenericPreview>();
        if (_currentPreview == null)
            _currentPreview = previewObject.AddComponent<GenericPreview>();

        _currentPreview.validColor = previewValidColor;
        _currentPreview.invalidColor = previewInvalidColor;
        _origValid = _currentPreview.validColor;
        _origInvalid = _currentPreview.invalidColor;

        _currentPreview.SetDirection(_currentDir);
        _currentPreview.SetSize(_currentPrefab.size);
        SetPreviewIcon();
    }

    private void SetPreviewIcon()
    {
        if (_currentPreview == null || _currentPrefab == null) return;
        BuildingData data = GetBuildingDataForCurrentPrefab();
        if (data != null && data.icon != null)
            _currentPreview.SetIcon(data.icon);
    }

    private BuildingData GetBuildingDataForCurrentPrefab()
    {
        if (buildingList == null || _currentPrefab == null) return null;
        foreach (var bd in buildingList.allBuildings)
        {
            if (bd.prefab != null && bd.prefab.GetType() == _currentPrefab.GetType())
                return bd;
        }
        return null;
    }

    private bool EraseActive => _mode == BrushMode.Erase || _input.IsHoldEraseActive();

    // ========== 输入处理 ==========
    private void HandleBuildModeInput(Vector2Int cell, Vector3 worldPos)
    {
        // 旋转预览
        if (_input.IsRotatePressed())
        {
            RotatePreview();
        }

        // 模式切换（放置/擦除）
        if (_input.IsEraseTogglePressed())
        {
            _mode = (_mode == BrushMode.Place) ? BrushMode.Erase : BrushMode.Place;
            _dragLastBuilding = null;
        }

        // 左键拖动
        if (_input.IsBuildActionPressed() && !_input.IsPointerOverUI())
        {
            _isDragging = true;
            _dragLastCell = cell;

            if (EraseActive)
            {
                EraseOne(cell);
            }
            else
            {
                PlaceOne(cell, worldPos, _currentDir, true);
            }
        }

        if (_isDragging && _input.IsBuildActionHeld() && !_input.IsPointerOverUI())
        {
            if (cell != _dragLastCell)
            {
                if (EraseActive) StepAndEraseAlongPath(_dragLastCell, cell);
                else StepAndPlaceAlongPath(_dragLastCell, cell);
            }
        }

        if (_input.IsBuildActionReleased())
        {
            _isDragging = false;
            _dragLastBuilding = null;
        }

        // 取消/退出建造模式
        if (_input.IsBuildCancelled() && !_input.IsPointerOverUI())
        {
            ExitBuildMode();
        }
        if (_input.IsExitBuildPressed())
        {
            ExitBuildMode();
        }
    }

    private void RotatePreview()
    {
        // 旋转方向：右→下→左→上→右...
        Vector2Int[] rotationCycle = new Vector2Int[]
        {
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.up
        };

        int currentIndex = System.Array.IndexOf(rotationCycle, _currentDir);
        int nextIndex = (currentIndex + 1) % rotationCycle.Length;
        SetDirection(rotationCycle[nextIndex], true);
    }

    private void SetDirection(Vector2Int direction, bool animate = false)
    {
        _currentDir = direction;
        
        if (_currentPreview != null)
        {
            if (animate)
            {
                // 使用 DOTween 平滑旋转
                _currentRotateTween?.Kill();
                
                // 计算目标旋转角度
                float targetAngle = GetRotationAngleFromDirection(direction);
                Vector3 targetRotation = new Vector3(0, 0, targetAngle);
                
                _currentRotateTween = _currentPreview.transform.DORotate(targetRotation, previewRotateDuration)
                    .SetEase(rotateEase)
                    .OnComplete(() => _currentPreview.SetDirection(direction));
            }
            else
            {
                _currentPreview.SetDirection(direction);
            }
        }
    }

    private float GetRotationAngleFromDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.right) return 0f;
        if (direction == Vector2Int.down) return 90f;
        if (direction == Vector2Int.left) return 180f;
        if (direction == Vector2Int.up) return 270f;
        return 0f;
    }
    
    // ===== 放置和擦除逻辑保持不变 =====
    private void PlaceOne(Vector2Int cell, Vector3 worldPos, Vector2Int dirForThis, bool adjustPrev)
    {
        if (!grid.AreCellsFree(cell, _currentPrefab.size))
        {
            _dragLastCell = cell;
            return;
        }

        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
    
        if (building is IOrientable orientable)
        {
            orientable.SetDirection(dirForThis);
        }

        building.OnPlaced(grid, cell);

        if (adjustPrev && _dragLastBuilding is IOrientable prevOrient)
        {
            var delta = cell - _dragLastCell;
            delta = NormalizeToCardinal(delta);
            if (delta != Vector2Int.zero)
                prevOrient.SetDirection(delta);
        }

        _dragLastCell = cell;
        _dragLastBuilding = building;
    }

    private static Vector2Int NormalizeToCardinal(Vector2Int delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int((int)Mathf.Sign(delta.x), 0);
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            return new Vector2Int(0, (int)Mathf.Sign(delta.y));
        return Vector2Int.zero;
    }

    private void StepAndPlaceAlongPath(Vector2Int last, Vector2Int target)
    {
        var cur = last;
        while (cur != target)
        {
            var step = target - cur;
            Vector2Int move = (Mathf.Abs(step.x) >= Mathf.Abs(step.y))
                ? new Vector2Int((int)Mathf.Sign(step.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(step.y));

            var next = cur + move;
            var nextWorld = grid.CellToWorld(next);
            PlaceOne(next, nextWorld, move, adjustPrev: true);
            cur = next;
        }
    }

    private void EraseOne(Vector2Int cell)
    {
        var b = grid.GetBuildingAt(cell);
        if (b != null) b.OnRemoved();
        _dragLastCell = cell;
    }

    private void StepAndEraseAlongPath(Vector2Int last, Vector2Int target)
    {
        var cur = last;
        while (cur != target)
        {
            var step = target - cur;
            Vector2Int move = (Mathf.Abs(step.x) >= Mathf.Abs(step.y))
                ? new Vector2Int((int)Mathf.Sign(step.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(step.y));
            var next = cur + move;
            EraseOne(next);
            cur = next;
        }
    }

    // ===== 选择与 UI =====
    public void SetCurrentBuilding(Building buildingPrefab)
    {
        _currentPrefab = buildingPrefab;
        ClearPreview();

        _isDragging = false;
        _dragLastBuilding = null;
        _mode = BrushMode.Place;
    }

    public void SelectIndex(int idx)
    {
        if (buildingList == null || buildingList.allBuildings.Count == 0) return;

        SelectedIndex = Mathf.Clamp(idx, 1, buildingList.allBuildings.Count);
        if (SelectedIndex <= buildingList.allBuildings.Count)
            SetCurrentBuilding(buildingList.allBuildings[SelectedIndex - 1].prefab);
    }
}