using UnityEngine;
using UnityEngine.UI;

public class GameClearPanel : BasePanel
{
    [Header("reference")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text detailsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    protected override void Awake()
    {
        base.Awake();
        if (restartButton)  restartButton.onClick.AddListener(OnClickRestart);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(OnClickMainMenu);
    }

    public override void OpenPanel(string name)
    {
        base.OpenPanel(name);

        if (titleText)    titleText.text = "You have finished the game!";
        if (subtitleText) subtitleText.text = BuildSubtitle();
        if (detailsText)  detailsText.text = BuildDetails();
    }

    private string BuildSubtitle()
    {
        // 统计用时
        var seconds = Mathf.Max(0f, Time.timeSinceLevelLoad);
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"Using Time {m:D2}:{s:D2}";
    }

    private string BuildDetails()
    {
        var hub = FindObjectOfType<Hub>();
        if (hub == null || hub.stages == null || hub.stages.Count == 0) return " ";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Stage Progress");
        for (int i = 0; i < hub.stages.Count; i++)
        {
            var stage = hub.stages[i];
            string stageName = string.IsNullOrEmpty(stage.stageName) ? $"Stage {i + 1}" : stage.stageName;
            sb.AppendLine($"- {stageName}");

            if (stage.requirements != null && stage.requirements.Count > 0)
            {
                foreach (var req in stage.requirements)
                {
                    int have = InventoryManager.Instance.GetItemCount(req.item);
                    sb.AppendLine($"    • {req.item?.itemId ?? "item"}：{have}/{req.requiredAmount}");
                }
            }
        }
        return sb.ToString();
    }

    private void OnClickRestart()
    {
        ClosePanel();
        GameManager.Instance.RestartGame();
        SceneLoader.Instance.ReloadCurrentScene();
    }

    private void OnClickMainMenu()
    {
        ClosePanel();
        GameManager.Instance.ReturnToMainMenu();
    }
}