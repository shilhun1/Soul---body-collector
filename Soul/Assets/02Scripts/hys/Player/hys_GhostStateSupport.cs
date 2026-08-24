using UnityEngine;
using UnityEngine.InputSystem;

// 유령 상태에서만 자유비행, 벽 통과, 중력 제거를 적용하는 플레이어 보조 스크립트입니다.
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(100)]
public class hys_GhostStateSupport : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private hys_Player_State playerState;
    [SerializeField] private hys_Player_Attack playerAttack;

    [Header("Ghost Movement")]
    // 유령은 방향키로만 상하좌우 자유롭게 이동합니다.
    [SerializeField, Min(0f)] private float ghostMoveSpeed = 9f;
    [SerializeField] private bool normalizeDiagonalMovement = true;
    [SerializeField] private bool stopVelocityOnStateChange = true;

    [Header("Ghost Physics")]
    // 유령 상태에서는 육체용 콜라이더를 꺼 큰 플레이어 박스가 유령을 따라오지 않게 합니다.
    [SerializeField] private bool disableBodyCollidersWhileGhost = true;
    // 콜라이더 비활성화를 끈 경우에는 Trigger 방식으로 벽과 바닥을 통과합니다.
    [SerializeField] private bool passThroughAllColliders = true;

    [Header("Ghost Actions")]
    // 유령 상태에서는 몸 전용 공격 스크립트를 꺼 공격 입력을 차단합니다.
    [SerializeField] private bool disableAttackWhileGhost = true;

    [Header("Invincible")]
    // 유령 상태 동안 짧은 무적 시간을 계속 갱신합니다.
    [SerializeField] private bool grantInvincibleWhileGhost = true;
    [SerializeField, Min(0.02f)] private float invincibleRefreshSeconds = 0.25f;

    [Header("Layer")]
    // Ghost 레이어가 프로젝트에 있을 때만 유령 상태의 레이어를 함께 변경합니다.
    [SerializeField] private bool changeLayerWhileGhost;
    [SerializeField] private string ghostLayerName = "Ghost";
    [SerializeField] private bool includeChildren = true;

    [Header("Map Bounds")]
    [SerializeField] private bool clampToMapBounds;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -20f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 20f);

    [Header("Debug")]
    [SerializeField] private bool isGhostActive;
    [SerializeField] private Vector2 ghostMoveInput;
    [SerializeField] private string lastGhostSupportState;

    private Transform[] cachedTransforms;
    private int[] originalLayers;
    private Collider2D[] cachedColliders;
    private bool[] originalColliderEnabledStates;
    private bool[] originalTriggerStates;
    private float originalGravityScale;
    private bool originalAttackEnabled;
    private bool hasPhysicsBackup;
    private bool hasLayerBackup;

    public bool IsGhostActive => isGhostActive;
    public Vector2 GhostMoveInput => ghostMoveInput;

    private void Awake()
    {
        CacheReferences();
        CachePhysicsBackup();
        CacheLayerBackup();
    }

    private void OnEnable()
    {
        CacheReferences();
        RefreshGhostModeImmediately();
    }

    private void Update()
    {
        CacheReferences();

        if (hys_PlayerCinematicControlLock.IsLockedFor(transform))
        {
            ghostMoveInput = Vector2.zero;
            return;
        }

        bool shouldBeGhost = IsGhostState();
        if (isGhostActive != shouldBeGhost)
        {
            SetGhostMode(shouldBeGhost);
        }

        if (!shouldBeGhost)
        {
            ghostMoveInput = Vector2.zero;
            return;
        }

        if (IsDeadState())
        {
            // 제한 시간이 끝난 뒤에는 유령 물리를 유지한 채 이동과 공격만 완전히 멈춥니다.
            ghostMoveInput = Vector2.zero;
            if (playerState != null && playerState.CurrentState != hys_PlayerState.Dead)
            {
                playerState.SetDead();
            }
            return;
        }

        ghostMoveInput = ReadGhostMoveInput();
        RefreshGhostInvincible();
    }

    private void FixedUpdate()
    {
        if (!isGhostActive || rb == null)
        {
            return;
        }

        if (hys_PlayerCinematicControlLock.IsLockedFor(transform))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 중력과 기존 몸 속도를 완전히 무시하고 유령 입력만 Rigidbody에 적용합니다.
        rb.gravityScale = 0f;
        rb.linearVelocity = ghostMoveInput * ghostMoveSpeed;
    }

    private void LateUpdate()
    {
        if (isGhostActive && clampToMapBounds)
        {
            ClampPositionToMapBounds();
        }
    }

    private void OnDisable()
    {
        RestoreBodyMode();
    }

    private void OnDestroy()
    {
        RestoreBodyMode();
    }

    private void CacheReferences()
    {
        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (playerState == null)
        {
            playerState = GetComponent<hys_Player_State>();
        }

        if (playerAttack == null)
        {
            playerAttack = GetComponent<hys_Player_Attack>();
        }
    }

    private void RefreshGhostModeImmediately()
    {
        bool shouldBeGhost = IsGhostState();
        if (shouldBeGhost != isGhostActive)
        {
            SetGhostMode(shouldBeGhost);
        }
    }

    private void SetGhostMode(bool active)
    {
        if (!hasPhysicsBackup)
        {
            CachePhysicsBackup();
        }

        isGhostActive = active;

        if (active)
        {
            ApplyGhostPhysics();
            ApplyGhostLayer();
            SetAttackEnabled(false);

            if (playerState != null && IsDeadState())
            {
                playerState.SetDead();
            }
            else if (playerState != null && playerState.CurrentState == hys_PlayerState.Dead)
            {
                // 육체 사망은 최종 사망이 아니므로 Soul 전환 시 행동 상태 잠금을 해제합니다.
                playerState.ResetFromDead(hys_PlayerState.Idle);
            }
            else if (playerState != null)
            {
                playerState.SetState(hys_PlayerState.Idle);
            }

            lastGhostSupportState = "유령 자유비행 활성화";
            return;
        }

        RestoreBodyMode();
        lastGhostSupportState = "육신 이동 복구";
    }

    private bool IsGhostState()
    {
        // BodyToSoul 동안에는 육신의 Die 애니메이션과 바닥 물리를 유지하고,
        // 전환이 끝나 Soul이 된 시점부터 유령 물리를 적용합니다.
        return soulSystem != null
            && (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
                || soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead);
    }

    private bool IsDeadState()
    {
        return soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead;
    }

    private Vector2 ReadGhostMoveInput()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.downArrowKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.upArrowKey.isPressed)
        {
            input.y += 1f;
        }

        return normalizeDiagonalMovement && input.sqrMagnitude > 1f
            ? input.normalized
            : input;
    }

    private void CachePhysicsBackup()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        originalGravityScale = rb != null ? rb.gravityScale : 0f;
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
        originalColliderEnabledStates = new bool[cachedColliders.Length];
        originalTriggerStates = new bool[cachedColliders.Length];

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            originalColliderEnabledStates[i] = cachedColliders[i] != null && cachedColliders[i].enabled;
            originalTriggerStates[i] = cachedColliders[i] != null && cachedColliders[i].isTrigger;
        }

        originalAttackEnabled = playerAttack != null && playerAttack.enabled;
        hasPhysicsBackup = true;
    }

    private void ApplyGhostPhysics()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;

            if (stopVelocityOnStateChange)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        if (cachedColliders == null)
        {
            return;
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] == null)
            {
                continue;
            }

            if (disableBodyCollidersWhileGhost)
            {
                cachedColliders[i].enabled = false;
            }
            else if (passThroughAllColliders)
            {
                cachedColliders[i].isTrigger = true;
            }
        }
    }

    private void RestoreBodyMode()
    {
        isGhostActive = false;
        ghostMoveInput = Vector2.zero;

        if (hasPhysicsBackup)
        {
            if (rb != null)
            {
                rb.gravityScale = originalGravityScale;

                if (stopVelocityOnStateChange)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }

            if (cachedColliders != null && originalTriggerStates != null)
            {
                for (int i = 0; i < cachedColliders.Length && i < originalTriggerStates.Length; i++)
                {
                    if (cachedColliders[i] != null)
                    {
                        if (originalColliderEnabledStates != null && i < originalColliderEnabledStates.Length)
                        {
                            cachedColliders[i].enabled = originalColliderEnabledStates[i];
                        }

                        cachedColliders[i].isTrigger = originalTriggerStates[i];
                    }
                }
            }
        }

        RestoreOriginalLayers();
        SetAttackEnabled(originalAttackEnabled);

        if (playerState != null && playerState.CurrentState != hys_PlayerState.Dead)
        {
            playerState.SetState(hys_PlayerState.Idle);
        }
    }

    private void SetAttackEnabled(bool enabled)
    {
        if (disableAttackWhileGhost && playerAttack != null)
        {
            playerAttack.enabled = enabled;
        }
    }

    private void RefreshGhostInvincible()
    {
        if (grantInvincibleWhileGhost && runtimeStatus != null)
        {
            runtimeStatus.GrantInvincibility(invincibleRefreshSeconds);
        }
    }

    private void ClampPositionToMapBounds()
    {
        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);

        if (rb != null)
        {
            rb.position = position;
            return;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void CacheLayerBackup()
    {
        cachedTransforms = includeChildren
            ? GetComponentsInChildren<Transform>(true)
            : new[] { transform };

        originalLayers = new int[cachedTransforms.Length];

        for (int i = 0; i < cachedTransforms.Length; i++)
        {
            originalLayers[i] = cachedTransforms[i] != null
                ? cachedTransforms[i].gameObject.layer
                : gameObject.layer;
        }

        hasLayerBackup = true;
    }

    private void ApplyGhostLayer()
    {
        if (!changeLayerWhileGhost)
        {
            return;
        }

        int ghostLayer = LayerMask.NameToLayer(ghostLayerName);
        if (ghostLayer < 0)
        {
            lastGhostSupportState = "Ghost 레이어가 없어 Trigger 벽 통과만 적용";
            return;
        }

        if (!hasLayerBackup)
        {
            CacheLayerBackup();
        }

        for (int i = 0; i < cachedTransforms.Length; i++)
        {
            if (cachedTransforms[i] != null)
            {
                cachedTransforms[i].gameObject.layer = ghostLayer;
            }
        }
    }

    private void RestoreOriginalLayers()
    {
        if (!hasLayerBackup || cachedTransforms == null || originalLayers == null)
        {
            return;
        }

        for (int i = 0; i < cachedTransforms.Length && i < originalLayers.Length; i++)
        {
            if (cachedTransforms[i] != null)
            {
                cachedTransforms[i].gameObject.layer = originalLayers[i];
            }
        }
    }
}
