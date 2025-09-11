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
    
    // 存储建筑和端口的字典
    private readonly Dictionary<Vector2Int, Building> _buildings = new();
    private readonly Dictionary<Vector2Int, IItemPort> _ports = new();

    // 缓存已检查的格子建造权限
    private readonly Dictionary<Vector2Int, bool> _buildableCache = new();
    
    #region 坐标转换和检查
    
    public Vector2Int WorldToCell(Vector3 world) =>
        new(Mathf.RoundToInt(world.x / cellSize), Mathf.RoundToInt(world.y / cellSize));
    
    public Vector3 CellToWorld(Vector2Int cell) =>
        new(cell.x * cellSize, cell.y * cellSize, 0);

    #endregion
    
    #region 检查权限
    
    public bool AreCellsFree(Vector2Int startCell, Vector2Int size, Building building)
    {
        for (int x = startCell.x; x < startCell.x + size.x; x++)
        {
            for (int y = startCell.y; y < startCell.y + size.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (!CanBuildAt(cell, building) || _buildings.ContainsKey(cell))
                    return false;
            }
        }
        return true;
    }

    private bool CheckObstacles(Vector2Int cell, Vector3 worldPos)
    {
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
    
    public bool CanBuildAt(Vector2Int cell, Building building)
    {
        return CheckBuildabilityWithBuilding(cell, building);
    }
    
    private bool CheckBuildabilityWithBuilding(Vector2Int cell, Building building)
    {
        Vector3 worldPos = CellToWorld(cell);
        if (!(building is PowerConveyer) && CheckObstacles(cell, worldPos)) return false;
        if (_buildings.ContainsKey(cell)) return false;

        // 图层探测
        bool hasGround = false;
        if (groundTilemap != null)
        {
            Vector3Int gCell = groundTilemap.WorldToCell(worldPos);
            hasGround = groundTilemap.HasTile(gCell);
        }

        bool hasSurface = false;
        if (surfaceTilemap != null)
        {
            Vector3Int sCell = surfaceTilemap.WorldToCell(worldPos);
            hasSurface = surfaceTilemap.HasTile(sCell);
        }
        
        var zone = (building != null) ? building.buildZone : BuildZone.GroundOnly;

        switch (zone)
        {
            case BuildZone.GroundOnly:
                // 普通建筑：必须在地面
                if (!hasGround) return false;
                return hasGround;

            case BuildZone.SurfaceOnly:
                // 蘑菇：必须接触地表
                if (!hasSurface) return false;
                return hasSurface;

            case BuildZone.Both:
                // 两者任意其一即可
                bool okGround = hasGround;
                bool okSurface = hasSurface;
                return (hasGround && okGround) || (hasSurface && okSurface);

            default:
                return false;
        }
    }
    
    public Building GetBuildingAt(Vector2Int cell)
    {
        _buildings.TryGetValue(cell, out var building);
        return building;
    }    
    
    #endregion
    
    #region 放置格子操作与邻居有关操作
    
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
            MsgCenter.SendMsg(MsgConst.NEIGHBOR_CHANGED, neighborCell);
        
            // 如果有建筑，也通知具体的建筑
            var building = GetBuildingAt(neighborCell);
            if (building != null)
            {
                building.OnNeighborChanged();
            }
        }
    }
    
    private void NotifyBuildingPlaced(Vector2Int cell, Building building)
    {
        MsgCenter.SendMsg(MsgConst.BUILDING_PLACED, cell, building);
    }   
    
    private void NotifyBuildingRemoved(Vector2Int cell)
    {
        MsgCenter.SendMsg(MsgConst.BUILDING_REMOVED, cell);
    }
    
    #endregion

    #region 端口

    public void RegisterPort(Vector2Int cell, IItemPort port)
    {
        _ports[cell] = port;
    }

    public void UnregisterPort(Vector2Int cell, IItemPort port)
    {
        if (_ports.TryGetValue(cell, out var p) && ReferenceEquals(p, port))
        {
            _ports.Remove(cell);
        }
    }
    
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