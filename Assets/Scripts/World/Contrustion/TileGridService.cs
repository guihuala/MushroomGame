using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格管理服务，负责处理建筑放置、坐标转换和端口管理
/// </summary>
public class TileGridService : MonoBehaviour
{
    [Header("网格设置")]
    [Tooltip("每个网格单元的大小")]
    public float cellSize = 1f;

    // 存储建筑和端口的字典
    private readonly Dictionary<Vector2Int, Building> _buildings = new();
    private readonly Dictionary<Vector2Int, IItemPort> _ports = new();

    /// <summary>
    /// 世界坐标转换为网格坐标
    /// </summary>
    public Vector2Int WorldToCell(Vector3 world) =>
        new(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.y / cellSize));

    /// <summary>
    /// 网格坐标转换为世界坐标
    /// </summary>
    public Vector3 CellToWorld(Vector2Int cell) =>
        new(cell.x * cellSize, cell.y * cellSize, 0);

    /// <summary>
    /// 检查指定网格是否空闲（没有建筑）
    /// </summary>
    public bool IsFree(Vector2Int c) => !_buildings.ContainsKey(c);

    /// <summary>
    /// 获取指定位置的建筑
    /// </summary>
    public Building GetBuildingAt(Vector2Int cell)
    {
        _buildings.TryGetValue(cell, out var building);
        return building;
    }
    
    public void OccupyCell(Vector2Int c, Building b)
    {
        _buildings[c] = b;
        DebugManager.Log($"Cell {c} occupied by {b.GetType().Name}", this);
    }

    public void ReleaseCell(Vector2Int c)
    {
        if (_buildings.Remove(c))
        {
            DebugManager.Log($"Cell {c} released", this);
        }
        _ports.Remove(c);
    }

    // 端口管理方法

    /// <summary>
    /// 在指定网格注册物品端口
    /// </summary>
    public void RegisterPort(Vector2Int c, IItemPort port)
    {
        _ports[c] = port;
    }

    /// <summary>
    /// 从指定网格注销物品端口
    /// </summary>
    public void UnregisterPort(Vector2Int c, IItemPort port)
    {
        if (_ports.TryGetValue(c, out var p) && ReferenceEquals(p, port))
        {
            _ports.Remove(c);
            DebugManager.Log($"Port unregistered from cell {c}", this);
        }
    }

    /// <summary>
    /// 获取指定网格的物品端口
    /// </summary>
    public IItemPort GetPortAt(Vector2Int cell)
    {
        if (_ports.TryGetValue(cell, out var port))
        {
            return port;
        }

        DebugManager.LogWarning($"No port found at cell {cell}");
        return null;
    }
}