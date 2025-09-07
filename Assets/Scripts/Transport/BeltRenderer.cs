using System.Collections.Generic;
using UnityEngine;

public class BeltRenderer : MonoBehaviour
{
    [Header("Prefab & Visual")]
    public GameObject itemVisualPrefab;

    [Header("Smooth 动画")]
    public float smoothTime = 0.08f;
    public float maxSpeed = 100f;

    // 内部缓存
    private readonly Dictionary<Conveyer, List<GameObject>> _itemVisuals = new();
    private readonly Queue<GameObject> _visualPool = new();
    private readonly HashSet<Conveyer> _activeConveyors = new();
    private readonly Dictionary<GameObject, Vector3> _velocities = new(); // SmoothDamp 速度状态

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
        // 逐条更新
        foreach (var c in _activeConveyors)
            UpdateConveyorVisuals(c);
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
            // 回收该带对应的视觉到对象池
            if (_itemVisuals.TryGetValue(c, out var list))
            {
                foreach (var go in list) Return(go);
                _itemVisuals.Remove(c);
            }
        }
    }

    private void UpdateConveyorVisuals(Conveyer c)
    {
        if (c == null) return;
        if (!_itemVisuals.ContainsKey(c))
            _itemVisuals[c] = new List<GameObject>();

        var visuals = _itemVisuals[c];
        var items   = c.Items;
        
        while (visuals.Count < c.maxItems)
            visuals.Add(Spawn(c));

        for (int i = 0; i < visuals.Count; i++)
        {
            var go = visuals[i];
            if (i < items.Count)
            {
                go.SetActive(true);
                UpdateItemVisual(go, items[i]);
            }
            else
            {
                go.SetActive(false);
            }
        }
    }

    private GameObject Spawn(Conveyer c)
    {
        var go = _visualPool.Count > 0 ? _visualPool.Dequeue() : Instantiate(itemVisualPrefab);
        go.transform.SetParent(c.transform, false);

        go.transform.position = c.GetWorldPosition();
        return go;
    }

    private void Return(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
        go.transform.SetParent(null);
        _velocities.Remove(go);
        _visualPool.Enqueue(go);
    }
    
    private void UpdateItemVisual(GameObject go, BeltItem item)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null && item.payload.item != null)
            sr.sprite = item.payload.item.icon;

        var target = item.payload.worldPos;

        if (!_velocities.TryGetValue(go, out var v))
            v = Vector3.zero;

        var pos = go.transform.position;
        var next = Vector3.SmoothDamp(pos, target, ref v, smoothTime, maxSpeed);
        
        go.transform.position = next;
        _velocities[go] = v;
    }
}
