using UnityEngine;

public interface IHoverPanel
{
    // 首次显示在屏幕坐标
    void ShowAtScreenPosition(Vector2 screenPos);
    // 悬浮中持续跟随
    void FollowMouse(Vector2 screenPos);
    // 关闭/隐藏
    void ClosePanel();
    // 当悬浮目标变化时刷新内容（例如更换数据源）
    void SetContext(object context);
}