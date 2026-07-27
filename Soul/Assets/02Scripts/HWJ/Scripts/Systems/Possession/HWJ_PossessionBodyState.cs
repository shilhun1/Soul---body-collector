using UnityEngine;

public class HWJ_PossessionBodyState : MonoBehaviour
{
    [Header("빙의 상태")]
    [Tooltip("한 번 빙의에 사용된 몸인지 표시합니다. 켜져 있으면 같은 몸에 다시 빙의할 수 없습니다.")]
    [SerializeField] private bool isConsumed;
    [Tooltip("살아있는 상태에서 빙의되었다가 해제된 몸인지 표시합니다.")]
    [SerializeField] private bool isReleasedAfterPossession;
    [Tooltip("HP가 0이 되어 제거된 빙의체인지 표시합니다. 켜져 있으면 다시 빙의할 수 없습니다.")]
    [SerializeField] private bool isRemovedAfterPossession;
    [Tooltip("빙의가 시작될 때 대상이 살아 있었는지 표시합니다.")]
    [SerializeField] private bool wasAliveWhenPossessed;
    [Tooltip("빙의 해제 시 원래 몬스터 오브젝트를 다시 활성화해야 하는지 표시합니다.")]
    [SerializeField] private bool restoreObjectOnPossessionExit;

    private bool capturedActiveSelf;
    private bool hasCapturedObjectState;
    private HWJ_EnemyNavigationSystem capturedNavigation;
    private HWJ_MonsterAISystem capturedMonsterAI;
    private HWJ_EnemyAttackSystem capturedEnemyAttack;
    private Collider2D[] capturedColliders;
    private bool[] capturedColliderEnabledStates;
    private SpriteRenderer[] capturedRenderers;
    private bool[] capturedRendererEnabledStates;
    private Animator[] capturedAnimators;
    private bool[] capturedAnimatorEnabledStates;
    private Rigidbody2D capturedBody;
    private bool capturedBodySimulated;

    public bool IsConsumed => isConsumed;
    public bool IsReleasedAfterPossession => isReleasedAfterPossession;
    public bool IsRemovedAfterPossession => isRemovedAfterPossession;
    public bool WasAliveWhenPossessed => wasAliveWhenPossessed;
    public bool ShouldRestoreObjectOnPossessionExit => restoreObjectOnPossessionExit
        && wasAliveWhenPossessed
        && hasCapturedObjectState;

    public void CaptureBeforePossession(bool wasAlive, bool restoreOnExit)
    {
        wasAliveWhenPossessed = wasAlive;
        restoreObjectOnPossessionExit = restoreOnExit;
        capturedActiveSelf = gameObject.activeSelf;
        capturedNavigation = GetComponent<HWJ_EnemyNavigationSystem>();
        capturedMonsterAI = GetComponent<HWJ_MonsterAISystem>();
        capturedEnemyAttack = GetComponent<HWJ_EnemyAttackSystem>();
        capturedColliders = GetComponentsInChildren<Collider2D>(true);
        capturedColliderEnabledStates = CaptureEnabledStates(capturedColliders);
        capturedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        capturedRendererEnabledStates = CaptureEnabledStates(capturedRenderers);
        capturedAnimators = GetComponentsInChildren<Animator>(true);
        capturedAnimatorEnabledStates = CaptureEnabledStates(capturedAnimators);
        capturedBody = GetComponent<Rigidbody2D>();
        capturedBodySimulated = capturedBody != null && capturedBody.simulated;
        hasCapturedObjectState = true;
    }

    public void MarkConsumed()
    {
        isConsumed = true;
    }

    public void MarkReleasedAfterPossession()
    {
        isReleasedAfterPossession = true;
    }

    public void MarkRemovedAfterPossession()
    {
        isRemovedAfterPossession = true;
        restoreObjectOnPossessionExit = false;
    }

    public void ResetConsumed()
    {
        isConsumed = false;
        isReleasedAfterPossession = false;
        isRemovedAfterPossession = false;
        wasAliveWhenPossessed = false;
        restoreObjectOnPossessionExit = false;
        hasCapturedObjectState = false;
    }

    public void RestoreCapturedObjectState(Transform targetTransform)
    {
        if (!ShouldRestoreObjectOnPossessionExit)
        {
            return;
        }

        if (targetTransform != null)
        {
            Vector3 restorePosition = targetTransform.position;
            restorePosition.z = transform.position.z;
            transform.SetPositionAndRotation(restorePosition, targetTransform.rotation);
        }

        gameObject.SetActive(capturedActiveSelf || wasAliveWhenPossessed);
        RestoreEnabledStates(capturedColliders, capturedColliderEnabledStates);
        RestoreEnabledStates(capturedRenderers, capturedRendererEnabledStates);
        RestoreEnabledStates(capturedAnimators, capturedAnimatorEnabledStates);
        ReactivateReturnedMonsterSystems(targetTransform);

        if (capturedBody != null)
        {
            capturedBody.simulated = capturedBodySimulated;
            capturedBody.linearVelocity = Vector2.zero;
            capturedBody.angularVelocity = 0f;
        }

        if (capturedNavigation != null)
        {
            capturedNavigation.enabled = true;
            capturedNavigation.SetTarget(targetTransform);
        }

        if (capturedMonsterAI != null)
        {
            capturedMonsterAI.enabled = true;
            capturedMonsterAI.SetTarget(targetTransform);
            capturedMonsterAI.TrySetAIState(HWJ_MonsterAIState.Idle, 0f);
        }

        if (capturedEnemyAttack != null)
        {
            capturedEnemyAttack.enabled = true;
            capturedEnemyAttack.SetTarget(targetTransform);
        }

        MarkReleasedAfterPossession();
        restoreObjectOnPossessionExit = false;
    }

    private void ReactivateReturnedMonsterSystems(Transform targetTransform)
    {
        // 살아있는 적이 정신력 0으로 복귀할 때 다시 추적/공격 루프에 들어가도록 보장합니다.
        HWJ_EnemyNavigationSystem[] navigationSystems = GetComponentsInChildren<HWJ_EnemyNavigationSystem>(true);

        for (int i = 0; i < navigationSystems.Length; i++)
        {
            if (navigationSystems[i] == null)
            {
                continue;
            }

            navigationSystems[i].enabled = true;
            navigationSystems[i].SetTarget(targetTransform);
        }

        HWJ_MonsterAISystem[] monsterAISystems = GetComponentsInChildren<HWJ_MonsterAISystem>(true);

        for (int i = 0; i < monsterAISystems.Length; i++)
        {
            if (monsterAISystems[i] == null)
            {
                continue;
            }

            monsterAISystems[i].enabled = true;
            monsterAISystems[i].SetTarget(targetTransform);
            monsterAISystems[i].TrySetAIState(HWJ_MonsterAIState.Idle, 0f);
        }

        HWJ_EnemyAttackSystem[] attackSystems = GetComponentsInChildren<HWJ_EnemyAttackSystem>(true);

        for (int i = 0; i < attackSystems.Length; i++)
        {
            if (attackSystems[i] == null)
            {
                continue;
            }

            attackSystems[i].enabled = true;
            attackSystems[i].SetTarget(targetTransform);
        }

        HWJ_SkillActionSystem[] skillActionSystems = GetComponentsInChildren<HWJ_SkillActionSystem>(true);

        for (int i = 0; i < skillActionSystems.Length; i++)
        {
            if (skillActionSystems[i] != null)
            {
                skillActionSystems[i].enabled = true;
            }
        }
    }

    private static bool[] CaptureEnabledStates(Behaviour[] behaviours)
    {
        if (behaviours == null)
        {
            return null;
        }

        bool[] states = new bool[behaviours.Length];

        for (int i = 0; i < behaviours.Length; i++)
        {
            states[i] = behaviours[i] != null && behaviours[i].enabled;
        }

        return states;
    }

    private static bool[] CaptureEnabledStates(Collider2D[] colliders)
    {
        if (colliders == null)
        {
            return null;
        }

        bool[] states = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
        {
            states[i] = colliders[i] != null && colliders[i].enabled;
        }

        return states;
    }

    private static bool[] CaptureEnabledStates(Renderer[] renderers)
    {
        if (renderers == null)
        {
            return null;
        }

        bool[] states = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            states[i] = renderers[i] != null && renderers[i].enabled;
        }

        return states;
    }

    private static void RestoreEnabledStates(Behaviour[] behaviours, bool[] states)
    {
        if (behaviours == null || states == null)
        {
            return;
        }

        int count = Mathf.Min(behaviours.Length, states.Length);

        for (int i = 0; i < count; i++)
        {
            if (behaviours[i] != null)
            {
                behaviours[i].enabled = states[i];
            }
        }
    }

    private static void RestoreEnabledStates(Collider2D[] colliders, bool[] states)
    {
        if (colliders == null || states == null)
        {
            return;
        }

        int count = Mathf.Min(colliders.Length, states.Length);

        for (int i = 0; i < count; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = states[i];
            }
        }
    }

    private static void RestoreEnabledStates(Renderer[] renderers, bool[] states)
    {
        if (renderers == null || states == null)
        {
            return;
        }

        int count = Mathf.Min(renderers.Length, states.Length);

        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = states[i];
            }
        }
    }
}
