using UnityEngine;

public enum PortType
{
    Input,  // 输入端口
    Output  // 输出端口
}

public class ItemPort : IItemPort
{
    public Vector2Int Cell { get; private set; }
    public PortType PortType { get; private set; }
    private readonly MultiGridBuilding _building;  // 与该端口关联的建筑

    public ItemPort(Vector2Int cell, MultiGridBuilding building, PortType portType)
    {
        Cell = cell;
        _building = building;
        PortType = portType;
    }

    // 输入端口
    public bool CanReceive => PortType == PortType.Input;

    // 输出端口
    public bool CanProvide => PortType == PortType.Output;

    // 接收物品（仅适用于输入端口）
    public bool TryReceive(in ItemPayload payload)
    {
        if (CanReceive)
        {
            return _building.ReceiveItem(payload);  // 调用建筑的接收方法
        }
        return false;
    }

    // 提供物品（仅适用于输出端口）
    public bool TryProvide(ref ItemPayload payload)
    {
        if (CanProvide)
        {
            return _building.ProvideItem(ref payload);  // 调用建筑的提供方法
        }
        return false;
    }
}
