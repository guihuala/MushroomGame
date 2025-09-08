using UnityEngine;

public enum BuildZone
{
    GroundOnly, // 只能在 groundTilemap 上
    SurfaceOnly, // 只能在 surfaceTilemap 上（地表/地表以上）
    Both // ground 或 surface 都可
}

public abstract class Building : MonoBehaviour
{
    [Header("建造限制")] public BuildZone buildZone = BuildZone.GroundOnly; // 普通建筑默认 GroundOnly
    
    public TileGridService grid;
    public Vector2Int cell; // 建筑的起始格子坐标
    public Vector2Int size = Vector2Int.one; // 默认1x1的建筑

    public virtual void OnPlaced(TileGridService g, Vector2Int c)
    {
        grid = g;
        cell = c;
        grid.OccupyCells(cell, size, this); // 占用多格
    }

    public virtual void OnRemoved()
    {
        grid.ReleaseCells(cell, size); // 释放多格
        Destroy(gameObject);
    }

    public virtual void OnNeighborChanged()
    {
    }


    // 旋转建筑及其端口
    public void RotateBuilding(float angle)
    {
        Vector2Int[] occupiedCells = GetOccupiedCells();
        Vector2Int[] rotatedCells = RotateCells(occupiedCells, angle);

        // 更新建筑的位置
        foreach (var c in occupiedCells)
        {
            grid.ReleaseCells(c, Vector2Int.one);
        }

        foreach (var c in rotatedCells)
        {
            grid.OccupyCells(c, Vector2Int.one, this);
        }
    }

    // 获取建筑占用的格子
    public Vector2Int[] GetOccupiedCells()
    {
        Vector2Int[] occupiedCells = new Vector2Int[size.x * size.y];
        int index = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                occupiedCells[index++] = new Vector2Int(cell.x + x, cell.y + y);
            }
        }

        return occupiedCells;
    }

    // 旋转单个格子
    public Vector2Int RotateCell(Vector2Int originalCell, float angle)
    {
        float rad = Mathf.Deg2Rad * angle;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        // 使用锚点进行旋转
        Vector2Int offset = new Vector2Int(originalCell.x - cell.x, originalCell.y - cell.y);
        int newX = Mathf.RoundToInt(cos * offset.x - sin * offset.y + cell.x);
        int newY = Mathf.RoundToInt(sin * offset.x + cos * offset.y + cell.y);

        return new Vector2Int(newX, newY);
    }

    // 旋转多个格子
    private Vector2Int[] RotateCells(Vector2Int[] cells, float angle)
    {
        Vector2Int[] rotatedCells = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            rotatedCells[i] = RotateCell(cells[i], angle);
        }

        return rotatedCells;
    }
}