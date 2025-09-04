using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 网格管理服务，负责处理建筑放置、坐标转换和端口管理
/// </summary>
public class TileGridService : MonoBehaviour
{
    [Header("网格设置")]
    [Tooltip("每个网格单元的大小")]
    public float cellSize = 1f;

    [Header("地图网格引用")]
    public Tilemap groundTilemap; // 引用地形Tilemap
    public Tilemap obstacleTilemap; // 引用障碍物Tilemap
    public LayerMask obstacleLayers; // 障碍物图层

    [Header("建造设置")]
    public bool onlyBuildOnGround = true; // 是否只能在地面上建造
    public bool checkObstacles = true; // 是否检查障碍物

    // 存储建筑和端口的字典
    private readonly Dictionary<Vector2Int, Building> _buildings = new();
    private readonly Dictionary<Vector2Int, IItemPort> _ports = new();

    // 缓存已检查的格子建造权限
    private readonly Dictionary<Vector2Int, bool> _buildableCache = new();

    void Start()
    {
        // 自动查找Tilemap如果未设置
        if (groundTilemap == null)
        {
            groundTilemap = FindObjectOfType<Tilemap>();
            if (groundTilemap != null)
            {
                DebugManager.Log($"自动找到地面Tilemap: {groundTilemap.name}", this);
            }
        }
    }

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
    /// 检查指定网格是否空闲（没有建筑且可以建造）
    /// </summary>
    public bool IsFree(Vector2Int cell)
    {
        // 首先检查是否可以建造
        if (!CanBuildAt(cell))
        {
            return false;
        }

        // 然后检查是否有建筑
        return !_buildings.ContainsKey(cell);
    }

    /// <summary>
    /// 检查指定位置是否可以建造
    /// </summary>
    public bool CanBuildAt(Vector2Int cell)
    {
        // 使用缓存提高性能
        if (_buildableCache.TryGetValue(cell, out bool cachedResult))
        {
            return cachedResult;
        }

        bool canBuild = CheckBuildability(cell);
        _buildableCache[cell] = canBuild;
        return canBuild;
    }

    /// <summary>
    /// 检查指定格子的建造权限
    /// </summary>
    private bool CheckBuildability(Vector2Int cell)
    {
        Vector3 worldPos = CellToWorld(cell);

        // 1. 检查是否有地面（如果启用）
        if (onlyBuildOnGround)
        {
            bool hasGround = CheckGroundTile(cell, worldPos);
            if (!hasGround)
            {
                return false;
            }
        }

        // 2. 检查是否有障碍物（如果启用）
        if (checkObstacles)
        {
            bool hasObstacle = CheckObstacles(cell, worldPos);
            if (hasObstacle)
            {
                return false;
            }
        }

        // 3. 检查是否有其他建筑（通过_buildings字典）
        if (_buildings.ContainsKey(cell))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查地面Tile
    /// </summary>
    private bool CheckGroundTile(Vector2Int cell, Vector3 worldPos)
    {
        // 优先使用Tilemap检查
        if (groundTilemap != null)
        {
            Vector3Int tilemapCell = groundTilemap.WorldToCell(worldPos);
            return groundTilemap.HasTile(tilemapCell);
        }

        // 如果没有Tilemap，使用物理检测（备用方案）
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos);
        foreach (var collider in colliders)
        {
            // 这里可以根据需要添加特定的地面标签或图层检查
            if (collider.CompareTag("Ground") || collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查障碍物
    /// </summary>
    private bool CheckObstacles(Vector2Int cell, Vector3 worldPos)
    {
        // 检查障碍物Tilemap
        if (obstacleTilemap != null)
        {
            Vector3Int tilemapCell = obstacleTilemap.WorldToCell(worldPos);
            if (obstacleTilemap.HasTile(tilemapCell))
            {
                return true;
            }
        }

        // 使用物理检测障碍物
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos, obstacleLayers);
        if (colliders.Length > 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 清除建造缓存（当地图发生变化时调用）
    /// </summary>
    public void ClearBuildCache()
    {
        _buildableCache.Clear();
        DebugManager.Log("建造缓存已清除", this);
    }

    /// <summary>
    /// 预计算区域内所有格子的建造权限（可选，用于性能优化）
    /// </summary>
    public void PrecomputeBuildability(Vector2Int startCell, Vector2Int endCell)
    {
        for (int x = startCell.x; x <= endCell.x; x++)
        {
            for (int y = startCell.y; y <= endCell.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                _buildableCache[cell] = CheckBuildability(cell);
            }
        }
    }

    /// <summary>
    /// 获取指定位置的建筑
    /// </summary>
    public Building GetBuildingAt(Vector2Int cell)
    {
        _buildings.TryGetValue(cell, out var building);
        return building;
    }
    
    /// <summary>
    /// 占用单元格
    /// </summary>
    public void OccupyCell(Vector2Int cell, Building building)
    {
        if (!CanBuildAt(cell))
        {
            DebugManager.LogWarning($"Cannot occupy cell {cell} - not buildable", this);
            return;
        }

        _buildings[cell] = building;
        
        // 更新缓存
        _buildableCache[cell] = false;
        
        DebugManager.Log($"Cell {cell} occupied by {building.GetType().Name}", this);
    }

    /// <summary>
    /// 释放单元格
    /// </summary>
    public void ReleaseCell(Vector2Int cell)
    {
        if (_buildings.Remove(cell))
        {
            // 更新缓存（重新检查建造权限）
            _buildableCache.Remove(cell);
            DebugManager.Log($"Cell {cell} released", this);
        }
        _ports.Remove(cell);
    }

    // 端口管理方法

    /// <summary>
    /// 在指定网格注册物品端口
    /// </summary>
    public void RegisterPort(Vector2Int cell, IItemPort port)
    {
        if (!CanBuildAt(cell))
        {
            DebugManager.LogWarning($"Cannot register port at {cell} - not buildable", this);
            return;
        }

        _ports[cell] = port;
    }

    /// <summary>
    /// 从指定网格注销物品端口
    /// </summary>
    public void UnregisterPort(Vector2Int cell, IItemPort port)
    {
        if (_ports.TryGetValue(cell, out var p) && ReferenceEquals(p, port))
        {
            _ports.Remove(cell);
            DebugManager.Log($"Port unregistered from cell {cell}", this);
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

        return null;
    }

    /// <summary>
    /// 在Scene视图中绘制可建造区域（调试用）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 绘制最近检查过的格子（调试用）
        Gizmos.color = Color.green;
        foreach (var kvp in _buildableCache)
        {
            if (kvp.Value) // 可建造
            {
                Vector3 worldPos = CellToWorld(kvp.Key);
                Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.8f);
            }
        }
    }
}