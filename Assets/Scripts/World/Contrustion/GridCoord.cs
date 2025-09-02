using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct GridCoord
{
    public int x, y;  // 网格坐标

    // 构造函数
    public GridCoord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    // 从世界坐标转换到网格坐标
    public static GridCoord FromWorld(Vector3 world, float cell) =>
        new GridCoord(Mathf.RoundToInt(world.x / cell), Mathf.RoundToInt(world.y / cell));

    // 从网格坐标转换到世界坐标
    public Vector3 ToWorld(float cell) => new Vector3(x * cell, y * cell, 0);
}