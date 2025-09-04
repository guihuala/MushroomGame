using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("通用预览设置")]
    public Color previewValidColor = new Color(0, 1, 0, 0.5f);    // 绿色半透明
    public Color previewInvalidColor = new Color(1, 0, 0, 0.5f);  // 红色半透明
    
    [Header("组件")] 
    public Camera mainCam;
    public TileGridService grid;
    public CameraController cameraController;
    public BuildingListManager buildingListManager;
    
    [Header("预览预制件")]
    public GameObject previewPrefab;
    
    // 预览相关
    private GenericPreview _currentPreview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;
    
    public int SelectedIndex { get; private set; } = 1;
    public bool IsInBuildMode { get; private set; } = false;

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

    /// <summary>
    /// 进入建造模式
    /// </summary>
    public void EnterBuildMode()
    {
        IsInBuildMode = true;
        if (cameraController) cameraController.enabled = false;
        Cursor.visible = true;
        SelectIndex(1);
    }

    /// <summary>
    /// 退出建造模式
    /// </summary>
    public void ExitBuildMode()
    {
        IsInBuildMode = false;
        if (cameraController) cameraController.enabled = true;
        
        ClearPreview();
        _currentPrefab = null;
    }

    /// <summary>
    /// 清理预览
    /// </summary>
    private void ClearPreview()
    {
        if (_currentPreview != null)
        {
            Destroy(_currentPreview.gameObject);
            _currentPreview = null;
        }
    }

    /// <summary>
    /// 更新预览
    /// </summary>
    private void UpdatePreview(Vector3 worldPos, Vector2Int cell)
    {
        if (_currentPrefab == null) return;
        
        // 创建或更新预览
        if (_currentPreview == null)
        {
            CreatePreview(worldPos);
        }
        else
        {
            _currentPreview.transform.position = worldPos;
        }
        
        // 设置方向
        if (_currentPreview != null)
        {
            _currentPreview.SetDirection(_currentDir);
            
            // 更新预览状态
            bool canPlace = grid.IsFree(cell);
            _currentPreview.SetPreviewState(canPlace);
        }
    }

    /// <summary>
    /// 创建预览对象
    /// </summary>
    private void CreatePreview(Vector3 position)
    {
        if (_currentPrefab == null || previewPrefab == null) return;
        
        // 实例化专门的预览预制件
        var previewObject = Instantiate(previewPrefab, position, Quaternion.identity);
        
        // 添加通用预览组件
        _currentPreview = previewObject.GetComponent<GenericPreview>();
        if (_currentPreview == null)
        {
            _currentPreview = previewObject.AddComponent<GenericPreview>();
        }
        
        _currentPreview.validColor = previewValidColor;
        _currentPreview.invalidColor = previewInvalidColor;
        
        // 设置初始方向
        _currentPreview.SetDirection(_currentDir);
        
        // 设置预览尺寸（如果需要）
        _currentPreview.SetSize(_currentPrefab.size);
        
        // 设置预览图标
        SetPreviewIcon();
    }

    /// <summary>
    /// 设置预览图标
    /// </summary>
    private void SetPreviewIcon()
    {
        if (_currentPreview == null || _currentPrefab == null) return;
        
        // 获取当前建筑的 BuildingData
        BuildingData buildingData = GetBuildingDataForCurrentPrefab();
        if (buildingData != null && buildingData.icon != null)
        {
            _currentPreview.SetIcon(buildingData.icon);
        }
    }

    /// <summary>
    /// 获取当前预制件对应的 BuildingData
    /// </summary>
    private BuildingData GetBuildingDataForCurrentPrefab()
    {
        if (buildingListManager == null || _currentPrefab == null) return null;
        
        // 通过预制件名称或类型来匹配 BuildingData
        foreach (var buildingData in buildingListManager.allBuildings)
        {
            if (buildingData.prefab != null && buildingData.prefab.GetType() == _currentPrefab.GetType())
            {
                return buildingData;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleBuildModeInput(Vector2Int cell, Vector3 worldPos)
    {
        // WASD 方向控制
        if (Input.GetKeyDown(KeyCode.W)) SetDirection(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) SetDirection(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) SetDirection(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) SetDirection(Vector2Int.right);
        
        // 放置建筑
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            TryPlaceBuilding(cell, worldPos);
        }
        
        // 退出建造模式
        if (Input.GetMouseButtonDown(1) && !IsPointerOverUI())
        {
            ExitBuildMode();
        }
    }

    /// <summary>
    /// 设置方向
    /// </summary>
    private void SetDirection(Vector2Int direction)
    {
        _currentDir = direction;
        if (_currentPreview != null)
        {
            _currentPreview.SetDirection(direction);
        }
    }

    /// <summary>
    /// 尝试放置建筑
    /// </summary>
    private void TryPlaceBuilding(Vector2Int cell, Vector3 worldPos)
    {
        if (!grid.AreCellsFree(cell, _currentPrefab.size))
        {
            DebugManager.LogWarning($"Cannot place building at {cell} - cells occupied");
            return;
        }
        
        // 实例化实际建筑（不是预览对象）
        var building = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
        
        // 设置方向
        if (building is IOrientable orientable)
        {
            orientable.SetDirection(_currentDir);
        }
        
        building.OnPlaced(grid, cell);
        DebugManager.Log($"Placed {building.GetType().Name} at {cell} facing {_currentDir}");
    }

    /// <summary>
    /// 设置当前建筑
    /// </summary>
    public void SetCurrentBuilding(Building buildingPrefab)
    {
        _currentPrefab = buildingPrefab;
        ClearPreview();
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
    
    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}