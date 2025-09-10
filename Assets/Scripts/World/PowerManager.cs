using System.Collections.Generic;
using UnityEngine;

public class PowerManager : Singleton<PowerManager>
{
    private float totalPower = 0f;

    private readonly List<PowerPlant> powerPlants = new(); // 电源
    private readonly List<PowerRelay> powerRelays = new(); // 中继

    // —— 新增：网络缓存 —— //
    private readonly List<Node> nodes = new();             // 植入当前场景的所有“节点”（电源/中继）
    private readonly List<int>[] graph = new List<int>[0]; // 邻接表（按 nodes 索引）
    private readonly HashSet<int> poweredNodeIdx = new();  // 与至少一个电源连通的节点索引
    private bool dirtyGraph = true;

    // 覆盖判定时用到的格子->是否有电缓存（可选，减少开销）
    private readonly Dictionary<Vector2Int, bool> cellPoweredCache = new();

    // —— 新增：默认增产倍率（可在 Inspector 做面板，先写死也行） —— //
    [Range(1f, 5f)] public float defaultPoweredMultiplier = 1.25f;

    #region Power amount (原有)

    public void AddPowerSource(PowerPlant powerPlant)
    {
        if (!powerPlants.Contains(powerPlant))
        {
            powerPlants.Add(powerPlant);
            MarkDirty();
        }
    }

    public void RemovePowerSource(PowerPlant powerPlant)
    {
        if (powerPlants.Remove(powerPlant)) MarkDirty();
    }

    public void AddPowerRelay(PowerRelay relay)
    {
        if (!powerRelays.Contains(relay))
        {
            powerRelays.Add(relay);
            MarkDirty();
        }
    }

    public void RemovePowerRelay(PowerRelay relay)
    {
        if (powerRelays.Remove(relay)) MarkDirty();
    }

    public void AddPower(float powerAmount)
    {
        totalPower += powerAmount;
        if (totalPower < 0f) totalPower = 0f;
        // Debug.Log("Total power: " + totalPower);
    }

    public float GetPower() => totalPower;
    public void SetPower(float powerAmount) => totalPower = Mathf.Max(0f, powerAmount);

    #endregion

    #region 覆盖/连通计算（新增）

    private struct Node
    {
        public Vector3 worldPos; // 世界坐标（节点中心）
        public float range;      // 覆盖半径（世界单位）
        public bool isPlant;     // 是否电源（true=电源，false=中继）
    }

    private void MarkDirty()
    {
        dirtyGraph = true;
        cellPoweredCache.Clear();
    }

    private void RebuildGraph()
    {
        dirtyGraph = false;
        nodes.Clear();
        poweredNodeIdx.Clear();

        // 1) 收集节点
        foreach (var p in powerPlants)
        {
            if (p == null) continue;
            nodes.Add(new Node { worldPos = p.transform.position, range = p.coverageRange, isPlant = true });
        }
        foreach (var r in powerRelays)
        {
            if (r == null) continue;
            nodes.Add(new Node { worldPos = r.transform.position, range = r.powerTransmissionRange, isPlant = false });
        }

        // 2) 建图（邻接）
        int n = nodes.Count;
        var tmpGraph = new List<int>[n];
        for (int i = 0; i < n; i++) tmpGraph[i] = new List<int>();

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                float d = Vector3.Distance(nodes[i].worldPos, nodes[j].worldPos);
                float linkDist = Mathf.Min(nodes[i].range, nodes[j].range); // 简单规则：两圈能互相覆盖的就连边
                if (d <= linkDist)
                {
                    tmpGraph[i].Add(j);
                    tmpGraph[j].Add(i);
                }
            }
        }

        // 3) 从所有电源做一次 BFS/DFS，标记可达节点
        var visited = new bool[n];
        var q = new Queue<int>();

        for (int i = 0; i < n; i++)
        {
            if (nodes[i].isPlant)
            {
                q.Enqueue(i);
                visited[i] = true;
                poweredNodeIdx.Add(i);
            }
        }

        while (q.Count > 0)
        {
            int u = q.Dequeue();
            foreach (var v in tmpGraph[u])
            {
                if (!visited[v])
                {
                    visited[v] = true;
                    poweredNodeIdx.Add(v);
                    q.Enqueue(v);
                }
            }
        }

        // 存一下邻接（如果你后面要做更多图运算）
        // graph = tmpGraph; // 若需要可存，否则省内存
    }

    /// <summary>判断一个格子中心是否在有电网络的覆盖圈内</summary>
    public bool IsCellPowered(Vector2Int cell, TileGridService grid = null)
    {
        if (dirtyGraph) RebuildGraph();

        if (cellPoweredCache.TryGetValue(cell, out var cached))
            return cached;

        Vector3 worldPos = grid.CellToWorld(cell);

        // 只在“可达的节点”的覆盖圈内才算有电
        foreach (var idx in poweredNodeIdx)
        {
            var node = nodes[idx];
            float d = Vector3.Distance(worldPos, node.worldPos);
            if (d <= node.range)
            {
                cellPoweredCache[cell] = true;
                return true;
            }
        }

        cellPoweredCache[cell] = false;
        return false;
    }

    /// <summary>获取某个格子的增产倍率</summary>
    public float GetSpeedMultiplier(Vector2Int cell, TileGridService grid = null, float overrideMultiplier = 0f)
    {
        bool powered = IsCellPowered(cell, grid);
        if (!powered) return 1f;
        return (overrideMultiplier > 0f) ? overrideMultiplier : defaultPoweredMultiplier;
    }

    #endregion
}
