using UnityEngine;
using DG.Tweening; 

public class MushroomAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float squashAmount = 0.8f;      // 压扁的程度
    public float squashDuration = 0.1f;    // 压扁持续的时间
    public float recoveryDuration = 0.1f;  // 恢复的时间

    private Vector3 _originalScale;
    private bool _isAnimating = false;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }
    
    public void PlaySquashAndStretch()
    {
        if (!_isAnimating)
        {
            StartCoroutine(SquashAndStretchRoutine());
        }
    }

    private System.Collections.IEnumerator SquashAndStretchRoutine()
    {
        _isAnimating = true;
        
        transform.DOScale(_originalScale * squashAmount, squashDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(squashDuration);
        transform.DOScale(_originalScale, recoveryDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(recoveryDuration);

        _isAnimating = false;
    }
    
    public static GameObject CreateBadge(Transform parent, Sprite sprite, Vector3 localPos, float scale = 1f, int sortingOrder = 5000, string name = "StageBadge")
    {
        if (parent == null || sprite == null) return null;

        var go = new GameObject(name);
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        var anim = go.AddComponent<MushroomAnimator>();
        anim.PlaySquashAndStretch(); // 生成时播放“弹一下”的动效

        return go;
    }
}