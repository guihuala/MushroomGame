using System.Collections.Generic;
using UnityEngine;

public class TickManager : Singleton<TickManager>
{
    private readonly List<ITickable> _tickables = new();

    public void Register(ITickable t)
    {
        if (t != null && !_tickables.Contains(t)) _tickables.Add(t);
    }
    public void Unregister(ITickable t)
    {
        if (t != null) _tickables.Remove(t);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        // 不做并发修改：遍历前复制一份
        for (int i = 0; i < _tickables.Count; i++)
            _tickables[i].Tick(dt);
    }
}