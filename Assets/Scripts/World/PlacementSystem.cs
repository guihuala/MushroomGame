using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private TileGridService grid;

    private Building _preview;
    private Building _toPlacePrefab;
    private bool _isPlacing = false;

    public void BeginPlace(Building prefab)
    { 
        _toPlacePrefab = prefab;
        _isPlacing = true;
    }
    
    public void CancelPlace()
    { 
        _toPlacePrefab = null; 
        _isPlacing = false;
        if (_preview) Destroy(_preview.gameObject); 
    }

    void Update()
    {
        HandlePlacementInput();
        UpdatePreview();
    }

    private void HandlePlacementInput()
    {
        // 检测取消放置的输入
        if (_isPlacing && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CancelPlace();
            return;
        }

        // 如果没有在放置模式，不执行后续逻辑
        if (!_isPlacing) return;

        // 检测放置确认
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding();
        }
    }

    private void UpdatePreview()
    {
        if (_toPlacePrefab == null) return;
        
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCam.transform.position.z);
        Vector3 worldPosition = mainCam.ScreenToWorldPoint(mousePosition);

        var cell = grid.WorldToCell(worldPosition);
        var pos = grid.CellToWorld(cell);

        // 创建或更新预览
        if (_preview == null) 
        {
            _preview = Instantiate(_toPlacePrefab, pos, Quaternion.identity);
            _preview.SetPreview(true);
        }
        else 
        {
            _preview.transform.position = pos;
        }

        // 更新预览状态
        bool isFree = grid.IsFree(cell);
        _preview.SetPreview(isFree);
    }

    private void TryPlaceBuilding()
    {
        // 获取鼠标位置
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCam.transform.position.z);
        Vector3 worldPosition = mainCam.ScreenToWorldPoint(mousePosition);

        var cell = grid.WorldToCell(worldPosition);
        var pos = grid.CellToWorld(cell);

        if (grid.IsFree(cell))
        {
            var b = Instantiate(_toPlacePrefab, pos, Quaternion.identity);
            b.OnPlaced(grid, cell);
            
            CancelPlace();
        }
    }
}