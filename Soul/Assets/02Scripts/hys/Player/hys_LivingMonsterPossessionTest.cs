using UnityEngine;
using UnityEngine.InputSystem;

// 이 컴포넌트가 붙은 몬스터 한 마리만 살아 있는 상태에서 E키 빙의를 시험할 수 있습니다.
[DisallowMultipleComponent]
public class hys_LivingMonsterPossessionTest : MonoBehaviour
{
    [Header("실험 대상")]
    [SerializeField] private HWJ_RootObjectDataResolver targetResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem targetStatus;
    [SerializeField] private hys_BodyCollisionProfile collisionProfile;
    [SerializeField] private hys_PossessionFeetAnchor feetAnchor;

    [Header("플레이어")]
    [SerializeField] private HWJ_SoulSystem playerSoulSystem;
    [SerializeField] private HWJ_PossessionSystem playerPossessionSystem;
    [SerializeField] private hys_PossessionPhysicsResolver possessionPhysicsResolver;

    [Header("빙의 위치 및 충돌")]
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private Collider2D playerBodyCollider;
    [SerializeField] private Collider2D targetBodyCollider;
    [SerializeField] private bool alignColliderBottomAfterPossession = true;
    [SerializeField] private bool restorePlayerCollisionImmediately = true;
    [SerializeField] private float groundClearance = 0.05f;
    [SerializeField] private bool findActualGroundSurface = true;
    [SerializeField, Min(0.1f)] private float groundSearchDistance = 12f;
    [SerializeField] private LayerMask groundLayerMask = ~0;

    [Header("입력")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3.5f;
    [SerializeField] private bool requireEKey = true;

    [Header("디버그")]
    [SerializeField] private bool isPlayerInRange;
    [SerializeField] private string lastResult;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        CacheReferences();
        isPlayerInRange = IsPlayerInRange();

        if (!isPlayerInRange || !IsPossessionInputPressed())
        {
            return;
        }

        TryPossessLivingTarget();
    }

    // 인스펙터나 다른 hys 테스트 코드에서도 같은 실험을 호출할 수 있게 공개합니다.
    public bool TryPossessLivingTarget()
    {
        CacheReferences();

        if (playerSoulSystem == null || playerPossessionSystem == null)
        {
            return StoreResult(false, "플레이어의 HWJ 빙의 시스템을 찾지 못했습니다.");
        }

        if (targetResolver == null || targetStatus == null)
        {
            return StoreResult(false, "실험 몬스터의 데이터 또는 상태 컴포넌트가 없습니다.");
        }

        if (playerSoulSystem.CurrentState != HWJ_SoulRuntimeState.Soul)
        {
            return StoreResult(false, "유령 상태에서만 살아 있는 몬스터에게 빙의할 수 있습니다.");
        }

        if (!IsPlayerInRange())
        {
            return StoreResult(false, "실험 몬스터에게 더 가까이 이동해야 합니다.");
        }

        if (!TryGetPossessionData(out HWJ_PossessionData possessionData))
        {
            return StoreResult(false, "실험 몬스터에 빙의 데이터가 없습니다.");
        }

        bool originalCanBePossessed = possessionData.canBePossessed;
        bool originalRequiresDefeatedState = possessionData.requiresDefeatedState;
        HWJ_RuntimeState originalRuntimeState = targetStatus.CurrentState;
        float placementSurfaceY = feetAnchor != null
            ? feetAnchor.WorldPosition.y
            : targetBodyCollider != null
                ? targetBodyCollider.bounds.min.y
                : transform.position.y;

        if (findActualGroundSurface && TryFindActualGroundSurface(out float actualGroundY))
        {
            placementSurfaceY = actualGroundY;
        }

        Vector2 placementFeet = feetAnchor != null
            ? feetAnchor.WorldPosition
            : new Vector2(transform.position.x, placementSurfaceY);
        placementFeet.y = placementSurfaceY;
        hys_BodyCollisionSettings collisionSettings = collisionProfile != null
            ? collisionProfile.GetSettings()
            : hys_BodyCollisionSettings.FromCollider(playerBodyCollider as BoxCollider2D, groundClearance);
        bool physicsTransitionStarted = possessionPhysicsResolver != null
            && possessionPhysicsResolver.BeginPossessionTransition();

        try
        {
            // HWJ 원본을 수정하지 않고 이 한 번의 호출에서만 생존 차단 조건을 통과시킵니다.
            possessionData.canBePossessed = true;
            possessionData.requiresDefeatedState = false;
            targetStatus.SetState(HWJ_RuntimeState.Dead);

            bool succeeded = playerPossessionSystem.TryPossess(targetResolver);

            if (succeeded)
            {
                if (physicsTransitionStarted)
                {
                    possessionPhysicsResolver.CompletePossessionTransition(collisionSettings, placementFeet);
                }
                else
                {
                    // 전용 Resolver가 없을 때만 이전 위치 보정을 안전장치로 사용합니다.
                    RestorePlayerBodyPhysics();
                    AlignPlayerToGroundSurface(placementSurfaceY);
                }
            }

            if (!succeeded && targetStatus != null)
            {
                if (physicsTransitionStarted)
                {
                    possessionPhysicsResolver.CancelPossessionTransition();
                }

                targetStatus.SetState(originalRuntimeState);
            }

            return StoreResult(
                succeeded,
                succeeded
                    ? "살아 있는 실험 몬스터 빙의에 성공했습니다."
                    : ResolvePossessionFailureMessage());
        }
        finally
        {
            // ScriptableObject 공유 설정은 다른 몬스터에 영향이 없도록 즉시 원상 복구합니다.
            possessionData.canBePossessed = originalCanBePossessed;
            possessionData.requiresDefeatedState = originalRequiresDefeatedState;
        }
    }

    private string ResolvePossessionFailureMessage()
    {
        // 새 HWJ 버전의 결과 속성이 있으면 사용하고, 구버전에서는 기본 문구로 안전하게 대체합니다.
        if (playerPossessionSystem == null) return "플레이어 빙의 시스템을 찾지 못했습니다.";

        System.Reflection.PropertyInfo resultProperty = playerPossessionSystem.GetType()
            .GetProperty("LastPossessionResult");
        string result = resultProperty?.GetValue(playerPossessionSystem) as string;
        return string.IsNullOrWhiteSpace(result) ? "살아있는 몬스터 빙의에 실패했습니다." : result;
    }

    private void CacheReferences()
    {
        if (targetResolver == null)
        {
            targetResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (targetStatus == null)
        {
            targetStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (collisionProfile == null)
        {
            collisionProfile = GetComponent<hys_BodyCollisionProfile>();
        }

        if (feetAnchor == null)
        {
            feetAnchor = GetComponent<hys_PossessionFeetAnchor>();
        }

        if (playerSoulSystem == null)
        {
            playerSoulSystem = FindFirstObjectByType<HWJ_SoulSystem>();
        }

        if (playerPossessionSystem == null && playerSoulSystem != null)
        {
            playerPossessionSystem = playerSoulSystem.GetComponent<HWJ_PossessionSystem>();
        }

        if (possessionPhysicsResolver == null && playerSoulSystem != null)
        {
            possessionPhysicsResolver = playerSoulSystem.GetComponent<hys_PossessionPhysicsResolver>();
        }

        if (playerBody == null && playerSoulSystem != null)
        {
            playerBody = playerSoulSystem.GetComponent<Rigidbody2D>();
        }

        if (playerBodyCollider == null && playerSoulSystem != null)
        {
            playerBodyCollider = playerSoulSystem.GetComponent<Collider2D>();
        }

        if (targetBodyCollider == null)
        {
            targetBodyCollider = GetComponent<Collider2D>();
        }
    }

    private void RestorePlayerBodyPhysics()
    {
        if (playerBody != null)
        {
            playerBody.simulated = true;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        if (restorePlayerCollisionImmediately && playerBodyCollider != null)
        {
            // 유령 상태의 Trigger가 남지 않도록 빙의 성공 프레임에 본체 충돌을 즉시 복구합니다.
            playerBodyCollider.enabled = true;
            playerBodyCollider.isTrigger = false;
        }
    }

    private void AlignPlayerToGroundSurface(float groundSurfaceY)
    {
        if (!alignColliderBottomAfterPossession || playerBodyCollider == null)
        {
            return;
        }

        // 플레이어 Collider의 발바닥을 실제 타일 바닥 표면 위로 옮깁니다.
        float playerBottomY = playerBodyCollider.bounds.min.y;
        float verticalOffset = groundSurfaceY - playerBottomY + groundClearance;

        if (playerBody != null)
        {
            playerBody.position += Vector2.up * verticalOffset;
            return;
        }

        transform.position += Vector3.up * verticalOffset;
    }

    private bool TryFindActualGroundSurface(out float groundSurfaceY)
    {
        groundSurfaceY = 0f;

        Vector2 origin = targetBodyCollider != null
            ? new Vector2(targetBodyCollider.bounds.center.x, targetBodyCollider.bounds.max.y + 0.5f)
            : (Vector2)transform.position + Vector2.up;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, groundSearchDistance, groundLayerMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.isTrigger)
            {
                continue;
            }

            Transform hitTransform = hitCollider.transform;
            bool belongsToTarget = hitTransform == transform || hitTransform.IsChildOf(transform);
            bool belongsToPlayer = playerSoulSystem != null
                && (hitTransform == playerSoulSystem.transform || hitTransform.IsChildOf(playerSoulSystem.transform));

            if (belongsToTarget || belongsToPlayer)
            {
                continue;
            }

            groundSurfaceY = hits[i].point.y;
            return true;
        }

        return false;
    }

    private bool IsPlayerInRange()
    {
        if (playerSoulSystem == null)
        {
            return false;
        }

        float distance = Vector2.Distance(playerSoulSystem.transform.position, transform.position);
        return distance <= interactionRange;
    }

    private bool IsPossessionInputPressed()
    {
        if (!requireEKey)
        {
            return true;
        }

        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    private bool TryGetPossessionData(out HWJ_PossessionData possessionData)
    {
        possessionData = null;

        if (targetResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            possessionData = enemyData.PossessionBody;
        }
        else if (targetResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionData = bossData.PossessionBody;
        }

        return possessionData != null;
    }

    private bool StoreResult(bool succeeded, string message)
    {
        lastResult = message;

        if (succeeded)
        {
            Debug.Log($"[hys 생존 빙의 실험] {message}", this);
        }
        else
        {
            Debug.LogWarning($"[hys 생존 빙의 실험] {message}", this);
        }

        return succeeded;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
