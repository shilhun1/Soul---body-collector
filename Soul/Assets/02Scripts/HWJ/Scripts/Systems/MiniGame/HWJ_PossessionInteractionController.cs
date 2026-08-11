using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 유령 상태에서 빙의 입력과 주변 대상 탐색을 담당합니다.
///
/// R:
/// 살아 있는 몬스터를 찾고 생체 빙의 미니게임을 시작합니다.
///
/// E:
/// 죽은 몬스터를 찾고 즉시 시체 빙의를 시도합니다.
///
/// 기존 HWJ_PlayerInputSystem에 연결할 수도 있고,
/// 이 컴포넌트가 키보드를 직접 읽도록 사용할 수도 있습니다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Systems/Possession Interaction Controller")]
public sealed class HWJ_PossessionInteractionController : MonoBehaviour
{
    [Header("핵심 참조")]
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessionTargetValidator targetValidator;
    [SerializeField] private HWJ_LivePossessionMinigameController minigameController;

    [Space(8f)]
    [Header("대상 탐색")]
    [SerializeField] private Transform detectionOrigin;
    [SerializeField, Min(0.1f)] private float detectionRadius = 2f;
    [SerializeField] private LayerMask targetLayerMask = ~0;

    [Tooltip("켜면 이 스크립트가 R/E 키를 직접 읽습니다. 기존 입력 시스템에서 호출할 경우 끄십시오.")]
    [SerializeField] private bool readKeyboardDirectly = true;

    [Space(8f)]
    [Header("디버그")]
    [SerializeField] private HWJ_RootObjectDataResolver lastLiveTarget;
    [SerializeField] private HWJ_RootObjectDataResolver lastCorpseTarget;
    [SerializeField] private string runtimeMessage;
    [SerializeField] private bool drawDetectionRange = true;

    private readonly HashSet<int> uniqueResolverIds = new HashSet<int>();

    public HWJ_RootObjectDataResolver LastLiveTarget => lastLiveTarget;
    public HWJ_RootObjectDataResolver LastCorpseTarget => lastCorpseTarget;
    public string RuntimeMessage => runtimeMessage;

    private void Reset()
    {
        ResolveReferences();
        detectionOrigin = transform;
    }

    private void Awake()
    {
        ResolveReferences();

        if (detectionOrigin == null)
        {
            detectionOrigin = transform;
        }
    }

    private void Update()
    {
        if (!readKeyboardDirectly)
        {
            return;
        }

        if (minigameController != null && minigameController.IsRunning)
        {
            return;
        }

        if (WasLivePossessionPressed())
        {
            TryStartLivePossessionFromInput();
            return;
        }

        if (WasCorpsePossessionPressed())
        {
            TryPossessCorpseFromInput();
        }
    }

    /// <summary>
    /// R 입력 시 호출합니다.
    /// 주변에서 가장 가까운 살아 있는 몬스터를 찾은 뒤 미니게임을 시작합니다.
    /// </summary>
    public bool TryStartLivePossessionFromInput()
    {
        ResolveReferences();

        if (possessionSystem == null || targetValidator == null)
        {
            runtimeMessage =
                "생체 빙의 시작 실패: PossessionSystem 또는 TargetValidator가 없습니다.";
            Debug.LogWarning(runtimeMessage, this);
            return false;
        }

        if (minigameController == null)
        {
            runtimeMessage =
                "생체 빙의 시작 실패: LivePossessionMinigameController가 없습니다.";
            Debug.LogWarning(runtimeMessage, this);
            return false;
        }

        lastLiveTarget = FindNearestTarget(requireLiveTarget: true);

        if (lastLiveTarget == null)
        {
            runtimeMessage = "주변에 살아 있는 빙의 대상이 없습니다.";
            return false;
        }

        if (!possessionSystem.CanStartLivePossessionMinigame(lastLiveTarget))
        {
            runtimeMessage = string.IsNullOrWhiteSpace(possessionSystem.LastPossessionResult)
                ? "생체 빙의 미니게임을 시작할 수 없습니다."
                : possessionSystem.LastPossessionResult;
            return false;
        }

        bool started = minigameController.BeginMinigame(lastLiveTarget);

        runtimeMessage = started
            ? $"생체 빙의 미니게임 시작: {lastLiveTarget.name}"
            : "생체 빙의 미니게임 시작에 실패했습니다.";

        return started;
    }

    /// <summary>
    /// E 입력 시 호출합니다.
    /// 주변에서 가장 가까운 시체를 찾은 뒤 즉시 빙의합니다.
    /// </summary>
    public bool TryPossessCorpseFromInput()
    {
        ResolveReferences();

        if (possessionSystem == null || targetValidator == null)
        {
            runtimeMessage =
                "시체 빙의 실패: PossessionSystem 또는 TargetValidator가 없습니다.";
            Debug.LogWarning(runtimeMessage, this);
            return false;
        }

        lastCorpseTarget = FindNearestTarget(requireLiveTarget: false);

        if (lastCorpseTarget == null)
        {
            runtimeMessage = "주변에 빙의 가능한 시체가 없습니다.";
            return false;
        }

        bool possessed = possessionSystem.TryPossess(lastCorpseTarget);

        runtimeMessage = possessed
            ? $"시체 빙의 성공: {lastCorpseTarget.name}"
            : string.IsNullOrWhiteSpace(possessionSystem.LastPossessionResult)
                ? "시체 빙의에 실패했습니다."
                : possessionSystem.LastPossessionResult;

        return possessed;
    }

    /// <summary>
    /// 기존 HWJ_PlayerInputSystem에서 R 입력을 감지했다면 이 메서드를 호출합니다.
    /// </summary>
    public void ReceiveLivePossessionInput()
    {
        TryStartLivePossessionFromInput();
    }

    /// <summary>
    /// 기존 HWJ_PlayerInputSystem에서 E 입력을 감지했다면 이 메서드를 호출합니다.
    /// </summary>
    public void ReceiveCorpsePossessionInput()
    {
        TryPossessCorpseFromInput();
    }

    private HWJ_RootObjectDataResolver FindNearestTarget(bool requireLiveTarget)
    {
        Vector2 origin = detectionOrigin != null
            ? detectionOrigin.position
            : transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            origin,
            Mathf.Max(0.1f, detectionRadius),
            targetLayerMask);

        uniqueResolverIds.Clear();

        HWJ_RootObjectDataResolver nearestTarget = null;
        float nearestDistanceSquared = float.PositiveInfinity;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            HWJ_RootObjectDataResolver resolver =
                hit.GetComponentInParent<HWJ_RootObjectDataResolver>();

            if (resolver == null)
            {
                continue;
            }

            if (resolver.transform.root == transform.root)
            {
                continue;
            }

            int resolverId = resolver.GetInstanceID();

            if (!uniqueResolverIds.Add(resolverId))
            {
                continue;
            }

            if (!targetValidator.TryGetPossessionBodyData(
                    resolver,
                    out HWJ_PossessionData possessionBody))
            {
                continue;
            }

            bool isLiveTarget =
                targetValidator.IsLiveTarget(resolver, possessionBody);

            if (isLiveTarget != requireLiveTarget)
            {
                continue;
            }

            if (!requireLiveTarget
                && !targetValidator.IsDefeatedTarget(resolver))
            {
                continue;
            }

            float distanceSquared =
                ((Vector2)resolver.transform.position - origin).sqrMagnitude;

            if (distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestTarget = resolver;
        }

        return nearestTarget;
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (targetValidator == null)
        {
            targetValidator = GetComponent<HWJ_PossessionTargetValidator>();
        }

        if (minigameController == null)
        {
            minigameController =
                GetComponent<HWJ_LivePossessionMinigameController>();
        }
    }

    private static bool WasLivePossessionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
            && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    private static bool WasCorpsePossessionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
            && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDetectionRange)
        {
            return;
        }

        Transform originTransform =
            detectionOrigin != null ? detectionOrigin : transform;

        Gizmos.DrawWireSphere(
            originTransform.position,
            Mathf.Max(0.1f, detectionRadius));
    }
}
