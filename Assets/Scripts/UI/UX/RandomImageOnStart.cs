using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RandomImageOnStart : MonoBehaviour
{
    public enum TargetKind { Auto, UIImage, SpriteRenderer }

    [Header("候选图片")]
    [SerializeField] private List<Sprite> candidates = new List<Sprite>();

    [Header("目标绑定")]
    [SerializeField] private TargetKind targetKind = TargetKind.Auto;
    [SerializeField] private Image uiImage;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("触发时机")]
    [Tooltip("在 Start() 时随机一次")]
    [SerializeField] private bool pickOnStart = true;
    [Tooltip("在 OnEnable() 时也随机一次（适合对象反复启用/禁用）")]
    [SerializeField] private bool pickOnEnable = false;

    [Header("行为设置")]
    [Tooltip("避免与上一次结果重复（仅对本组件实例有效）")]
    [SerializeField] private bool avoidImmediateRepeat = true;
    [Tooltip("固定随机种子（<0 表示不固定）")]
    [SerializeField] private int fixedSeed = -1;

    private int _lastIndex = -1;
    private System.Random _rng;

    private void Awake()
    {
        if (targetKind == TargetKind.Auto)
        {
            if (uiImage != null || TryGetComponent(out uiImage))
            {
                targetKind = TargetKind.UIImage;
            }
            else if (spriteRenderer != null || TryGetComponent(out spriteRenderer))
            {
                targetKind = TargetKind.SpriteRenderer;
            }
            else
            {
                // 两者都没有时，尝试父子物体里找一个 Image
                uiImage = GetComponentInChildren<Image>();
                if (uiImage != null) targetKind = TargetKind.UIImage;
            }
        }

        // 初始化随机数
        _rng = fixedSeed >= 0 ? new System.Random(fixedSeed) : new System.Random();
    }

    private void OnEnable()
    {
        if (pickOnEnable) PickAndApply();
    }

    private void Start()
    {
        if (pickOnStart) PickAndApply();
    }

    /// <summary>手动触发随机挑选（你也可以在别的脚本里调用）</summary>
    public void PickAndApply()
    {
        if (candidates == null || candidates.Count == 0)
        {
            Debug.LogWarning($"[{nameof(RandomImageOnStart)}] 候选列表为空。", this);
            return;
        }

        int idx = NextIndex();
        var sprite = candidates[idx];

        switch (targetKind)
        {
            case TargetKind.UIImage:
                if (uiImage == null && !TryGetComponent(out uiImage))
                {
                    Debug.LogWarning($"[{nameof(RandomImageOnStart)}] 未找到 Image 组件。", this);
                    return;
                }
                uiImage.sprite = sprite;
                break;

            case TargetKind.SpriteRenderer:
                if (spriteRenderer == null && !TryGetComponent(out spriteRenderer))
                {
                    Debug.LogWarning($"[{nameof(RandomImageOnStart)}] 未找到 SpriteRenderer 组件。", this);
                    return;
                }
                spriteRenderer.sprite = sprite;
                break;

            default:
                Debug.LogWarning($"[{nameof(RandomImageOnStart)}] 目标类型未识别，无法赋值。", this);
                break;
        }
    }

    /// <summary>从列表里取一个随机索引，尽量避免与上次相同。</summary>
    private int NextIndex()
    {
        int count = candidates.Count;
        if (!avoidImmediateRepeat || count <= 1 || _lastIndex < 0)
        {
            _lastIndex = _rng.Next(0, count);
            return _lastIndex;
        }

        // 避免与上一次相同
        int idx;
        do { idx = _rng.Next(0, count); }
        while (idx == _lastIndex && count > 1);

        _lastIndex = idx;
        return idx;
    }
}
