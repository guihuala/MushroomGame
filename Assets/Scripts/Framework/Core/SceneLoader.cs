using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
#if TMP_PRESENT
using TMPro;
#endif

public enum GameScene
{
    MainMenu = 0,   // 主菜单
    Gameplay = 1,   // 游戏场景
}

public class SceneLoader : SingletonPersistent<SceneLoader>
{
    [Header("加载界面设置")]
    [SerializeField] private CanvasGroup loadingCanvas;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minLoadingTime = 1.5f;

    [Header("打字机文案")]
    [TextArea(2, 4)]
    [SerializeField] private string[] messages = new string[] { };

    [Header("打字机参数")]
    [SerializeField, Tooltip("字符/秒")] private float charsPerSecond = 28f;
    [SerializeField] private bool loopMessages = true;
    [SerializeField] private bool showCursor = true;
    [SerializeField] private string cursorChar = "▏";
    [SerializeField] private float cursorBlinkInterval = 0.4f;

    [Header("文案目标")]
    [SerializeField] private Text uiText;

    [Header("完成时的提示")]
    [SerializeField] private string completeText = "加载完成";

    private AsyncOperation loadingOperation;
    private bool isLoading = false;

    // 打字机内部
    private Coroutine _typeRoutine;
    private bool _typingActive = false;     // 外部控制协程结束
    private bool _blinkOn = false;          // 光标闪烁态

    private void Start()
    {
        // 初始化加载界面
        if (loadingCanvas != null)
        {
            loadingCanvas.alpha = 0f;
            loadingCanvas.gameObject.SetActive(false);
        }
        SetTextImmediate(string.Empty);
    }

    /// <summary>
    /// 加载指定枚举场景
    /// </summary>
    public void LoadScene(GameScene scene, Action onComplete = null)
    {
        if (isLoading) return;

        string sceneName = scene.ToString();
        StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene(Action onComplete = null)
    {
        if (isLoading) return;

        string currentScene = SceneManager.GetActiveScene().name;
        StartCoroutine(LoadSceneRoutine(currentScene, onComplete));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
    {
        isLoading = true;
        float startTime = Time.time;

        // 淡入加载界面
        yield return StartCoroutine(FadeLoadingScreen(0f, 1f));

        // 启动打字机
        StartTypewriter();

        // 开始异步加载场景
        loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        loadingOperation.allowSceneActivation = false;
        
        while (!loadingOperation.isDone)
        {
            if (loadingOperation.progress >= 0.9f &&
                Time.time - startTime >= minLoadingTime)
            {
                // 停止打字机，显示“加载完成”
                StopTypewriter(showComplete: true);

                // 给玩家 0.2~0.3 秒读到“加载完成”字样（可选）
                yield return new WaitForSeconds(0.25f);

                loadingOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 等待一帧确保场景完全加载
        yield return null;

        // 淡出加载界面
        yield return StartCoroutine(FadeLoadingScreen(1f, 0f));
        SetTextImmediate(string.Empty); // 清空文案

        isLoading = false;
        onComplete?.Invoke();
    }

    private IEnumerator FadeLoadingScreen(float startAlpha, float targetAlpha)
    {
        if (loadingCanvas == null) yield break;

        loadingCanvas.gameObject.SetActive(true);
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            loadingCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        loadingCanvas.alpha = targetAlpha;
        
        if (Mathf.Approximately(targetAlpha, 0f))
        {
            loadingCanvas.gameObject.SetActive(false);
        }
    }
    
    private void StartTypewriter()
    {
        StopTypewriter(false);
        _typingActive = true;
        _typeRoutine = StartCoroutine(TypewriterLoop());
    }
    
    private void StopTypewriter(bool showComplete)
    {
        _typingActive = false;
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        if (showComplete)
        {
            SetTextImmediate(completeText);
        }
    }
    
    private IEnumerator TypewriterLoop()
    {
        if (messages == null || messages.Length == 0)
        {
            messages = new[] { "now loading" };
        }

        int index = 0;
        float secPerChar = (charsPerSecond <= 0.01f) ? 0.034f : 1f / charsPerSecond;

        // 光标闪烁协程
        Coroutine blink = null;
        if (showCursor)
        {
            blink = StartCoroutine(CursorBlink());
        }

        while (_typingActive)
        {
            string msg = messages[index];
            // 逐字输出
            for (int i = 0; i <= msg.Length && _typingActive; i++)
            {
                string head = msg.Substring(0, i);
                if (showCursor)
                {
                    SetTextImmediate(head + (_blinkOn ? cursorChar : " "));
                }
                else
                {
                    SetTextImmediate(head);
                }
                yield return new WaitForSeconds(secPerChar);
            }

            // 句子末尾稍作停顿
            float endPause = Mathf.Clamp(secPerChar * 8f, 0.2f, 0.6f);
            float t = 0f;
            while (t < endPause && _typingActive)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // 下一条/循环
            index++;
            if (index >= messages.Length)
            {
                if (loopMessages)
                    index = 0;
                else
                    break;
            }
        }

        if (blink != null) StopCoroutine(blink);
    }
    
    private IEnumerator CursorBlink()
    {
        while (_typingActive)
        {
            _blinkOn = !_blinkOn;
            yield return new WaitForSeconds(cursorBlinkInterval <= 0f ? 0.4f : cursorBlinkInterval);
        }
    }
    
    private void SetTextImmediate(string text)
    {
        if (uiText != null) { uiText.text = text; return; }
    }
    
    public bool IsLoading() => isLoading;
}
