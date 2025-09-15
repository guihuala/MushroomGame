using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class ParallaxEffect : MonoBehaviour
{
    [Header("视差设置")]
    [Tooltip("X/Y 方向的视差速度（0~1：跟随较少；>1：跟随更快）")]
    public Vector2 parallaxSpeed = new Vector2(0.5f, 0.5f);

    [Header("无限滚动")]
    public bool infiniteHorizontal = true;
    public bool infiniteVertical = false;

    [Header("重复单位")]
    [Tooltip("勾选后将忽略自动测量，使用下面的 repeatUnitSize 作为一块背景/Tilemap 的世界尺寸")]
    public bool overrideRepeatSize = false;
    [Tooltip("世界单位下的宽高")]
    public Vector2 repeatUnitSize = Vector2.zero;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    
    private float unitSizeX;
    private float unitSizeY;

    void Awake()
    {
        // 相机引用
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("[ParallaxEffect] 未找到主相机（MainCamera）。请确保场景中有带有 MainCamera 标签的相机。");
        }

        CacheRepeatUnitSize();
    }

    void Start()
    {
        if (cameraTransform != null)
            lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // 按 X/Y 分别的视差速度移动
        transform.position += new Vector3(deltaMovement.x * parallaxSpeed.x,
                                          deltaMovement.y * parallaxSpeed.y,
                                          0f);

        lastCameraPosition = cameraTransform.position;

        // 无限水平滚动
        if (infiniteHorizontal && unitSizeX > 0.0001f)
        {
            float distX = cameraTransform.position.x - transform.position.x;

            // 使用 Repeat 得到 [0, unitSizeX) 的偏移，避免负数取模问题
            if (Mathf.Abs(distX) >= unitSizeX)
            {
                float offsetX = Mathf.Repeat(distX, unitSizeX);
                transform.position = new Vector3(cameraTransform.position.x - offsetX,
                                                 transform.position.y,
                                                 transform.position.z);
            }
        }

        // 无限垂直滚动
        if (infiniteVertical && unitSizeY > 0.0001f)
        {
            float distY = cameraTransform.position.y - transform.position.y;

            if (Mathf.Abs(distY) >= unitSizeY)
            {
                float offsetY = Mathf.Repeat(distY, unitSizeY);
                transform.position = new Vector3(transform.position.x,
                                                 cameraTransform.position.y - offsetY,
                                                 transform.position.z);
            }
        }
    }
    
    private void CacheRepeatUnitSize()
    {
        if (overrideRepeatSize && repeatUnitSize != Vector2.zero)
        {
            unitSizeX = Mathf.Abs(repeatUnitSize.x);
            unitSizeY = Mathf.Abs(repeatUnitSize.y);
            return;
        }

        // 优先尝试 SpriteRenderer
        if (TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
        {
            // bounds 是世界空间并且包含缩放
            Vector3 size = sr.bounds.size;
            unitSizeX = Mathf.Abs(size.x);
            unitSizeY = Mathf.Abs(size.y);
            if (unitSizeX > 0f || unitSizeY > 0f) return;
        }

        // 再尝试 Tilemap（推荐）或 TilemapRenderer
        if (TryGetComponent<Tilemap>(out var tm))
        {
            // localBounds 是局部空间，乘以 lossyScale 得到世界空间大小
            Vector3 localSize = tm.localBounds.size;
            Vector3 worldSize = Vector3.Scale(localSize, transform.lossyScale);
            unitSizeX = Mathf.Abs(worldSize.x);
            unitSizeY = Mathf.Abs(worldSize.y);
            if (unitSizeX > 0f || unitSizeY > 0f) return;
        }

        if (TryGetComponent<TilemapRenderer>(out var tmr))
        {
            Vector3 size = tmr.bounds.size;
            unitSizeX = Mathf.Abs(size.x);
            unitSizeY = Mathf.Abs(size.y);
            if (unitSizeX > 0f || unitSizeY > 0f) return;
        }

        // 任意 Renderer 兜底
        if (TryGetComponent<Renderer>(out var r))
        {
            Vector3 size = r.bounds.size;
            unitSizeX = Mathf.Abs(size.x);
            unitSizeY = Mathf.Abs(size.y);
        }

        // 若仍为 0，提示用户手动覆盖
        if (unitSizeX <= 0.0001f || unitSizeY <= 0.0001f)
        {
            Debug.LogWarning("[ParallaxEffect] 无法自动测量重复单位尺寸。请勾选 overrideRepeatSize 并填写 repeatUnitSize。");
        }
    }

#if UNITY_EDITOR
    // 在编辑器里参数变化时，方便重新计算
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (!overrideRepeatSize)
                CacheRepeatUnitSize();
            else
            {
                unitSizeX = Mathf.Abs(repeatUnitSize.x);
                unitSizeY = Mathf.Abs(repeatUnitSize.y);
            }
        }
    }
#endif
}