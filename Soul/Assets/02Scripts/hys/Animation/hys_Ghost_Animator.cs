using UnityEngine;

[RequireComponent(typeof(Animator))]
public class hys_Ghost_Animator : MonoBehaviour
{
    // 고스트 전용 애니메이션 파라미터를 관리합니다.
    // Animator 구조는 등장, 이동, 빙의, 영혼 사망을 플레이어 몸 애니메이터와 분리해 사용합니다.

    [Header("References")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    // 고스트 이동, 빙의 성공, 제한 시간 종료 순간만 Animator에 전달합니다.
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string appearTriggerParameter = "Appear";
    [SerializeField] private string possessTriggerParameter = "Possess";
    [SerializeField] private string deadTriggerParameter = "Dead";

    [Header("Move Check")]
    // 이 값보다 입력이나 속도가 크면 이동 중으로 판단합니다.
    [SerializeField] private float moveThreshold = 0.05f;

    [Header("Facing")]
    // 좌우 방향은 애니메이션 파라미터가 아니라 스프라이트 flipX로 처리합니다.
    [SerializeField] private bool flipByMoveDirection = true;
    [SerializeField] private bool rightFacingSprite = true;

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

        // IsMoving=false면 Standing, true면 WalkStart/Walking 쪽으로 넘어갑니다.
        SetBoolIfExists(isMovingHash, isMoving);
        TryPlayAppearTrigger(currentSoulState);
        TryPlayPossessTrigger(currentSoulState);
        TryPlayDeadTrigger(currentSoulState);

        previousSoulState = currentSoulState;
    }

    private void UpdateFacing()
    {
        if (!flipByMoveDirection || spriteRenderer == null)
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
        // HWJ 입력 시스템의 이동값을 우선 사용하고, 없으면 Rigidbody 속도로 보정합니다.
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
        // 몸에서 영혼으로 전환이 끝난 순간에만 등장 애니메이션을 한 번 재생합니다.
        if (previousSoulState == HWJ_SoulRuntimeState.BodyToSoul
            && currentSoulState == HWJ_SoulRuntimeState.Soul)
        {
            SetTriggerIfExists(appearTriggerHash);
        }
    }

    private void TryPlayPossessTrigger(HWJ_SoulRuntimeState currentSoulState)
    {
        // Soul에서 Body로 돌아오고 실제 빙의 몸이 있을 때만 Possess 트리거를 보냅니다.
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
        // 유령 10초 타이머가 끝나 Dead 상태가 되는 순간 사라짐/죽음 모션 트리거를 보냅니다.
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
