using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 움직이지 않는 최종보스의 손짓, 패턴 순서, 취소와 고정 위치를 관리합니다.
/// 개별 수치와 프리팹은 HWJ_FinalBossPatternDefinitionSO에서 읽습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed partial class HWJ_FinalBossPatternSystem :
    MonoBehaviour,
    HWJ_IBossSpecialPatternExecutor
{
    public const string FinalBossExecutorKey = "final_boss";

    private sealed class RuntimeAttackHandle
    {
        public GameObject Root;
        public BoxCollider2D Collider;
        public SpriteRenderer PlaceholderRenderer;
        public HWJ_FinalBossAttackHitbox Hitbox;
        public GameObject CustomVisual;
        public bool InUse;
    }

    [Header("공통 시스템 연결")]
    [Tooltip("보스의 페이즈, 보스방 범위, 그로기 상태를 제공하는 시스템입니다.")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;

    [Tooltip("보스의 현재 HP와 사망 상태를 제공하는 시스템입니다.")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;

    [Tooltip("최종보스 공격력을 사용해 플레이어 HP 피해를 계산합니다.")]
    [SerializeField] private HWJ_CombatSystem combatSystem;

    [Tooltip("1·2페이즈 방어막의 별도 내구도를 관리합니다.")]
    [SerializeField] private HWJ_FinalBossBarrierSystem barrierSystem;

    [Tooltip("2-3 실제 돌진에서 이동시킬 보스 Rigidbody2D입니다.")]
    [SerializeField] private Rigidbody2D body;

    [Tooltip("손짓 애니메이션 Trigger를 실행할 Animator입니다.")]
    [SerializeField] private Animator animator;

    [Header("표현 위치")]
    [Tooltip("본체 판정을 움직이지 않고 공중 연출만 할 시각 루트입니다.")]
    [SerializeField] private Transform bossVisualRoot;

    [Tooltip("구체, 창, 무기 투사체가 생성되는 손 또는 몸 주변 위치입니다.")]
    [SerializeField] private Transform projectileSocket;

    [Tooltip("포탈을 배치할 위치입니다. 2-4는 앞의 두 위치를 사용합니다.")]
    [SerializeField] private Transform[] portalAnchors;

    [Tooltip("런타임 투사체와 장판을 정리해서 담을 부모입니다.")]
    [SerializeField] private Transform runtimeEffectRoot;

    [Header("패턴 데이터")]
    [Tooltip("패턴 ID별 최종보스 실행 수치와 프리팹 정의입니다.")]
    [SerializeField] private HWJ_FinalBossPatternDefinitionSO[] patternDefinitions;

    [Header("충돌 및 디버그")]
    [Tooltip("돌진 및 투사체가 더 진행하지 못하게 막는 지형 레이어입니다.")]
    [SerializeField] private LayerMask obstacleLayers;

    [SerializeField] private string activePatternId;
    [SerializeField] private HWJ_FinalBossPatternKind activePatternKind;
    [SerializeField] private string lastExecutionMessage;

    private readonly List<RuntimeAttackHandle> runtimeAttackPool =
        new List<RuntimeAttackHandle>();
    private readonly List<GameObject> transientVisuals = new List<GameObject>();
    private Coroutine activePatternRoutine;
    private HWJ_FinalBossPatternDefinitionSO activeDefinition;
    private Transform activeTarget;
    private Vector3 stationaryAnchorPosition;
    private Vector3 visualRootInitialLocalPosition;
    private float originalGravityScale;
    private bool animationCastMomentReceived;
    private bool allowBossRootMovement;
    private bool ownsRuntimeEffectRoot;
    private int summonedMonsterSequence;

    public string ExecutorKey => FinalBossExecutorKey;
    public bool IsPatternRunning => activePatternRoutine != null;
    public string ActivePatternId => activePatternId;
    public HWJ_FinalBossPatternKind ActivePatternKind => activePatternKind;
    public string LastExecutionMessage => lastExecutionMessage;
    public int ActiveRuntimeAttackCount => CountActiveRuntimeAttacks();

    private void Awake()
    {
        ResolveReferences();
        stationaryAnchorPosition = transform.position;
        visualRootInitialLocalPosition = bossVisualRoot != null
            ? bossVisualRoot.localPosition
            : Vector3.zero;

        if (body != null)
        {
            originalGravityScale = body.gravityScale;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearVelocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (allowBossRootMovement)
        {
            return;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = stationaryAnchorPosition;
        }
        else
        {
            transform.position = stationaryAnchorPosition;
        }
    }

    private void OnEnable()
    {
        if (body != null)
        {
            body.gravityScale = 0f;
        }
    }

    private void OnDisable()
    {
        CancelActivePattern();

        if (body != null)
        {
            body.gravityScale = originalGravityScale;
        }
    }

    private void OnDestroy()
    {
        if (ownsRuntimeEffectRoot && runtimeEffectRoot != null)
        {
            Destroy(runtimeEffectRoot.gameObject);
            runtimeEffectRoot = null;
        }
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        ResolveReferences();

        if (pattern == null
            || !pattern.UseCustomPatternExecutor
            || !string.Equals(
                pattern.CustomPatternExecutorKey,
                FinalBossExecutorKey,
                System.StringComparison.Ordinal)
            || IsPatternRunning
            || runtimeStatus == null
            || runtimeStatus.IsDead
            || target == null
            || IsSpiritTarget(target))
        {
            return false;
        }

        return FindDefinition(pattern.PatternId, pattern.PatternNumber) != null;
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        if (!CanUsePattern(pattern, target))
        {
            lastExecutionMessage = "최종보스 패턴 실행 실패: 실행 조건 또는 패턴 정의를 확인하세요.";
            return false;
        }

        HWJ_FinalBossPatternDefinitionSO definition =
            FindDefinition(pattern.PatternId, pattern.PatternNumber);
        return BeginPattern(definition, target);
    }

    /// <summary>
    /// Unity 인스펙터 테스트 도구에서 특정 패턴을 직접 검증할 때 사용합니다.
    /// 실제 AI 실행은 HWJ_BossPatternSystem을 통해야 합니다.
    /// </summary>
    public bool TryStartPatternForDebug(string patternId, Transform targetOverride = null)
    {
        if (IsPatternRunning)
        {
            return false;
        }

        HWJ_FinalBossPatternDefinitionSO definition = FindDefinition(patternId, 0);
        Transform resolvedTarget = targetOverride != null
            ? targetOverride
            : bossBrain != null ? bossBrain.Target : null;

        return definition != null
            && resolvedTarget != null
            && !IsSpiritTarget(resolvedTarget)
            && BeginPattern(definition, resolvedTarget);
    }

    public void NotifyAnimationCastMoment()
    {
        if (IsPatternRunning)
        {
            animationCastMomentReceived = true;
        }
    }

    public void SetStationaryAnchor(Vector3 worldPosition)
    {
        stationaryAnchorPosition = worldPosition;

        if (!allowBossRootMovement)
        {
            SetBossPosition(worldPosition);
        }
    }

    public void CancelActivePattern()
    {
        if (activePatternRoutine != null)
        {
            StopCoroutine(activePatternRoutine);
            activePatternRoutine = null;
        }

        StopAllCoroutines();
        ReleaseAllRuntimeAttacks();
        DestroyTransientVisuals();
        barrierSystem?.DeactivateBarrier();
        allowBossRootMovement = false;
        SetBossPosition(stationaryAnchorPosition);
        ResetVisualRoot();
        activeDefinition = null;
        activeTarget = null;
        activePatternId = string.Empty;
        animationCastMomentReceived = false;
        lastExecutionMessage = "진행 중인 최종보스 패턴을 취소했습니다.";
    }

    private bool BeginPattern(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        if (definition == null || target == null)
        {
            return false;
        }

        activeDefinition = definition;
        activeTarget = target;
        activePatternId = definition.PatternId;
        activePatternKind = definition.PatternKind;
        animationCastMomentReceived = false;
        PlayGesture(definition);
        activePatternRoutine = StartCoroutine(RunActivePattern(definition, target));
        lastExecutionMessage = $"최종보스 패턴 실행 시작: {definition.PatternId}";
        return true;
    }

    private IEnumerator RunActivePattern(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        yield return RunPatternSequence(definition, target);

        if (definition.RecoverySeconds > 0f)
        {
            yield return new WaitForSeconds(definition.RecoverySeconds);
        }

        allowBossRootMovement = false;
        SetBossPosition(stationaryAnchorPosition);
        ResetVisualRoot();
        activePatternRoutine = null;
        activeDefinition = null;
        activeTarget = null;
        activePatternId = string.Empty;
        animationCastMomentReceived = false;
        lastExecutionMessage = $"최종보스 패턴 실행 완료: {definition.PatternId}";
    }

    private IEnumerator WaitForCastMoment(HWJ_FinalBossPatternDefinitionSO definition)
    {
        if (!definition.PreferAnimationCastEvent)
        {
            yield return new WaitForSeconds(definition.PreparationSeconds);
            yield break;
        }

        float elapsed = 0f;
        float timeout = Mathf.Max(
            definition.PreparationSeconds,
            definition.AnimationEventTimeoutSeconds);

        while (!animationCastMomentReceived && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void PlayGesture(HWJ_FinalBossPatternDefinitionSO definition)
    {
        if (animator == null || definition == null)
        {
            return;
        }

        SetAnimatorIntegerIfPresent("FinalBossPattern", (int)definition.PatternKind);

        if (!string.IsNullOrWhiteSpace(definition.GestureTrigger)
            && HasAnimatorParameter(definition.GestureTrigger, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(definition.GestureTrigger);
        }
    }

    private void SetAnimatorIntegerIfPresent(string parameterName, int value)
    {
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(parameterName, value);
        }
    }

    private bool HasAnimatorParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == parameterType
                && parameters[i].name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private HWJ_FinalBossPatternDefinitionSO FindDefinition(
        string patternId,
        int patternNumber)
    {
        if (patternDefinitions == null)
        {
            return null;
        }

        for (int i = 0; i < patternDefinitions.Length; i++)
        {
            HWJ_FinalBossPatternDefinitionSO definition = patternDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(patternId)
                && string.Equals(
                    definition.PatternId,
                    patternId,
                    System.StringComparison.Ordinal))
            {
                return definition;
            }

            if (patternNumber > 0 && (int)definition.PatternKind == patternNumber)
            {
                return definition;
            }
        }

        return null;
    }

    private void ResolveReferences()
    {
        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (barrierSystem == null)
        {
            barrierSystem = GetComponent<HWJ_FinalBossBarrierSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (bossVisualRoot == null && animator != null)
        {
            bossVisualRoot = animator.transform;
        }

        if (projectileSocket == null)
        {
            projectileSocket = bossVisualRoot != null ? bossVisualRoot : transform;
        }

        if (runtimeEffectRoot == null)
        {
            Transform existing = transform.Find("HWJ_FinalBoss_RuntimeEffects");

            if (existing == null)
            {
                GameObject root = new GameObject("HWJ_FinalBoss_RuntimeEffects");
                existing = root.transform;
                existing.SetParent(null, true);
                ownsRuntimeEffectRoot = true;
            }

            runtimeEffectRoot = existing;
        }
    }

    private static bool IsSpiritTarget(Transform target)
    {
        HWJ_SoulSystem soul = target != null
            ? target.GetComponentInParent<HWJ_SoulSystem>()
            : null;
        return soul != null
            && soul.CurrentExistenceState == HWJ_PlayerExistenceState.Spirit;
    }

    private void SetBossPosition(Vector3 worldPosition)
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = worldPosition;
        }

        transform.position = worldPosition;
    }

    private void ResetVisualRoot()
    {
        if (bossVisualRoot != null)
        {
            bossVisualRoot.localPosition = visualRootInitialLocalPosition;
        }
    }

    private int CountActiveRuntimeAttacks()
    {
        int count = 0;

        for (int i = 0; i < runtimeAttackPool.Count; i++)
        {
            if (runtimeAttackPool[i].InUse)
            {
                count++;
            }
        }

        return count;
    }
}
