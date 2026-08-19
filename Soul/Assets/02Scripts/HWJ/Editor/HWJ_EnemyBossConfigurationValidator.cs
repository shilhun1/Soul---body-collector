using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 최종보스와 일반 몬스터의 에셋 연결 상태를 변경 없이 검사합니다.
/// 씬 테스트 전에 Missing Script, 필수 컴포넌트, 핵심 기획 수치를 확인할 때 사용합니다.
/// </summary>
public static class HWJ_EnemyBossConfigurationValidator
{
    private const string FinalBossPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_FinalBoss_Runtime_Prefab.prefab";
    private const string FinalBossRootDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_FinalBoss_RootObjectData.asset";
    private const string FinalBossDefinitionFolder =
        "Assets/02Scripts/HWJ/ScriptableObjects/Systems/Boss/FinalBoss";
    private const string GeneralEnemyPrefabFolder =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady/Enemies";

    [MenuItem("Tools/HWJ/Test/최종보스 및 일반 몬스터 구성 검증")]
    public static void ValidateFromMenu()
    {
        List<string> errors = ValidateAll();
        LogResult(errors);
    }

    public static void ValidateBatch()
    {
        List<string> errors = ValidateAll();
        LogResult(errors);
        EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }

    public static List<string> ValidateAll()
    {
        List<string> errors = new List<string>();
        ValidateLoadedScenes(errors);
        ValidateFinalBoss(errors);
        ValidateGeneralEnemy("Slime", errors);
        ValidateGeneralEnemy("Rat", errors);
        return errors;
    }

    private static void ValidateLoadedScenes(List<string> errors)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(sceneIndex);

            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                continue;
            }

            GameObject[] sceneRoots = loadedScene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < sceneRoots.Length; rootIndex++)
            {
                Transform[] sceneTransforms =
                    sceneRoots[rootIndex].GetComponentsInChildren<Transform>(true);

                for (int transformIndex = 0; transformIndex < sceneTransforms.Length; transformIndex++)
                {
                    GameObject targetObject = sceneTransforms[transformIndex].gameObject;
                    int missingCount =
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(targetObject);

                    if (missingCount <= 0)
                    {
                        continue;
                    }

                    errors.Add(
                        $"현재 씬 오브젝트에 Missing Script가 {missingCount}개 있습니다: "
                        + $"{loadedScene.path} > {GetHierarchyPath(targetObject.transform)}");
                }
            }
        }
    }

    private static string GetHierarchyPath(Transform targetTransform)
    {
        string hierarchyPath = targetTransform.name;
        Transform currentParent = targetTransform.parent;

        while (currentParent != null)
        {
            hierarchyPath = $"{currentParent.name}/{hierarchyPath}";
            currentParent = currentParent.parent;
        }

        return hierarchyPath;
    }

    private static void ValidateFinalBoss(List<string> errors)
    {
        GameObject prefab = LoadRequired<GameObject>(FinalBossPrefabPath, errors);
        HWJ_RootObjectDataSO rootData =
            LoadRequired<HWJ_RootObjectDataSO>(FinalBossRootDataPath, errors);

        if (prefab != null)
        {
            ValidateMissingScripts(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_RootObjectDataResolver>(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_RuntimeStatusSystem>(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_CombatSystem>(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_BossBrainSystem>(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_BossPatternSystem>(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_FinalBossPatternSystem>(prefab, FinalBossPrefabPath, errors);
            RequireComponent<HWJ_FinalBossBarrierSystem>(prefab, FinalBossPrefabPath, errors);
        }

        if (rootData != null)
        {
            if (!rootData.TryGetTypeData(out HWJ_BossTypeDataSO bossType))
            {
                errors.Add("최종보스 RootObjectData에 BossTypeData가 연결되지 않았습니다.");
            }
            else
            {
                if (bossType.BossRank != HWJ_BossRank.FinalBoss)
                {
                    errors.Add("최종보스 BossRank가 FinalBoss가 아닙니다.");
                }

                if (bossType.FSM.locomotionMode != HWJ_BossLocomotionMode.StationaryCaster)
                {
                    errors.Add("최종보스 이동 정책이 StationaryCaster가 아닙니다.");
                }

                if (bossType.PossessionBody != null && bossType.PossessionBody.canBePossessed)
                {
                    errors.Add("최종보스는 빙의 불가여야 합니다.");
                }
            }
        }

        string[] definitionGuids = AssetDatabase.FindAssets(
            "t:HWJ_FinalBossPatternDefinitionSO",
            new[] { FinalBossDefinitionFolder });

        if (definitionGuids.Length != 10)
        {
            errors.Add($"최종보스 패턴 Definition은 10개여야 합니다. 현재={definitionGuids.Length}");
        }

        HashSet<HWJ_FinalBossPatternKind> kinds = new HashSet<HWJ_FinalBossPatternKind>();
        HWJ_FinalBossPatternDefinitionSO chargePattern = null;
        HWJ_FinalBossPatternDefinitionSO portalPattern = null;

        for (int i = 0; i < definitionGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(definitionGuids[i]);
            HWJ_FinalBossPatternDefinitionSO definition =
                AssetDatabase.LoadAssetAtPath<HWJ_FinalBossPatternDefinitionSO>(path);

            if (definition == null)
            {
                errors.Add($"최종보스 패턴 Definition을 불러오지 못했습니다: {path}");
                continue;
            }

            if (!kinds.Add(definition.PatternKind))
            {
                errors.Add($"최종보스 패턴 종류가 중복되었습니다: {definition.PatternKind}");
            }

            if (definition.PatternKind == HWJ_FinalBossPatternKind.Phase2BlackFlameCharge)
            {
                chargePattern = definition;
            }
            else if (definition.PatternKind == HWJ_FinalBossPatternKind.Phase2DoublePortalSummon)
            {
                portalPattern = definition;
            }
        }

        if (chargePattern == null
            || !Mathf.Approximately(chargePattern.PossessionMentalDamageRatio, 0.1f))
        {
            errors.Add("최종보스 2-3은 현재 빙의 정신력을 최대치의 10% 차감해야 합니다.");
        }

        if (portalPattern == null
            || portalPattern.PortalCount != 2
            || portalPattern.MonstersPerPortal != 4)
        {
            errors.Add("최종보스 2-4는 포탈 2개에서 각각 4마리씩 소환해야 합니다.");
        }
    }

    private static void ValidateGeneralEnemy(string codeName, List<string> errors)
    {
        string prefabPath =
            $"{GeneralEnemyPrefabFolder}/HWJ_Runtime_Enemy_General_{codeName}.prefab";
        GameObject prefab = LoadRequired<GameObject>(prefabPath, errors);

        if (prefab == null)
        {
            return;
        }

        ValidateMissingScripts(prefab, prefabPath, errors);
        HWJ_RootObjectDataResolver resolver =
            RequireComponent<HWJ_RootObjectDataResolver>(prefab, prefabPath, errors);
        RequireComponent<HWJ_RuntimeStatusSystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_CombatSystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_CharacterMotionSystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_EnemyNavigationSystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_EnemyPerceptionSystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_MonsterAISystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_EnemyAttackSystem>(prefab, prefabPath, errors);
        RequireComponent<HWJ_PossessionBodyState>(prefab, prefabPath, errors);
        RequireComponent<HWJ_LivePossessionMentalState>(prefab, prefabPath, errors);
        RequireComponent<Rigidbody2D>(prefab, prefabPath, errors);

        if (prefab.GetComponent<Collider2D>() == null)
        {
            errors.Add($"Collider2D가 없습니다: {prefabPath}");
        }

        if (resolver == null || resolver.RootObjectData == null)
        {
            errors.Add($"RootObjectData가 연결되지 않았습니다: {prefabPath}");
            return;
        }

        if (!resolver.RootObjectData.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyType))
        {
            errors.Add($"EnemyTypeData가 연결되지 않았습니다: {prefabPath}");
            return;
        }

        if (enemyType.PossessionBody == null || !enemyType.PossessionBody.canBePossessed)
        {
            errors.Add($"일반 몬스터가 생체 빙의 가능으로 설정되지 않았습니다: {prefabPath}");
        }
    }

    private static T LoadRequired<T>(string path, List<string> errors) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset == null)
        {
            errors.Add($"필수 에셋이 없습니다: {path}");
        }

        return asset;
    }

    private static T RequireComponent<T>(GameObject prefab, string path, List<string> errors)
        where T : Component
    {
        T component = prefab != null ? prefab.GetComponent<T>() : null;

        if (component == null)
        {
            errors.Add($"필수 컴포넌트 {typeof(T).Name}이 없습니다: {path}");
        }

        return component;
    }

    private static void ValidateMissingScripts(
        GameObject prefab,
        string path,
        List<string> errors)
    {
        Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
        int missingCount = 0;

        for (int i = 0; i < transforms.Length; i++)
        {
            missingCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                transforms[i].gameObject);
        }

        if (missingCount > 0)
        {
            errors.Add($"Missing Script가 {missingCount}개 있습니다: {path}");
        }
    }

    private static void LogResult(List<string> errors)
    {
        if (errors.Count == 0)
        {
            Debug.Log(
                "[HWJ 전투 테스트 검증] PASS: 최종보스, 슬라임, 쥐의 필수 구성과 핵심 수치가 정상입니다.");
            return;
        }

        for (int i = 0; i < errors.Count; i++)
        {
            Debug.LogError($"[HWJ 전투 테스트 검증][{i + 1}] {errors[i]}");
        }

        Debug.LogError($"[HWJ 전투 테스트 검증] FAIL: 오류 {errors.Count}개");
    }
}
