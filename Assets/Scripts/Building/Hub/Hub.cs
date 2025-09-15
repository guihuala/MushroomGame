using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HubStage
{
    public string stageName;
    public Sprite stageSprite;
    public List<ItemRequirement> requirements;
    public GameObject[] stageObjectsToEnable;
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

    // NEW: 记录SpriteRenderer的初始本地位置，避免偏移累计
    private Vector3 _baseSpriteLocalPos; 

    [Header("Storage")]
    public int maxStorage = 999;
    
    [Header("Stage Settings")]
    public List<HubStage> stages = new();
    public int currentStageIndex = 0;
    public bool isFinalStageComplete = false;
    
    [Header("Initial Items")]
    public List<ItemStack> initialItems = new();

    void Start()
    {
        grid = FindObjectOfType<TileGridService>();
        centerCell = grid.WorldToCell(transform.position);

        // 记录初始本地位置（没有SpriteRenderer时也安全）
        var sr = GetComponent<SpriteRenderer>();
        _baseSpriteLocalPos = sr ? sr.transform.localPosition : Vector3.zero; // NEW

        RegisterInitialPorts();
        InitializeCurrentStage();
        AddInitialItemsToInventory();
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
        var cell = centerCell + offset;
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
        
        // 启用/禁用阶段特定的游戏对象
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageObjectsToEnable != null)
            {
                foreach (var obj in stages[i].stageObjectsToEnable)
                {
                    if (obj != null)
                    {
                        obj.SetActive(i == currentStageIndex);
                    }
                }
            }
        }
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

    public float GetCurrentStageProgress()
    {
        if (currentStageIndex >= stages.Count || isFinalStageComplete) return 1f;
        
        var currentStage = stages[currentStageIndex];
        if (currentStage.requirements.Count == 0) return 0f;
        
        float totalProgress = 0f;
        foreach (var requirement in currentStage.requirements)
        {
            int currentAmount = GetItemCount(requirement.item);
            float itemProgress = Mathf.Clamp01((float)currentAmount / requirement.requiredAmount);
            totalProgress += itemProgress;
        }
        
        return totalProgress / currentStage.requirements.Count;
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
}