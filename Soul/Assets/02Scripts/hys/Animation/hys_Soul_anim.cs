using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class hys_Soul_anim : MonoBehaviour
{
    private enum MoveDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float moveThreshold = 0.01f;

    [Header("Parameters")]
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveYParameter = "MoveY";
    [SerializeField] private string moveStartTrigger = "MoveStart";
    [SerializeField] private string moveEndTrigger = "MoveEnd";
    [SerializeField] private string appearTrigger = "Appear";

    [Header("Direction")]
    [SerializeField] private bool flipSpriteByHorizontalDirection = true;

    private Rigidbody2D rb;
    private HWJ_SoulSystem soulSystem;
    private Vector2 lastMoveDirection = Vector2.right;
    private MoveDirection currentDirection = MoveDirection.Right;
    private HWJ_SoulRuntimeState previousSoulState;
    private bool wasMoving;
    private int isMovingHash;
    private int moveXHash;
    private int moveYHash;
    private int moveStartHash;
    private int moveEndHash;
    private int appearHash;

    public Vector2 LastMoveDirection => lastMoveDirection;
    public bool IsMoving => GetMoveVelocity().sqrMagnitude > moveThreshold * moveThreshold;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        soulSystem = GetComponent<HWJ_SoulSystem>();
        previousSoulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        CacheParameterHashes();
    }

    private void Update()
    {
        Vector2 moveVelocity = GetMoveVelocity();
        bool isMoving = moveVelocity.sqrMagnitude > moveThreshold * moveThreshold;

        UpdateMoveDirection(moveVelocity, isMoving);
        UpdateSoulAppearTrigger();
        UpdateAnimatorParameters(isMoving);

        wasMoving = isMoving;
        previousSoulState = soulSystem != null ? soulSystem.CurrentState : previousSoulState;
    }

    private Vector2 GetMoveVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }

    private void UpdateMoveDirection(Vector2 moveVelocity, bool isMoving)
    {
        if (!isMoving)
        {
            return;
        }

        lastMoveDirection = moveVelocity.normalized;
        currentDirection = GetDominantDirection(lastMoveDirection);
        UpdateSpriteFlip();
    }

    private void UpdateAnimatorParameters(bool isMoving)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(isMovingHash, isMoving);
        animator.SetFloat(moveXHash, lastMoveDirection.x);
        animator.SetFloat(moveYHash, lastMoveDirection.y);

        if (isMoving && !wasMoving)
        {
            animator.ResetTrigger(moveEndHash);
            animator.SetTrigger(moveStartHash);
        }
        else if (!isMoving && wasMoving)
        {
            animator.ResetTrigger(moveStartHash);
            animator.SetTrigger(moveEndHash);
        }
    }

    private void UpdateSoulAppearTrigger()
    {
        if (animator == null || soulSystem == null)
        {
            return;
        }

        if (previousSoulState != HWJ_SoulRuntimeState.Soul &&
            soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            animator.ResetTrigger(moveStartHash);
            animator.ResetTrigger(moveEndHash);
            animator.SetTrigger(appearHash);
        }
    }

    private MoveDirection GetDominantDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x < 0f ? MoveDirection.Left : MoveDirection.Right;
        }

        return direction.y < 0f ? MoveDirection.Down : MoveDirection.Up;
    }

    private void UpdateSpriteFlip()
    {
        if (!flipSpriteByHorizontalDirection || spriteRenderer == null)
        {
            return;
        }

        if (currentDirection == MoveDirection.Left)
        {
            spriteRenderer.flipX = true;
        }
        else if (currentDirection == MoveDirection.Right)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void CacheParameterHashes()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        moveXHash = Animator.StringToHash(moveXParameter);
        moveYHash = Animator.StringToHash(moveYParameter);
        moveStartHash = Animator.StringToHash(moveStartTrigger);
        moveEndHash = Animator.StringToHash(moveEndTrigger);
        appearHash = Animator.StringToHash(appearTrigger);
    }
}
