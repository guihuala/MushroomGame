using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class TipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TipType
    {
        Text,
        Icon,
        TextAndIcon
    }

    [Header("Tip Settings")]
    public TipType tipType = TipType.Text;
    [TextArea]
    public string textTip;
    public Sprite iconTip;

    [Header("Behavior")]
    [Tooltip("鼠标悬停时是否让 Tip 跟随鼠标移动")]
    public bool followCursor = true;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector3 screenPos = Input.mousePosition;

        switch (tipType)
        {
            case TipType.Text:
                if (!string.IsNullOrEmpty(textTip))
                    TipsController.Instance.ShowTextTip(textTip, screenPos, followCursor);
                break;

            case TipType.Icon:
                if (iconTip != null)
                    TipsController.Instance.ShowIconTip(iconTip, screenPos, followCursor);
                break;

            case TipType.TextAndIcon:
                if (!string.IsNullOrEmpty(textTip) && iconTip != null)
                    TipsController.Instance.ShowTextAndIconTip(textTip, iconTip, screenPos, followCursor);
                break;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TipsController.Instance.HideTips();
    }
}

