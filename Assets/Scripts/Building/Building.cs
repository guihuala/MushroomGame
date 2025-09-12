using UnityEngine;

public enum BuildZone
{
    GroundOnly,
    SurfaceOnly,
    Both
}

public abstract class Building : MonoBehaviour
{
    [Header("建造限制")] public BuildZone buildZone = BuildZone.GroundOnly;
    
    public TileGridService grid;
    public Vector2Int cell; // 建筑的起始格子坐标
    public Vector2Int size = Vector2Int.one; // 默认1x1的建筑
    
    protected static Vector2Int RotCW(Vector2Int v)  => new Vector2Int(v.y, -v.x);
    protected static Vector2Int RotCCW(Vector2Int v) => new Vector2Int(-v.y, v.x);

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
}