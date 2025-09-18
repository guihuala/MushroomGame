using UnityEngine;

public partial class Conveyer
{
    private int _lastAutoTileFrame = -1;
    
    public void AutoTile(bool force = false)
    {
        if (!force && _lastAutoTileFrame == Time.frameCount) return;
        _lastAutoTileFrame = Time.frameCount;

        AutoTileSystem.RewireAround(grid, this, false);  // 只探测，不改向
        UpdateVisualSprite();
    }
}

public static class AutoTileSystem
{
    public static void RewireAround(TileGridService g, Conveyer me, bool force = false)
    {
        if (g == null || me == null) return;

        // 输出探测
        Vector2Int forward = me.outDir;
        Vector2Int right   = new Vector2Int(forward.y, -forward.x);
        Vector2Int left    = new Vector2Int(-forward.y, forward.x);

        bool CanOut(Vector2Int dir)
        {
            var down = g.GetPortAt(me.cell + dir) as IItemPort;
            return (down != null) && TransportCompat.DownAccepts(dir, down);
        }

        bool out_forward = CanOut(forward);
        bool out_right   = CanOut(right);
        bool out_left    = CanOut(left);

        // 输入探测
        Vector2Int back    = -me.outDir;
        Vector2Int ibRight = new Vector2Int(back.y, -back.x);
        Vector2Int ibLeft  = new Vector2Int(-back.y, back.x);

        bool CanIn(Vector2Int dirFromNeighborToMe)
        {
            var up = g.GetPortAt(me.cell + dirFromNeighborToMe) as IItemPort;
            return (up != null) && TransportCompat.UpFeeds(dirFromNeighborToMe, up);
        }

        bool in_back    = CanIn(back);
        bool in_ibRight = CanIn(ibRight);
        bool in_ibLeft  = CanIn(ibLeft);
        
        me.UpdateVisualSprite();
    }
}

// 检验端口能否接受/供给物品的类
public static class TransportCompat
{
    public static bool DownAccepts(Vector2Int d, IItemPort down)
    {
        if (down == null || !down.CanReceive) 
        {
            return false;
        }

        return true; // 默认接受
    }
    
    public static bool UpFeeds(Vector2Int dirToA, IItemPort up)
    {
        if (up == null || !up.CanProvide) 
        {
            return false;
        }
        
        switch (up)
        {
            case Conveyer c: 
                return c.OutDir == dirToA; // 传送带输出方向要匹配
                
            case Miner m: 
                return m.outDir == dirToA; // 矿机输出方向要匹配
                
            default: 
                return true;
        }
    }
}