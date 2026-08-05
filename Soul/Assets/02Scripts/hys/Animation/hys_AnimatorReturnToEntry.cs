using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대시, 공격, 피격처럼 한 번만 재생되는 애니메이션이 끝나면 무기별 Idle 상태로 복귀시킵니다.
/// HWJ와 hys 중 어느 시스템이 애니메이션을 시작해도 같은 Animator에서 독립적으로 복귀를 보장합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class hys_AnimatorReturnToEntry : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private int layerIndex;
    [SerializeField, Range(0.5f, 1f)] private float returnNormalizedTime = 0.98f;
    [SerializeField, Min(0f)] private float transitionSeconds = 0.03f;
    [SerializeField] private string anyStateMotionTag = "hys_Pattern";
    [SerializeField] private string fallbackEntryStateName = "";
    [SerializeField] private string[] oneShotStateNames = new string[0];

    private readonly HashSet<int> oneShotStateHashes = new HashSet<int>();
    private RuntimeAnimatorController previousController;
    private bool warnedMissingEntry;

    private void Awake()
    {
        CacheAnimator();
        RebuildStateHashes();
    }

    private void OnEnable()
    {
        CacheAnimator();
        RebuildStateHashes();
        previousController = null;
        warnedMissingEntry = false;
    }

    private void LateUpdate()
    {
        CacheAnimator();
        if (animator == null || animator.runtimeAnimatorController == null ||
            !animator.isActiveAndEnabled || layerIndex < 0 || layerIndex >= animator.layerCount)
        {
            return;
        }

        if (previousController != animator.runtimeAnimatorController)
        {
            previousController = animator.runtimeAnimatorController;
            warnedMissingEntry = false;
        }

        if (animator.IsInTransition(layerIndex))
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool isConfiguredOneShot = oneShotStateHashes.Contains(state.shortNameHash);
        bool isTaggedAnyStateMotion = !string.IsNullOrEmpty(anyStateMotionTag) &&
            state.IsTag(anyStateMotionTag);

        if ((!isConfiguredOneShot && !isTaggedAnyStateMotion) ||
            state.normalizedTime < returnNormalizedTime)
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
            if (!warnedMissingEntry)
            {
                Debug.LogWarning(
                    $"[hys Animator] 복귀할 기본 상태를 찾지 못했습니다: {entryStateName}",
                    this);
                warnedMissingEntry = true;
            }
            return;
        }

        if (state.fullPathHash != entryFullPathHash)
        {
            animator.CrossFade(entryFullPathHash, transitionSeconds, layerIndex, 0f);
        }
    }

    private void CacheAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void RebuildStateHashes()
    {
        oneShotStateHashes.Clear();
        if (oneShotStateNames == null)
        {
            return;
        }

        foreach (string stateName in oneShotStateNames)
        {
            if (!string.IsNullOrWhiteSpace(stateName))
            {
                oneShotStateHashes.Add(Animator.StringToHash(stateName));
            }
        }
    }

    private string ResolveEntryStateName()
    {
        string controllerName = animator.runtimeAnimatorController.name;
        const string playerPrefix = "hys_Player_";
        const string monsterPrefix = "hys_Monster_";

        if (controllerName.StartsWith(playerPrefix))
        {
            string weapon = controllerName.Substring(playerPrefix.Length);
            string weaponIdleState = $"hys_{weapon}_Idle";
            if (HasBaseLayerState(weaponIdleState))
            {
                return weaponIdleState;
            }

            // 기존 4종 컨트롤러는 내부 상태 이름을 hys_Sword_*로 공유하므로 해당 Idle도 지원합니다.
            const string legacySharedIdleState = "hys_Sword_Idle";
            if (HasBaseLayerState(legacySharedIdleState))
            {
                return legacySharedIdleState;
            }
        }

        if (controllerName.StartsWith(monsterPrefix))
        {
            string weapon = controllerName.Substring(monsterPrefix.Length);
            return $"hys_Monster_{weapon}_Idle";
        }

        return fallbackEntryStateName;
    }

    private bool HasBaseLayerState(string stateName)
    {
        int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");
        return animator.HasState(layerIndex, fullPathHash);
    }
}
