using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipsController : Singleton<TipsController>
{
    [Header("UI Elements")] public GameObject tipTextPrefab;
    public GameObject tipIconPrefab;
    public GameObject tipTextAndIconPrefab;

    [Header("References")] public Canvas uiCanvas;

    [Header("Z-Order")] public bool alwaysOnTop = true;

    private GameObject currentTip;
    private bool _followCursor;
    private Vector2 _screenOffset;
    private Coroutine fadeCoroutine;

    private Camera UICamera => uiCanvas != null ? uiCanvas.worldCamera : null;

    public void ShowTextTip(string message, Vector3 screenPosition, bool followCursor = true)
    {
        SpawnAndCommonSetup(tipTextPrefab, screenPosition, followCursor);
        var txts = currentTip.GetComponentsInChildren<Text>(true);
        foreach (var txt in txts)
        {
            txt.text = message;
        }
    }

    public void ShowIconTip(Sprite icon, Vector3 screenPosition, bool followCursor = true)
    {
        SpawnAndCommonSetup(tipIconPrefab, screenPosition, followCursor);
        var img = currentTip.GetComponentInChildren<Image>(true);
        if (img) img.sprite = icon;
    }

    public void ShowTextAndIconTip(string message, Sprite icon, Vector3 screenPosition, bool followCursor = true)
    {
        SpawnAndCommonSetup(tipTextAndIconPrefab, screenPosition, followCursor);
        var img = currentTip.GetComponentInChildren<Image>(true);
        if (img) img.sprite = icon;
        var txt = currentTip.GetComponentInChildren<Text>(true);
        if (txt) txt.text = message;
    }

    private void SpawnAndCommonSetup(GameObject prefab, Vector3 screenPosition, bool followCursor)
    {
        if (!uiCanvas)
        {
            Debug.LogError("[TipsController] 请在 Inspector 中指定 uiCanvas。");
            return;
        }

        if (currentTip != null)
            Destroy(currentTip);

        currentTip = Instantiate(prefab, uiCanvas.transform);
        if (alwaysOnTop) currentTip.transform.SetAsLastSibling();

        _followCursor = followCursor;

        AutoAdjustOffset(screenPosition);
        UpdateTipPositionByScreen(screenPosition);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn(currentTip));
    }

    private void AutoAdjustOffset(Vector3 screenPos)
    {
        if (currentTip == null) return;

        var rt = currentTip.GetComponent<RectTransform>();
        if (!rt) return;

        Vector2 size = rt.sizeDelta;
        Vector2 offset = new Vector2(size.x * 0.5f + 10f, -size.y * 0.5f - 10f);

        if (screenPos.x + size.x > Screen.width)
            offset.x = -(size.x * 0.5f + 10f);

        if (screenPos.y - size.y < 0)
            offset.y = size.y * 0.5f + 10f;

        _screenOffset = offset;
    }

    private void UpdateTipPositionByScreen(Vector3 screenPosition)
    {
        if (currentTip == null) return;

        screenPosition += (Vector3)_screenOffset;

        var rt = currentTip.GetComponent<RectTransform>();
        if (!rt) return;

        var canvasRect = uiCanvas.transform as RectTransform;
        if (!canvasRect) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            UICamera,
            out localPoint
        );

        rt.localPosition = localPoint;

        if (alwaysOnTop)
            currentTip.transform.SetAsLastSibling();
    }

    private void LateUpdate()
    {
        if (currentTip == null) return;
        if (!_followCursor) return;

        var screenPos = (Vector3)Input.mousePosition;
        UpdateTipPositionByScreen(screenPos);
    }

    public void HideTips()
    {
        if (currentTip != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutAndDestroy(currentTip));
            currentTip = null;
        }
    }

    #region 动画

    private IEnumerator FadeIn(GameObject tip)
    {
        var cg = tip.GetComponent<CanvasGroup>();
        if (!cg) cg = tip.AddComponent<CanvasGroup>();

        var rt = tip.GetComponent<RectTransform>();

        cg.alpha = 0f;
        rt.localScale = Vector3.one * 0.8f;

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float progress = t / 0.2f;

            cg.alpha = Mathf.Lerp(0f, 1f, progress);
            rt.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, progress);

            yield return null;
        }

        cg.alpha = 1f;
        rt.localScale = Vector3.one;
    }

    private IEnumerator FadeOutAndDestroy(GameObject tip)
    {
        var cg = tip.GetComponent<CanvasGroup>();
        if (!cg) cg = tip.AddComponent<CanvasGroup>();

        var rt = tip.GetComponent<RectTransform>();

        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float progress = t / 0.15f;

            cg.alpha = Mathf.Lerp(1f, 0f, progress);
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, progress);

            yield return null;
        }

        Destroy(tip);
    }

    #endregion
}