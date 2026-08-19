using System.Collections;
using UnityEngine;

/// <summary>
/// 최종보스 붉은 포탈과 빙의 가능 몬스터 생성을 담당합니다.
/// 2페이즈 소환은 포탈 2개에서 각각 4마리씩 총 8마리를 생성합니다.
/// </summary>
public sealed partial class HWJ_FinalBossPatternSystem
{
    private IEnumerator RunPortalSummonSequence(
        HWJ_FinalBossPatternDefinitionSO definition,
        Transform target)
    {
        int portalCount = definition.PortalCount;
        GameObject[] portals = new GameObject[portalCount];

        for (int portalIndex = 0; portalIndex < portalCount; portalIndex++)
        {
            Vector3 portalPosition = ResolvePortalPosition(portalIndex, portalCount);
            portals[portalIndex] = HWJ_FinalBossRuntimeVisualFactory.Create(
                definition.PortalVisualPrefab,
                $"HWJ_FinalBoss_RedPortal_{portalIndex + 1}",
                portalPosition,
                runtimeEffectRoot,
                new Vector2(1.8f, 3f),
                new Color(0.75f, 0.02f, 0.08f, 0.8f),
                20);
            transientVisuals.Add(portals[portalIndex]);
        }

        for (int monsterIndex = 0; monsterIndex < definition.MonstersPerPortal; monsterIndex++)
        {
            for (int portalIndex = 0; portalIndex < portalCount; portalIndex++)
            {
                Vector3 portalPosition = ResolvePortalPosition(portalIndex, portalCount);
                Vector2 stepOffset = definition.SummonStepOffset
                    * (monsterIndex - (definition.MonstersPerPortal - 1) * 0.5f);
                SpawnPossessableMonster(
                    definition,
                    portalPosition + (Vector3)stepOffset,
                    portalIndex,
                    monsterIndex,
                    target);
            }

            if (monsterIndex < definition.MonstersPerPortal - 1)
            {
                yield return new WaitForSeconds(definition.SummonIntervalSeconds);
            }
        }

        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < portals.Length; i++)
        {
            if (portals[i] != null)
            {
                transientVisuals.Remove(portals[i]);
                Destroy(portals[i]);
            }
        }
    }

    private void SpawnPossessableMonster(
        HWJ_FinalBossPatternDefinitionSO definition,
        Vector3 spawnPosition,
        int portalIndex,
        int monsterIndex,
        Transform target)
    {
        int flattenedIndex = portalIndex * definition.MonstersPerPortal + monsterIndex;
        HWJ_RootObjectDataSO rootData = GetSummonRootObject(definition, flattenedIndex);
        GameObject prefab = GetSummonPrefab(definition, flattenedIndex, rootData);

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[HWJ][FinalBoss] 소환 프리팹 누락: 패턴={definition.PatternId}, 순번={flattenedIndex}",
                this);
            return;
        }

        GameObject instance = HWJ_GameAccess.HasManager
            ? HWJ_GameAccess.Spawn(prefab, spawnPosition, Quaternion.identity)
            : Instantiate(prefab, spawnPosition, Quaternion.identity);

        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver =
            EnsureSummonComponent<HWJ_RootObjectDataResolver>(instance);

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        EnsureSummonedMonsterRuntime(instance);
        AssignSummonSaveIdentity(instance, resolver, rootData, portalIndex, monsterIndex);

        HWJ_PossessionBodyState possessionBodyState =
            instance.GetComponent<HWJ_PossessionBodyState>();
        possessionBodyState?.ResetConsumed();

        HWJ_RuntimeStatusSystem spawnedStatus =
            instance.GetComponent<HWJ_RuntimeStatusSystem>();
        spawnedStatus?.RefreshCurrentHpFromData(true);

        HWJ_MonsterAISystem monsterAI = instance.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.RefreshData();
            monsterAI.SetTarget(target);
        }

        instance.GetComponent<HWJ_EnemyNavigationSystem>()?.SetTarget(target);
        instance.GetComponent<HWJ_EnemyAttackSystem>()?.SetTarget(target);
    }

    private void EnsureSummonedMonsterRuntime(GameObject instance)
    {
        EnsureSummonComponent<HWJ_RuntimeObjectContext>(instance);
        EnsureSummonComponent<HWJ_RuntimeStatusSystem>(instance);
        EnsureSummonComponent<HWJ_CombatSystem>(instance);
        EnsureSummonComponent<HWJ_PossessionBodyState>(instance);
        EnsureSummonComponent<HWJ_LivePossessionMentalState>(instance);
        EnsureSummonComponent<HWJ_CharacterMotionSystem>(instance);
        EnsureSummonComponent<HWJ_EnemyAttackSystem>(instance);
        EnsureSummonComponent<HWJ_EnemyNavigationSystem>(instance);
        EnsureSummonComponent<HWJ_MonsterAISystem>(instance);
    }

    private GameObject GetSummonPrefab(
        HWJ_FinalBossPatternDefinitionSO definition,
        int index,
        HWJ_RootObjectDataSO rootData)
    {
        GameObject[] prefabs = definition.SummonMonsterPrefabs;

        if (prefabs != null && prefabs.Length > 0)
        {
            GameObject prefab = prefabs[Mathf.Abs(index) % prefabs.Length];

            if (prefab != null)
            {
                return prefab;
            }
        }

        return rootData != null && rootData.Model != null
            ? rootData.Model.modelPrefab
            : null;
    }

    private static HWJ_RootObjectDataSO GetSummonRootObject(
        HWJ_FinalBossPatternDefinitionSO definition,
        int index)
    {
        HWJ_RootObjectDataSO[] rootObjects = definition.SummonMonsterRootObjects;
        return rootObjects != null && rootObjects.Length > 0
            ? rootObjects[Mathf.Abs(index) % rootObjects.Length]
            : null;
    }

    private Vector3 ResolvePortalPosition(int portalIndex, int portalCount)
    {
        if (portalAnchors != null
            && portalIndex >= 0
            && portalIndex < portalAnchors.Length
            && portalAnchors[portalIndex] != null)
        {
            return portalAnchors[portalIndex].position;
        }

        float centerOffset = portalIndex - (portalCount - 1) * 0.5f;
        return stationaryAnchorPosition + Vector3.right * centerOffset * 4f;
    }

    private void AssignSummonSaveIdentity(
        GameObject instance,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RootObjectDataSO rootData,
        int portalIndex,
        int monsterIndex)
    {
        if (instance == null)
        {
            return;
        }

        string rootId = rootData != null && rootData.Identity != null
            ? rootData.Identity.objectId
            : resolver != null && resolver.Identity != null
                ? resolver.Identity.objectId
                : string.Empty;

        if (string.IsNullOrWhiteSpace(rootId))
        {
            return;
        }

        HWJ_RuntimeSaveIdentity saveIdentity =
            EnsureSummonComponent<HWJ_RuntimeSaveIdentity>(instance);

        if (saveIdentity == null || saveIdentity.HasStableInstanceId)
        {
            return;
        }

        string stableId =
            $"final_boss_summon_{summonedMonsterSequence:0000}_{portalIndex:00}_{monsterIndex:00}";
        summonedMonsterSequence++;
        saveIdentity.SetManualIdentity(stableId, rootId);
    }

    private static T EnsureSummonComponent<T>(GameObject instance) where T : Component
    {
        if (instance == null)
        {
            return null;
        }

        T component = instance.GetComponent<T>();
        return component != null ? component : instance.AddComponent<T>();
    }
}
