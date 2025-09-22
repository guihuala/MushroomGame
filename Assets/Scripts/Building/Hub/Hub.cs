using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HubStage
{
    public string stageName;
    public Sprite stageSprite;
    public List<ItemRequirement> requirements;

    public List<Vector2Int> portOffsets = new();
}

[Serializable]
public class ItemRequirement
{
    public ItemDef item;
    public int requiredAmount;
    public int currentAmount;
}

public class Hub : MonoBehaviour
{
    public TileGridService grid;

    private Vector2Int centerCell;
    private readonly List<HubPort> _ports = new();

    [Header("Storage")] public int maxStorage = 999;

    [Header("Stage Settings")] public List<HubStage> stages = new();
    public int currentStageIndex = 0;
    public bool isFinalStageComplete = false;

    [Header("Initial Items")] public List<ItemStack> initialItems = new();

    [Header("Stage Badge")] public Sprite stageBadgeSprite; // 每个阶段完成后生成的徽章图
    public Vector3 badgeLocalOffset = new Vector3(0f, 1.4f, 0f); // 徽章锚点：头顶
    public float badgeSpacingX = 0.35f; // 多个徽章横向间距
    public float badgeScale = 1.0f; // 徽章缩放
    public int badgeSortingOrder = 5000; // 渲染层级
    private Transform _badgeRoot; // 徽章根节点
    private int _badgeCount = 0; // 已生成徽章数量


    void Start()
    {
        grid = FindObjectOfType<TileGridService>();
        centerCell = grid.WorldToCell(transform.position);

        var sr = GetComponent<SpriteRenderer>();

        RegisterInitialPorts();
        InitializeCurrentStage();
        AddInitialItemsToInventory();

        if (_badgeRoot == null)
        {
            var go = new GameObject("Badges");
            _badgeRoot = go.transform;
            _badgeRoot.SetParent(transform, worldPositionStays: false);
            _badgeRoot.localPosition = badgeLocalOffset;
        }
    }

    private void AddInitialItemsToInventory()
    {
        foreach (var initialItem in initialItems)
        {
            if (initialItem.item != null && initialItem.amount > 0)
            {
                InventoryManager.Instance.AddItemStack(initialItem);
            }
        }
    }

    private void InitializeCurrentStage()
    {
        if (stages.Count == 0) return;

        currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stages.Count - 1);
        UpdateStageVisuals();
    }

    private void RegisterInitialPorts()
    {
        AddPort(Vector2Int.zero);
        AddPort(Vector2Int.left);
        AddPort(Vector2Int.right);
    }

    private void OnDestroy()
    {
        foreach (var port in _ports)
        {
            grid.UnregisterPort(port.Cell, port);
        }
    }

    public void AddPort(Vector2Int offset)
    {
        DebugManager.Log("AddPort: " + offset.ToString());
        var cell = centerCell + offset + Vector2Int.up;
        var port = new HubPort(cell, this);
        _ports.Add(port);
        grid.RegisterPort(cell, port);
    }

    public bool ReceiveItem(in ItemPayload payload)
    {
        if (InventoryManager.Instance.GetTotalItemCount() >= maxStorage)
        {
            DebugManager.LogWarning("Hub storage full!", this);
            return false;
        }

        bool success = InventoryManager.Instance.AddItem(payload.item, payload.amount);

        if (success)
        {
            MsgCenter.SendMsg(MsgConst.HUB_ITEM_RECEIVED, payload);
            CheckStageCompletion();
        }

        return success;
    }

    private void CheckStageCompletion()
    {
        if (isFinalStageComplete || currentStageIndex >= stages.Count) return;

        var currentStage = stages[currentStageIndex];
        bool stageComplete = true;

        foreach (var requirement in currentStage.requirements)
        {
            int currentAmount = InventoryManager.Instance.GetItemCount(requirement.item);
            if (currentAmount < requirement.requiredAmount)
            {
                stageComplete = false;
                break;
            }
        }

        if (stageComplete)
        {
            CompleteCurrentStage();
        }
    }

    private void CompleteCurrentStage()
    {
        var currentStage = stages[currentStageIndex];
        foreach (var requirement in currentStage.requirements)
        {
            InventoryManager.Instance.RemoveItem(requirement.item, requirement.requiredAmount);
        }

        MsgCenter.SendMsg(MsgConst.HUB_STAGE_COMPLETED, currentStageIndex);

        TrySpawnStageBadge();

        if (currentStageIndex < stages.Count - 1)
        {
            currentStageIndex++;
            UpdateStageVisuals();
        }
        else
        {
            isFinalStageComplete = true;
            MsgCenter.SendMsgAct(MsgConst.HUB_ALL_STAGES_COMPLETE);
            DebugManager.Log("All hub stages completed!", this);

            GameManager.Instance.SetGameState(GameManager.GameState.GameCleared);
        }
    }

    private void UpdateStageVisuals()
    {
        if (currentStageIndex >= stages.Count) return;

        var currentStage = stages[currentStageIndex];

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 更新贴图
            if (currentStage.stageSprite != null)
            {
                spriteRenderer.sprite = currentStage.stageSprite;
            }
        }
        
        RefreshPortsForStage();
    }

    public int GetItemCount(ItemDef item)
    {
        return InventoryManager.Instance.GetItemCount(item);
    }

    public int GetTotalItemCount()
    {
        return InventoryManager.Instance.GetTotalItemCount();
    }

    public HubStage GetCurrentStage()
    {
        return currentStageIndex < stages.Count ? stages[currentStageIndex] : null;
    }

    public float GetItemProgress(ItemDef item)
    {
        if (currentStageIndex >= stages.Count || isFinalStageComplete) return 1f;
        
        var currentStage = stages[currentStageIndex];
        foreach (var requirement in currentStage.requirements)
        {
            if (requirement.item == item)
            {
                int currentAmount = GetItemCount(item);
                return Mathf.Clamp01((float)currentAmount / requirement.requiredAmount);
            }
        }
        
        return 0f;
    }
    
    private void TrySpawnStageBadge()
    {
        if (stageBadgeSprite == null) return;
        if (_badgeRoot == null)
        {
            var go = new GameObject("Badges");
            _badgeRoot = go.transform;
            _badgeRoot.SetParent(transform, worldPositionStays: false);
            _badgeRoot.localPosition = badgeLocalOffset;
        }

        // 计算该徽章的相对位置：横向排布（也可改为纵向/圆弧等）
        var offset = new Vector3((_badgeCount) * badgeSpacingX, 0f, 0f);
        
        var goBadge = MushroomAnimator.CreateBadge(
            _badgeRoot,
            stageBadgeSprite,
            offset,
            badgeScale,
            badgeSortingOrder,
            name: $"StageBadge_{_badgeCount + 1}"
        );

        _badgeCount++;
    }
    
    private void RefreshPortsForStage()
    {
        // 先清理旧端口
        foreach (var port in _ports)
        {
            grid.UnregisterPort(port.Cell, port);
        }
        _ports.Clear();

        // 按当前阶段配置新端口
        var currentStage = GetCurrentStage();
        if (currentStage == null) return;

        foreach (var offset in currentStage.portOffsets)
        {
            AddPort(offset);
        }
    }
}