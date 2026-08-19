using UnityEngine;

// 중간보스2의 이동, 패턴 단계, 그로기와 사망을 단순한 Animator 파라미터로 전달합니다.
[DisallowMultipleComponent]
public class hys_SecondBossAnimatorBridge : MonoBehaviour
{
    private const int HitActionId = 11;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int ActionHash = Animator.StringToHash("Action");
    private static readonly int ActionIdHash = Animator.StringToHash("ActionId");
    private static readonly int IsGroggyHash = Animator.StringToHash("IsGroggy");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.hys_MidBoss2_Idle");
    private static readonly int MoveStateHash = Animator.StringToHash("Base Layer.hys_MidBoss2_Move");
    private static readonly int DeathStateHash = Animator.StringToHash("Base Layer.hys_MidBoss2_Death");

    [Header("중간보스2 참조")]
    [SerializeField] private hys_SecondBossLogic bossLogic;
    [SerializeField] private hys_SecondBossPattern patternSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;

    [Header("피격 및 사망")]
    [SerializeField, Min(0.01f)] private float emptyHitFallbackSeconds = 0.2f;
    [SerializeField, Range(0.9f, 1f)] private float deathFreezeNormalizedTime = 0.995f;

    private hys_SecondBossAnimationAction previousAction;
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
        if (!HasSecondBossController()) return;

        bool isDead = runtimeStatus != null && runtimeStatus.IsDead;
        animator.SetBool(IsDeadHash, isDead);
        if (isDead)
        {
            FreezeCorpseAtDeathEnd();
            return;
        }

        RestoreLivingBodyIfNeeded();
        animator.SetFloat(MoveSpeedHash, ResolveMoveSpeed());

        bool isGroggy = bossLogic != null && bossLogic.IsGroggy;
        animator.SetBool(IsGroggyHash, isGroggy);
        if (!isGroggy && previousGroggy) ReturnToLocomotion();
        previousGroggy = isGroggy;

        UpdatePatternAction(isGroggy);
        UpdateHitAction(isGroggy);
    }

    public void Initialize(
        hys_SecondBossLogic logic,
        hys_SecondBossPattern patterns,
        Animator targetAnimator)
    {
        bossLogic = logic;
        patternSystem = patterns;
        animator = targetAnimator;
        runtimeStatus = logic != null ? logic.GetComponent<HWJ_RuntimeStatusSystem>() : null;
        body = logic != null ? logic.GetComponent<Rigidbody2D>() : null;
        CacheLivingState();
    }

    // 단발 동작의 마지막 프레임에 이 Animation Event를 넣으면 즉시 이동/대기 상태로 복귀합니다.
    public void hys_OnActionAnimationFinished()
    {
        hitReturnTime = 0f;
        if (runtimeStatus != null && runtimeStatus.IsDead) return;
        if (bossLogic != null && bossLogic.IsGroggy) return;
        if (patternSystem != null
            && patternSystem.CurrentAnimationAction != hys_SecondBossAnimationAction.None) return;

        ReturnToLocomotion();
    }

    private void UpdatePatternAction(bool isGroggy)
    {
        hys_SecondBossAnimationAction currentAction = patternSystem != null
            ? patternSystem.CurrentAnimationAction
            : hys_SecondBossAnimationAction.None;
        if (currentAction == previousAction) return;

        hys_SecondBossAnimationAction oldAction = previousAction;
        previousAction = currentAction;

        if (currentAction != hys_SecondBossAnimationAction.None)
        {
            hitReturnTime = 0f;
            PlayAction((int)currentAction);
            return;
        }

        if (oldAction != hys_SecondBossAnimationAction.None && !isGroggy)
            ReturnToLocomotion();
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
            && patternSystem.CurrentAnimationAction != hys_SecondBossAnimationAction.None;
        if (currentHp < previousHp - 0.001f && !isGroggy && !hasPatternAction)
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
        if (animator.HasState(0, destination))
            animator.CrossFade(destination, 0.05f, 0, 0f);
    }

    private float ResolveMoveSpeed()
    {
        if (bossLogic != null && !bossLogic.IsMoving) return 0f;
        return body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
    }

    private bool HasSecondBossController()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrideController)
            controller = overrideController.runtimeAnimatorController;

        return controller != null && controller.name == "hys_MidBoss2";
    }

    private void FreezeCorpseAtDeathEnd()
    {
        if (corpseFrozen || animator.IsInTransition(0)) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != DeathStateHash) return;

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        bool emptyClip = clips.Length == 0 || clips[0].clip == null || clips[0].clip.empty;
        if (!emptyClip && state.normalizedTime < deathFreezeNormalizedTime) return;

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
        if (bossLogic == null) bossLogic = GetComponentInParent<hys_SecondBossLogic>();
        if (patternSystem == null && bossLogic != null)
            patternSystem = bossLogic.GetComponent<hys_SecondBossPattern>();
        if (runtimeStatus == null && bossLogic != null)
            runtimeStatus = bossLogic.GetComponent<HWJ_RuntimeStatusSystem>();
        if (body == null && bossLogic != null)
            body = bossLogic.GetComponent<Rigidbody2D>();
    }
}
