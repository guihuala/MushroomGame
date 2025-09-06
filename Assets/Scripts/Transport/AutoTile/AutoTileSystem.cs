using UnityEngine;

public static class AutoTileSystem
{
    public static void RewireAround(TileGridService g, Conveyer me)
    {
        foreach (var dv in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
        {
            Vector2Int neighborCell = me.cell + dv;
            if (!(g.GetPortAt(neighborCell) is Conveyer nb)) continue;
            
            // 如果邻居也是传送带，并且与当前传送带方向垂直
            bool ortho = IsPerp(nb.inDir, me.outDir) || IsPerp(nb.outDir, me.outDir);
            if (!ortho || !TransportCompat.IsAnchored(g, nb)) continue;

            var dBA = dv;
            var dAB = -dBA;

            bool alreadyIn  = (me.inDir  == dBA) && (nb.outDir == dAB); // 我吃邻居
            bool alreadyOut = (me.outDir == dBA) && (nb.inDir  == dAB); // 我喂邻居
            if (alreadyIn || alreadyOut) continue;

            // 只改邻居"一侧"
            if (me.inDir == dBA && nb.outDir != dAB)
            {
                nb.outDir = dAB;
                nb.ApplyDirAndRebuild();
            }
            else if (me.outDir == dBA && nb.inDir != dAB)
            {
                nb.inDir = dAB;
                nb.ApplyDirAndRebuild();
            }
        }

        // 自己：只连下游并更新路径
        me.FindBestOutputConnection();
    }
    
    private static bool IsPerp(Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y == 0;
}