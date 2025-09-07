using UnityEngine;

public class LightSource : Building
{
    [Header("光源设置")]
    public float lightRadius = 5f;

    [Header("视觉效果")]
    public SpriteRenderer lightHalo;
    public float haloBaseAlpha = 0.6f;
    
    protected bool isPlaced = false;
    private float _pulseTimer;
    
    public override void OnPlaced(TileGridService grid, Vector2Int cell)
    {
        base.OnPlaced(grid, cell);
        isPlaced = true;
    }
    
    public override void OnRemoved()
    {
        
        SetLightVisuals(false, 0f);
        base.OnRemoved();
    }
    
    void Update()
    {
        if (isPlaced)
        {
            _pulseTimer += Time.deltaTime;
            if (lightHalo != null)
            {
                float pulse = Mathf.Sin(_pulseTimer) * 0.1f + 0.9f;
                Color color = lightHalo.color;
                color.a = haloBaseAlpha * pulse;
                lightHalo.color = color;
            }
        }
    }
    
    public void SetLightVisuals(bool enabled, float radius)
    {
        if (lightHalo != null)
        {
            lightHalo.enabled = enabled;
            if (enabled) lightHalo.transform.localScale = Vector3.one * radius * 0.3f;
        }
    }
    
    public void UpdateVisuals(float radius)
    {
        if (lightHalo != null)
        {
            lightHalo.transform.localScale = Vector3.one * radius * 0.3f;
        }
    }
}