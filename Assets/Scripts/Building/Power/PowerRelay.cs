using UnityEngine;

public class PowerRelay : Building
{
    [Header("电力中继器设置")]
    public float powerTransmissionRange = 5f; // 电力传输的最大距离
    public float powerLossPerTransmission = 0.1f; // 每传递一次电力后衰减的比例
    
    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        PowerManager.Instance.AddPowerRelay(this); // 将中继器添加到电力管理系统
    }

    public override void OnRemoved()
    {
        PowerManager.Instance.RemovePowerRelay(this); // 从电力管理系统中移除
        base.OnRemoved();
    }

    public void TransmitPower(Vector2Int targetCell)
    {
        float distance = Vector2Int.Distance(cell, targetCell);
        if (distance <= powerTransmissionRange)
        {
            float transmittedPower = PowerManager.Instance.GetPower() * (1 - powerLossPerTransmission);
            PowerManager.Instance.SetPower(transmittedPower);
            Debug.Log("Transmitted power to " + targetCell + " with loss.");
        }
        else
        {
            Debug.LogWarning("Power transmission failed: out of range.");
        }
    }
}