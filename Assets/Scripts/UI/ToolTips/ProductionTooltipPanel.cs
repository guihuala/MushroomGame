using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProductionTooltipPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Text title;
    [SerializeField] private Image progressFill;
    [SerializeField] private Transform inputsRoot;           // 输入图标的容器
    [SerializeField] private Transform outputsRoot;          // 输出图标的容器
    [SerializeField] private GameObject iconEntryPrefab;     // 上文的图标条目预制体
    [SerializeField] private Text extraText;      // 额外说明（功率/速率等）

    [Header("Icon Source")]
    [Tooltip("当 IOEntry.item 为空时，将从 Resources/{resourceIconFolder}/{resourceKey} 加载图标")]
    [SerializeField] private string resourceIconFolder = "Icons";

    [Header("Placement")]
    [SerializeField] private bool clampToCanvas = true;

    private IProductionInfoProvider _provider;

    private void Awake()
    {
        gameObject.SetActive(false);
    }
    
    public void SetContext(object context)
    {
        _provider = context as IProductionInfoProvider;
        if (_provider != null) RefreshNow(_provider.GetProductionInfo());
    }

    public void ShowAtScreenPosition(Vector2 screenPos)
    {
        if (_provider != null) RefreshNow(_provider.GetProductionInfo());
        gameObject.SetActive(true);
    }
    
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private void RefreshNow(ProductionInfo info)
    {
        if (title) title.text = info.displayName;

        if (progressFill)
            progressFill.fillAmount = Mathf.Clamp01(info.progress01);

        BuildIconRow(inputsRoot, info.inputs, isInput: true);
        BuildIconRow(outputsRoot, info.outputs, isInput: false);

        if (extraText) extraText.text = info.extraText ?? string.Empty;
    }

    private void BuildIconRow(Transform root, List<IOEntry> list, bool isInput)
    {
        if (root == null) return;

        // 清空旧子物体
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        if (list == null) return;

        foreach (var e in list)
        {
            var go = Instantiate(iconEntryPrefab, root);
            var icon = go.transform.GetChild(0).GetComponent<Image>();
            var texts = go.GetComponentsInChildren<Text>(true);

            Text bigText = null, smallBadge = null;
            foreach (var t in texts)
            {
                if (t.gameObject.name.ToLower().Contains("badge")) smallBadge = t;
                else bigText = t;
            }

            // 图标
            Sprite s = e.item != null ? e.item.icon : LoadResourceIcon(e.resourceKey);
            if (icon) icon.sprite = s;

            // 数量（大字）：显示缓存 have/上限
            if (bigText)
            {
                if (e.cap >= 0) bigText.text = $"{e.have}/{e.cap}";
                else bigText.text = $"{e.have}/∞";
            }

            // 小角标：配方每次需要/产出
            if (smallBadge)
            {
                smallBadge.text = (e.want > 0) ? $"x{e.want}" : "";
                smallBadge.gameObject.SetActive(e.want > 0);
            }
        }
    }

    private Sprite LoadResourceIcon(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return Resources.Load<Sprite>($"{resourceIconFolder}/{key}");
    }
}
