using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ConnectionLine : MonoBehaviour
{
    private RectTransform from;
    private RectTransform to;
    private Image lineImage;

    private void Awake()
    {
        lineImage = GetComponent<Image>();
    }

    public void Initialize(RectTransform fromTransform, RectTransform toTransform)
    {
        from = fromTransform;
        to = toTransform;
        UpdateLine();
    }

    private void Update()
    {
        if (from != null && to != null)
        {
            UpdateLine();
        }
    }

    private void UpdateLine()
    {
        Vector2 fromPos = from.anchoredPosition;
        Vector2 toPos = to.anchoredPosition;
        
        // 设置位置和旋转
        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        GetComponent<RectTransform>().anchoredPosition = fromPos + direction * 0.5f;
        GetComponent<RectTransform>().sizeDelta = new Vector2(distance, 5f);
        GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, angle);
    }
}