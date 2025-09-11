using UnityEngine;


public static class AutoTileSystem
{
    /// <summary>
    /// 仅重建 “me 自身” 的 in/out；不再修改邻居，避免并行带相互抢方向。
    /// 规则：
    /// 1) 先确定 out：直行优先，其次右转、左转；必须 DownAccepts；
    /// 2) 再确定 in：优先来自反向直邻，其次左右邻；必须 UpFeeds；
    /// </summary>
    public static void RewireAround(TileGridService g, Conveyer me, bool force = false)
    {
        if (g == null || me == null) return;

        // -------- 1) 选择输出（我喂谁）--------
        Vector2Int forward = me.outDir; // 你已有的当前出向
        Vector2Int right   = new Vector2Int(forward.y, -forward.x);
        Vector2Int left    = new Vector2Int(-forward.y, forward.x);

        Vector2Int ChooseOut(Vector2Int dir)
        {
            var down = g.GetPortAt(me.cell + dir) as IItemPort;
            return (down != null && TransportCompat.DownAccepts(dir, down)) ? dir : Vector2Int.zero;
        }

        Vector2Int newOut =
            ChooseOut(forward) != Vector2Int.zero ? forward :
            ChooseOut(right)   != Vector2Int.zero ? right   :
            ChooseOut(left)    != Vector2Int.zero ? left    :
            Vector2Int.zero;

        if (newOut != Vector2Int.zero && newOut != me.outDir)
        {
            me.SetDirection(newOut);   // 改出向（你已有）
            me.ApplyDirAndRebuild();   // 重建可视（你已有）
        }

        // -------- 2) 选择输入（谁喂我）--------
        // 期望有人从我 cell - outDir 的方向来喂（直线优先），否则尝试左右邻
        Vector2Int back = -me.outDir;
        Vector2Int ibRight = new Vector2Int(back.y, -back.x);
        Vector2Int ibLeft  = new Vector2Int(-back.y, back.x);

        Vector2Int ChooseIn(Vector2Int dirFromNeighborToMe)
        {
            var up = g.GetPortAt(me.cell + dirFromNeighborToMe) as IItemPort;
            return (up != null && TransportCompat.UpFeeds(dirFromNeighborToMe, up)) ? dirFromNeighborToMe : Vector2Int.zero;
        }

        Vector2Int newIn =
            ChooseIn(back)    != Vector2Int.zero ? back    :
            ChooseIn(ibRight) != Vector2Int.zero ? ibRight :
            ChooseIn(ibLeft)  != Vector2Int.zero ? ibLeft  :
            Vector2Int.zero;

        if (newIn != Vector2Int.zero && newIn != me.inDir)
        {
            me.inDir = newIn;          // 只改自己，不触邻居
            me.ApplyDirAndRebuild();
        }
    }
}

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
}