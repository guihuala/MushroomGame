using UnityEngine;

public abstract class Building : MonoBehaviour
{
    protected TileGridService grid;
    protected Vector2Int cell;

    // 新增的占地尺寸
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

    // 预览上色
    public virtual void SetPreview(bool ok)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = ok ? Color.white : new Color(1, 0.6f, 0.6f, 0.85f);
    }
}