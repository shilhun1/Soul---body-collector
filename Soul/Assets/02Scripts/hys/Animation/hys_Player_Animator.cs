using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private HWJ_SoulSystem soulSystem;

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

    [Header("Jump Trigger")]
    // 1단 점프와 2단 점프가 새로 발생할 때 Jump_Start로 다시 보내기 위한 설정입니다.
    [SerializeField] private string jumpTriggerParameter = "JumpTrigger";
    [SerializeField] private bool sendJumpTriggerParameter;
    [SerializeField] private bool crossFadeToJumpStartOnJump;
    [SerializeField] private string jumpStartStateName = "hys_Sword_Jump_Start";
    [SerializeField] private float jumpStartTransitionSeconds = 0.03f;

    [Header("Soul Trigger")]
    // 육신에서 소울 애니메이션으로 넘어가는 순간 한 번만 보냅니다.
    [SerializeField] private string soulTriggerParameter = "SoulTrigger";

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
    private bool wasSoulState;
    private hys_PlayerState previousPlayerState = hys_PlayerState.Idle;
    private float actionStateStartTime;
    private int animationAttackComboStep;
    private float animationAttackStartTime = -1f;
    private bool queuedSecondAttackAnimation;
    private bool queuedRestartAttackAnimation;

    private int playerStateHash;
    private int isMovingHash;
    private int isGroundedHash;
    private int isSoulHash;
    private int attackComboStepHash;
    private int horizontalSpeedHash;
    private int verticalSpeedHash;
    private int jumpTriggerHash;
    private int soulTriggerHash;

    private void Awake()
    {
        CacheReferences();
        CacheParameterHashes();
        ResetJumpVersion();
        wasSoulState = IsSoulState();
    }

    private void OnEnable()
    {
        CacheReferences();
        ResetJumpVersion();
        wasSoulState = IsSoulState();
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
        hys_PlayerState currentState = playerState != null ? playerState.CurrentState : hys_PlayerState.Idle;
        int attackComboStep = UpdateAttackAnimationCombo(currentState);

        SetAnimatorInt(playerStateHash, (int)currentState);
        SetAnimatorInt(attackComboStepHash, attackComboStep);
        SetAnimatorBool(isMovingHash, isMoving);
        SetAnimatorBool(isGroundedHash, isGrounded);
        SetAnimatorBool(isSoulHash, isSoulState);
        SetAnimatorFloat(horizontalSpeedHash, Mathf.Abs(horizontalVelocity));
        SetAnimatorFloat(verticalSpeedHash, verticalVelocity);

        UpdateJumpTrigger();
        UpdateSoulTrigger(isSoulState, currentState);
        UpdateFacing(horizontalVelocity);
        UpdateActionStateTimer(currentState);
        TryReturnFromFinishedActionState();
        TryReturnFromTimedActionState(currentState);

        previousPlayerState = currentState;
        wasSoulState = isSoulState;
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

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }
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
            animator.CrossFade(jumpStartStateName, jumpStartTransitionSeconds, 0);
        }
    }

    private void UpdateSoulTrigger(bool isSoulState, hys_PlayerState currentState)
    {
        // 사망 애니메이션 중에는 SoulTrigger가 Die 모션을 끊지 않게 막습니다.
        if (currentState == hys_PlayerState.Dead)
        {
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
        if (currentState == hys_PlayerState.Attack &&
            animationAttackComboStep == 0 &&
            (previousPlayerState != hys_PlayerState.Attack || attackPressed))
        {
            // Attack 상태가 유지된 채 2타가 끝난 뒤에도 새 입력이 오면 다시 1타부터 시작합니다.
            StartAttackAnimationStep(1);
        }

        if (animationAttackComboStep == 0)
        {
            return 0;
        }

        float elapsedTime = Time.time - animationAttackStartTime;
        if (animationAttackComboStep == 1 && attackPressed)
        {
            // 연타 감각을 위해 2타 입력은 시간창을 기다리지 않고 바로 예약합니다.
            queuedSecondAttackAnimation = true;
        }
        else if (animationAttackComboStep == 2 && attackPressed)
        {
            // 2타 중 입력은 다음 1타로 이어지게 해서 공격이 뚝 끊기지 않게 합니다.
            queuedRestartAttackAnimation = true;
        }

        if (animationAttackComboStep == 1 && queuedSecondAttackAnimation && elapsedTime >= comboLinkTime)
        {
            StartAttackAnimationStep(2);
            return animationAttackComboStep;
        }

        float currentStepTime = animationAttackComboStep == 1 ? firstAttackAnimationTime : secondAttackAnimationTime;
        if (elapsedTime >= currentStepTime)
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
        if (!playAttackStateDirectly || animator == null)
        {
            return;
        }

        string stateName = comboStep == 1 ? firstAttackStateName : secondAttackStateName;
        if (string.IsNullOrEmpty(stateName))
        {
            return;
        }

        // 같은 공격 모션을 다시 시작해야 할 때도 첫 프레임부터 재생되게 합니다.
        animator.CrossFade(stateName, attackTransitionSeconds, baseLayerIndex, 0f);
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
        animator.CrossFade(defaultAnimatorStateName, returnTransitionSeconds, baseLayerIndex);
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
        animator.CrossFade(defaultAnimatorStateName, returnTransitionSeconds, baseLayerIndex);
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
        if (autoReturnStateNames == null)
        {
            return false;
        }

        for (int i = 0; i < autoReturnStateNames.Length; i++)
        {
            string stateName = autoReturnStateNames[i];
            if (!string.IsNullOrEmpty(stateName) && stateInfo.IsName(stateName))
            {
                return true;
            }
        }

        return false;
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
