using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 기본 공격 입력과 공격 쿨타임을 관리하는 기본 시스템입니다.
/// 실제 판정 생성은 SkillActionSystem에 넘기고, 이 시스템은 PlayerTypeDataSO.Attack의 입력/간격 데이터만 담당합니다.
/// </summary>
public class HWJ_PlayerAttackSystem : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_CombatExecutionSystem combatExecutionSystem;
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;

    [Space(8f)]
    [Header("Attack Tuning")]
    [SerializeField] private LayerMask attackTargetLayer;
    [SerializeField] private float minimumAttackRange = 2f;
    [SerializeField] private int possessedSkillSlotCount = 3;

    [Space(8f)]
    [Header("Charge")]
    [SerializeField] private bool useChargeReleaseInput;
    [SerializeField] private float maxBasicAttackChargeSeconds = 2f;

    [Space(8f)]
    [Header("Rules")]
    [SerializeField] private bool useGameplayAttackRule = true;
    [SerializeField] private HWJ_RuleExecutionCoreSO possessedBodyAttackExecutionCore;
    [SerializeField] private string possessedBodyAttackExecutionCoreId = "possessed_body_attack_execution";
    [SerializeField] private HWJ_GameplayRuleSO possessedBodyAttackRule;
    [SerializeField] private string possessedBodyAttackRuleId = "possessed_body_can_attack";

    [Space(8f)]
    [Header("Debug")]
    [SerializeField] private string lastAttackResult;
    [SerializeField] private float lastDamageApplied;

    private float nextAttackTime;
    private Coroutine basicAttackRoutine;
    private int currentBasicAttackComboStep;
    private float lastBasicAttackComboTime = float.NegativeInfinity;
    private float lastResolvedAttackChargeSeconds;
    private bool isChargingBasicAttack;
    private float basicAttackChargeStartTime;

    public string LastAttackResult => lastAttackResult;
    public float LastDamageApplied => lastDamageApplied;
    public int CurrentBasicAttackComboStep => currentBasicAttackComboStep;
    public float LastResolvedAttackChargeSeconds => lastResolvedAttackChargeSeconds;
    public bool IsChargingBasicAttack => isChargingBasicAttack;
    public float CurrentBasicAttackChargeSeconds => ResolveCurrentBasicAttackChargeSeconds();
    public float MaxBasicAttackChargeSeconds => Mathf.Max(0f, maxBasicAttackChargeSeconds);
    public float CurrentBasicAttackChargeRatio => ResolveCurrentBasicAttackChargeRatio();

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

            if (skillActionSystem == null)
            {
                skillActionSystem = gameObject.AddComponent<HWJ_SkillActionSystem>();
            }
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (playerInput == null)
        {
            ResolvePlayerInput();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (combatExecutionSystem == null)
        {
            combatExecutionSystem = GetComponent<HWJ_CombatExecutionSystem>();
        }

        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = GetComponent<HWJ_BodyDecaySystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }
    }

    private void Update()
    {
        ResolvePlayerInput();

        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            lastAttackResult = "Basic attack charge cancelled: control is locked.";
            CancelBasicAttackCharge(lastAttackResult);
            return;
        }

        if (isChargingBasicAttack && !CanContinueBasicAttackCharge())
        {
            CancelBasicAttackCharge();
            return;
        }

        if (playerInput != null)
        {
            if (useChargeReleaseInput)
            {
                HandleBasicAttackChargeInput();
            }
            else if (playerInput.AttackPressedThisFrame)
            {
                TryBasicAttack();
            }
        }

        TryPossessedSkillSlotInputs();
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null)
        {
            return;
        }

        playerInput = GetComponent<HWJ_PlayerInputSystem>();

        if (playerInput == null)
        {
            playerInput = HWJ_GameAccess.PlayerInput;
        }
    }

    private void HandleBasicAttackChargeInput()
    {
        if (playerInput.AttackPressedThisFrame)
        {
            BeginBasicAttackCharge();
        }

        if (!isChargingBasicAttack)
        {
            return;
        }

        if (!CanContinueBasicAttackCharge())
        {
            CancelBasicAttackCharge();
            return;
        }

        if (playerInput.AttackReleasedThisFrame || !playerInput.AttackHeld)
        {
            ReleaseBasicAttackCharge();
        }
    }

    /// <summary>
    /// 플레이어 기본 공격을 시도합니다.
    /// basicAttackSkillActionId가 있으면 SkillActionSystem으로 실행하고, 없으면 쿨타임과 상태만 갱신합니다.
    /// </summary>
    public bool TryBasicAttack()
    {
        return TryBasicAttack(0f);
    }

    public bool TryBasicAttack(float chargeSeconds)
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            lastAttackResult = "Missing player type data.";
            return false;
        }

        if (Time.time < nextAttackTime)
        {
            lastAttackResult = "Attack is on cooldown.";
            return false;
        }

        if (runtimeStatus != null && !runtimeStatus.CanAttack)
        {
            lastAttackResult = "Attack is locked.";
            return false;
        }

        if (!CanAttack(playerData))
        {
            lastAttackResult = "Attack requires a possessed body.";
            return false;
        }

        if (!IsPossessedBodyAttackRuleSatisfied())
        {
            return false;
        }

        if (playerData.Attack == null)
        {
            lastAttackResult = "Missing attack data.";
            return false;
        }

        string skillActionId = GetBasicAttackSkillActionId(playerData);
        float resolvedChargeSeconds = Mathf.Max(0f, chargeSeconds);
        int resolvedComboStep = ResolveNextBasicAttackComboStep(playerData.Attack);

        if (skillActionSystem != null && !string.IsNullOrEmpty(skillActionId))
        {
            if (!skillActionSystem.TryUseSkill(skillActionId))
            {
                lastAttackResult = skillActionSystem.LastSkillResult;
                return false;
            }

            nextAttackTime = Time.time + playerData.Attack.attackIntervalSeconds;
            runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
            lastDamageApplied = skillActionSystem.LastDamageApplied;
            lastAttackResult = skillActionSystem.LastSkillResult;
            RegisterBasicAttackComboUse(resolvedComboStep, resolvedChargeSeconds);
            ApplyBasicAttackDecayAndNotify(skillActionId, resolvedChargeSeconds, resolvedComboStep);
            return true;
        }

        if (!ApplyBasicAttackDamage(playerData))
        {
            return false;
        }

        nextAttackTime = Time.time + playerData.Attack.attackIntervalSeconds;
        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
        RegisterBasicAttackComboUse(resolvedComboStep, resolvedChargeSeconds);
        ApplyBasicAttackDecayAndNotify("basic_attack", resolvedChargeSeconds, resolvedComboStep);
        return true;
    }

    public bool BeginBasicAttackCharge()
    {
        if (isChargingBasicAttack)
        {
            return true;
        }

        if (!CanBeginBasicAttackCharge())
        {
            return false;
        }

            isChargingBasicAttack = true;
            basicAttackChargeStartTime = Time.time;
            lastResolvedAttackChargeSeconds = 0f;
            lastAttackResult = "Basic attack charge started.";
            RaiseBasicAttackChargeChanged(true, 0f, lastAttackResult);
            return true;
        }

    public bool ReleaseBasicAttackCharge()
    {
        if (!isChargingBasicAttack)
        {
            lastAttackResult = "Basic attack charge was not started.";
            return false;
        }

        float chargeSeconds = ResolveCurrentBasicAttackChargeSeconds();
        isChargingBasicAttack = false;
        basicAttackChargeStartTime = 0f;
        RaiseBasicAttackChargeChanged(false, chargeSeconds, "Basic attack charge released.");
        return TryBasicAttack(chargeSeconds);
    }

    private bool CanBeginBasicAttackCharge()
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            lastAttackResult = "Missing player type data.";
            return false;
        }

        if (Time.time < nextAttackTime)
        {
            lastAttackResult = "Attack is on cooldown.";
            return false;
        }

        if (runtimeStatus != null && !runtimeStatus.CanAttack)
        {
            lastAttackResult = "Attack is locked.";
            return false;
        }

        if (!CanAttack(playerData))
        {
            lastAttackResult = "Charge requires a possessed body.";
            return false;
        }

        if (!IsPossessedBodyAttackRuleSatisfied())
        {
            return false;
        }

        if (playerData.Attack == null)
        {
            lastAttackResult = "Missing attack data.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 빙의 상태에서 숫자키 1/2/3으로 몬스터의 스킬 목록을 직접 실행합니다.
    /// 스킬 자체 쿨타임은 SkillActionSystem이 관리하므로 여기서는 입력과 상태 조건만 확인합니다.
    /// </summary>
    public bool TryPossessedSkillSlot(int slotIndex)
    {
        return TryPossessedSkillSlot(slotIndex, 0f);
    }

    public bool TryPossessedSkillSlot(int slotIndex, float chargeSeconds)
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            lastAttackResult = "Missing player type data.";
            return false;
        }

        if (!CanAttack(playerData))
        {
            lastAttackResult = "Possessed skill requires a possessed body.";
            return false;
        }

        if (!IsPossessedBodyAttackRuleSatisfied())
        {
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastAttackResult = "Cannot use possessed skill while dead.";
            return false;
        }

        if (runtimeStatus != null && !runtimeStatus.CanAttack)
        {
            lastAttackResult = "Cannot use possessed skill while action locked.";
            return false;
        }

        if (possessionSystem == null
            || !possessionSystem.TryGetPossessedSkillIdAt(slotIndex, out string skillActionId))
        {
            lastAttackResult = $"Missing possessed skill slot {slotIndex + 1}.";
            return false;
        }

        if (skillActionSystem == null || !skillActionSystem.TryUseSkill(skillActionId))
        {
            lastAttackResult = skillActionSystem != null
                ? skillActionSystem.LastSkillResult
                : "Missing skill action system.";
            return false;
        }

        runtimeStatus?.SetState(HWJ_RuntimeState.Attack);
        lastDamageApplied = skillActionSystem.LastDamageApplied;
        lastAttackResult = skillActionSystem.LastSkillResult;
        ApplySkillDecayAndNotify(skillActionId, Mathf.Max(0f, chargeSeconds));
        return true;
    }

    private bool ApplyBasicAttackDamage(HWJ_PlayerTypeDataSO playerData)
    {
        if (playerData.Attack == null)
        {
            lastAttackResult = "Missing attack data.";
            return false;
        }

        lastDamageApplied = 0f;

        if (combatExecutionSystem == null)
        {
            motionSystem?.PlayAttack(null);
            lastAttackResult = "Attack failed: missing combat execution system.";
            return false;
        }

        if (basicAttackRoutine != null)
        {
            StopCoroutine(basicAttackRoutine);
        }

        basicAttackRoutine = StartCoroutine(TimedBasicAttackRoutine(playerData));
        lastAttackResult = "Basic attack started.";
        return true;
    }

    public void CancelCurrentAttack()
    {
        if (basicAttackRoutine != null)
        {
            StopCoroutine(basicAttackRoutine);
            basicAttackRoutine = null;
        }

        runtimeStatus?.CancelAttackAction();
        ResetBasicAttackCombo();
        CancelBasicAttackCharge();
    }

    private IEnumerator TimedBasicAttackRoutine(HWJ_PlayerTypeDataSO playerData)
    {
        HWJ_PlayerAttackData attackData = playerData.Attack;
        float hitStart = Mathf.Max(0f, attackData.hitStartSeconds);
        float activeSeconds = Mathf.Max(0.01f, attackData.hitActiveSeconds);
        float recoverySeconds = Mathf.Max(0f, attackData.recoverySeconds);
        float totalSeconds = hitStart + activeSeconds + recoverySeconds;

        runtimeStatus?.BeginTimedAttackAction(
            totalSeconds,
            attackData.movementLockSeconds,
            attackData.dashCancelStartSeconds,
            attackData.canDashCancel);
        motionSystem?.PlayAttack(null);

        if (hitStart > 0f)
        {
            yield return new WaitForSeconds(hitStart);
        }

        float range = Mathf.Max(minimumAttackRange, attackData.attackRange);
        bool attacked = combatExecutionSystem.TryExecuteAreaAttack(range, 1f, null, false);
        lastDamageApplied = combatExecutionSystem.LastDamageApplied;
        lastAttackResult = combatExecutionSystem.LastExecutionResult;

        if (activeSeconds > 0f)
        {
            yield return new WaitForSeconds(activeSeconds);
        }

        if (recoverySeconds > 0f)
        {
            yield return new WaitForSeconds(recoverySeconds);
        }

        if (!attacked)
        {
            lastAttackResult = $"Basic attack missed: {combatExecutionSystem.LastExecutionResult}";
        }

        basicAttackRoutine = null;
    }

    private bool CanAttack(HWJ_PlayerTypeDataSO playerData)
    {
        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            return false;
        }

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            return false;
        }

        return playerData.State.canAttackOnBodyState
            && possessionSystem != null
            && possessionSystem.HasActivePossessedBody;
    }

    private bool CanContinueBasicAttackCharge()
    {
        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            lastAttackResult = "Basic attack charge cancelled: control is locked.";
            return false;
        }

        if (runtimeStatus != null && !runtimeStatus.CanAttack)
        {
            lastAttackResult = "Basic attack charge cancelled: attack is locked.";
            return false;
        }

        if (dataResolver == null
            || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            || !CanAttack(playerData))
        {
            lastAttackResult = "Basic attack charge cancelled: possessed body is unavailable.";
            return false;
        }

        return true;
    }

    private void TryPossessedSkillSlotInputs()
    {
        if (playerInput == null || possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            return;
        }

        int slotCount = Mathf.Max(0, possessedSkillSlotCount);

        for (int i = 0; i < slotCount; i++)
        {
            if (playerInput.WasSkillSlotPressedThisFrame(i))
            {
                TryPossessedSkillSlot(i);
                return;
            }
        }
    }

    private string GetBasicAttackSkillActionId(HWJ_PlayerTypeDataSO playerData)
    {
        if (possessionSystem != null && possessionSystem.TryGetPrimaryPossessedSkillId(out string possessedSkillId))
        {
            return possessedSkillId;
        }

        return playerData.Attack != null ? playerData.Attack.basicAttackSkillActionId : null;
    }

    private int ResolveNextBasicAttackComboStep(HWJ_PlayerAttackData attackData)
    {
        if (attackData == null || attackData.comboResetSeconds <= 0f)
        {
            return 1;
        }

        bool canContinueCombo = currentBasicAttackComboStep > 0
            && Time.time - lastBasicAttackComboTime <= attackData.comboResetSeconds;

        return canContinueCombo ? currentBasicAttackComboStep + 1 : 1;
    }

    private void RegisterBasicAttackComboUse(int comboStep, float chargeSeconds)
    {
        currentBasicAttackComboStep = Mathf.Max(1, comboStep);
        lastBasicAttackComboTime = Time.time;
        lastResolvedAttackChargeSeconds = Mathf.Max(0f, chargeSeconds);
    }

    private void ResetBasicAttackCombo()
    {
        currentBasicAttackComboStep = 0;
        lastBasicAttackComboTime = float.NegativeInfinity;
        lastResolvedAttackChargeSeconds = 0f;
    }

    private void CancelBasicAttackCharge(string message = null)
    {
        bool wasCharging = isChargingBasicAttack;
        float chargeSeconds = ResolveCurrentBasicAttackChargeSeconds();
        isChargingBasicAttack = false;
        basicAttackChargeStartTime = 0f;

        if (wasCharging)
        {
            RaiseBasicAttackChargeChanged(false, chargeSeconds, string.IsNullOrEmpty(message) ? lastAttackResult : message);
        }
    }

    private float ResolveCurrentBasicAttackChargeSeconds()
    {
        if (!isChargingBasicAttack)
        {
            return 0f;
        }

        float elapsedSeconds = Mathf.Max(0f, Time.time - basicAttackChargeStartTime);

        return maxBasicAttackChargeSeconds > 0f
            ? Mathf.Min(elapsedSeconds, maxBasicAttackChargeSeconds)
            : elapsedSeconds;
    }

    private float ResolveCurrentBasicAttackChargeRatio()
    {
        return maxBasicAttackChargeSeconds > 0f
            ? Mathf.Clamp01(ResolveCurrentBasicAttackChargeSeconds() / maxBasicAttackChargeSeconds)
            : 0f;
    }

    private void RaiseBasicAttackChargeChanged(bool isCharging, float chargeSeconds, string message)
    {
        HWJ_GameplayEvents.RaiseBasicAttackChargeChanged(
            new HWJ_BasicAttackChargeEvent(
                this,
                isCharging,
                chargeSeconds,
                maxBasicAttackChargeSeconds,
                message));
    }

    private void ApplyBasicAttackDecayAndNotify(string abilityId, float chargeSeconds, int comboStep)
    {
        ResolveBodyDecaySystem();
        bodyDecaySystem?.ApplyBasicAttackDecay(ResolveSkillActionData(abilityId), chargeSeconds, comboStep);
        RaiseAbilityUsed(abilityId, true, comboStep, chargeSeconds);
    }

    private void ApplySkillDecayAndNotify(string abilityId, float chargeSeconds)
    {
        ResolveBodyDecaySystem();
        lastResolvedAttackChargeSeconds = Mathf.Max(0f, chargeSeconds);
        bodyDecaySystem?.ApplySkillDecay(ResolveSkillActionData(abilityId), chargeSeconds);
        RaiseAbilityUsed(abilityId, false, 0, chargeSeconds);
    }

    private void ResolveBodyDecaySystem()
    {
        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = GetComponent<HWJ_BodyDecaySystem>();
        }
    }

    private HWJ_SkillActionDataSO ResolveSkillActionData(string abilityId)
    {
        if (skillActionSystem == null || string.IsNullOrEmpty(abilityId))
        {
            return null;
        }

        return skillActionSystem.TryGetSkillAction(abilityId, out HWJ_SkillActionDataSO skillAction)
            ? skillAction
            : null;
    }

    private void RaiseAbilityUsed(string abilityId, bool isBasicAttack, int comboStep, float chargeSeconds)
    {
        string resolvedAbilityId = string.IsNullOrEmpty(abilityId)
            ? (isBasicAttack ? "basic_attack" : "skill")
            : abilityId;
        float decayValue = bodyDecaySystem != null ? bodyDecaySystem.CurrentDecayValue : 0f;

        HWJ_GameplayEvents.RaiseAbilityUsed(
            new HWJ_AbilityUsedEvent(
                this,
                dataResolver,
                resolvedAbilityId,
                isBasicAttack,
                decayValue,
                lastAttackResult,
                comboStep,
                chargeSeconds));
    }

    private bool IsPossessedBodyAttackRuleSatisfied()
    {
        if (!useGameplayAttackRule)
        {
            return true;
        }

        HWJ_GameplayContext context = HWJ_GameplayContext
            .Create(dataResolver, null)
            .WithSource(this);

        if (ResolvePossessedBodyAttackExecutionCore(out HWJ_RuleExecutionCoreSO executionCore))
        {
            bool passed = executionCore.TryExecute(context, out HWJ_RuleExecutionResult result);
            lastAttackResult = result.Message;
            return passed;
        }

        HWJ_GameplayRuleSO rule = possessedBodyAttackRule;

        if (rule == null
            && !string.IsNullOrEmpty(possessedBodyAttackRuleId)
            && HWJ_GameAccess.TryGetGameplayRule(possessedBodyAttackRuleId, out HWJ_GameplayRuleSO resolvedRule))
        {
            rule = resolvedRule;
        }

        if (rule == null)
        {
            return true;
        }

        bool rulePassed = rule.TryEvaluate(context, out HWJ_RuleEvaluationResult ruleResult);
        lastAttackResult = ruleResult.Message;
        return rulePassed;
    }

    private bool ResolvePossessedBodyAttackExecutionCore(out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;

        if (possessedBodyAttackExecutionCore != null)
        {
            executionCore = possessedBodyAttackExecutionCore;
            return true;
        }

        if (string.IsNullOrEmpty(possessedBodyAttackExecutionCoreId))
        {
            return false;
        }

        return HWJ_GameAccess.TryGetRuleExecutionCore(possessedBodyAttackExecutionCoreId, out executionCore);
    }

    private void OnDrawGizmosSelected()
    {
        float range = minimumAttackRange;

        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Attack != null)
        {
            range = Mathf.Max(minimumAttackRange, playerData.Attack.attackRange);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
