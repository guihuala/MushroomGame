using UnityEngine;

// 当一个传送带（Conveyor）被放置或邻居变化时，会调用 RewireAround 方法。
// 它会检查当前传送带四个方向上的邻居（上下左右）。
// 并且该邻居已经与其它传送带连接（IsAnchored），则尝试修正邻居的输入或输出方向，使其与当前传送带兼容。
// 最后，当前传送带会重新寻找最佳输出连接并重建本地路径。
public static class AutoTileSystem
{
    public static void RewireAround(TileGridService g, Conveyor me)
    {
        foreach (var dv in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
        {
            if (!(g.GetPortAt(me.cell + dv) is Conveyor nb)) continue;
            
            // 如果邻居也是传送带，并且与当前传送带方向垂直
            bool ortho = IsPerp(nb.inDir, me.outDir) || IsPerp(nb.outDir, me.outDir);
            if (!ortho || !TransportCompat.IsAnchored(g, nb)) continue;

            var dBA = dv;
            var dAB = -dBA;

            bool alreadyIn  = (me.inDir  == dBA) && (nb.outDir == dAB); // 我吃邻居
            bool alreadyOut = (me.outDir == dBA) && (nb.inDir  == dAB); // 我喂邻居
            if (alreadyIn || alreadyOut) continue;

            // 只改邻居“一侧”
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
        me.FindBestOutputConnection_ByRouter();
        me.BuildLocalPath();
    }
    
    private static bool IsPerp(Vector2Int a, Vector2Int b) => a.x * b.x + a.y * b.y == 0;
}