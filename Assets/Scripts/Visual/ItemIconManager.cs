using UnityEngine;


public class ItemIconManager : Singleton<ItemIconManager>
{
    [SerializeField] private GameObject itemIconPrefab;
    
    /// <summary>
    /// 创建物品图标
    /// </summary>
    /// <param name="itemDef">物品信息</param>
    /// <param name="position">生成位置</param>
    /// <returns>图标实例</returns>
    public GameObject CreateItemIcon(ItemDef itemDef, Vector3 position)
    {
        if (itemDef == null || itemDef.icon == null)
        {
            Debug.LogWarning("ItemDef or icon is missing.");
            return null;
        }

        // 从对象池中获取物品图标
        GameObject iconObject = ObjectPool.Instance.GetObject(itemIconPrefab);

        var spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = itemDef.icon;
        }

        iconObject.transform.position = position;
        iconObject.SetActive(true);

        // 为图标添加生命周期管理
        PushSelfBase pushSelfBase = iconObject.GetComponent<PushSelfBase>();
        if (pushSelfBase == null)
        {
            pushSelfBase = iconObject.AddComponent<PushSelfBase>();
        }

        pushSelfBase.InitPushTimer(); // 初始化推送计时器

        return iconObject;
    }

    // 销毁物品图标
    public void DestroyItemIcon(GameObject iconObject)
    {
        if (iconObject != null)
        {
            ObjectPool.Instance.PushObject(iconObject);
        }
    }
}
