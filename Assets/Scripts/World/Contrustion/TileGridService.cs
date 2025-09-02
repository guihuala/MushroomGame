// World/TileGridService.cs
using System.Collections.Generic;
using UnityEngine;

public class TileGridService : MonoBehaviour
{
    public float cellSize = 1f;

    private readonly Dictionary<Vector2Int, Building> _buildings = new();
    private readonly Dictionary<Vector2Int, IItemPort> _ports = new();

    // —— 坐标换算 ——
    public Vector2Int WorldToCell(Vector3 world) =>
        new(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.y / cellSize));
    public Vector3 CellToWorld(Vector2Int cell) =>
        new(cell.x * cellSize, cell.y * cellSize, 0);

    // —— 占位 —— 
    public bool IsFree(Vector2Int c) => !_buildings.ContainsKey(c);
    public void OccupyCell(Vector2Int c, Building b) => _buildings[c] = b;
    public void ReleaseCell(Vector2Int c){ _buildings.Remove(c); _ports.Remove(c); }

    // —— 端口注册/查询 —— 
    public void RegisterPort(Vector2Int c, IItemPort port) => _ports[c] = port;
    public void UnregisterPort(Vector2Int c, IItemPort port)
    {
        if (_ports.TryGetValue(c, out var p) && ReferenceEquals(p, port)) _ports.Remove(c);
    }
    public IItemPort GetPortAt(Vector2Int c) => _ports.TryGetValue(c, out var p) ? p : null;
}