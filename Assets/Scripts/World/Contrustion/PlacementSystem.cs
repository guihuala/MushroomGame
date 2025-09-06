using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("通用预览设置")]
    public Color previewValidColor = new Color(0, 1, 0, 0.5f);
    public Color previewInvalidColor = new Color(1, 0, 0, 0.5f);

    [Header("擦除模式预览颜色")]
    public Color eraseValidColor = new Color(1, 0, 0, 0.35f);
    public Color eraseInvalidColor = new Color(1, 0, 0, 0.15f);

    [Header("组件")]
    public Camera mainCam;
    public TileGridService grid;
    public CameraController cameraController;
    public BuildingListManager buildingListManager;

    [Header("预览预制件")]
    public GameObject previewPrefab;

    // 预览
    private GenericPreview _currentPreview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;
    private Color _origValid, _origInvalid;

    public int SelectedIndex { get; private set; } = 1;
    public bool IsInBuildMode { get; private set; } = false;

    // 拖动连续绘制/擦除
    private bool _isDragging = false;
    private Vector2Int _dragLastCell;
    private Building _dragLastBuilding;

    // ===== 模式与热键 =====
    private enum BrushMode { Place, Erase }
    private BrushMode _mode = BrushMode.Place;

    [Header("快捷键")]
    public KeyCode eraseToggleKey = KeyCode.X;                 // 切换放置/擦除
    public KeyCode holdToErasePrimary = KeyCode.LeftAlt;       // 按住即临时擦除（主）
    public KeyCode holdToEraseSecondary = KeyCode.RightAlt;    // 按住即临时擦除（副，可设为 None）
    public KeyCode exitBuildKey = KeyCode.Escape;              // 退出建造模式

    private bool IsHoldEraseActive()
    {
        bool primary = holdToErasePrimary != KeyCode.None && Input.GetKey(holdToErasePrimary);
        bool secondary = holdToEraseSecondary != KeyCode.None && Input.GetKey(holdToEraseSecondary);
        return primary || secondary;
    }

    private bool EraseActive => _mode == BrushMode.Erase || IsHoldEraseActive();
    
    [Header("HUD 设置")]
    public bool showHUD = true;                                 // 显示状态面板
    public Vector2 hudAnchor = new Vector2(12, 12);             // 左下角偏移（像素）
    public float hudLine = 18f;                                 // 行高
    public float hudWidth = 360f;                               // 面板宽
    private GUIStyle _hudStyle, _hudHeader, _hudSmall;          // 样式缓存

    void Start()
    {
        if (!mainCam) mainCam = Camera.main;
        if (!cameraController) cameraController = FindObjectOfType<CameraController>();
        ExitBuildMode();
    }

    void Update()
    {
        if (!IsInBuildMode) return;

        var mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
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
    }

    private void ClearPreview()
    {
        if (_currentPreview != null)
        {
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
            _currentPreview.transform.position = worldPos;
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

            _currentPreview.SetDirection(_currentDir);

            // 放置：空=可；擦除：有物=可
            bool ok = !EraseActive ? grid.AreCellsFree(cell, _currentPrefab.size) // 修改这里以判断多格占地
                : (grid.GetBuildingAt(cell) != null);
            _currentPreview.SetPreviewState(ok);
        }
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
        if (buildingListManager == null || _currentPrefab == null) return null;
        foreach (var bd in buildingListManager.allBuildings)
        {
            if (bd.prefab != null && bd.prefab.GetType() == _currentPrefab.GetType())
                return bd;
        }
        return null;
    }

    // ========== 输入 ==========
    private void HandleBuildModeInput(Vector2Int cell, Vector3 worldPos)
    {
        // 方向键
        if (Input.GetKeyDown(KeyCode.W)) SetDirection(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) SetDirection(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) SetDirection(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) SetDirection(Vector2Int.right);

        // 模式切换（放置/擦除）
        if (Input.GetKeyDown(eraseToggleKey))
        {
            _mode = (_mode == BrushMode.Place) ? BrushMode.Erase : BrushMode.Place;
            _dragLastBuilding = null; // 切模式清除链
        }

        // 左键拖动：连续放置或擦除
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
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

        if (_isDragging && Input.GetMouseButton(0) && !IsPointerOverUI())
        {
            if (cell != _dragLastCell)
            {
                if (EraseActive) StepAndEraseAlongPath(_dragLastCell, cell);
                else             StepAndPlaceAlongPath(_dragLastCell, cell);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            _dragLastBuilding = null;
        }

        // 取消/退出建造模式：右键或 Esc（HUD 有提示）
        if (Input.GetMouseButtonDown(1) && !IsPointerOverUI())
        {
            ExitBuildMode();
        }
        if (Input.GetKeyDown(exitBuildKey))
        {
            ExitBuildMode();
        }
    }

    private void SetDirection(Vector2Int direction)
    {
        _currentDir = direction;
        if (_currentPreview != null)
            _currentPreview.SetDirection(direction);
    }
    
    // ===== 放置：工具函数 =====
    private void PlaceOne(Vector2Int cell, Vector3 worldPos, Vector2Int dirForThis, bool adjustPrev)
    {
        // 判断整个建筑的占地范围
        if (!grid.AreCellsFree(cell, _currentPrefab.size))
        {
            _dragLastCell = cell;
            return;
        }

        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
    
        if (building is IOrientable orientable)
        {
            orientable.SetDirection(dirForThis); // 先放置再设置方向，确认已经grid赋值
        }

        building.OnPlaced(grid, cell);

        // 让上一格朝向这一格，形成连续链（仅当上一格也可转向）
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

    // ===== 擦除：工具函数 =====
    private void EraseOne(Vector2Int cell)
    {
        var b = grid.GetBuildingAt(cell);
        if (b != null) b.OnRemoved();  // TileGridService 会通知邻居
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
        if (buildingListManager == null || buildingListManager.allBuildings.Count == 0) return;

        SelectedIndex = Mathf.Clamp(idx, 1, buildingListManager.allBuildings.Count);
        if (SelectedIndex <= buildingListManager.allBuildings.Count)
            SetCurrentBuilding(buildingListManager.allBuildings[SelectedIndex - 1].prefab);
    }

    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}