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
    public Tilemap surfaceTilemap; // 新增：地表层Tilemap
    public Tilemap obstacleTilemap; // 引用障碍物Tilemap
    public LayerMask obstacleLayers; // 障碍物图层
    public LayerMask surfaceLayers; // 新增：地表图层

    [Header("建造设置")]
    public bool onlyBuildOnGround = true; // 是否只能在地面上建造
    public bool checkObstacles = true; // 是否检查障碍物
    public bool checkSurface = true; // 新增：是否检查地表接触

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
    /// 检查是否接触到地表层（用于蘑菇等需要接触地表的建筑）
    /// </summary>
    public bool IsTouchingSurface(Vector2Int cell)
    {
        if (!checkSurface) return true; // 如果不检查地表，默认返回true

        Vector3 worldPos = CellToWorld(cell);

        // 检查地表层Tilemap
        if (surfaceTilemap != null)
        {
            Vector3Int tilemapCell = surfaceTilemap.WorldToCell(worldPos);
            if (surfaceTilemap.HasTile(tilemapCell))
            {
                return true;
            }
        }

        // 检查上方格子是否有地表（用于垂直接触）
        Vector2Int belowCell = new Vector2Int(cell.x, cell.y + 1);
        Vector3 belowWorldPos = CellToWorld(belowCell);

        if (surfaceTilemap != null)
        {
            Vector3Int belowTilemapCell = surfaceTilemap.WorldToCell(belowWorldPos);
            if (surfaceTilemap.HasTile(belowTilemapCell))
            {
                return true;
            }
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
    
    public bool AreCellsFree(Vector2Int startCell, Vector2Int size)
    {
        for (int x = startCell.x; x < startCell.x + size.x; x++)
        {
            for (int y = startCell.y; y < startCell.y + size.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!CanBuildAt(cell) || _buildings.ContainsKey(cell))  // 不能放置
                {
                    return false;
                }
            }
        }
        return true;
    }

    // 占用格子
    public void OccupyCells(Vector2Int startCell, Vector2Int size, Building building)
    {
        for (int x = startCell.x; x < startCell.x + size.x; x++)
        {
            for (int y = startCell.y; y < startCell.y + size.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                _buildings[cell] = building;
                _buildableCache[cell] = false;  // 更新缓存
            }
        }
    }

    // 释放格子
    public void ReleaseCells(Vector2Int startCell, Vector2Int size)
    {
        for (int x = startCell.x; x < startCell.x + size.x; x++)
        {
            for (int y = startCell.y; y < startCell.y + size.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                _buildings.Remove(cell);
                _buildableCache.Remove(cell);
            }
        }
    }

    
    public void RegisterPort(Vector2Int cell, IItemPort port)
    {
        // 允许在已占用的建筑格上注册端口（端口是格子的"功能"，并不额外占地）
        _ports[cell] = port;
        DebugManager.Log($"Port registered at cell {cell} by {port.GetType().Name}", this);
    }

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

    #region 地表检查，用于种植蘑菇

    public bool IsFree(Vector2Int cell, bool checkMushrooms = false)
    {
        if (!CanBuildAt(cell)) return false;

        if (checkMushrooms)
        {
            // 对于蘑菇，需要额外检查是否接触到地表
            return IsTouchingSurface(cell);
        }

        return !_buildings.ContainsKey(cell);
    }

    #endregion
}