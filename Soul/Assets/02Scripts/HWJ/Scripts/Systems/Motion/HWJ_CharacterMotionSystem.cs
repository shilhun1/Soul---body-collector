using UnityEngine;

public class HWJ_CharacterMotionSystem : MonoBehaviour
{
    private const string JumpMotionKey = "Jump";
    private const string DoubleJumpMotionKey = "DoubleJump";
    private const string DropJumpMotionKey = "DropJump";
    private const string DashMotionKey = "Dash";
    private const string HitMotionKey = "Hit";
    private const string DeadMotionKey = "Dead";

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PlayerMovementSystem playerMovement;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

    [Header("Motion Profiles")]
    [SerializeField] private HWJ_MotionProfileSO fallbackMotionProfile;
    [SerializeField] private HWJ_MotionProfileSO[] weaponMotionProfiles;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string horizontalSpeedParameter = "HorizontalSpeed";
    [SerializeField] private string verticalSpeedParameter = "VerticalSpeed";
    [SerializeField] private string isMovingParameter = "IsMoving";
    [SerializeField] private string isGroundedParameter = "IsGrounded";
    [SerializeField] private string isPossessedParameter = "IsPossessed";
    [SerializeField] private string isSoulParameter = "IsSoul";
    [SerializeField] private string isDeadParameter = "IsDead";
    [SerializeField] private string runtimeStateParameter = "RuntimeState";
    [SerializeField] private string weaponTypeParameter = "WeaponType";
    [SerializeField] private string attackTriggerParameter = "AttackTrigger";
    [SerializeField] private string jumpTriggerParameter = "JumpTrigger";
    [SerializeField] private string doubleJumpTriggerParameter = "DoubleJumpTrigger";
    [SerializeField] private string dropJumpTriggerParameter = "DropJumpTrigger";
    [SerializeField] private string dashTriggerParameter = "DashTrigger";
    [SerializeField] private string hitTriggerParameter = "HitTrigger";
    [SerializeField] private string deadTriggerParameter = "DeadTrigger";

    [Header("Facing")]
    [SerializeField] private bool flipSpriteByMoveDirection = true;
    [SerializeField] private bool useInitialSpriteFlipAsRightFacing = true;
    [SerializeField] private bool flipTransformScaleWhenNoSpriteRenderer;
    [Tooltip("켜져 있으면 Rigidbody2D 속도에 따라 자동으로 방향을 전환합니다. 몬스터 AI는 플레이어 인식 전 방향 고정을 위해 런타임에서 끕니다.")]
    [SerializeField] private bool updateFacingFromVelocity = true;
    [SerializeField] private float moveThreshold = 0.05f;
    [SerializeField] private float facingThreshold = 0.01f;

    private HWJ_RuntimeState previousRuntimeState = HWJ_RuntimeState.None;
    private HWJ_WeaponType currentWeaponType = HWJ_WeaponType.None;
    private HWJ_MotionProfileSO currentMotionProfile;
    private Vector3 lastPosition;
    private bool hasLastPosition;
    private int lastAttackMotionFrame = -1;
    private int lastHitMotionFrame = -1;
    private int lastDeadMotionFrame = -1;
    private bool initialSpriteFlipX;
    private bool hasInitialSpriteFlipX;
    private float initialScaleXSign = 1f;
    private bool hasInitialScaleXSign;

    public float CurrentFacingDirection => ResolveCurrentFacingDirection();
    public bool UpdatesFacingFromVelocity => updateFacingFromVelocity;
    public SpriteRenderer VisualRenderer
    {
        get
        {
            CacheReferences();
            return spriteRenderer;
        }
    }

    public Animator VisualAnimator
    {
        get
        {
            CacheReferences();
            return animator;
        }
    }

    private void Awake()
    {
        CacheReferences();
        RefreshFacingBaseline();
        previousRuntimeState = runtimeStatus != null ? runtimeStatus.CurrentState : HWJ_RuntimeState.None;
        ApplyMotionProfile(true);
    }

    private void OnEnable()
    {
        hasLastPosition = false;
        previousRuntimeState = runtimeStatus != null ? runtimeStatus.CurrentState : HWJ_RuntimeState.None;
    }

    private void Update()
    {
        CacheReferences();
        ApplyMotionProfile(false);
        UpdateAnimatorParameters();
        UpdateStateTriggers();
        UpdateFacing();
        CachePosition();
    }

    public void PlayAttack(string motionKey)
    {
        lastAttackMotionFrame = Time.frameCount;

        if (PlayMotionKey(motionKey))
        {
            return;
        }

        if (currentMotionProfile != null
            && currentMotionProfile.TryGetDefaultAttackMotion(out HWJ_MotionClipData defaultAttackMotion)
            && PlayMotion(defaultAttackMotion))
        {
            return;
        }

        SetAnimatorTrigger(attackTriggerParameter);
    }

    public void PlayJump()
    {
        if (!PlayMotionKey(JumpMotionKey))
        {
            SetAnimatorTrigger(jumpTriggerParameter);
        }
    }

    public void PlayDoubleJump()
    {
        if (!PlayMotionKey(DoubleJumpMotionKey))
        {
            SetAnimatorTrigger(doubleJumpTriggerParameter);
        }
    }

    public void PlayDropJump()
    {
        if (!PlayMotionKey(DropJumpMotionKey))
        {
            SetAnimatorTrigger(dropJumpTriggerParameter);
        }
    }

    public void PlayDash()
    {
        if (!PlayMotionKey(DashMotionKey))
        {
            SetAnimatorTrigger(dashTriggerParameter);
        }
    }

    public void PlayHit()
    {
        lastHitMotionFrame = Time.frameCount;

        if (!PlayMotionKey(HitMotionKey))
        {
            SetAnimatorTrigger(hitTriggerParameter);
        }
    }

    public void PlayDead()
    {
        lastDeadMotionFrame = Time.frameCount;

        if (!PlayMotionKey(DeadMotionKey))
        {
            SetAnimatorTrigger(deadTriggerParameter);
        }
    }

    public bool PlayMotionKey(string motionKey)
    {
        if (string.IsNullOrEmpty(motionKey))
        {
            return false;
        }

        if (currentMotionProfile != null
            && currentMotionProfile.TryGetMotion(motionKey, out HWJ_MotionClipData motion)
            && PlayMotion(motion))
        {
            return true;
        }

        return SetAnimatorTrigger(motionKey);
    }

    /// <summary>
    /// Stores the current renderer flip as the visual state used when this character faces right.
    /// Use this after swapping a sprite, because some sprites are imported facing left and are corrected in the prefab with flipX.
    /// </summary>
    public void RefreshFacingBaseline()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            initialSpriteFlipX = spriteRenderer.flipX;
            hasInitialSpriteFlipX = true;
        }

        float scaleSign = Mathf.Sign(transform.localScale.x);
        initialScaleXSign = scaleSign == 0f ? 1f : scaleSign;
        hasInitialScaleXSign = true;
    }

    public void FaceDirection(float directionX)
    {
        if (Mathf.Abs(directionX) <= facingThreshold)
        {
            return;
        }

        ApplyFacing(directionX < 0f);
    }

    /// <summary>
    /// 이동 속도가 바뀔 때 자동으로 바라보는 방향까지 바꿀지 설정합니다.
    /// 명시적인 FaceDirection 호출은 이 설정과 관계없이 계속 동작합니다.
    /// </summary>
    public void SetVelocityFacingEnabled(bool isEnabled)
    {
        updateFacingFromVelocity = isEnabled;
    }

    public float ResolveCurrentFacingDirection()
    {
        CacheReferences();

        if (spriteRenderer != null && flipSpriteByMoveDirection)
        {
            if (!hasInitialSpriteFlipX)
            {
                initialSpriteFlipX = spriteRenderer.flipX;
                hasInitialSpriteFlipX = true;
            }

            bool faceLeft = useInitialSpriteFlipAsRightFacing
                ? spriteRenderer.flipX != initialSpriteFlipX
                : spriteRenderer.flipX;
            return faceLeft ? -1f : 1f;
        }

        float scaleSign = Mathf.Sign(transform.localScale.x);
        float rightFacingSign = hasInitialScaleXSign ? initialScaleXSign : 1f;
        scaleSign = scaleSign == 0f ? rightFacingSign : scaleSign;
        return Mathf.Approximately(scaleSign, rightFacingSign) ? 1f : -1f;
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<HWJ_PlayerMovementSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (!hasInitialSpriteFlipX && spriteRenderer != null)
        {
            initialSpriteFlipX = spriteRenderer.flipX;
            hasInitialSpriteFlipX = true;
        }

        if (!hasInitialScaleXSign)
        {
            float scaleSign = Mathf.Sign(transform.localScale.x);
            initialScaleXSign = scaleSign == 0f ? 1f : scaleSign;
            hasInitialScaleXSign = true;
        }
    }

    private void ApplyMotionProfile(bool force)
    {
        HWJ_WeaponType nextWeaponType = ResolveWeaponType();
        HWJ_MotionProfileSO nextProfile = ResolveMotionProfile(nextWeaponType);

        if (!force && currentWeaponType == nextWeaponType && currentMotionProfile == nextProfile)
        {
            return;
        }

        currentWeaponType = nextWeaponType;
        currentMotionProfile = nextProfile;

        if (animator != null
            && currentMotionProfile != null
            && currentMotionProfile.AnimatorController != null
            && animator.runtimeAnimatorController != currentMotionProfile.AnimatorController)
        {
            animator.runtimeAnimatorController = currentMotionProfile.AnimatorController;
        }
    }

    private HWJ_WeaponType ResolveWeaponType()
    {
        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            return possessionSystem.CurrentWeaponType;
        }

        return dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
    }

    private HWJ_MotionProfileSO ResolveMotionProfile(HWJ_WeaponType weaponType)
    {
        if (weaponMotionProfiles != null)
        {
            for (int i = 0; i < weaponMotionProfiles.Length; i++)
            {
                HWJ_MotionProfileSO profile = weaponMotionProfiles[i];

                if (profile != null && profile.WeaponType == weaponType)
                {
                    return profile;
                }
            }
        }

        return fallbackMotionProfile;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        Vector2 velocity = GetCurrentVelocity();
        float horizontalSpeed = Mathf.Abs(velocity.x);
        float totalSpeed = velocity.magnitude;
        bool isMoving = horizontalSpeed > moveThreshold;
        bool isGrounded = playerMovement != null && playerMovement.IsGrounded;
        bool isPossessed = possessionSystem != null && possessionSystem.HasActivePossessedBody;
        bool isSoul = soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul;
        bool isDead = runtimeStatus != null && runtimeStatus.IsDead;
        HWJ_RuntimeState runtimeState = runtimeStatus != null ? runtimeStatus.CurrentState : HWJ_RuntimeState.None;

        SetAnimatorFloat(speedParameter, totalSpeed);
        SetAnimatorFloat(horizontalSpeedParameter, horizontalSpeed);
        SetAnimatorFloat(verticalSpeedParameter, velocity.y);
        SetAnimatorBool(isMovingParameter, isMoving);
        SetAnimatorBool(isGroundedParameter, isGrounded);
        SetAnimatorBool(isPossessedParameter, isPossessed);
        SetAnimatorBool(isSoulParameter, isSoul);
        SetAnimatorBool(isDeadParameter, isDead);
        SetAnimatorInt(runtimeStateParameter, (int)runtimeState);
        SetAnimatorInt(weaponTypeParameter, (int)currentWeaponType);
    }

    private Vector2 GetCurrentVelocity()
    {
        if (body != null)
        {
            return body.linearVelocity;
        }

        if (!hasLastPosition || Time.deltaTime <= 0f)
        {
            return Vector2.zero;
        }

        Vector3 delta = transform.position - lastPosition;
        return new Vector2(delta.x / Time.deltaTime, delta.y / Time.deltaTime);
    }

    private void UpdateStateTriggers()
    {
        if (runtimeStatus == null)
        {
            return;
        }

        HWJ_RuntimeState currentState = runtimeStatus.CurrentState;

        if (currentState == previousRuntimeState)
        {
            return;
        }

        switch (currentState)
        {
            case HWJ_RuntimeState.Attack:
                if (lastAttackMotionFrame != Time.frameCount)
                {
                    PlayAttack(null);
                }
                break;
            case HWJ_RuntimeState.Hit:
                if (lastHitMotionFrame != Time.frameCount)
                {
                    PlayHit();
                }
                break;
            case HWJ_RuntimeState.Dead:
                if (lastDeadMotionFrame != Time.frameCount)
                {
                    PlayDead();
                }
                break;
        }

        previousRuntimeState = currentState;
    }

    private void UpdateFacing()
    {
        if (!updateFacingFromVelocity)
        {
            return;
        }

        Vector2 velocity = GetCurrentVelocity();

        if (Mathf.Abs(velocity.x) <= facingThreshold)
        {
            return;
        }

        ApplyFacing(velocity.x < 0f);
    }

    private void ApplyFacing(bool faceLeft)
    {
        if (spriteRenderer != null && flipSpriteByMoveDirection)
        {
            if (!hasInitialSpriteFlipX)
            {
                initialSpriteFlipX = spriteRenderer.flipX;
                hasInitialSpriteFlipX = true;
            }

            spriteRenderer.flipX = useInitialSpriteFlipAsRightFacing
                ? faceLeft ? !initialSpriteFlipX : initialSpriteFlipX
                : faceLeft;
            return;
        }

        if (!flipTransformScaleWhenNoSpriteRenderer)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        float absoluteX = Mathf.Abs(scale.x);
        float rightFacingSign = hasInitialScaleXSign ? initialScaleXSign : 1f;
        scale.x = absoluteX * (faceLeft ? -rightFacingSign : rightFacingSign);
        transform.localScale = scale;
    }

    private void CachePosition()
    {
        lastPosition = transform.position;
        hasLastPosition = true;
    }

    private bool PlayMotion(HWJ_MotionClipData motion)
    {
        if (animator == null || motion == null)
        {
            return false;
        }

        bool played = false;

        if (motion.UseTrigger && SetAnimatorTrigger(motion.AnimatorTriggerName, motion.ResetTriggerBeforeSet))
        {
            played = true;
        }

        if (!string.IsNullOrEmpty(motion.AnimatorStateName))
        {
            if (motion.UseCrossFade)
            {
                animator.CrossFadeInFixedTime(
                    motion.AnimatorStateName,
                    Mathf.Max(0f, motion.TransitionSeconds),
                    motion.LayerIndex);
            }
            else
            {
                animator.Play(motion.AnimatorStateName, motion.LayerIndex);
            }

            played = true;
        }

        return played;
    }

    private bool SetAnimatorFloat(string parameterName, float value)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float, out int parameterHash))
        {
            return false;
        }

        animator.SetFloat(parameterHash, value);
        return true;
    }

    private bool SetAnimatorBool(string parameterName, bool value)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool, out int parameterHash))
        {
            return false;
        }

        animator.SetBool(parameterHash, value);
        return true;
    }

    private bool SetAnimatorInt(string parameterName, int value)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Int, out int parameterHash))
        {
            return false;
        }

        animator.SetInteger(parameterHash, value);
        return true;
    }

    private bool SetAnimatorTrigger(string parameterName)
    {
        return SetAnimatorTrigger(parameterName, true);
    }

    private bool SetAnimatorTrigger(string parameterName, bool resetBeforeSet)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger, out int parameterHash))
        {
            return false;
        }

        if (resetBeforeSet)
        {
            animator.ResetTrigger(parameterHash);
        }

        animator.SetTrigger(parameterHash);
        return true;
    }

    private bool HasAnimatorParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType,
        out int parameterHash)
    {
        parameterHash = 0;

        if (animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        parameterHash = Animator.StringToHash(parameterName);
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
