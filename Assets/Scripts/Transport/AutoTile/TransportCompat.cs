using UnityEngine;

public static class TransportCompat
{
    // 我沿d输出，down是否能从这条边接收
    public static bool DownAccepts(Vector2Int d, IItemPort down)
    {
        if (down == null || !down.CanReceive) 
        {
            Debug.Log($"[DownAccepts] direction {d}: down is null or cannot receive");
            return false;
        }
        
        switch (down)
        {
            case Conveyor c: 
                bool accepts = d == -c.inDir;
                Debug.Log($"[DownAccepts] Conveyor at {c.cell}: direction {d} == -inDir{-c.inDir} = {accepts}");
                return accepts;
                
            case Miner: 
                Debug.Log($"[DownAccepts] Miner: always false");
                return false;
                
            case HubPort:
                Debug.Log($"[DownAccepts] HubPort: always true");
                return true;
                
            default: 
                Debug.Log($"[DownAccepts] Unknown type {down.GetType().Name}: default true");
                return true;
        }
    }

    // up是否能沿dirToA方向提供物品给我
    public static bool UpFeeds(Vector2Int dirToA, IItemPort up)
    {
        if (up == null || !up.CanProvide) 
        {
            Debug.Log($"[UpFeeds] direction {dirToA}: up is null or cannot provide");
            return false;
        }
        
        switch (up)
        {
            case Conveyor c: 
                bool feeds = c.outDir == dirToA;
                Debug.Log($"[UpFeeds] Conveyor at {c.cell}: outDir{c.outDir} == dirToA{dirToA} = {feeds}");
                return feeds;
                
            case Miner m: 
                bool minerFeeds = m.outDir == dirToA;
                Debug.Log($"[UpFeeds] Miner: outDir{m.outDir} == dirToA{dirToA} = {minerFeeds}");
                return minerFeeds;
                
            default: 
                Debug.Log($"[UpFeeds] Unknown type {up.GetType().Name}: default true");
                return true;
        }
    }

    public static bool IsAnchored(TileGridService g, Conveyor a)
    {
        var down = g.GetPortAt(a.cell + a.outDir);
        bool downAccepts = DownAccepts(a.outDir, down);
        
        var up = g.GetPortAt(a.cell - a.inDir);
        bool upFeeds = UpFeeds(a.inDir, up);

        Debug.Log($"[IsAnchored] Conveyor at {a.cell}: downAccepts={downAccepts}, upFeeds={upFeeds}");
        return downAccepts || upFeeds;
    }
}