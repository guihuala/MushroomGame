using System.Collections.Generic;
using UnityEngine;

public class TickManager : Singleton<TickManager>
{
    private readonly List<ITickable> _tickables = new();
    private float _accumulatedTime = 0f;

    [Header("Tick Settings")]
    public float minTickInterval = 0.1f; // 默认100毫秒

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
        _accumulatedTime += dt;

        // 只有当累积时间达到最小时间单位时才执行Tick
        if (_accumulatedTime >= minTickInterval)
        {
            int tickCount = Mathf.FloorToInt(_accumulatedTime / minTickInterval);
            _accumulatedTime -= tickCount * minTickInterval;
            
            for (int i = 0; i < tickCount; i++)
            {
                for (int j = 0; j < _tickables.Count; j++)
                {
                    _tickables[j].Tick(minTickInterval);
                }
            }
        }
    }
}