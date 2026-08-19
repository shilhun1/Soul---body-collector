using UnityEngine;

// 최종 보스의 이동, 패턴 단계, 그로기와 사망 상태를 단순 Animator 파라미터로 전달합니다.
[DisallowMultipleComponent]
public class hys_FinalBossAnimatorBridge : MonoBehaviour
{
    private const int HitActionId = 19;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int ActionHash = Animator.StringToHash("Action");
    private static readonly int ActionIdHash = Animator.StringToHash("ActionId");
    private static readonly int IsGroggyHash = Animator.StringToHash("IsGroggy");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.hys_FinalBoss_Idle");
    private static readonly int MoveStateHash = Animator.StringToHash("Base Layer.hys_FinalBoss_Move");
    private static readonly int DeathStateHash = Animator.StringToHash("Base Layer.hys_FinalBoss_Death");

    [Header("최종 보스 참조")]
    [SerializeField] private hys_FinalBossLogic bossLogic;
    [SerializeField] private hys_FinalBossPattern patternSystem;
    [SerializeField] private hys_FinalBossShield shieldSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;

    [Header("피격 및 사망")]
    [SerializeField, Min(0.01f)] private float emptyHitFallbackSeconds = 0.2f;
    [SerializeField, Range(0.9f, 1f)] private float deathFreezeNormalizedTime = 0.995f;

    private int previousActionVersion = int.MinValue;
    private hys_FinalBossAnimationAction previousAction;
    private bool previousGroggy;
    private bool corpseFrozen;
    private float previousHp = float.NaN;
    private float hitReturnTime;
    private RigidbodyConstraints2D livingConstraints;

    private void Awake()
    {
        CacheReferences();
        CacheLivingState();
    }

    private void Update()
    {
        CacheReferences();
        if (!HasFinalBossController()) return;

        bool isDead = runtimeStatus != null && runtimeStatus.IsDead;
        animator.SetBool(IsDeadHash, isDead);
        if (isDead)
        {
            FreezeCorpseAtDeathEnd();
            return;
        }

        RestoreLivingBodyIfNeeded();
        animator.SetFloat(MoveSpeedHash, ResolveMoveSpeed());

        bool isGroggy = bossLogic != null && bossLogic.IsGroggy
            || patternSystem != null && patternSystem.IsGroggy;
        animator.SetBool(IsGroggyHash, isGroggy);
        if (!isGroggy && previousGroggy) ReturnToLocomotion();
        previousGroggy = isGroggy;

        UpdatePatternAction(isGroggy);
        UpdateHitAction(isGroggy);
    }

    public void Initialize(
        hys_FinalBossLogic logic,
        hys_FinalBossPattern patterns,
        Animator targetAnimator)
    {
        bossLogic = logic;
        patternSystem = patterns;
        animator = targetAnimator;
        runtimeStatus = logic != null ? logic.GetComponent<HWJ_RuntimeStatusSystem>() : null;
        shieldSystem = logic != null ? logic.GetComponent<hys_FinalBossShield>() : null;
        body = logic != null ? logic.GetComponent<Rigidbody2D>() : null;
        CacheLivingState();
    }

    // 단발 동작의 마지막 프레임에 넣으면 패턴 종료 후 즉시 이동/대기 상태로 돌아갑니다.
    public void hys_OnActionAnimationFinished()
    {
        hitReturnTime = 0f;
        if (runtimeStatus != null && runtimeStatus.IsDead) return;
        if (patternSystem != null && (patternSystem.IsPatternRunning || patternSystem.IsGroggy)) return;
        ReturnToLocomotion();
    }

    private void UpdatePatternAction(bool isGroggy)
    {
        int version = patternSystem != null ? patternSystem.AnimationActionVersion : 0;
        if (version == previousActionVersion) return;
        previousActionVersion = version;

        hys_FinalBossAnimationAction action = patternSystem != null
            ? patternSystem.CurrentAnimationAction
            : hys_FinalBossAnimationAction.None;
        hys_FinalBossAnimationAction oldAction = previousAction;
        previousAction = action;

        if (action != hys_FinalBossAnimationAction.None)
        {
            hitReturnTime = 0f;
            PlayAction((int)action);
        }
        else if (oldAction != hys_FinalBossAnimationAction.None && !isGroggy)
        {
            ReturnToLocomotion();
        }
    }

    private void UpdateHitAction(bool isGroggy)
    {
        if (runtimeStatus == null) return;
        float currentHp = runtimeStatus.CurrentHp;
        if (float.IsNaN(previousHp))
        {
            previousHp = currentHp;
            return;
        }

        bool hasPatternAction = patternSystem != null
            && patternSystem.CurrentAnimationAction != hys_FinalBossAnimationAction.None;
        bool shieldActive = shieldSystem != null && shieldSystem.IsActive;
        if (currentHp < previousHp - 0.001f && !isGroggy && !hasPatternAction && !shieldActive)
        {
            PlayAction(HitActionId);
            hitReturnTime = Time.time + Mathf.Max(0.01f, emptyHitFallbackSeconds);
        }

        previousHp = currentHp;
        if (hitReturnTime > 0f && Time.time >= hitReturnTime)
        {
            hitReturnTime = 0f;
            ReturnToLocomotion();
        }
    }

    private void PlayAction(int actionId)
    {
        animator.SetFloat(ActionIdHash, actionId);
        animator.ResetTrigger(ActionHash);
        animator.SetTrigger(ActionHash);
    }

    private void ReturnToLocomotion()
    {
        int destination = ResolveMoveSpeed() > 0.01f ? MoveStateHash : IdleStateHash;
        if (animator.HasState(0, destination)) animator.CrossFade(destination, 0.05f, 0, 0f);
    }

    private float ResolveMoveSpeed()
    {
        if (bossLogic != null && !bossLogic.IsMoving) return 0f;
        return body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
    }

    private bool HasFinalBossController()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrideController)
            controller = overrideController.runtimeAnimatorController;
        return controller != null && controller.name == "hys_FinalBoss";
    }

    private void FreezeCorpseAtDeathEnd()
    {
        if (corpseFrozen || animator.IsInTransition(0)) return;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != DeathStateHash) return;

        bool emptyDeathMotion = state.length <= 0.0001f;
        if (!emptyDeathMotion && state.normalizedTime < deathFreezeNormalizedTime) return;
        animator.speed = 0f;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        corpseFrozen = true;
    }

    private void RestoreLivingBodyIfNeeded()
    {
        if (!corpseFrozen) return;
        corpseFrozen = false;
        animator.speed = 1f;
        if (body != null) body.constraints = livingConstraints;
    }

    private void CacheLivingState()
    {
        livingConstraints = body != null ? body.constraints : RigidbodyConstraints2D.None;
        previousHp = runtimeStatus != null ? runtimeStatus.CurrentHp : float.NaN;
    }

    private void CacheReferences()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (bossLogic == null) bossLogic = GetComponentInParent<hys_FinalBossLogic>();
        if (patternSystem == null && bossLogic != null)
            patternSystem = bossLogic.GetComponent<hys_FinalBossPattern>();
        if (shieldSystem == null && bossLogic != null)
            shieldSystem = bossLogic.GetComponent<hys_FinalBossShield>();
        if (runtimeStatus == null && bossLogic != null)
            runtimeStatus = bossLogic.GetComponent<HWJ_RuntimeStatusSystem>();
        if (body == null && bossLogic != null)
            body = bossLogic.GetComponent<Rigidbody2D>();
    }
}
