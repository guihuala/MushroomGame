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
        
        DebugManager.Log($"DownAccepts: {down}");
        switch (down)
        {
            case Conveyor c: 
                bool accepts = d == -c.inDir;
                return accepts;
                
            case Miner: 
                return false;
                
            case HubPort:
                return true;
                
            default: 
                return true;
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
            case Conveyor c: 
                bool feeds = c.outDir == dirToA;
                return feeds;
                
            case Miner m: 
                bool minerFeeds = m.outDir == dirToA;
                return minerFeeds;
                
            default: 
                return true;
        }
    }

    public static bool IsAnchored(TileGridService g, Conveyor a)
    {
        var down = g.GetPortAt(a.cell + a.outDir);
        bool downAccepts = DownAccepts(a.outDir, down);
        
        var up = g.GetPortAt(a.cell - a.inDir);
        bool upFeeds = UpFeeds(a.inDir, up);

        return downAccepts || upFeeds;
    }
}