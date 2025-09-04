using UnityEngine;
using System.Collections.Generic;

public class ItemFlowAnimation : MonoBehaviour
{
    public float speed = 2f;  // 物品流动的速度

    private List<Vector3> _path; // 物品流动的路径
    private int _currentIndex = 0;  // 当前物品所在的路径位置
    private float _journeyLength;  // 动画的总路程
    private float _startTime;  // 动画开始的时间
    
    public void Init(Vector3 startPos, List<Vector3> path)
    {
        _path = path;
        transform.position = startPos;
        _journeyLength = CalculateJourneyLength(path); // 计算整个路径的长度
        _startTime = Time.time;  // 记录动画开始时间
    }

    // 每帧更新物品的位置
    void Update()
    {
        if (_path == null || _currentIndex >= _path.Count - 1) return;

        // 计算物品的当前位置
        float distanceCovered = (Time.time - _startTime) * speed;
        float fractionOfJourney = distanceCovered / _journeyLength;

        // 物品沿路径滑动
        transform.position = Vector3.Lerp(_path[_currentIndex], _path[_currentIndex + 1], fractionOfJourney);

        // 如果物品到达当前路径点，切换到下一个路径点
        if (fractionOfJourney >= 1f)
        {
            _currentIndex++;
            _startTime = Time.time;
        }

        // 当物品到达终点时销毁
        if (_currentIndex >= _path.Count - 1)
        {
            ObjectPool.Instance.PushObject(gameObject);
            Destroy(this);
        }
    }

    // 计算整个路径的长度
    private float CalculateJourneyLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector3.Distance(path[i], path[i + 1]);
        }
        return length;
    }
}