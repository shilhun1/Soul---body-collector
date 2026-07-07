using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossPatternDataSO 목록에서 현재 HP 조건과 쿨타임에 맞는 보스 패턴을 실행하는 기본 시스템입니다.
/// 실제 애니메이션 타이밍과 히트박스 생성은 SkillActionSystem 또는 애니메이션 이벤트로 확장합니다.
/// </summary>
public class HWJ_BossPatternSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_BossPatternDataSO[] patterns;
    [SerializeField] private bool autoUsePatterns;

    private readonly Dictionary<string, float> nextUseTimes = new Dictionary<string, float>();

    private void Awake()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }
    }

    private void Update()
    {
        if (autoUsePatterns)
        {
            TryUseAvailablePattern();
        }
    }

    /// <summary>
    /// 현재 조건에서 사용할 수 있는 패턴 하나를 실행합니다.
    /// 보스 AI가 공격 타이밍을 잡았을 때 호출하는 용도로 사용합니다.
    /// </summary>
    public bool TryUseAvailablePattern()
    {
        return TryUseAvailablePattern(null);
    }

    public bool TryUseAvailablePattern(Transform target)
    {
        if (!TrySelectPattern(out HWJ_BossPatternDataSO pattern))
        {
            return false;
        }

        return ExecutePattern(pattern, target);
    }

    public bool TryUsePhaseChangedPattern(Transform target)
    {
        if (!TrySelectPattern(HWJ_BossPatternTrigger.PhaseChanged, out HWJ_BossPatternDataSO pattern))
        {
            return false;
        }

        return ExecutePattern(pattern, target);
    }

    private bool ExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        if (pattern == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(pattern.PatternId))
        {
            nextUseTimes[pattern.PatternId] = Time.time + pattern.CooldownSeconds;
        }

        if (pattern.SkillActions != null && skillActionSystem != null)
        {
            for (int i = 0; i < pattern.SkillActions.Length; i++)
            {
                skillActionSystem.TryUseSkill(pattern.SkillActions[i], target);
            }
        }

        return true;
    }

    private bool TrySelectPattern(out HWJ_BossPatternDataSO selectedPattern)
    {
        return TrySelectPattern(null, out selectedPattern);
    }

    private bool TrySelectPattern(HWJ_BossPatternTrigger? requiredTrigger, out HWJ_BossPatternDataSO selectedPattern)
    {
        selectedPattern = null;

        if (patterns == null || runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return false;
        }

        float hpRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;

        for (int i = 0; i < patterns.Length; i++)
        {
            HWJ_BossPatternDataSO pattern = patterns[i];

            if (pattern == null || !pattern.IsHpConditionMatched(hpRatio))
            {
                continue;
            }

            if (requiredTrigger.HasValue)
            {
                if (pattern.Trigger != requiredTrigger.Value)
                {
                    continue;
                }
            }
            else if (pattern.Trigger == HWJ_BossPatternTrigger.PhaseChanged)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(pattern.PatternId)
                && nextUseTimes.TryGetValue(pattern.PatternId, out float nextUseTime)
                && Time.time < nextUseTime)
            {
                continue;
            }

            selectedPattern = pattern;
            return true;
        }

        return false;
    }
}
