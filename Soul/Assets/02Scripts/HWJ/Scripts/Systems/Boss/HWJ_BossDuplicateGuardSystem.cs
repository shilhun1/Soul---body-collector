using UnityEngine;

/// <summary>
/// 같은 보스 데이터가 같은 위치에 중복 배치되었을 때 하나만 실행되도록 보호합니다.
/// 씬 제작 중 프리팹을 복제한 뒤 기존 인스턴스를 지우지 않은 경우, 보스와 체력 바가
/// 동시에 두 개 표시되는 문제를 전투 로직이 시작되기 전에 차단합니다.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(HWJ_RootObjectDataResolver))]
public sealed class HWJ_BossDuplicateGuardSystem : MonoBehaviour
{
    [Tooltip("이 거리 안에 같은 RootObjectData를 사용하는 보스가 있으면 중복으로 판단합니다.")]
    [SerializeField, Min(0.01f)] private float duplicateRadius = 0.5f;
    [Tooltip("중복 중 하나가 맵/스테이지 오브젝트의 자식이면 해당 인스턴스를 우선 사용합니다.")]
    [SerializeField] private bool preferParentedInstance = true;

    private HWJ_RootObjectDataResolver dataResolver;

    public float DuplicateRadius => duplicateRadius;
    public bool WasSuppressedAsDuplicate { get; private set; }

    private void Awake()
    {
        dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        SuppressThisInstanceWhenDuplicate();
    }

    /// <summary>
    /// 같은 씬, 같은 데이터, 같은 위치의 보스 중 어느 인스턴스를 사용할지 결정합니다.
    /// 프리팹 자체에는 씬 정보를 저장하지 않으므로 실행 시점에만 검사합니다.
    /// </summary>
    private void SuppressThisInstanceWhenDuplicate()
    {
        if (dataResolver == null || dataResolver.RootObjectData == null)
        {
            return;
        }

        HWJ_BossDuplicateGuardSystem[] guards =
            FindObjectsByType<HWJ_BossDuplicateGuardSystem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        HWJ_BossDuplicateGuardSystem winner = this;

        for (int i = 0; i < guards.Length; i++)
        {
            HWJ_BossDuplicateGuardSystem candidate = guards[i];
            if (!IsOverlappingDuplicate(candidate))
            {
                continue;
            }

            if (IsPreferred(candidate, winner))
            {
                winner = candidate;
            }
        }

        if (winner != this)
        {
            SuppressAsDuplicate(winner);
            return;
        }

        // 나중에 생성된 부모 소속 인스턴스가 우선될 수도 있으므로 승자가 기존 중복도 정리합니다.
        for (int i = 0; i < guards.Length; i++)
        {
            HWJ_BossDuplicateGuardSystem candidate = guards[i];
            if (IsOverlappingDuplicate(candidate))
            {
                candidate.SuppressAsDuplicate(this);
            }
        }
    }

    private void SuppressAsDuplicate(HWJ_BossDuplicateGuardSystem winner)
    {
        if (WasSuppressedAsDuplicate || !gameObject.activeSelf)
        {
            return;
        }

        WasSuppressedAsDuplicate = true;
        Debug.LogWarning(
            $"[HWJ Boss] 중복 배치된 '{name}'을(를) 비활성화했습니다. " +
            $"사용 인스턴스: '{winner.name}'",
            this);
        gameObject.SetActive(false);
    }

    private bool IsOverlappingDuplicate(HWJ_BossDuplicateGuardSystem candidate)
    {
        if (candidate == null || candidate == this || !candidate.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (candidate.gameObject.scene != gameObject.scene)
        {
            return false;
        }

        HWJ_RootObjectDataResolver candidateResolver =
            candidate.GetComponent<HWJ_RootObjectDataResolver>();
        if (candidateResolver == null
            || candidateResolver.RootObjectData != dataResolver.RootObjectData)
        {
            return false;
        }

        float allowedRadius = Mathf.Max(0.01f, Mathf.Max(duplicateRadius, candidate.duplicateRadius));
        return (candidate.transform.position - transform.position).sqrMagnitude
            <= allowedRadius * allowedRadius;
    }

    private bool IsPreferred(
        HWJ_BossDuplicateGuardSystem candidate,
        HWJ_BossDuplicateGuardSystem currentWinner)
    {
        if (preferParentedInstance || candidate.preferParentedInstance)
        {
            bool candidateIsParented = candidate.transform.parent != null;
            bool winnerIsParented = currentWinner.transform.parent != null;
            if (candidateIsParented != winnerIsParented)
            {
                return candidateIsParented;
            }
        }

        // 우선순위가 같으면 InstanceID로 한 개만 남도록 항상 같은 비교 규칙을 사용합니다.
        return candidate.GetInstanceID() < currentWinner.GetInstanceID();
    }
}
