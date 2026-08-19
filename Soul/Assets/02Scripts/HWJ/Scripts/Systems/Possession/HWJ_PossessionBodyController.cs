using UnityEngine;

/// <summary>
/// 원본 몬스터의 활성 상태, AI, 충돌체, Rigidbody와 육체 복원/제거를 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessionBodyController : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

    [Space(8f)]
    [Header("육체 전환 설정")]
    [SerializeField] private bool moveOwnerToPossessedBody = true;
    [SerializeField] private bool consumePossessedBody = true;
    [SerializeField] private bool deactivateConsumedBody = true;

    [Space(8f)]
    [Header("빙의체 충돌체")]
    [Tooltip("빙의 중 플레이어 BoxCollider2D를 대상 육체의 크기와 중심으로 변경합니다.")]
    [SerializeField] private bool usePossessedBodyCollider = true;

    private BoxCollider2D ownerBoxCollider;
    private Vector2 ownerOriginalColliderSize;
    private Vector2 ownerOriginalColliderOffset;
    private float ownerOriginalColliderEdgeRadius;
    private PhysicsMaterial2D ownerOriginalColliderMaterial;
    private bool ownerOriginalColliderUsedByEffector;
    private bool ownerOriginalColliderEnabled;
    private bool hasOwnerColliderCache;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        CacheOwnerCollider();
    }

    public void CaptureBeforePossession(
        HWJ_RootObjectDataResolver targetDataResolver,
        bool restoreOriginalBodyOnExit)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        HWJ_PossessionBodyState bodyState =
            targetDataResolver.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState == null)
        {
            bodyState = targetDataResolver.gameObject.AddComponent<HWJ_PossessionBodyState>();
        }

        bool wasAliveWhenPossessed = IsEnemyAlive(targetDataResolver);
        bodyState.CaptureBeforePossession(wasAliveWhenPossessed, restoreOriginalBodyOnExit);
        bodyState.MarkConsumed();
    }

    public void TransferOwnerToPossessedBody(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionVisualController visualController)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        if (moveOwnerToPossessedBody)
        {
            Vector3 targetPosition = targetDataResolver.transform.position;
            targetPosition.z = transform.position.z;

            transform.SetPositionAndRotation(
                targetPosition,
                targetDataResolver.transform.rotation);

            if (TryGetComponent(out Rigidbody2D ownerBody))
            {
                ownerBody.linearVelocity = Vector2.zero;
                ownerBody.angularVelocity = 0f;
            }
        }

        ApplyPossessedBodyCollider(targetDataResolver.gameObject);

        visualController?.ApplyFromTarget(targetDataResolver.gameObject);

        if (consumePossessedBody)
        {
            DisableTargetObject(targetDataResolver.gameObject);
        }
    }

    public bool RestoreOriginalBodyAfterPossession(
        HWJ_RootObjectDataResolver previousBodyResolver,
        HWJ_PossessedBodyRuntimeState previousBodyState)
    {
        if (previousBodyResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState =
            previousBodyResolver.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState == null || !bodyState.ShouldRestoreObjectOnPossessionExit)
        {
            return false;
        }

        bodyState.RestoreCapturedObjectState(transform);
        RestoreReleasedBodyHp(previousBodyResolver, previousBodyState);
        return true;
    }

    public bool RemovePossessedBody(HWJ_RootObjectDataResolver previousBodyResolver)
    {
        if (previousBodyResolver == null)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState =
            previousBodyResolver.GetComponent<HWJ_PossessionBodyState>();

        bodyState?.MarkRemovedAfterPossession();

        if (HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Manager.Despawn(previousBodyResolver.gameObject);
        }
        else
        {
            previousBodyResolver.gameObject.SetActive(false);
        }

        return true;
    }

    public bool ApplyPossessedBodyCollider(GameObject possessedBody)
    {
        CacheOwnerCollider();

        if (!usePossessedBodyCollider || ownerBoxCollider == null || possessedBody == null)
        {
            return false;
        }

        BoxCollider2D possessedCollider = possessedBody.GetComponent<BoxCollider2D>();

        if (possessedCollider == null || possessedCollider.isTrigger)
        {
            BoxCollider2D[] targetColliders =
                possessedBody.GetComponentsInChildren<BoxCollider2D>(true);

            possessedCollider = null;

            for (int i = 0; i < targetColliders.Length; i++)
            {
                if (targetColliders[i] != null && !targetColliders[i].isTrigger)
                {
                    possessedCollider = targetColliders[i];
                    break;
                }
            }
        }

        if (possessedCollider == null)
        {
            return false;
        }

        Vector3 targetScale = possessedCollider.transform.lossyScale;
        Vector3 ownerScale = ownerBoxCollider.transform.lossyScale;
        float scaleX = Mathf.Abs(ownerScale.x) > Mathf.Epsilon
            ? Mathf.Abs(targetScale.x / ownerScale.x)
            : 1f;
        float scaleY = Mathf.Abs(ownerScale.y) > Mathf.Epsilon
            ? Mathf.Abs(targetScale.y / ownerScale.y)
            : 1f;

        ownerBoxCollider.size = new Vector2(
            possessedCollider.size.x * scaleX,
            possessedCollider.size.y * scaleY);
        ownerBoxCollider.offset = new Vector2(
            possessedCollider.offset.x * scaleX,
            possessedCollider.offset.y * scaleY);
        ownerBoxCollider.edgeRadius = possessedCollider.edgeRadius * Mathf.Min(scaleX, scaleY);
        ownerBoxCollider.sharedMaterial = possessedCollider.sharedMaterial;
        ownerBoxCollider.usedByEffector = possessedCollider.usedByEffector;
        ownerBoxCollider.isTrigger = false;
        ownerBoxCollider.enabled = true;
        return true;
    }

    public void RestoreOwnerCollider()
    {
        CacheOwnerCollider();

        if (ownerBoxCollider == null || !hasOwnerColliderCache)
        {
            return;
        }

        ownerBoxCollider.size = ownerOriginalColliderSize;
        ownerBoxCollider.offset = ownerOriginalColliderOffset;
        ownerBoxCollider.edgeRadius = ownerOriginalColliderEdgeRadius;
        ownerBoxCollider.sharedMaterial = ownerOriginalColliderMaterial;
        ownerBoxCollider.usedByEffector = ownerOriginalColliderUsedByEffector;
        ownerBoxCollider.isTrigger = false;
        ownerBoxCollider.enabled = ownerOriginalColliderEnabled;
    }

    private static void RestoreReleasedBodyHp(
        HWJ_RootObjectDataResolver previousBodyResolver,
        HWJ_PossessedBodyRuntimeState previousBodyState)
    {
        if (previousBodyResolver == null || previousBodyState == null)
        {
            return;
        }

        HWJ_RuntimeStatusSystem restoredStatus =
            previousBodyResolver.GetComponent<HWJ_RuntimeStatusSystem>();

        if (restoredStatus == null)
        {
            return;
        }

        float restoredHp = Mathf.Max(0f, previousBodyState.CurrentHp);
        restoredStatus.RestoreHpSnapshot(restoredHp, restoredHp, restoredHp);

        if (restoredHp > 0f)
        {
            restoredStatus.SetState(HWJ_RuntimeState.Idle);
        }
    }

    private static bool IsEnemyAlive(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null
            || targetDataResolver.ObjectType != HWJ_ObjectType.Enemy)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem status =
            targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();

        if (status == null)
        {
            return true;
        }

        return status.CurrentState != HWJ_RuntimeState.Dead
            && (!status.UsesHp || status.CurrentHp > 0f);
    }

    private void DisableTargetObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        HWJ_EnemyAttackSystem[] attackSystems =
            targetObject.GetComponentsInChildren<HWJ_EnemyAttackSystem>(true);

        for (int i = 0; i < attackSystems.Length; i++)
        {
            if (attackSystems[i] == null)
            {
                continue;
            }

            attackSystems[i].CancelPreparedBasicAttack();
            attackSystems[i].SetTarget(null);
            attackSystems[i].enabled = false;
        }

        HWJ_SkillActionSystem[] skillActionSystems =
            targetObject.GetComponentsInChildren<HWJ_SkillActionSystem>(true);

        for (int i = 0; i < skillActionSystems.Length; i++)
        {
            if (skillActionSystems[i] == null)
            {
                continue;
            }

            skillActionSystems[i].CancelCurrentAction();
            skillActionSystems[i].enabled = false;
        }

        HWJ_EnemyNavigationSystem navigation =
            targetObject.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.SetTarget(null);
            navigation.enabled = false;
        }

        HWJ_MonsterAISystem monsterAI =
            targetObject.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.SetTarget(null);
            monsterAI.enabled = false;
        }

        Collider2D[] colliders = targetObject.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        SpriteRenderer[] renderers = targetObject.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        Animator animator = targetObject.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        Rigidbody2D body = targetObject.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        if (deactivateConsumedBody)
        {
            targetObject.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    private void CacheOwnerCollider()
    {
        if (ownerBoxCollider == null)
        {
            ownerBoxCollider = GetComponent<BoxCollider2D>();
        }

        if (ownerBoxCollider == null || hasOwnerColliderCache)
        {
            return;
        }

        ownerOriginalColliderSize = ownerBoxCollider.size;
        ownerOriginalColliderOffset = ownerBoxCollider.offset;
        ownerOriginalColliderEdgeRadius = ownerBoxCollider.edgeRadius;
        ownerOriginalColliderMaterial = ownerBoxCollider.sharedMaterial;
        ownerOriginalColliderUsedByEffector = ownerBoxCollider.usedByEffector;
        ownerOriginalColliderEnabled = ownerBoxCollider.enabled;
        hasOwnerColliderCache = true;
    }
}
