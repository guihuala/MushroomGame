using UnityEngine;

public abstract class Building : MonoBehaviour
{
    protected TileGridService grid;
    protected Vector2Int cell;

    public virtual void OnPlaced(TileGridService g, Vector2Int c)
    {
        grid = g;
        cell = c;
        grid.Occupy(cell, this);
    }

    public virtual void OnRemoved()
    {
        grid?.Release(cell);
        Destroy(gameObject);
    }

    // 预览材质/颜色切换
    public virtual void SetPreview(bool canPlace)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (!sr) return;
        sr.color = canPlace ? Color.white : new Color(1, 0.6f, 0.6f, 0.8f);
    }
}