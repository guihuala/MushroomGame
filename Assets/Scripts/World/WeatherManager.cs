using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    
    public Color ambientLightColor = Color.white;
    [Range(0f, 2f)] public float ambientIntensity = 1f;
    
    public float targetEmissionRate = -1f;
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

    [Header("URP 2D Lighting")]
    [Tooltip("场景中的 Global Light 2D（类型设为 Global）。")]
    public Light2D globalLight;

    [Header("调试信息")]
    public float currentTemperature;
    public float currentHumidity;
    public WeatherType currentWeather;
    public WeatherType previousWeather;

    private readonly Dictionary<WeatherType, ParticleSystem> activeParticles = new();
    private readonly Dictionary<WeatherType, float> particleBaseRate = new(); // 记录初始发射率
    private Coroutine weatherChangeCoroutine;
    private Coroutine transitionCoroutine;
    
    private Color originalLightColor = Color.white;
    private float originalLightIntensity = 1f;
    private Color originalAmbientLight;
    private float originalAmbientIntensity;

    void Start()
    {
        CacheOriginalEnvironmentSettings();
        InitializeWeatherSystem();

        // 初始化温湿度并确定首个天气
        currentTemperature = Random.Range(minTemperature, maxTemperature);
        currentHumidity = Random.Range(minHumidity, maxHumidity);
        DetermineWeather();
        
        ApplyWeatherEnvironment(GetWeatherCondition(currentWeather));

        StartWeatherChanges();
    }

    void CacheOriginalEnvironmentSettings()
    {
        if (globalLight != null)
        {
            originalLightColor = globalLight.color;
            originalLightIntensity = globalLight.intensity;
        }

        originalAmbientLight = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
    }

    void InitializeWeatherSystem()
    {
        if (weatherConditions.Count == 0)
            SetupDefaultWeatherConditions();

        // 预实例化粒子 & 记录基础发射率
        foreach (var condition in weatherConditions)
        {
            if (condition.particleEffect != null)
            {
                var ps = Instantiate(condition.particleEffect, transform);
                ps.gameObject.SetActive(false);
                activeParticles[condition.type] = ps;

                var emission = ps.emission;
                float baseRate = emission.rateOverTime.constant;
                particleBaseRate[condition.type] = baseRate > 0 ? baseRate : 10f; // 合理默认
            }
        }

        if (globalLight == null)
            Debug.LogWarning("[WeatherManager] 未指定 Global Light 2D，将回退到 RenderSettings。建议绑定 Global Light 2D。");
    }

    void SetupDefaultWeatherConditions()
    {
        weatherConditions.Add(new WeatherCondition {
            type = WeatherType.Sunny,
            minTemperature = 20f, maxTemperature = 45f,
            minHumidity = 0f,  maxHumidity = 40f,
            ambientLightColor = new Color(1.00f, 0.98f, 0.90f, 1f), // 微暖、偏黄
            particleEffect = Resources.Load<ParticleSystem>("Particles/Sunny"),
            ambientIntensity  = 1.25f,
            targetEmissionRate = 0f
        });

        weatherConditions.Add(new WeatherCondition {
            type = WeatherType.Cloudy,
            minTemperature = 10f, maxTemperature = 28f,
            minHumidity = 30f, maxHumidity = 70f,
            ambientLightColor = new Color(0.80f, 0.82f, 0.85f, 1f), // 偏冷中性
            particleEffect = Resources.Load<ParticleSystem>("Particles/Cloudy"),
            ambientIntensity  = 0.65f,
            targetEmissionRate = 0f
        });

        weatherConditions.Add(new WeatherCondition {
            type = WeatherType.Rainy,
            minTemperature = 5f,  maxTemperature = 22f,
            minHumidity = 70f, maxHumidity = 100f,
            ambientLightColor = new Color(0.78f, 0.82f, 0.90f, 1f), // 稍冷、偏蓝
            particleEffect = Resources.Load<ParticleSystem>("Particles/Rainy"),
            ambientIntensity  = 0.70f,
            targetEmissionRate = 30f // 下雨粒子默认发射率
        });

        weatherConditions.Add(new WeatherCondition {
            type = WeatherType.Snowy,
            minTemperature = -15f, maxTemperature = 2f,
            minHumidity = 60f,  maxHumidity = 100f,
            ambientLightColor = new Color(0.95f, 0.97f, 1.00f, 1f), // 冷白、略亮
            particleEffect = Resources.Load<ParticleSystem>("Particles/Snowy"),
            ambientIntensity  = 1.10f,
            targetEmissionRate = 20f // 飘雪较轻
        });
    }

    void StartWeatherChanges()
    {
        if (weatherChangeCoroutine != null)
            StopCoroutine(weatherChangeCoroutine);

        weatherChangeCoroutine = StartCoroutine(WeatherChangeRoutine());
    }

    IEnumerator WeatherChangeRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(weatherChangeInterval);

            currentTemperature = Mathf.Clamp(currentTemperature + Random.Range(-5f, 5f), minTemperature, maxTemperature);
            currentHumidity    = Mathf.Clamp(currentHumidity + Random.Range(-10f, 10f), minHumidity, maxHumidity);

            DetermineWeather();
        }
    }

    void DetermineWeather()
    {
        var possible = new List<WeatherType>();
        foreach (var c in weatherConditions)
        {
            if (currentTemperature >= c.minTemperature && currentTemperature <= c.maxTemperature
             && currentHumidity    >= c.minHumidity    && currentHumidity    <= c.maxHumidity)
                possible.Add(c.type);
        }

        WeatherType newWeather = possible.Count > 0 ? possible[Random.Range(0, possible.Count)] : WeatherType.Sunny;

        if (newWeather != currentWeather)
        {
            previousWeather = currentWeather;
            currentWeather  = newWeather;

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(TransitionToNewWeather());
        }
    }

    IEnumerator TransitionToNewWeather()
    {
        float elapsed = 0f;
        var toCondition = GetWeatherCondition(currentWeather);
        
        Color startLightColor = (globalLight != null) ? globalLight.color : RenderSettings.ambientLight;
        float startLightIntensity = (globalLight != null) ? globalLight.intensity : RenderSettings.ambientIntensity;

        // 粒子起止发射率
        float prevStartRate = 0f, prevTargetRate = 0f;
        float currStartRate = 0f, currTargetRate = 0f;

        if (activeParticles.TryGetValue(previousWeather, out var prevPs) && prevPs != null)
        {
            var em = prevPs.emission;
            prevPs.gameObject.SetActive(true);
            prevStartRate  = em.rateOverTime.constant;
            prevTargetRate = 0f; // 逐步停
        }

        if (activeParticles.TryGetValue(currentWeather, out var currPs) && currPs != null)
        {
            var em = currPs.emission;
            currPs.gameObject.SetActive(true);
            currPs.Play();

            currStartRate  = 0f;
            float baseRate = (particleBaseRate.TryGetValue(currentWeather, out var br) ? br : 10f);
            currTargetRate = (toCondition.targetEmissionRate >= 0f) ? toCondition.targetEmissionRate : baseRate;
            em.rateOverTime = currStartRate;
        }

        while (elapsed < transitionDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            // Light2D 颜色/强度过渡
            if (globalLight != null)
            {
                globalLight.color     = Color.Lerp(startLightColor, toCondition.ambientLightColor, t);
                globalLight.intensity = Mathf.Lerp(startLightIntensity, toCondition.ambientIntensity, t);
            }
            else
            {
                RenderSettings.ambientLight     = Color.Lerp(startLightColor, toCondition.ambientLightColor, t);
                RenderSettings.ambientIntensity = Mathf.Lerp(startLightIntensity, toCondition.ambientIntensity, t);
            }

            // 粒子过渡
            if (prevPs != null)
            {
                var em = prevPs.emission;
                em.rateOverTime = Mathf.Lerp(prevStartRate, prevTargetRate, t);
            }
            if (currPs != null)
            {
                var em = currPs.emission;
                em.rateOverTime = Mathf.Lerp(currStartRate, currTargetRate, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 锁定终值
        ApplyWeatherEnvironment(toCondition);

        if (prevPs != null)
        {
            var em = prevPs.emission;
            em.rateOverTime = 0f;
            prevPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            prevPs.gameObject.SetActive(false);
        }
    }

    WeatherCondition GetWeatherCondition(WeatherType type)
    {
        // 找不到就返回第一项，防止空引用
        foreach (var c in weatherConditions)
            if (c.type == type) return c;
        return weatherConditions.Count > 0 ? weatherConditions[0] : new WeatherCondition();
    }

    void ApplyWeatherEnvironment(WeatherCondition c)
    {
        if (globalLight != null)
        {
            globalLight.color     = c.ambientLightColor;
            globalLight.intensity = c.ambientIntensity;
        }
        else
        {
            RenderSettings.ambientLight     = c.ambientLightColor;
            RenderSettings.ambientIntensity = c.ambientIntensity;
        }

        // 只保留当前天气的粒子（如果有）
        foreach (var kv in activeParticles)
        {
            bool isCurrent = kv.Key == c.type;
            if (kv.Value == null) continue;

            var em = kv.Value.emission;
            if (isCurrent)
            {
                float baseRate = (particleBaseRate.TryGetValue(kv.Key, out var br) ? br : 10f);
                float target   = (c.targetEmissionRate >= 0f) ? c.targetEmissionRate : baseRate;
                em.rateOverTime = target;
                kv.Value.gameObject.SetActive(true);
                kv.Value.Play();
            }
            else
            {
                em.rateOverTime = 0f;
                kv.Value.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                kv.Value.gameObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (globalLight != null)
        {
            globalLight.color     = originalLightColor;
            globalLight.intensity = originalLightIntensity;
        }

        RenderSettings.ambientLight     = originalAmbientLight;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
    }
}
