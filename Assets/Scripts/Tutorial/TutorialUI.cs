using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image illustration;
    [SerializeField] private Button okButton;

    private System.Action _onClosed;

    private void Awake()
    {
        if (okButton) okButton.onClick.AddListener(() => Close());
        HideImmediate();
    }

    public void Show(string title, string bodyTMP, Sprite pic = null, bool pauseGame = false, System.Action onClosed = null)
    {
        _onClosed = onClosed;
        if (titleText) titleText.text = title ?? "";
        if (bodyText)
        {
            bodyText.richText = true;     // 允许 <sprite>
            bodyText.enableWordWrapping = true;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.text = bodyTMP ?? "";
        }
        if (illustration)
        {
            illustration.gameObject.SetActive(pic != null);
            illustration.sprite = pic;
        }

        if (pauseGame) Time.timeScale = 0f;
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
        var cb = _onClosed; _onClosed = null;
        cb?.Invoke();
    }

    public void HideImmediate()
    {
        if (panel) panel.SetActive(false);
    }
}