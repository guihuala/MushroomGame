using UnityEngine;


public static class AutoTileSystem
{
    // 新签名：带 force，用于同帧级联
    public static void RewireAround(TileGridService g, Conveyer me, bool force = false)
    {
        // 1) 遍历四邻，若是传送带且与我出向正交，则尝试“只改邻居一侧”
        foreach (var dv in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
        {
            Vector2Int neighborCell = me.cell + dv;
            if (!(g.GetPortAt(neighborCell) is Conveyer nb)) continue; // 只处理传送带邻居

            // 只要求几何上正交；不再强制邻居已“锚定”（放宽）
            bool ortho = IsPerp(me.outDir, dv) || IsPerp(me.inDir, dv);
            if (!ortho) continue;

            var dBA = dv;      // nb -> me 的方向
            var dAB = -dBA;    // me -> nb 的方向

            bool alreadyIn  = (me.inDir  == dBA) && (nb.outDir == dAB); // 我吃邻居
            bool alreadyOut = (me.outDir == dBA) && (nb.inDir  == dAB); // 我喂邻居
            if (alreadyIn || alreadyOut) continue;

            // 只改邻居“一侧”，保持你原有策略
            if (me.inDir == dBA && nb.outDir != dAB)
            {
                nb.outDir = dAB;
                nb.ApplyDirAndRebuild();
                // 关键：同帧让邻居再跑一次 AutoTile，确保链式稳定
                nb.AutoTile();
            }
            else if (me.outDir == dBA && nb.inDir != dAB)
            {
                nb.inDir = dAB;
                nb.ApplyDirAndRebuild();
                nb.AutoTile();
            }
        }

        // 2) 自己：先按“直行→右转→左转”的优先级连下游（你已有）
        me.FindBestOutputConnection(); // 若成功会更新朝向与可视

        // 3) 失败回退：如果仍然没有可用输出，尝试把 outDir 指向正交邻格上“存在传送带”的方向，再重建
        bool hasValid =
            g.GetPortAt(me.cell + me.Direction) != null &&
            TransportCompat.DownAccepts(me.Direction, g.GetPortAt(me.cell + me.Direction));

        if (!hasValid)
        {
            // 正交方向优先：先右转、再左转（与你现有 FindBestOutputConnection 的顺序一致）
            Vector2Int RotCW(Vector2Int v)  => new Vector2Int(v.y, -v.x);
            Vector2Int RotCCW(Vector2Int v) => new Vector2Int(-v.y, v.x);

            foreach (var dir in new[] { RotCW(me.outDir), RotCCW(me.outDir) })
            {
                var p = g.GetPortAt(me.cell + dir);
                if (p is Conveyer) // 看到是带子，就先把几何对齐，再交给 ApplyDir 校正端口
                {
                    me.SetDirection(dir);
                    me.ApplyDirAndRebuild();
                    break;
                }
            }
        }
    }

    private static bool IsPerp(Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y == 0;
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