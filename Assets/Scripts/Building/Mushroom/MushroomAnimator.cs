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
}