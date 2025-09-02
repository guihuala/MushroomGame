using UnityEngine;

public class ResourceNode : MonoBehaviour, IResourceSource
{
    public ItemDef yieldItem;
    public int richness = 999999;
    public bool infinite = true;

    public ItemDef YieldItem => yieldItem;

    public bool TryConsumeOnce()
    {
        if (infinite) return true;
        if (richness <= 0) return false;
        richness--;
        return true;
    }
}