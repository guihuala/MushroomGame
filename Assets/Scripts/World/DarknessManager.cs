using System.Collections.Generic;
using UnityEngine;

public class DarknessManager : Singleton<DarknessManager>,IManager
{
    [Header("黑暗视觉效果")]
    public Material darknessMaterial;
    public float updateInterval = 0.1f;
    public int maxLights = 50;
    
    [Header("Shader参数")]
    public Color darknessColor = new Color(0, 0, 0, 0.8f);
    
    private float _updateTimer;
    
    // 黑暗等级缓存
    private Dictionary<Vector2Int, float> _cellDarknessLevels = new Dictionary<Vector2Int, float>();

    // 初始化方法
    public void Initialize()
    {
        DebugManager.Log("DarknessManager initialized");
        _updateTimer = updateInterval;
        _cellDarknessLevels.Clear();
        InitializeMaterial();
    }
    
    void Start()
    {
        _updateTimer = updateInterval;
        InitializeMaterial();
    }
    
    void Update()
    {
        _updateTimer -= Time.deltaTime;
        if (_updateTimer <= 0f)
        {
            _updateTimer = updateInterval;
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (darknessMaterial != null)
        {
            Graphics.Blit(source, destination, darknessMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    private void InitializeMaterial()
    {
        if (darknessMaterial == null)
        {
            Shader darknessShader = Shader.Find("Custom/Darkness2D");
            if (darknessShader != null)
            {
                darknessMaterial = new Material(darknessShader);
            }
        }
        
        if (darknessMaterial != null)
        {
            darknessMaterial.SetColor("_DarknessColor", darknessColor);
            // 初始化空数组
            Vector4[] emptyPositions = new Vector4[maxLights];
            float[] emptyRadiuses = new float[maxLights];
            for (int i = 0; i < maxLights; i++)
            {
                emptyPositions[i] = Vector4.zero;
                emptyRadiuses[i] = 0f;
            }
            darknessMaterial.SetInt("_LightCount", 0);
            darknessMaterial.SetVectorArray("_LightPositions", emptyPositions);
            darknessMaterial.SetFloatArray("_LightRadiuses", emptyRadiuses);
        }
    }
}