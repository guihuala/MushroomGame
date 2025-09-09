using UnityEngine;

[CreateAssetMenu(menuName = "Factory/Item")]
public class ItemDef : ScriptableObject
{
    public string itemId; // 唯一ID
    [TextArea] public string displayName;
    public Sprite icon;
    public float stackLimit = 100;
}