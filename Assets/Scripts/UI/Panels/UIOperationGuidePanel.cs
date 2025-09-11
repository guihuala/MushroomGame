using UnityEngine;
using UnityEngine.UI;

public class UIOperationGuidePanel : BasePanel
{
    [Header("操作指南组件")]
    public Text titleText;
    public Text contentText;
    public Text pageText;
    public Button closeButton;
    public Button nextButton;
    public Button prevButton;

    [Header("指南内容")]
    public string[] pageTitles;
    [TextArea(3, 10)]
    public string[] pageContents;

    private int currentPage = 0;

    protected override void Awake()
    {
        base.Awake();
        
        // 初始化按钮事件
        closeButton.onClick.AddListener(OnCloseClick);
        nextButton.onClick.AddListener(OnNextClick);
        prevButton.onClick.AddListener(OnPrevClick);
    }

    public override void OpenPanel(string name)
    {
        base.OpenPanel(name);
        
        // 初始化内容
        currentPage = 0;
        UpdateContent();
        
        // 设置初始交互状态
        SetInteractable(true);
    }

    private void Update()
    {
        // 快捷键支持
        if (canvasGroup.alpha > 0.9f && canvasGroup.interactable)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.H))
            {
                OnCloseClick();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                OnNextClick();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                OnPrevClick();
            }
        }
    }

    private void UpdateContent()
    {
        if (pageTitles.Length == 0 || pageContents.Length == 0) return;

        titleText.text = pageTitles[currentPage];
        contentText.text = pageContents[currentPage];
        pageText.text = $"{currentPage + 1}/{pageTitles.Length}";

        // 更新按钮状态
        prevButton.interactable = currentPage > 0;
        nextButton.interactable = currentPage < pageTitles.Length - 1;
    }

    private void OnNextClick()
    {
        if (currentPage < pageTitles.Length - 1)
        {
            currentPage++;
            UpdateContent();
        }
    }

    private void OnPrevClick()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateContent();
        }
    }

    private void OnCloseClick()
    {
        UIManager.Instance.ClosePanel(panelName);
    }

    // 设置面板内容（可选，用于动态更新）
    public void SetGuideContent(string[] titles, string[] contents)
    {
        if (titles != null && contents != null && titles.Length == contents.Length)
        {
            pageTitles = titles;
            pageContents = contents;
            currentPage = 0;
            UpdateContent();
        }
    }
}