using System;
using UnityEngine;

// HWJ 공통 패턴 선택기가 HYS 중간보스2 패턴을 호출할 수 있게 연결하는 읽기 전용 어댑터입니다.
[DisallowMultipleComponent]
public class hys_SecondBossPatternExecutor : MonoBehaviour, HWJ_IBossSpecialPatternExecutor
{
    public const string HysExecutorKey = "hys_second_boss";

    [SerializeField] private hys_SecondBossPattern patternSystem;

    public string ExecutorKey => HysExecutorKey;
    public bool IsPatternRunning => patternSystem != null && patternSystem.IsPatternRunning;

    private void Awake()
    {
        CacheReferences();
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        CacheReferences();
        if (!isActiveAndEnabled || patternSystem == null || pattern == null || target == null) return false;
        if (!string.Equals(pattern.CustomPatternExecutorKey, HysExecutorKey, StringComparison.Ordinal)) return false;
        return TryMapPattern(pattern.PatternNumber, out hys_SecondBossPatternId patternId)
            && patternSystem.CanUsePattern(patternId, target);
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        if (!CanUsePattern(pattern, target)) return false;
        TryMapPattern(pattern.PatternNumber, out hys_SecondBossPatternId patternId);
        float direction = Mathf.Approximately(target.position.x, transform.position.x)
            ? 1f
            : Mathf.Sign(target.position.x - transform.position.x);
        return patternSystem.TryStartPattern(patternId, target, direction, null);
    }

    public void CancelActivePattern()
    {
        CacheReferences();
        patternSystem?.CancelActivePattern();
    }

    public void Initialize(hys_SecondBossPattern patterns)
    {
        patternSystem = patterns;
    }

    private void CacheReferences()
    {
        if (patternSystem == null) patternSystem = GetComponent<hys_SecondBossPattern>();
    }

    private static bool TryMapPattern(int patternNumber, out hys_SecondBossPatternId patternId)
    {
        switch (patternNumber)
        {
            case 1: patternId = hys_SecondBossPatternId.DashReverseSlash; return true;
            case 2: patternId = hys_SecondBossPatternId.SummonCommand; return true;
            case 3: patternId = hys_SecondBossPatternId.HighSpeedPiercingSlash; return true;
            case 4: patternId = hys_SecondBossPatternId.DarkMagicSummonAssault; return true;
            case 5: patternId = hys_SecondBossPatternId.MagicSwordEncirclement; return true;
            case 6: patternId = hys_SecondBossPatternId.GroundSwordEruption; return true;
            default:
                patternId = hys_SecondBossPatternId.None;
                return false;
        }
    }
}
