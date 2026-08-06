using UnityEngine;

[RequireComponent(typeof(HWJ_RootObjectDataResolver))]
[RequireComponent(typeof(HWJ_SoulSystem))]
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Systems/Body Decay System")]
public class HWJ_BodyDecaySystem : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;

    [Space(8f)]
    [Header("시체 부패 런타임")]
    [Tooltip("현재 시체에 누적된 부패 수치입니다. 최대치에 도달하면 육체가 붕괴합니다.")]
    [SerializeField] private float currentDecayValue;

    [Tooltip("새로운 시체에 빙의할 때 부패 수치를 초기화합니다.")]
    [SerializeField] private bool resetDecayWhenEnterBody = true;

    [Tooltip("현재 시체 부패가 시간 또는 행동에 따라 증가하고 있는지 표시합니다.")]
    [SerializeField] private bool isDecaying;

    [Tooltip("마지막 부패 처리 결과입니다. UI와 디버깅에서 상태 확인용으로 사용합니다.")]
    [SerializeField] private string runtimeStateMessage;

    private float decayTimer;
    private bool hasInitializedDecay;

    private HWJ_SoulRuntimeState previousSoulState;
    private HWJ_RootObjectDataResolver previousPossessedBodyResolver;

    private HWJ_DecayDangerLevel previousDangerLevel =
        HWJ_DecayDangerLevel.Stable;

    public float CurrentDecayValue =>
        TryGetCurrentPossessedBodyState(
            out HWJ_PossessedBodyRuntimeState bodyState)
            ? bodyState.CurrentDecayValue
            : currentDecayValue;

    public float MaxDecayValue =>
        TryGetCurrentPossessedBodyState(
            out HWJ_PossessedBodyRuntimeState bodyState)
            ? bodyState.MaxDecayValue
            : TryGetBodyDecayData(
                out HWJ_BodyDecayData bodyDecay)
                ? Mathf.Max(
                    0f,
                    bodyDecay.maxDecayValue)
                : 0f;

    public float CurrentDecayRatio =>
        MaxDecayValue > 0f
            ? Mathf.Clamp01(
                CurrentDecayValue / MaxDecayValue)
            : 0f;

    public float RemainingDecayValue =>
        Mathf.Max(
            0f,
            MaxDecayValue - CurrentDecayValue);

    public float RemainingDecayRatio =>
        MaxDecayValue > 0f
            ? Mathf.Clamp01(
                RemainingDecayValue / MaxDecayValue)
            : 0f;

    public bool HasDecayRemaining =>
        MaxDecayValue <= 0f
        || CurrentDecayValue < MaxDecayValue;

    public bool IsDecaying =>
        isDecaying;

    public HWJ_DecayDangerLevel CurrentDangerLevel =>
        ResolveDangerLevel();

    public string RuntimeStateMessage =>
        runtimeStateMessage;

    /*
     * 기존 UI나 외부 시스템과의 호환성을 위해
     * 기존 정신력 이름의 프로퍼티는 유지합니다.
     *
     * 실제 의미는 시체 부패 수치입니다.
     */
    public float ConsumedPossessionMentalValue =>
        CurrentDecayValue;

    public float CurrentPossessionMentalValue =>
        RemainingDecayValue;

    public float MaxPossessionMentalValue =>
        MaxDecayValue;

    public float RemainingPossessionMentalValue =>
        RemainingDecayValue;

    public float ConsumedPossessionMentalRatio =>
        CurrentDecayRatio;

    public float CurrentPossessionMentalRatio =>
        RemainingDecayRatio;

    public float RemainingPossessionMentalRatio =>
        RemainingDecayRatio;

    public bool HasPossessionMentalRemaining =>
        HasDecayRemaining;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();

        if (TryGetBodyDecayData(
                out HWJ_BodyDecayData bodyDecay))
        {
            SetDecayToInitial(bodyDecay);
        }

        previousSoulState =
            soulSystem != null
                ? soulSystem.CurrentState
                : HWJ_SoulRuntimeState.Body;
    }

    private void Update()
    {
        ResolveReferences();

        /*
         * 살아 있는 몬스터에게 빙의한 상태에서는
         * 시체 부패가 절대 증가하지 않습니다.
         */
        if (possessionSystem != null
            && possessionSystem.IsLivePossessionActive)
        {
            StopDecay(
                "생체 빙의 중에는 부패가 적용되지 않습니다.");

            return;
        }

        if (soulSystem == null)
        {
            StopDecay("Missing HWJ_SoulSystem.");
            return;
        }

        HWJ_SoulRuntimeState currentSoulState =
            soulSystem.CurrentState;

        HWJ_RootObjectDataResolver currentPossessedBodyResolver =
            possessionSystem != null
                ? possessionSystem.PossessedBodyResolver
                : null;

        SyncDecayFromRuntimeBodyState();

        /*
         * 새로운 시체에 빙의한 경우 부패 수치를 초기화합니다.
         */
        if (currentSoulState == HWJ_SoulRuntimeState.Body
            && (previousSoulState
                    != HWJ_SoulRuntimeState.Body
                || previousPossessedBodyResolver
                    != currentPossessedBodyResolver)
            && currentPossessedBodyResolver != null
            && resetDecayWhenEnterBody)
        {
            ResetDecay();
        }

        previousSoulState =
            currentSoulState;

        previousPossessedBodyResolver =
            currentPossessedBodyResolver;

        /*
         * 유령 상태에서는 부패가 적용되지 않습니다.
         */
        if (currentSoulState != HWJ_SoulRuntimeState.Body)
        {
            StopDecay(
                $"Soul state is {currentSoulState}.");

            return;
        }

        if (possessionSystem == null
            || !possessionSystem.HasActivePossessedBody)
        {
            StopDecay(
                "부패는 시체 빙의 상태에서만 적용됩니다.");

            return;
        }

        if (!TryGetBodyDecayData(
                out HWJ_BodyDecayData bodyDecay))
        {
            StopDecay(
                "시체 부패 데이터가 없습니다.");

            return;
        }

        if (!bodyDecay.startDecayOnEnterBody)
        {
            StopDecay(
                "시체 부패 증가가 비활성화되어 있습니다.");

            return;
        }

        if (!hasInitializedDecay)
        {
            SetDecayToInitial(bodyDecay);
        }

        isDecaying = true;
        runtimeStateMessage =
            "시체 부패 증가가 진행 중입니다.";

        if (MaxDecayValue > 0f
            && currentDecayValue >= MaxDecayValue)
        {
            TryReleasePossessionWhenDecayMaxed(
                bodyDecay);

            return;
        }

        float tickSeconds =
            Mathf.Max(
                0.01f,
                bodyDecay.decayTickSeconds);

        decayTimer += Time.deltaTime;

        if (decayTimer < tickSeconds)
        {
            return;
        }

        decayTimer = 0f;

        ApplyDecayAmount(
            bodyDecay.decayAmountPerTick,
            "time_tick",
            bodyDecay);

        TryReleasePossessionWhenDecayMaxed(
            bodyDecay);

        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ResetDecay()
    {
        ResolveReferences();

        if (TryGetBodyDecayData(
                out HWJ_BodyDecayData bodyDecay))
        {
            SetDecayToInitial(bodyDecay);
        }
    }

    public void RestoreDecaySnapshot(
        float restoredDecayValue)
    {
        ResolveReferences();

        if (!TryGetBodyDecayData(
                out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        SetCurrentDecayValue(
            restoredDecayValue,
            bodyDecay);

        decayTimer = 0f;
        hasInitializedDecay = true;
        previousDangerLevel = CurrentDangerLevel;

        previousSoulState =
            soulSystem != null
                ? soulSystem.CurrentState
                : previousSoulState;

        previousPossessedBodyResolver =
            possessionSystem != null
                ? possessionSystem.PossessedBodyResolver
                : previousPossessedBodyResolver;
    }

    public void ApplyHitDecayPenalty()
    {
        ApplyHitDecayPenalty(0f);
    }

    public void ApplyHitDecayPenalty(
        float incomingDamage)
    {
        /*
         * 피격은 부패를 증가시키지 않고
         * 빙의체의 HP만 감소시킵니다.
         */
        runtimeStateMessage =
            "피격으로 빙의체 HP만 감소했으며 "
            + "부패는 변경되지 않았습니다.";
    }

    public void ApplyMoveDecay(
        float deltaSeconds)
    {
        if (!TryBeginActionDecay(
                out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(
            bodyDecay.moveDecayPerSecond
            * Mathf.Max(0f, deltaSeconds),
            "move",
            bodyDecay);

        TryReleasePossessionWhenDecayMaxed(
            bodyDecay);

        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplyBasicAttackDecay()
    {
        ApplyBasicAttackDecay(null);
    }

    public void ApplyBasicAttackDecay(
        HWJ_SkillActionDataSO skillAction)
    {
        ApplyBasicAttackDecay(
            skillAction,
            0f);
    }

    public void ApplyBasicAttackDecay(
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds)
    {
        ApplyBasicAttackDecay(
            skillAction,
            chargeSeconds,
            true);
    }

    public void ApplyBasicAttackDecay(
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds,
        int comboStep)
    {
        ApplyBasicAttackDecay(
            skillAction,
            chargeSeconds,
            Mathf.Max(1, comboStep) > 1);
    }

    private void ApplyBasicAttackDecay(
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds,
        bool applyComboBodyDecayMultiplier)
    {
        if (!TryBeginActionDecay(
                out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        float decayAmount =
            ResolveSkillActionDecayAmount(
                bodyDecay.basicAttackDecayAmount,
                skillAction,
                chargeSeconds,
                applyComboBodyDecayMultiplier);

        ApplyDecayAmount(
            decayAmount,
            "basic_attack",
            bodyDecay);

        TryReleasePossessionWhenDecayMaxed(
            bodyDecay);

        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplySkillDecay()
    {
        ApplySkillDecay(null);
    }

    public void ApplySkillDecay(
        HWJ_SkillActionDataSO skillAction)
    {
        ApplySkillDecay(
            skillAction,
            0f);
    }

    public void ApplySkillDecay(
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds)
    {
        ApplySkillDecay(
            skillAction,
            chargeSeconds,
            true);
    }

    public void ApplySkillDecay(
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds,
        int comboStep)
    {
        ApplySkillDecay(
            skillAction,
            chargeSeconds,
            Mathf.Max(1, comboStep) > 1);
    }

    private void ApplySkillDecay(
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds,
        bool applyComboBodyDecayMultiplier)
    {
        if (!TryBeginActionDecay(
                out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        float decayAmount =
            ResolveSkillActionDecayAmount(
                bodyDecay.skillDecayAmount,
                skillAction,
                chargeSeconds,
                applyComboBodyDecayMultiplier);

        ApplyDecayAmount(
            decayAmount,
            "skill",
            bodyDecay);

        TryReleasePossessionWhenDecayMaxed(
            bodyDecay);

        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplyActionDecay(
        float rawDecayAmount)
    {
        if (!TryBeginActionDecay(
                out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(
            rawDecayAmount
            + bodyDecay.actionDecayAmount,
            "action",
            bodyDecay);

        TryReleasePossessionWhenDecayMaxed(
            bodyDecay);

        SavePlayerRuntimeSnapshotIfOwner();
    }

    /// <summary>
    /// 기존 외부 호출과의 호환을 위해 메서드 이름은 유지합니다.
    /// 시체 빙의 중 호출되면 전달받은 값을 부패 증가량으로 처리합니다.
    /// </summary>
    public void ApplyPossessionMentalCost(
        float rawMentalCost)
    {
        ApplyPossessionMentalCost(
            rawMentalCost,
            "corpse_decay_cost");
    }

    public void ApplyPossessionMentalCost(
        float rawMentalCost,
        string reason)
    {
        if (!TryBeginActionDecay(
                out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(
            rawMentalCost,
            string.IsNullOrWhiteSpace(reason)
                ? "corpse_decay_cost"
                : reason,
            bodyDecay);

        TryReleasePossessionWhenDecayMaxed(
            bodyDecay);

        SavePlayerRuntimeSnapshotIfOwner();
    }

    private void ResolveReferences()
    {
        if (dataResolver == null)
        {
            dataResolver =
                GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem =
                GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem =
                GetComponent<HWJ_PossessionSystem>();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem =
                GetComponent<HWJ_PossessedBodySystem>();
        }
    }

    private bool TryGetBodyDecayData(
        out HWJ_BodyDecayData bodyDecay)
    {
        bodyDecay = null;

        /*
         * 생체 빙의 상태에서는 부패 데이터를 반환하지 않습니다.
         */
        if (possessionSystem != null
            && possessionSystem.IsLivePossessionActive)
        {
            return false;
        }

        if (possessionSystem != null
            && possessionSystem
                .TryGetActivePossessedBodyDecayData(
                    out bodyDecay))
        {
            return true;
        }

        if (dataResolver == null
            || !dataResolver.TryGetTypeData(
                out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        bodyDecay = playerData.BodyDecay;
        return bodyDecay != null;
    }

    private void SetDecayToInitial(
        HWJ_BodyDecayData bodyDecay)
    {
        SetCurrentDecayValue(
            bodyDecay.initialDecayValue,
            bodyDecay);

        decayTimer = 0f;
        hasInitializedDecay = true;
        previousDangerLevel = CurrentDangerLevel;
    }

    private void StopDecay(
        string message)
    {
        isDecaying = false;
        decayTimer = 0f;
        runtimeStateMessage = message;
    }

    private void TryReleasePossessionWhenDecayMaxed(
        HWJ_BodyDecayData bodyDecay)
    {
        if (MaxDecayValue <= 0f
            || currentDecayValue < MaxDecayValue)
        {
            return;
        }

        currentDecayValue = MaxDecayValue;

        possessedBodySystem?
            .SetCurrentDecayValue(currentDecayValue);

        isDecaying = false;

        if (!bodyDecay.enterSoulStateWhenMaxed
            && !bodyDecay.enterSoulStateWhenEmpty)
        {
            runtimeStateMessage =
                ResolveDepletedMessage(
                    bodyDecay,
                    "시체 부패가 최대치에 도달했지만 "
                    + "영혼 전환이 비활성화되어 있습니다.");

            return;
        }

        runtimeStateMessage =
            ResolveDepletedMessage(
                bodyDecay,
                "시체 부패가 최대치에 도달하여 "
                + "육체가 붕괴하고 영혼 상태로 복귀합니다.");

        if (possessionSystem != null
            && possessionSystem
                .ReleasePossessedBodyByDecayDepletion())
        {
            return;
        }

        soulSystem?.EnterSoulState(
            false,
            HWJ_PossessedBodyExitReason.DecayDepleted);
    }

    private void SetCurrentDecayValue(
        float value,
        HWJ_BodyDecayData bodyDecay)
    {
        float maxDecayValue =
            bodyDecay != null
                ? Mathf.Max(
                    0f,
                    bodyDecay.maxDecayValue)
                : MaxDecayValue;

        float previousValue =
            currentDecayValue;

        HWJ_DecayDangerLevel previousLevel =
            previousDangerLevel;

        currentDecayValue =
            Mathf.Clamp(
                value,
                0f,
                maxDecayValue);

        if (possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(
                out _))
        {
            possessedBodySystem.SetCurrentDecayValue(
                currentDecayValue);
        }

        RaiseDecayEvents(
            previousValue,
            previousLevel);
    }

    private bool TryBeginActionDecay(
        out HWJ_BodyDecayData bodyDecay)
    {
        ResolveReferences();
        bodyDecay = null;

        /*
         * 유령 상태 또는 생체 빙의 상태에서는
         * 이동, 공격, 스킬에 의한 부패 증가도 차단합니다.
         */
        if (soulSystem == null
            || soulSystem.CurrentState
                != HWJ_SoulRuntimeState.Body
            || possessionSystem == null
            || !possessionSystem.HasActivePossessedBody
            || possessionSystem.IsLivePossessionActive)
        {
            return false;
        }

        if (!TryGetBodyDecayData(out bodyDecay))
        {
            return false;
        }

        if (!hasInitializedDecay)
        {
            SetDecayToInitial(bodyDecay);
        }

        SyncDecayFromRuntimeBodyState();

        return bodyDecay.startDecayOnEnterBody;
    }

    private void ApplyDecayAmount(
        float rawAmount,
        string reason,
        HWJ_BodyDecayData bodyDecay)
    {
        if (rawAmount <= 0f)
        {
            return;
        }

        float resistanceMultiplier =
            1f - Mathf.Clamp01(
                bodyDecay != null
                    ? bodyDecay.decayResistance
                    : 0f);

        float finalAmount =
            Mathf.Max(
                0f,
                rawAmount * resistanceMultiplier);

        if (finalAmount <= 0f)
        {
            return;
        }

        SetCurrentDecayValue(
            currentDecayValue + finalAmount,
            bodyDecay);

        runtimeStateMessage =
            $"시체 부패 {finalAmount:0.###} 증가. "
            + $"사유: {reason}. "
            + $"현재 부패: "
            + $"{CurrentDecayValue:0.###}/"
            + $"{MaxDecayValue:0.###}.";
    }

    private static string ResolveDepletedMessage(
        HWJ_BodyDecayData bodyDecay,
        string fallbackMessage)
    {
        if (bodyDecay != null
            && !string.IsNullOrWhiteSpace(
                bodyDecay.depletedMessage))
        {
            return bodyDecay.depletedMessage;
        }

        return fallbackMessage;
    }

    private static float ResolveSkillActionDecayAmount(
        float defaultDecayAmount,
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds,
        bool applyComboBodyDecayMultiplier)
    {
        float resolvedDecayAmount =
            Mathf.Max(
                0f,
                defaultDecayAmount);

        if (skillAction == null)
        {
            return resolvedDecayAmount;
        }

        if (skillAction.UsesCustomBodyDecayAmount)
        {
            resolvedDecayAmount =
                Mathf.Max(
                    0f,
                    skillAction.CustomBodyDecayAmount);
        }

        resolvedDecayAmount +=
            Mathf.Max(
                0f,
                skillAction.AdditionalBodyDecayAmount);

        if (applyComboBodyDecayMultiplier
            && skillAction.UsesComboBodyDecayMultiplier)
        {
            resolvedDecayAmount *=
                Mathf.Max(
                    0f,
                    skillAction.ComboBodyDecayMultiplier);
        }

        float chargeDecayAmount =
            Mathf.Max(
                0f,
                chargeSeconds)
            * Mathf.Max(
                0f,
                skillAction.ChargeBodyDecayPerSecond);

        if (skillAction.MaxChargeBodyDecayAmount > 0f)
        {
            chargeDecayAmount =
                Mathf.Min(
                    chargeDecayAmount,
                    skillAction.MaxChargeBodyDecayAmount);
        }

        return resolvedDecayAmount
            + chargeDecayAmount;
    }

    private HWJ_DecayDangerLevel ResolveDangerLevel()
    {
        if (MaxDecayValue > 0f
            && CurrentDecayValue >= MaxDecayValue)
        {
            return HWJ_DecayDangerLevel.Collapsed;
        }

        if (!TryGetBodyDecayData(
                out HWJ_BodyDecayData bodyDecay)
            || bodyDecay.dangerThresholds == null
            || bodyDecay.dangerThresholds.Length == 0)
        {
            return HWJ_DecayDangerLevel.Stable;
        }

        HWJ_DecayDangerLevel result =
            HWJ_DecayDangerLevel.Stable;

        float currentRatio =
            CurrentDecayRatio;

        for (int i = 0;
             i < bodyDecay.dangerThresholds.Length;
             i++)
        {
            HWJ_DecayDangerThresholdData threshold =
                bodyDecay.dangerThresholds[i];

            if (threshold == null
                || currentRatio < threshold.ratio)
            {
                continue;
            }

            if (threshold.dangerLevel > result)
            {
                result = threshold.dangerLevel;
            }
        }

        return result;
    }

    private void RaiseDecayEvents(
        float previousValue,
        HWJ_DecayDangerLevel previousLevel)
    {
        HWJ_DecayDangerLevel currentLevel =
            CurrentDangerLevel;

        previousDangerLevel =
            currentLevel;

        HWJ_GameplayEvents.RaiseBodyDecayChanged(
            new HWJ_BodyDecayChangedEvent(
                this,
                previousValue,
                currentDecayValue,
                MaxDecayValue,
                currentLevel));

        if (previousLevel != currentLevel)
        {
            HWJ_GameplayEvents.RaiseDecayDangerLevelChanged(
                new HWJ_DecayDangerLevelChangedEvent(
                    this,
                    previousLevel,
                    currentLevel));
        }
    }

    private void SyncDecayFromRuntimeBodyState()
    {
        if (TryGetCurrentPossessedBodyState(
                out HWJ_PossessedBodyRuntimeState bodyState))
        {
            currentDecayValue =
                bodyState.CurrentDecayValue;
        }
    }

    private bool TryGetCurrentPossessedBodyState(
        out HWJ_PossessedBodyRuntimeState bodyState)
    {
        if (possessedBodySystem == null)
        {
            possessedBodySystem =
                GetComponent<HWJ_PossessedBodySystem>();
        }

        bodyState = null;

        return possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(
                out bodyState)
            && bodyState != null;
    }

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager
            && HWJ_GameAccess.Manager.PlayerBodyDecay
            == this)
        {
            HWJ_GameAccess.Manager
                .SavePlayerRuntimeSnapshot();
        }
    }
}