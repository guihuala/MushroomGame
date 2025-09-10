using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerManager : Singleton<PowerManager>
{
    private float totalPower = 0f;
    private List<PowerPlant> powerPlants = new List<PowerPlant>(); // 存储所有电力来源
    private List<PowerRelay> powerRelays = new List<PowerRelay>(); // 存储所有中继器

    public void AddPowerSource(PowerPlant powerPlant)
    {
        if (!powerPlants.Contains(powerPlant))
            powerPlants.Add(powerPlant);
    }

    public void RemovePowerSource(PowerPlant powerPlant)
    {
        if (powerPlants.Contains(powerPlant))
            powerPlants.Remove(powerPlant);
    }

    public void AddPower(float powerAmount)
    {
        totalPower += powerAmount;
        Debug.Log("Total power: " + totalPower);
    }

    public float GetPower()
    {
        return totalPower;
    }

    public void SetPower(float powerAmount)
    {
        totalPower = Mathf.Max(0f, powerAmount); // 电力不能为负
    }

    public void AddPowerRelay(PowerRelay relay)
    {
        if (!powerRelays.Contains(relay))
            powerRelays.Add(relay);
    }

    public void RemovePowerRelay(PowerRelay relay)
    {
        if (powerRelays.Contains(relay))
            powerRelays.Remove(relay);
    }
}
