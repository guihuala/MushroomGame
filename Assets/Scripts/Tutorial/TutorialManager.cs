// TutorialManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialManager : Singleton<TutorialManager>, IManager
{
    [Header("Data")]
    [SerializeField] private TutorialSequence sequence;     // 在 Inspector 赋值
    [SerializeField] private string savePrefix = "TUT_SEEN_"; // PlayerPrefs 键前缀

    [Header("UI")]
    [SerializeField] private TutorialUI ui;                 // 在 Inspector 赋值

    // 消息中心（按你项目需要引入）
    private readonly Dictionary<string, List<TutorialStep>> _stepsByMsg = new();
    private readonly HashSet<string> _seen = new();         // 运行时缓存去重
    private readonly Queue<TutorialStep> _queue = new();

    private bool _showing;

    public void Initialize()
    {
        BuildIndex();
        RegisterAllMsg();
        PreloadSeen();
    }

    private void OnDestroy()
    {
        UnregisterAllMsg();
    }

    // 把 steps 按 triggerMsg 分类索引
    private void BuildIndex()
    {
        _stepsByMsg.Clear();
        if (sequence == null || sequence.steps == null) return;

        foreach (var step in sequence.steps.Where(s => s != null))
        {
            if (!string.IsNullOrEmpty(step.triggerMsg))
            {
                if (!_stepsByMsg.TryGetValue(step.triggerMsg, out var list))
                {
                    list = new List<TutorialStep>();
                    _stepsByMsg[step.triggerMsg] = list;
                }
                list.Add(step);
            }
        }

        // 每个消息内按 priority 排序
        foreach (var kv in _stepsByMsg) kv.Value.Sort((a, b) => a.priority.CompareTo(b.priority));
    }

    private void RegisterAllMsg()
    {
        if (sequence == null || sequence.steps == null) return;
        
    }

    private void UnregisterAllMsg()
    {
        
    }

    // 事件到来时派发
    private void OnMsg(params object[] args)
    {
        // 约定：args[0] = msgName（如果你的 MsgCenter 不这样，请改成它的签名）
        if (args == null || args.Length == 0) return;
        var msgName = args[0] as string;
        if (string.IsNullOrEmpty(msgName)) return;

        if (_stepsByMsg.TryGetValue(msgName, out var list))
        {
            foreach (var step in list)
                TryEnqueue(step);
            TryShowNext();
        }
    }

    // 手动触发（供代码使用）
    public void TriggerById(string id)
    {
        var step = sequence.steps.FirstOrDefault(s => s != null && s.id == id);
        if (step != null)
        {
            TryEnqueue(step);
            TryShowNext();
        }
    }

    // 从菜单回看某一步
    public void ShowById(string id)
    {
        var step = sequence.steps.FirstOrDefault(s => s != null && s.id == id);
        if (step != null)
        {
            // 回看不写入已看
            ShowNow(step, markSeen: false);
        }
    }

    private void TryEnqueue(TutorialStep step)
    {
        if (step == null) return;

        // 避免重复排队
        if (_queue.Contains(step)) return;

        _queue.Enqueue(step);
    }

    private void TryShowNext()
    {
        if (_showing || _queue.Count == 0) return;
        var step = _queue.Dequeue();
        ShowNow(step, markSeen: true);
    }

    private void ShowNow(TutorialStep step, bool markSeen)
    {
        _showing = true;

        ui.Show(step.title, step.descriptionTMP, step.illustration, step.pauseGame, () =>
        {
            if (markSeen) SetSeen(step.id);
            _showing = false;
            TryShowNext();
        });
    }

    // ---- Seen 存档 ----
    private void PreloadSeen()
    {
        _seen.Clear();
        if (sequence == null || sequence.steps == null) return;
        foreach (var step in sequence.steps)
        {
            if (step == null || string.IsNullOrEmpty(step.id)) continue;
            if (PlayerPrefs.GetInt(savePrefix + step.id, 0) == 1)
                _seen.Add(step.id);
        }
    }

    private bool IsSeen(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return _seen.Contains(id);
    }

    private void SetSeen(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _seen.Add(id);
        PlayerPrefs.SetInt(savePrefix + id, 1);
        PlayerPrefs.Save();
    }
}