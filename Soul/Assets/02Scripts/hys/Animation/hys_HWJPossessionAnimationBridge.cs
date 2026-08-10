using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HWJ 빙의 성공 이벤트를 받아 유령의 Possession 모션을 끝까지 재생한 뒤 육체 Controller로 넘깁니다.
/// 빙의 해제 시에는 저장한 유령 Controller를 복구하므로 씬별 Animator 설정이 필요하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-900)]
public sealed class hys_HWJPossessionAnimationBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private hys_PossessionAnimationLibrary possessionAnimationLibrary;
    [SerializeField, Range(0.5f, 1f)] private float handoffNormalizedTime = 0.98f;
    [SerializeField, Min(0.1f)] private float fallbackHandoffSeconds = 1f;

    private RuntimeAnimatorController ghostController;
    private AnimatorOverrideController activePossessionController;
    private bool possessionAnimationPlaying;
    private bool soulExitAnimationPlaying;
    private bool restoreMotionSystemEnabled;
    private float possessionStartedAt;
    private float activeFallbackHandoffSeconds;
    private HWJ_WeaponType lastPossessedWeaponType = HWJ_WeaponType.None;

    // 다른 hys 애니메이션 브리지가 빙의 모션을 덮어쓰지 않도록 현재 재생 여부를 공유합니다.
    public bool IsPossessionAnimationPlaying => possessionAnimationPlaying || soulExitAnimationPlaying;

    private void Awake()
    {
        CacheReferences();
        CacheGhostController();
    }

    private void OnEnable()
    {
        CacheReferences();
        CacheGhostController();

        HWJ_GameplayEvents.PossessionChanged += OnPossessionChanged;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.PossessionChanged -= OnPossessionChanged;
        FinishPossessionAnimation();
    }

    private void Update()
    {
        CacheReferences();
        CacheGhostController();
        if (!IsPossessionAnimationPlaying || animator == null)
        {
            return;
        }

        bool timedOut = Time.unscaledTime - possessionStartedAt >= activeFallbackHandoffSeconds;
        bool finishedState = false;
        if (!animator.IsInTransition(0))
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            finishedState = state.shortNameHash == Animator.StringToHash("Possession")
                && state.normalizedTime >= handoffNormalizedTime;
        }

        if (finishedState || timedOut)
        {
            FinishPossessionAnimation();
        }
    }

    private void OnPossessionChanged(HWJ_PossessionEvent possessionEvent)
    {
        if (possessionEvent.PossessionSystem != possessionSystem)
        {
            return;
        }

        if (possessionEvent.Possessed)
        {
            HWJ_WeaponType weaponType = possessionEvent.BodyResolver != null
                ? possessionEvent.BodyResolver.WeaponType
                : possessionSystem.CurrentWeaponType;
            PlayPossessionAnimation(weaponType);
        }
        else
        {
            HWJ_WeaponType weaponType = possessionEvent.BodyResolver != null
                ? possessionEvent.BodyResolver.WeaponType
                : lastPossessedWeaponType;
            PlaySoulExitAnimation(weaponType);
        }
    }

    private void PlayPossessionAnimation(HWJ_WeaponType weaponType)
    {
        CacheReferences();
        CacheGhostController();
        if (animator == null || ghostController == null)
        {
            return;
        }

        lastPossessedWeaponType = weaponType;
        AnimationClip possessionClip = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetPossessionClip(weaponType)
            : null;
        activePossessionController = CreatePossessionOverrideController(possessionClip);
        if (activePossessionController == null)
        {
            // 무기 전용 클립이 없을 때 Sword 기본 모션을 잘못 보여주지 않고 즉시 육신 모션으로 넘깁니다.
            Debug.LogWarning($"[hys Animator] {weaponType} 전용 빙의 클립을 찾지 못해 공용 Sword 모션을 재생하지 않습니다.", this);
            if (motionSystem != null && !motionSystem.enabled)
            {
                motionSystem.enabled = true;
            }
            ApplyPlayerController(weaponType);
            return;
        }

        restoreMotionSystemEnabled = motionSystem != null && motionSystem.enabled;
        if (motionSystem != null)
        {
            // HWJ가 같은 프레임에 육체 Controller로 교체하지 않도록 빙의 모션 동안만 애니메이션 갱신을 보류합니다.
            motionSystem.enabled = false;
        }

        if (animator.runtimeAnimatorController != activePossessionController)
        {
            animator.runtimeAnimatorController = activePossessionController;
            animator.Rebind();
            animator.Update(0f);
        }

        int stateHash = Animator.StringToHash("Base Layer.Possession");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);

            // Sword 빙의 클립처럼 1초보다 긴 모션도 중간에 잘리지 않도록 실제 상태 길이를 사용합니다.
            AnimatorStateInfo possessionState = animator.GetCurrentAnimatorStateInfo(0);
            activeFallbackHandoffSeconds = Mathf.Max(
                fallbackHandoffSeconds,
                possessionState.length * handoffNormalizedTime + 0.05f);
        }
        else
        {
            SetTriggerIfPresent("Possess");
            activeFallbackHandoffSeconds = fallbackHandoffSeconds;
        }

        possessionAnimationPlaying = true;
        soulExitAnimationPlaying = false;
        possessionStartedAt = Time.unscaledTime;
        Debug.Log($"[hys Animator] Possession clip -> {possessionClip.name}, Weapon={weaponType}", this);
    }

    private void PlaySoulExitAnimation(HWJ_WeaponType weaponType)
    {
        CacheReferences();
        CacheGhostController();
        if (animator == null || ghostController == null)
        {
            return;
        }

        if (weaponType == HWJ_WeaponType.None)
        {
            weaponType = lastPossessedWeaponType;
        }

        AnimationClip soulExitClip = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetSoulExitClip(weaponType)
            : null;
        activePossessionController = CreatePossessionOverrideController(soulExitClip);
        if (activePossessionController == null)
        {
            Debug.LogWarning($"[hys Animator] {weaponType} 전용 빙의 해제 클립을 찾지 못했습니다.", this);
            RestoreGhostStanding();
            return;
        }

        restoreMotionSystemEnabled = motionSystem != null && motionSystem.enabled;
        if (motionSystem != null)
        {
            motionSystem.enabled = false;
        }

        animator.runtimeAnimatorController = activePossessionController;
        animator.Rebind();
        animator.Update(0f);

        int stateHash = Animator.StringToHash("Base Layer.Possession");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
            AnimatorStateInfo exitState = animator.GetCurrentAnimatorStateInfo(0);
            activeFallbackHandoffSeconds = Mathf.Max(
                fallbackHandoffSeconds,
                exitState.length * handoffNormalizedTime + 0.05f);
        }
        else
        {
            activeFallbackHandoffSeconds = fallbackHandoffSeconds;
        }

        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = true;
        possessionStartedAt = Time.unscaledTime;
        Debug.Log($"[hys Animator] Soul exit clip -> {soulExitClip.name}, Weapon={weaponType}", this);
    }

    private AnimatorOverrideController CreatePossessionOverrideController(AnimationClip replacementClip)
    {
        if (replacementClip == null || ghostController == null)
        {
            return null;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(ghostController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip originalClip = overrides[i].Key;
            if (originalClip == null || originalClip.name != "hys_Ghost_possession")
            {
                continue;
            }

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, replacementClip);
            overrideController.ApplyOverrides(overrides);
            return overrideController;
        }

        return null;
    }

    private void FinishPossessionAnimation()
    {
        if (!IsPossessionAnimationPlaying)
        {
            return;
        }

        bool finishedSoulExit = soulExitAnimationPlaying;
        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = false;
        activePossessionController = null;

        if (finishedSoulExit)
        {
            RestoreGhostStanding();
        }
        else
        {
            ApplyPlayerController(lastPossessedWeaponType);
        }

        if (motionSystem != null && restoreMotionSystemEnabled)
        {
            motionSystem.enabled = true;
        }
    }

    private void RestoreGhostStanding()
    {
        CacheReferences();
        if (animator == null || ghostController == null)
        {
            return;
        }

        animator.runtimeAnimatorController = ghostController;
        animator.Rebind();
        animator.Update(0f);
        int standingHash = Animator.StringToHash("Base Layer.Standing");
        if (animator.HasState(0, standingHash))
        {
            // 육체의 Soul 모션이 이미 해제 연출을 끝냈으므로 Ghost 등장 모션을 중복 재생하지 않습니다.
            animator.Play(standingHash, 0, 0f);
            animator.Update(0f);
        }
    }

    private void ApplyPlayerController(HWJ_WeaponType weaponType)
    {
        RuntimeAnimatorController playerController = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetPlayerController(weaponType)
            : null;
        if (animator == null || playerController == null)
        {
            return;
        }

        animator.runtimeAnimatorController = playerController;
        animator.Rebind();
        animator.Update(0f);

        string weaponName = ResolveWeaponName(weaponType);
        int idleHash = Animator.StringToHash($"Base Layer.hys_{weaponName}_Idle");
        if (animator.HasState(0, idleHash))
        {
            animator.Play(idleHash, 0, 0f);
            animator.Update(0f);
        }
    }

    private static string ResolveWeaponName(HWJ_WeaponType weaponType)
    {
        switch (weaponType)
        {
            case HWJ_WeaponType.Axe:
                return "Axe";
            case HWJ_WeaponType.Bow:
                return "Bow";
            case HWJ_WeaponType.Lance:
                return "Lance";
            case HWJ_WeaponType.Shield:
                return "Shield";
            default:
                return "Sword";
        }
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (possessionAnimationLibrary == null)
        {
            // Resources의 공용 표를 사용해 씬별 인스펙터 설정 없이 같은 무기별 빙의 모션을 사용합니다.
            possessionAnimationLibrary = Resources.Load<hys_PossessionAnimationLibrary>(
                "hys_PossessionAnimationLibrary");
        }
    }

    private void CacheGhostController()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        if (currentController is AnimatorOverrideController overrideController)
        {
            // 재생 중인 무기별 Override가 아니라 원본 Ghost Controller만 보관합니다.
            RuntimeAnimatorController baseController = overrideController.runtimeAnimatorController;
            if (baseController != null
                && baseController.name.StartsWith("hys_Ghost", System.StringComparison.Ordinal))
            {
                ghostController = baseController;
            }

            return;
        }

        if (currentController.name.StartsWith("hys_Ghost", System.StringComparison.Ordinal))
        {
            ghostController = currentController;
        }
    }

    private void SetTriggerIfPresent(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                animator.ResetTrigger(parameter.nameHash);
                animator.SetTrigger(parameter.nameHash);
                return;
            }
        }
    }
}
