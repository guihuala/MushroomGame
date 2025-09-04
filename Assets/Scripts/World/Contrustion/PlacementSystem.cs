using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("组件")] 
    public Camera mainCam;
    public TileGridService grid;
    public CameraController cameraController;
    
    [Header("建筑列表")]
    public BuildingListManager buildingListManager;

    public int SelectedIndex { get; private set; } = 1;
    public bool IsInBuildMode { get; private set; } = false;

    private Building _preview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;

    void Start()
    {
        if (!mainCam) mainCam = Camera.main;
        if (!cameraController) cameraController = FindObjectOfType<CameraController>();
        ExitBuildMode(); // 默认进入浏览模式
    }

    void Update()
    {
        // 模式切换 - 移除B键切换，改为只能通过UI进入建造模式
        // if (Input.GetKeyDown(KeyCode.B))
        // {
        //     ToggleBuildMode();
        // }

        if (!IsInBuildMode) return;
        
        var mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        var cell = grid.WorldToCell(mouseWorld);
        var worldPos = grid.CellToWorld(cell);
        
        UpdatePreview(worldPos, cell);
        
        HandleBuildModeInput(cell, worldPos);
    }

    /// <summary>
    /// 切换建造模式
    /// </summary>
    public void ToggleBuildMode()
    {
        if (IsInBuildMode)
        {
            ExitBuildMode();
        }
        else
        {
            EnterBuildMode();
        }
    }

    /// <summary>
    /// 进入建造模式
    /// </summary>
    public void EnterBuildMode()
    {
        IsInBuildMode = true;
        if (cameraController) cameraController.enabled = false; // 禁用相机控制
        Cursor.visible = true;
        SelectIndex(1); // 默认选择第一个建筑
        DebugManager.Log("进入建造模式 - WASD旋转, 鼠标左键放置, 右键取消");
    }

    /// <summary>
    /// 退出建造模式
    /// </summary>
    public void ExitBuildMode()
    {
        IsInBuildMode = false;
        if (cameraController) cameraController.enabled = true; // 启用相机控制
        
        // 清理预览
        if (_preview != null)
        {
            Destroy(_preview.gameObject);
            _preview = null;
        }
        
        _currentPrefab = null;
        DebugManager.Log("退出建造模式");
    }

    /// <summary>
    /// 更新预览建筑的位置和状态
    /// </summary>
    private void UpdatePreview(Vector3 worldPos, Vector2Int cell)
    {
        if (_preview == null && _currentPrefab != null)
        {
            _preview = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
            _preview.SetPreview(true);
        }
        else if (_preview != null)
        {
            _preview.transform.position = worldPos;
        }
        
        // 设置方向
        if (_preview is IOrientable oriPrev)
        {
            oriPrev.SetDirection(_currentDir);
        }
        
        // 更新预览颜色
        if (_preview != null)
        {
            bool canPlace = grid.IsFree(cell);
            _preview.SetPreview(canPlace);
        }
    }

    /// <summary>
    /// 处理建造模式下的输入
    /// </summary>
    private void HandleBuildModeInput(Vector2Int cell, Vector3 worldPos)
    {
        // WASD 旋转控制
        if (Input.GetKeyDown(KeyCode.A)) RotateDirCCW();
        if (Input.GetKeyDown(KeyCode.D)) RotateDirCW();
        if (Input.GetKeyDown(KeyCode.W)) RotateDirCW();
        if (Input.GetKeyDown(KeyCode.S)) RotateDirCCW();
        
        // ESC键退出建造模式
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitBuildMode();
            return;
        }
        
        // 鼠标操作
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding(cell, worldPos);
        }

        // 鼠标右键取消建造模式
        if (Input.GetMouseButtonDown(1))
        {
            // 检查是否在UI上点击，避免与拆除建筑冲突
            if (!IsPointerOverUI())
            {
                ExitBuildMode();
                return;
            }
        }
    }

    /// <summary>
    /// 检查鼠标是否在UI上
    /// </summary>
    private bool IsPointerOverUI()
    {
        // 使用EventSystem检查是否点击在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
        return false;
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

    /// <summary>
    /// 设置当前建筑
    /// </summary>
    public void SetCurrentBuilding(Building buildingPrefab)
    {
        _currentPrefab = buildingPrefab;
    
        // 清理旧的预览
        if (_preview != null)
        {
            Destroy(_preview.gameObject);
            _preview = null;
        }
    
        DebugManager.Log($"Selected building: {(_currentPrefab != null ? _currentPrefab.GetType().Name : "None")}");
    }
    
    public void SelectIndex(int idx)
    {
        if (buildingListManager == null || buildingListManager.allBuildings.Count == 0) return;
    
        SelectedIndex = Mathf.Clamp(idx, 1, buildingListManager.allBuildings.Count);
    
        if (SelectedIndex <= buildingListManager.allBuildings.Count)
        {
            SetCurrentBuilding(buildingListManager.allBuildings[SelectedIndex - 1].prefab);
        }
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
    /// 在屏幕上显示模式提示
    /// </summary>
    void OnGUI()
    {
        if (!IsInBuildMode) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.green;
        
        GUI.Label(new Rect(10, 10, 300, 30), "建造模式 - 右键取消", style);
        GUI.Label(new Rect(10, 40, 300, 30), "WASD: 旋转  鼠标左键: 放置", style);
    }
}