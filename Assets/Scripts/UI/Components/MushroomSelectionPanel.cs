using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MushroomSelectionPanel : MonoBehaviour
{
    [Header("组件")]
    public GameObject panel;
    public Transform mushroomsContainer;
    public Button mushroomButtonPrefab;
    private List<Building> availableMushrooms = new List<Building>();

    private TileGridService grid;
    private Vector2Int targetCell;  // 记录目标位置，玩家选择蘑菇后放置

    private void Start()
    {
        grid = FindObjectOfType<TileGridService>();
        panel.SetActive(false);
    }

    public void ShowMushroomPanel(List<Building> mushrooms, Vector2Int cell)
    {
        availableMushrooms = mushrooms;
        targetCell = cell;

        // 清空容器中的按钮
        foreach (Transform child in mushroomsContainer)
        {
            Destroy(child.gameObject);
        }

        // 为每个蘑菇建筑创建按钮
        foreach (var mushroom in mushrooms)
        {
            Button button = Instantiate(mushroomButtonPrefab, mushroomsContainer);
            button.GetComponentInChildren<Text>().text = mushroom.name;  // 显示蘑菇的名称
            button.onClick.AddListener(() => OnMushroomSelected(mushroom));
        }

        // 显示面板
        panel.SetActive(true);
    }

    // 玩家选择蘑菇后，放置蘑菇并关闭面板
    private void OnMushroomSelected(Building selectedMushroom)
    {
        PlaceMushroom(selectedMushroom);
        HidePanel();
    }

    // 放置蘑菇
    private void PlaceMushroom(Building mushroom)
    {
        // 检查目标格子是否可以放置蘑菇
        if (mushroom != null && availableMushrooms.Contains(mushroom))
        {
            var mushroomInstance = Instantiate(mushroom, grid.CellToWorld(targetCell), Quaternion.identity);
            mushroomInstance.OnPlaced(grid, targetCell);  // 设置位置和网格
        }
    }

    // 隐藏蘑菇选择面板
    private void HidePanel()
    {
        panel.SetActive(false);
    }
}