using UnityEngine;

/// <summary>
/// 플레이어 기본 공격 입력과 공격 쿨타임을 관리하는 기본 시스템입니다.
/// 실제 판정 생성은 SkillActionSystem에 넘기고, 이 시스템은 PlayerTypeDataSO.Attack의 입력/간격 데이터만 담당합니다.
/// </summary>
public class HWJ_PlayerAttackSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_CombatExecutionSystem combatExecutionSystem;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private LayerMask attackTargetLayer;
    [SerializeField] private float minimumAttackRange = 2f;
    [SerializeField] private int possessedSkillSlotCount = 3;
    [SerializeField] private string lastAttackResult;
    [SerializeField] private float lastDamageApplied;

    private float nextAttackTime;

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
            playerInput = GetComponent<HWJ_PlayerInputSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (combatExecutionSystem == null)
        {
            combatExecutionSystem = GetComponent<HWJ_CombatExecutionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }
    }

    private void Update()
    {
        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            return;
        }

        if (playerInput != null && playerInput.AttackPressedThisFrame)
        {
            TryBasicAttack();
        }

        TryPossessedSkillSlotInputs();
    }

    /// <summary>
    /// 플레이어 기본 공격을 시도합니다.
    /// basicAttackSkillActionId가 있으면 SkillActionSystem으로 실행하고, 없으면 쿨타임과 상태만 갱신합니다.
    /// </summary>
    public bool TryBasicAttack()
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

        if (!CanAttack(playerData))
        {
            lastAttackResult = "Current soul state cannot attack.";
            return false;
        }

        if (playerData.Attack == null)
        {
            lastAttackResult = "Missing attack data.";
            return false;
        }

        nextAttackTime = Time.time + playerData.Attack.attackIntervalSeconds;

        if (runtimeStatus != null)
        {
            runtimeStatus.SetState(HWJ_RuntimeState.Attack);
        }

        string skillActionId = GetBasicAttackSkillActionId(playerData);

        if (skillActionSystem != null && !string.IsNullOrEmpty(skillActionId))
        {
            return skillActionSystem.TryUseSkill(skillActionId);
        }

        return ApplyBasicAttackDamage(playerData);
    }

    /// <summary>
    /// 빙의 상태에서 숫자키 1/2/3으로 몬스터의 스킬 목록을 직접 실행합니다.
    /// 스킬 자체 쿨타임은 SkillActionSystem이 관리하므로 여기서는 입력과 상태 조건만 확인합니다.
    /// </summary>
    public bool TryPossessedSkillSlot(int slotIndex)
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            lastAttackResult = "Missing player type data.";
            return false;
        }

        if (!CanAttack(playerData))
        {
            lastAttackResult = "Current soul state cannot use possessed skill.";
            return false;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastAttackResult = "Cannot use possessed skill while dead.";
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

        float range = Mathf.Max(minimumAttackRange, playerData.Attack.attackRange);
        bool attacked = combatExecutionSystem.TryExecuteAreaAttack(range);
        lastDamageApplied = combatExecutionSystem.LastDamageApplied;
        lastAttackResult = combatExecutionSystem.LastExecutionResult;

        if (attacked)
        {
            return true;
        }

        return false;
    }

    private bool CanAttack(HWJ_PlayerTypeDataSO playerData)
    {
        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            return false;
        }

        if (soulSystem == null)
        {
            return playerData.State.canAttackOnBodyState;
        }

        return soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
            ? playerData.State.canAttackOnSoulState
            : playerData.State.canAttackOnBodyState;
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
