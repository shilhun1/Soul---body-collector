using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HWJ_RuntimeBodyEffectState
{
    [SerializeField] private string effectId;
    [SerializeField] private float remainingSeconds;
    [SerializeField] private int stackCount = 1;

    public string EffectId => effectId;
    public float RemainingSeconds => remainingSeconds;
    public int StackCount => stackCount;

    public HWJ_RuntimeBodyEffectState(string effectId, float remainingSeconds, int stackCount)
    {
        this.effectId = effectId;
        this.remainingSeconds = Mathf.Max(0f, remainingSeconds);
        this.stackCount = Mathf.Max(1, stackCount);
    }

    public void SetRemainingSeconds(float value)
    {
        remainingSeconds = Mathf.Max(0f, value);
    }
}

[Serializable]
public class HWJ_RuntimeCooldownState
{
    [SerializeField] private string actionId;
    [SerializeField] private float remainingSeconds;
    [SerializeField] private float durationSeconds;

    public string ActionId => actionId;
    public float RemainingSeconds => remainingSeconds;
    public float DurationSeconds => durationSeconds;
    public bool IsReady => remainingSeconds <= 0f;

    public HWJ_RuntimeCooldownState(string actionId, float durationSeconds)
    {
        this.actionId = actionId;
        this.durationSeconds = Mathf.Max(0f, durationSeconds);
        remainingSeconds = this.durationSeconds;
    }

    public void SetRemainingSeconds(float value)
    {
        remainingSeconds = Mathf.Clamp(value, 0f, durationSeconds);
    }
}

[Serializable]
public class HWJ_RuntimeStatModifierData
{
    public float maxHp;
    public float moveSpeed;
    public float attackPower;
    public float defense;
    public float attackSpeed;

    public void Clear()
    {
        maxHp = 0f;
        moveSpeed = 0f;
        attackPower = 0f;
        defense = 0f;
        attackSpeed = 0f;
    }
}

[Serializable]
public class HWJ_PossessedBodyRuntimeState
{
    [SerializeField] private string runtimeInstanceId;
    [SerializeField] private string definitionDataId;
    [SerializeField] private int sourceUnityInstanceId;
    [SerializeField] private float currentHp;
    [SerializeField] private float maxHp;
    [SerializeField] private float currentDecayValue;
    [SerializeField] private float maxDecayValue;
    [SerializeField] private bool isPossessed;
    [SerializeField] private bool isCollapsed;
    [SerializeField] private List<HWJ_RuntimeBodyEffectState> activeEffects = new List<HWJ_RuntimeBodyEffectState>();
    [SerializeField] private List<HWJ_RuntimeCooldownState> cooldowns = new List<HWJ_RuntimeCooldownState>();
    [SerializeField] private HWJ_RuntimeStatModifierData temporaryStatModifiers = new HWJ_RuntimeStatModifierData();

    public string RuntimeInstanceId => runtimeInstanceId;
    public string DefinitionDataId => definitionDataId;
    public int SourceUnityInstanceId => sourceUnityInstanceId;
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float CurrentDecayValue => currentDecayValue;
    public float MaxDecayValue => maxDecayValue;
    public float CurrentHpRatio => maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
    public float CurrentDecayRatio => maxDecayValue > 0f ? Mathf.Clamp01(currentDecayValue / maxDecayValue) : 0f;
    public bool IsPossessed => isPossessed;
    public bool IsCollapsed => isCollapsed;
    public IReadOnlyList<HWJ_RuntimeBodyEffectState> ActiveEffects => activeEffects;
    public IReadOnlyList<HWJ_RuntimeCooldownState> Cooldowns => cooldowns;
    public HWJ_RuntimeStatModifierData TemporaryStatModifiers => temporaryStatModifiers;

    public static HWJ_PossessedBodyRuntimeState Create(
        string definitionDataId,
        int sourceUnityInstanceId,
        float maxHp,
        float initialHp,
        float maxDecayValue,
        float initialDecayValue)
    {
        HWJ_PossessedBodyRuntimeState state = new HWJ_PossessedBodyRuntimeState();
        state.Initialize(
            Guid.NewGuid().ToString("N"),
            definitionDataId,
            sourceUnityInstanceId,
            maxHp,
            initialHp,
            maxDecayValue,
            initialDecayValue);
        return state;
    }

    public void Initialize(
        string runtimeInstanceId,
        string definitionDataId,
        int sourceUnityInstanceId,
        float maxHp,
        float initialHp,
        float maxDecayValue,
        float initialDecayValue)
    {
        this.runtimeInstanceId = string.IsNullOrEmpty(runtimeInstanceId)
            ? Guid.NewGuid().ToString("N")
            : runtimeInstanceId;
        this.definitionDataId = definitionDataId;
        this.sourceUnityInstanceId = sourceUnityInstanceId;
        this.maxHp = Mathf.Max(0f, maxHp);
        this.maxDecayValue = Mathf.Max(0f, maxDecayValue);
        currentHp = ClampHp(initialHp);
        currentDecayValue = ClampDecay(initialDecayValue);
        isPossessed = true;
        isCollapsed = false;
        activeEffects.Clear();
        cooldowns.Clear();
        temporaryStatModifiers.Clear();

        if (this.maxDecayValue > 0f && currentDecayValue >= this.maxDecayValue)
        {
            MarkCollapsed();
        }
    }

    public void SetPossessed(bool value)
    {
        isPossessed = value;
    }

    public void MarkCollapsed()
    {
        isCollapsed = true;
        isPossessed = false;
        currentHp = 0f;
        currentDecayValue = maxDecayValue;
    }

    public void SetCurrentHp(float value)
    {
        currentHp = ClampHp(value);

        if (currentHp <= 0f)
        {
            isCollapsed = true;
        }
    }

    public void RestoreHpToMax()
    {
        currentHp = maxHp;
    }

    public void SetCurrentDecayValue(float value)
    {
        currentDecayValue = ClampDecay(value);

        if (maxDecayValue > 0f && currentDecayValue >= maxDecayValue)
        {
            MarkCollapsed();
        }
    }

    public void ResetDecay(float initialDecayValue)
    {
        currentDecayValue = ClampDecay(initialDecayValue);

        if (currentDecayValue < maxDecayValue)
        {
            isCollapsed = false;
        }
    }

    public void ClearTransientState()
    {
        activeEffects.Clear();
        cooldowns.Clear();
        temporaryStatModifiers.Clear();
    }

    private float ClampHp(float value)
    {
        return Mathf.Clamp(value, 0f, maxHp);
    }

    private float ClampDecay(float value)
    {
        return Mathf.Clamp(value, 0f, maxDecayValue);
    }
}
