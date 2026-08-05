using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(150)]
// hys_Player_State의 PlayerState Int 값을 중심으로 Animator를 제어하는 스크립트입니다.
public class hys_Player_Animator : MonoBehaviour
{
    [Header("References")]
    // 필요한 컴포넌트를 자동으로 찾되, 인스펙터에서 직접 넣어도 됩니다.
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private hys_Player_State playerState;
    [SerializeField] private hys_Player_Movement playerMovement;
    [SerializeField] private hys_Player_Attack playerAttack;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

    [Header("Animator Parameters")]
    // PlayerState가 핵심 상태 값이고, 나머지는 보조 조건입니다.
    [SerializeField] private string playerStateParameter = "PlayerState";
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string isGroundedParameter = "IsGrounded";
    [SerializeField] private string isSoulParameter = "IsSoul";
    [SerializeField] private string attackComboStepParameter = "AttackComboStep";
    [SerializeField] private string horizontalSpeedParameter = "HorizontalSpeed";
    [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";

    [Header("Attack Animation Combo")]
    // 공격 데이터가 완성되기 전까지 애니메이션 전환만 임시로 관리합니다.
    [SerializeField] private bool useAnimationOnlyAttackCombo = true;
    [SerializeField] private float firstAttackAnimationTime = 0.28f;
    [SerializeField] private float secondAttackAnimationTime = 0.28f;
    [SerializeField] private float comboInputOpenTime = 0f;
    [SerializeField] private float comboInputCloseTime = 0.28f;
    [SerializeField] private float comboInputGraceTime = 0.1f;
    [SerializeField] private float comboLinkTime = 0.12f;
    [SerializeField] private bool playAttackStateDirectly;
    [SerializeField] private string firstAttackStateName = "hys_Sword_Attack1";
    [SerializeField] private string secondAttackStateName = "hys_Sword_Attack2";
    [SerializeField] private float attackTransitionSeconds = 0.03f;

    [Header("Sword Attack")]
    // 원본 Queen 공격을 두 번 온전히 휘두르도록 첫 공격 후반부에서 다음 타격을 연결합니다.
    // 기본 Red Queen 공격 클립 길이에 맞추고, 두 번째 입력은 즉시 다음 콤보로 연결합니다.
    [SerializeField, Min(0.05f)] private float swordFirstAttackAnimationTime = 0.6f;
    [SerializeField, Min(0.05f)] private float swordSecondAttackAnimationTime = 0.6f;
    [SerializeField, Min(0f)] private float swordComboLinkTime = 0.52f;
    [SerializeField] private string swordFirstAttackStateName = "hys_Sword_Attack1";
    [SerializeField] private string swordSecondAttackStateName = "hys_Sword_Attack2";

    [Header("Axe Heavy Attack")]
    // King의 5프레임 도끼 공격을 끝까지 보여 준 뒤 다음 타격으로 연결해 무거운 타격감을 유지합니다.
    [SerializeField, Min(0.05f)] private float axeFirstAttackAnimationTime = 1.1f;
    [SerializeField, Min(0.05f)] private float axeSecondAttackAnimationTime = 1.1f;
    [SerializeField, Min(0f)] private float axeComboLinkTime = 1.05f;

    [Header("Shield Attack")]
    // Red Rook 공격 프레임이 중간에 끊기지 않도록 Shield 전용 재생 시간과 연계 시점을 사용합니다.
    [SerializeField, Min(0.05f)] private float shieldFirstAttackAnimationTime = 0.75f;
    [SerializeField, Min(0.05f)] private float shieldSecondAttackAnimationTime = 0.6f;
    [SerializeField, Min(0f)] private float shieldComboLinkTime = 0.68f;

    [Header("Axe Dive Attack")]
    // Axe 낙하 공격이 시작되면 일반 공중 공격 콤보 대신 내려찍기 모션을 재생합니다.
    [SerializeField] private bool playAxeDiveAttackDirectly = true;
    [SerializeField] private string axeDiveAttackParameter = "IsAxeDiveAttacking";
    [SerializeField] private string axePlungeGroundedParameter = "IsAxePlungeGrounded";
    [SerializeField] private string axePlungeStartStateName = "hys_Axe_Plunge_Start";
    [SerializeField] private string axePlungeFallStateName = "hys_Axe_Plunge_Fall";
    [SerializeField, Min(0f)] private float axePlungeTransitionSeconds = 0.02f;

    [Header("Shield Plunge Attack")]
    // 방패 낙공은 같은 물리 판정을 사용하되 Red Rook 전용 준비/낙하/착지 상태를 재생합니다.
    [SerializeField] private string shieldPlungeStartStateName = "hys_Shield_Plunge_Start";
    [SerializeField] private string shieldPlungeFallStateName = "hys_Shield_Plunge_Fall";
    [SerializeField] private string shieldPlungeLandStateName = "hys_Shield_Plunge_Land";
    [SerializeField, Min(0f)] private float shieldPlungeTransitionSeconds = 0.02f;

    [Header("Jump Trigger")]
    // 1단 점프와 2단 점프가 새로 발생할 때 Jump_Start로 다시 보내기 위한 설정입니다.
    [SerializeField] private string jumpTriggerParameter = "JumpTrigger";
    [SerializeField] private bool sendJumpTriggerParameter;
    [SerializeField] private bool crossFadeToJumpStartOnJump;
    [SerializeField] private string jumpStartStateName = "hys_Sword_Jump_Start";
    [SerializeField] private float jumpStartTransitionSeconds = 0.03f;

    [Header("Dash Animation")]
    // 대시가 시작될 때 현재 무기 컨트롤러의 Dash 상태를 직접 첫 프레임부터 재생합니다.
    [SerializeField] private bool crossFadeToDashOnStart = true;
    [SerializeField] private string dashStateName = "hys_Sword_Dash";
    [SerializeField, Min(0f)] private float dashTransitionSeconds;

    [Header("Soul Trigger")]
    // 육신에서 소울 애니메이션으로 넘어가는 순간 한 번만 보냅니다.
    [SerializeField] private string soulTriggerParameter = "SoulTrigger";

    [Header("Animator Controller Swap")]
    // 영혼 상태가 되면 플레이어 컨트롤러에서 유령 컨트롤러로 교체합니다.
    [SerializeField] private bool switchControllerInSoulState = true;
    [SerializeField] private RuntimeAnimatorController bodyAnimatorController;
    [SerializeField] private RuntimeAnimatorController swordAnimatorController;
    [SerializeField] private RuntimeAnimatorController axeAnimatorController;
    [SerializeField] private RuntimeAnimatorController bowAnimatorController;
    [SerializeField] private RuntimeAnimatorController lanceAnimatorController;
    [SerializeField] private RuntimeAnimatorController shieldAnimatorController;
    [SerializeField] private RuntimeAnimatorController ghostAnimatorController;
    [SerializeField] private bool switchGhostControllerOnBodyToSoul = true;

    [Header("Ghost Handoff")]
    // 빙의와 영혼 사망 모션이 끝날 때까지 유령 컨트롤러를 유지합니다.
    [SerializeField] private string ghostPossessionStateName = "Possession";
    [SerializeField] private string ghostPossessionClipName = "hys_Ghost_possession";
    [SerializeField] private string ghostDeadStateName = "Die";
    [SerializeField] private AnimationClip swordGhostPossessionClip;
    [SerializeField] private AnimationClip axeGhostPossessionClip;
    [SerializeField] private AnimationClip bowGhostPossessionClip;
    [SerializeField] private AnimationClip lanceGhostPossessionClip;
    [SerializeField] private AnimationClip shieldGhostPossessionClip;
    [SerializeField, Range(0.5f, 1f)] private float ghostHandoffNormalizedTime = 0.98f;
    [SerializeField, Min(0.05f)] private float ghostPossessionFallbackSeconds = 0.45f;

    [Header("Body Locomotion Sync")]
    // 유령에서 육신으로 돌아온 직후에도 Idle/Run 상태가 확실히 선택되도록 보정합니다.
    [SerializeField] private bool forceBodyLocomotionStateSync = true;
    [SerializeField] private string bodyIdleStateName = "hys_Sword_Idle";
    [SerializeField] private string bodyMoveStateName = "hys_Sword_Run";
    [SerializeField, Min(0f)] private float locomotionTransitionSeconds = 0.05f;

    [Header("Facing")]
    // Transform 스케일은 절대 바꾸지 않고 SpriteRenderer.flipX만 사용합니다.
    [SerializeField] private bool flipSpriteByMoveDirection = true;
    [SerializeField] private float moveThreshold = 0.05f;

    [Header("Auto Return")]
    // Dash/Hit 같은 1회성 상태가 끝나면 기본 Idle 상태로 되돌립니다.
    [SerializeField] private bool autoReturnFinishedActionStates = true;
    [SerializeField] private bool autoReturnByStateTime = true;
    [SerializeField] private int baseLayerIndex;
    [SerializeField] private string defaultAnimatorStateName = "hys_Sword_Idle";
    [SerializeField] private string[] autoReturnStateNames = { "hys_Sword_Dash", "hys_Sword_Hit" };
    [SerializeField] private float returnNormalizedTime = 0.98f;
    [SerializeField] private float returnTransitionSeconds = 0.05f;
    [SerializeField] private float dashReturnSeconds = 0.25f;
    [SerializeField] private float hitReturnSeconds = 0.35f;

    private int previousJumpVersion;
    private int previousDashVersion;
    private bool wasSoulState;
    private hys_PlayerState previousPlayerState = hys_PlayerState.Idle;
    private float actionStateStartTime;
    private int animationAttackComboStep;
    private float animationAttackStartTime = -1f;
    private bool queuedSecondAttackAnimation;
    private bool queuedRestartAttackAnimation;
    private RuntimeAnimatorController cachedBodyAnimatorController;
    private AnimatorOverrideController activeGhostPossessionController;
    private HWJ_SoulRuntimeState previousSoulRuntimeState = HWJ_SoulRuntimeState.Body;
    private bool isGhostPossessionPlaying;
    private bool isGhostDeadPlaying;
    private float ghostPossessionStartedTime;
    private bool wasAxeDiveStarting;
    private bool wasAxeDiveFalling;
    private bool wasAxeDiveGrounded;

    // 이동 스크립트가 빙의 연출이 끝날 때까지 플레이어 입력을 잠글 때 사용합니다.
    public bool IsPossessionTransitionPlaying => isGhostPossessionPlaying;

    private int playerStateHash;
    private int isMovingHash;
    private int isGroundedHash;
    private int isSoulHash;
    private int attackComboStepHash;
    private int horizontalSpeedHash;
    private int verticalSpeedHash;
    private int axeDiveAttackHash;
    private int axePlungeGroundedHash;
    private int jumpTriggerHash;
    private int soulTriggerHash;

    private void Awake()
    {
        CacheReferences();

        // Animator가 자식 오브젝트에 있는 RuntimeReady 프리팹에서도 이벤트 수신기가 같은 위치에 붙게 합니다.
        GameObject effectHost = animator != null ? animator.gameObject : gameObject;
        if (effectHost.GetComponent<hys_PlayerSkillEffectPlayer>() == null)
        {
            effectHost.AddComponent<hys_PlayerSkillEffectPlayer>();
        }

        CacheParameterHashes();
        ResetJumpVersion();
        ResetDashVersion();
        CacheBodyAnimatorController();
        wasSoulState = IsSoulState();
        previousSoulRuntimeState = GetSoulRuntimeState();
    }

    private void OnEnable()
    {
        CacheReferences();
        CacheBodyAnimatorController();
        ResetJumpVersion();
        ResetDashVersion();
        wasSoulState = IsSoulState();
        previousSoulRuntimeState = GetSoulRuntimeState();
    }

    private void Update()
    {
        CacheReferences();

        if (animator == null)
        {
            return;
        }

        float horizontalVelocity = rb != null ? rb.linearVelocity.x : 0f;
        float verticalVelocity = rb != null ? rb.linearVelocity.y : 0f;
        bool isMoving = Mathf.Abs(horizontalVelocity) > moveThreshold;
        bool isGrounded = GetIsGroundedFromState();
        bool isSoulState = IsSoulState();
        HWJ_SoulRuntimeState soulRuntimeState = GetSoulRuntimeState();
        hys_PlayerState currentState = playerState != null ? playerState.CurrentState : hys_PlayerState.Idle;
        bool isAxeDiveAttacking = playerAttack != null && playerAttack.IsAxeDiveAttacking;
        bool isAxeDiveStarting = playerAttack != null && playerAttack.IsAxeDiveStarting;
        bool isAxeDiveFalling = playerAttack != null && playerAttack.IsAxeDiveFalling;
        bool isAxeDiveGrounded = playerAttack != null && playerAttack.IsAxeDiveGrounded;
        if (isAxeDiveAttacking)
        {
            ResetAttackAnimationCombo();
        }
        int attackComboStep = isAxeDiveAttacking ? 0 : UpdateAttackAnimationCombo(currentState);

        UpdateAnimatorControllerForSoulState(soulRuntimeState);
        bool isUsingGhostController = IsUsingGhostAnimatorController();
        SetAnimatorInt(playerStateHash, (int)currentState);
        SetAnimatorInt(attackComboStepHash, attackComboStep);
        if (!isUsingGhostController)
        {
            // 유령 이동 파라미터는 hys_Ghost_Animator만 갱신합니다.
            SetAnimatorBool(isMovingHash, isMoving);
        }
        SetAnimatorBool(isGroundedHash, isGrounded);
        SetAnimatorBool(isSoulHash, isSoulState);
        SetAnimatorFloat(horizontalSpeedHash, Mathf.Abs(horizontalVelocity));
        SetAnimatorFloat(verticalSpeedHash, verticalVelocity);

        SyncBodyLocomotionState(currentState, isSoulState, isGrounded, isMoving);

        if (!isUsingGhostController)
        {
            // 유령 컨트롤러에는 플레이어 전용 Jump/Dash/Hit 상태를 요청하지 않습니다.
            UpdateJumpTrigger();
            UpdateDashState();
            UpdateAxeDiveAttackAnimation(
                isAxeDiveAttacking,
                isAxeDiveStarting,
                isAxeDiveFalling,
                isAxeDiveGrounded);
            UpdateSoulTrigger(isSoulState, currentState);
            UpdateFacing(horizontalVelocity);
            UpdateActionStateTimer(currentState);
            TryReturnFromFinishedActionState();
            TryReturnFromTimedActionState(currentState);
        }

        previousPlayerState = currentState;
        wasSoulState = isSoulState;
        previousSoulRuntimeState = soulRuntimeState;
    }

    private void UpdateAxeDiveAttackAnimation(
        bool isActive,
        bool isStarting,
        bool isFalling,
        bool isGrounded)
    {
        SetAnimatorBool(axeDiveAttackHash, isActive);
        // 실제 Collider 접촉으로 켜지는 전용 조건을 Animator 전환에 전달합니다.
        SetAnimatorBool(axePlungeGroundedHash, isGrounded);

        bool isShieldController = animator.runtimeAnimatorController != null
            && animator.runtimeAnimatorController.name == "hys_Player_Shield";
        string plungeStartStateName = isShieldController
            ? shieldPlungeStartStateName
            : axePlungeStartStateName;
        string plungeFallStateName = isShieldController
            ? shieldPlungeFallStateName
            : axePlungeFallStateName;
        float plungeTransitionSeconds = isShieldController
            ? shieldPlungeTransitionSeconds
            : axePlungeTransitionSeconds;

        if (isStarting && !wasAxeDiveStarting && playAxeDiveAttackDirectly &&
            !string.IsNullOrEmpty(plungeStartStateName))
        {
            // 점프/낙하 상태에서 낙공 준비 자세의 첫 프레임으로 진입합니다.
            CrossFadeAnimatorState(plungeStartStateName, plungeTransitionSeconds, 0f);
        }

        if (isFalling && !wasAxeDiveFalling && playAxeDiveAttackDirectly &&
            !string.IsNullOrEmpty(plungeFallStateName))
        {
            // 준비가 끝난 순간 정면 내려찍기 낙하 루프로 전환합니다.
            CrossFadeAnimatorState(plungeFallStateName, plungeTransitionSeconds, 0f);
        }

        if (isShieldController && isGrounded && !wasAxeDiveGrounded &&
            !string.IsNullOrEmpty(shieldPlungeLandStateName))
        {
            // 실제 지면 접촉 순간 방패 충격 착지 모션을 처음부터 재생합니다.
            CrossFadeAnimatorState(
                shieldPlungeLandStateName,
                shieldPlungeTransitionSeconds,
                0f);
        }

        if (isShieldController && !isActive &&
            (wasAxeDiveStarting || wasAxeDiveFalling || wasAxeDiveGrounded))
        {
            // 낙공 후딜이 끝나면 전환선이 없는 전용 상태에서 Shield Idle로 안전하게 복귀합니다.
            CrossFadeAnimatorState(
                ResolveWeaponStateName("Idle", bodyIdleStateName),
                shieldPlungeTransitionSeconds);
        }

        wasAxeDiveStarting = isStarting;
        wasAxeDiveFalling = isFalling;
        wasAxeDiveGrounded = isGrounded;
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (playerState == null)
        {
            playerState = GetComponent<hys_Player_State>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<hys_Player_Movement>();
        }

        if (playerAttack == null)
        {
            playerAttack = GetComponent<hys_Player_Attack>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

    }

    private void CacheBodyAnimatorController()
    {
        if (animator == null)
        {
            return;
        }

        if (bodyAnimatorController == null && !IsUsingGhostAnimatorController())
        {
            bodyAnimatorController = animator.runtimeAnimatorController;
        }

        if (cachedBodyAnimatorController == null && bodyAnimatorController != null)
        {
            cachedBodyAnimatorController = bodyAnimatorController;
        }
    }

    private void UpdateAnimatorControllerForSoulState(HWJ_SoulRuntimeState soulState)
    {
        if (!switchControllerInSoulState || animator == null || ghostAnimatorController == null)
        {
            return;
        }

        bool startedPossession = previousSoulRuntimeState == HWJ_SoulRuntimeState.Soul
            && soulState == HWJ_SoulRuntimeState.Body
            && possessionSystem != null
            && possessionSystem.HasActivePossessedBody;
        bool startedDead = soulState == HWJ_SoulRuntimeState.Dead
            && !isGhostDeadPlaying;

        if (previousSoulRuntimeState == HWJ_SoulRuntimeState.Dead
            && soulState != HWJ_SoulRuntimeState.Dead)
        {
            // 재시작이나 테스트 복귀 시 유령 사망 고정 상태를 해제합니다.
            isGhostDeadPlaying = false;
        }

        if (startedPossession)
        {
            isGhostPossessionPlaying = true;
            isGhostDeadPlaying = false;
            ghostPossessionStartedTime = Time.unscaledTime;
            activeGhostPossessionController = CreateGhostPossessionOverrideController();
        }

        if (startedDead)
        {
            isGhostDeadPlaying = true;
            isGhostPossessionPlaying = false;
            activeGhostPossessionController = null;
        }

        bool shouldUseGhostController = soulState == HWJ_SoulRuntimeState.Soul ||
            (switchGhostControllerOnBodyToSoul && soulState == HWJ_SoulRuntimeState.BodyToSoul) ||
            soulState == HWJ_SoulRuntimeState.Dead ||
            isGhostPossessionPlaying ||
            isGhostDeadPlaying;
        RuntimeAnimatorController possessionGhostController = isGhostPossessionPlaying
            && activeGhostPossessionController != null
            ? activeGhostPossessionController
            : ghostAnimatorController;
        RuntimeAnimatorController targetController = shouldUseGhostController
            ? possessionGhostController
            : ResolveBodyAnimatorController();

        if (targetController != null && animator.runtimeAnimatorController != targetController)
        {
            // 컨트롤러 교체 직후 첫 프레임을 안정적으로 다시 잡습니다.
            animator.runtimeAnimatorController = targetController;
            animator.Rebind();
            animator.Update(0f);
            Debug.Log($"[hys Animator] Controller -> {targetController.name}, SoulState={soulState}", this);
        }

        if (startedPossession)
        {
            PlayGhostState(ghostPossessionStateName);
        }

        if (startedDead)
        {
            PlayGhostState(ghostDeadStateName);
        }

        // Possession 상태가 실제로 있으면 클립의 마지막 프레임까지 기다리고, 상태가 없을 때만 안전 타이머를 씁니다.
        bool hasPossessionState = HasGhostState(ghostPossessionStateName);
        bool possessionFallbackFinished = isGhostPossessionPlaying
            && !hasPossessionState
            && Time.unscaledTime - ghostPossessionStartedTime >= ghostPossessionFallbackSeconds;
        if (isGhostPossessionPlaying
            && !startedPossession
            && (HasGhostStateFinished(ghostPossessionStateName) || possessionFallbackFinished))
        {
            // 빙의 모션이 끝난 다음 프레임부터 육신 애니메이터를 사용합니다.
            isGhostPossessionPlaying = false;
            RuntimeAnimatorController bodyController = ResolveBodyAnimatorController();

            if (bodyController != null && animator.runtimeAnimatorController != bodyController)
            {
                animator.runtimeAnimatorController = bodyController;
                animator.Rebind();
                animator.Update(0f);
                Debug.Log(
                    $"[hys Animator] Possession complete -> {bodyController.name}, Weapon={possessionSystem?.CurrentWeaponType}",
                    this);

                // 빙의 직후 육체 컨트롤러의 첫 프레임을 반드시 Idle로 표시합니다.
                // 영혼이 육신으로 들어온 뒤에는 전용 복귀 모션을 먼저 재생하고, 없는 컨트롤러만 Idle로 돌아갑니다.
                // 무기 전용 Possession이 끝났으므로 육신 컨트롤러에서는 복귀 모션을 중복 재생하지 않습니다.
                string idleStateName = ResolveWeaponStateName("Idle", bodyIdleStateName);
                if (!string.IsNullOrEmpty(idleStateName))
                {
                    PlayAnimatorState(idleStateName);
                }
            }

            activeGhostPossessionController = null;
        }
    }

    private AnimatorOverrideController CreateGhostPossessionOverrideController()
    {
        AnimationClip replacementClip = ResolveGhostPossessionClip();
        if (replacementClip == null || string.IsNullOrEmpty(ghostPossessionClipName))
        {
            return null;
        }

        // 공용 Ghost Controller에서 Possession 클립 하나만 현재 무기 전용 클립으로 교체합니다.
        AnimatorOverrideController overrideController = new AnimatorOverrideController(ghostAnimatorController);
        overrideController[ghostPossessionClipName] = replacementClip;
        return overrideController;
    }

    private AnimationClip ResolveGhostPossessionClip()
    {
        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            return null;
        }

        switch (possessionSystem.CurrentWeaponType)
        {
            case HWJ_WeaponType.Axe:
                return axeGhostPossessionClip;
            case HWJ_WeaponType.Bow:
                return bowGhostPossessionClip;
            case HWJ_WeaponType.Lance:
                return lanceGhostPossessionClip;
            case HWJ_WeaponType.Shield:
                return shieldGhostPossessionClip;
            default:
                return swordGhostPossessionClip;
        }
    }

    private bool IsUsingGhostAnimatorController()
    {
        if (animator == null)
        {
            return false;
        }

        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        return currentController == ghostAnimatorController
            || (activeGhostPossessionController != null
                && currentController == activeGhostPossessionController);
    }

    private RuntimeAnimatorController ResolveBodyAnimatorController()
    {
        // 빙의한 육신의 무기 종류에 맞는 hys 플레이어 컨트롤러를 선택합니다.
        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            switch (possessionSystem.CurrentWeaponType)
            {
                case HWJ_WeaponType.Sword:
                    // Sword 시체에 빙의했을 때 기본 Axe 컨트롤러로 빠지지 않도록 전용 컨트롤러를 선택합니다.
                    return swordAnimatorController != null ? swordAnimatorController : GetDefaultBodyAnimatorController();
                case HWJ_WeaponType.Axe:
                    return axeAnimatorController != null ? axeAnimatorController : GetDefaultBodyAnimatorController();
                case HWJ_WeaponType.Bow:
                    return bowAnimatorController != null ? bowAnimatorController : GetDefaultBodyAnimatorController();
                case HWJ_WeaponType.Lance:
                    return lanceAnimatorController != null ? lanceAnimatorController : GetDefaultBodyAnimatorController();
                case HWJ_WeaponType.Shield:
                    return shieldAnimatorController != null ? shieldAnimatorController : GetDefaultBodyAnimatorController();
            }
        }

        return GetDefaultBodyAnimatorController();
    }

    private RuntimeAnimatorController GetDefaultBodyAnimatorController()
    {
        return bodyAnimatorController != null ? bodyAnimatorController : cachedBodyAnimatorController;
    }

    private void PlayGhostState(string stateName)
    {
        PlayAnimatorState(stateName);
    }

    private bool PlayAnimatorState(string stateName)
    {
        if (!TryResolveAnimatorStateHash(stateName, out int stateHash))
        {
            return false;
        }

        animator.Play(stateHash, baseLayerIndex, 0f);
        animator.Update(0f);
        return true;
    }

    private bool CrossFadeAnimatorState(
        string stateName,
        float transitionSeconds,
        float normalizedTime = float.NegativeInfinity)
    {
        if (!TryResolveAnimatorStateHash(stateName, out int stateHash))
        {
            return false;
        }

        animator.CrossFade(stateHash, transitionSeconds, baseLayerIndex, normalizedTime);
        return true;
    }

    private bool TryResolveAnimatorStateHash(string stateName, out int stateHash)
    {
        stateHash = 0;
        if (animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        // 중첩 StateMachine 상태도 안전하게 재생하도록 전체 경로를 검사합니다.
        string layerName = animator.GetLayerName(baseLayerIndex);
        string[] statePaths =
        {
            $"{layerName}.{stateName}",
            $"{layerName}.Jump.{stateName}",
            $"{layerName}.Attack.{stateName}"
        };

        for (int i = 0; i < statePaths.Length; i++)
        {
            int candidateHash = Animator.StringToHash(statePaths[i]);
            if (animator.HasState(baseLayerIndex, candidateHash))
            {
                stateHash = candidateHash;
                return true;
            }
        }

        Debug.LogWarning(
            $"[hys Animator] 컨트롤러 '{animator.runtimeAnimatorController.name}'에 상태 '{stateName}'가 없습니다.",
            this);
        return false;
    }

    private bool HasGhostStateFinished(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || animator.IsInTransition(baseLayerIndex))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
        int stateHash = Animator.StringToHash(stateName);
        return stateInfo.shortNameHash == stateHash
            && stateInfo.normalizedTime >= ghostHandoffNormalizedTime;
    }

    private bool HasGhostState(string stateName)
    {
        // 긴 영혼 흡수·기상 연출이 기존 0.45초 폴백에 잘리지 않도록 상태 존재 여부를 먼저 확인합니다.
        return animator != null
            && !string.IsNullOrEmpty(stateName)
            && animator.HasState(baseLayerIndex, Animator.StringToHash(stateName));
    }

    private void SyncBodyLocomotionState(
        hys_PlayerState currentState,
        bool isSoulState,
        bool isGrounded,
        bool isMoving)
    {
        if (!forceBodyLocomotionStateSync
            || animator == null
            || isSoulState
            || isGhostPossessionPlaying
            || isGhostDeadPlaying
            || animationAttackComboStep != 0
            || !isGrounded
            || (currentState != hys_PlayerState.Idle && currentState != hys_PlayerState.Move))
        {
            return;
        }

        // Rigidbody 속도와 hys 상태 중 하나라도 이동을 뜻하면 Run을 선택합니다.
        bool shouldMove = currentState == hys_PlayerState.Move || isMoving;
        string targetStateName = shouldMove
            ? ResolveWeaponStateName("Run", bodyMoveStateName)
            : ResolveWeaponStateName("Idle", bodyIdleStateName);

        if (string.IsNullOrEmpty(targetStateName) || IsCurrentOrNextAnimatorState(targetStateName))
        {
            return;
        }

        CrossFadeAnimatorState(targetStateName, locomotionTransitionSeconds);
    }

    private bool IsCurrentOrNextAnimatorState(string stateName)
    {
        // 짧은 상태 이름 해시로 비교해 같은 애니메이션을 매 프레임 재시작하지 않습니다.
        int stateHash = Animator.StringToHash(stateName);
        AnimatorStateInfo currentStateInfo = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
        if (currentStateInfo.shortNameHash == stateHash)
        {
            return true;
        }

        return animator.IsInTransition(baseLayerIndex)
            && animator.GetNextAnimatorStateInfo(baseLayerIndex).shortNameHash == stateHash;
    }

    private void CacheParameterHashes()
    {
        playerStateHash = Animator.StringToHash(playerStateParameter);
        isMovingHash = Animator.StringToHash(isMovingParameter);
        isGroundedHash = Animator.StringToHash(isGroundedParameter);
        isSoulHash = Animator.StringToHash(isSoulParameter);
        attackComboStepHash = Animator.StringToHash(attackComboStepParameter);
        horizontalSpeedHash = Animator.StringToHash(horizontalSpeedParameter);
        verticalSpeedHash = Animator.StringToHash(verticalSpeedParameter);
        axeDiveAttackHash = Animator.StringToHash(axeDiveAttackParameter);
        axePlungeGroundedHash = Animator.StringToHash(axePlungeGroundedParameter);
        jumpTriggerHash = Animator.StringToHash(jumpTriggerParameter);
        soulTriggerHash = Animator.StringToHash(soulTriggerParameter);
    }

    private void ResetJumpVersion()
    {
        previousJumpVersion = playerMovement != null ? playerMovement.JumpVersion : 0;
    }

    private bool GetIsGroundedFromState()
    {
        // 점프 전환이 멈추지 않도록 실제 바닥 체크 값을 Animator에 전달합니다.
        if (playerMovement != null)
        {
            return playerMovement.Is_Grounded;
        }

        if (playerState == null)
        {
            return true;
        }

        hys_PlayerState state = playerState.CurrentState;
        return state != hys_PlayerState.Jump && state != hys_PlayerState.Fall;
    }

    private bool IsSoulState()
    {
        return soulSystem != null &&
            (soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul ||
             soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul);
    }

    private void ResetDashVersion()
    {
        previousDashVersion = playerMovement != null ? playerMovement.DashVersion : 0;
    }

    private void UpdateDashState()
    {
        if (playerMovement == null)
        {
            return;
        }

        int currentDashVersion = playerMovement.DashVersion;
        if (currentDashVersion == previousDashVersion)
        {
            return;
        }

        previousDashVersion = currentDashVersion;

        bool isDashState = playerState != null && playerState.CurrentState == hys_PlayerState.Dash;
        if (!crossFadeToDashOnStart
            || (!playerMovement.Is_Dashing && !isDashState)
            || string.IsNullOrEmpty(dashStateName))
        {
            return;
        }

        // Any State 전환과 다른 모션 요청이 겹쳐도 편집한 Dash 클립을 첫 프레임부터 고정 재생합니다.
        CrossFadeAnimatorState(
            ResolveWeaponStateName("Dash", dashStateName),
            dashTransitionSeconds,
            0f);
    }

    private HWJ_SoulRuntimeState GetSoulRuntimeState()
    {
        return soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
    }

    private void UpdateJumpTrigger()
    {
        if (playerMovement == null)
        {
            return;
        }

        int currentJumpVersion = playerMovement.JumpVersion;
        if (currentJumpVersion == previousJumpVersion)
        {
            return;
        }

        previousJumpVersion = currentJumpVersion;

        if (sendJumpTriggerParameter)
        {
            SetAnimatorTrigger(jumpTriggerHash);
        }

        if (crossFadeToJumpStartOnJump && !string.IsNullOrEmpty(jumpStartStateName))
        {
            // Apex/Fall 도중 2단 점프를 하면 즉시 Jump_Start로 다시 되돌립니다.
            CrossFadeAnimatorState(
                ResolveWeaponStateName("Jump_Start", jumpStartStateName),
                jumpStartTransitionSeconds);
        }
    }

    private void UpdateSoulTrigger(bool isSoulState, hys_PlayerState currentState)
    {
        // Shield는 Die 상태의 Exit Time 뒤에 SoulExit이 연결되어 있으므로 전환 시작 시 트리거를 미리 예약합니다.
        if (currentState == hys_PlayerState.Dead)
        {
            bool startedBodyToSoul = !wasSoulState
                && isSoulState
                && GetSoulRuntimeState() == HWJ_SoulRuntimeState.BodyToSoul;
            bool isShieldController = animator.runtimeAnimatorController != null
                && animator.runtimeAnimatorController.name == "hys_Player_Shield";

            if (startedBodyToSoul && isShieldController)
            {
                SetAnimatorTrigger(soulTriggerHash);
            }

            return;
        }

        // 육신에서 유령 상태로 넘어가는 순간 Soul 모션을 실행합니다.
        if (!wasSoulState && isSoulState)
        {
            SetAnimatorTrigger(soulTriggerHash);
        }
    }

    private int UpdateAttackAnimationCombo(hys_PlayerState currentState)
    {
        if (!useAnimationOnlyAttackCombo)
        {
            return 0;
        }

        if (currentState == hys_PlayerState.Hit || currentState == hys_PlayerState.Dead)
        {
            ResetAttackAnimationCombo();
            return 0;
        }

        bool attackPressed = IsAttackInputPressedForAnimation();
        bool startedFirstAttackThisFrame = false;
        if (currentState == hys_PlayerState.Attack &&
            animationAttackComboStep == 0 &&
            (previousPlayerState != hys_PlayerState.Attack || attackPressed))
        {
            // Attack 상태가 유지된 채 2타가 끝난 뒤에도 새 입력이 오면 다시 1타부터 시작합니다.
            // 첫 입력은 1타 시작에만 사용하고 같은 프레임의 2타 예약으로 중복 처리하지 않습니다.
            StartAttackAnimationStep(1);
            startedFirstAttackThisFrame = true;
        }

        if (animationAttackComboStep == 0)
        {
            return 0;
        }

        float elapsedTime = Time.time - animationAttackStartTime;
        if (animationAttackComboStep == 1 && attackPressed && !startedFirstAttackThisFrame)
        {
            // 연타 감각을 위해 2타 입력은 시간창을 기다리지 않고 바로 예약합니다.
            queuedSecondAttackAnimation = true;
        }
        else if (animationAttackComboStep == 2 && attackPressed)
        {
            // 2타 중 입력은 다음 1타로 이어지게 해서 공격이 뚝 끊기지 않게 합니다.
            queuedRestartAttackAnimation = true;
        }

        // 씬에서 Axe 컨트롤러를 직접 지정한 테스트 상태도 도끼 공격 타이밍으로 판정합니다.
        bool isAxeAttack = (possessionSystem != null
            && possessionSystem.CurrentWeaponType == HWJ_WeaponType.Axe)
            || (animator.runtimeAnimatorController != null
                && animator.runtimeAnimatorController.name == "hys_Player_Axe");
        bool isSwordAttack = animator.runtimeAnimatorController != null
            && animator.runtimeAnimatorController.name == "hys_Player_Sword";
        bool isShieldAttack = animator.runtimeAnimatorController != null
            && animator.runtimeAnimatorController.name == "hys_Player_Shield";
        float activeComboLinkTime = isAxeAttack
            ? axeComboLinkTime
            : isShieldAttack
                ? shieldComboLinkTime
                : isSwordAttack
                    ? swordComboLinkTime
                    : comboLinkTime;
        // Sword는 두 번째 입력이 들어오면 Attack1 완료를 기다리지 않고 즉시 Attack2를 재생합니다.
        bool canLinkNextAttack = elapsedTime >= activeComboLinkTime;
        if (animationAttackComboStep == 1 && queuedSecondAttackAnimation && canLinkNextAttack)
        {
            StartAttackAnimationStep(2);
            return animationAttackComboStep;
        }

        float currentStepTime = isAxeAttack
            ? (animationAttackComboStep == 1 ? axeFirstAttackAnimationTime : axeSecondAttackAnimationTime)
            : isShieldAttack
                ? (animationAttackComboStep == 1 ? shieldFirstAttackAnimationTime : shieldSecondAttackAnimationTime)
                : isSwordAttack
                    ? (animationAttackComboStep == 1 ? swordFirstAttackAnimationTime : swordSecondAttackAnimationTime)
                    : (animationAttackComboStep == 1 ? firstAttackAnimationTime : secondAttackAnimationTime);
        bool canFinishCurrentAttack = elapsedTime >= currentStepTime;
        if (canFinishCurrentAttack)
        {
            if (queuedRestartAttackAnimation)
            {
                StartAttackAnimationStep(1);
            }
            else
            {
                ResetAttackAnimationCombo();
            }
        }

        return animationAttackComboStep;
    }

    private void StartAttackAnimationStep(int comboStep)
    {
        animationAttackComboStep = comboStep;
        animationAttackStartTime = Time.time;
        queuedSecondAttackAnimation = false;
        queuedRestartAttackAnimation = false;

        PlayAttackAnimationState(comboStep);
    }

    private void PlayAttackAnimationState(int comboStep)
    {
        if (animator == null)
        {
            return;
        }

        // Sword는 중첩된 Attack 상태 머신 전이 순서와 무관하게 정확한 1타/2타 상태를 직접 재생합니다.
        bool isSwordController = animator.runtimeAnimatorController != null
            && animator.runtimeAnimatorController.name == "hys_Player_Sword";
        if (!playAttackStateDirectly && !isSwordController)
        {
            return;
        }

        // 빙의 후 Sword 컨트롤러로 바뀌면 씬에 남은 Shield 상태명 대신 Sword 전용 상태를 사용합니다.
        string stateName = isSwordController
            ? (comboStep == 1 ? swordFirstAttackStateName : swordSecondAttackStateName)
            : (comboStep == 1 ? firstAttackStateName : secondAttackStateName);
        if (string.IsNullOrEmpty(stateName))
        {
            return;
        }

        // 같은 공격 모션을 다시 시작해야 할 때도 첫 프레임부터 재생되게 합니다.
        CrossFadeAnimatorState(stateName, attackTransitionSeconds, 0f);
    }

    private void ResetAttackAnimationCombo()
    {
        animationAttackComboStep = 0;
        animationAttackStartTime = -1f;
        queuedSecondAttackAnimation = false;
        queuedRestartAttackAnimation = false;
    }

    private bool IsAttackInputPressedForAnimation()
    {
        // 현재 hys 공격 입력과 같은 X 키를 애니메이션 콤보 입력으로 사용합니다.
        return Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
    }

    private void UpdateFacing(float horizontalVelocity)
    {
        if (!flipSpriteByMoveDirection || spriteRenderer == null)
        {
            return;
        }

        if (Mathf.Abs(horizontalVelocity) <= moveThreshold)
        {
            return;
        }

        // flipX는 Transform과 Collider 위치를 바꾸지 않고 이미지만 좌우 반전합니다.
        spriteRenderer.flipX = horizontalVelocity < 0f;
    }

    private void TryReturnFromFinishedActionState()
    {
        if (!autoReturnFinishedActionStates || string.IsNullOrEmpty(defaultAnimatorStateName))
        {
            return;
        }

        if (animator.IsInTransition(baseLayerIndex))
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
        if (!IsAutoReturnState(stateInfo))
        {
            return;
        }

        if (stateInfo.normalizedTime < returnNormalizedTime)
        {
            return;
        }

        ResetActionStateToIdle();
        CrossFadeAnimatorState(
            ResolveWeaponStateName("Idle", defaultAnimatorStateName),
            returnTransitionSeconds);
    }

    private void UpdateActionStateTimer(hys_PlayerState currentState)
    {
        if (currentState == previousPlayerState)
        {
            return;
        }

        if (currentState == hys_PlayerState.Hit || currentState == hys_PlayerState.Dash)
        {
            actionStateStartTime = Time.time;
        }
    }

    private void TryReturnFromTimedActionState(hys_PlayerState currentState)
    {
        if (!autoReturnByStateTime || string.IsNullOrEmpty(defaultAnimatorStateName))
        {
            return;
        }

        float returnSeconds = 0f;
        if (currentState == hys_PlayerState.Hit)
        {
            returnSeconds = hitReturnSeconds;
        }
        else if (currentState == hys_PlayerState.Dash)
        {
            returnSeconds = dashReturnSeconds;
        }
        else
        {
            return;
        }

        if (Time.time - actionStateStartTime < returnSeconds)
        {
            return;
        }

        ResetActionStateToIdle();
        CrossFadeAnimatorState(
            ResolveWeaponStateName("Idle", defaultAnimatorStateName),
            returnTransitionSeconds);
    }

    private void ResetActionStateToIdle()
    {
        if (playerState == null)
        {
            return;
        }

        // Hit/Dash 값이 남아 있으면 Any State 조건이 계속 재진입하므로 상태값도 같이 풀어줍니다.
        if (playerState.CurrentState == hys_PlayerState.Hit ||
            playerState.CurrentState == hys_PlayerState.Dash)
        {
            playerState.SetState(hys_PlayerState.Idle);
        }
    }

    private bool IsAutoReturnState(AnimatorStateInfo stateInfo)
    {
        // 현재 무기 컨트롤러의 Dash/Hit 이름을 우선 검사해 무기 교체 뒤에도 자동 복귀가 동작하게 합니다.
        string currentDashStateName = ResolveWeaponStateName("Dash", dashStateName);
        string currentHitStateName = ResolveWeaponStateName("Hit", string.Empty);
        if (stateInfo.shortNameHash == Animator.StringToHash(currentDashStateName)
            || stateInfo.shortNameHash == Animator.StringToHash(currentHitStateName))
        {
            return true;
        }

        if (autoReturnStateNames == null)
        {
            return false;
        }

        for (int i = 0; i < autoReturnStateNames.Length; i++)
        {
            string stateName = autoReturnStateNames[i];
            if (!string.IsNullOrEmpty(stateName)
                && stateInfo.shortNameHash == Animator.StringToHash(stateName))
            {
                return true;
            }
        }

        return false;
    }

    private string ResolveWeaponStateName(string suffix, string fallbackStateName)
    {
        if (string.IsNullOrEmpty(suffix))
        {
            return fallbackStateName;
        }

        // 컨트롤러 이름을 기준으로 상태 접두사를 결정해 Sword/Axe/Shield 전환 시 직렬화된 옛 이름에 묶이지 않게 합니다.
        string controllerName = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : string.Empty;
        string prefix = string.Empty;

        if (controllerName.Contains("Shield"))
        {
            prefix = "hys_Shield";
        }
        else if (controllerName.Contains("Axe"))
        {
            prefix = "hys_Axe";
        }
        else if (controllerName.Contains("Sword"))
        {
            prefix = "hys_Sword";
        }
        else if (controllerName.Contains("Bow"))
        {
            prefix = "hys_Bow";
        }
        else if (controllerName.Contains("Lance"))
        {
            prefix = "hys_Lance";
        }

        return string.IsNullOrEmpty(prefix) ? fallbackStateName : $"{prefix}_{suffix}";
    }

    private void SetAnimatorBool(int parameterHash, bool value)
    {
        if (HasParameter(parameterHash, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private void SetAnimatorFloat(int parameterHash, float value)
    {
        if (HasParameter(parameterHash, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(parameterHash, value);
        }
    }

    private void SetAnimatorInt(int parameterHash, int value)
    {
        if (HasParameter(parameterHash, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(parameterHash, value);
        }
    }

    private void SetAnimatorTrigger(int parameterHash)
    {
        if (HasParameter(parameterHash, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(parameterHash);
            animator.SetTrigger(parameterHash);
        }
    }

    private bool HasParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash && parameters[i].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
