using System.Collections.Generic;
using UnityEngine;

public class BeltRenderer : MonoBehaviour
{
    [Header("Prefab & Roots")]
    public GameObject itemVisualPrefab;
    [Tooltip("所有可视货物的统一父物体；若为空将自动在运行时创建")]
    public Transform itemRoot;

    [Header("Smooth 跟随")]
    public float smoothTime = 0.08f;
    [Tooltip("建议用 Infinity，防止目标变化时跟丢")]
    public float maxSpeed = Mathf.Infinity;
    [Tooltip("超过该距离（世界单位）直接贴靠，避免拖尾/卡顿")]
    public float snapDistance = 0.6f;

    [Header("双轨并道（lane）")]
    [Tooltip("后半段逐渐展开的横向偏移量（世界单位）")]
    public float mergeLaneOffset = 0.12f;

    // —— 运行期容器 —— //
    private readonly HashSet<Conveyer> _activeConveyors = new();
    private readonly Dictionary<BeltItem, GameObject> _goByItem = new();
    private readonly Dictionary<GameObject, Vector3> _velByGo = new();
    private readonly Queue<GameObject> _pool = new();
    private readonly HashSet<BeltItem> _seenThisFrame = new();

    private void Awake()
    {
        if (!itemRoot)
        {
            var go = new GameObject("ItemRoot");
            itemRoot = go.transform;
        }
    }

    private void OnEnable()
    {
        MsgCenter.RegisterMsg(MsgConst.CONVEYOR_PLACED, OnPlaced);
        MsgCenter.RegisterMsg(MsgConst.CONVEYOR_REMOVED, OnRemoved);

        // 初次收集场景里的传送带
        var belts = FindObjectsOfType<Conveyer>();
        foreach (var c in belts) _activeConveyors.Add(c);
    }

    private void OnDisable()
    {
        MsgCenter.UnregisterMsg(MsgConst.CONVEYOR_PLACED, OnPlaced);
        MsgCenter.UnregisterMsg(MsgConst.CONVEYOR_REMOVED, OnRemoved);
    }

    private void OnPlaced(params object[] args)
    {
        if (args.Length > 0 && args[0] is Conveyer c) _activeConveyors.Add(c);
    }

    private void OnRemoved(params object[] args)
    {
        if (args.Length > 0 && args[0] is Conveyer c)
        {
            _activeConveyors.Remove(c);

            // 立刻回收“当前挂在该带下”的 GO（尽管本实现统一挂 itemRoot，做个安全清理）
            var toRemove = new List<BeltItem>();
            foreach (var kv in _goByItem)
            {
                var go = kv.Value;
                if (!go) { toRemove.Add(kv.Key); continue; }
            }
            foreach (var it in toRemove)
            {
                if (_goByItem.TryGetValue(it, out var go))
                {
                    go.SetActive(false);
                    go.transform.SetParent(itemRoot, false);
                    _velByGo.Remove(go);
                    _pool.Enqueue(go);
                }
                _goByItem.Remove(it);
            }
        }
    }

    private void LateUpdate()
    {
        _seenThisFrame.Clear();

        foreach (var c in _activeConveyors)
        {
            if (!c) continue;
            var items = c.Items;
            int count = items.Count;
            for (int i = 0; i < count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                _seenThisFrame.Add(it);

                if (!_goByItem.TryGetValue(it, out var go))
                {
                    go = (_pool.Count > 0) ? _pool.Dequeue() : Instantiate(itemVisualPrefab);
                    go.transform.SetParent(itemRoot, false);
                    go.SetActive(true);
                    _goByItem[it] = go;
                    _velByGo[go] = Vector3.zero;

                    // 首帧直接贴靠目标位置，避免追击抖动
                    var spawnTarget = c.grid.CellToWorld(c.cell); // 入带当帧已写 worldPos，这里只是兜底
                    if (it.payload.IsValid) spawnTarget = it.payload.worldPos;
                    go.transform.position = spawnTarget;

                    // 设置图标
                    var sr0 = go.GetComponent<SpriteRenderer>();
                    if (sr0 != null) sr0.sprite = it.payload.item ? it.payload.item.icon : null;
                }
                else
                {
                    // 每帧确保图标与 payload 对齐（防旧图标残留）
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = it.payload.item ? it.payload.item.icon : null;
                }

                UpdateItemVisual(go, it, c);
            }
        }

        // 回收这帧没出现的（已离场）物体
        var dead = new List<BeltItem>();
        foreach (var kv in _goByItem)
            if (!_seenThisFrame.Contains(kv.Key)) dead.Add(kv.Key);

        foreach (var it in dead)
        {
            var go = _goByItem[it];
            if (go)
            {
                go.SetActive(false);
                go.transform.SetParent(itemRoot, false);
                _velByGo.Remove(go);
                _pool.Enqueue(go);
            }
            _goByItem.Remove(it);
        }
    }

    private void UpdateItemVisual(GameObject go, BeltItem item, Conveyer owner)
    {
        // 目标：逻辑层提供的 worldPos
        Vector3 target = (item.payload.IsValid) ? item.payload.worldPos : go.transform.position;

        // —— 后半段双轨并道：按 lane 对目标施加法向偏移 —— //
        // 带方向（网格向量）→ 世界右手法线： (y, -x)
        Vector2Int d = owner.outDir;
        Vector3 normal = new Vector3(d.y, -d.x, 0f).normalized;

        if (item.lane != 0)
        {
            float t = Mathf.Clamp01((item.pos - 0.5f) / 0.5f); // 只在后半段展开
            t = Mathf.SmoothStep(0f, 1f, t);
            target += normal * (mergeLaneOffset * item.lane * t);
        }

        go.transform.rotation = Quaternion.identity;

        if (!_velByGo.TryGetValue(go, out var v)) v = Vector3.zero;

        // 远距直接贴靠，避免“慢慢追过去”的卡顿
        var pos = go.transform.position;
        if ((pos - target).sqrMagnitude > (snapDistance * snapDistance))
        {
            pos = target;
            v = Vector3.zero;
        }

        var next = Vector3.SmoothDamp(pos, target, ref v, smoothTime, maxSpeed);
        go.transform.position = next;
        _velByGo[go] = v;
    }
}
