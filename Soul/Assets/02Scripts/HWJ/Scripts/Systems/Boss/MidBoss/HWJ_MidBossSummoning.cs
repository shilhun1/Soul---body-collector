using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks summon charges and creates possessable monsters with runtime data and save identity.
/// This partial belongs to the single HWJ_MidBossPatternSystem component.
/// </summary>
public partial class HWJ_MidBossPatternSystem
{
    private void RefreshPattern1Charges()
    {
        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return;
        }

        if (!Mathf.Approximately(trackedMaxHp, runtimeStatus.MaxHp) || nextPattern1HpRatioThreshold <= 0f)
        {
            ResetPattern1Tracking();
        }

        float interval = Mathf.Clamp(pattern1HpLossIntervalRatio, 0.01f, 1f);
        float currentRatio = Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp);

        while (nextPattern1HpRatioThreshold > 0f && currentRatio <= nextPattern1HpRatioThreshold)
        {
            pendingPattern1Charges++;
            nextPattern1HpRatioThreshold -= interval;
        }
    }

    private void ResetPattern1Tracking()
    {
        trackedMaxHp = runtimeStatus != null ? runtimeStatus.MaxHp : 0f;
        float interval = Mathf.Clamp(pattern1HpLossIntervalRatio, 0.01f, 1f);
        nextPattern1HpRatioThreshold = 1f - interval;
        pendingPattern1Charges = 0;
    }

    private bool MatchesPatternExecutor(HWJ_BossPatternDataSO pattern)
    {
        if (pattern == null || !pattern.UseCustomPatternExecutor)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(pattern.CustomPatternExecutorKey)
            || string.IsNullOrWhiteSpace(executorKey)
            || pattern.CustomPatternExecutorKey == executorKey;
    }

    private bool HasPossessableCorpseInRoom()
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (IsPossessableCorpse(resolvers[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPossessableCorpse(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.gameObject == gameObject)
        {
            return false;
        }

        if (resolver.ObjectType != HWJ_ObjectType.Enemy && resolver.ObjectType != HWJ_ObjectType.Boss)
        {
            return false;
        }

        if (!IsInsideBossRoom(resolver.transform.position))
        {
            return false;
        }

        if (!TryGetPossessionBodyData(resolver, out HWJ_PossessionData possessionData)
            || !possessionData.canBePossessed)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState = resolver.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState != null && bodyState.IsConsumed)
        {
            return false;
        }

        if (!possessionData.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null && targetStatus.IsDead;
    }

    private bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver resolver,
        out HWJ_PossessionData possessionData)
    {
        possessionData = null;

        if (resolver == null)
        {
            return false;
        }

        if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            possessionData = enemyData.PossessionBody;
        }
        else if (resolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionData = bossData.PossessionBody;
        }

        return possessionData != null;
    }

    private void SpawnPossessableMonster(int spawnIndex)
    {
        HWJ_RootObjectDataSO rootData = GetPossessableMonsterRootObject(spawnIndex);
        GameObject prefab = GetPossessableMonsterPrefab(spawnIndex, rootData);

        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = ResolvePattern1SummonPosition(spawnIndex);
        GameObject instance = HWJ_GameAccess.HasManager
            ? HWJ_GameAccess.Spawn(prefab, spawnPosition, Quaternion.identity)
            : Instantiate(prefab, spawnPosition, Quaternion.identity);

        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        EnsureSummonedMonsterRuntimeComponents(instance, rootData, spawnIndex);

        HWJ_PossessionBodyState bodyState = instance.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState != null)
        {
            bodyState.ResetConsumed();
        }

        HWJ_RuntimeStatusSystem spawnedStatus = instance.GetComponent<HWJ_RuntimeStatusSystem>();

        if (spawnedStatus != null)
        {
            spawnedStatus.RefreshCurrentHpFromData(true);
        }

        AssignSummonedMonsterSaveIdentity(instance, resolver, rootData, spawnIndex);

        HWJ_MonsterAISystem monsterAI = instance.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.RefreshData();
            monsterAI.SetTarget(FindPlayerTarget());
        }

        HWJ_EnemyNavigationSystem navigation = instance.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.SetTarget(FindPlayerTarget());
        }

        HWJ_EnemyAttackSystem attackSystem = instance.GetComponent<HWJ_EnemyAttackSystem>();

        if (attackSystem != null)
        {
            attackSystem.SetTarget(FindPlayerTarget());
        }
    }

    private GameObject GetPossessableMonsterPrefab(int index, HWJ_RootObjectDataSO rootData)
    {
        if (possessableMonsterPrefabs != null && possessableMonsterPrefabs.Length > 0)
        {
            int safeIndex = Mathf.Abs(index) % possessableMonsterPrefabs.Length;
            GameObject indexedPrefab = possessableMonsterPrefabs[safeIndex];

            if (indexedPrefab != null)
            {
                return indexedPrefab;
            }
        }

        if (summonMonsterPrefab != null)
        {
            return summonMonsterPrefab;
        }

        return rootData != null && rootData.Model != null
            ? rootData.Model.modelPrefab
            : null;
    }

    private Vector3 ResolvePattern1SummonPosition(int spawnIndex)
    {
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += pattern1SummonStepOffset.x * Mathf.Max(0, spawnIndex);
        spawnPosition.y += pattern1SummonStepOffset.y * Mathf.Max(0, spawnIndex);
        return spawnPosition;
    }

    private HWJ_RootObjectDataSO GetPossessableMonsterRootObject(int index)
    {
        if (possessableMonsterRootObjects == null || possessableMonsterRootObjects.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Abs(index) % possessableMonsterRootObjects.Length;
        return possessableMonsterRootObjects[safeIndex];
    }

    private void EnsureSummonedMonsterRuntimeComponents(
        GameObject instance,
        HWJ_RootObjectDataSO rootData,
        int spawnIndex)
    {
        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = EnsureComponent<HWJ_RootObjectDataResolver>(instance);

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        Rigidbody2D summonedBody = EnsureComponent<Rigidbody2D>(instance);

        if (summonedBody != null)
        {
            summonedBody.freezeRotation = true;
            summonedBody.gravityScale = Mathf.Approximately(summonedBody.gravityScale, 0f)
                ? 1f
                : summonedBody.gravityScale;
        }

        if (instance.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D boxCollider = instance.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.8f, 1f);
        }

        EnsureComponent<HWJ_PossessionBodyState>(instance);
        EnsureComponent<HWJ_KnockbackSystem>(instance);
        EnsureComponent<HWJ_RuntimeStatusSystem>(instance);
        EnsureComponent<HWJ_CombatSystem>(instance);
        EnsureComponent<HWJ_CombatExecutionSystem>(instance);
        EnsureComponent<HWJ_SkillActionSystem>(instance);
        EnsureComponent<HWJ_CharacterMotionSystem>(instance);
        EnsureComponent<HWJ_EnemyAttackSystem>(instance);
        EnsureComponent<HWJ_EnemyNavigationSystem>(instance);
        EnsureComponent<HWJ_MonsterAISystem>(instance);
    }

    private void AssignSummonedMonsterSaveIdentity(
        GameObject instance,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RootObjectDataSO rootData,
        int spawnIndex)
    {
        if (instance == null)
        {
            return;
        }

        string rootObjectId = ResolveRootObjectId(rootData, resolver);

        if (string.IsNullOrEmpty(rootObjectId))
        {
            return;
        }

        HWJ_RuntimeSaveIdentity saveIdentity = EnsureComponent<HWJ_RuntimeSaveIdentity>(instance);

        if (saveIdentity == null || saveIdentity.HasStableInstanceId)
        {
            return;
        }

        string bossId = ResolveRootObjectId(
            null,
            bossBrain != null ? bossBrain.GetComponent<HWJ_RootObjectDataResolver>() : GetComponent<HWJ_RootObjectDataResolver>());
        string sourceId = string.IsNullOrEmpty(bossId) ? pattern1SummonSaveIdPrefix : bossId + "_" + pattern1SummonSaveIdPrefix;
        string stableId = $"{sourceId}_{summonedMonsterSequence:000}_{Mathf.Max(0, spawnIndex):00}";
        summonedMonsterSequence++;
        saveIdentity.SetManualIdentity(stableId, rootObjectId);
    }

    private static string ResolveRootObjectId(
        HWJ_RootObjectDataSO rootData,
        HWJ_RootObjectDataResolver resolver)
    {
        if (rootData != null && rootData.Identity != null && !string.IsNullOrEmpty(rootData.Identity.objectId))
        {
            return rootData.Identity.objectId;
        }

        return resolver != null && resolver.Identity != null
            ? resolver.Identity.objectId
            : null;
    }

    private static T EnsureComponent<T>(GameObject instance) where T : Component
    {
        if (instance == null)
        {
            return null;
        }

        T component = instance.GetComponent<T>();
        return component != null ? component : instance.AddComponent<T>();
    }

}
