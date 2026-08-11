using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HWJ가 재생한 일회성 hys 모션이 끝나면 무기별 Entry 허브로 복귀시킵니다.
/// 점프 낙하는 지상에 닿기 전에는 복귀하지 않고, 사망 모션은 마지막 프레임을 유지합니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public sealed class hys_AnimatorReturnToEntry : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_PlayerMovementSystem playerMovement;
    [SerializeField] private int layerIndex;
    [SerializeField, Range(0.5f, 1f)] private float returnNormalizedTime = 0.98f;
    [SerializeField, Min(0f)] private float transitionSeconds = 0.03f;
    [SerializeField] private string anyStateMotionTag = "hys_Pattern";
    [SerializeField] private string fallbackEntryStateName = "";
    [SerializeField] private string[] oneShotStateNames = new string[0];

    private readonly HashSet<int> configuredOneShotHashes = new HashSet<int>();
    private RuntimeAnimatorController previousController;
    private bool warnedMissingEntry;

    private void Awake()
    {
        CacheReferences();
        RebuildStateHashes();
    }

    private void OnEnable()
    {
        CacheReferences();
        RebuildStateHashes();
        previousController = null;
        warnedMissingEntry = false;
    }

    private void LateUpdate()
    {
        CacheReferences();
        if (!CanInspectAnimator())
        {
            return;
        }

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (previousController != controller)
        {
            previousController = controller;
            warnedMissingEntry = false;
        }

        // 유령 Controller의 빙의 연출은 전용 브리지가 완료 시점을 관리합니다.
        if (!controller.name.StartsWith("hys_Player_", System.StringComparison.Ordinal))
        {
            return;
        }

        if (animator.IsInTransition(layerIndex))
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layerIndex);
        string stateName = ResolveCurrentStateName(state);
        if (string.IsNullOrEmpty(stateName) || IsDeathState(stateName))
        {
            return;
        }

        bool isJumpFall = stateName.EndsWith("_Jump_Fall", System.StringComparison.Ordinal);
        if (isJumpFall && !IsGrounded())
        {
            return;
        }

        bool isConfiguredOneShot = configuredOneShotHashes.Contains(state.shortNameHash);
        bool isTaggedOneShot = !string.IsNullOrEmpty(anyStateMotionTag) && state.IsTag(anyStateMotionTag);
        bool isKnownOneShot = IsKnownOneShotState(stateName);
        if (!isJumpFall && !isConfiguredOneShot && !isTaggedOneShot && !isKnownOneShot)
        {
            return;
        }

        if (!isJumpFall && state.normalizedTime < returnNormalizedTime)
        {
            return;
        }

        string entryStateName = ResolveEntryStateName();
        if (string.IsNullOrEmpty(entryStateName))
        {
            return;
        }

        int entryFullPathHash = Animator.StringToHash($"Base Layer.{entryStateName}");
        if (!animator.HasState(layerIndex, entryFullPathHash))
        {
            WarnMissingEntry(entryStateName);
            return;
        }

        ResetConsumedTrigger(stateName);
        if (state.fullPathHash != entryFullPathHash)
        {
            animator.CrossFade(entryFullPathHash, transitionSeconds, layerIndex, 0f);
        }
    }

    private bool CanInspectAnimator()
    {
        return animator != null
            && animator.runtimeAnimatorController != null
            && animator.isActiveAndEnabled
            && layerIndex >= 0
            && layerIndex < animator.layerCount;
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<HWJ_PlayerMovementSystem>();
        }
    }

    private void RebuildStateHashes()
    {
        configuredOneShotHashes.Clear();
        if (oneShotStateNames == null)
        {
            return;
        }

        foreach (string stateName in oneShotStateNames)
        {
            if (!string.IsNullOrWhiteSpace(stateName))
            {
                configuredOneShotHashes.Add(Animator.StringToHash(stateName));
            }
        }
    }

    private string ResolveCurrentStateName(AnimatorStateInfo state)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return string.Empty;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            // 상태 이름과 클립 이름이 다른 경우가 있으므로 아래 자동 규칙은 해시 비교를 우선합니다.
            if (clip != null && state.IsName(clip.name))
            {
                return clip.name;
            }
        }

        string weapon = ResolveWeaponToken();
        if (string.IsNullOrEmpty(weapon))
        {
            return string.Empty;
        }

        string[] candidates =
        {
            $"hys_{weapon}_Attack1",
            $"hys_{weapon}_Attack2",
            $"hys_{weapon}_Dash",
            $"hys_{weapon}_Hit",
            $"hys_{weapon}_Die",
            $"hys_{weapon}_Jump_Fall",
            $"hys_{weapon}_Plunge_Land"
        };

        foreach (string candidate in candidates)
        {
            if (state.shortNameHash == Animator.StringToHash(candidate))
            {
                return candidate;
            }
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger
                && parameter.name.StartsWith("PlayerSkill_", System.StringComparison.Ordinal)
                && state.shortNameHash == Animator.StringToHash(parameter.name))
            {
                return parameter.name;
            }
        }

        return string.Empty;
    }

    private static bool IsKnownOneShotState(string stateName)
    {
        return stateName.EndsWith("_Attack1", System.StringComparison.Ordinal)
            || stateName.EndsWith("_Attack2", System.StringComparison.Ordinal)
            || stateName.EndsWith("_Dash", System.StringComparison.Ordinal)
            || stateName.EndsWith("_Hit", System.StringComparison.Ordinal)
            || stateName.EndsWith("_Plunge_Land", System.StringComparison.Ordinal)
            || stateName.StartsWith("PlayerSkill_", System.StringComparison.Ordinal);
    }

    private static bool IsDeathState(string stateName)
    {
        return stateName.EndsWith("_Die", System.StringComparison.Ordinal)
            || stateName.EndsWith("_Death", System.StringComparison.Ordinal)
            || stateName == "Die";
    }

    private bool IsGrounded()
    {
        if (playerMovement != null)
        {
            return playerMovement.IsGrounded;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == "IsGrounded")
            {
                return animator.GetBool(parameter.nameHash);
            }
        }

        return false;
    }

    private string ResolveEntryStateName()
    {
        string weapon = ResolveWeaponToken();
        if (!string.IsNullOrEmpty(weapon))
        {
            string entryState = $"hys_{weapon}_Entry";
            if (HasBaseLayerState(entryState))
            {
                return entryState;
            }

            string idleState = $"hys_{weapon}_Idle";
            if (HasBaseLayerState(idleState))
            {
                return idleState;
            }
        }

        return fallbackEntryStateName;
    }

    private string ResolveWeaponToken()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return string.Empty;
        }

        const string prefix = "hys_Player_";
        string controllerName = animator.runtimeAnimatorController.name;
        return controllerName.StartsWith(prefix, System.StringComparison.Ordinal)
            ? controllerName.Substring(prefix.Length)
            : string.Empty;
    }

    private bool HasBaseLayerState(string stateName)
    {
        return animator.HasState(layerIndex, Animator.StringToHash($"Base Layer.{stateName}"));
    }

    private void ResetConsumedTrigger(string stateName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == stateName)
            {
                animator.ResetTrigger(parameter.nameHash);
                return;
            }
        }
    }

    private void WarnMissingEntry(string entryStateName)
    {
        if (warnedMissingEntry)
        {
            return;
        }

        Debug.LogWarning($"[hys Animator] 복귀할 Entry 상태를 찾지 못했습니다: {entryStateName}", this);
        warnedMissingEntry = true;
    }
}
