using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ProductionTooltipPanel : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Text title;
    
    [SerializeField] private Transform inputsRoot;
    [SerializeField] private Transform outputsRoot;
    
    [SerializeField] private GameObject inputsSection;
    [SerializeField] private GameObject outputsSection;

    [SerializeField] private GameObject iconEntryPrefab;

    [Header("Producing & Progress")]
    [SerializeField] private Image producingDot;
    [SerializeField] private Image  progressFillImage;

    [Header("Extra Text/Icons")]
    [SerializeField] private Text extraText;
    [SerializeField] private Transform extraInlineRoot;
    [SerializeField] private GameObject extraIconPrefab;

    [Header("Icon Source")]
    [SerializeField] private string resourceIconFolder = "Icons";

    [Header("Placement")]
    [SerializeField] private bool clampToCanvas = true;

    private IProductionInfoProvider _provider;

    private static readonly Regex kIconTag = new Regex(@"\{icon:([^\}]+)\}", RegexOptions.Compiled);

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

    public void ClosePanel() => gameObject.SetActive(false);
    
    private void RefreshNow(ProductionInfo info)
    {
        if (title) title.text = (info.displayName ?? "").Replace("(Clone)", "").Trim();

        // --- 1) IO 区域：无项时整块隐藏 ---
        BuildIconRow(inputsRoot, info.inputs, true);
        BuildIconRow(outputsRoot, info.outputs, false);
        if (inputsSection == null && inputsRoot != null) inputsSection = inputsRoot.gameObject;
        if (outputsSection == null && outputsRoot != null) outputsSection = outputsRoot.gameObject;
        if (inputsSection)  inputsSection.SetActive(info.inputs != null && info.inputs.Count > 0);
        if (outputsSection) outputsSection.SetActive(info.outputs != null && info.outputs.Count > 0);
        
        // 生产状态小点
        if (producingDot)
        {
            var c = producingDot.color;
            producingDot.color = info.isProducing ? new Color(0.35f, 1f, 0.4f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
        } 
        
        if (progressFillImage) { progressFillImage.gameObject.SetActive(true); progressFillImage.fillAmount = Mathf.Clamp01(info.progress01); }
        
        if (extraInlineRoot != null && extraIconPrefab != null)
        {
            BuildInlineExtra(extraInlineRoot, extraIconPrefab, info.extraText);
            if (extraText) extraText.gameObject.SetActive(false);
        }
        else
        {
            if (extraText)
            {
                extraText.text = info.extraText ?? "";
                extraText.gameObject.SetActive(!string.IsNullOrEmpty(extraText.text));
            }
        }
    }

    private void BuildIconRow(Transform root, List<IOEntry> list, bool isInput)
    {
        if (!root) return;

        // 清空旧子物体
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        if (list == null || list.Count == 0) return;

        foreach (var e in list)
        {
            var go = Instantiate(iconEntryPrefab, root);
            var icon = go.transform.GetChild(0).GetComponent<Image>();
            var text = go.GetComponentInChildren<Text>(true);

            Text smallBadge = text;

            // 图标
            Sprite s = e.item != null ? e.item.icon : LoadResourceIcon(e.resourceKey);
            if (icon) icon.sprite = s;

            // 配方每次需要/产出
            if (smallBadge)
            {
                smallBadge.text = (e.want > 0) ? $"x{e.want}" : "";
                smallBadge.gameObject.SetActive(e.want > 0);
            }
        }
    }

    private void BuildInlineExtra(Transform root, GameObject iconPrefab, string raw)
    {
        // 清空
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        if (string.IsNullOrEmpty(raw)) { root.gameObject.SetActive(false); return; }

        root.gameObject.SetActive(true);

        int last = 0;
        foreach (Match m in kIconTag.Matches(raw))
        {
            // 前面的文字片段
            if (m.Index > last)
            {
                string textFrag = raw.Substring(last, m.Index - last);
                AddTextFrag(root, textFrag);
            }

            // 图标片段
            string key = m.Groups[1].Value.Trim();
            var iconGO = Instantiate(iconPrefab, root);
            var img = iconGO.GetComponentInChildren<Image>();
            if (img) img.sprite = LoadResourceIcon(key);

            last = m.Index + m.Length;
        }

        // 收尾文字片段
        if (last < raw.Length)
            AddTextFrag(root, raw.Substring(last));
    }

    private void AddTextFrag(Transform parent, string txt)
    {
        if (string.IsNullOrEmpty(txt)) return;
        var go = new GameObject("txt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // 或者在 Inspector 指定通用字体
        t.color = Color.white;
        t.raycastTarget = false;
    }

    private Sprite LoadResourceIcon(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return Resources.Load<Sprite>($"{resourceIconFolder}/{key}");
    }
}
