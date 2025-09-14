using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class BuildingSelectionUI : Singleton<BuildingSelectionUI>
{
    [Header("引用")]
    public PlacementSystem placementSystem;
    public BuildingList buildingList;
    
    [Header("UI预制体")]
    public GameObject categoryTabPrefab;
    public GameObject buildingButtonPrefab;
    
    [Header("UI容器")]
    public Transform categoryTabsContainer;
    public Transform buildingButtonsContainer;
    
    [Header("颜色设置")]
    public Color normalTabColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    public Color selectedTabColor = Color.white;
    public Color normalButtonColor = new Color(1, 1, 1, 0.35f);
    public Color selectedButtonColor = Color.white;
    
    [Header("动效设置")]
    public float tabTransitionDuration = 0.3f;
    public float buttonScaleDuration = 0.2f;
    public float detailsFadeDuration = 0.3f;
    public float buttonHoverScale = 1.1f;
    public float buttonSelectScale = 1.15f;
    
    [Header("建筑详情栏")]
    public GameObject buildingDetailsPanel;
    public Text buildingNameText;
    public Text buildingDescriptionText;
    public Image buildingIconImage;
    public ConstructionCostPanel costPanel;
    
    [Header("工具栏")]
    public ProductionTooltipPanel productionTooltipPanel; 
    public MinerTooltipPanel minerTooltipPanel;
    
    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory;
    private BuildingCategory currentCategory = BuildingCategory.Production;
    private BuildingData currentSelectedBuilding;
    
    private List<Button> categoryTabButtons = new List<Button>();
    private List<Button> buildingButtons = new List<Button>();
    private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();
    
    void Start()
    {
        CloseBuildingDetails();
        InitializeUI();
    }
    
    void Update()
    {
        UpdateSelectionVisuals();
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        if (buildingList == null || placementSystem == null) return;
        
        // 按分类组织建筑
        buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingData>>();
        foreach (var category in buildingList.GetAllCategories())
        {
            buildingsByCategory[category] = buildingList.GetBuildingsByCategory(category);
        }
        
        CreateCategoryTabs();
        CreateBuildingButtons();
        SelectCategory(BuildingCategory.Production);
    }
    
    private void CreateCategoryTabs()
    {
        if (categoryTabsContainer == null || categoryTabPrefab == null) return;
    
        // 清空现有页签
        foreach (Transform child in categoryTabsContainer)
        {
            Destroy(child.gameObject);
        }
        categoryTabButtons.Clear();
    
        // 创建新页签
        foreach (var category in buildingsByCategory.Keys)
        {
            if (buildingsByCategory[category].Count == 0) continue;
        
            var tabGO = Instantiate(categoryTabPrefab, categoryTabsContainer);
            var tabButton = tabGO.GetComponent<Button>();
            var tabImage = tabGO.transform.GetChild(0).GetComponent<Image>();
            var tabIcon = GetCategoryIcon(category);
        
            if (tabImage != null && tabIcon != null)
            {
                tabImage.sprite = tabIcon;
            }
            
            BuildingCategory cat = category;
            tabButton.onClick.AddListener(() => SelectCategory(cat));
            
            tabButton.GetComponent<Image>().DOColor(normalTabColor, 0.5f).SetUpdate(true);
            categoryTabButtons.Add(tabButton);
        }
    }

    /// <summary>
    /// 创建建筑按钮
    /// </summary>
    private void CreateBuildingButtons()
    {
        if (buildingButtonsContainer == null || buildingButtonPrefab == null) return;

        foreach (Transform child in buildingButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        buildingButtons.Clear();
        originalButtonScales.Clear();
        
        foreach (var buildingData in buildingsByCategory.Values.SelectMany(x => x))
        {
            var buttonGO = Instantiate(buildingButtonPrefab, buildingButtonsContainer);
            var button = buttonGO.GetComponent<Button>();
            var iconImage = buttonGO.transform.GetChild(0).GetComponent<Image>();
            
            if (iconImage != null && buildingData.icon != null)
            {
                iconImage.sprite = buildingData.icon;
            }
            
            // 保存原始尺寸
            originalButtonScales[button] = button.transform.localScale;
            
            AddClickEffect(button, buttonScaleDuration);
            
            BuildingData data = buildingData;
            button.onClick.AddListener(() => OnBuildingSelected(data));
            
            buildingButtons.Add(button);
            
            // 初始隐藏并添加渐显效果
            buttonGO.GetComponent<CanvasGroup>().alpha = 0;
            buttonGO.GetComponent<CanvasGroup>().DOFade(1f, 0.3f).SetUpdate(true);
        }
    }
    
    /// <summary>
    /// 为按钮添加点击效果
    /// </summary>
    private void AddClickEffect(Button button, float duration)
    {
        button.onClick.AddListener(() => {
            // 点击时放大效果
            button.transform.DOScale(originalButtonScales[button] * buttonSelectScale, duration / 2)
                .OnComplete(() => {
                    button.transform.DOScale(originalButtonScales[button], duration / 2);
                });
        });
    }
    
    /// <summary>
    /// 选择分类
    /// </summary>
    private void SelectCategory(BuildingCategory category)
    {
        currentCategory = category;
        
        // 添加页签切换动画
        UpdateCategoryTabsVisualWithAnimation();

        ShowBuildingsInCategoryWithAnimation(category);
    }
    
    /// <summary>
    /// 显示指定分类的建筑（带动画）
    /// </summary>
    private void ShowBuildingsInCategoryWithAnimation(BuildingCategory category)
    {
        Sequence sequence = DOTween.Sequence();
        
        for (int i = 0; i < buildingButtons.Count; i++)
        {
            var buildingData = buildingsByCategory.Values.SelectMany(x => x).ElementAt(i);
            var button = buildingButtons[i];
            var shouldShow = buildingData.category == category;
            
            if (button.gameObject.activeSelf != shouldShow)
            {
                if (shouldShow)
                {
                    button.gameObject.SetActive(true);
                    button.transform.localScale = Vector3.zero;
                    sequence.Insert(i * 0.05f,
                        button.transform.DOScale(originalButtonScales[button], 0.02f).SetEase(Ease.OutBack));
                }
                else
                {
                    button.gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// 建筑选择事件
    /// </summary>
    private void OnBuildingSelected(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;
        
        if (placementSystem != null && buildingData.prefab != null)
        {
            placementSystem.SetCurrentBuildingData(buildingData);

            if (!placementSystem.IsInBuildMode)
            {
                placementSystem.EnterBuildMode();
                placementSystem.SetCurrentBuildingData(buildingData);
            }
        }
        
        AudioManager.Instance.PlaySfx("LightClick");
        
        ShowBuildingDetailsWithAnimation(buildingData);
    }
    
    /// <summary>
    /// 更新选择视觉效果
    /// </summary>
    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < buildingButtons.Count; i++)
        {
            var buildingData = buildingsByCategory.Values.SelectMany(x => x).ElementAt(i);
            var button = buildingButtons[i];
            
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = (currentSelectedBuilding == buildingData) ? selectedButtonColor : normalButtonColor;
                button.colors = colors;
            }
        }
    }
    
    /// <summary>
    /// 更新页签视觉效果（带动画）
    /// </summary>
    private void UpdateCategoryTabsVisualWithAnimation()
    {
        for (int i = 0; i < categoryTabButtons.Count; i++)
        {
            var tabButton = categoryTabButtons[i];
            if (tabButton != null)
            {
                bool isSelected = i == (int)currentCategory;
                Color targetColor = isSelected ? selectedTabColor : normalTabColor;
                
                // 使用动画过渡颜色
                tabButton.GetComponent<Image>().DOColor(targetColor, tabTransitionDuration);
                
                // 添加缩放效果
                if (isSelected)
                {
                    tabButton.transform.DOScale(1.1f, tabTransitionDuration / 2)
                        .OnComplete(() => tabButton.transform.DOScale(1f, tabTransitionDuration / 2));
                }
            }
        }
    }
 
    private Sprite GetCategoryIcon(BuildingCategory category)
    {
        switch (category)
        {
            case BuildingCategory.Production:
                return Resources.Load<Sprite>("Icons/ProductionIcon");
            case BuildingCategory.Logistics:
                return Resources.Load<Sprite>("Icons/LogisticsIcon");
            case BuildingCategory.Mushroom:
                return Resources.Load<Sprite>("Icons/MushroomIcon");
            default:
                return null;
        }
    }

    #region BuildingDetail

    /// <summary>
    /// 显示建筑详情（带动画）
    /// </summary>
    private void ShowBuildingDetailsWithAnimation(BuildingData buildingData)
    {
        buildingDetailsPanel.SetActive(true);
        
        // 重置详情面板的透明度
        CanvasGroup detailsCanvasGroup = buildingDetailsPanel.GetComponent<CanvasGroup>();
        if (detailsCanvasGroup == null)
            detailsCanvasGroup = buildingDetailsPanel.AddComponent<CanvasGroup>();
            
        detailsCanvasGroup.alpha = 0;
        
        buildingNameText.text = buildingData.buildingName;
        buildingDescriptionText.text = buildingData.description;
        buildingIconImage.sprite = buildingData.icon;
        
        // 图标放大动画
        buildingIconImage.transform.localScale = Vector3.zero;
        buildingIconImage.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    
        // 显示建筑信息时关闭生产信息面板
        if (productionTooltipPanel != null && productionTooltipPanel.gameObject.activeSelf)
        {
            productionTooltipPanel.ClosePanel();
        }
    
        // 显示建筑的材料信息面板
        if (costPanel != null)
            costPanel.SetData(buildingData);

        // 使用 DOTween 添加建筑详情面板的淡入效果
        detailsCanvasGroup.DOFade(1f, detailsFadeDuration);
    }
    
    /// <summary>
    /// 关闭建筑详情（带动画）
    /// </summary>
    private void CloseBuildingDetailsWithAnimation()
    {
        if (buildingDetailsPanel != null && buildingDetailsPanel.activeSelf)
        {
            CanvasGroup detailsCanvasGroup = buildingDetailsPanel.GetComponent<CanvasGroup>();
            if (detailsCanvasGroup == null)
                detailsCanvasGroup = buildingDetailsPanel.AddComponent<CanvasGroup>();
                
            detailsCanvasGroup.DOFade(0f, detailsFadeDuration / 2)
                .OnComplete(() => buildingDetailsPanel.SetActive(false));
        }
    }
        
    private void CloseBuildingDetails()
    {
        if (buildingDetailsPanel != null)
        {
            buildingDetailsPanel.SetActive(false);
        }
    }

    #endregion

    #region Tootips

    public void CloseAllTooltips()
    {
        CloseBuildingDetailsWithAnimation();
        
        if (productionTooltipPanel != null && productionTooltipPanel.gameObject.activeSelf)
            productionTooltipPanel.ClosePanel();
        
        if (minerTooltipPanel != null && minerTooltipPanel.gameObject.activeSelf)
            minerTooltipPanel.ClosePanel();
    }

    // 显示生产信息面板
    public void ShowProductionTooltip(IProductionInfoProvider provider)
    {
        // 关闭所有面板
        CloseAllTooltips();

        if (productionTooltipPanel != null)
        {
            productionTooltipPanel.SetContext(provider);
            productionTooltipPanel.ShowAtScreenPosition(Input.mousePosition);
        }
    }

    // 显示矿机信息面板
    public void ShowMinerTooltip(Miner miner)
    {
        CloseAllTooltips();

        if (minerTooltipPanel != null)
        {
            minerTooltipPanel.SetMiner(miner);
            minerTooltipPanel.ShowAtScreenPosition(Input.mousePosition);
        }
    }

    #endregion
}