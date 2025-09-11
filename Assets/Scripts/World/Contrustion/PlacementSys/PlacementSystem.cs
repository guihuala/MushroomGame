using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public partial class PlacementSystem : MonoBehaviour
{
    [Header("通用预览设置")]
    public Color previewValidColor = new Color(0, 1, 0, 0.5f);
    public Color previewInvalidColor = new Color(1, 0, 0, 0.5f);
    
    [Header("拆除确认设置")]
    public Color pendingEraseColor = new Color(1f, 0.5f, 0f, 0.8f); // 橙色半透明
    public float eraseConfirmDuration = 2f; // 拆除确认时间

    // 待拆除建筑列表
    private HashSet<Building> _pendingEraseBuildings = new HashSet<Building>();
    private List<Color[]> _originalBuildingColors = new List<Color[]>();
    private bool _isEraseCancelled = false;
    
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