using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightSource : Building
{
    [Header("光源设置")]
    public float lightRadius = 5f;
    public float lightIntensity = 1f;

    [Header("视觉效果")]
    public SpriteRenderer lightHalo;
    public float haloBaseAlpha = 0.6f;
    
    protected bool isPlaced = false;
    private float _pulseTimer;
    private Light2D _light2DComponent;
    
    public override void OnPlaced(TileGridService grid, Vector2Int cell)
    {
        base.OnPlaced(grid, cell);
        isPlaced = true;
        
        EnsureLight2DComponent();
        
        SetLightVisuals(true, lightRadius);
    }
    
    public override void OnRemoved()
    {
        SetLightVisuals(false, 0f);
        
        if (_light2DComponent != null)
        {
            Destroy(_light2DComponent);
        }
        
        base.OnRemoved();
    }
    
    void Start()
    {
        EnsureLight2DComponent();
    }
    
    void Update()
    {
        if (isPlaced)
        {
            _pulseTimer += Time.deltaTime;
            
            UpdateHaloPulse();
        }
    }
    
    private void EnsureLight2DComponent()
    {
        _light2DComponent = GetComponent<Light2D>();
        if (_light2DComponent == null)
        {
            _light2DComponent = gameObject.AddComponent<Light2D>();
            SetupLight2DComponent();
        }
    }
    
    private void SetupLight2DComponent()
    {
        _light2DComponent.lightType = Light2D.LightType.Point;
        _light2DComponent.pointLightOuterRadius = lightRadius;
        _light2DComponent.intensity = lightIntensity;
        _light2DComponent.color = Color.white;
        _light2DComponent.enabled = isPlaced;
    }
    
    private void UpdateHaloPulse()
    {
        if (lightHalo != null)
        {
            float pulse = Mathf.Sin(_pulseTimer * 2f) * 0.1f + 0.9f;
            Color color = lightHalo.color;
            color.a = haloBaseAlpha * pulse;
            lightHalo.color = color;
        }
    }
    
    public void SetLightVisuals(bool enabled, float radius)
    {
        if (lightHalo != null)
        {
            lightHalo.enabled = enabled;
            if (enabled) 
            {
                lightHalo.transform.localScale = Vector3.one * radius * 0.3f;
            }
        }
        
        if (_light2DComponent != null)
        {
            _light2DComponent.enabled = enabled;
        }
    }
    
    public void UpdateLightRadius(float newRadius)
    {
        lightRadius = newRadius;
        if (_light2DComponent != null)
        {
            _light2DComponent.pointLightOuterRadius = newRadius;
        }
        UpdateVisuals(newRadius);
    }
    
    public void UpdateLightIntensity(float newIntensity)
    {
        lightIntensity = newIntensity;
        if (_light2DComponent != null)
        {
            _light2DComponent.intensity = newIntensity;
        }
    }
    
    public void UpdateVisuals(float radius)
    {
        if (lightHalo != null)
        {
            lightHalo.transform.localScale = Vector3.one * radius * 0.3f;
        }
    }
    
    void OnValidate()
    {
        if (_light2DComponent != null)
        {
            _light2DComponent.pointLightOuterRadius = lightRadius;
            _light2DComponent.intensity = lightIntensity;
        }
        
        if (lightHalo != null && isPlaced)
        {
            lightHalo.transform.localScale = Vector3.one * lightRadius * 0.3f;
        }
    }
}