using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinerTooltipPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Text title;
    [SerializeField] private Text miningRateText;
    [SerializeField] private Transform resourcesContainer;
    [SerializeField] private GameObject resourceRowPrefab;
    
    [Header("Status & Progress")]
    [SerializeField] private Image miningDot;            // 正在采集的小圆点（绿/灰）
    [SerializeField] private Image  progressFillImage;   // 可选：用 fillAmount 表示进度

    [Header("Current Resource")]
    [SerializeField] private Image currentResIcon;       // 当前目标资源图标

    private Miner _miner;

    private void Awake()
    {
        gameObject.SetActive(false);
    }
    
    // 初始化面板
    public void SetMiner(Miner miner)
    {
        _miner = miner;
        RefreshPanel();
    }

    public void RefreshPanel()
    {
        if (_miner == null) return;

        if (title != null)
            title.text = _miner.name.Replace("(Clone)", "").Trim();

        // 采集倍率（沿用你原有文字）
        if (miningRateText != null)
        {
            float multiplier = PowerManager.Instance.GetSpeedMultiplier(_miner.cell, _miner.grid);
            miningRateText.text = $"Mining Rate: x{multiplier:F2}";
        }

        // —— 是否正在采集 + 进度 —— 
        bool mining = _miner.IsMining;
        float prog = _miner.Progress01;
        
        if (miningDot)
            miningDot.color = mining ? new Color(0.35f, 1f, 0.4f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
        
        if (progressFillImage) { progressFillImage.gameObject.SetActive(true); progressFillImage.fillAmount = Mathf.Clamp01(prog); }
        
        // —— 当前目标资源（若有绑定节点） —— 
        var res = _miner.CurrentResource;
        if (currentResIcon && res != null)
        {
            currentResIcon.gameObject.SetActive(true);
            currentResIcon.sprite = res.icon;
        }
        else currentResIcon.gameObject.SetActive(false);

        // —— 可采资源列表：无项时隐藏容器 —— 
        if (resourcesContainer != null)
        {
            // 清空旧项
            foreach (Transform child in resourcesContainer)
                Destroy(child.gameObject);

            bool hasList = _miner.allowedResourceTypes != null && _miner.allowedResourceTypes.Count > 0;
            resourcesContainer.gameObject.SetActive(hasList);

            if (hasList)
            {
                foreach (var resource in _miner.allowedResourceTypes)
                {
                    if (resource == null) continue;
                    var row = Instantiate(resourceRowPrefab, resourcesContainer);
                    var rowImage = row.GetComponentInChildren<Image>();
                    if (rowImage) rowImage.sprite = resource.icon;
                }
            }
        }
    }

    // 显示面板在指定位置
    public void ShowAtScreenPosition(Vector2 position)
    {
        gameObject.SetActive(true);
    }

    // 关闭面板
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}