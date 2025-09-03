using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("组件")] 
    public Camera mainCam;
    public TileGridService grid;
    public CameraController cameraController;
    
    [Header("Prefabs")] 
    public Building minerPrefab;
    public Conveyor conveyorPrefab;

    public int SelectedIndex { get; private set; } = 1;

    private Building _preview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;

    void Start()
    {
        if (!mainCam) mainCam = Camera.main;
        if (!cameraController) cameraController = FindObjectOfType<CameraController>();
        SelectIndex(1);
    }

    void Update()
    {
        if (_currentPrefab == null) return;
        
        var mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        var cell = grid.WorldToCell(mouseWorld);
        var worldPos = grid.CellToWorld(cell);
        
        UpdatePreview(worldPos, cell);
        
        HandleKeyboardInput();
        
        // 处理鼠标输入
        if (!IsCameraDragging())
        {
            HandleMouseInput(cell, worldPos);
        }
    }

    /// <summary>
    /// 检查是否正在拖拽相机
    /// </summary>
    private bool IsCameraDragging()
    {
        // 检查中键拖拽
        if (Input.GetMouseButton(2)) return true;
        
        return false;
    }

    /// <summary>
    /// 更新预览建筑的位置和状态
    /// </summary>
    private void UpdatePreview(Vector3 worldPos, Vector2Int cell)
    {
        if (_preview == null)
        {
            _preview = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
            _preview.SetPreview(true);
        }
        else
        {
            _preview.transform.position = worldPos;
        }
        
        // 设置方向
        if (_preview is IOrientable oriPrev)
        {
            oriPrev.SetDirection(_currentDir);
        }
        
        // 更新预览颜色（红色表示不能放置，白色表示可以）
        bool canPlace = grid.IsFree(cell);
        _preview.SetPreview(canPlace);
    }

    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 建筑选择热键
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectIndex(4);
        
        // 旋转热键 - 只在有预览建筑时生效
        if (_preview != null)
        {
            if (Input.GetKeyDown(KeyCode.R)) RotateDirCW();
            if (Input.GetKeyDown(KeyCode.Q)) RotateDirCCW();
        }
        
        // ESC键取消放置模式
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    /// <summary>
    /// 处理鼠标输入
    /// </summary>
    private void HandleMouseInput(Vector2Int cell, Vector3 worldPos)
    {
        // 鼠标滚轮 - 旋转方向
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.1f)
        {
            if (scroll > 0.1f) RotateDirCW();
            else RotateDirCCW();
            return;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding(cell, worldPos);
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryRemoveAt(cell);
        }
    }

    /// <summary>
    /// 取消放置模式
    /// </summary>
    private void CancelPlacement()
    {
        if (_preview != null)
        {
            Destroy(_preview.gameObject);
            _preview = null;
        }
        DebugManager.Log("Placement cancelled");
    }

    /// <summary>
    /// 尝试放置建筑
    /// </summary>
    private void TryPlaceBuilding(Vector2Int cell, Vector3 worldPos)
    {
        if (!grid.IsFree(cell)) 
        {
            DebugManager.LogWarning($"Cannot place building at {cell} - cell occupied");
            return;
        }

        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
        
        // 设置方向（如果是可定向建筑）
        if (building is IOrientable orientable)
        {
            orientable.SetDirection(_currentDir);
        }
        
        building.OnPlaced(grid, cell);
        DebugManager.Log($"Placed {building.GetType().Name} at {cell} facing {_currentDir}");
    }

    /// <summary>
    /// 选择建筑类型
    /// </summary>
    public void SelectIndex(int idx)
    {
        SelectedIndex = Mathf.Clamp(idx, 1, 3);
        
        switch (SelectedIndex)
        {
            case 1: _currentPrefab = minerPrefab; break;
            case 2: _currentPrefab = conveyorPrefab; break;
            case 3: _currentPrefab = null; break; // 空手模式
        }
        
        // 清理旧的预览
        if (_preview != null)
        {
            Destroy(_preview.gameObject);
            _preview = null;
        }
        
        DebugManager.Log($"Selected building type: {(_currentPrefab != null ? _currentPrefab.GetType().Name : "None")}");
    }

    /// <summary>
    /// 顺时针旋转方向
    /// </summary>
    private void RotateDirCW()
    {
        if (_currentDir == Vector2Int.right) 
            _currentDir = Vector2Int.down;
        else if (_currentDir == Vector2Int.down) 
            _currentDir = Vector2Int.left;
        else if (_currentDir == Vector2Int.left) 
            _currentDir = Vector2Int.up;
        else 
            _currentDir = Vector2Int.right;
    }

    /// <summary>
    /// 逆时针旋转方向
    /// </summary>
    private void RotateDirCCW()
    {
        if (_currentDir == Vector2Int.right) 
            _currentDir = Vector2Int.up;
        else if (_currentDir == Vector2Int.up) 
            _currentDir = Vector2Int.left;
        else if (_currentDir == Vector2Int.left) 
            _currentDir = Vector2Int.down;
        else 
            _currentDir = Vector2Int.right;
        
        DebugManager.Log($"Rotated direction to: {_currentDir}");
    }

    /// <summary>
    /// 尝试移除指定位置的建筑
    /// </summary>
    private void TryRemoveAt(Vector2Int cell)
    {
        var building = grid.GetBuildingAt(cell);
        if (building != null)
        {
            building.OnRemoved();
            DebugManager.Log($"Removed building at {cell}");
        }
        else
        {
            DebugManager.LogWarning($"No building to remove at {cell}");
        }
    }
}