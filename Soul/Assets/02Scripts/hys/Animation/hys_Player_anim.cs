using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(hys_Player_Movement))]
[RequireComponent(typeof(hys_Player_State))]
public class hys_Player_anim : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float moveThreshold = 0.01f;

    [Header("Parameters")]
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string yVelocityParameter = "YVelocity";
    [SerializeField] private string isDashingParameter = "IsDashing";
    [SerializeField] private string isGroundedParameter = "IsGrounded";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string deathTrigger = "Death";

    [Header("Direction")]
    [SerializeField] private bool flipSpriteByDirection = true;

    private Rigidbody2D rb;
    private hys_Player_Movement playerMovement;
    private hys_Player_State playerState;
    private hys_PlayerState previousPlayerState;
    private int previousJumpVersion;
    private int isMovingHash;
    private int moveXHash;
    private int yVelocityHash;
    private int isDashingHash;
    private int isGroundedHash;
    private int jumpHash;
    private int attackHash;
    private int deathHash;

    private bool IsMoving => Mathf.Abs(GetMoveVelocity().x) > moveThreshold;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<hys_Player_Movement>();
        playerState = GetComponent<hys_Player_State>();
        previousPlayerState = playerState != null ? playerState.CurrentState : hys_PlayerState.Idle;
        previousJumpVersion = playerMovement != null ? playerMovement.JumpVersion : 0;

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
        UpdateSpriteDirection();
        UpdateAnimatorParameters();
        previousPlayerState = playerState != null ? playerState.CurrentState : previousPlayerState;
    }

    private Vector2 GetMoveVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }

    private void UpdateSpriteDirection()
    {
        if (!flipSpriteByDirection || spriteRenderer == null || playerMovement == null)
        {
            return;
        }

        if (playerMovement.LastMoveDirection < 0f)
        {
            spriteRenderer.flipX = true;
        }
        else if (playerMovement.LastMoveDirection > 0f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        bool isMoving = IsMoving;
        bool isDashing = playerMovement != null && playerMovement.Is_Dashing;
        bool isGrounded = playerMovement != null && playerMovement.Is_Grounded;
        float yVelocity = GetMoveVelocity().y;
        float moveX = playerMovement != null ? playerMovement.LastMoveDirection : Mathf.Sign(GetMoveVelocity().x);

        animator.SetBool(isMovingHash, isMoving);
        animator.SetBool(isDashingHash, isDashing);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetFloat(moveXHash, moveX);
        animator.SetFloat(yVelocityHash, yVelocity);

        if (playerMovement != null && playerMovement.JumpVersion != previousJumpVersion)
        {
            animator.SetTrigger(jumpHash);
            previousJumpVersion = playerMovement.JumpVersion;
        }

        if (playerState != null && playerState.CurrentState != previousPlayerState)
        {
            if (playerState.CurrentState == hys_PlayerState.Attack)
            {
                animator.SetTrigger(attackHash);
            }
            else if (playerState.CurrentState == hys_PlayerState.Dead)
            {
                animator.SetBool(isMovingHash, false);
                animator.SetBool(isDashingHash, false);
                animator.SetBool(isGroundedHash, isGrounded);
                animator.SetTrigger(deathHash);
            }
        }
    }

    private void CacheParameterHashes()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        moveXHash = Animator.StringToHash(moveXParameter);
        yVelocityHash = Animator.StringToHash(yVelocityParameter);
        isDashingHash = Animator.StringToHash(isDashingParameter);
        isGroundedHash = Animator.StringToHash(isGroundedParameter);
        jumpHash = Animator.StringToHash(jumpTrigger);
        attackHash = Animator.StringToHash(attackTrigger);
        deathHash = Animator.StringToHash(deathTrigger);
    }
}
