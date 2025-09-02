using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [Header("组件")] public Camera mainCam;
    public TileGridService grid;
    [Header("Prefabs")] public Building minerPrefab;
    public Building conveyorPrefab;

    public int SelectedIndex { get; private set; } = 1;

    private Building _preview;
    private Building _currentPrefab;
    private Vector2Int _currentDir = Vector2Int.right;

    void Start()
    {
        if (!mainCam) mainCam = Camera.main;
        SelectIndex(1);
    }

    void Update()
    {
        if (_currentPrefab == null) return;

        // —— 热键 ——
        if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.E)) RotateDirCW();
        if (Input.GetKeyDown(KeyCode.Q)) RotateDirCCW();
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectIndex(3);
        
        var mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        var cell = grid.WorldToCell(mouseWorld);
        var worldPos = grid.CellToWorld(cell);
        
        if (_preview == null) _preview = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
        else _preview.transform.position = worldPos;
        if (_preview is IOrientable oriPrev) oriPrev.SetDirection(_currentDir);

        bool canPlace = grid.IsFree(cell);
        _preview.SetPreview(canPlace);
        
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            var b = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
            if (b is IOrientable ori) ori.SetDirection(_currentDir);
            b.OnPlaced(grid, cell);
        }
        
        if (Input.GetMouseButtonDown(1)) TryRemoveAt(cell);
    }

    public void SelectIndex(int idx)
    {
        SelectedIndex = Mathf.Clamp(idx, 1, 3);
        _currentPrefab = SelectedIndex == 1 ? minerPrefab : conveyorPrefab;
        if (_preview) Destroy(_preview.gameObject);
        _preview = null;
    }

    private void RotateDirCW()
    {
        if (_currentDir == Vector2Int.right) _currentDir = Vector2Int.down;
        else if (_currentDir == Vector2Int.down) _currentDir = Vector2Int.left;
        else if (_currentDir == Vector2Int.left) _currentDir = Vector2Int.up;
        else _currentDir = Vector2Int.right;
    }

    private void RotateDirCCW()
    {
        if (_currentDir == Vector2Int.right) _currentDir = Vector2Int.up;
        else if (_currentDir == Vector2Int.up) _currentDir = Vector2Int.left;
        else if (_currentDir == Vector2Int.left) _currentDir = Vector2Int.down;
        else _currentDir = Vector2Int.right;
    }

    private void TryRemoveAt(Vector2Int cell)
    {
        var world = grid.CellToWorld(cell);
        var hit = Physics2D.OverlapPoint(world);
        if (!hit) return;
        var b = hit.GetComponentInParent<Building>();
        if (b != null) b.OnRemoved();
    }
}