using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    [Tooltip("鼠标悬停时的缩放倍数")]
    public float hoverScale = 1.1f;
    [Tooltip("动画时长")]
    public float animationDuration = 0.2f;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * hoverScale, animationDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, animationDuration);
    }

    private void OnDestroy()
    {
        if (transform != null)
        {
            DOTween.Kill(transform);
        }
    }
}
