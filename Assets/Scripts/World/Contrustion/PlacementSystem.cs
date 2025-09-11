using UnityEngine;
using DG.Tweening;

public class PlacementSystem : MonoBehaviour
{
    [Header("通用预览设置")]
    public Color previewValidColor = new Color(0, 1, 0, 0.5f);
    public Color previewInvalidColor = new Color(1, 0, 0, 0.5f);
    
    [Header("动画设置")]
    public float previewMoveDuration = 0.2f;
    public Ease moveEase = Ease.OutQuad;

    [Header("组件")]
    public Camera mainCam;
    public TileGridService grid;
    public BuildingList buildingList;

    [Header("预览预制件")]
    public GameObject previewPrefab;
    
    [Header("Cursor")]
    [SerializeField] private Texture2D cursorDefault;   // 默认光标
    [SerializeField] private Texture2D cursorErase;     // 拆除光标（如小锤子/橡皮）
    [SerializeField] private Vector2 cursorHotspot = new Vector2(8, 8); // 热点（像素坐标）

    // 预览与当前选择
    private GenericPreview _currentPreview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;
    private Color _origValid, _origInvalid;
    private Tween _currentMoveTween, _currentRotateTween;

    public int SelectedIndex { get; private set; } = 1;
    public bool IsInBuildMode { get; private set; }

    // 拖动连续绘制/擦除
    private bool _isDragging;
    private Vector2Int _dragLastCell;
    private Building _dragLastBuilding;
    
    // 直线绘制锁轴
    private enum LineAxis { Free, Horizontal, Vertical }
    private LineAxis _lineAxis = LineAxis.Free;
    private Vector2Int _dragAnchorCell; // 鼠标按下时的锚点（确定直线轴用）

    // 默认模式右键清除
    private bool _isRightErasing;
    private bool _eraseAsBox;
    private Vector2Int _eraseAnchorCell;


    private enum BrushMode { Place, Erase }
    private BrushMode _mode = BrushMode.Place;

    private InputManager _input;

    #region 生命周期
    void Start()
    {
        _input = InputManager.Instance;
        if (!mainCam) mainCam = Camera.main;
        ExitBuildMode();
    }

    void Update()
    {
        Vector2 mouse = _input.GetMousePosition();
        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(mouse);
        Vector2Int cell = grid.WorldToCell(mouseWorld);
        Vector3 worldPos = grid.CellToWorld(cell);

        if (IsInBuildMode)
        {
            UpdatePreview(worldPos, cell);
            HandleBuildModeInput(cell, worldPos);
        }
        else
        {
            HandleDefaultModeInput(cell); // 默认模式：右键清除
        }
    }
    
    private void OnDisable()
    {
        SetEraseCursor(false);
    }

    #endregion

    #region 模式切换
    public void EnterBuildMode()
    {
        IsInBuildMode = true;
        Cursor.visible = true;
        SelectIndex(1);
    }

    public void ExitBuildMode()
    {
        IsInBuildMode = false;

        ClearPreview();
        _currentPrefab = null;
        _isDragging = false;
        _dragLastBuilding = null;
        _mode = BrushMode.Place;

        _currentMoveTween?.Kill();
        _currentRotateTween?.Kill();
    }

    private void SelectIndexInternal(int idx)
    {
        if (buildingList == null || buildingList.allBuildings.Count == 0) return;

        SelectedIndex = Mathf.Clamp(idx, 1, buildingList.allBuildings.Count);
        if (SelectedIndex <= buildingList.allBuildings.Count)
        {
            SetCurrentBuilding(buildingList.allBuildings[SelectedIndex - 1].prefab);
        }
    }
    #endregion

    #region 预览
    
    private void ClearPreview()
    {
        if (_currentPreview == null) return;
        _currentMoveTween?.Kill();
        _currentRotateTween?.Kill();
        Destroy(_currentPreview.gameObject);
        _currentPreview = null;
    }

    private void UpdatePreview(Vector3 worldPos, Vector2Int cell)
    {
        if (_currentPrefab == null) return;

        if (_currentPreview == null) CreatePreview(worldPos);
        else SmoothMovePreview(worldPos);

        if (_currentPreview == null) return;

        ApplyPreviewPalette();
        bool ok = EvaluatePlacementOK(cell);

        TintPreview(ok);
    }

    private void CreatePreview(Vector3 position)
    {
        if (_currentPrefab == null || previewPrefab == null) return;

        if (_currentPreview == null)
        {
            var previewObject = Instantiate(previewPrefab, position, Quaternion.identity);
            _currentPreview = previewObject.GetComponent<GenericPreview>();
            if (_currentPreview == null) _currentPreview = previewObject.AddComponent<GenericPreview>();

            _currentPreview.validColor = previewValidColor;
            _currentPreview.invalidColor = previewInvalidColor;
            _origValid = _currentPreview.validColor;
            _origInvalid = _currentPreview.invalidColor;

            _currentPreview.SetDirection(_currentDir);
            _currentPreview.SetSize(_currentPrefab.size);
            SetPreviewIcon();
        }
    }

    private void SetPreviewIcon()
    {
        if (_currentPreview == null || _currentPrefab == null) return;
        var data = GetBuildingDataForCurrentPrefab();
        if (data != null && data.icon != null) _currentPreview.SetIcon(data.icon);
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

    private void SmoothMovePreview(Vector3 targetPosition)
    {
        if (_currentPreview == null) return;
        _currentMoveTween?.Kill();
        _currentMoveTween = _currentPreview.transform
            .DOMove(targetPosition, previewMoveDuration)
            .SetEase(moveEase);
    }

    private void ApplyPreviewPalette()
    {
        if (_currentPreview == null) return;
        _currentPreview.validColor = _origValid;
        _currentPreview.invalidColor = _origInvalid;
    }

    private void TintPreview(bool ok)
    {
        if (_currentPreview == null) return;
        Color c = ok ? _currentPreview.validColor : _currentPreview.invalidColor;
        var renderers = _currentPreview.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers) r.color = c;
    }

    private bool EvaluatePlacementOK(Vector2Int cell)
    {
        if (_currentPrefab == null) return false;
        
        return grid.AreCellsFree(cell, _currentPrefab.size);
    }

    
    private void RotatePreview()
    {
        Vector2Int[] rotationCycle = { Vector2Int.right, Vector2Int.down, Vector2Int.left, Vector2Int.up };
        int currentIndex = System.Array.IndexOf(rotationCycle, _currentDir);
        int nextIndex = (currentIndex + 1) % rotationCycle.Length;
        SetDirection(rotationCycle[nextIndex]);
    }

    private void SetDirection(Vector2Int direction)
    {
        _currentDir = direction;

        if (_currentPreview == null) return;
        _currentPreview.SetDirection(direction);
    }

    #endregion

    #region 输入
    
    private void HandleBuildModeInput(Vector2Int cell, Vector3 worldPos)
    {
        if (_input.IsRotatePressed()) RotatePreview();

        // 右键退出建造模式
        if (_input.IsRightMouseDown() && !_input.IsPointerOverUI())
        {
            ExitBuildMode();
            SetEraseCursor(false);
            return;
        }

        // 左键开始放置：记录锚点，锁轴状态重置
        if (_input.IsBuildActionPressed() && !_input.IsPointerOverUI())
        {
            _isDragging = true;
            _dragAnchorCell = cell;
            _dragLastCell = cell;
            _lineAxis = LineAxis.Free;

            PlaceOne(cell, worldPos, _currentDir, true);
            return;
        }

        // 拖拽连续绘制（直线锁轴）
        if (_isDragging && _input.IsBuildActionHeld() && !_input.IsPointerOverUI())
        {
            if (cell != _dragLastCell)
            {
                // 首次移动时确定轴向
                if (_lineAxis == LineAxis.Free)
                {
                    var d = cell - _dragAnchorCell;
                    _lineAxis = (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) ? LineAxis.Horizontal : LineAxis.Vertical;
                }

                // 将当前鼠标格投影到锁定轴上
                Vector2Int projected = (_lineAxis == LineAxis.Horizontal)
                    ? new Vector2Int(cell.x, _dragAnchorCell.y)
                    : new Vector2Int(_dragAnchorCell.x, cell.y);

                // 沿着“上一格 -> 投影格”逐格放置（保证严格直线）
                if (projected != _dragLastCell)
                {
                    StepAndPlaceAlongLockedAxis(_dragLastCell, projected);
                }
            }
            return;
        }

        // 松开结束
        if (_input.IsBuildActionReleased())
        {
            _isDragging = false;
            _dragLastBuilding = null;
            _lineAxis = LineAxis.Free;
        }
    }
    
    private void HandleDefaultModeInput(Vector2Int cell)
    {
        // 右键按下：进入拆除流程 -> 切换为拆除光标
        if (_input.IsRightMouseDown() && !_input.IsPointerOverUI())
        {
            _isRightErasing = true;
            _eraseAsBox = false;
            _eraseAnchorCell = cell;

            SetEraseCursor(true);
            return;
        }

        // 右键按住：是否转为框选，仅维持状态（光标保持拆除状态）
        if (_isRightErasing && _input.IsRightMouseHeld() && !_input.IsPointerOverUI())
        {
            if (!_eraseAsBox && cell != _eraseAnchorCell)
                _eraseAsBox = true;

            return;
        }

        // 右键抬起：执行单格或矩形拆除 -> 恢复默认光标
        if (_isRightErasing && _input.IsRightMouseUp())
        {
            if (_eraseAsBox)
                EraseArea(_eraseAnchorCell, cell);
            else
                EraseOne(cell);

            _isRightErasing = false;
            _eraseAsBox = false;

            SetEraseCursor(false);  // ← 恢复默认鼠标
        }
    }

    #endregion

    #region 放置/擦除
    private void PlaceOne(Vector2Int cell, Vector3 worldPos, Vector2Int dirForThis, bool adjustPrev)
    {
        // 1) 统一可行性：整块+上下文
        if (!EvaluatePlacementOK(cell))
        {
            _dragLastCell = cell;
            return;
        }

        // 2) 实例化并设置朝向
        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
        if (building is IOrientable orientable) orientable.SetDirection(dirForThis);

        // 3) TileGridService 已提供 OccupyCells，按 size 矩形逐格占用
        grid.OccupyCells(cell, building.size, building);

        // 4) 建筑回调（注册端口/自定义逻辑等）
        building.OnPlaced(grid, cell);

        // 5) 连续拖动时，让上一件按拖动方向自动调整朝向
        if (adjustPrev && _dragLastBuilding is IOrientable prevOrient)
        {
            var delta = NormalizeToCardinal(cell - _dragLastCell);
            if (delta != Vector2Int.zero) prevOrient.SetDirection(delta);
        }

        _dragLastCell = cell;
        _dragLastBuilding = building;
    }

    private void EraseOne(Vector2Int cell)
    {
        var b = grid.GetBuildingAt(cell);
        if (b != null) b.OnRemoved();
        _dragLastCell = cell;
    }
    
    // 仅沿水平或垂直（锁定轴）步进放置
    private void StepAndPlaceAlongLockedAxis(Vector2Int last, Vector2Int target)
    {
        var cur = last;
        while (cur != target)
        {
            Vector2Int step = (target.x != cur.x)
                ? new Vector2Int((int)Mathf.Sign(target.x - cur.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(target.y - cur.y));

            var next = cur + step;
            PlaceOne(next, grid.CellToWorld(next), step, adjustPrev: true);
            cur = next;
        }
    }
    
    private void EraseArea(Vector2Int start, Vector2Int end)
    {
        Vector2Int min = Vector2Int.Min(start, end);
        Vector2Int max = Vector2Int.Max(start, end);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                EraseOne(cell);  // 按区域清除
            }
        }
    }

    private static Vector2Int StepToward(Vector2Int cur, Vector2Int target)
    {
        var step = target - cur;
        return (Mathf.Abs(step.x) >= Mathf.Abs(step.y))
            ? new Vector2Int((int)Mathf.Sign(step.x), 0)
            : new Vector2Int(0, (int)Mathf.Sign(step.y));
    }

    private static Vector2Int NormalizeToCardinal(Vector2Int delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return new Vector2Int((int)Mathf.Sign(delta.x), 0);
        if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
            return new Vector2Int(0, (int)Mathf.Sign(delta.y));
        return Vector2Int.zero;
    }
    #endregion

    #region 选择与外部接口
    public void SetCurrentBuilding(Building buildingPrefab)
    {
        _currentPrefab = buildingPrefab;
        ClearPreview();
        _isDragging = false;
        _dragLastBuilding = null;
        _mode = BrushMode.Place;
        
        if (_currentPreview == null)
        {
            CreatePreview(Vector3.zero); // 使用默认位置进行实例化
        }

        // 检查建筑是否可旋转
        var orientableBuilding = buildingPrefab as IOrientable;
        if (orientableBuilding == null)
        {
            // 禁用旋转预览
            _currentPreview.SetRotationEnabled(false);
        }
        else
        {
            // 启用旋转预览
            _currentPreview.SetRotationEnabled(true);
        }
    }
 
    public void SelectIndex(int idx) => SelectIndexInternal(idx);

    #endregion

    #region cursor

    private void SetEraseCursor(bool active)
    {
        if (active)
            Cursor.SetCursor(cursorErase, cursorHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(cursorDefault, cursorHotspot, CursorMode.Auto);
    }


    #endregion
}