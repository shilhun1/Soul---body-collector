using System.Collections;
using UnityEngine;

/// <summary>
/// HWJ의 실제 이동·접지·공격 상태를 공용 hys Animator 상태로 직접 연결합니다.
/// 예전 PlayerState 파라미터에 의존하지 않아 모든 씬에서 같은 방식으로 동작합니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(700)]
public sealed class hys_HWJBasicAttackAnimationBridge : MonoBehaviour
{
    private const string PlayerControllerPrefix = "hys_Player_";
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HWJ_PlayerMovementSystem playerMovement;
    [SerializeField] private HWJ_PlayerAttackSystem attackSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private hys_Player_Animator playerAnimator;
    [SerializeField] private hys_HWJPossessionAnimationBridge possessionAnimationBridge;
    [SerializeField] private int layerIndex;
    [SerializeField, Min(0f)] private float transitionSeconds = 0.02f;
    [SerializeField, Min(0f)] private float controllerWaitSeconds = 0.25f;
    [SerializeField, Min(0f)] private float moveThreshold = 0.05f;
    [SerializeField, Min(0f)] private float apexVelocityThreshold = 1.25f;

    private Coroutine pendingAttackRoutine;
    private HWJ_RuntimeState previousRuntimeState = HWJ_RuntimeState.None;
    private int lastAttackStep = 1;
    private int lastAttackPlayFrame = -1;
    private RuntimeAnimatorController cachedController;
    private RuntimeAnimatorController cachedPossessedPlayerController;
    private HWJ_WeaponType cachedPossessedWeaponType = HWJ_WeaponType.None;
    private string weaponToken = "Sword";

    private void Awake()
    {
        CacheReferences();
        previousRuntimeState = runtimeStatus != null
            ? runtimeStatus.CurrentState
            : HWJ_RuntimeState.None;
    }

    private void OnEnable()
    {
        CacheReferences();
        HWJ_GameplayEvents.AbilityUsed += OnAbilityUsed;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;

        if (pendingAttackRoutine != null)
        {
            StopCoroutine(pendingAttackRoutine);
            pendingAttackRoutine = null;
        }
    }

    private void LateUpdate()
    {
        CacheReferences();
        if (IsPossessionAnimationPlaying())
        {
            // 유령 빙의 모션이 끝날 때까지 Controller 교체와 이동 상태 재생을 보류합니다.
            return;
        }

        EnsurePossessedPlayerController();
        if (!CanDriveAnimator())
        {
            return;
        }

        HWJ_RuntimeState currentRuntimeState = runtimeStatus != null
            ? runtimeStatus.CurrentState
            : HWJ_RuntimeState.None;

        // 이벤트 연결이 누락된 경우에도 Attack 진입 순간 공격 모션을 한 번 보장합니다.
        if (currentRuntimeState == HWJ_RuntimeState.Attack)
        {
            if (previousRuntimeState != HWJ_RuntimeState.Attack
                && lastAttackPlayFrame != Time.frameCount)
            {
                TryPlayAttack(lastAttackStep);
            }

            previousRuntimeState = currentRuntimeState;
            return;
        }

        previousRuntimeState = currentRuntimeState;
        if (currentRuntimeState == HWJ_RuntimeState.Hit
            || currentRuntimeState == HWJ_RuntimeState.Dead)
        {
            return;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool isTransitioningToProtectedMotion = animator.IsInTransition(layerIndex)
            && IsProtectedOneShot(animator.GetNextAnimatorStateInfo(layerIndex));
        if (IsProtectedOneShot(currentState) || isTransitioningToProtectedMotion)
        {
            return;
        }

        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
        bool isGrounded = playerMovement != null && playerMovement.IsGrounded;
        string idleState = GetWeaponStateName("Idle");
        string runState = GetWeaponStateName("Run");
        string jumpStartState = GetWeaponStateName("Jump_Start");
        string jumpApexState = GetWeaponStateName("Jump_Apex");
        string jumpFallState = GetWeaponStateName("Jump_Fall");
        string desiredState;

        if (!isGrounded)
        {
            desiredState = velocity.y > apexVelocityThreshold
                ? jumpStartState
                : velocity.y >= -apexVelocityThreshold
                    ? jumpApexState
                    : jumpFallState;
        }
        else
        {
            // HWJ는 입력이 들어오면 RuntimeState.Move를 먼저 기록하므로 저속 가속 구간도 즉시 걷기로 표시합니다.
            bool isMoving = currentRuntimeState == HWJ_RuntimeState.Move
                || Mathf.Abs(velocity.x) > moveThreshold;
            desiredState = isMoving ? runState : idleState;
        }

        PlayLocomotionIfChanged(currentState, desiredState);
    }

    private void OnAbilityUsed(HWJ_AbilityUsedEvent abilityEvent)
    {
        if (!abilityEvent.IsBasicAttack || abilityEvent.User != attackSystem)
        {
            return;
        }

        // 콤보 값이 없는 기본 공격은 1타로, 이후 값은 1타/2타 순환으로 정규화합니다.
        lastAttackStep = abilityEvent.ComboStep <= 1
            ? 1
            : ((abilityEvent.ComboStep - 1) % 2) + 1;

        if (pendingAttackRoutine != null)
        {
            StopCoroutine(pendingAttackRoutine);
        }

        pendingAttackRoutine = StartCoroutine(PlayAttackWhenControllerIsReady(lastAttackStep));
    }

    private IEnumerator PlayAttackWhenControllerIsReady(int attackStep)
    {
        float deadline = Time.unscaledTime + controllerWaitSeconds;

        do
        {
            CacheReferences();
            if (TryPlayAttack(attackStep))
            {
                pendingAttackRoutine = null;
                yield break;
            }

            yield return null;
        }
        while (Time.unscaledTime <= deadline);

        pendingAttackRoutine = null;
    }

    private bool TryPlayAttack(int attackStep)
    {
        if (!CanDriveAnimator())
        {
            return false;
        }

        // HWJ가 하위 StateMachine을 놓치지 않도록 최상위 런타임 공격 상태를 재생합니다.
        string statePath = $"Base Layer.{GetWeaponStateName($"Attack{attackStep}")}";
        animator.CrossFadeInFixedTime(statePath, transitionSeconds, layerIndex, 0f);
        lastAttackPlayFrame = Time.frameCount;
        return true;
    }

    private void PlayLocomotionIfChanged(AnimatorStateInfo currentState, string stateName)
    {
        int shortNameHash = Animator.StringToHash(stateName);
        if (currentState.shortNameHash == shortNameHash)
        {
            return;
        }

        // 이미 원하는 상태로 전환 중이면 CrossFade를 다시 시작하지 않아 전환 완료를 보장합니다.
        if (animator.IsInTransition(layerIndex)
            && animator.GetNextAnimatorStateInfo(layerIndex).shortNameHash == shortNameHash)
        {
            return;
        }

        animator.CrossFadeInFixedTime($"Base Layer.{stateName}", transitionSeconds, layerIndex, 0f);
    }

    private bool IsProtectedOneShot(AnimatorStateInfo state)
    {
        return state.shortNameHash == Animator.StringToHash(GetWeaponStateName("Attack1"))
            || state.shortNameHash == Animator.StringToHash(GetWeaponStateName("Attack2"))
            || state.shortNameHash == Animator.StringToHash(GetWeaponStateName("Dash"))
            || state.shortNameHash == Animator.StringToHash(GetWeaponStateName("Hit"))
            || state.shortNameHash == Animator.StringToHash(GetWeaponStateName("Die"))
            || state.shortNameHash == Animator.StringToHash(GetWeaponStateName("Soul"))
            || state.shortNameHash == Animator.StringToHash($"PlayerSkill_{weaponToken}_ReapSlash")
            || state.shortNameHash == Animator.StringToHash($"PlayerSkill_{weaponToken}_DashSlash")
            || state.shortNameHash == Animator.StringToHash($"PlayerSkill_{weaponToken}_ForceSlash")
            || state.shortNameHash == Animator.StringToHash($"PlayerSkill_{weaponToken}_FinalSlash")
            || state.IsTag("hys_Pattern");
    }

    private bool CanDriveAnimator()
    {
        bool canDrive = animator != null
            && animator.isActiveAndEnabled
            && animator.runtimeAnimatorController != null
            && animator.runtimeAnimatorController.name.StartsWith(
                PlayerControllerPrefix,
                System.StringComparison.Ordinal)
            && layerIndex >= 0
            && layerIndex < animator.layerCount;

        if (canDrive)
        {
            RefreshWeaponToken();
        }

        return canDrive;
    }

    private void RefreshWeaponToken()
    {
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (cachedController == controller)
        {
            return;
        }

        cachedController = controller;
        string controllerName = controller.name;
        weaponToken = controllerName.Length > PlayerControllerPrefix.Length
            ? controllerName.Substring(PlayerControllerPrefix.Length)
            : "Sword";
    }

    private string GetWeaponStateName(string suffix)
    {
        return $"hys_{weaponToken}_{suffix}";
    }

    private void EnsurePossessedPlayerController()
    {
        if (animator == null
            || possessionSystem == null
            || !possessionSystem.HasActivePossessedBody)
        {
            return;
        }

        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        HWJ_WeaponType weaponType = possessionSystem.CurrentWeaponType;

        if (currentController != null
            && currentController.name.StartsWith(PlayerControllerPrefix, System.StringComparison.Ordinal))
        {
            cachedPossessedPlayerController = currentController;
            cachedPossessedWeaponType = weaponType;
            return;
        }

        RuntimeAnimatorController playerController = cachedPossessedWeaponType == weaponType
            ? cachedPossessedPlayerController
            : null;

        if (playerController == null)
        {
            // HWJ 빙의 시 복사된 몬스터 컨트롤러 대신 현재 무기의 플레이어 모션 프로필을 찾습니다.
            HWJ_MotionProfileSO[] profiles = Resources.FindObjectsOfTypeAll<HWJ_MotionProfileSO>();
            for (int i = 0; i < profiles.Length; i++)
            {
                HWJ_MotionProfileSO profile = profiles[i];
                RuntimeAnimatorController candidate = profile != null
                    ? profile.AnimatorController
                    : null;

                if (profile != null
                    && profile.WeaponType == weaponType
                    && candidate != null
                    && candidate.name.StartsWith(PlayerControllerPrefix, System.StringComparison.Ordinal))
                {
                    playerController = candidate;
                    break;
                }
            }
        }

        if (playerController == null)
        {
            return;
        }

        // 빙의 대상이 복사한 몬스터 컨트롤러를 해당 무기의 플레이어 컨트롤러로 전환합니다.
        animator.runtimeAnimatorController = playerController;
        cachedPossessedPlayerController = playerController;
        cachedPossessedWeaponType = weaponType;
        cachedController = null;

        Debug.Log(
            $"[hys Animator] 빙의 플레이어 컨트롤러 전환: {currentController?.name ?? "None"} -> {playerController.name}",
            this);
    }

    private void CacheReferences()
    {
        if (attackSystem == null)
        {
            attackSystem = GetComponent<HWJ_PlayerAttackSystem>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<HWJ_PlayerMovementSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<hys_Player_Animator>();
        }

        if (possessionAnimationBridge == null)
        {
            possessionAnimationBridge = GetComponent<hys_HWJPossessionAnimationBridge>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private bool IsPossessionAnimationPlaying()
    {
        return (playerAnimator != null && playerAnimator.IsPossessionTransitionPlaying)
            || (possessionAnimationBridge != null
                && possessionAnimationBridge.IsPossessionAnimationPlaying);
    }
}
