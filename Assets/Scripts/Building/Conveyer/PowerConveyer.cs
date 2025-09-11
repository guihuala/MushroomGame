using UnityEngine;

public class PowerConveyer : Conveyer
{
    [Header("电力需求")]
    [Tooltip("每个物品每秒的耗电（只在移动时扣除）")]
    public float powerPerSecondPerItem = 0.2f;

    [Header("速度")]
    [Tooltip("有电且电量充足时的速度（格/秒）")]
    public float poweredSpeed = 2.0f;
    [Tooltip("无电或电量不足时的速度（通常为 0）")]
    public float powerlessSpeed = 0.0f;

    // 覆写调度：按电力状态决定这帧的速度 & 是否扣电
    public override void StepMove(float dt)
    {
        // 未接入电网覆盖 → 按断电速度运行（一般为 0）
        bool covered = PowerManager.Instance.IsCellPowered(cell, grid);
        float useSpeed = powerlessSpeed;

        if (covered)
        {
            // 估算本帧需要的电量（按“在带上的物品数×时间×单价”）
            int movingCount = Items.Count;
            float need = movingCount * powerPerSecondPerItem * dt;

            if (movingCount == 0 || PowerManager.Instance.TryConsumePower(need))
            {
                // 有电且电够 → 用通电速度
                useSpeed = poweredSpeed;
            }
            else
            {
                // 电不够 → 退回断电速度
                useSpeed = powerlessSpeed;
            }
        }

        float prev = beltSpeed;
        beltSpeed = useSpeed;
        base.StepMove(dt);
        beltSpeed = prev;
    }
}
