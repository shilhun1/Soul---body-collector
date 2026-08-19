using UnityEngine;

// 모든 hys 몬스터 Animator의 패턴 복귀와 사망 후 시체 고정을 공통으로 처리합니다.
[DisallowMultipleComponent]
public class hys_MonsterAnimatorLifecycle : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int PatternTagHash = Animator.StringToHash("hys_Pattern");

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;

    [Header("패턴 종료 후 기본 상태 복귀")]
    // Entry는 재생 상태가 아니므로 Entry가 가리키는 Idle 상태로 직접 복귀합니다.
    [SerializeField] private int baseLayerIndex = 0;
    [SerializeField, Range(0.8f, 1f)] private float patternReturnNormalizedTime = 0.98f;
    [SerializeField, Min(0f)] private float patternReturnTransitionSeconds = 0.05f;

    [Header("사망 시체 유지")]
    // Death 클립 마지막 프레임에 도달하면 Animator와 물리를 고정하여 시체를 남깁니다.
    [SerializeField, Range(0.9f, 1f)] private float deathFreezeNormalizedTime = 0.995f;

    private string configuredControllerName;
    private int idleStateHash;
    private int deathStateHash;
    private bool hasMoveSpeedParameter;
    private bool hasHitParameter;
    private bool hasIsDeadParameter;
    private bool hasVerticalSpeedParameter;
    private bool hasIsGroundedParameter;
    private bool hasDashParameter;
    private bool hasRuntimeStateSnapshot;
    private HWJ_RuntimeState previousRuntimeState;
    private bool corpseFrozen;
    private float normalAnimatorSpeed = 1f;
    private RigidbodyConstraints2D normalBodyConstraints;
    private Vector2 previousWorldPosition;
    private Vector2 sampledWorldVelocity;
    private bool hasPositionSnapshot;

    private void Awake()
    {
        CacheReferences();
        CacheOriginalSettings();
        ConfigureStateHashes();
    }

    private void OnEnable()
    {
        CacheReferences();
        ConfigureStateHashes();
        hasRuntimeStateSnapshot = false;
        ResetPositionSampling();

        if (runtimeStatus == null || !runtimeStatus.IsDead)
        {
            RestoreLivingSettings();
        }
    }

    private void Update()
    {
        CacheReferences();
        ConfigureStateHashes();

        if (animator == null || string.IsNullOrEmpty(configuredControllerName))
        {
            return;
        }

        bool isDead = runtimeStatus != null
            ? runtimeStatus.IsDead
            : hasIsDeadParameter && animator.GetBool(IsDeadHash);

        if (isDead)
        {
            EnterAndFreezeDeathState();
            return;
        }

        if (hasIsDeadParameter)
        {
            animator.SetBool(IsDeadHash, false);
        }

        UpdateLivingAnimatorParameters();
        TryReturnFinishedPatternToIdle();
    }

    // 이동 속도와 피격 진입 순간만 전달해 같은 모션이 매 프레임 재시작되지 않게 합니다.
    private void UpdateLivingAnimatorParameters()
    {
        SampleWorldVelocity();

        if (hasMoveSpeedParameter)
        {
            // AI가 Transform을 직접 옮길 때도 Walk 상태가 선택되도록 실제 위치 변화량을 함께 사용합니다.
            float rigidbodySpeed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
            float moveSpeed = Mathf.Max(rigidbodySpeed, Mathf.Abs(sampledWorldVelocity.x));
            animator.SetFloat(MoveSpeedHash, moveSpeed);
        }

        // 점프도 Rigidbody 또는 실제 위치 변화 중 더 큰 값을 사용해 상승·최상단·낙하를 구분합니다.
        float rigidbodyVerticalSpeed = body != null ? body.linearVelocity.y : 0f;
        float verticalSpeed = Mathf.Abs(rigidbodyVerticalSpeed) >= Mathf.Abs(sampledWorldVelocity.y)
            ? rigidbodyVerticalSpeed
            : sampledWorldVelocity.y;
        if (hasVerticalSpeedParameter)
        {
            animator.SetFloat(VerticalSpeedHash, verticalSpeed);
        }
        if (hasIsGroundedParameter)
        {
            animator.SetBool(IsGroundedHash, Mathf.Abs(verticalSpeed) <= 0.05f);
        }

        if (runtimeStatus == null)
        {
            return;
        }

        HWJ_RuntimeState currentState = runtimeStatus.CurrentState;
        bool enteredHit = currentState == HWJ_RuntimeState.Hit
            && (!hasRuntimeStateSnapshot || previousRuntimeState != HWJ_RuntimeState.Hit);
        if (enteredHit && hasHitParameter)
        {
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(HitHash);
        }

        previousRuntimeState = currentState;
        hasRuntimeStateSnapshot = true;
    }

    private void SampleWorldVelocity()
    {
        Vector2 currentPosition = transform.position;
        if (!hasPositionSnapshot || Time.deltaTime <= Mathf.Epsilon)
        {
            previousWorldPosition = currentPosition;
            sampledWorldVelocity = Vector2.zero;
            hasPositionSnapshot = true;
            return;
        }

        sampledWorldVelocity = (currentPosition - previousWorldPosition) / Time.deltaTime;
        previousWorldPosition = currentPosition;
    }

    private void ResetPositionSampling()
    {
        previousWorldPosition = transform.position;
        sampledWorldVelocity = Vector2.zero;
        hasPositionSnapshot = true;
    }

    // 현재 또는 이후의 Sword 대시 로직이 호출할 수 있는 애니메이션 전용 진입점입니다.
    public void PlayDashAnimation()
    {
        if (animator == null || !hasDashParameter || runtimeStatus != null && runtimeStatus.IsDead)
        {
            return;
        }

        animator.ResetTrigger(DashHash);
        animator.SetTrigger(DashHash);
    }

    // 자동 설치 도구가 찾은 Animator와 HWJ 상태 시스템을 즉시 연결할 때 사용합니다.
    public void Initialize(Animator targetAnimator, HWJ_RuntimeStatusSystem targetStatus)
    {
        animator = targetAnimator;
        runtimeStatus = targetStatus;
        body = targetStatus != null
            ? targetStatus.GetComponent<Rigidbody2D>()
            : targetAnimator != null ? targetAnimator.GetComponentInParent<Rigidbody2D>() : null;
        CacheOriginalSettings();
        ConfigureStateHashes(true);
    }

    private void TryReturnFinishedPatternToIdle()
    {
        if (animator.IsInTransition(baseLayerIndex))
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
        // 패턴 이름이 직업마다 달라도 공통 태그로 종료 여부를 확인합니다.
        bool isPatternState = stateInfo.tagHash == PatternTagHash;

        // 아직 Motion이 없는 빈 슬롯은 즉시 Idle로 복귀하고, 클립 추가 후에는 재생 종료를 기다립니다.
        bool hasNoPatternMotion = stateInfo.length <= 0.0001f;
        if (!isPatternState
            || !hasNoPatternMotion && stateInfo.normalizedTime < patternReturnNormalizedTime)
        {
            return;
        }

        if (animator.HasState(baseLayerIndex, idleStateHash))
        {
            animator.CrossFade(idleStateHash, patternReturnTransitionSeconds, baseLayerIndex, 0f);
        }
    }

    private void EnterAndFreezeDeathState()
    {
        // 사망 전환과 이동/피격 명령이 같은 프레임에 충돌하지 않도록 먼저 정리합니다.
        if (hasMoveSpeedParameter)
        {
            animator.SetFloat(MoveSpeedHash, 0f);
        }

        if (hasHitParameter)
        {
            animator.ResetTrigger(HitHash);
        }

        if (hasIsDeadParameter)
        {
            animator.SetBool(IsDeadHash, true);
        }

        if (body != null && !corpseFrozen)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            body.linearVelocity = velocity;
            body.angularVelocity = 0f;
        }

        if (corpseFrozen || animator.IsInTransition(baseLayerIndex))
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
        if (stateInfo.fullPathHash != deathStateHash
            || stateInfo.normalizedTime < deathFreezeNormalizedTime)
        {
            return;
        }

        // 마지막 사망 프레임을 그대로 유지하고 오브젝트를 비활성화하거나 삭제하지 않습니다.
        animator.speed = 0f;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        corpseFrozen = true;
    }

    private void RestoreLivingSettings()
    {
        corpseFrozen = false;

        if (animator != null)
        {
            animator.speed = normalAnimatorSpeed > 0f ? normalAnimatorSpeed : 1f;
        }

        if (body != null)
        {
            body.constraints = normalBodyConstraints;
        }
    }

    private void ConfigureStateHashes(bool force = false)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            configuredControllerName = null;
            return;
        }

        string controllerName = animator.runtimeAnimatorController.name;
        if (!controllerName.StartsWith("hys_Monster_"))
        {
            configuredControllerName = null;
            return;
        }

        if (!force && controllerName == configuredControllerName)
        {
            return;
        }

        configuredControllerName = controllerName;
        string statePrefix = $"Base Layer.{controllerName}";

        idleStateHash = Animator.StringToHash($"{statePrefix}_Idle");
        deathStateHash = Animator.StringToHash($"{statePrefix}_Death");
        hasMoveSpeedParameter = HasParameter(MoveSpeedHash, AnimatorControllerParameterType.Float);
        hasHitParameter = HasParameter(HitHash, AnimatorControllerParameterType.Trigger);
        hasIsDeadParameter = HasParameter(IsDeadHash, AnimatorControllerParameterType.Bool);
        hasVerticalSpeedParameter = HasParameter(VerticalSpeedHash, AnimatorControllerParameterType.Float);
        hasIsGroundedParameter = HasParameter(IsGroundedHash, AnimatorControllerParameterType.Bool);
        hasDashParameter = HasParameter(DashHash, AnimatorControllerParameterType.Trigger);
        hasRuntimeStateSnapshot = false;
    }

    private bool HasParameter(int parameterHash, AnimatorControllerParameterType expectedType)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash && parameters[i].type == expectedType)
            {
                return true;
            }
        }

        return false;
    }

    private void CacheReferences()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private void CacheOriginalSettings()
    {
        if (animator != null && animator.speed > 0f)
        {
            normalAnimatorSpeed = animator.speed;
        }

        if (body != null)
        {
            normalBodyConstraints = body.constraints;
        }
    }
}

// hys 몬스터 Controller를 사용하는 현재/추후 생성 몬스터에 생명주기 컴포넌트를 자동 연결합니다.
internal sealed class hys_MonsterAnimatorLifecycleInstaller : MonoBehaviour
{
    private const float ScanIntervalSeconds = 0.5f;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstaller()
    {
        if (FindAnyObjectByType<hys_MonsterAnimatorLifecycleInstaller>() != null)
        {
            return;
        }

        GameObject installerObject = new GameObject("hys_MonsterAnimatorLifecycleInstaller");
        installerObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(installerObject);
        installerObject.AddComponent<hys_MonsterAnimatorLifecycleInstaller>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
        AttachMissingComponents();
    }

    private static void AttachMissingComponents()
    {
        Animator[] animators = FindObjectsByType<Animator>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator targetAnimator = animators[i];
            if (targetAnimator == null
                || targetAnimator.runtimeAnimatorController == null
                || !targetAnimator.runtimeAnimatorController.name.StartsWith("hys_Monster_"))
            {
                continue;
            }

            HWJ_RuntimeStatusSystem status = targetAnimator.GetComponentInParent<HWJ_RuntimeStatusSystem>();
            GameObject hostObject = status != null ? status.gameObject : targetAnimator.gameObject;
            hys_MonsterAnimatorLifecycle lifecycle =
                hostObject.GetComponent<hys_MonsterAnimatorLifecycle>();

            if (lifecycle == null)
            {
                lifecycle = hostObject.AddComponent<hys_MonsterAnimatorLifecycle>();
                lifecycle.Initialize(targetAnimator, status);
            }
        }
    }
}
