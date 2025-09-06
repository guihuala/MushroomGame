using UnityEngine;
using System.Collections.Generic;

public class MultiGridBuilding : Building
{
    // 管理输入输出端口
    public Dictionary<Vector2Int, IItemPort> inputPorts = new Dictionary<Vector2Int, IItemPort>();
    public Dictionary<Vector2Int, IItemPort> outputPorts = new Dictionary<Vector2Int, IItemPort>();

    // 物品接收方法
    public virtual bool ReceiveItem(in ItemPayload payload)
    {
        // 处理物品接收逻辑，具体逻辑根据建筑类型不同而定
        return true; // 这里可以根据需求进行扩展
    }

    // 物品输出方法
    public virtual bool ProvideItem(ref ItemPayload payload)
    {
        // 处理物品输出逻辑，具体逻辑根据建筑类型不同而定
        return true; // 这里可以根据需求进行扩展
    }

    // 注册输入端口和输出端口
    public void RegisterPort(Vector2Int cell, IItemPort port, bool isInput)
    {
        if (isInput)
        {
            inputPorts[cell] = port;  // 输入端口
        }
        else
        {
            outputPorts[cell] = port; // 输出端口
        }
    }

    // 卸载输入输出端口
    public void UnregisterPort(Vector2Int cell, bool isInput)
    {
        if (isInput)
        {
            inputPorts.Remove(cell);  // 卸载输入端口
        }
        else
        {
            outputPorts.Remove(cell);  // 卸载输出端口
        }
    }

    // 更新旋转时的端口位置
    public void RotatePorts(float angle)
    {
        Dictionary<Vector2Int, IItemPort> rotatedInputPorts = new Dictionary<Vector2Int, IItemPort>();
        Dictionary<Vector2Int, IItemPort> rotatedOutputPorts = new Dictionary<Vector2Int, IItemPort>();

        // 旋转输入端口
        foreach (var port in inputPorts)
        {
            Vector2Int rotatedPosition = RotateCell(port.Key, angle);
            rotatedInputPorts[rotatedPosition] = port.Value;
        }

        // 旋转输出端口
        foreach (var port in outputPorts)
        {
            Vector2Int rotatedPosition = RotateCell(port.Key, angle);
            rotatedOutputPorts[rotatedPosition] = port.Value;
        }

        // 更新端口字典
        inputPorts = rotatedInputPorts;
        outputPorts = rotatedOutputPorts;
    }
}