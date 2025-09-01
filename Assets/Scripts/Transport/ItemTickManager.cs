using System.Collections.Generic;
using UnityEngine;

public class ItemTickManager : Singleton<ItemTickManager>
{
    private readonly List<Conveyor> _conveyors = new();

    public void Register(Conveyor c)
    {
        if (!_conveyors.Contains(c)) _conveyors.Add(c);
    }

    public void Unregister(Conveyor c)
    {
        _conveyors.Remove(c);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        // 先全部 Tick，再处理可能的图形刷新，保持稳定
        foreach (var c in _conveyors) c.Tick(dt);
    }
}