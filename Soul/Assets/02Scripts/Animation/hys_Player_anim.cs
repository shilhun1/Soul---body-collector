using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(hys_Player_Movement))]
public class hys_Player_anim : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float moveThreshold = 0.01f;

    [Header("Parameters")]
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string moveXParameter = "MoveX";
    [SerializeField] private string moveStartTrigger = "MoveStart";
    [SerializeField] private string moveEndTrigger = "MoveEnd";
    [SerializeField] private string isDashingParameter = "IsDashing";
    [SerializeField] private string dashTrigger = "Dash";

    [Header("Direction")]
    [SerializeField] private bool flipSpriteByDirection = true;

    private Rigidbody2D rb;
    private hys_Player_Movement playerMovement;
    private bool wasMoving;
    private bool wasDashing;
    private int isMovingHash;
    private int moveXHash;
    private int moveStartHash;
    private int moveEndHash;
    private int isDashingHash;
    private int dashHash;

    private bool IsMoving => Mathf.Abs(GetMoveVelocity().x) > moveThreshold;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<hys_Player_Movement>();

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
        float moveX = playerMovement != null ? playerMovement.LastMoveDirection : Mathf.Sign(GetMoveVelocity().x);

        animator.SetBool(isMovingHash, isMoving);
        animator.SetBool(isDashingHash, isDashing);
        animator.SetFloat(moveXHash, moveX);

        if (isDashing && !wasDashing)
        {
            animator.SetTrigger(dashHash);
        }

        if (!isDashing)
        {
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

        wasMoving = isMoving;
        wasDashing = isDashing;
    }

    private void CacheParameterHashes()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        moveXHash = Animator.StringToHash(moveXParameter);
        moveStartHash = Animator.StringToHash(moveStartTrigger);
        moveEndHash = Animator.StringToHash(moveEndTrigger);
        isDashingHash = Animator.StringToHash(isDashingParameter);
        dashHash = Animator.StringToHash(dashTrigger);
    }
}
