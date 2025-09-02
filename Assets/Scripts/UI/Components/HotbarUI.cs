using UnityEngine;
using UnityEngine.UI;

// 测试用
public class HotbarUI : MonoBehaviour
{
    public PlacementSystem placement;

    [System.Serializable]
    public class Slot
    {
        public Image bg;
        public Text label;
    }

    public Slot[] slots = new Slot[3];

    [Header("Colors")] public Color normal = new Color(1, 1, 1, 0.35f);
    public Color selected = Color.white;

    [Header("Names")] public string name1 = "Miner";
    public string name2 = "Conveyor";

    void Update()
    {
        if (!placement || slots == null || slots.Length < 3) return;
        var names = new[] { name1, name2 };
        for (int i = 0; i < 3; i++)
        {
            if (slots[i]?.label) slots[i].label.text = $"{i + 1}. {names[i]}";
            if (slots[i]?.bg) slots[i].bg.color = (placement.SelectedIndex - 1 == i) ? selected : normal;
        }
    }
}