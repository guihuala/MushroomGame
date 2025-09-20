public class MsgConst
{
    // ========== 建筑相关消息 (1000-1999) ==========
    public const int BUILDING_PLACED = 1001; // 建筑放置事件
    public const int BUILDING_REMOVED = 1002; // 建筑移除事件
    public const int NEIGHBOR_CHANGED = 1003; // 邻居变化事件
    public const int CONVEYOR_PLACED = 1004;
    public const int CONVEYOR_REMOVED = 1005;
    public const int ERASE_MODE_ENTER = 1006;
    public const int ERASE_MODE_EXIT = 1007;
    
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
    public const int ERASE_CANCEL_SHOW_HINT = 4003;    // 显示拆除取消提示
    public const int ERASE_CANCEL_HIDE_HINT = 4004;    // 隐藏拆除取消提示
    public const int ERASE_CANCELLED = 4005;           // 拆除已取消
    public const int ERASE_CONFIRMED = 4006;           // 拆除已确认
    
    // ========== 科技树消息 (5000-5999) ==========
    public const int BUILDING_UNLOCKED = 5001;
    
    // ========== 教程 (6000-6999) ==========
    public const int BUILD_MENU_OPENED    = 6001;     // 打开建造面板
    public const int FIRST_LINE_BUILT     = 6002;      // 首条菌丝生产线建成
    public const int FIRST_MUSHROOM_PLANTED = 6003;
    public const int TECH_TREE_OPENED     = 6004;
    public const int MAP_EDGE_TOUCHED     = 6005;      // 探索到边界/黑暗区
    public const int STAGE_TASK_COMPLETED = 6006;  // 阶段任务完成
}