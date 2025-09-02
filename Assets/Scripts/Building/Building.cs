using UnityEngine;

public abstract class Building : MonoBehaviour
{
    protected TileGridService grid;
    protected Vector2Int cell;

    public virtual void OnPlaced(TileGridService g, Vector2Int c)
    {
        grid = g; cell = c;
        grid.OccupyCell(cell, this);
    }

    public virtual void OnRemoved()
    {
        grid.ReleaseCell(cell);
        Destroy(gameObject);
    }

    // 预览上色（放置系统用）
    public virtual void SetPreview(bool ok)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.color = ok ? Color.white : new Color(1, 0.6f, 0.6f, 0.85f);
    }
}