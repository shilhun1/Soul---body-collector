using UnityEngine;

[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(200)]
public class hys_Ghost_Animator : MonoBehaviour
{
    // 怨좎뒪???꾩슜 ?좊땲硫붿씠???뚮씪誘명꽣瑜?愿由ы빀?덈떎.
    // Animator 援ъ“???깆옣, ?대룞, 鍮숈쓽, ?곹샎 ?щ쭩???뚮젅?댁뼱 紐??좊땲硫붿씠?곗? 遺꾨━???ъ슜?⑸땲??

    [Header("References")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private hys_GhostStateSupport ghostStateSupport;
    [SerializeField] private hys_HWJPossessionAnimationBridge possessionAnimationBridge;

    [Header("Animator Parameters")]
    // 怨좎뒪???대룞, 鍮숈쓽 ?깃났, ?쒗븳 ?쒓컙 醫낅즺 ?쒓컙留?Animator???꾨떖?⑸땲??
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string appearTriggerParameter = "Appear";
    [SerializeField] private string possessTriggerParameter = "Possess";
    [SerializeField] private string deadTriggerParameter = "Dead";

    [Header("Move Check")]
    // ??媛믩낫???낅젰?대굹 ?띾룄媛 ?щ㈃ ?대룞 以묒쑝濡??먮떒?⑸땲??
    [SerializeField] private float moveThreshold = 0.05f;

    [Header("Facing")]
    // 醫뚯슦 諛⑺뼢? ?좊땲硫붿씠???뚮씪誘명꽣媛 ?꾨땲???ㅽ봽?쇱씠??flipX濡?泥섎━?⑸땲??
    [SerializeField] private bool flipByMoveDirection = true;
    [SerializeField] private bool rightFacingSprite = true;

    [Header("Soul Appear")]
    // 육체 전용 영혼 이탈 모션을 재생한 경우 공용 Appear가 다시 나오는 것을 막습니다.
    [SerializeField] private bool suppressAppearAfterBodySoul;

    private int isMovingHash;
    private int appearTriggerHash;
    private int possessTriggerHash;
    private int deadTriggerHash;
    private HWJ_SoulRuntimeState previousSoulState;
    private float lastMoveDirectionX = 1f;

    private void Awake()
    {
        CacheReferences();
        CacheParameterHashes();
        previousSoulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
    }

    private void OnValidate()
    {
        CacheReferences();
        CacheParameterHashes();
    }

    private void Update()
    {
        CacheReferences();
        UpdateGhostAnimation();
        UpdateFacing();
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponentInParent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponentInParent<HWJ_PossessionSystem>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponentInParent<HWJ_PlayerInputSystem>();
        }

        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody2D>();
        }

        if (ghostStateSupport == null)
        {
            ghostStateSupport = GetComponentInParent<hys_GhostStateSupport>();
        }

        if (possessionAnimationBridge == null)
        {
            possessionAnimationBridge = GetComponentInParent<hys_HWJPossessionAnimationBridge>();
        }
    }

    private void CacheParameterHashes()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        appearTriggerHash = Animator.StringToHash(appearTriggerParameter);
        possessTriggerHash = Animator.StringToHash(possessTriggerParameter);
        deadTriggerHash = Animator.StringToHash(deadTriggerParameter);
    }

    private void UpdateGhostAnimation()
    {
        if (animator == null)
        {
            return;
        }

        HWJ_SoulRuntimeState currentSoulState = GetCurrentSoulState();
        Vector2 move = GetGhostMoveVector();
        bool isGhost = IsGhostState(currentSoulState);
        bool isMoving = isGhost && move.sqrMagnitude > moveThreshold * moveThreshold;

        // 육체 상태에서는 플레이어 Animator의 IsMoving 값을 덮어쓰지 않습니다.
        if (isGhost)
        {
            SetBoolIfExists(isMovingHash, isMoving);
        }
        else if (currentSoulState == HWJ_SoulRuntimeState.Dead)
        {
            SetBoolIfExists(isMovingHash, false);
        }
        // 전용 브리지가 연출 중일 때는 같은 Animator에 Appear/Possess를 다시 보내지 않습니다.
        bool bridgeOwnsAnimator = possessionAnimationBridge != null
            && possessionAnimationBridge.IsPossessionAnimationPlaying;
        if (!bridgeOwnsAnimator)
        {
            TryPlayAppearTrigger(currentSoulState);
            TryPlayPossessTrigger(currentSoulState);
            TryPlayDeadTrigger(currentSoulState);
        }

        previousSoulState = currentSoulState;
    }

    private void UpdateFacing()
    {
        if (!IsGhostState(GetCurrentSoulState())
            || !flipByMoveDirection
            || spriteRenderer == null)
        {
            return;
        }

        Vector2 move = GetGhostMoveVector();
        if (Mathf.Abs(move.x) > moveThreshold)
        {
            lastMoveDirectionX = Mathf.Sign(move.x);
        }

        bool faceLeft = lastMoveDirectionX < 0f;
        spriteRenderer.flipX = rightFacingSprite ? faceLeft : !faceLeft;
    }

    private Vector2 GetGhostMoveVector()
    {
        // 실제 WASD 입력을 처리하는 유령 이동 스크립트의 값을 가장 먼저 사용합니다.
        if (ghostStateSupport != null
            && ghostStateSupport.IsGhostActive
            && ghostStateSupport.GhostMoveInput.sqrMagnitude > moveThreshold * moveThreshold)
        {
            return Vector2.ClampMagnitude(ghostStateSupport.GhostMoveInput, 1f);
        }

        // HWJ ?낅젰 ?쒖뒪?쒖쓽 ?대룞媛믪쓣 ?곗꽑 ?ъ슜?섍퀬, ?놁쑝硫?Rigidbody ?띾룄濡?蹂댁젙?⑸땲??
        if (playerInput != null && playerInput.MoveInput.sqrMagnitude > moveThreshold * moveThreshold)
        {
            return Vector2.ClampMagnitude(playerInput.MoveInput, 1f);
        }

        return rb != null ? rb.linearVelocity : Vector2.zero;
    }

    private HWJ_SoulRuntimeState GetCurrentSoulState()
    {
        return soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
    }

    private bool IsGhostState(HWJ_SoulRuntimeState state)
    {
        return state == HWJ_SoulRuntimeState.Soul
            || state == HWJ_SoulRuntimeState.BodyToSoul;
    }

    private void TryPlayAppearTrigger(HWJ_SoulRuntimeState currentSoulState)
    {
        // BodyToSoul과 Soul에서 각각 호출하면 두 번 재생되므로 전환 완료 순간에만 실행합니다.
        bool completedSoulTransition = previousSoulState == HWJ_SoulRuntimeState.BodyToSoul
            && currentSoulState == HWJ_SoulRuntimeState.Soul;

        if (!completedSoulTransition)
        {
            return;
        }

        // 전용 브리지에서 Soul Exit를 이미 재생했다면 같은 전환의 공용 Appear는 소비하고 끝냅니다.
        bool soulExitAlreadyShowedGhost = possessionAnimationBridge != null
            && possessionAnimationBridge.ConsumeSoulExitAppearSuppression();
        if (!soulExitAlreadyShowedGhost && !suppressAppearAfterBodySoul)
        {
            SetTriggerIfExists(appearTriggerHash);
        }
    }

    private void TryPlayPossessTrigger(HWJ_SoulRuntimeState currentSoulState)
    {
        // Soul?먯꽌 Body濡??뚯븘?ㅺ퀬 ?ㅼ젣 鍮숈쓽 紐몄씠 ?덉쓣 ?뚮쭔 Possess ?몃━嫄곕? 蹂대깄?덈떎.
        bool changedFromGhostToBody = previousSoulState == HWJ_SoulRuntimeState.Soul
            && currentSoulState == HWJ_SoulRuntimeState.Body;
        bool hasPossessedBody = possessionSystem != null && possessionSystem.HasActivePossessedBody;

        if (changedFromGhostToBody && hasPossessedBody)
        {
            SetTriggerIfExists(possessTriggerHash);
        }
    }

    private void TryPlayDeadTrigger(HWJ_SoulRuntimeState currentSoulState)
    {
        // ?좊졊 10珥???대㉧媛 ?앸굹 Dead ?곹깭媛 ?섎뒗 ?쒓컙 ?щ씪吏?二쎌쓬 紐⑥뀡 ?몃━嫄곕? 蹂대깄?덈떎.
        if (previousSoulState != HWJ_SoulRuntimeState.Dead
            && currentSoulState == HWJ_SoulRuntimeState.Dead)
        {
            SetTriggerIfExists(deadTriggerHash);
        }
    }

    private void SetBoolIfExists(int parameterHash, bool value)
    {
        if (HasParameter(parameterHash, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterHash, value);
        }
    }

    private void SetTriggerIfExists(int parameterHash)
    {
        if (HasParameter(parameterHash, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parameterHash);
        }
    }

    private bool HasParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash && parameters[i].type == parameterType)
            {
                return true;
            }
        }

        return false;
    }
}
