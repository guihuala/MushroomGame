using System.Collections.Generic;
using UnityEngine;

public class BeltRenderer : MonoBehaviour
{
    [Header("Prefab & Visual")] public GameObject itemVisualPrefab;

    [Header("Smooth 动画")] public float smoothTime = 0.08f;
    public float maxSpeed = 100f;
    
    [Header("Merge 预览")]
    public bool debugMergeGhosts = true;     // 开关：随时可在 Inspector 勾掉
    public float upstreamWindowStart = 0.5f; // 上游进入投影窗的阈值
    public float ghostAlpha = 0.6f;          // 幽灵半透明

    private readonly Dictionary<Conveyer, List<GameObject>> _mergeGhosts = new();
    private readonly Queue<GameObject> _ghostPool = new();

    
    private readonly HashSet<Conveyer> _activeConveyors = new();

    private readonly Dictionary<BeltItem, GameObject> _goByItem = new();
    private readonly Dictionary<GameObject, Vector3> _velByGo = new();
    private readonly Queue<GameObject> _pool = new();
    private readonly HashSet<BeltItem> _seenThisFrame = new();

    void Start()
    {
        MsgCenter.RegisterMsg(MsgConst.CONVEYOR_PLACED, OnPlaced);
        MsgCenter.RegisterMsg(MsgConst.CONVEYOR_REMOVED, OnRemoved);

        // 场景已有的传送带纳入
        foreach (var c in FindObjectsOfType<Conveyer>())
            _activeConveyors.Add(c);
    }

    void OnDestroy()
    {
        MsgCenter.UnregisterMsg(MsgConst.CONVEYOR_PLACED, OnPlaced);
        MsgCenter.UnregisterMsg(MsgConst.CONVEYOR_REMOVED, OnRemoved);
    }

    void LateUpdate()
    {
        _seenThisFrame.Clear();
        foreach (var c in _activeConveyors)
        {
            var items = c.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                _seenThisFrame.Add(it);

                if (!_goByItem.TryGetValue(it, out var go))
                {
                    go = (_pool.Count > 0 ? _pool.Dequeue() : Instantiate(itemVisualPrefab));
                    _goByItem[it] = go;

                    // 刚生成：直接贴到目标，避免首帧追击
                    go.transform.SetParent(c.transform, false);
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr && it.payload.item != null) sr.sprite = it.payload.item.icon;
                    go.transform.position = it.payload.worldPos;
                    go.SetActive(true);
                    _velByGo[go] = Vector3.zero;
                }
                else if (go.transform.parent != c.transform)
                {
                    // 跨带：换父级 + 当帧贴位 + 清零速度，避免“倒退”
                    go.transform.SetParent(c.transform, false);
                    go.transform.position = it.payload.worldPos;
                    _velByGo[go] = Vector3.zero;
                }

                // 正常更新
                UpdateItemVisual(go, it);
                
                if (debugMergeGhosts) UpdateMergePreview(c);
            }
        }

        // 回收这帧没见到的（已经离场的）物体
        var dead = new List<BeltItem>();
        foreach (var kv in _goByItem)
            if (!_seenThisFrame.Contains(kv.Key))
                dead.Add(kv.Key);
        foreach (var it in dead)
        {
            var go = _goByItem[it];
            go.SetActive(false);
            go.transform.SetParent(null);
            _velByGo.Remove(go);
            _pool.Enqueue(go);
            _goByItem.Remove(it);
        }
    }

    private void OnPlaced(params object[] args)
    {
        if (args.Length > 0 && args[0] is Conveyer c)
            _activeConveyors.Add(c);
    }

    private void OnRemoved(params object[] args)
    {
        if (args.Length > 0 && args[0] is Conveyer c)
        {
            _activeConveyors.Remove(c);

            // 立刻回收挂在该带下面的可视对象
            var toRemove = new List<BeltItem>();
            foreach (var kv in _goByItem)
            {
                var go = kv.Value;
                if (go && go.transform.parent == c.transform)
                {
                    go.SetActive(false);
                    go.transform.SetParent(null);
                    _velByGo.Remove(go);
                    _pool.Enqueue(go);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var it in toRemove) _goByItem.Remove(it);
        }
    }

    private void UpdateItemVisual(GameObject go, BeltItem item)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = item.payload.item ? item.payload.item.icon : null;

        var target = item.payload.worldPos;
        go.transform.rotation = Quaternion.identity;

        if (!_velByGo.TryGetValue(go, out var v)) v = Vector3.zero;

        var pos = go.transform.position;
        if ((pos - target).sqrMagnitude > 0.25f) // SNAP阈值
        {
            pos = target;
            v = Vector3.zero;
        }

        var next = Vector3.SmoothDamp(pos, target, ref v, smoothTime, Mathf.Infinity);
        go.transform.position = next;
        _velByGo[go] = v;
    }

    private void UpdateMergePreview(Conveyer c)
    {
        if (!_mergeGhosts.TryGetValue(c, out var ghosts))
        {
            ghosts = new List<GameObject>();
            _mergeGhosts[c] = ghosts;
        }
        // 回收上次的幽灵
        foreach (var g in ghosts) ReturnGhost(g);
        ghosts.Clear();

        // 统计上游传送带
        var inputs = c.Inputs; // 需要 Conveyer 暴露 IReadOnlyList<IItemPort> Inputs
        int upstreamBelts = 0;

        // 下游带的世界端点（与逻辑层一致）
        Vector3 a = c.grid.CellToWorld(c.cell);
        Vector3 b = c.grid.CellToWorld(c.cell + c.outDir);

        foreach (var ip in inputs)
        {
            if (ip is not Conveyer up) continue;
            upstreamBelts++;

            var upItems = up.Items;
            for (int i = 0; i < upItems.Count; i++)
            {
                var it = upItems[i];
                if (it.pos < upstreamWindowStart) continue;

                // 把上游 [upstreamWindowStart,1] 映射到下游 [0.5,1]
                float t = Mathf.InverseLerp(upstreamWindowStart, 1f, it.pos);
                float downPos = Mathf.Lerp(0.5f, 1f, t);
                Vector3 target = Vector3.Lerp(a, b, downPos);

                var go = SpawnGhost(c);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) {
                    sr.sprite = it.payload.item ? it.payload.item.icon : null;
                    var col = sr.color; col.a = ghostAlpha; sr.color = col;
                }
                go.transform.position = target;
                go.transform.rotation = Quaternion.identity;
                ghosts.Add(go);
            }
        }

        // 不是合流（上游不到 2 条）就不显示幽灵
        if (upstreamBelts < 2)
        {
            foreach (var g in ghosts) ReturnGhost(g);
            ghosts.Clear();
        }
    }

    private GameObject SpawnGhost(Conveyer c)
    {
        var go = _ghostPool.Count > 0 ? _ghostPool.Dequeue() : Instantiate(itemVisualPrefab);
        go.transform.SetParent(c.transform, false);
        go.SetActive(true);
        return go;
    }
    private void ReturnGhost(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
        go.transform.SetParent(null);
        _ghostPool.Enqueue(go);
    }
}