using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 중간보스2의 소환 위치, 예고 마법진, 등장 애니메이션과 충돌 정리를 전담합니다.
[DisallowMultipleComponent]
public class hys_SecondBossSummonSpawner : MonoBehaviour
{
    [Header("소환 대상")]
    [SerializeField] private GameObject[] monsterPrefabs = new GameObject[5];

    [Header("기본 진형")]
    [SerializeField, Min(0.1f)] private float formationSpacing = 2.2f;
    [SerializeField] private float verticalOffset = 0.2f;

    [Header("소환 연출")]
    [SerializeField, Min(0.05f)] private float telegraphSeconds = 0.55f;
    [SerializeField, Min(0f)] private float spawnIntervalSeconds = 0.12f;
    [SerializeField, Min(0.05f)] private float arrivalSeconds = 0.28f;
    [SerializeField, Min(0.1f)] private float portalRadius = 0.85f;
    [SerializeField] private Color portalColor = new Color(0.58f, 0.13f, 0.95f, 0.92f);

    [Header("실행 확인")]
    [SerializeField] private int spawnSequenceCount;
    [SerializeField] private int totalSpawnedCount;
    [SerializeField] private int lastRequestedCount;
    [SerializeField] private int lastSpawnedCount;
    [SerializeField] private int activeSequenceCount;
    [SerializeField] private int lastActivatedAiCount;

    public float VerticalOffset => verticalOffset;
    public int SpawnSequenceCount => spawnSequenceCount;
    public int TotalSpawnedCount => totalSpawnedCount;
    public int LastRequestedCount => lastRequestedCount;
    public int LastSpawnedCount => lastSpawnedCount;
    public bool IsSpawning => activeSequenceCount > 0;
    public int LastActivatedAiCount => lastActivatedAiCount;
    public bool HasConfiguredPrefabs
    {
        get
        {
            if (monsterPrefabs == null) return false;
            for (int i = 0; i < monsterPrefabs.Length; i++)
                if (monsterPrefabs[i] != null) return true;
            return false;
        }
    }

    public void Configure(GameObject[] prefabs)
    {
        monsterPrefabs = prefabs != null ? (GameObject[])prefabs.Clone() : Array.Empty<GameObject>();
    }

    public IEnumerator SpawnFormationRoutine(
        int requestedCount,
        Action<List<GameObject>> completed = null)
    {
        int safeCount = Mathf.Max(1, requestedCount);
        List<Vector3> positions = new List<Vector3>(safeCount);
        for (int i = 0; i < safeCount; i++)
        {
            float centeredIndex = i - (safeCount - 1) * 0.5f;
            positions.Add(transform.position
                + Vector3.right * centeredIndex * formationSpacing
                + Vector3.up * verticalOffset);
        }

        yield return SpawnAtPositionsRoutine(positions, completed);
    }

    public IEnumerator SpawnAtPositionsRoutine(
        IReadOnlyList<Vector3> positions,
        Action<List<GameObject>> completed = null)
    {
        List<GameObject> spawnedMonsters = new List<GameObject>();
        if (positions == null || positions.Count == 0)
        {
            completed?.Invoke(spawnedMonsters);
            yield break;
        }

        List<GameObject> candidates = BuildUniqueCandidates();
        int count = Mathf.Min(positions.Count, candidates.Count);
        lastRequestedCount = positions.Count;
        lastSpawnedCount = 0;
        lastActivatedAiCount = 0;
        spawnSequenceCount++;
        activeSequenceCount++;

        // 실제 생성 전에 모든 위치를 동시에 보여줘 플레이어가 소환 진형을 읽을 수 있게 합니다.
        for (int i = 0; i < count; i++)
        {
            hys_SkillWarningIndicator.ShowCircle(
                positions[i],
                portalRadius,
                telegraphSeconds,
                portalColor,
                0.09f);
            GameObject portalMark = hys_SecondBossMagicVisual.SpawnTrackingMark(
                null,
                telegraphSeconds,
                portalRadius * 0.72f,
                portalColor);
            if (portalMark != null) portalMark.transform.position = positions[i];
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, telegraphSeconds));

        for (int i = 0; i < count; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
            GameObject prefab = candidates[selectedIndex];
            candidates.RemoveAt(selectedIndex);

            GameObject spawned = HWJ_GameAccess.Spawn(prefab, positions[i], Quaternion.identity);
            if (spawned == null) spawned = Instantiate(prefab, positions[i], Quaternion.identity);
            if (spawned != null)
            {
                IgnoreOwnerCollision(spawned);
                ActivateSummonedMonster(spawned);
                spawnedMonsters.Add(spawned);
                lastSpawnedCount++;
                totalSpawnedCount++;
                StartCoroutine(PlayArrivalRoutine(spawned));
            }

            hys_SecondBossMagicVisual.SpawnShockwave(
                positions[i],
                0.38f,
                portalRadius * 2.1f,
                portalRadius * 1.4f,
                portalColor);
            if (spawnIntervalSeconds > 0f && i < count - 1)
                yield return new WaitForSeconds(spawnIntervalSeconds);
        }

        activeSequenceCount = Mathf.Max(0, activeSequenceCount - 1);
        completed?.Invoke(spawnedMonsters);
    }

    // 보스가 부른 적은 일반 시야 탐색을 기다리지 않고 현재 플레이어를 즉시 추적하게 합니다.
    private void ActivateSummonedMonster(GameObject spawnedMonster)
    {
        if (spawnedMonster == null) return;

        Transform playerTarget = FindPlayerTarget();
        HWJ_EnemyPerceptionSystem perception =
            spawnedMonster.GetComponentInChildren<HWJ_EnemyPerceptionSystem>(true);
        HWJ_MonsterAISystem monsterAi =
            spawnedMonster.GetComponentInChildren<HWJ_MonsterAISystem>(true);

        if (perception != null)
        {
            perception.enabled = true;
            perception.CaptureSpawnOrigin();
            if (playerTarget != null) perception.SetCandidateTarget(playerTarget, true);
        }

        if (monsterAi != null)
        {
            monsterAi.enabled = true;
            monsterAi.RefreshData();
            if (playerTarget != null) monsterAi.SetTarget(playerTarget);
            lastActivatedAiCount++;
        }
    }

    private static Transform FindPlayerTarget()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
            return HWJ_GameAccess.Manager.PlayerResolver.transform;

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];
            if (resolver != null && resolver.ObjectType == HWJ_ObjectType.Player)
                return resolver.transform;
        }

        return null;
    }

    private List<GameObject> BuildUniqueCandidates()
    {
        List<GameObject> candidates = new List<GameObject>(5);
        if (monsterPrefabs == null) return candidates;
        for (int i = 0; i < monsterPrefabs.Length; i++)
        {
            GameObject prefab = monsterPrefabs[i];
            if (prefab != null && !candidates.Contains(prefab)) candidates.Add(prefab);
        }
        return candidates;
    }

    private IEnumerator PlayArrivalRoutine(GameObject spawned)
    {
        if (spawned == null) yield break;
        Transform spawnedTransform = spawned.transform;
        Vector3 targetScale = spawnedTransform.localScale;
        if (targetScale.sqrMagnitude <= 0.0001f) targetScale = Vector3.one;
        spawnedTransform.localScale = targetScale * 0.12f;

        float startTime = Time.time;
        float endTime = startTime + Mathf.Max(0.05f, arrivalSeconds);
        while (spawnedTransform != null && Time.time < endTime)
        {
            float progress = Mathf.InverseLerp(startTime, endTime, Time.time);
            float overshoot = Mathf.Sin(progress * Mathf.PI) * 0.18f;
            spawnedTransform.localScale = Vector3.LerpUnclamped(
                targetScale * 0.12f,
                targetScale,
                progress + overshoot);
            yield return null;
        }

        if (spawnedTransform != null) spawnedTransform.localScale = targetScale;
    }

    private void IgnoreOwnerCollision(GameObject spawnedMonster)
    {
        Collider2D[] ownerColliders = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] monsterColliders = spawnedMonster.GetComponentsInChildren<Collider2D>(true);
        for (int ownerIndex = 0; ownerIndex < ownerColliders.Length; ownerIndex++)
        {
            Collider2D ownerCollider = ownerColliders[ownerIndex];
            if (ownerCollider == null) continue;
            for (int monsterIndex = 0; monsterIndex < monsterColliders.Length; monsterIndex++)
            {
                Collider2D monsterCollider = monsterColliders[monsterIndex];
                if (monsterCollider != null)
                    Physics2D.IgnoreCollision(ownerCollider, monsterCollider, true);
            }
        }
    }
}
