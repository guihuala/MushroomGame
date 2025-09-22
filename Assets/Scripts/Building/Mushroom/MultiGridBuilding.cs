using UnityEngine;
using System.Collections.Generic;

public enum CellSide { Up, Right, Down, Left }

[System.Serializable]
public struct PortDefinition
{
    public Vector2Int localCell; // 相对锚点(左下角)
    public CellSide   side;      // 兼容旧数据：四向枚举
    public PortType   type;      // 输入/输出
}

public class MultiGridBuilding : Building
{
    [Header("多格端口定义")]
    public List<PortDefinition> portDefs = new List<PortDefinition>();
    
    public Dictionary<Vector2Int, IItemPort> inputPorts = new Dictionary<Vector2Int, IItemPort>();
    public Dictionary<Vector2Int, IItemPort> outputPorts = new Dictionary<Vector2Int, IItemPort>();
    
    public int rotationSteps;
    
    public virtual bool ReceiveItem(in ItemPayload payload) { return true; }
    public virtual bool ProvideItem(ref ItemPayload payload) { return true; }

    // ======== 方向&旋转工具 ========
    
    public static Vector2Int RotateLocal(Vector2Int p, int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        return steps switch {
            0 => p,
            1 => new Vector2Int( p.y, -p.x),
            2 => new Vector2Int(-p.x, -p.y),
            3 => new Vector2Int(-p.y,  p.x),
            _ => p
        };
    }

    public static CellSide RotateSide(CellSide side, int steps)
    {
        int v = (((int)side) + steps) % 4;
        return (CellSide)v;
    }

    public static Vector2Int SideToOffset(CellSide side)
    {
        switch (side)
        {
            case CellSide.Up:    return new Vector2Int(0, 1);
            case CellSide.Right: return new Vector2Int(1, 0);
            case CellSide.Down:  return new Vector2Int(0, -1);
            case CellSide.Left:  return new Vector2Int(-1, 0);
            default: return Vector2Int.zero;
        }
    }
}
