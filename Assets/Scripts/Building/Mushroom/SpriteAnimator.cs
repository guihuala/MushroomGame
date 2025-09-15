using UnityEngine;

[DisallowMultipleComponent]
public class SpriteAnimator : MonoBehaviour
{
    [Header("Target")]
    public SpriteRenderer targetRenderer;
    
    [Header("Global")]
    public float timeScale = 1f;
    [Range(0f, 360f)] public float phaseDegrees = 0f;
    
    [Header("Tilt (左右倾斜)")]
    public bool enableTilt = true;
    public float tiltAmplitudeDeg = 7.5f;
    public float tiltFrequency = 1.2f;
    public AnimationCurve tiltCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Squash & Stretch (挤压拉伸)")]
    public bool enableSquash = true;
    public float squashX = 0.08f;
    public float squashY = 0.12f;
    [Range(0f, 1f)] public float squashFollowBob = 0.85f;
    public float squashFrequency = 2.4f;
    public AnimationCurve squashCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Randomness")]
    public bool randomizePhase = true;
    [Range(0f, 45f)] public float randomPhaseJitterDeg = 15f;
    public int randomSeed = 0;

    // 内部状态
    Vector3 _initialLocalPos;
    Vector3 _initialLocalScale;
    Quaternion _initialLocalRot;
    float _randomPhaseOffset;

    void Reset()
    {
        TryAutoAssignRenderer();
    }

    void OnEnable()
    {
        TryAutoAssignRenderer();
        CacheInitialStates();
        ApplyRandomPhase();
    }

    void OnDisable()
    {
        if (targetRenderer != null)
        {
            var t = targetRenderer.transform;
            t.localScale = _initialLocalScale;
            t.localRotation = _initialLocalRot;
            t.localPosition = _initialLocalPos;
        }
    }

    void Update()
    {
        if (targetRenderer == null || targetRenderer.sprite == null)
            return;

        float t = (Application.isPlaying ? Time.time : 0f) * Mathf.Max(0.0001f, timeScale);
        float cycle = t + _randomPhaseOffset + phaseDegrees / 360f;

        Animate(cycle);
    }

    void TryAutoAssignRenderer()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    void CacheInitialStates()
    {
        if (targetRenderer != null)
        {
            var tr = targetRenderer.transform;
            _initialLocalPos = tr.localPosition;
            _initialLocalScale = tr.localScale;
            _initialLocalRot = tr.localRotation;
        }
    }

    void ApplyRandomPhase()
    {
        if (!randomizePhase) { _randomPhaseOffset = 0f; return; }
        System.Random rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);
        _randomPhaseOffset = (float)(rng.NextDouble() * (randomPhaseJitterDeg / 360f));
    }

    void Animate(float cycle)
    {
        float tt = Mathf.Repeat(cycle * tiltFrequency, 1f);
        float ts = Mathf.Repeat(cycle * squashFrequency, 1f);
        
        float tiltN = enableTilt ? EvalSymmetric(tiltCurve, tt) : 0f;

        float squashN = 0f;
        if (enableSquash)
        {
            if (squashFollowBob > 0f)
                squashN = Mathf.Lerp(EvalSymmetric(squashCurve, ts), 0, squashFollowBob);
            else
                squashN = EvalSymmetric(squashCurve, ts);
        }

        var tr = targetRenderer.transform;

        // 旋转：倾斜
        tr.localRotation = _initialLocalRot * Quaternion.Euler(0f, 0f, tiltAmplitudeDeg * tiltN);

        // 缩放：挤压拉伸
        float sx = 1f + squashX * squashN;
        float sy = 1f - squashY * squashN;
        tr.localScale = new Vector3(_initialLocalScale.x * sx, _initialLocalScale.y * sy, _initialLocalScale.z);
    }

    static float EvalSymmetric(AnimationCurve curve, float t01)
    {
        float sine = Mathf.Sin(t01 * Mathf.PI * 2f);
        float weight = Mathf.Clamp(curve.Evaluate(t01), -1f, 1f);
        return Mathf.Clamp(sine * (0.5f + 0.5f * weight), -1f, 1f);
    }
}
