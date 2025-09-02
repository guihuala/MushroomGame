using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName="Factory/ResourceTile")]
public class ResourceTile : Tile
{
    public ItemDef yieldItem;
    public bool infinite = true;
    public int baseRichness = 50;
}