using UnityEngine;

public abstract class Building : MonoBehaviour
{
    protected TileGridService grid;
    public Vector2Int cell;
    
    public Vector2Int size = Vector2Int.one;  // 默认1x1的建筑

    public virtual void OnPlaced(TileGridService g, Vector2Int c)
    {
        grid = g; 
        cell = c;
        grid.OccupyCells(cell, size, this);  // 占用多格
    }

    public virtual void OnRemoved()
    {
        grid.ReleaseCells(cell, size);  // 释放多格
        Destroy(gameObject);
    }

    /// <summary>
    /// 当邻居建筑发生变化时调用
    /// </summary>
    public virtual void OnNeighborChanged()
    {
    }
}