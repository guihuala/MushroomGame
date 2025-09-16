using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CostRow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image icon;
    [SerializeField] private Text amountText; 
    
    [Header("Colors")]
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color lackColor   = new Color(1f, 0.35f, 0.35f, 1f);

    private ItemDef _item;
    private int _need;

    public void Bind(ItemDef item, int need, int have)
    {
        _item = item;
        _need = Mathf.Max(0, need);

        if (icon != null)
            icon.sprite = item != null ? item.icon : null;

        UpdateAmount(have);
    }

    public void UpdateAmount(int have)
    {
        have = Mathf.Max(0, have);

        if (amountText != null)
        {
            amountText.text = $"{have}/{_need}";
            amountText.color = (have >= _need) ? enoughColor : lackColor;
        }
    }
}
