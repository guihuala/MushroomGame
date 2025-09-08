using UnityEngine;
using System.Collections.Generic;

public enum CellSide { Up, Right, Down, Left }

[System.Serializable]
public struct PortDefinition
{
    public Vector2Int localCell; // 相对锚点(左下角)的子格坐标，如(0,0)是第1格
    public CellSide   side;      // 端口在该子格的哪条边
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
    
    public void BuildAndRegisterPorts(Vector2Int anchorCell)
    {
        inputPorts.Clear();
        outputPorts.Clear();

        foreach (var def in portDefs)
        {
            Vector2Int rotatedLocal = RotateLocal(def.localCell, rotationSteps);
            CellSide   rotatedSide  = RotateSide(def.side, rotationSteps);

            Vector2Int worldCellOfSubtile = anchorCell + rotatedLocal;
            Vector2Int portCell = worldCellOfSubtile + SideToOffset(rotatedSide);

            var port = new ItemPort(portCell, this, def.type);
            
            RegisterPort(portCell, port, def.type == PortType.Input);

            // 注册到 TileGridService
            if (grid != null)
            {
                grid.RegisterPort(portCell, port);
            }
        }
    }

    
    public void RegisterPort(Vector2Int cell, IItemPort port, bool isInput)
    {
        if (isInput) inputPorts[cell] = port;
        else         outputPorts[cell] = port;
    }

    public void UnregisterPort(Vector2Int cell, bool isInput)
    {
        if (isInput) inputPorts.Remove(cell);
        else         outputPorts.Remove(cell);
    }

    // ======== 方向&旋转工具 ========
    static Vector2Int SideToOffset(CellSide side)
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

    static CellSide RotateSide(CellSide side, int steps)
    {
        int v = ((int)side + steps) % 4;
        return (CellSide)v;
    }

    static Vector2Int RotateLocal(Vector2Int p, int steps)
    {
        // 以锚点为原点的格子旋转
        switch (((steps % 4) + 4) % 4)
        {
            case 0:  return p;
            case 1:  return new Vector2Int(p.y, -p.x);
            case 2:  return new Vector2Int(-p.x, -p.y);
            case 3:  return new Vector2Int(-p.y, p.x);
            default: return p;
        }
    }
}
