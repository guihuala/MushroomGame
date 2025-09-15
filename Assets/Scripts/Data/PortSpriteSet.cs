using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Port Sprite Set")]
public class PortSpriteSet : ScriptableObject
{
    [Header("Sprites")]
    public Sprite inputSprite;   // 流入图标
    public Sprite outputSprite;  // 流出图标

    [Header("Visual")]
    [Tooltip("图标世界尺度")]
    public float worldScale = 0.5f;
    public Vector2 worldOffset = Vector2.zero;

    [Header("Sorting")]
    public string sortingLayer = "";
    public int orderInLayerOffset = 5;  // 相对建筑主体的排序偏移
}