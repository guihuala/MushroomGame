using UnityEngine;

/// <summary>
/// 用于抵消美术锚点（如底部锚点）造成的视觉偏移。
/// anchor01 为 [0,1] 的归一化锚点： (0,0)=左下，(0.5,0)=底部中点，(0.5,0.5)=几何中心。
/// extraOffsetCells 以“格”为单位的细调偏移；例如把底边从格中心下移半格可设 (0, -0.5)。
/// </summary>
public class VisualAnchor2D : MonoBehaviour
{
    [Header("归一化锚点(0..1)")]
    public Vector2 anchor01 = new Vector2(0.5f, 0f);
    [Header("以格为单位的附加偏移")]
    public Vector2 extraOffsetCells = Vector2.zero;   // 如需要底边下移半格： (0, -0.5)
}