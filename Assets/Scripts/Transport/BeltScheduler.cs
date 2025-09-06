using System.Collections.Generic;
using UnityEngine;

// 检查环路之类的
public class BeltScheduler : MonoBehaviour
{
    [SerializeField] private TileGridService grid; // 拖引用或运行时查找
    [SerializeField] private float tickInterval = 0.1f;

    private float _acc;
    private readonly List<List<IBeltNode>> _paths = new();

    void Awake() {
        RebuildAllPaths();
    }

    public void RebuildAllPaths()
    {
        _paths.Clear();

        // 1) 收集所有带子（场景中实现了 IBeltNode 的对象）
        var belts = new List<IBeltNode>();
        belts.RemoveAll(m => m is not IBeltNode);
        var set = new HashSet<IBeltNode>();
        foreach (var b in belts) set.Add((IBeltNode)b);

        // 2) 统计入边：谁被别人 Out 指到
        var hasIncoming = new HashSet<IBeltNode>();
        foreach (IBeltNode n in set) {
            var next = grid.GetPortAt(n.Cell + n.OutDir) as IBeltNode;
            if (next != null) hasIncoming.Add(next);
        }

        // 3) 以“无入边”为起点串链；中途遇环就停止
        foreach (IBeltNode start in set) {
            if (hasIncoming.Contains(start)) continue;
            var path = new List<IBeltNode>();
            var cur = start;
            var seen = new HashSet<IBeltNode>();
            int guard = 0;

            while (cur != null && guard++ < 4096) {
                if (!seen.Add(cur)) break; // 防环
                path.Add(cur);

                var next = grid.GetPortAt(cur.Cell + cur.OutDir) as IBeltNode;
                // 只有“下一格也是带子”才继续串；带→建筑在传输阶段处理
                if (next == null) break;
                cur = next;
            }
            if (path.Count > 0) _paths.Add(path);
        }
    }

    void Update()
    {
        _acc += Time.deltaTime;
        while (_acc >= tickInterval) {
            _acc -= tickInterval;
            TickOnce();
        }
    }

    public void TickOnce()
    {
        // 阶段1：先让每条路径所有带子做“位移”可视
        foreach (var path in _paths) {
            foreach (var belt in path) belt.StepMove(tickInterval);
        }
        // 阶段2：从尾到头做“交接”，避免同帧顶撞
        foreach (var path in _paths) {
            for (int i = path.Count - 1; i >= 0; i--) path[i].StepTransfer();
        }
    }
}