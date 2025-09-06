using UnityEngine;

public static class TransportCompat
{
    // 我沿d输出，down是否能从这条边接收
    public static bool DownAccepts(Vector2Int d, IItemPort down)
    {
        if (down == null || !down.CanReceive) 
        {
            return false;
        }
        
        switch (down)
        {
            case Conveyer c: 
                return d == -c.inDir; // 方向相反才能接收
                
            case Miner: 
                return false; // 矿机不接收物品
                
            case HubPort:
                return true; // Hub总是可以接收
                
            default: 
                return true; // 其他建筑默认可以接收
        }
    }

    // up是否能沿dirToA方向提供物品给我
    public static bool UpFeeds(Vector2Int dirToA, IItemPort up)
    {
        if (up == null || !up.CanProvide) 
        {
            return false;
        }
        
        switch (up)
        {
            case Conveyer c: 
                return c.outDir == dirToA; // 传送带输出方向要匹配
                
            case Miner m: 
                return m.outDir == dirToA; // 矿机输出方向要匹配
                
            default: 
                return true; // 其他建筑默认可以提供
        }
    }

    public static bool IsAnchored(TileGridService g, Conveyer a)
    {
        // 检查下游是否能接收
        var down = g.GetPortAt(a.cell + a.outDir);
        bool downAccepts = DownAccepts(a.outDir, down);
        
        // 检查上游是否能提供
        var up = g.GetPortAt(a.cell - a.inDir);
        bool upFeeds = UpFeeds(a.inDir, up);

        // 只要有一端连接成功就算锚定
        return downAccepts || upFeeds;
    }
}