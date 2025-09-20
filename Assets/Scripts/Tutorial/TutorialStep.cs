using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Step", fileName = "TutStep_")]
public class TutorialStep : ScriptableObject
{
    [Header("Identity")]
    public string id;                     // 唯一ID（用于存档/去重）
    [Tooltip("优先级越小越先弹（同帧触发冲突时用）")]
    public int priority = 100;

    [Header("Trigger")]
    [Tooltip("监听的消息名（用 MsgConst.*），为空则仅手动触发")]
    public string triggerMsg;

    [Header("UI")]
    public string title;
    [TextArea(2, 6)] public string descriptionTMP;   // 允许 <sprite name=Key>
    public Sprite illustration;                      // 配图（比如小示意）
    public bool pauseGame = false;                   // 弹窗时是否暂停
}