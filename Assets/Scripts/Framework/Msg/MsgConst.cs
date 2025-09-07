public class MsgConst
{
    // ========== 建筑相关消息 (1000-1999) ==========
    public const int BUILDING_PLACED = 1001; // 建筑放置事件
    public const int BUILDING_REMOVED = 1002; // 建筑移除事件
    public const int NEIGHBOR_CHANGED = 1003; // 邻居变化事件
    public const int CONVEYOR_PLACED = 1004;
    public const int CONVEYOR_REMOVED = 1005;
    public const int SHOW_MUSHROOM_PANEL = 1006; // 弹出蘑菇面板
    
    // ========== Hub 相关消息 (2000-2999) ==========
    public const int HUB_CLICKED = 2001;
    public const int HUB_ITEM_RECEIVED = 2002;
    public const int HUB_STAGE_COMPLETED = 2003;
    public const int HUB_ALL_STAGES_COMPLETE = 2004;
    
    // ========== 库存相关消息 (3000-3999) ==========
    public const int INVENTORY_CHANGED = 3001;
    public const int INVENTORY_ITEM_ADDED = 3002;
    public const int INVENTORY_ITEM_REMOVED = 3003;
    
    // ========== UI 相关消息 (4000-4999) ==========
    public const int UI_TASK_PANEL_UPDATE = 4001;
    public const int UI_HUD_INVENTORY_UPDATE = 4002;
    
    // ========== 科技树消息 (5000-5999) ==========
    public const int BUILDING_UNLOCKED = 5001;
    
    // ========== 游戏状态消息 (6000-6999) ==========
    public const int GAME_PAUSED = 6001;
    public const int GAME_RESUMED = 6002;
}