using UnityEngine;

/// <summary>
/// Contract used by HWJ_BossPatternSystem when a boss pattern needs custom logic
/// instead of only SkillActionDataSO execution.
/// </summary>
public interface HWJ_IBossSpecialPatternExecutor
{
    string ExecutorKey { get; }
    bool IsPatternRunning { get; }

    bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target);
    bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target);
    void CancelActivePattern();
}

