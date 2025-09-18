using System.Collections.Generic;
using UnityEngine;

public class BeltRenderer : MonoBehaviour
{
    [Header("Prefab & Visual")] public GameObject itemVisualPrefab;

    [Header("Smooth 动画")] public float smoothTime = 0.08f;
    public float maxSpeed = 100f;
    
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
        // 确保图标与 payload 对齐（避免旧图标残留）
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = item.payload.item ? item.payload.item.icon : null;

        var target = item.payload.worldPos;
        go.transform.rotation = Quaternion.identity;

        if (!_velByGo.TryGetValue(go, out var v)) v = Vector3.zero;

        var pos = go.transform.position;
        if ((pos - target).sqrMagnitude > 1f)
        {
            pos = target;
            v = Vector3.zero;
        }

        var next = Vector3.SmoothDamp(pos, target, ref v, smoothTime, maxSpeed);
        go.transform.position = next;
        _velByGo[go] = v;
    }
}