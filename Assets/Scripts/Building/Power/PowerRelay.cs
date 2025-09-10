using UnityEngine;

public class PowerRelay : Building
{
    [Header("电力中继器设置")]
    public float powerTransmissionRange = 5f;
    public float powerLossPerTransmission = 0.1f;

    [Header("范围可视化")]
    public bool showRingOnHover = true;
    public Color ringColor = new Color(0.3f, 0.8f, 1f, 0.45f);
    public int ringSegments = 64;
    private LineRenderer _ring;

    public override void OnPlaced(TileGridService g, Vector2Int c)
    {
        base.OnPlaced(g, c);
        PowerManager.Instance.AddPowerRelay(this);
        EnsureRing();
        SetRingVisible(false);
    }

    public override void OnRemoved()
    {
        PowerManager.Instance.RemovePowerRelay(this);
        DestroyRing();
        base.OnRemoved();
    }

    private void EnsureRing()
    {
        if (_ring != null) return;
        _ring = gameObject.AddComponent<LineRenderer>();
        _ring.loop = true;
        _ring.useWorldSpace = false;
        _ring.widthMultiplier = 0.05f;
        _ring.material = new Material(Shader.Find("Sprites/Default"));
        _ring.startColor = _ring.endColor = ringColor;
        _ring.positionCount = ringSegments;
        for (int i = 0; i < ringSegments; i++)
        {
            float t = i / (float)ringSegments * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * powerTransmissionRange);
        }
        _ring.sortingOrder = 5000;
    }

    private void DestroyRing()
    {
        if (_ring != null) Destroy(_ring);
    }

    private void SetRingVisible(bool v)
    {
        if (_ring) _ring.enabled = v && showRingOnHover;
    }

    private void OnMouseEnter() => SetRingVisible(true);
    private void OnMouseExit()  => SetRingVisible(false);

    public void TransmitPower(Vector2Int targetCell)
    {
        float distance = Vector2Int.Distance(cell, targetCell);
        if (distance <= powerTransmissionRange)
        {
            float transmittedPower = PowerManager.Instance.GetPower() * (1 - powerLossPerTransmission);
            PowerManager.Instance.SetPower(transmittedPower);
        }
    }
}