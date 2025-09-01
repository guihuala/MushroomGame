using UnityEngine;

public class PlacementTestController : MonoBehaviour
{
    [Header("测试设置")]
    public Building testBuildingPrefab;
    public KeyCode startPlacementKey = KeyCode.P;
    public KeyCode cancelPlacementKey = KeyCode.C;
    
    private PlacementSystem _placementSystem;
    private TileGridService _gridService;

    private void Start()
    {
        // 获取引用
        _placementSystem = FindObjectOfType<PlacementSystem>();
        _gridService = FindObjectOfType<TileGridService>();
        
        if (_placementSystem == null)
        {
            Debug.LogError("找不到 PlacementSystem！");
            return;
        }
        
        if (_gridService == null)
        {
            Debug.LogError("找不到 TileGridService！");
            return;
        }

        Debug.Log("测试控制器已启动");
        Debug.Log($"按 {startPlacementKey} 开始放置建筑");
        Debug.Log($"按 {cancelPlacementKey} 取消放置");
        Debug.Log("左键点击：放置建筑");
        Debug.Log("右键/ESC：取消放置");
    }

    private void Update()
    {
        HandleTestInput();
    }

    private void HandleTestInput()
    {
        // 开始放置测试
        if (Input.GetKeyDown(startPlacementKey))
        {
            StartPlacementTest();
        }
        
        // 取消放置测试
        if (Input.GetKeyDown(cancelPlacementKey))
        {
            CancelPlacementTest();
        }
        
        // 测试网格功能
        if (Input.GetKeyDown(KeyCode.G))
        {
            TestGridFunctions();
        }
        
        // 清空所有建筑
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearAllBuildings();
        }
    }

    private void StartPlacementTest()
    {
        if (testBuildingPrefab == null)
        {
            Debug.LogError("测试建筑预制体未分配！");
            return;
        }
        
        _placementSystem.BeginPlace(testBuildingPrefab);
        Debug.Log("开始放置测试建筑...");
    }

    private void CancelPlacementTest()
    {
        _placementSystem.CancelPlace();
        Debug.Log("取消放置测试");
    }

    private void TestGridFunctions()
    {
        // 测试网格坐标转换
        Vector3 testWorldPos = new Vector3(2.5f, 3.5f, 0);
        Vector2Int cell = _gridService.WorldToCell(testWorldPos);
        Vector3 backToWorld = _gridService.CellToWorld(cell);
        
        Debug.Log($"网格测试: 世界坐标 {testWorldPos} -> 网格坐标 ({cell.x}, {cell.y}) -> 返回世界坐标 {backToWorld}");
        
        // 测试网格占用
        bool isFree = _gridService.IsFree(cell);
        Debug.Log($"网格 ({cell.x}, {cell.y}) 是否空闲: {isFree}");
    }

    private void ClearAllBuildings()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();
        foreach (Building building in allBuildings)
        {
            // 跳过预览建筑
            if (building.GetComponent<Building>() != null && 
                building.GetComponent<SpriteRenderer>().color.a < 1f)
            {
                continue;
            }
            
            // 获取建筑所在的网格位置
            Vector2Int cell = _gridService.WorldToCell(building.transform.position);
            _gridService.Release(cell);
            Destroy(building.gameObject);
        }
        
        Debug.Log($"已清除 {allBuildings.Length} 个建筑");
    }

    // 在场景中显示调试信息
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 40, 400, 30), $"按 {startPlacementKey} - 开始放置测试建筑", style);
        GUI.Label(new Rect(10, 70, 400, 30), $"按 {cancelPlacementKey} - 取消放置", style);
        GUI.Label(new Rect(10, 100, 400, 30), "按 G - 测试网格功能", style);
        GUI.Label(new Rect(10, 130, 400, 30), "按 R - 清空所有建筑", style);
        GUI.Label(new Rect(10, 160, 400, 30), "左键点击 - 放置建筑", style);
        GUI.Label(new Rect(10, 190, 400, 30), "右键/ESC - 取消放置", style);
    }
}