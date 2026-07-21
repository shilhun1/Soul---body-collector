using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossPatternDataSO 목록에서 현재 HP 조건과 쿨타임에 맞는 보스 패턴을 실행하는 기본 시스템입니다.
/// 실제 애니메이션 타이밍과 히트박스 생성은 SkillActionSystem 또는 애니메이션 이벤트로 확장합니다.
/// </summary>
public class HWJ_BossPatternSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_Stage1BossPatternSystem stageOnePatternSystem;
    [SerializeField] private MonoBehaviour[] specialPatternExecutors;
    [SerializeField] private HWJ_BossPatternDataSO[] patterns;
    [SerializeField] private bool autoUsePatterns;
    [SerializeField] private bool preventSamePatternRepeat = true;
    [SerializeField] private bool useGameplayPatternRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO bossPatternExecutionCore;
    [SerializeField] private string bossPatternExecutionCoreId = "boss_pattern_execution";
    [SerializeField] private HWJ_GameplayRuleSO bossPatternRule;
    [SerializeField] private string bossPatternRuleId = "boss_can_use_combat_pattern";
    [SerializeField] private string lastPatternResult;

    private readonly Dictionary<string, float> nextUseTimes = new Dictionary<string, float>();
    private readonly List<HWJ_IBossSpecialPatternExecutor> cachedSpecialPatternExecutors =
        new List<HWJ_IBossSpecialPatternExecutor>();
    private string lastExecutedPatternKey;

    public string LastPatternResult => lastPatternResult;
    public bool IsSpecialPatternRunning => IsAnySpecialPatternRunning();

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (stageOnePatternSystem == null)
        {
            stageOnePatternSystem = GetComponent<HWJ_Stage1BossPatternSystem>();
        }

        RefreshSpecialPatternExecutors();
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

    public bool TryUseAvailablePattern(Transform target, int phaseNumber, bool isCloseRange)
    {
        if (!TrySelectPattern(null, target, phaseNumber, true, isCloseRange, true, out HWJ_BossPatternDataSO pattern))
        {
            return false;
        }

        return ExecutePattern(pattern, target);
    }

    public bool TryUsePhaseChangedPattern(Transform target)
    {
        if (!TrySelectPattern(HWJ_BossPatternTrigger.PhaseChanged, target, 1, false, false, false, out HWJ_BossPatternDataSO pattern))
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

        if (!IsPatternRuleSatisfied(target))
        {
            return false;
        }

        bool executed = false;

        if (pattern.UseCustomPatternExecutor || pattern.UseStageOneSpecialExecution)
        {
            executed = TryExecuteSpecialPattern(pattern, target);
        }

        if (!executed && pattern.SkillActions != null && skillActionSystem != null)
        {
            for (int i = 0; i < pattern.SkillActions.Length; i++)
            {
                executed |= skillActionSystem.TryUseSkill(pattern.SkillActions[i], target);
            }
        }

        if (executed)
        {
            string patternKey = GetPatternKey(pattern);

            if (!string.IsNullOrEmpty(patternKey))
            {
                nextUseTimes[patternKey] = Time.time + pattern.EffectiveCooldownSeconds;
                lastExecutedPatternKey = patternKey;
            }
        }

        return executed;
    }

    private bool IsPatternRuleSatisfied(Transform target)
    {
        if (!useGameplayPatternRule)
        {
            return true;
        }

        HWJ_RootObjectDataResolver targetResolver = target != null
            ? target.GetComponentInParent<HWJ_RootObjectDataResolver>()
            : null;
        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(dataResolver, targetResolver)
            .WithSource(this);

        if (target != null)
        {
            context.WithTarget(target);
        }

        if (ResolveBossPatternExecutionCore(out HWJ_RuleExecutionCoreSO executionCore))
        {
            bool passed = executionCore.TryExecute(context, out HWJ_RuleExecutionResult result);
            lastPatternResult = result.Message;
            return passed;
        }

        HWJ_GameplayRuleSO rule = bossPatternRule;

        if (rule == null
            && !string.IsNullOrEmpty(bossPatternRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(bossPatternRuleId, out HWJ_GameplayRuleSO resolvedRule))
        {
            rule = resolvedRule;
        }

        if (rule == null)
        {
            return true;
        }

        bool rulePassed = rule.TryEvaluate(context, out HWJ_RuleEvaluationResult ruleResult);
        lastPatternResult = ruleResult.Message;
        return rulePassed;
    }

    private bool ResolveBossPatternExecutionCore(out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (bossPatternExecutionCore != null)
        {
            executionCore = bossPatternExecutionCore;
            return true;
        }

        if (string.IsNullOrEmpty(bossPatternExecutionCoreId))
        {
            return false;
        }

        return HWJ_GameAccess.TryGetRuleExecutionCore(bossPatternExecutionCoreId, out executionCore);
    }

    private bool TrySelectPattern(out HWJ_BossPatternDataSO selectedPattern)
    {
        return TrySelectPattern(null, null, 1, false, false, false, out selectedPattern);
    }

    private bool TrySelectPattern(HWJ_BossPatternTrigger? requiredTrigger, out HWJ_BossPatternDataSO selectedPattern)
    {
        return TrySelectPattern(requiredTrigger, null, 1, false, false, false, out selectedPattern);
    }

    private bool TrySelectPattern(
        HWJ_BossPatternTrigger? requiredTrigger,
        Transform target,
        int phaseNumber,
        bool usePhaseFilter,
        bool isCloseRange,
        bool useRangeFilter,
        out HWJ_BossPatternDataSO selectedPattern)
    {
        selectedPattern = null;

        HWJ_BossPatternDataSO[] availablePatterns = GetAvailablePatterns();

        if (availablePatterns == null || runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return false;
        }

        float hpRatio = runtimeStatus.CurrentHp / runtimeStatus.MaxHp;

        for (int i = 0; i < availablePatterns.Length; i++)
        {
            HWJ_BossPatternDataSO pattern = availablePatterns[i];

            if (pattern == null || !pattern.IsHpConditionMatched(hpRatio))
            {
                continue;
            }

            if (usePhaseFilter && !pattern.IsPhaseAllowed(phaseNumber))
            {
                continue;
            }

            if (useRangeFilter && !pattern.IsRangeMatched(isCloseRange))
            {
                continue;
            }

            if ((pattern.UseCustomPatternExecutor || pattern.UseStageOneSpecialExecution)
                && !CanUseSpecialPattern(pattern, target))
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

            string patternKey = GetPatternKey(pattern);

            if (preventSamePatternRepeat
                && !string.IsNullOrEmpty(patternKey)
                && patternKey == lastExecutedPatternKey)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(patternKey)
                && nextUseTimes.TryGetValue(patternKey, out float nextUseTime)
                && Time.time < nextUseTime)
            {
                continue;
            }

            selectedPattern = pattern;
            return true;
        }

        return false;
    }

    public void CancelActiveSpecialPatterns()
    {
        RefreshSpecialPatternExecutors();

        for (int i = 0; i < cachedSpecialPatternExecutors.Count; i++)
        {
            cachedSpecialPatternExecutors[i]?.CancelActivePattern();
        }
    }

    private bool TryExecuteSpecialPattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        RefreshSpecialPatternExecutors();

        for (int i = 0; i < cachedSpecialPatternExecutors.Count; i++)
        {
            HWJ_IBossSpecialPatternExecutor executor = cachedSpecialPatternExecutors[i];

            if (executor != null
                && executor.CanUsePattern(pattern, target)
                && executor.TryExecutePattern(pattern, target))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanUseSpecialPattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        RefreshSpecialPatternExecutors();

        for (int i = 0; i < cachedSpecialPatternExecutors.Count; i++)
        {
            HWJ_IBossSpecialPatternExecutor executor = cachedSpecialPatternExecutors[i];

            if (executor != null && executor.CanUsePattern(pattern, target))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAnySpecialPatternRunning()
    {
        RefreshSpecialPatternExecutors();

        for (int i = 0; i < cachedSpecialPatternExecutors.Count; i++)
        {
            if (cachedSpecialPatternExecutors[i] != null && cachedSpecialPatternExecutors[i].IsPatternRunning)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshSpecialPatternExecutors()
    {
        cachedSpecialPatternExecutors.Clear();

        AddSpecialPatternExecutor(stageOnePatternSystem);

        MonoBehaviour[] localComponents = GetComponents<MonoBehaviour>();

        for (int i = 0; i < localComponents.Length; i++)
        {
            AddSpecialPatternExecutor(localComponents[i] as HWJ_IBossSpecialPatternExecutor);
        }

        if (specialPatternExecutors == null)
        {
            return;
        }

        for (int i = 0; i < specialPatternExecutors.Length; i++)
        {
            AddSpecialPatternExecutor(specialPatternExecutors[i] as HWJ_IBossSpecialPatternExecutor);
        }
    }

    private void AddSpecialPatternExecutor(HWJ_IBossSpecialPatternExecutor executor)
    {
        if (executor == null || cachedSpecialPatternExecutors.Contains(executor))
        {
            return;
        }

        cachedSpecialPatternExecutors.Add(executor);
    }

    private HWJ_BossPatternDataSO[] GetAvailablePatterns()
    {
        if (patterns != null && patterns.Length > 0)
        {
            return patterns;
        }

        return HWJ_GameAccess.Database != null ? HWJ_GameAccess.Database.BossPatterns : null;
    }

    private static string GetPatternKey(HWJ_BossPatternDataSO pattern)
    {
        if (pattern == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(pattern.PatternId))
        {
            return pattern.PatternId;
        }

        return pattern.PatternNumber > 0 ? $"Pattern_{pattern.PatternNumber}" : null;
    }
}
