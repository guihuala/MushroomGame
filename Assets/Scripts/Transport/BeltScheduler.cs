using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeltScheduler : Singleton<BeltScheduler>
{
    [SerializeField] private TileGridService grid;

    private readonly List<List<IBeltNode>> _paths = new();

    protected override void Awake()
    {
        base.Awake();
        RebuildAllPaths();
    }

    public void RebuildAllPaths()
    {
        _paths.Clear();

        var set = new HashSet<IBeltNode>(
            FindObjectsOfType<MonoBehaviour>(true).OfType<IBeltNode>()
        );

        var hasIncoming = new HashSet<IBeltNode>();
        foreach (IBeltNode n in set) {
            var next = grid.GetPortAt(n.Cell + n.OutDir) as IBeltNode;
            if (next != null) hasIncoming.Add(next);
        }

        foreach (IBeltNode start in set) {
            if (hasIncoming.Contains(start)) continue;
            var path = new List<IBeltNode>();
            var cur = start;
            var seen = new HashSet<IBeltNode>();
            int guard = 0;

            while (cur != null && guard++ < 4096) {
                if (!seen.Add(cur)) break;
                path.Add(cur);

                var next = grid.GetPortAt(cur.Cell + cur.OutDir) as IBeltNode;
                if (next == null) break;
                cur = next;
            }
            if (path.Count > 0) _paths.Add(path);
        }
    }
    
    public void TickOnce(float dt)
    {
        foreach (var path in _paths) {
            foreach (var belt in path) belt.StepMove(dt);
        }
        foreach (var path in _paths) {
            for (int i = path.Count - 1; i >= 0; i--) path[i].StepTransfer();
        }
    }
}