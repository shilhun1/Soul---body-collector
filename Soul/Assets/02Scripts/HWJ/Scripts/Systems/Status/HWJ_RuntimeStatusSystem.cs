using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어, 적, 보스가 공통으로 사용할 런타임 HP와 상태를 관리합니다.
/// 원본 능력치는 RootObjectDataSO에서 읽고, 구슬/버프 같은 임시 변화는 이 컴포넌트의 보너스로만 관리합니다.
/// </summary>
public class HWJ_RuntimeStatusSystem : MonoBehaviour
{
    [Header("연결 컴포넌트")]
    [Tooltip("현재 오브젝트의 런타임 데이터 접근 창구입니다. 빙의, 저장, 전투 시스템이 같은 런타임 상태를 공유할 때 사용합니다.")]
    [InspectorName("런타임 컨텍스트")]
    [SerializeField] private HWJ_RuntimeObjectContext runtimeContext;
    [Tooltip("RootObjectDataSO를 읽는 컴포넌트입니다. 최대 체력, 공격력, 방어력 같은 원본 데이터를 여기서 가져옵니다.")]
    [InspectorName("데이터 리졸버")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [Tooltip("플레이어가 어떤 육신에 빙의 중인지 확인하고, 빙의한 육신의 스탯을 가져올 때 사용합니다.")]
    [InspectorName("빙의 시스템")]
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [Tooltip("플레이어의 영혼, 육신, 사망 상태를 확인하는 시스템입니다.")]
    [InspectorName("영혼 시스템")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [Tooltip("빙의 유지 정신력의 시간/행동 소모를 확인할 때 사용합니다. 기존 BodyDecay 시스템명을 호환용으로 유지합니다.")]
    [InspectorName("빙의체 정신력 시스템")]
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;
    [Tooltip("현재 빙의한 육신의 런타임 HP와 육신 전용 상태를 관리하는 시스템입니다.")]
    [InspectorName("빙의 육신 시스템")]
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;
    [Tooltip("HP가 0이 되어 육신이 무너질 때 입력 차단과 영혼 복귀를 처리하는 시스템입니다.")]
    [InspectorName("육신 붕괴 시스템")]
    [SerializeField] private HWJ_CollapseSystem collapseSystem;
    [Tooltip("이동, 점프, 대쉬 같은 캐릭터 움직임 잠금 상태를 반영할 때 사용하는 시스템입니다.")]
    [InspectorName("움직임 시스템")]
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [Tooltip("보스 전용 슈퍼아머, 그로기, 페이즈 상태를 반영할 때 사용합니다. 일반 플레이어와 몬스터는 비워둘 수 있습니다.")]
    [InspectorName("보스 두뇌 시스템")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;

    [Header("현재 런타임 상태")]
    [Tooltip("현재 캐릭터 상태입니다. Idle, Move, Attack, Dead 같은 전투/행동 상태 판정에 사용됩니다.")]
    [InspectorName("현재 상태")]
    [SerializeField] private HWJ_RuntimeState currentState = HWJ_RuntimeState.Idle;
    [Tooltip("현재 조작 중인 몸의 HP입니다. 육신 상태에서는 육신 HP, 영혼 상태에서는 영혼 HP와 동기화됩니다.")]
    [InspectorName("현재 HP")]
    [SerializeField] private float currentHp;
    [Tooltip("영혼 상태일 때 유지되는 HP 값입니다. 씬 이동이나 빙의 해제 후 영혼 체력을 유지할 때 사용합니다.")]
    [InspectorName("영혼 HP")]
    [SerializeField] private float soulHp;
    [Tooltip("빙의한 육신의 HP 값입니다. 육신에서 빠져나가거나 씬을 넘어갈 때 현재 육신 상태를 보존하는 데 사용합니다.")]
    [InspectorName("빙의 육신 HP")]
    [SerializeField] private float possessedBodyHp;

    [Header("행동 잠금 시간")]
    [Tooltip("이 시간보다 현재 시간이 작으면 이동할 수 없습니다. 피격, 붕괴 연출, 전환 연출에서 사용합니다.")]
    [InspectorName("이동 잠금 종료 시간")]
    [SerializeField] private float moveLockEndTime;
    [Tooltip("이 시간보다 현재 시간이 작으면 공격할 수 없습니다. 공격 후딜, 피격, 스킬 잠금에서 사용합니다.")]
    [InspectorName("공격 잠금 종료 시간")]
    [SerializeField] private float attackLockEndTime;
    [Tooltip("이 시간보다 현재 시간이 작으면 대쉬할 수 없습니다. 대쉬 쿨타임이나 피격 중 조작 차단에 사용합니다.")]
    [InspectorName("대쉬 잠금 종료 시간")]
    [SerializeField] private float dashLockEndTime;

    [Header("피격/무적 상태")]
    [Tooltip("이 시간보다 현재 시간이 작으면 히트스턴 상태입니다. 히트스턴 중에는 이동, 공격, 대쉬가 차단됩니다.")]
    [InspectorName("히트스턴 종료 시간")]
    [SerializeField] private float hitStunEndTime;
    [Tooltip("이 시간보다 현재 시간이 작으면 임시 무적 상태입니다. 연속 피격 방지에 사용합니다.")]
    [InspectorName("무적 종료 시간")]
    [SerializeField] private float invincibleEndTime;
    [Tooltip("이 시간보다 현재 시간이 작으면 피격 리액션과 넉백을 무시합니다. 슈퍼아머와 별도로 짧은 면역을 줄 때 사용합니다.")]
    [InspectorName("피격 리액션 면역 종료 시간")]
    [SerializeField] private float hitReactionImmuneEndTime;
    [Tooltip("짧은 시간에 너무 많이 맞았을 때 추가 피격 리액션을 제한하는 종료 시간입니다.")]
    [InspectorName("연속 피격 제한 종료 시간")]
    [SerializeField] private float hitReactionLimitEndTime;
    [Tooltip("연속 피격 횟수를 계산하는 시간 창의 종료 시간입니다.")]
    [InspectorName("연속 피격 계산 창 종료 시간")]
    [SerializeField] private float hitReactionWindowEndTime;
    [Tooltip("현재 피격 계산 창 안에서 발생한 피격 리액션 횟수입니다.")]
    [InspectorName("연속 피격 횟수")]
    [SerializeField] private int hitReactionCountInWindow;

    [Header("몸 충돌 필터")]
    [Tooltip("플레이어와 일반/보스 몬스터의 몸 콜라이더끼리 물리 충돌해서 서로 밀리는 것을 막습니다. 트리거 콜라이더는 제외합니다.")]
    [SerializeField] private bool ignorePlayerMonsterBodyCollision = true;
    [Tooltip("일반 몬스터와 보스 몬스터끼리 몸 콜라이더로 서로 밀리는 것을 막습니다. 트리거 콜라이더는 제외합니다.")]
    [SerializeField] private bool ignoreMonsterBodyCollision = true;
    [Tooltip("새로 생성된 몬스터까지 충돌 무시 대상으로 갱신하는 주기입니다.")]
    [SerializeField] private float bodyCollisionRefreshSeconds = 0.25f;

    private readonly Dictionary<int, float> nextDamageTimesBySource = new Dictionary<int, float>();
    private float maxHpBonus;
    private float moveSpeedBonus;
    private float attackPowerBonus;
    private float defenseBonus;
    private float attackSpeedBonus;
    private Collider2D[] bodyCollisionColliders;
    private float nextBodyCollisionRefreshTime;

    public HWJ_RuntimeState CurrentState => currentState;
    public float CurrentHp => currentHp;
    public float SoulHp => soulHp;
    public float PossessedBodyHp => possessedBodyHp;
    public float SoulMaxHp => GetOwnerBaseStatusValue(status => status.maxHp) + maxHpBonus;
    public float MaxHp => GetRuntimeBodyMaxHpOrBase() + maxHpBonus;
    public float CurrentSpiritMentalValue => soulHp;
    public float MaxSpiritMentalValue => SoulMaxHp;
    public float SpiritMentalRatio => MaxSpiritMentalValue > 0f
        ? Mathf.Clamp01(CurrentSpiritMentalValue / MaxSpiritMentalValue)
        : 0f;
    public bool HasSpiritMentalRemaining => MaxSpiritMentalValue <= 0f || CurrentSpiritMentalValue > 0f;
    public float MoveSpeed => GetBaseStatusValue(status => status.moveSpeed) + moveSpeedBonus;
    public float AttackPower => GetBaseStatusValue(status => status.attackPower) + attackPowerBonus;
    public float Defense => GetBaseStatusValue(status => status.defense) + defenseBonus;
    public float AttackSpeed => GetBaseStatusValue(status => status.attackSpeed) + attackSpeedBonus;
    public bool UsesHp => MaxHp > 0f;
    public bool IsHitStunned => Time.time < hitStunEndTime;
    public bool IsTemporarilyInvincible => Time.time < invincibleEndTime;
    public bool IsHitReactionImmune => Time.time < hitReactionImmuneEndTime;
    public bool IsHitReactionLimited => Time.time < hitReactionLimitEndTime;
    public bool HasSuperArmor => HasDataSuperArmor() || (bossBrain != null && bossBrain.HasSuperArmor);
    public bool ShouldIgnoreKnockback => IsBossBody()
        || HasSuperArmor
        || IsHitReactionImmune
        || IsHitReactionLimited
        || ShouldDataIgnoreKnockback();
    public bool CanMove => !IsDead && Time.time >= moveLockEndTime && !IsHitStunned;
    public bool CanAttack => !IsDead && Time.time >= attackLockEndTime && !IsHitStunned;
    public bool CanDash => !IsDead && Time.time >= dashLockEndTime && !IsHitStunned;
    public float KnockbackScale => 1f / Mathf.Max(0.01f, GetKnockbackWeight());
    public bool IsDead => currentState == HWJ_RuntimeState.Dead
        || (soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead)
        || (soulSystem == null && UsesHp && currentHp <= 0f);

    private void Awake()
    {
        ResolveReferences();
        CacheBodyCollisionColliders();
        soulHp = SoulMaxHp;
        possessedBodyHp = MaxHp;
        currentHp = GetStoredHpForActiveState();
    }

    private void Update()
    {
        UpdateBodyCollisionIgnores();
    }

    private void ResolveReferences()
    {
        if (runtimeContext == null)
        {
            runtimeContext = GetComponent<HWJ_RuntimeObjectContext>();
        }

        if (dataResolver == null)
        {
            dataResolver = runtimeContext != null
                ? runtimeContext.DataResolver
                : GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = GetComponent<HWJ_BodyDecaySystem>();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        if (collapseSystem == null)
        {
            collapseSystem = GetComponent<HWJ_CollapseSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }
    }

    /// <summary>
    /// 현재 상태를 변경합니다.
    /// 이동, 공격, AI, UI가 같은 상태 값을 보도록 한 곳에서 관리합니다.
    /// </summary>
    public void SetState(HWJ_RuntimeState state)
    {
        if (currentState == state)
        {
            return;
        }

        HWJ_RuntimeState previousState = currentState;
        currentState = state;
        HWJ_GameplayEvents.RaiseRuntimeStateChanged(
            new HWJ_RuntimeStateChangedEvent(this, previousState, currentState));
    }

    public HWJ_RuntimeStatSnapshot CreateStatSnapshot()
    {
        return HWJ_RuntimeStatSnapshot.FromStatus(this);
    }

    public void LockMovement(float seconds)
    {
        moveLockEndTime = Mathf.Max(moveLockEndTime, Time.time + Mathf.Max(0f, seconds));
    }

    public void LockAttack(float seconds)
    {
        attackLockEndTime = Mathf.Max(attackLockEndTime, Time.time + Mathf.Max(0f, seconds));
    }

    public void LockDash(float seconds)
    {
        dashLockEndTime = Mathf.Max(dashLockEndTime, Time.time + Mathf.Max(0f, seconds));
    }

    public void LockControl(float seconds)
    {
        LockMovement(seconds);
        LockAttack(seconds);
        LockDash(seconds);
    }

    public void BeginTimedAttackAction(
        float totalSeconds,
        float movementLockSeconds,
        float dashCancelStartSeconds,
        bool canDashCancel)
    {
        float total = Mathf.Max(0f, totalSeconds);
        LockAttack(total);
        LockMovement(movementLockSeconds > 0f ? movementLockSeconds : total);
        LockDash(canDashCancel ? Mathf.Max(0f, dashCancelStartSeconds) : total);
    }

    public void CancelAttackAction()
    {
        attackLockEndTime = Mathf.Min(attackLockEndTime, Time.time);
    }

    public void GrantInvincibility(float seconds)
    {
        invincibleEndTime = Mathf.Max(invincibleEndTime, Time.time + Mathf.Max(0f, seconds));
    }

    public void ClearHitReactionLimit()
    {
        hitReactionLimitEndTime = 0f;
        hitReactionWindowEndTime = 0f;
        hitReactionCountInWindow = 0;
    }

    public bool CanReceiveHitFrom(Component source)
    {
        if (IsDead || IsTemporarilyInvincible || IsDataInvincible())
        {
            return false;
        }

        int sourceId = source != null ? source.GetInstanceID() : 0;
        return sourceId == 0
            || !nextDamageTimesBySource.TryGetValue(sourceId, out float nextDamageTime)
            || Time.time >= nextDamageTime;
    }

    /// <summary>
    /// 피해를 적용합니다.
    /// 플레이어는 빙의 상태 HP가 0이면 영혼 상태가 되고, 영혼 상태 HP가 0이면 게임오버 상태가 됩니다.
    /// </summary>
    public void ApplyDamage(float damage)
    {
        ApplyDamage(damage, null, null);
    }

    public void ApplyDamage(float damage, Component source, HWJ_DamageData sourceDamage)
    {
        ResolveReferences();

        if (IsDead || damage <= 0f || !CanReceiveHitFrom(source))
        {
            return;
        }

        bool wasDead = IsDead;
        currentHp = Mathf.Max(0f, currentHp - damage);
        CacheCurrentHpForActiveState();
        ApplyPostHitTimers(source, sourceDamage);

        if (currentHp <= 0f)
        {
            HandleEmptyHp();
        }
        else
        {
            ApplyHitReaction(damage, sourceDamage);
        }

        float remainingHpAfterDamage = currentHp;
        HWJ_GameplayEvents.RaiseDamageApplied(
            new HWJ_DamageEvent(this, source, sourceDamage, damage, remainingHpAfterDamage, !wasDead && IsDead));
        SavePlayerRuntimeSnapshotIfOwner();
    }

    /// <summary>
    /// HP를 회복합니다.
    /// 최대 HP를 넘지 않도록 RuntimeStatusSystem에서 제한합니다.
    /// </summary>
    public void Heal(float amount)
    {
        ResolveReferences();

        if (IsDead)
        {
            return;
        }

        currentHp = Mathf.Min(MaxHp, currentHp + amount);
        CacheCurrentHpForActiveState();
    }

    /// <summary>
    /// 기존 SoulHp 값을 영혼 정신력으로 사용하는 호환 API입니다.
    /// 영혼 상태의 기믹, 빙의 시도, 특수 이동이 정신력을 소모할 때 사용합니다.
    /// </summary>
    public bool TryApplySpiritMentalCost(float mentalCost)
    {
        return TryApplySpiritMentalCost(mentalCost, "spirit_mental_cost");
    }

    public bool TryApplySpiritMentalCost(float mentalCost, string reason)
    {
        ResolveReferences();

        if (mentalCost <= 0f)
        {
            return true;
        }

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Soul)
        {
            return false;
        }

        soulHp = Mathf.Max(0f, soulHp - mentalCost);

        if (soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            currentHp = soulHp;
        }

        if (soulHp <= 0f)
        {
            if (soulSystem != null)
            {
                soulSystem.EnterDeadState();
            }
            else
            {
                SetState(HWJ_RuntimeState.Dead);
            }
        }

        SavePlayerRuntimeSnapshotIfOwner();
        return true;
    }

    /// <summary>
    /// 현재 데이터 기준으로 HP를 다시 맞춥니다.
    /// 빙의 성공으로 육신 스탯을 읽기 시작할 때 최대 HP를 새 몸 기준으로 갱신합니다.
    /// </summary>
    public void RefreshCurrentHpFromData(bool refillToMax)
    {
        ResolveReferences();

        if (soulSystem == null)
        {
            currentHp = refillToMax ? MaxHp : Mathf.Min(currentHp, MaxHp);
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body && possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            if (TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
            {
                if (refillToMax)
                {
                    bodyState.RestoreHpToMax();
                }

                possessedBodyHp = bodyState.CurrentHp;
                currentHp = possessedBodyHp;
                return;
            }

            float bodyMaxHp = MaxHp;
            possessedBodyHp = refillToMax ? bodyMaxHp : Mathf.Min(possessedBodyHp, bodyMaxHp);
            currentHp = possessedBodyHp;
            return;
        }

        float soulMaxHp = SoulMaxHp;
        soulHp = refillToMax ? soulMaxHp : Mathf.Min(soulHp, soulMaxHp);
        currentHp = soulHp;
    }

    public void RestoreHpSnapshot(float restoredCurrentHp, float restoredSoulHp, float restoredPossessedBodyHp)
    {
        ResolveReferences();

        float bodyMaxHp = Mathf.Max(0f, MaxHp);
        float soulMaxHp = Mathf.Max(0f, SoulMaxHp);

        soulHp = soulMaxHp > 0f ? Mathf.Clamp(restoredSoulHp, 0f, soulMaxHp) : 0f;
        possessedBodyHp = bodyMaxHp > 0f ? Mathf.Clamp(restoredPossessedBodyHp, 0f, bodyMaxHp) : 0f;

        if (soulSystem == null)
        {
            currentHp = bodyMaxHp > 0f ? Mathf.Clamp(restoredCurrentHp, 0f, bodyMaxHp) : 0f;
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            currentHp = soulHp;
            return;
        }

        currentHp = bodyMaxHp > 0f ? Mathf.Clamp(restoredCurrentHp, 0f, bodyMaxHp) : 0f;
        possessedBodyHp = currentHp;

        if (TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
        {
            bodyState.SetCurrentHp(possessedBodyHp);
        }
    }

    /// <summary>
    /// 현재 표시 중인 HP를 영혼 HP 또는 빙의 육신 HP 저장값에 반영합니다.
    /// 상태 전환 직전에 호출해서 두 체력 풀을 서로 덮어쓰지 않게 합니다.
    /// </summary>
    public void CacheCurrentHpForActiveState()
    {
        ResolveReferences();

        if (soulSystem == null)
        {
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            possessedBodyHp = currentHp;

            if (TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
            {
                bodyState.SetCurrentHp(currentHp);
            }

            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            soulHp = currentHp;
        }
    }

    /// <summary>
    /// 능력치 구슬 데이터를 런타임 보너스로 적용합니다.
    /// ScriptableObject 원본 스탯은 수정하지 않기 때문에 데이터 오염을 막을 수 있습니다.
    /// </summary>
    public bool CanApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        ResolveReferences();

        if (statOrbData == null)
        {
            return false;
        }

        HWJ_StatOrbProgressSystem statOrbProgressSystem = GetComponent<HWJ_StatOrbProgressSystem>();
        return statOrbProgressSystem == null || statOrbProgressSystem.CanApplyStatOrb(statOrbData);
    }

    public bool TryApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        ResolveReferences();

        if (statOrbData == null)
        {
            return false;
        }

        HWJ_StatOrbProgressSystem statOrbProgressSystem = GetComponent<HWJ_StatOrbProgressSystem>();

        if (statOrbProgressSystem != null)
        {
            return statOrbProgressSystem.TryApplyStatOrb(statOrbData, this, out _);
        }

        ApplyStatOrbBonus(statOrbData, false);
        return true;
    }

    public void ApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        TryApplyStatOrb(statOrbData);
    }

    public void ClearStatOrbBonuses()
    {
        ResolveReferences();

        maxHpBonus = 0f;
        moveSpeedBonus = 0f;
        attackPowerBonus = 0f;
        defenseBonus = 0f;
        attackSpeedBonus = 0f;
        RefreshCurrentHpFromData(false);
    }

    public void ApplyStatOrbBonus(HWJ_StatOrbDataSO statOrbData, bool preserveCurrentHp)
    {
        ResolveReferences();

        if (statOrbData == null)
        {
            return;
        }

        switch (statOrbData.OrbType)
        {
            case HWJ_StatOrbType.MaxHp:
                maxHpBonus += statOrbData.Amount;
                if (preserveCurrentHp)
                {
                    RefreshCurrentHpFromData(false);
                }
                else
                {
                    currentHp = Mathf.Min(MaxHp, currentHp + statOrbData.Amount);
                    CacheCurrentHpForActiveState();
                }
                break;
            case HWJ_StatOrbType.MoveSpeed:
                moveSpeedBonus += statOrbData.Amount;
                break;
            case HWJ_StatOrbType.AttackPower:
                attackPowerBonus += statOrbData.Amount;
                break;
            case HWJ_StatOrbType.Defense:
                defenseBonus += statOrbData.Amount;
                break;
            case HWJ_StatOrbType.AttackSpeed:
                attackSpeedBonus += statOrbData.Amount;
                break;
        }
    }

    private float GetBaseStatusValue(System.Func<HWJ_StatusData, float> selector)
    {
        if (runtimeContext != null)
        {
            HWJ_StatusData effectiveStatus = runtimeContext.GetEffectiveStatusData();
            return effectiveStatus != null ? selector(effectiveStatus) : 0f;
        }

        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return selector(possessedStatus);
        }

        if (dataResolver == null || dataResolver.Status == null)
        {
            return 0f;
        }

        return selector(dataResolver.Status);
    }

    private float GetRuntimeBodyMaxHpOrBase()
    {
        if (TryGetCurrentPossessedBodyState(out HWJ_PossessedBodyRuntimeState bodyState))
        {
            return bodyState.MaxHp;
        }

        return GetBaseStatusValue(status => status.maxHp);
    }

    private HWJ_ReceivedDamageData GetReceivedDamageData()
    {
        if (runtimeContext != null)
        {
            return runtimeContext.GetEffectiveReceivedDamageData();
        }

        if (possessionSystem != null
            && possessionSystem.TryGetPossessedReceivedDamage(out HWJ_ReceivedDamageData possessedReceivedDamage))
        {
            return possessedReceivedDamage;
        }

        return dataResolver != null ? dataResolver.ReceivedDamage : null;
    }

    private bool IsDataInvincible()
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();
        return receivedDamage != null && receivedDamage.isInvincible;
    }

    private bool HasDataSuperArmor()
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage != null && receivedDamage.hasSuperArmor)
        {
            return true;
        }

        return dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.State != null
            && enemyData.State.hasSuperArmor;
    }

    private bool ShouldDataIgnoreKnockback()
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();
        return receivedDamage != null && receivedDamage.ignoreKnockback;
    }

    private bool IsBossBody()
    {
        return dataResolver != null && dataResolver.ObjectType == HWJ_ObjectType.Boss;
    }

    private bool ShouldUseBodyCollisionFilter()
    {
        if (dataResolver == null)
        {
            return false;
        }

        return dataResolver.ObjectType == HWJ_ObjectType.Player
            || dataResolver.ObjectType == HWJ_ObjectType.Enemy
            || dataResolver.ObjectType == HWJ_ObjectType.Boss;
    }

    private void CacheBodyCollisionColliders()
    {
        bodyCollisionColliders = GetComponentsInChildren<Collider2D>();
    }

    private void UpdateBodyCollisionIgnores()
    {
        if (Time.time < nextBodyCollisionRefreshTime)
        {
            return;
        }

        nextBodyCollisionRefreshTime = Time.time + Mathf.Max(0.02f, bodyCollisionRefreshSeconds);

        if (!ShouldUseBodyCollisionFilter())
        {
            return;
        }

        if (bodyCollisionColliders == null || bodyCollisionColliders.Length == 0)
        {
            CacheBodyCollisionColliders();
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver otherResolver = resolvers[i];

            if (otherResolver == null
                || otherResolver == dataResolver
                || !ShouldIgnoreBodyCollisionWith(otherResolver))
            {
                continue;
            }

            IgnoreBodyCollisionWith(otherResolver);
        }
    }

    private bool ShouldIgnoreBodyCollisionWith(HWJ_RootObjectDataResolver otherResolver)
    {
        if (dataResolver == null || otherResolver == null)
        {
            return false;
        }

        HWJ_ObjectType selfType = dataResolver.ObjectType;
        HWJ_ObjectType otherType = otherResolver.ObjectType;

        if (selfType == HWJ_ObjectType.Player)
        {
            return ignorePlayerMonsterBodyCollision && IsMonsterType(otherType);
        }

        if (IsMonsterType(selfType))
        {
            return otherType == HWJ_ObjectType.Player && ignorePlayerMonsterBodyCollision
                || IsMonsterType(otherType) && ignoreMonsterBodyCollision;
        }

        return false;
    }

    private void IgnoreBodyCollisionWith(HWJ_RootObjectDataResolver otherResolver)
    {
        if (bodyCollisionColliders == null)
        {
            return;
        }

        Collider2D[] otherColliders = otherResolver.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < bodyCollisionColliders.Length; i++)
        {
            Collider2D ownedCollider = bodyCollisionColliders[i];

            if (!IsPhysicalBodyCollider(ownedCollider))
            {
                continue;
            }

            for (int j = 0; j < otherColliders.Length; j++)
            {
                Collider2D otherCollider = otherColliders[j];

                if (!IsPhysicalBodyCollider(otherCollider) || otherCollider == ownedCollider)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(ownedCollider, otherCollider, true);
            }
        }
    }

    private static bool IsPhysicalBodyCollider(Collider2D collider)
    {
        return collider != null && !collider.isTrigger;
    }

    private static bool IsMonsterType(HWJ_ObjectType objectType)
    {
        return objectType == HWJ_ObjectType.Enemy || objectType == HWJ_ObjectType.Boss;
    }

    private float GetKnockbackWeight()
    {
        HWJ_StatusData status = GetStatusDataForWeight();
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();
        float bodyWeight = status != null && status.bodyWeight > 0f ? status.bodyWeight : 1f;
        float reactionWeight = receivedDamage != null && receivedDamage.knockbackWeightMultiplier > 0f
            ? receivedDamage.knockbackWeightMultiplier
            : 1f;
        return bodyWeight * reactionWeight;
    }

    private HWJ_StatusData GetStatusDataForWeight()
    {
        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return possessedStatus;
        }

        return dataResolver != null ? dataResolver.Status : null;
    }

    private float GetHitStunSeconds(HWJ_DamageData sourceDamage)
    {
        if (IsHitReactionImmune)
        {
            return 0f;
        }

        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage != null && receivedDamage.ignoreHitStun)
        {
            return 0f;
        }

        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            && enemyData.State != null
            && enemyData.State.immuneToHitStun)
        {
            return 0f;
        }

        if (sourceDamage != null && sourceDamage.hitStunSeconds > 0f)
        {
            return sourceDamage.hitStunSeconds;
        }

        return receivedDamage != null ? Mathf.Max(0f, receivedDamage.hitStunSeconds) : 0f;
    }

    private void ApplyPostHitTimers(Component source, HWJ_DamageData sourceDamage)
    {
        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (receivedDamage != null)
        {
            GrantInvincibility(receivedDamage.invincibleSecondsAfterHit);
            hitReactionImmuneEndTime = Mathf.Max(
                hitReactionImmuneEndTime,
                Time.time + Mathf.Max(0f, receivedDamage.hitReactionImmuneSeconds));
        }

        int sourceId = source != null ? source.GetInstanceID() : 0;

        if (sourceId == 0)
        {
            return;
        }

        float cooldownSeconds = sourceDamage != null
            ? Mathf.Max(0f, sourceDamage.sameTargetHitCooldownSeconds)
            : 0f;

        if (cooldownSeconds > 0f)
        {
            nextDamageTimesBySource[sourceId] = Time.time + cooldownSeconds;
        }
    }

    private float GetOwnerBaseStatusValue(System.Func<HWJ_StatusData, float> selector)
    {
        if (dataResolver == null || dataResolver.Status == null)
        {
            return 0f;
        }

        return selector(dataResolver.Status);
    }

    private float GetStoredHpForActiveState()
    {
        if (soulSystem == null)
        {
            return MaxHp;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            return possessedBodyHp;
        }

        return soulHp;
    }

    private void ApplyHitReaction(float damage, HWJ_DamageData sourceDamage)
    {
        bool reactionBlockedBySuperArmor = HasSuperArmor;
        bool reactionBlockedByLimit = IsHitReactionLimited;
        bossBrain?.NotifyDamageTaken(damage, reactionBlockedBySuperArmor, reactionBlockedByLimit);

        if (bossBrain != null && bossBrain.IsGroggy)
        {
            return;
        }

        if (reactionBlockedBySuperArmor)
        {
            return;
        }

        HWJ_ReceivedDamageData receivedDamage = GetReceivedDamageData();

        if (!TryConsumeHitReactionSlot(receivedDamage))
        {
            return;
        }

        float hitStunSeconds = GetHitStunSeconds(sourceDamage);

        if (hitStunSeconds > 0f)
        {
            hitStunEndTime = Mathf.Max(hitStunEndTime, Time.time + hitStunSeconds);
            LockControl(hitStunSeconds);
        }

        SetState(HWJ_RuntimeState.Hit);
        motionSystem?.PlayHit();
    }

    private bool TryConsumeHitReactionSlot(HWJ_ReceivedDamageData receivedDamage)
    {
        if (receivedDamage == null || receivedDamage.maxHitReactionsPerWindow <= 0)
        {
            return true;
        }

        if (IsHitReactionLimited)
        {
            return false;
        }

        float windowSeconds = Mathf.Max(0.01f, receivedDamage.hitReactionWindowSeconds);

        if (Time.time >= hitReactionWindowEndTime)
        {
            hitReactionWindowEndTime = Time.time + windowSeconds;
            hitReactionCountInWindow = 0;
        }

        if (hitReactionCountInWindow >= receivedDamage.maxHitReactionsPerWindow)
        {
            float immuneSeconds = Mathf.Max(0f, receivedDamage.hitReactionLimitImmuneSeconds);

            if (immuneSeconds > 0f)
            {
                hitReactionLimitEndTime = Mathf.Max(hitReactionLimitEndTime, Time.time + immuneSeconds);
            }

            return false;
        }

        hitReactionCountInWindow++;
        return true;
    }

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerStatus == this)
        {
            HWJ_GameAccess.Manager.SavePlayerRuntimeSnapshot();
        }
    }

    private void HandleEmptyHp()
    {
        if (soulSystem == null)
        {
            SetState(HWJ_RuntimeState.Dead);
            motionSystem?.PlayDead();
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body)
        {
            if (collapseSystem == null)
            {
                collapseSystem = GetComponent<HWJ_CollapseSystem>();
            }

            if (collapseSystem != null)
            {
                collapseSystem.TryCollapseCurrentBody(HWJ_BodyCollapseReason.HpDepleted);
                return;
            }

            possessedBodySystem?.MarkCurrentBodyCollapsed();
            soulSystem.EnterSoulState(false, HWJ_PossessedBodyExitReason.HpDepleted);
            return;
        }

        if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            soulSystem.EnterDeadState();
            return;
        }

        SetState(HWJ_RuntimeState.Dead);
        motionSystem?.PlayDead();
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
}
