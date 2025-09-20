using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

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
    public float buttonSelectScale = 1.15f;
    
    [Header("建筑详情栏")]
    public GameObject buildingDetailsPanel;
    public Text buildingNameText;
    public Image buildingIconImage;
    public ConstructionCostPanel costPanel;
    
    [Header("建筑描述（TMP 渲染）")]
    [SerializeField] private TMP_Text descriptionTMP;
    [SerializeField] private TMP_SpriteAsset iconSpriteAsset; 

    [Header("工具栏")]
    public ProductionTooltipPanel productionTooltipPanel; 
    public MinerTooltipPanel minerTooltipPanel;

    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory;
    private BuildingCategory currentCategory = BuildingCategory.Production;
    private BuildingData currentSelectedBuilding;
    
    private List<Button> categoryTabButtons = new List<Button>();
    private List<Button> buildingButtons = new List<Button>();
    private Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();
    
    private MsgRecAction _onBuildingUnlocked;
    
    void Start()
    {
        CloseBuildingDetails();
    }

    private void OnEnable()
    {
        _onBuildingUnlocked = OnBuildingUnlocked;
        MsgCenter.RegisterMsg(MsgConst.BUILDING_UNLOCKED, _onBuildingUnlocked);
    }

    private void OnDisable()
    {
        if (_onBuildingUnlocked != null)
            MsgCenter.UnregisterMsg(MsgConst.BUILDING_UNLOCKED, _onBuildingUnlocked);
    }

    void Update()
    {
        UpdateSelectionVisuals();
    }
    
    public void InitializeUI()
    {
        if (placementSystem == null) return;

        buildingsByCategory = new Dictionary<BuildingCategory, List<BuildingData>>();

        foreach (var category in buildingList.GetAllCategories())
        {
            var unlocked = TechTreeManager.Instance != null
                ? TechTreeManager.Instance.GetUnlockedBuildingsByCategory(category)
                : new List<BuildingData>();
            
            var displayable = unlocked
                .Where(b => b != null && b.prefab != null)
                .ToList();

            buildingsByCategory[category] = displayable;
        }

        CreateCategoryTabs();
        CreateBuildingButtons();

        var nonEmpty = buildingsByCategory
            .Where(kv => kv.Value != null && kv.Value.Count > 0)
            .Select(kv => kv.Key).ToList();

        if (nonEmpty.Count > 0)
            SelectCategory(nonEmpty.Contains(currentCategory) ? currentCategory : nonEmpty[0]);
        else
            SelectCategory(BuildingCategory.Production);
    }

    private void CreateCategoryTabs()
    {
        if (categoryTabsContainer == null || categoryTabPrefab == null) return;
    
        foreach (Transform child in categoryTabsContainer) Destroy(child.gameObject);
        categoryTabButtons.Clear();
    
        foreach (var category in buildingsByCategory.Keys)
        {
            if (buildingsByCategory[category].Count == 0) continue;
        
            var tabGO = Instantiate(categoryTabPrefab, categoryTabsContainer);
            var tabButton = tabGO.GetComponent<Button>();
            var tabImage = tabGO.transform.GetChild(0).GetComponent<Image>();
            var tabIcon = GetCategoryIcon(category);
        
            if (tabImage != null && tabIcon != null)
                tabImage.sprite = tabIcon;
            
            BuildingCategory cat = category;
            tabButton.onClick.AddListener(() => SelectCategory(cat));
            
            tabButton.GetComponent<Image>().DOColor(normalTabColor, 0.5f).SetUpdate(true);
            categoryTabButtons.Add(tabButton);
        }
    }

    private void CreateBuildingButtons()
    {
        if (buildingButtonsContainer == null || buildingButtonPrefab == null) return;
        
        var containerCG = buildingButtonsContainer.GetComponent<CanvasGroup>();
        if (containerCG == null) containerCG = buildingButtonsContainer.gameObject.AddComponent<CanvasGroup>();
        containerCG.alpha = 0f;

        foreach (Transform child in buildingButtonsContainer) Destroy(child.gameObject);
        buildingButtons.Clear();
        originalButtonScales.Clear();
        
        foreach (var buildingData in buildingsByCategory.Values.SelectMany(x => x))
        {
            var buttonGO = Instantiate(buildingButtonPrefab, buildingButtonsContainer);
            var button = buttonGO.GetComponent<Button>();
            var iconImage = buttonGO.transform.GetChild(0).GetComponent<Image>();

            if (iconImage != null && buildingData.icon != null)
                iconImage.sprite = buildingData.icon;
            
            var childCG = buttonGO.GetComponent<CanvasGroup>();
            if (childCG != null) childCG.alpha = 1f;

            originalButtonScales[button] = button.transform.localScale;
            AddClickEffect(button, buttonScaleDuration);

            BuildingData data = buildingData;
            button.onClick.AddListener(() => OnBuildingSelected(data));
            buildingButtons.Add(button);
        }
        
        containerCG.DOFade(1f, 0.1f).SetUpdate(true);
    }

    private void AddClickEffect(Button button, float duration)
    {
        button.onClick.AddListener(() =>
        {
            button.transform.DOScale(originalButtonScales[button] * buttonSelectScale, duration / 2)
                .OnComplete(() => { button.transform.DOScale(originalButtonScales[button], duration / 2); });
        });
    }
    
    private void SelectCategory(BuildingCategory category)
    {
        currentCategory = category;
        UpdateCategoryTabsVisualWithAnimation();
        ShowBuildingsInCategory(category);
    }
    
    private void ShowBuildingsInCategory(BuildingCategory category)
    {
        for (int i = 0; i < buildingButtons.Count; i++)
        {
            var buildingData = buildingsByCategory.Values.SelectMany(x => x).ElementAt(i);
            var button = buildingButtons[i];
            var shouldShow = buildingData.category == category;
            button.gameObject.SetActive(shouldShow);
            if (shouldShow) button.transform.localScale = originalButtonScales[button];
        }
    }
    
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
    
    private void UpdateCategoryTabsVisualWithAnimation()
    {
        for (int i = 0; i < categoryTabButtons.Count; i++)
        {
            var tabButton = categoryTabButtons[i];
            if (tabButton != null)
            {
                bool isSelected = i == (int)currentCategory;
                Color targetColor = isSelected ? selectedTabColor : normalTabColor;
                
                tabButton.GetComponent<Image>().DOColor(targetColor, tabTransitionDuration);
                
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
    
    private void ShowBuildingDetailsWithAnimation(BuildingData buildingData)
    {
        buildingDetailsPanel.SetActive(true);
        
        var detailsCanvasGroup = buildingDetailsPanel.GetComponent<CanvasGroup>();
        if (detailsCanvasGroup == null) detailsCanvasGroup = buildingDetailsPanel.AddComponent<CanvasGroup>();
        detailsCanvasGroup.alpha = 0;
        
        buildingNameText.text = buildingData.buildingName;
        buildingIconImage.sprite = buildingData.icon;

        // ★ 用 TMP 渲染简介（自动换行 + 图标混排）
        if (descriptionTMP != null)
        {
            descriptionTMP.spriteAsset = iconSpriteAsset;                 // 绑定图标集
            descriptionTMP.enableWordWrapping = true;                     // 自动换行
            descriptionTMP.overflowMode = TextOverflowModes.Overflow;     // 高度可扩展
            descriptionTMP.alignment = TextAlignmentOptions.TopLeft;      // 左上对齐
            descriptionTMP.text = IconMarkupTMP.ToTMP(buildingData.description);
        }
        
        // 图标放大动画
        buildingIconImage.transform.localScale = Vector3.zero;
        buildingIconImage.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    
        if (productionTooltipPanel != null && productionTooltipPanel.gameObject.activeSelf)
            productionTooltipPanel.ClosePanel();
    
        if (costPanel != null)
            costPanel.SetData(buildingData);
        
        detailsCanvasGroup.DOFade(1f, detailsFadeDuration);
    }
    
    private void CloseBuildingDetailsWithAnimation()
    {
        if (buildingDetailsPanel != null && buildingDetailsPanel.activeSelf)
        {
            var detailsCanvasGroup = buildingDetailsPanel.GetComponent<CanvasGroup>();
            if (detailsCanvasGroup == null) detailsCanvasGroup = buildingDetailsPanel.AddComponent<CanvasGroup>();
            detailsCanvasGroup.DOFade(0f, detailsFadeDuration / 2)
                .OnComplete(() => buildingDetailsPanel.SetActive(false));
        }
    }
        
    private void CloseBuildingDetails()
    {
        if (buildingDetailsPanel != null) buildingDetailsPanel.SetActive(false);
    }
    
    #endregion

    #region Tootips
    
    public void CloseAllTooltips(bool immediate = false)
    {
        if (immediate)
        {
            if (buildingDetailsPanel) buildingDetailsPanel.SetActive(false);
            if (productionTooltipPanel) productionTooltipPanel.ClosePanel();
            if (minerTooltipPanel) minerTooltipPanel.ClosePanel();
            return;
        }
        
        CloseBuildingDetailsWithAnimation();
        if (productionTooltipPanel && productionTooltipPanel.gameObject.activeSelf)
            productionTooltipPanel.ClosePanel();
        if (minerTooltipPanel && minerTooltipPanel.gameObject.activeSelf)
            minerTooltipPanel.ClosePanel();
    }
    
    public void ShowProductionTooltip(IProductionInfoProvider provider)
    {
        CloseAllTooltips(immediate: true);
        if (productionTooltipPanel != null)
        {
            productionTooltipPanel.SetContext(provider);
            productionTooltipPanel.ShowAtScreenPosition(Input.mousePosition);
        }
    }
    
    public void ShowMinerTooltip(Miner miner)
    {
        CloseAllTooltips(immediate: true);
        if (minerTooltipPanel != null)
        {
            minerTooltipPanel.SetMiner(miner);
            minerTooltipPanel.ShowAtScreenPosition(Input.mousePosition);
        }
    }
    
    #endregion
    
    private void OnBuildingUnlocked(params object[] args)
    {
        InitializeUI();
    }
}