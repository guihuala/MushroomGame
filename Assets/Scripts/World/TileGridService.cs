using System.Collections.Generic;
using UnityEngine;

public class TileGridService : MonoBehaviour
{
    public float cellSize = 1f;  // 每个网格单元的大小
    private readonly Dictionary<Vector2Int, Building> _occupied = new();  // 记录被占用的网格

    // 检查网格是否空闲
    public bool IsFree(Vector2Int c) => !_occupied.ContainsKey(c);
    
    // 占用网格（放置建筑）
    public void Occupy(Vector2Int c, Building b) => _occupied[c] = b;

    // 释放网格（移除建筑）
    public void Release(Vector2Int c)
    {
        if (_occupied.ContainsKey(c)) _occupied.Remove(c);
    }

    // 世界坐标转网格坐标
    public Vector2Int WorldToCell(Vector3 world) =>
        new(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.y / cellSize));

    // 网格坐标转世界坐标
    public Vector3 CellToWorld(Vector2Int cell) => new(cell.x * cellSize, cell.y * cellSize, 0);
}