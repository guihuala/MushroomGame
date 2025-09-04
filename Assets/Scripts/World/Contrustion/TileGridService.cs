using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 网格管理服务，负责处理建筑放置、坐标转换和端口管理
/// </summary>
public class TileGridService : MonoBehaviour
{
    [Header("网格设置")] [Tooltip("每个网格单元的大小")]
    public float cellSize = 1f;

    [Header("地图网格引用")]
    public Tilemap groundTilemap; // 引用地形Tilemap
    public Tilemap surfaceTilemap; // 地表层Tilemap
    public Tilemap obstacleTilemap; // 引用障碍物Tilemap

    [Header("建造设置")]
    public bool onlyBuildOnGround = true; // 是否只能在地面上建造
    public bool checkObstacles = true; // 是否检查障碍物
    public bool checkSurface = true; // 是否检查地表接触

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

    #region 坐标转换和检查

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

    #endregion
    
    #region 检查权限
    
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
        
        return false;
    }

    /// <summary>
    /// 检查是否接触到地表层（用于蘑菇等需要接触地表的建筑）
    /// </summary>
    public bool IsTouchingSurface(Vector2Int cell)
    {
        if (!checkSurface) return true;
        
        Vector2Int targetCell = new Vector2Int(cell.x, cell.y);
        Vector3 belowWorldPos = CellToWorld(targetCell);

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

    #endregion

    /// <summary>
    /// 获取指定位置的建筑
    /// </summary>
    public Building GetBuildingAt(Vector2Int cell)
    {
        _buildings.TryGetValue(cell, out var building);
        return building;
    }

    #region 放置格子操作与邻居有关操作

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

    public void OccupyCells(Vector2Int startCell, Vector2Int size, Building building)
    {
        for (int x = startCell.x; x < startCell.x + size.x; x++)
        {
            for (int y = startCell.y; y < startCell.y + size.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                _buildings[cell] = building;
                _buildableCache[cell] = false;
            }
        }
        
        NotifyNeighborsOfChange(startCell);
        NotifyBuildingPlaced(startCell, building);
    }

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
        
        NotifyNeighborsOfChange(startCell);
        NotifyBuildingRemoved(startCell);
    }
    
    private void NotifyNeighborsOfChange(Vector2Int changedCell)
    {
        // 定义4个方向的邻居
        Vector2Int[] directions = { 
            Vector2Int.up, 
            Vector2Int.right, 
            Vector2Int.down, 
            Vector2Int.left 
        };
    
        foreach (var dir in directions)
        {
            var neighborCell = changedCell + dir;
        
            // 发送邻居变化消息
            MsgCenter.SendMsg(MsgConst.MSG_NEIGHBOR_CHANGED, neighborCell);
        
            // 如果有建筑，也通知具体的建筑
            var building = GetBuildingAt(neighborCell);
            if (building != null)
            {
                building.OnNeighborChanged();
            }
        }
    
        DebugManager.Log($"Notified neighbors of change at {changedCell}", this);
    }
    
    private void NotifyBuildingPlaced(Vector2Int cell, Building building)
    {
        MsgCenter.SendMsg(MsgConst.MSG_BUILDING_PLACED, cell, building);
        DebugManager.Log($"Building placed at {cell}: {building.GetType().Name}", this);
    }   
    
    private void NotifyBuildingRemoved(Vector2Int cell)
    {
        MsgCenter.SendMsg(MsgConst.MSG_BUILDING_REMOVED, cell);
        DebugManager.Log($"Building removed from {cell}", this);
    }
    
    #endregion

    #region 端口

    public void RegisterPort(Vector2Int cell, IItemPort port)
    {
        // 允许在已占用的建筑格上注册端口
        // 端口是格子的"功能"，并不额外占地
        _ports[cell] = port;
    }

    public void UnregisterPort(Vector2Int cell, IItemPort port)
    {
        if (_ports.TryGetValue(cell, out var p) && ReferenceEquals(p, port))
        {
            _ports.Remove(cell);
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

    #endregion
}