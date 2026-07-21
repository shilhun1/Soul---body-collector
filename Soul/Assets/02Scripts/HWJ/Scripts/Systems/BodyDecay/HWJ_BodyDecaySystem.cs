using UnityEngine;

[RequireComponent(typeof(HWJ_RootObjectDataResolver))]
[RequireComponent(typeof(HWJ_SoulSystem))]
public class HWJ_BodyDecaySystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CollapseSystem collapseSystem;
    [SerializeField] private float currentDecayValue;
    [SerializeField] private bool resetDecayWhenEnterBody = true;
    [SerializeField] private bool isDecaying;
    [SerializeField] private string runtimeStateMessage;

    private float decayTimer;
    private bool hasInitializedDecay;
    private HWJ_SoulRuntimeState previousSoulState;
    private HWJ_RootObjectDataResolver previousPossessedBodyResolver;
    private HWJ_DecayDangerLevel previousDangerLevel = HWJ_DecayDangerLevel.Stable;

    public float CurrentDecayValue => TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState)
        ? bodyState.CurrentDecayValue
        : currentDecayValue;
    public float MaxDecayValue => TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState)
        ? bodyState.MaxDecayValue
        : TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay)
        ? Mathf.Max(0f, bodyDecay.maxDecayValue)
        : 0f;
    public float CurrentDecayRatio => MaxDecayValue > 0f
        ? Mathf.Clamp01(CurrentDecayValue / MaxDecayValue)
        : 0f;
    public float RemainingDecayValue => Mathf.Max(0f, MaxDecayValue - CurrentDecayValue);
    public float RemainingDecayRatio => MaxDecayValue > 0f
        ? Mathf.Clamp01(RemainingDecayValue / MaxDecayValue)
        : 0f;
    public bool HasDecayRemaining => MaxDecayValue <= 0f || CurrentDecayValue < MaxDecayValue;
    public bool IsDecaying => isDecaying;
    public HWJ_DecayDangerLevel CurrentDangerLevel => ResolveDangerLevel();
    public string RuntimeStateMessage => runtimeStateMessage;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();

        if (TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            SetDecayToInitial(bodyDecay);
        }

        previousSoulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
    }

    private void Update()
    {
        ResolveReferences();

        if (soulSystem == null)
        {
            StopDecay("Missing HWJ_SoulSystem.");
            return;
        }

        HWJ_SoulRuntimeState currentSoulState = soulSystem.CurrentState;
        HWJ_RootObjectDataResolver currentPossessedBodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : null;

        SyncDecayFromRuntimeBodyState();

        if (currentSoulState == HWJ_SoulRuntimeState.Body
            && (previousSoulState != HWJ_SoulRuntimeState.Body
                || previousPossessedBodyResolver != currentPossessedBodyResolver)
            && currentPossessedBodyResolver != null
            && resetDecayWhenEnterBody)
        {
            ResetDecay();
        }

        previousSoulState = currentSoulState;
        previousPossessedBodyResolver = currentPossessedBodyResolver;

        if (currentSoulState != HWJ_SoulRuntimeState.Body)
        {
            StopDecay($"Soul state is {currentSoulState}.");
            return;
        }

        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            StopDecay("Body decay waits for an active possessed corpse.");
            return;
        }

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            StopDecay("Missing player BodyDecay data.");
            return;
        }

        if (!bodyDecay.startDecayOnEnterBody)
        {
            StopDecay("Body decay is disabled on the PlayerTypeData asset.");
            return;
        }

        if (!hasInitializedDecay)
        {
            SetDecayToInitial(bodyDecay);
        }

        isDecaying = true;
        runtimeStateMessage = "Body decay is running.";

        if (MaxDecayValue > 0f && currentDecayValue >= MaxDecayValue)
        {
            TryEnterSoulStateWhenDecayMaxed(bodyDecay);
            return;
        }

        float tickSeconds = Mathf.Max(0.01f, bodyDecay.decayTickSeconds);
        decayTimer += Time.deltaTime;

        if (decayTimer < tickSeconds)
        {
            return;
        }

        decayTimer = 0f;
        ApplyDecayAmount(bodyDecay.decayAmountPerTick, "time_tick", bodyDecay);
        TryEnterSoulStateWhenDecayMaxed(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ResetDecay()
    {
        ResolveReferences();

        if (TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            SetDecayToInitial(bodyDecay);
        }
    }

    public void RestoreDecaySnapshot(float restoredDecayValue)
    {
        ResolveReferences();

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        SetCurrentDecayValue(restoredDecayValue, bodyDecay);
        decayTimer = 0f;
        hasInitializedDecay = true;
        previousDangerLevel = CurrentDangerLevel;
        previousSoulState = soulSystem != null ? soulSystem.CurrentState : previousSoulState;
        previousPossessedBodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : previousPossessedBodyResolver;
    }

    public void ApplyHitDecayPenalty()
    {
        ApplyHitDecayPenalty(0f);
    }

    public void ApplyHitDecayPenalty(float incomingDamage)
    {
        ResolveReferences();

        if (soulSystem == null
            || soulSystem.CurrentState != HWJ_SoulRuntimeState.Body
            || possessionSystem == null
            || !possessionSystem.HasActivePossessedBody)
        {
            return;
        }

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        if (!hasInitializedDecay)
        {
            SetDecayToInitial(bodyDecay);
        }

        SyncDecayFromRuntimeBodyState();

        float decayPenalty = Mathf.Max(bodyDecay.hitDecayPenalty, incomingDamage);
        ApplyDecayAmount(decayPenalty, "hit", bodyDecay);
        TryEnterSoulStateWhenDecayMaxed(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplyMoveDecay(float deltaSeconds)
    {
        if (!TryBeginActionDecay(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(bodyDecay.moveDecayPerSecond * Mathf.Max(0f, deltaSeconds), "move", bodyDecay);
        TryEnterSoulStateWhenDecayMaxed(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplyBasicAttackDecay()
    {
        ApplyBasicAttackDecay(null);
    }

    public void ApplyBasicAttackDecay(HWJ_SkillActionDataSO skillAction)
    {
        ApplyBasicAttackDecay(skillAction, 0f);
    }

    public void ApplyBasicAttackDecay(HWJ_SkillActionDataSO skillAction, float chargeSeconds)
    {
        ApplyBasicAttackDecay(skillAction, chargeSeconds, true);
    }

    public void ApplyBasicAttackDecay(HWJ_SkillActionDataSO skillAction, float chargeSeconds, int comboStep)
    {
        ApplyBasicAttackDecay(skillAction, chargeSeconds, Mathf.Max(1, comboStep) > 1);
    }

    private void ApplyBasicAttackDecay(HWJ_SkillActionDataSO skillAction, float chargeSeconds, bool applyComboBodyDecayMultiplier)
    {
        if (!TryBeginActionDecay(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(ResolveSkillActionDecayAmount(bodyDecay.basicAttackDecayAmount, skillAction, chargeSeconds, applyComboBodyDecayMultiplier), "basic_attack", bodyDecay);
        TryEnterSoulStateWhenDecayMaxed(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplySkillDecay()
    {
        ApplySkillDecay(null);
    }

    public void ApplySkillDecay(HWJ_SkillActionDataSO skillAction)
    {
        ApplySkillDecay(skillAction, 0f);
    }

    public void ApplySkillDecay(HWJ_SkillActionDataSO skillAction, float chargeSeconds)
    {
        ApplySkillDecay(skillAction, chargeSeconds, true);
    }

    public void ApplySkillDecay(HWJ_SkillActionDataSO skillAction, float chargeSeconds, int comboStep)
    {
        ApplySkillDecay(skillAction, chargeSeconds, Mathf.Max(1, comboStep) > 1);
    }

    private void ApplySkillDecay(HWJ_SkillActionDataSO skillAction, float chargeSeconds, bool applyComboBodyDecayMultiplier)
    {
        if (!TryBeginActionDecay(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(ResolveSkillActionDecayAmount(bodyDecay.skillDecayAmount, skillAction, chargeSeconds, applyComboBodyDecayMultiplier), "skill", bodyDecay);
        TryEnterSoulStateWhenDecayMaxed(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ApplyActionDecay(float rawDecayAmount)
    {
        if (!TryBeginActionDecay(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        ApplyDecayAmount(rawDecayAmount + bodyDecay.actionDecayAmount, "action", bodyDecay);
        TryEnterSoulStateWhenDecayMaxed(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    private void ResolveReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (collapseSystem == null)
        {
            collapseSystem = GetComponent<HWJ_CollapseSystem>();
        }
    }

    private bool TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay)
    {
        bodyDecay = null;

        if (possessionSystem != null
            && possessionSystem.TryGetActivePossessedBodyDecayData(out bodyDecay))
        {
            return true;
        }

        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        bodyDecay = playerData.BodyDecay;
        return bodyDecay != null;
    }

    private void SetDecayToInitial(HWJ_BodyDecayData bodyDecay)
    {
        SetCurrentDecayValue(bodyDecay.initialDecayValue, bodyDecay);
        decayTimer = 0f;
        hasInitializedDecay = true;
        previousDangerLevel = CurrentDangerLevel;
    }

    private void StopDecay(string message)
    {
        isDecaying = false;
        decayTimer = 0f;
        runtimeStateMessage = message;
    }

    private void TryEnterSoulStateWhenDecayMaxed(HWJ_BodyDecayData bodyDecay)
    {
        if (MaxDecayValue <= 0f || currentDecayValue < MaxDecayValue)
        {
            return;
        }

        currentDecayValue = MaxDecayValue;
        possessedBodySystem?.SetCurrentDecayValue(currentDecayValue);
        isDecaying = false;

        if (!bodyDecay.enterSoulStateWhenMaxed && !bodyDecay.enterSoulStateWhenEmpty)
        {
            possessedBodySystem?.MarkCurrentBodyCollapsed();
            runtimeStateMessage = "Body decay reached max, but soul transition is disabled.";
            return;
        }

        if (collapseSystem != null)
        {
            HWJ_BodyCollapseResult collapseResult = collapseSystem.TryCollapseCurrentBody(HWJ_BodyCollapseReason.DecayMaxed);
            runtimeStateMessage = collapseResult.Message;
            return;
        }

        possessedBodySystem?.MarkCurrentBodyCollapsed();

        if (soulSystem == null)
        {
            runtimeStateMessage = "Body decay reached max, but HWJ_SoulSystem is missing.";
            return;
        }

        runtimeStateMessage = "Body decay reached max. Entering soul state.";
        soulSystem.EnterSoulState();
    }

    private void SetCurrentDecayValue(float value, HWJ_BodyDecayData bodyDecay)
    {
        float maxDecayValue = bodyDecay != null ? Mathf.Max(0f, bodyDecay.maxDecayValue) : MaxDecayValue;
        float previousValue = currentDecayValue;
        HWJ_DecayDangerLevel previousLevel = previousDangerLevel;
        currentDecayValue = Mathf.Clamp(value, 0f, maxDecayValue);

        if (possessedBodySystem != null && possessedBodySystem.TryGetCurrentBodyState(out _))
        {
            possessedBodySystem.SetCurrentDecayValue(currentDecayValue);
        }

        RaiseDecayEvents(previousValue, previousLevel);
    }

    private bool TryBeginActionDecay(out HWJ_BodyDecayData bodyDecay)
    {
        ResolveReferences();
        bodyDecay = null;

        if (soulSystem == null
            || soulSystem.CurrentState != HWJ_SoulRuntimeState.Body
            || possessionSystem == null
            || !possessionSystem.HasActivePossessedBody)
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

    private void ApplyDecayAmount(float rawAmount, string reason, HWJ_BodyDecayData bodyDecay)
    {
        if (rawAmount <= 0f)
        {
            return;
        }

        float resistanceMultiplier = 1f - Mathf.Clamp01(bodyDecay != null ? bodyDecay.decayResistance : 0f);
        float finalAmount = Mathf.Max(0f, rawAmount * resistanceMultiplier);

        if (finalAmount <= 0f)
        {
            return;
        }

        float previousDecayValue = currentDecayValue;
        SetCurrentDecayValue(currentDecayValue + finalAmount, bodyDecay);
        runtimeStateMessage = $"Body decay increased by {finalAmount:0.###}. Reason: {reason}.";
        ApplyPossessedBodyHpLossForDecay(currentDecayValue - previousDecayValue, bodyDecay);
    }

    private void ApplyPossessedBodyHpLossForDecay(float appliedDecayAmount, HWJ_BodyDecayData bodyDecay)
    {
        if (appliedDecayAmount <= 0f || !TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
        {
            return;
        }

        float maxDecayValue = bodyDecay != null
            ? Mathf.Max(0f, bodyDecay.maxDecayValue)
            : bodyState.MaxDecayValue;

        if (maxDecayValue <= 0f)
        {
            return;
        }

        // Possessed body HP represents remaining usable body time, so decay progress must lower HP instead of healing it.
        float hpLoss = bodyState.MaxHp * Mathf.Clamp01(appliedDecayAmount / maxDecayValue);
        possessedBodySystem.SetCurrentHp(bodyState.CurrentHp - hpLoss);
        runtimeStatus?.RefreshCurrentHpFromData(false);
        TryEnterSoulStateWhenPossessedBodyHpEmpty();
    }

    private void TryEnterSoulStateWhenPossessedBodyHpEmpty()
    {
        if (!TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState)
            || bodyState.CurrentHp > 0f)
        {
            return;
        }

        isDecaying = false;

        if (collapseSystem != null)
        {
            HWJ_BodyCollapseResult collapseResult = collapseSystem.TryCollapseCurrentBody(HWJ_BodyCollapseReason.HpDepleted);
            runtimeStateMessage = collapseResult.Message;
            return;
        }

        possessedBodySystem?.MarkCurrentBodyCollapsed();

        if (soulSystem == null)
        {
            runtimeStateMessage = "Possessed body HP reached 0, but HWJ_SoulSystem is missing.";
            return;
        }

        runtimeStateMessage = "Possessed body HP reached 0. Entering soul state.";
        soulSystem.EnterSoulState();
    }

    private static float ResolveSkillActionDecayAmount(
        float defaultDecayAmount,
        HWJ_SkillActionDataSO skillAction,
        float chargeSeconds,
        bool applyComboBodyDecayMultiplier)
    {
        float resolvedDecayAmount = Mathf.Max(0f, defaultDecayAmount);

        if (skillAction == null)
        {
            return resolvedDecayAmount;
        }

        // SkillAction data can either replace the slot default or add extra decay for heavier moves.
        if (skillAction.UsesCustomBodyDecayAmount)
        {
            resolvedDecayAmount = Mathf.Max(0f, skillAction.CustomBodyDecayAmount);
        }

        resolvedDecayAmount += Mathf.Max(0f, skillAction.AdditionalBodyDecayAmount);

        if (applyComboBodyDecayMultiplier && skillAction.UsesComboBodyDecayMultiplier)
        {
            resolvedDecayAmount *= Mathf.Max(0f, skillAction.ComboBodyDecayMultiplier);
        }

        float chargeDecayAmount = Mathf.Max(0f, chargeSeconds) * Mathf.Max(0f, skillAction.ChargeBodyDecayPerSecond);

        if (skillAction.MaxChargeBodyDecayAmount > 0f)
        {
            chargeDecayAmount = Mathf.Min(chargeDecayAmount, skillAction.MaxChargeBodyDecayAmount);
        }

        return resolvedDecayAmount + chargeDecayAmount;
    }

    private HWJ_DecayDangerLevel ResolveDangerLevel()
    {
        if (MaxDecayValue > 0f && CurrentDecayValue >= MaxDecayValue)
        {
            return HWJ_DecayDangerLevel.Collapsed;
        }

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay)
            || bodyDecay.dangerThresholds == null
            || bodyDecay.dangerThresholds.Length == 0)
        {
            return HWJ_DecayDangerLevel.Stable;
        }

        HWJ_DecayDangerLevel result = HWJ_DecayDangerLevel.Stable;
        float currentRatio = CurrentDecayRatio;

        for (int i = 0; i < bodyDecay.dangerThresholds.Length; i++)
        {
            HWJ_DecayDangerThresholdData threshold = bodyDecay.dangerThresholds[i];

            if (threshold == null || currentRatio < threshold.ratio)
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

    private void RaiseDecayEvents(float previousValue, HWJ_DecayDangerLevel previousLevel)
    {
        HWJ_DecayDangerLevel currentLevel = CurrentDangerLevel;
        previousDangerLevel = currentLevel;

        HWJ_GameplayEvents.RaiseBodyDecayChanged(
            new HWJ_BodyDecayChangedEvent(this, previousValue, currentDecayValue, MaxDecayValue, currentLevel));

        if (previousLevel != currentLevel)
        {
            HWJ_GameplayEvents.RaiseDecayDangerLevelChanged(
                new HWJ_DecayDangerLevelChangedEvent(this, previousLevel, currentLevel));
        }
    }

    private void SyncDecayFromRuntimeBodyState()
    {
        if (TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
        {
            currentDecayValue = bodyState.CurrentDecayValue;
        }
    }

    private bool TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState)
    {
        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        bodyState = null;
        return possessedBodySystem != null
            && possessedBodySystem.TryGetCurrentBodyState(out bodyState)
            && bodyState != null;
    }

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerBodyDecay == this)
        {
            HWJ_GameAccess.Manager.SavePlayerRuntimeSnapshot();
        }
    }
}
