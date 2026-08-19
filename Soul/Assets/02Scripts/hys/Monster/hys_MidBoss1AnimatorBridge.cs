using UnityEngine;

// 중간보스 1의 AI 상태를 단순한 Animator 파라미터 다섯 개로 전달합니다.
[DisallowMultipleComponent]
public class hys_MidBoss1AnimatorBridge : MonoBehaviour
{
    private const int PhaseTwoLaserAction = 6;
    private const int HitAction = 7;
    private const int PhaseTransitionAction = 8;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int ActionHash = Animator.StringToHash("Action");
    private static readonly int ActionIdHash = Animator.StringToHash("ActionId");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int IsGroggyHash = Animator.StringToHash("IsGroggy");

    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.hys_MidBoss1_Idle");
    private static readonly int MoveStateHash = Animator.StringToHash("Base Layer.hys_MidBoss1_Move");
    private static readonly int DeathStateHash = Animator.StringToHash("Base Layer.hys_MidBoss1_Death");

    [Header("중간보스 참조")]
    [SerializeField] private hys_Stage1MidBossLogic bossLogic;
    [SerializeField] private hys_Stage1MidBossPattern patternSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;

    [Header("사망 시체 유지")]
    [SerializeField, Range(0.9f, 1f)] private float deathFreezeNormalizedTime = 0.995f;

    private int lastPatternNumber;
    private bool previousGroggy;
    private bool previousPhaseTransition;
    private bool previousHitState;
    private bool corpseFrozen;
    private RigidbodyConstraints2D livingConstraints;

    private void Awake()
    {
        CacheReferences();
        livingConstraints = body != null ? body.constraints : RigidbodyConstraints2D.None;
    }

    private void Update()
    {
        CacheReferences();
        if (!HasMidBossController())
        {
            return;
        }

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
        if (!isGroggy && previousGroggy)
        {
            ReturnToLocomotion();
        }

        previousGroggy = isGroggy;
        UpdatePatternAction();
        UpdatePhaseAction();
        UpdateHitAction(isGroggy);
    }

    public void Initialize(
        hys_Stage1MidBossLogic logic,
        hys_Stage1MidBossPattern patterns,
        Animator targetAnimator)
    {
        bossLogic = logic;
        patternSystem = patterns;
        animator = targetAnimator;
        runtimeStatus = logic != null ? logic.GetComponent<HWJ_RuntimeStatusSystem>() : null;
        body = logic != null ? logic.GetComponent<Rigidbody2D>() : null;
        livingConstraints = body != null ? body.constraints : RigidbodyConstraints2D.None;
    }

    // 액션 클립 마지막 프레임에 이 함수를 Animation Event로 넣으면 자연스럽게 이동 상태로 복귀합니다.
    public void hys_OnActionAnimationFinished()
    {
        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            return;
        }

        if (bossLogic != null && bossLogic.IsGroggy)
        {
            return;
        }

        if (patternSystem != null && patternSystem.IsPatternRunning)
        {
            return;
        }

        ReturnToLocomotion();
    }

    private void UpdatePatternAction()
    {
        int currentPattern = patternSystem != null ? patternSystem.CurrentPatternNumber : 0;
        if (currentPattern == lastPatternNumber)
        {
            return;
        }

        int previousPattern = lastPatternNumber;
        lastPatternNumber = currentPattern;

        if (currentPattern >= 1 && currentPattern <= 5)
        {
            PlayAction(currentPattern);
            return;
        }

        if (currentPattern == PhaseTwoLaserAction)
        {
            PlayAction(PhaseTwoLaserAction);
            return;
        }

        if (previousPattern > 0 && !previousGroggy)
        {
            ReturnToLocomotion();
        }
    }

    private void UpdatePhaseAction()
    {
        bool isPhaseTransition = bossLogic != null && bossLogic.IsPhaseTransition;
        if (isPhaseTransition && !previousPhaseTransition
            && (patternSystem == null || patternSystem.CurrentPatternNumber != PhaseTwoLaserAction))
        {
            PlayAction(PhaseTransitionAction);
        }

        if (!isPhaseTransition && previousPhaseTransition && lastPatternNumber == 0)
        {
            ReturnToLocomotion();
        }

        previousPhaseTransition = isPhaseTransition;
    }

    private void UpdateHitAction(bool isGroggy)
    {
        bool hasActivePattern = patternSystem != null && patternSystem.CurrentPatternNumber != 0;
        bool isHit = !isGroggy
            && !hasActivePattern
            && (bossLogic == null || !bossLogic.IsPhaseTransition)
            && runtimeStatus != null
            && runtimeStatus.CurrentState == HWJ_RuntimeState.Hit;

        if (isHit && !previousHitState)
        {
            PlayAction(HitAction);
        }
        else if (!isHit && previousHitState)
        {
            ReturnToLocomotion();
        }

        previousHitState = isHit;
    }

    private void PlayAction(int actionId)
    {
        animator.SetFloat(ActionIdHash, actionId);
        animator.ResetTrigger(ActionHash);
        animator.SetTrigger(ActionHash);
    }

    private void ReturnToLocomotion()
    {
        float moveSpeed = ResolveMoveSpeed();
        int destination = moveSpeed > 0.01f ? MoveStateHash : IdleStateHash;
        if (animator.HasState(0, destination))
        {
            animator.CrossFade(destination, 0.05f, 0, 0f);
        }
    }

    private float ResolveMoveSpeed()
    {
        if (bossLogic != null && !bossLogic.IsMoving)
        {
            return 0f;
        }

        return body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
    }

    private bool HasMidBossController()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrideController)
        {
            controller = overrideController.runtimeAnimatorController;
        }

        return controller != null && controller.name == "hys_MidBoss1";
    }

    private void FreezeCorpseAtDeathEnd()
    {
        if (corpseFrozen || animator.IsInTransition(0))
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != DeathStateHash)
        {
            return;
        }

        // 빈 클립은 즉시, 프레임을 넣은 뒤에는 사망 애니메이션의 마지막 프레임에서 고정합니다.
        bool emptyDeathMotion = state.length <= 0.0001f;
        if (!emptyDeathMotion && state.normalizedTime < deathFreezeNormalizedTime)
        {
            return;
        }

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
        if (!corpseFrozen)
        {
            return;
        }

        corpseFrozen = false;
        animator.speed = 1f;
        if (body != null)
        {
            body.constraints = livingConstraints;
        }
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (bossLogic == null)
        {
            bossLogic = GetComponentInParent<hys_Stage1MidBossLogic>();
        }

        if (patternSystem == null && bossLogic != null)
        {
            patternSystem = bossLogic.GetComponent<hys_Stage1MidBossPattern>();
        }

        if (runtimeStatus == null && bossLogic != null)
        {
            runtimeStatus = bossLogic.GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (body == null && bossLogic != null)
        {
            body = bossLogic.GetComponent<Rigidbody2D>();
        }
    }
}
