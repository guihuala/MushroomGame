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
    private readonly MultiGridBuilding _building;

    public ItemPort(Vector2Int cell, MultiGridBuilding building, PortType portType)
    {
        Cell = cell;
        _building = building;
        PortType = portType;
    }

    internal void SetCell(Vector2Int newCell) // 新增：允许旋转时更新位置
    {
        Cell = newCell;
    }

    public bool CanReceive => PortType == PortType.Input;
    public bool CanProvide => PortType == PortType.Output;

    public bool TryReceive(in ItemPayload payload)
        => CanReceive && _building.ReceiveItem(payload);

    public bool TryProvide(ref ItemPayload payload)
        => CanProvide && _building.ProvideItem(ref payload);
}

