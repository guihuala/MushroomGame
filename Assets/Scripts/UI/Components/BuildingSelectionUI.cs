using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BuildingSelectionUI : MonoBehaviour
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
    
    [Header("建筑详情栏")]
    public GameObject buildingDetailsPanel;    // 建筑详情面板
    public Text buildingNameText;              // 建筑名称文本
    public Text buildingDescriptionText;       // 建筑描述文本
    public Image buildingIconImage;            // 建筑图标图片
 
    private Dictionary<BuildingCategory, List<BuildingData>> buildingsByCategory;
    private BuildingCategory currentCategory = BuildingCategory.Production;
    private BuildingData currentSelectedBuilding;
    
    private List<Button> categoryTabButtons = new List<Button>();
    private List<Button> buildingButtons = new List<Button>();
    
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
        
        foreach (var buildingData in buildingsByCategory.Values.SelectMany(x => x))
        {
            var buttonGO = Instantiate(buildingButtonPrefab, buildingButtonsContainer);
            var button = buttonGO.GetComponent<Button>();
            var iconImage = buttonGO.transform.GetChild(0).GetComponent<Image>();
            var nameText = buttonGO.GetComponentInChildren<Text>();
            
            if (iconImage != null && buildingData.icon != null)
            {
                iconImage.sprite = buildingData.icon;
            }
            
            if (nameText != null)
            {
                nameText.text = buildingData.buildingName;
            }
            
            BuildingData data = buildingData;
            button.onClick.AddListener(() => OnBuildingSelected(data));
            
            buildingButtons.Add(button);

            buttonGO.SetActive(false);
        }
    }
    
    /// <summary>
    /// 选择分类
    /// </summary>
    private void SelectCategory(BuildingCategory category)
    {
        currentCategory = category;
        
        UpdateCategoryTabsVisual();

        ShowBuildingsInCategory(category);
    }
    
    /// <summary>
    /// 显示指定分类的建筑
    /// </summary>
    private void ShowBuildingsInCategory(BuildingCategory category)
    {
        for (int i = 0; i < buildingButtons.Count; i++)
        {
            var buildingData = buildingsByCategory.Values.SelectMany(x => x).ElementAt(i);
            buildingButtons[i].gameObject.SetActive(buildingData.category == category);
        }
    }
    
    /// <summary>
    /// 建筑选择事件
    /// </summary>
    private void OnBuildingSelected(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;

        // 设置放置系统
        if (placementSystem != null && buildingData.prefab != null)
        {
            placementSystem.SetCurrentBuilding(buildingData.prefab);

            // 如果不在建造模式，进入建造模式
            if (!placementSystem.IsInBuildMode)
            {
                placementSystem.EnterBuildMode();
                placementSystem.SetCurrentBuilding(buildingData.prefab);
            }
        }
        
        ShowBuildingDetails(buildingData);
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
    /// 更新分类页签视觉效果
    /// </summary>
    private void UpdateCategoryTabsVisual()
    {
        for (int i = 0; i < categoryTabButtons.Count; i++)
        {
            var tabButton = categoryTabButtons[i];
            if (tabButton != null)
            {
                var colors = tabButton.colors;
                colors.normalColor = (i == (int)currentCategory) ? selectedTabColor : normalTabColor;
                tabButton.colors = colors;
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

    private void ShowBuildingDetails(BuildingData buildingData)
    {
        buildingDetailsPanel.SetActive(true); // 显示详情栏
        buildingNameText.text = buildingData.buildingName;
        buildingDescriptionText.text = buildingData.description;
        buildingIconImage.sprite = buildingData.icon;
    }
    
    private void CloseBuildingDetails()
    {
        if (buildingDetailsPanel != null)
        {
            buildingDetailsPanel.SetActive(false);
        }
    }
}