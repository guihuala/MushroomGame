using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeatherType
{
    Sunny,
    Cloudy,
    Rainy,
    Snowy,
}

[System.Serializable]
public class WeatherCondition
{
    public WeatherType type;
    public float minTemperature;
    public float maxTemperature;
    public float minHumidity;
    public float maxHumidity;
    public ParticleSystem particleEffect;
    public Color skyColor = Color.cyan;
    public Color ambientLightColor = Color.white;
    public float ambientIntensity = 1f;
}

public class WeatherManager : MonoBehaviour
{
    [Header("温度范围")]
    public float minTemperature = -10f;
    public float maxTemperature = 40f;
    
    [Header("湿度范围")]
    public float minHumidity = 0f;
    public float maxHumidity = 100f;
    
    [Header("天气条件设置")]
    public List<WeatherCondition> weatherConditions = new List<WeatherCondition>();
    
    [Header("天气变化设置")]
    public float weatherChangeInterval = 60f;
    public float transitionDuration = 5f;
    
    [Header("2D天空背景引用")]
    public SpriteRenderer skyBackground;
    public Camera mainCamera;
    
    [Header("调试信息")]
    public float currentTemperature;
    public float currentHumidity;
    public WeatherType currentWeather;
    public WeatherType previousWeather;
    
    private Dictionary<WeatherType, ParticleSystem> activeParticles = new Dictionary<WeatherType, ParticleSystem>();
    private Coroutine weatherChangeCoroutine;
    private Coroutine transitionCoroutine;
    
    // 环境设置缓存
    private Color originalSkyColor;
    private Color originalAmbientLight;
    private float originalAmbientIntensity;
    
    void Start()
    {
        CacheOriginalEnvironmentSettings();
        InitializeWeatherSystem();
        StartWeatherChanges();
    }
    
    void CacheOriginalEnvironmentSettings()
    {
        if (skyBackground != null)
            originalSkyColor = skyBackground.color;
        
        originalAmbientLight = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
    }
    
    void InitializeWeatherSystem()
    {
        if (weatherConditions.Count == 0)
        {
            SetupDefaultWeatherConditions();
        }
        
        // 预先实例化所有粒子系统并停用
        foreach (var condition in weatherConditions)
        {
            if (condition.particleEffect != null)
            {
                ParticleSystem particleInstance = Instantiate(condition.particleEffect, transform);
                particleInstance.gameObject.SetActive(false);
                activeParticles.Add(condition.type, particleInstance);
            }
        }
    }
    
    void SetupDefaultWeatherConditions()
    {
        // 晴朗天气
        weatherConditions.Add(new WeatherCondition()
        {
            type = WeatherType.Sunny,
            minTemperature = 20f,
            maxTemperature = 40f,
            minHumidity = 0f,
            maxHumidity = 30f,
            skyColor = new Color(0.8f, 0.9f, 1f, 1f), // 明亮的蓝色
            ambientLightColor = new Color(1f, 1f, 0.95f, 1f), // 温暖的阳光
            ambientIntensity = 1.2f
        });
        
        // 多云天气
        weatherConditions.Add(new WeatherCondition()
        {
            type = WeatherType.Cloudy,
            minTemperature = 10f,
            maxTemperature = 25f,
            minHumidity = 30f,
            maxHumidity = 60f,
            skyColor = new Color(0.7f, 0.75f, 0.8f, 1f), // 灰蓝色
            ambientLightColor = new Color(0.9f, 0.9f, 0.9f, 1f), // 中性光
            ambientIntensity = 0.9f
        });
        
        // 雨天
        weatherConditions.Add(new WeatherCondition()
        {
            type = WeatherType.Rainy,
            minTemperature = 5f,
            maxTemperature = 20f,
            minHumidity = 70f,
            maxHumidity = 100f,
            skyColor = new Color(0.5f, 0.6f, 0.7f, 1f), // 灰暗的蓝色
            ambientLightColor = new Color(0.8f, 0.8f, 0.85f, 1f), // 冷色调
            ambientIntensity = 0.7f
        });
        
        // 雪天
        weatherConditions.Add(new WeatherCondition()
        {
            type = WeatherType.Snowy,
            minTemperature = -10f,
            maxTemperature = 0f,
            minHumidity = 60f,
            maxHumidity = 100f,
            skyColor = new Color(0.85f, 0.9f, 0.95f, 1f), // 明亮的蓝白色
            ambientLightColor = new Color(0.95f, 0.95f, 1f, 1f), // 冷白色
            ambientIntensity = 1.1f
        });
    }
    
    void StartWeatherChanges()
    {
        if (weatherChangeCoroutine != null)
        {
            StopCoroutine(weatherChangeCoroutine);
        }
        weatherChangeCoroutine = StartCoroutine(WeatherChangeRoutine());
    }
    
    IEnumerator WeatherChangeRoutine()
    {
        // 初始天气
        currentTemperature = Random.Range(minTemperature, maxTemperature);
        currentHumidity = Random.Range(minHumidity, maxHumidity);
        DetermineWeather();
        
        while (true)
        {
            yield return new WaitForSeconds(weatherChangeInterval);
            
            // 随机生成温度和湿度（基于当前值小幅变化）
            currentTemperature = Mathf.Clamp(
                currentTemperature + Random.Range(-5f, 5f), 
                minTemperature, 
                maxTemperature
            );
            currentHumidity = Mathf.Clamp(
                currentHumidity + Random.Range(-10f, 10f), 
                minHumidity, 
                maxHumidity
            );
            
            // 根据条件确定天气
            DetermineWeather();
        }
    }
    
    void DetermineWeather()
    {
        // 找出符合条件的天气类型
        List<WeatherType> possibleWeathers = new List<WeatherType>();
        
        foreach (var condition in weatherConditions)
        {
            if (currentTemperature >= condition.minTemperature && 
                currentTemperature <= condition.maxTemperature &&
                currentHumidity >= condition.minHumidity && 
                currentHumidity <= condition.maxHumidity)
            {
                possibleWeathers.Add(condition.type);
            }
        }
        
        // 随机选择一个符合条件的天气
        WeatherType newWeather;
        if (possibleWeathers.Count > 0)
        {
            newWeather = possibleWeathers[Random.Range(0, possibleWeathers.Count)];
        }
        else
        {
            Debug.LogWarning("没有找到符合条件的天气类型");
            newWeather = WeatherType.Sunny;
        }
        
        // 如果天气发生变化，启动过渡
        if (newWeather != currentWeather)
        {
            previousWeather = currentWeather;
            currentWeather = newWeather;
            
            Debug.Log($"天气变化: {previousWeather} -> {currentWeather}, 温度: {currentTemperature}, 湿度: {currentHumidity}%");
            
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionToNewWeather());
        }
    }
    
    IEnumerator TransitionToNewWeather()
    {
        float elapsedTime = 0f;
        WeatherCondition fromCondition = GetWeatherCondition(previousWeather);
        WeatherCondition toCondition = GetWeatherCondition(currentWeather);
        
        // 获取当前环境设置作为起始值
        Color startSkyColor = skyBackground != null ? skyBackground.color : Color.white;
        Color startAmbientLight = RenderSettings.ambientLight;
        float startAmbientIntensity = RenderSettings.ambientIntensity;
        
        // 淡出旧天气的粒子效果
        if (activeParticles.ContainsKey(previousWeather) && activeParticles[previousWeather] != null)
        {
            ParticleSystem oldParticles = activeParticles[previousWeather];
            var emission = oldParticles.emission;
            emission.rateOverTime = 0f;
        }
        
        // 淡入新天气的粒子效果
        if (activeParticles.ContainsKey(currentWeather) && activeParticles[currentWeather] != null)
        {
            ParticleSystem newParticles = activeParticles[currentWeather];
            newParticles.gameObject.SetActive(true);
            var emission = newParticles.emission;
            emission.rateOverTime = 0f;
            newParticles.Play();
        }
        
        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            // 平滑过渡天空颜色
            if (skyBackground != null)
            {
                skyBackground.color = Color.Lerp(startSkyColor, toCondition.skyColor, smoothT);
            }
            else if (mainCamera != null)
            {
                mainCamera.backgroundColor = Color.Lerp(startSkyColor, toCondition.skyColor, smoothT);
            }
            
            // 平滑过渡环境光
            RenderSettings.ambientLight = Color.Lerp(startAmbientLight, toCondition.ambientLightColor, smoothT);
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbientIntensity, toCondition.ambientIntensity, smoothT);
            
            // 平滑过渡粒子效果
            if (activeParticles.ContainsKey(previousWeather) && activeParticles[previousWeather] != null)
            {
                ParticleSystem oldParticles = activeParticles[previousWeather];
                var emission = oldParticles.emission;
                emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0f, smoothT);
            }
            
            if (activeParticles.ContainsKey(currentWeather) && activeParticles[currentWeather] != null)
            {
                ParticleSystem newParticles = activeParticles[currentWeather];
                var emission = newParticles.emission;
                var originalEmission = newParticles.emission;
                emission.rateOverTime = Mathf.Lerp(0f, GetOriginalEmissionRate(newParticles), smoothT);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终状态正确
        ApplyWeatherEnvironment(toCondition);
        
        // 完全停用旧天气的粒子
        if (activeParticles.ContainsKey(previousWeather) && activeParticles[previousWeather] != null)
        {
            activeParticles[previousWeather].gameObject.SetActive(false);
        }
    }
    
    float GetOriginalEmissionRate(ParticleSystem ps)
    {
        // 获取粒子系统的原始发射率
        var emission = ps.emission;
        if (emission.rateOverTime.constant > 0)
            return emission.rateOverTime.constant;
        
        // 如果没有设置常数发射率，返回一个默认值
        return 10f;
    }
    
    WeatherCondition GetWeatherCondition(WeatherType type)
    {
        foreach (var condition in weatherConditions)
        {
            if (condition.type == type)
            {
                return condition;
            }
        }
        return weatherConditions[0];
    }
    
    void ApplyWeatherEnvironment(WeatherCondition condition)
    {
        if (skyBackground != null)
        {
            skyBackground.color = condition.skyColor;
        }
        else if (mainCamera != null)
        {
            mainCamera.backgroundColor = condition.skyColor;
        }
        
        RenderSettings.ambientLight = condition.ambientLightColor;
        RenderSettings.ambientIntensity = condition.ambientIntensity;
    }
    
    // 手动改变天气（带过渡）
    public void ChangeWeatherManually(WeatherType weatherType)
    {
        previousWeather = currentWeather;
        currentWeather = weatherType;
        
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionToNewWeather());
    }
    
    // 停止天气变化
    public void StopWeatherChanges()
    {
        if (weatherChangeCoroutine != null)
        {
            StopCoroutine(weatherChangeCoroutine);
            weatherChangeCoroutine = null;
        }
    }
    
    void OnDestroy()
    {
        // 恢复原始环境设置
        if (skyBackground != null)
            skyBackground.color = originalSkyColor;
        
        RenderSettings.ambientLight = originalAmbientLight;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
    }
    
    // 获取当前天气的调试信息
    public string GetWeatherInfo()
    {
        return $"天气: {currentWeather}, 温度: {currentTemperature:F1}°C, 湿度: {currentHumidity:F1}%";
    }
}