using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TechCostTooltip : MonoBehaviour
{
    public static TechCostTooltip Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Transform listRoot;
    [SerializeField] private GameObject costRowPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2 offset = new Vector2(16f, -16f); // 相对鼠标偏移
    [SerializeField] private RectTransform viewportForClamp;          // 用于防止出框

    private readonly List<CostRow> _rows = new();

    void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(TechNode node, Vector2 screenPos)
    {
        if (node == null || node.unlockCost == null || node.unlockCost.Count == 0)
        {
            Hide();
            return;
        }

        EnsureRows(node.unlockCost.Count);

        // 绑定/刷新
        for (int i = 0; i < _rows.Count; i++)
        {
            if (i < node.unlockCost.Count)
            {
                var cost = node.unlockCost[i];
                int have = InventoryManager.Instance.GetItemCount(cost.item);
                _rows[i].gameObject.SetActive(true);
                _rows[i].Bind(cost.item, cost.amount, have);
            }
            else
            {
                _rows[i].gameObject.SetActive(false);
            }
        }

        // 定位
        SetPositionByScreen(screenPos);

        panel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    public void RefreshAmounts(TechNode node)
    {
        if (!panel.gameObject.activeSelf || node == null) return;
        for (int i = 0; i < node.unlockCost.Count && i < _rows.Count; i++)
        {
            var cost = node.unlockCost[i];
            int have = InventoryManager.Instance.GetItemCount(cost.item);
            _rows[i].UpdateAmount(have);
        }
    }

    private void EnsureRows(int count)
    {
        // 扩容
        while (_rows.Count < count)
        {
            var go = Instantiate(costRowPrefab, listRoot);
            var row = go.GetComponent<CostRow>();
            _rows.Add(row);
        }
    }

    private void SetPositionByScreen(Vector2 screenPos)
    {
        // 转到 tooltip 的父节点坐标系
        var parent = panel.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPos, null, out var local))
        {
            var target = local + offset;

            // 防出框（可选）
            if (viewportForClamp != null)
            {
                var half = panel.rect.size * 0.5f;
                var vHalf = viewportForClamp.rect.size * 0.5f;

                target.x = Mathf.Clamp(target.x, -vHalf.x + half.x, vHalf.x - half.x);
                target.y = Mathf.Clamp(target.y, -vHalf.y + half.y, vHalf.y - half.y);
            }

            panel.anchoredPosition = target;
        }
    }
}
