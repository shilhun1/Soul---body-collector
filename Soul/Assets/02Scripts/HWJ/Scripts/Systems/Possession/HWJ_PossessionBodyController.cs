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

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
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

    /// <summary>
    /// 살아 있는 빙의체의 HP가 0이 되었을 때 원래 몬스터를 시체로 바꿉니다.
    /// </summary>
    public bool RestoreLiveBodyAsCorpse(
        HWJ_RootObjectDataResolver previousBodyResolver,
        HWJ_LivePossessionMentalState mentalState)
    {
        if (previousBodyResolver == null)
        {
            return false;
        }

        HWJ_RuntimeStatusSystem enemyStatus =
            previousBodyResolver.GetComponent<HWJ_RuntimeStatusSystem>();

        if (enemyStatus == null)
        {
            return false;
        }

        enemyStatus.RestoreHpSnapshot(0f, 0f, 0f);
        enemyStatus.SetState(HWJ_RuntimeState.Dead);
        mentalState?.MarkBecameCorpseAfterLivePossession();
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

        HWJ_EnemyNavigationSystem navigation =
            targetObject.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.enabled = false;
        }

        HWJ_MonsterAISystem monsterAI =
            targetObject.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
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
}
