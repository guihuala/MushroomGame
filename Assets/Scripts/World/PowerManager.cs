using System.Collections.Generic;
using UnityEngine;

public class PowerManager : Singleton<PowerManager>
{
    [Header("Link Rendering")] public bool drawLinks = true;
    public Material linkMaterial;
    [Range(0.01f, 0.3f)] public float linkWidth = 0.06f;
    public Color linkColor = new Color(1f, 1f, 1f, 0.7f);
    public float linkZ = -0.05f;

    private readonly List<LineRenderer> _linkLines = new();
    private Transform _linkRoot;

    private float totalPower = 0f;

    private readonly List<PowerPlant> powerPlants = new(); // 电源
    private readonly List<PowerRelay> powerRelays = new(); // 中继

    private readonly List<Node> nodes = new(); // 植入当前场景的所有节点
    private readonly HashSet<int> poweredNodeIdx = new(); // 与至少一个电源连通的节点索引
    private bool dirtyGraph = true;

    // 覆盖判定时用到的格子->是否有电缓存
    private readonly Dictionary<Vector2Int, bool> cellPoweredCache = new();
    
    // 顶部任意位置
    public event System.Action PowerCoverageChanged;

    [Range(1f, 5f)] public float defaultPoweredMultiplier = 1.25f;

    #region 生命周期

    private void LateUpdate()
    {
        if (dirtyGraph)
        {
            RebuildGraph();
        }
    }

    #endregion

    #region Power amount

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
        if (powerPlants.Remove(powerPlant))
        {
            MarkDirty();
            RemoveLinksForNode(powerPlant);
        }
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
        if (powerRelays.Remove(relay))
        {
            MarkDirty();
            RemoveLinksForNode(relay);
        }
    }
    
    public void AddPower(float powerAmount)
    {
        totalPower += powerAmount;
        if (totalPower < 0f) totalPower = 0f;
    }

    public bool TryConsumePower(float amount)
    {
        if (amount <= 0f) return true;
        if (totalPower >= amount)
        {
            totalPower -= amount;
            return true;
        }

        return false;
    }

    public float GetPower() => totalPower;
    public void SetPower(float powerAmount) => totalPower = Mathf.Max(0f, powerAmount);

    #endregion

    #region 覆盖/连通计算

    private struct Node
    {
        public Transform tr;
        public Vector3 worldPos; // 世界坐标
        public float range; // 覆盖半径
        public bool isPlant; // 是否电源
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
            nodes.Add(new Node
            {
                tr = p.transform,
                worldPos = p.transform.position,
                range = p.coverageRange,
                isPlant = true
            });
        }

        foreach (var r in powerRelays)
        {
            if (r == null) continue;
            nodes.Add(new Node
            {
                tr = r.transform,
                worldPos = r.transform.position,
                range = r.powerTransmissionRange,
                isPlant = false
            });
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
        if (drawLinks) RebuildLinkRenderers(tmpGraph);
        else ClearLinkRenderers();
        
        PowerCoverageChanged?.Invoke();
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

    #region line

    private void RebuildLinkRenderers(List<int>[] tmpGraph)
    {
        // 1) 初始化父物体
        if (_linkRoot == null)
        {
            var go = new GameObject("PowerLinks");
            _linkRoot = go.transform;
            _linkRoot.SetParent(this.transform, worldPositionStays: false);
        }

        // 2) 先把已有 LineRenderer 标记为“未使用”
        var used = new bool[_linkLines.Count];

        // 3) 逐边绘制（只画“带电网络”里的边：两端都在 poweredNodeIdx）
        var powered = poweredNodeIdx;
        int n = nodes.Count;

        for (int i = 0; i < n; i++)
        {
            foreach (var j in tmpGraph[i])
            {
                if (j <= i) continue; // 无向图避免重复
                if (!powered.Contains(i) || !powered.Contains(j)) continue;

                Vector3 a = nodes[i].worldPos;
                a.z = linkZ;
                Vector3 b = nodes[j].worldPos;
                b.z = linkZ;

                // 复用或新建一条线
                int idx = System.Array.IndexOf(used, false);
                LineRenderer lr = null;
                if (idx >= 0 && _linkLines.Count > 0 && idx < _linkLines.Count)
                {
                    lr = _linkLines[idx];
                    used[idx] = true;
                }
                else
                {
                    var go = new GameObject("PowerLink");
                    go.transform.SetParent(_linkRoot, worldPositionStays: false);
                    lr = go.AddComponent<LineRenderer>();
                    _linkLines.Add(lr);
                    
                    used = new bool[_linkLines.Count];
                    for (int k = 0; k < _linkLines.Count - 1; k++) used[k] = true;
                    used[_linkLines.Count - 1] = true;

                    lr.useWorldSpace = true;
                    lr.textureMode = LineTextureMode.Stretch;
                    lr.alignment = LineAlignment.View;
                    lr.numCornerVertices = 0;
                    lr.numCapVertices = 0;

                    lr.material = linkMaterial != null ? linkMaterial : new Material(Shader.Find("Sprites/Default"));

                    lr.widthMultiplier = linkWidth;
                    lr.startColor = lr.endColor = linkColor;
                    lr.sortingOrder = 99;
                }

                Vector3 c = nodes[i].worldPos; c.z = linkZ;
                Vector3 d = nodes[j].worldPos; d.z = linkZ;
                
                var lightning = lr.GetComponent<LightningLine>();
                if (lightning == null) lightning = lr.gameObject.AddComponent<LightningLine>();

                lightning.width = linkWidth;
                lightning.jaggedness = 0.30f; // 折线幅度（相对 AB 长度）
                lightning.segments = 10; // 主干段数
                lightning.taper = 0.5f; // 尾部收窄
                lightning.enableBranches = true; // 开分叉
                lightning.branchChance = 0.05f;

                lr.enabled = true;
                
                lightning.SetEndpoints(nodes[i].tr, nodes[j].tr, linkZ);
            }
        }

        // 4) 关闭未被使用的多余线段
        for (int i = 0; i < _linkLines.Count; i++)
        {
            if (i >= used.Length || !used[i])
            {
                if (_linkLines[i]) _linkLines[i].enabled = false;
            }
        }
    }

    private void ClearLinkRenderers()
    {
        if (_linkLines.Count == 0) return;
        for (int i = 0; i < _linkLines.Count; i++)
        {
            if (_linkLines[i]) _linkLines[i].enabled = false;
        }
    }
    
    private void RemoveLinksForNode(Building building)
    {
        var t = building.transform;
        var toRemove = new List<LineRenderer>();

        foreach (var lr in _linkLines)
        {
            if (!lr) continue;
            var lightning = lr.GetComponent<LightningLine>();
            if (lightning != null && lightning.HasEndpoint(t))
            {
                toRemove.Add(lr);
            }
        }

        foreach (var lr in toRemove)
        {
            _linkLines.Remove(lr);
            if (lr) Destroy(lr.gameObject);
        }
    }

    #endregion
}