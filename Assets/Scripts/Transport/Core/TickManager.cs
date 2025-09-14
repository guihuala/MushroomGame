using System.Collections.Generic;
using UnityEngine;

public class TickManager : Singleton<TickManager>,IManager
{
    private readonly List<ITickable> _tickables = new();
    private float _accumulatedTime = 0f;

    [Header("Tick Settings")]
    public float minTickInterval = 0.1f; // 默认100毫秒

    // 初始化方法
    public void Initialize()
    {
        _accumulatedTime = 0f;
        _tickables.Clear();
    }
    
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

        if (_accumulatedTime >= minTickInterval)
        {
            int tickCount = Mathf.FloorToInt(_accumulatedTime / minTickInterval);
            _accumulatedTime -= tickCount * minTickInterval;

            for (int i = 0; i < tickCount; i++)
            {
                // ① 先推进所有传送带（两阶段内部由 BeltScheduler 自己处理）
                BeltScheduler.Instance.TickOnce(minTickInterval);

                // ② 再让其他建筑各自 Tick
                for (int j = 0; j < _tickables.Count; j++)
                {
                    _tickables[j].Tick(minTickInterval);
                }
            }
        }
    }
}