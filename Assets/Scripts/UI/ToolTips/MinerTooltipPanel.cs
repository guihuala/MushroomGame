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
        {
            title.text = _miner.name.Replace("(Clone)", "").Trim();
        }

        // 设置开采倍率
        if (miningRateText != null)
        {
            float multiplier = PowerManager.Instance.GetSpeedMultiplier(_miner.cell, _miner.grid);
            miningRateText.text = $"Mining Rate: x{multiplier:F2}";
        }

        // 清空现有资源信息
        foreach (Transform child in resourcesContainer)
        {
            Destroy(child.gameObject);
        }

        // 显示可以开采的资源节点
        if (resourcesContainer != null && _miner.allowedResourceTypes.Count > 0)
        {
            foreach (var resource in _miner.allowedResourceTypes)
            {
                if (resource != null)
                {
                    // 创建新的行
                    var resourceRow = Instantiate(resourceRowPrefab, resourcesContainer);
                    var rowImage = resourceRow.GetComponentInChildren<Image>();
                    
                    if (rowImage != null && resource.icon != null) rowImage.sprite = resource.icon;
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