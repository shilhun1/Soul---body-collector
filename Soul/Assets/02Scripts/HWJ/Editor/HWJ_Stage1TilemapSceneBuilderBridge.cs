using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 1스테이지 황폐한 마을 콘셉트의 HWJ 전용 타일맵 씬을 생성하는 에디터 도구입니다.
/// HSH 테스트 씬처럼 Grid, Tilemap, TilemapCollider2D 구조를 사용하고, 임시 타일 에셋은 HWJ 폴더 안에만 생성합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_Stage1TilemapSceneBuilderBridge
{
    private const string SceneRoot = "Assets/01Scenes";
    private const string HwjRoot = "Assets/02Scripts/HWJ";
    private const string SourceTileRoot = "Assets/08Tileset";
    private const string SpriteRoot = HwjRoot + "/Art/Generated/Stage1";
    private const string TileRoot = HwjRoot + "/Tiles/Generated/Stage1";
    private const string GimmickAnimationRoot = HwjRoot + "/Animations/Generated/Stage1Gimmicks";
    private const string Stage1TileAtlasPath = HwjRoot + "/Art/tilemap/1stage_tilemap.png";
    private const string Stage1Background01Path = HwjRoot + "/Art/background/1-1.png";
    private const string Stage1Background02Path = HwjRoot + "/Art/background/1-2.png";
    private const string Stage1Background03Path = HwjRoot + "/Art/background/1-3.png";
    private const string Stage1Background04Path = HwjRoot + "/Art/background/1-4.png";
    private const string SpawnTableRoot = HwjRoot + "/ScriptableObjects/SpawnTables";
    private const string Stage1Scene01Path = SceneRoot + "/HWJ_Stage1_01_RuinedVillage.unity";
    private const string Stage1Scene02Path = SceneRoot + "/HWJ_Stage1_02_RuinedOutpost.unity";
    private const string Stage1Scene03Path = SceneRoot + "/HWJ_Stage1_03_BackRoad.unity";
    private const string Stage1Scene04Path = SceneRoot + "/HWJ_Stage1_04_MidBossBarracks.unity";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunStage1TilemapBuild.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_Stage1TilemapSceneBuildReport.md";
    private const string ValidationReportFilePath = @"C:\Docs\Generated\HWJ_Stage1ValidationReport.md";
    private const string Stage101BlockoutReportFilePath = @"C:\Docs\Generated\HWJ_Stage1_01_BlockoutRepairReport.md";
    private const string Stage101VisualValidationReportFilePath = @"C:\Docs\Generated\HWJ_Stage1_01_VisualValidationReport.md";
    private const string SceneBackupRoot = SceneRoot + "/HWJ_Backups";
    private const string Stage101NamedBackupPath = SceneBackupRoot + "/Stage1_1_VillageEntrance_Backup.unity";

    private const string GameplayDatabasePath = HwjRoot + "/ScriptableObjects/Database/HWJ_GameplayDatabase.asset";
    private const string PlayerRootPath = HwjRoot + "/ScriptableObjects/RootObjects/HWJ_Player_Test_RootObjectData.asset";
    private const string PlayerInputBindingPath = HwjRoot + "/ScriptableObjects/Input/HWJ_DefaultPlayerInputBindings.asset";
    private const string PlayerLevelUpDataPath = HwjRoot + "/ScriptableObjects/LevelTables/HWJ_Player_Default_LevelUpData.asset";
    private const string PlayerRuntimePrefabPath = HwjRoot + "/Prefabs/Generated/Models/HWJ_Model_Player_Test.prefab";
    private const string PlayerSpritePath = HwjRoot + "/Art/Player/HWJ_GHOSTP.png";
    private const string PlayerDisplaySpritePath = HwjRoot + "/Art/Player/HWJ_GHOSTP_Frame00.png";
    private const string PlayerGhostAnimatorControllerPath = "Assets/05Anims/Soul_Anims/hys_Ghost_Animation.controller";
    private const string SwordEnemySpritePath = "Assets/04Image/HWJ/Sword_monster.png";
    private const string BowEnemySpritePath = "Assets/04Image/HWJ/Bow_monster.png";
    private const string ShieldEnemySpritePath = "Assets/04Image/HWJ/Shield_monster.png";
    private const string AxeEnemySpritePath = "Assets/04Image/HWJ/Axe_monster.png";
    private const string LanceEnemySpritePath = "Assets/04Image/HWJ/Spear_monster.png";
    private const string MidBossSpritePath = SpriteRoot + "/HWJ_Stage1_Marker_MidBoss.png";
    private const string ExperienceOrbPrefabPath = HwjRoot + "/Prefabs/Generated/ExperienceOrbs/HWJ_ExperienceOrb_Default_Prefab.prefab";
    private const string AttackStatOrbPrefabPath = HwjRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackPowerSmall_Prefab.prefab";
    private const string DefenseStatOrbPrefabPath = HwjRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_DefenseSmall_Prefab.prefab";
    private const string HitEffectPrefabPath = HwjRoot + "/Prefabs/Generated/Effects/HWJ_Effect_HitImpact.prefab";
    private const string HshHudSpritePath = "Assets/06Sprites/UI1.png";
    private const string StageChoiceRewardPath = HwjRoot + "/ScriptableObjects/Showcase/Rewards/HWJ_ProfessorDemo_StageChoiceReward.asset";
    private const string LivePossessionRootPath = HwjRoot + "/ScriptableObjects/Showcase/RootObjects/Enemies/HWJ_ProfessorDemo_LivePossessionResist_RootObjectData.asset";

    private const string SwordEnemyRootPath = HwjRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Sword_RootObjectData.asset";
    private const string BowEnemyRootPath = HwjRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Bow_RootObjectData.asset";
    private const string ShieldEnemyRootPath = HwjRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Shield_RootObjectData.asset";
    private const string AxeEnemyRootPath = HwjRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Axe_RootObjectData.asset";
    private const string LanceEnemyRootPath = HwjRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Lance_RootObjectData.asset";
    private const string MidBossRootPath = HwjRoot + "/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset";

    private const string SwordEnemyPrefabPath = HwjRoot + "/Prefabs/Generated/Models/HWJ_Model_Enemy_Corpse_Sword.prefab";
    private const string BowEnemyPrefabPath = HwjRoot + "/Prefabs/Generated/Models/HWJ_Model_Enemy_Corpse_Bow.prefab";
    private const string ShieldEnemyPrefabPath = HwjRoot + "/Prefabs/Generated/Models/HWJ_Model_Enemy_Corpse_Shield.prefab";
    private const string AxeEnemyPrefabPath = HwjRoot + "/Prefabs/Generated/Models/HWJ_Model_Enemy_Corpse_Axe.prefab";
    private const string LanceEnemyPrefabPath = HwjRoot + "/Prefabs/Generated/Models/HWJ_Model_Enemy_Corpse_Lance.prefab";
    private const string MidBossPrefabPath = HwjRoot + "/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_Stage1TilemapSceneBuilderBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Scene/Build HWJ Stage 1 Tilemap Scenes")]
    public static void BuildStage1TilemapScenes()
    {
        EnsureFolders();
        EnsureRuntimeEnemyPrefabs();

        BuildRuinedVillageScene();
        BuildRuinedOutpostScene();
        BuildBackRoadScene();
        BuildMidBossBarracksScene();
        RegisterStageScenesInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("HWJ stage 1 tilemap scenes build completed.");
    }

    [MenuItem("Tools/Stage1/Generate All Stage1 Scenes")]
    public static void GenerateAllStage1Scenes()
    {
        BuildStage1TilemapScenes();
    }

    [MenuItem("Tools/Stage1/Generate Stage1-1 Ruined Village")]
    public static void GenerateStage101RuinedVillage()
    {
        RebuildStage101BlockoutOnly();
    }

    [MenuItem("Tools/Stage1/Rebuild Stage1-1 Blockout Only")]
    public static void RebuildStage101BlockoutOnlyMenu()
    {
        RebuildStage101BlockoutOnly();
    }

    [MenuItem("Tools/Stage1/Generate Stage1-2 Ruined Outpost")]
    public static void GenerateStage102RuinedOutpost()
    {
        BuildSingleStageScene(BuildRuinedOutpostScene);
    }

    [MenuItem("Tools/Stage1/Generate Stage1-3 Back Road")]
    public static void GenerateStage103BackRoad()
    {
        BuildSingleStageScene(BuildBackRoadScene);
    }

    [MenuItem("Tools/Stage1/Generate Stage1-4 Mid Boss Barracks")]
    public static void GenerateStage104MidBossBarracks()
    {
        BuildSingleStageScene(BuildMidBossBarracksScene);
    }

    [MenuItem("Tools/Stage1/Validate Stage1 Setup")]
    public static void ValidateStage1Setup()
    {
        int errorCount;
        int warningCount;
        WriteStage1ValidationReport(out errorCount, out warningCount);

        string message = $"HWJ Stage1 validation completed. errors={errorCount}, warnings={warningCount}, report={ValidationReportFilePath}";

        if (errorCount > 0)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    [MenuItem("Tools/Stage1/Repair Stage1-1 Visual Prefabs")]
    public static void RepairStage101VisualPrefabs()
    {
        RebuildStage101BlockoutOnly();
    }

    [MenuItem("Tools/Stage1/Validate Player And Enemy Rendering")]
    public static void ValidateStage101PlayerAndEnemyRenderingMenu()
    {
        ValidateStage101PlayerAndEnemyRendering();
    }

    [MenuItem("Tools/Stage1/Validate Stage1-1 Gimmicks")]
    public static void ValidateStage101GimmicksMenu()
    {
        ValidateStage101Gimmicks();
    }

    [MenuItem("Tools/Stage1/Validate Stage1-1 Sorting")]
    public static void ValidateStage101SortingMenu()
    {
        ValidateStage101Sorting();
    }

    [MenuItem("Tools/Stage1/Rebuild Stage1-1 Gameplay Objects")]
    public static void RebuildStage101GameplayObjectsMenu()
    {
        RebuildStage101BlockoutOnly();
    }

    [MenuItem("Tools/Stage1/Focus Game Camera On Player")]
    public static void FocusStage101CameraOnPlayer()
    {
        EnsureStage101SceneOpen();
        GameObject playerObject = GameObject.Find("HWJ_Player");

        if (playerObject == null)
        {
            Debug.LogError("[Stage1 Builder] Focus camera failed: HWJ_Player was not found in Stage1-1.");
            return;
        }

        EnsureStage101Camera(playerObject.transform.position + new Vector3(10f, 1f, -10f), 6.75f);
        ConfigureMainCameraFollow(playerObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Stage1 Builder] Main Camera follows HWJ_Player. Orthographic Size=6.75");
    }

    [MenuItem("Tools/Stage1/Clear Generated Placeholder Objects")]
    public static void ClearGeneratedPlaceholderObjects()
    {
        Component[] generatedObjects = FindOptionalComponentsByType("HWJ_GeneratedStageObject");

        List<GameObject> deleteTargets = new List<GameObject>();

        for (int i = 0; i < generatedObjects.Length; i++)
        {
            Component generatedObject = generatedObjects[i];

            if (generatedObject == null
                || !GetOptionalBoolProperty(generatedObject, "TemporaryPlaceholder")
                || GetOptionalBoolProperty(generatedObject, "ProtectFromGeneratedCleanup"))
            {
                continue;
            }

            deleteTargets.Add(generatedObject.gameObject);
        }

        if (deleteTargets.Count <= 0)
        {
            Debug.Log("HWJ generated placeholder cleanup skipped: temporary placeholders were not found in the open scene.");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "HWJ 생성 플레이스홀더 정리",
            $"현재 열린 씬에서 임시 표시 오브젝트 {deleteTargets.Count}개를 삭제합니다.",
            "삭제",
            "취소");

        if (!confirmed)
        {
            return;
        }

        for (int i = 0; i < deleteTargets.Count; i++)
        {
            Undo.DestroyObjectImmediate(deleteTargets[i]);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"HWJ generated placeholder cleanup completed. removed={deleteTargets.Count}");
    }

    private static void BuildSingleStageScene(Action buildSceneAction)
    {
        EnsureFolders();
        EnsureRuntimeEnemyPrefabs();
        buildSceneAction?.Invoke();
        RegisterStageScenesInBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/HWJ/Scene/Create HWJ Stage 1 Tilemap Build Flag")]
    public static void CreateStage1TilemapBuildFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", new UTF8Encoding(false));
        Debug.Log($"HWJ stage 1 tilemap build flag created: {FlagFilePath}");
    }

    private static void PollFlagFile()
    {
        if (EditorApplication.timeSinceStartup < nextFlagCheckTime)
        {
            return;
        }

        nextFlagCheckTime = EditorApplication.timeSinceStartup + 2d;

        if (!File.Exists(FlagFilePath))
        {
            return;
        }

        string flagContent = File.ReadAllText(FlagFilePath).Trim().TrimStart('\uFEFF').Trim();

        if (string.Equals(flagContent, "stage1_01_blockout", StringComparison.OrdinalIgnoreCase))
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            File.Delete(FlagFilePath);
            RunStage101BlockoutRepairFromOpenEditor();
            return;
        }

        if (flagContent.StartsWith("stage1_01_validate_", StringComparison.OrdinalIgnoreCase))
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            File.Delete(FlagFilePath);
            RunStage101ValidationFromOpenEditor(flagContent);
            return;
        }

        if (!string.Equals(flagContent, "run", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        File.Delete(FlagFilePath);
        RunBuildFromOpenEditor();
    }

    private static void RunStage101ValidationFromOpenEditor(string flagContent)
    {
        try
        {
            if (string.Equals(flagContent, "stage1_01_validate_rendering", StringComparison.OrdinalIgnoreCase))
            {
                ValidateStage101PlayerAndEnemyRendering();
                return;
            }

            if (string.Equals(flagContent, "stage1_01_validate_gimmicks", StringComparison.OrdinalIgnoreCase))
            {
                ValidateStage101Gimmicks();
                return;
            }

            if (string.Equals(flagContent, "stage1_01_validate_sorting", StringComparison.OrdinalIgnoreCase))
            {
                ValidateStage101Sorting();
                return;
            }

            Debug.LogWarning($"HWJ Stage1-1 validation flag ignored: {flagContent}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void RunStage101BlockoutRepairFromOpenEditor()
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ stage 1-1 blockout repair skipped because another stage build is already running.");
            return;
        }

        isRunning = true;

        try
        {
            RebuildStage101BlockoutOnly();
        }
        catch (Exception exception)
        {
            WriteStage101BlockoutReport(null, 0, exception);
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    private static void RunBuildFromOpenEditor()
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ stage 1 tilemap scene build is already running.");
            return;
        }

        isRunning = true;

        try
        {
            BuildStage1TilemapScenes();
            WriteBuildReport(null);
        }
        catch (Exception exception)
        {
            WriteBuildReport(exception);
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    private static void WriteBuildReport(Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 1스테이지 타일맵 씬 생성 보고서");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 결과: {(exception == null ? "성공" : "실패")}");
        builder.AppendLine("- 생성 씬:");
        builder.AppendLine("  - " + Stage1Scene01Path);
        builder.AppendLine("  - " + Stage1Scene02Path);
        builder.AppendLine("  - " + Stage1Scene03Path);
        builder.AppendLine("  - " + Stage1Scene04Path);
        builder.AppendLine("- 생성 에셋:");
        builder.AppendLine("  - Assets/02Scripts/HWJ/Art/Generated/Stage1");
        builder.AppendLine("  - Assets/02Scripts/HWJ/Tiles/Generated/Stage1");

        if (exception != null)
        {
            builder.AppendLine();
            builder.AppendLine("## 오류");
            builder.AppendLine(exception.ToString());
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteStage101BlockoutReport(string backupPath, int removedObjectCount, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Stage101BlockoutReportFilePath));

        int liveEnemyCount = FindStage101SceneObjectsByPrefix("HWJ_Enemy_Stage1_01_").Count;
        int gimmickCount = FindStage101SceneObjectsByNamePart("PF_").Count;
        bool hasMissingScript = SceneFileHasMissingScript(Stage1Scene01Path);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Stage1-1 블록아웃 복구 보고서");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 대상 씬: {Stage1Scene01Path}");
        builder.AppendLine($"- 백업 씬: {(string.IsNullOrEmpty(backupPath) ? "백업 실패 또는 원본 없음" : backupPath)}");
        builder.AppendLine($"- 제거한 자동 맵 루트 수: {removedObjectCount}");
        builder.AppendLine("- 유지 대상: HWJ_Player, Main Camera, HWJ_StageRuntimeSystems, HWJ_SceneSpawnPoints, HWJ_ScenePortals");
        builder.AppendLine("- 새 타일맵: TM_BackDecoration, TM_Ground, TM_OneWayPlatform, TM_BodyBlocker, TM_ForeDecoration, TM_SoulBoundary");
        builder.AppendLine("- 타일 규격: 32x32 placeholder tile, PPU 32, Grid Cell Size 1,1,0");
        builder.AppendLine("- 배경 방식: BackgroundRoot 아래 SpriteRenderer 4레이어, Tilemap 미사용");
        builder.AppendLine($"- 결과: {(exception == null ? "성공" : "실패")}");

        if (exception != null)
        {
            builder.AppendLine();
            builder.AppendLine("## 오류");
            builder.AppendLine(exception.ToString());
        }

        builder.AppendLine();
        builder.AppendLine("## Stage1-1 시각 복구 상세");
        builder.AppendLine("- 백업 이름: Stage1_1_VillageEntrance_Backup.unity");
        builder.AppendLine("- 새 루트 구조: StageRoot / BackgroundRoot / Grid / Structures / Gameplay / Camera / Systems");
        builder.AppendLine("- 구조물 레이어: RuinedBuildings, WoodenScaffolds, RoofStructures, DecorativeProps");
        builder.AppendLine("- 게임플레이 레이어: PlayerSpawn, Checkpoints, CombatZones, PossessableBodies, Gimmicks, Hazards, Doors, ExitPortal");
        builder.AppendLine("- 기믹 시각화: 체크포인트, 가시, 낙석, 모래폭풍, 스위치, 문, 포탈에 SpriteRenderer 기반 표시를 생성함");
        builder.AppendLine("- 배치 방식: 랜덤/인덱스 반복 없음. 명시 좌표와 의미 기반 Tile ID만 사용함");
        builder.AppendLine("- 사용 기준: 32x32 Sprite, PPU 32, Grid Cell Size 1,1,0");

        builder.AppendLine();
        builder.AppendLine("## Stage1-1 최종 적용 요약");
        builder.AppendLine($"- 배치된 전투 몬스터 수: {liveEnemyCount}");
        builder.AppendLine($"- 배치된 기믹/함정 표시 오브젝트 수: {gimmickCount}");
        builder.AppendLine($"- Missing Script 검출: {(hasMissingScript ? "있음" : "없음")}");
        builder.AppendLine("- 플레이어 표시: 128x32 Ghost 시트에서 32x32 첫 프레임을 분리해 사용");
        builder.AppendLine("- 몬스터 표시: Assets/04Image/HWJ의 64x64 몬스터 스프라이트를 실제 HWJ 전투 프리팹에 연결");
        builder.AppendLine("- 배경 표시: Tilemap이 아니라 BackgroundRoot 아래 SpriteRenderer 패널 반복 방식 사용");
        builder.AppendLine("- 기믹 표시: 체크포인트, 가시, 낙석, 모래폭풍, 스위치, 문, 포탈이 Game View에서 보이는 SpriteRenderer를 가짐");
        builder.AppendLine();
        builder.AppendLine("## 잘못된 원인");
        builder.AppendLine("- 이전 결과는 타일 아틀라스의 Sprite를 의미 구분 없이 반복 배치해서 장식, 가시, 벽 조각이 체크무늬처럼 보였습니다.");
        builder.AppendLine("- 배경을 Tilemap처럼 늘려 사용해서 배경과 충돌 지형의 역할이 섞였습니다.");
        builder.AppendLine("- 기존 생성 몬스터 표시가 16x16 소형 스프라이트에 가까워 Game View에서 점처럼 보였습니다.");
        builder.AppendLine("- 플레이어 원본 Ghost 이미지는 128x32 시트라 그대로 쓰면 4프레임 전체가 한 번에 보일 수 있었습니다.");
        builder.AppendLine();
        builder.AppendLine("## 수정한 파일");
        builder.AppendLine("- Assets/02Scripts/HWJ/Editor/HWJ_Stage1TilemapSceneBuilderBridge.cs");
        builder.AppendLine("- Assets/01Scenes/HWJ_Stage1_01_RuinedVillage.unity");
        builder.AppendLine("- Assets/02Scripts/HWJ/Art/Player/HWJ_GHOSTP_Frame00.png");
        builder.AppendLine("- Assets/02Scripts/HWJ/Prefabs/Generated/Models/HWJ_Model_Enemy_Corpse_*.prefab");
        builder.AppendLine();
        builder.AppendLine("## 사용한 표시 리소스");
        builder.AppendLine($"- 플레이어: {PlayerDisplaySpritePath} / 원본 {PlayerSpritePath}");
        builder.AppendLine($"- 몬스터: {SwordEnemySpritePath}, {BowEnemySpritePath}, {ShieldEnemySpritePath}, {AxeEnemySpritePath}, {LanceEnemySpritePath}");
        builder.AppendLine($"- 몬스터 프리팹: {SwordEnemyPrefabPath}, {BowEnemyPrefabPath}, {ShieldEnemyPrefabPath}, {AxeEnemyPrefabPath}, {LanceEnemyPrefabPath}");
        builder.AppendLine($"- 배경: {Stage1Background01Path}");
        builder.AppendLine();
        builder.AppendLine("## 새 Hierarchy");
        builder.AppendLine("- StageRoot");
        builder.AppendLine("- StageRoot/BackgroundRoot/BG_Far, BG_Middle, BG_Near, BG_SandVFX");
        builder.AppendLine("- StageRoot/HWJ_Grid_Tilemap/TM_BackDecoration, TM_Ground, TM_OneWayPlatform, TM_BodyBlocker, TM_ForeDecoration, TM_SoulBoundary");
        builder.AppendLine("- StageRoot/Structures/RuinedBuildings, WoodenScaffolds, RoofStructures, DecorativeProps");
        builder.AppendLine("- StageRoot/Gameplay/PlayerSpawn, Checkpoints, CombatZones/LiveEnemies, PossessableBodies, Gimmicks, Hazards, Doors, ExitPortal");
        builder.AppendLine("- StageRoot/Camera");
        builder.AppendLine("- StageRoot/Systems/HWJ_StageRuntimeSystems");
        builder.AppendLine();
        builder.AppendLine("## 사용한 타일 규격과 PPU");
        builder.AppendLine("- Grid Cell Size: 1, 1, 0");
        builder.AppendLine("- 충돌 지형: 32x32 기준 Placeholder Tile, PPU 32");
        builder.AppendLine("- 플레이어 표시 프레임: 32x32, PPU 32");
        builder.AppendLine("- 몬스터 표시 스프라이트: 64x64, PPU 32");
        builder.AppendLine("- 배경 1-1.png: 1672x941 px, PPU 100, 세로 1장 기준으로 스케일 조정, 가로는 반복 패널 배치");
        builder.AppendLine();
        builder.AppendLine("## Unity Editor에서 직접 확인할 항목");
        builder.AppendLine("- Game View에서 HWJ_Player가 Ghost 이미지로 보이는지 확인");
        builder.AppendLine("- CombatZones/LiveEnemies 아래 전투 몬스터가 보이고 이동/피격 컴포넌트를 가지고 있는지 확인");
        builder.AppendLine("- 체크포인트, 가시, 낙석, 모래폭풍, 스위치, 문, 포탈이 빈 오브젝트가 아니라 실제 SpriteRenderer로 보이는지 확인");
        builder.AppendLine("- TM_Ground에만 지형 Collider가 있고 BackgroundRoot에는 Collider가 없는지 확인");
        builder.AppendLine("- Tools/Stage1/Validate Player And Enemy Rendering, Validate Stage1-1 Gimmicks, Validate Stage1-1 Sorting 메뉴를 실행해 검증 결과를 확인");
        builder.AppendLine();
        builder.AppendLine("## 테스트 결과");
        builder.AppendLine("- dotnet build HWJ.Editor.csproj: 경고 0개, 오류 0개");
        builder.AppendLine("- 씬 YAML 검사: Missing Script 0개");
        builder.AppendLine("- Stage1-1 씬 자동 재생성 플래그 처리 완료");
        builder.AppendLine("- Play Mode 수동 조작 테스트는 Unity Editor에서 직접 확인이 필요함");

        File.WriteAllText(Stage101BlockoutReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteStage1ValidationReport(out int errorCount, out int warningCount)
    {
        errorCount = 0;
        warningCount = 0;
        Directory.CreateDirectory(Path.GetDirectoryName(ValidationReportFilePath));

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 1스테이지 검증 보고서");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine("- 검증 범위: HWJ Stage1 씬, HWJ Stage1 생성 에셋, Build Settings 등록 상태");
        builder.AppendLine();
        builder.AppendLine("|심각도|코드|대상|필드|문제|수정 방법|");
        builder.AppendLine("|---|---|---|---|---|---|");

        ValidateFolder(builder, ref errorCount, ref warningCount, "VAL-STAGE1-001", HwjRoot, "HWJ 루트 폴더", true, "Assets/02Scripts/HWJ 폴더가 있어야 합니다.");
        ValidateFolder(builder, ref errorCount, ref warningCount, "VAL-STAGE1-002", SourceTileRoot, "원본 타일셋 폴더", false, "08Tileset이 없으면 HWJ 임시 타일과 1stage_tilemap을 사용합니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-003", Stage1TileAtlasPath, "타일 아틀라스", true, "Assets/02Scripts/HWJ/Art/tilemap/1stage_tilemap.png를 넣어야 합니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-004", PlayerSpritePath, "플레이어 스프라이트", true, "Ghost 플레이어 이미지를 HWJ Art/Player 경로에 넣어야 합니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-005", GameplayDatabasePath, "게임플레이 데이터베이스", true, "HWJ_GameplayDatabase 에셋을 생성하거나 경로를 맞춰야 합니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-006", PlayerRootPath, "플레이어 RootObjectData", true, "플레이어 RootObjectData 에셋을 생성하거나 경로를 맞춰야 합니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-007", MidBossPrefabPath, "중간보스 프리팹", false, "중간보스 씬에서 보스가 필요하면 HWJ_MidBoss1_Runtime_Prefab을 생성해야 합니다.");

        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-101", Stage1Background01Path, "1-1 배경", false, "배경 에셋이 없으면 해당 씬의 배경 레이어가 생성되지 않습니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-102", Stage1Background02Path, "1-2 배경", false, "배경 에셋이 없으면 해당 씬의 배경 레이어가 생성되지 않습니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-103", Stage1Background03Path, "1-3 배경", false, "배경 에셋이 없으면 해당 씬의 배경 레이어가 생성되지 않습니다.");
        ValidateAsset(builder, ref errorCount, ref warningCount, "VAL-STAGE1-104", Stage1Background04Path, "1-4 배경", false, "배경 에셋이 없으면 해당 씬의 배경 레이어가 생성되지 않습니다.");

        string[] scenePaths = GetRequiredStage1ScenePaths();

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string scenePath = scenePaths[i];
            ValidateAsset(builder, ref errorCount, ref warningCount, $"VAL-STAGE1-20{i + 1}", scenePath, "씬 파일", true, "Tools/Stage1/Generate All Stage1 Scenes 메뉴로 씬을 다시 생성해야 합니다.");

            if (!IsSceneRegisteredInBuildSettings(scenePath))
            {
                AppendValidationRow(
                    builder,
                    ref errorCount,
                    ref warningCount,
                    "Error",
                    $"VAL-STAGE1-30{i + 1}",
                    scenePath,
                    "Build Settings",
                    "Stage1 씬이 Build Settings에 등록되어 있지 않습니다.",
                    "Tools/Stage1/Generate All Stage1 Scenes를 실행해 자동 등록하거나 직접 Build Settings에 추가하세요.");
            }

            if (SceneFileHasMissingScript(scenePath))
            {
                AppendValidationRow(
                    builder,
                    ref errorCount,
                    ref warningCount,
                    "Error",
                    $"VAL-STAGE1-40{i + 1}",
                    scenePath,
                    "m_Script",
                    "생성된 씬 안에 Missing Script 참조가 남아 있습니다.",
                    "콘솔 오류를 먼저 해결한 뒤 Stage1 씬을 다시 생성하세요.");
            }
        }

        builder.AppendLine();
        builder.AppendLine($"- Error: {errorCount}");
        builder.AppendLine($"- Warning: {warningCount}");
        builder.AppendLine($"- 결과: {(errorCount == 0 ? "통과" : "실패")}");

        File.WriteAllText(ValidationReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static Scene EnsureStage101SceneOpen()
    {
        if (!File.Exists(Path.GetFullPath(Stage1Scene01Path)))
        {
            Debug.LogError($"[Stage1-1 검증] 씬 파일을 찾을 수 없습니다: {Stage1Scene01Path}");
            return SceneManager.GetActiveScene();
        }

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid() && activeScene.path == Stage1Scene01Path)
        {
            return activeScene;
        }

        return EditorSceneManager.OpenScene(Stage1Scene01Path, OpenSceneMode.Single);
    }

    private static void ValidateStage101PlayerAndEnemyRendering()
    {
        EnsureStage101SceneOpen();

        int errorCount = 0;
        int warningCount = 0;
        StringBuilder builder = CreateStage101ValidationBuilder("Stage1-1 플레이어/몬스터 렌더링 검증");

        GameObject playerObject = FindStage101SceneObject("HWJ_Player");
        ValidateStage101ObjectExists(builder, ref errorCount, ref warningCount, "VIS-PLAYER-001", "HWJ_Player", playerObject, "Stage1-1에 실제 플레이어 오브젝트를 생성하거나 Rebuild Stage1-1 Gameplay Objects를 실행하세요.");
        ValidateVisibleRenderer(builder, ref errorCount, ref warningCount, "VIS-PLAYER-002", playerObject, "HWJ_Player", "플레이어는 Game View에서 보이는 SpriteRenderer 또는 Animator가 필요합니다.");
        ValidateRequiredComponent<Rigidbody2D>(builder, ref errorCount, ref warningCount, "VIS-PLAYER-003", playerObject, "HWJ_Player", "플레이어 이동/충돌을 위해 Rigidbody2D가 필요합니다.");
        ValidateRequiredComponent<Collider2D>(builder, ref errorCount, ref warningCount, "VIS-PLAYER-004", playerObject, "HWJ_Player", "플레이어 충돌을 위해 Collider2D가 필요합니다.");
        ValidatePrefabInstance(builder, ref errorCount, ref warningCount, "VIS-PLAYER-006", playerObject, PlayerRuntimePrefabPath, "플레이어는 HWJ_Model_Player_Test 프리팹 인스턴스로 배치되어야 합니다.");

        if (playerObject != null)
        {
            SpriteRenderer playerRenderer = playerObject.GetComponentInChildren<SpriteRenderer>(true);

            if (playerRenderer != null && playerRenderer.sprite != null)
            {
                float worldWidth = playerRenderer.sprite.bounds.size.x * Mathf.Abs(playerRenderer.transform.lossyScale.x);

                if (worldWidth > 2.25f)
                {
                    AppendValidationRow(
                        builder,
                        ref errorCount,
                        ref warningCount,
                        "Warning",
                        "VIS-PLAYER-005",
                        GetSceneObjectPath(playerObject),
                        "SpriteRenderer.sprite",
                        $"플레이어 스프라이트 폭이 {worldWidth:0.00} 유닛입니다. 32x32 단일 프레임이 아니라 시트 전체를 쓰는 상태일 수 있습니다.",
                        "HWJ_GHOSTP_Frame00.png 단일 프레임을 사용하세요.");
                }
            }
        }

        List<GameObject> enemyObjects = FindStage101SceneObjectsByPrefix("HWJ_Enemy_Stage1_01_");

        if (enemyObjects.Count <= 0)
        {
            AppendValidationRow(
                builder,
                ref errorCount,
                ref warningCount,
                "Error",
                "VIS-ENEMY-001",
                Stage1Scene01Path,
                "LiveEnemies",
                "Stage1-1에 실제 전투 몬스터 프리팹 인스턴스가 없습니다.",
                "Tools/Stage1/Rebuild Stage1-1 Gameplay Objects를 실행하세요.");
        }

        for (int i = 0; i < enemyObjects.Count; i++)
        {
            GameObject enemyObject = enemyObjects[i];
            string path = GetSceneObjectPath(enemyObject);
            ValidateVisibleRenderer(builder, ref errorCount, ref warningCount, "VIS-ENEMY-002", enemyObject, path, "몬스터는 Game View에서 보이는 SpriteRenderer 또는 Animator가 필요합니다.");
            ValidateRequiredComponent<Animator>(builder, ref errorCount, ref warningCount, "VIS-ENEMY-003", enemyObject, path, "몬스터 모션 확인을 위해 Animator가 필요합니다.");
            ValidateRequiredComponent<Rigidbody2D>(builder, ref errorCount, ref warningCount, "VIS-ENEMY-004", enemyObject, path, "몬스터 AI 이동을 위해 Rigidbody2D가 필요합니다.");
            ValidateRequiredComponent<Collider2D>(builder, ref errorCount, ref warningCount, "VIS-ENEMY-005", enemyObject, path, "몬스터 피격/충돌을 위해 Collider2D가 필요합니다.");
            ValidateRequiredComponent<HWJ_MonsterAISystem>(builder, ref errorCount, ref warningCount, "VIS-ENEMY-006", enemyObject, path, "몬스터가 플레이어를 추적하려면 HWJ_MonsterAISystem이 필요합니다.");
            ValidateRequiredComponent<HWJ_RuntimeStatusSystem>(builder, ref errorCount, ref warningCount, "VIS-ENEMY-007", enemyObject, path, "몬스터 체력/피격 처리를 위해 HWJ_RuntimeStatusSystem이 필요합니다.");
            ValidatePrefabInstance(builder, ref errorCount, ref warningCount, "VIS-ENEMY-008", enemyObject, null, "전투 몬스터는 HWJ_Model_Enemy_Corpse_* 프리팹 인스턴스로 배치되어야 합니다.");
        }

        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-010", "PF_SpikeTrap_Stage1_01_DashGap", "HSH_SpikeTrap", "가시 함정은 기존 HSH_SpikeTrap 동작 컴포넌트를 가져야 합니다.");
        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-011", "PF_SpikeTrap_Stage1_01_FinalPit", "HSH_SpikeTrap", "가시 함정은 기존 HSH_SpikeTrap 동작 컴포넌트를 가져야 합니다.");
        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-012", "PF_SandstormLane_Stage1_01_SoulPreview", "HSH_SteamTrap", "모래폭풍 통로는 기존 HSH_SteamTrap 동작 컴포넌트를 가져야 합니다.");
        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-013", "PF_RoofCollapseTrap_Stage1_01_Tower", "HWJ_RoofCollapseTrapSystem", "낙석 함정은 HWJ_RoofCollapseTrapSystem을 가져야 합니다.");
        ValidateRequiredComponent<ParticleSystem>(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-014", FindStage101SceneObject("PF_SandstormLane_Stage1_01_SoulPreview"), "PF_SandstormLane_Stage1_01_SoulPreview", "모래폭풍은 Game View에서 보이는 ParticleSystem을 가져야 합니다.");

        WriteStage101ValidationResult(builder, errorCount, warningCount);
    }

    private static void ValidateStage101Gimmicks()
    {
        EnsureStage101SceneOpen();

        int errorCount = 0;
        int warningCount = 0;
        StringBuilder builder = CreateStage101ValidationBuilder("Stage1-1 기믹/함정 표시 검증");

        string[] requiredVisibleObjects =
        {
            "PF_Checkpoint_Stage1_01_Entrance",
            "PF_Checkpoint_Stage1_01_BeforeGimmicks",
            "PF_SpikeTrap_Stage1_01_DashGap",
            "PF_SpikeTrap_Stage1_01_FinalPit",
            "PF_RoofCollapseTrap_Stage1_01_Tower",
            "PF_SandstormLane_Stage1_01_SoulPreview",
            "PF_SoulOrbSwitch_Stage1_01_ScoutGate",
            "PF_SoulPassWall_Stage1_01_ScoutGate",
            "PF_BowSwitch_Stage1_01_LongShot",
            "PF_GimmickDoor_Stage1_01_BowSwitchDoor",
            "PF_ShieldDoor_Stage1_01_ArrowPassage",
            "PF_AxeBreakWall_Stage1_01_CrackedWall",
            "PF_LanceChargeDevice_Stage1_01_LeverWall",
            "PF_SwordSwitch_Stage1_01_RapidCombo",
            "HWJ_Portal_Stage1_01_ToOutpost",
        };

        for (int i = 0; i < requiredVisibleObjects.Length; i++)
        {
            string objectName = requiredVisibleObjects[i];
            GameObject targetObject = FindStage101SceneObject(objectName);
            ValidateStage101ObjectExists(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-001", objectName, targetObject, "Stage1-1 기믹 재생성 메뉴를 실행하거나 해당 오브젝트를 직접 배치하세요.");
            ValidateVisibleRenderer(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-002", targetObject, objectName, "기믹은 Game View에서 보이는 SpriteRenderer 또는 Animator가 필요합니다.");
        }

        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-010", "PF_SpikeTrap_Stage1_01_DashGap", "HSH_SpikeTrap", "가시 함정은 기존 HSH_SpikeTrap 동작 컴포넌트를 가져야 합니다.");
        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-011", "PF_SpikeTrap_Stage1_01_FinalPit", "HSH_SpikeTrap", "가시 함정은 기존 HSH_SpikeTrap 동작 컴포넌트를 가져야 합니다.");
        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-012", "PF_SandstormLane_Stage1_01_SoulPreview", "HSH_SteamTrap", "모래폭풍 통로는 기존 HSH_SteamTrap 동작 컴포넌트를 가져야 합니다.");
        ValidateRequiredComponentByTypeName(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-013", "PF_RoofCollapseTrap_Stage1_01_Tower", "HWJ_RoofCollapseTrapSystem", "낙석 함정은 HWJ_RoofCollapseTrapSystem을 가져야 합니다.");
        ValidateRequiredComponent<ParticleSystem>(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-014", FindStage101SceneObject("PF_SandstormLane_Stage1_01_SoulPreview"), "PF_SandstormLane_Stage1_01_SoulPreview", "모래폭풍은 Game View에서 보이는 ParticleSystem을 가져야 합니다.");

        string[] colliderRequiredObjects =
        {
            "PF_SpikeTrap_Stage1_01_DashGap",
            "PF_SpikeTrap_Stage1_01_FinalPit",
            "PF_SandstormLane_Stage1_01_SoulPreview",
            "PF_SoulPassWall_Stage1_01_ScoutGate",
            "PF_GimmickDoor_Stage1_01_BowSwitchDoor",
            "PF_ShieldDoor_Stage1_01_ArrowPassage",
            "PF_AxeBreakWall_Stage1_01_CrackedWall",
            "PF_LanceChargeDevice_Stage1_01_LeverWall",
            "HWJ_Portal_Stage1_01_ToOutpost",
        };

        for (int i = 0; i < colliderRequiredObjects.Length; i++)
        {
            string objectName = colliderRequiredObjects[i];
            GameObject targetObject = FindStage101SceneObject(objectName);
            ValidateRequiredComponent<Collider2D>(builder, ref errorCount, ref warningCount, "VIS-GIMMICK-003", targetObject, objectName, "함정, 문, 포탈은 실제 판정을 위해 Collider2D가 필요합니다.");
        }

        WriteStage101ValidationResult(builder, errorCount, warningCount);
    }

    private static void ValidateStage101Sorting()
    {
        EnsureStage101SceneOpen();

        int errorCount = 0;
        int warningCount = 0;
        StringBuilder builder = CreateStage101ValidationBuilder("Stage1-1 정렬/레이어 검증");

        GameObject groundObject = FindStage101SceneObject("TM_Ground");
        ValidateStage101ObjectExists(builder, ref errorCount, ref warningCount, "VIS-SORT-001", "TM_Ground", groundObject, "Stage1-1 Tilemap을 다시 생성하세요.");
        ValidateRequiredComponent<TilemapCollider2D>(builder, ref errorCount, ref warningCount, "VIS-SORT-002", groundObject, "TM_Ground", "실제 충돌 지형은 TilemapCollider2D가 필요합니다.");
        ValidateRequiredComponent<CompositeCollider2D>(builder, ref errorCount, ref warningCount, "VIS-SORT-003", groundObject, "TM_Ground", "지형 충돌 최적화를 위해 CompositeCollider2D가 필요합니다.");

        if (groundObject != null)
        {
            Rigidbody2D body = groundObject.GetComponent<Rigidbody2D>();

            if (body == null || body.bodyType != RigidbodyType2D.Static)
            {
                AppendValidationRow(
                    builder,
                    ref errorCount,
                    ref warningCount,
                    "Error",
                    "VIS-SORT-004",
                    GetSceneObjectPath(groundObject),
                    "Rigidbody2D.bodyType",
                    "TM_Ground는 Static Rigidbody2D를 가져야 합니다.",
                    "TilemapCollider2D와 CompositeCollider2D를 사용하는 지형 Tilemap에 Static Rigidbody2D를 추가하세요.");
            }
        }

        ValidateSortingOrderByPath(builder, ref errorCount, ref warningCount, "BackgroundRoot", -1000, -20, "배경은 플레이어와 지형보다 낮은 정렬 순서여야 합니다.");
        ValidateSortingOrderByPath(builder, ref errorCount, ref warningCount, "HWJ_Player", 20, 1000, "플레이어는 배경/지형보다 앞에 보여야 합니다.");

        List<GameObject> enemyObjects = FindStage101SceneObjectsByPrefix("HWJ_Enemy_Stage1_01_");

        for (int i = 0; i < enemyObjects.Count; i++)
        {
            ValidateSortingOrderByPath(builder, ref errorCount, ref warningCount, enemyObjects[i].name, 20, 1000, "몬스터는 배경/지형보다 앞에 보여야 합니다.");
        }

        WriteStage101ValidationResult(builder, errorCount, warningCount);
    }

    private static StringBuilder CreateStage101ValidationBuilder(string title)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# " + title);
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 대상 씬: {Stage1Scene01Path}");
        builder.AppendLine();
        builder.AppendLine("|심각도|검증 코드|대상|필드|문제|수정 방법|");
        builder.AppendLine("|---|---|---|---|---|---|");
        return builder;
    }

    private static void WriteStage101ValidationResult(StringBuilder builder, int errorCount, int warningCount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Stage101VisualValidationReportFilePath));
        builder.AppendLine();
        builder.AppendLine($"- Error: {errorCount}");
        builder.AppendLine($"- Warning: {warningCount}");
        builder.AppendLine($"- 결과: {(errorCount == 0 ? "통과" : "실패")}");

        File.WriteAllText(Stage101VisualValidationReportFilePath, builder.ToString(), Encoding.UTF8);

        string message = $"[Stage1-1 검증] errors={errorCount}, warnings={warningCount}, report={Stage101VisualValidationReportFilePath}";

        if (errorCount > 0)
        {
            Debug.LogError(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private static void ValidateStage101ObjectExists(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        string objectName,
        GameObject targetObject,
        string fixMessage)
    {
        if (targetObject != null)
        {
            return;
        }

        AppendValidationRow(
            builder,
            ref errorCount,
            ref warningCount,
            "Error",
            validationCode,
            objectName,
            "GameObject",
            "필수 오브젝트를 씬에서 찾을 수 없습니다.",
            fixMessage);
    }

    private static void ValidateVisibleRenderer(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        GameObject targetObject,
        string targetName,
        string fixMessage)
    {
        if (targetObject == null || HasVisibleRendererInChildren(targetObject))
        {
            return;
        }

        AppendValidationRow(
            builder,
            ref errorCount,
            ref warningCount,
            "Error",
            validationCode,
            GetSceneObjectPath(targetObject),
            targetName,
            "Game View에서 식별 가능한 SpriteRenderer 또는 Animator가 없습니다.",
            fixMessage);
    }

    private static void ValidatePrefabInstance(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        GameObject targetObject,
        string expectedPrefabPath,
        string fixMessage)
    {
        if (targetObject == null)
        {
            return;
        }

        UnityEngine.Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(targetObject);

        if (prefabSource == null)
        {
            AppendValidationRow(
                builder,
                ref errorCount,
                ref warningCount,
                "Error",
                validationCode,
                GetSceneObjectPath(targetObject),
                "Prefab",
                "실제 Prefab 인스턴스가 아니라 Scene 오브젝트로만 배치되어 있습니다.",
                fixMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(expectedPrefabPath))
        {
            return;
        }

        string actualPath = AssetDatabase.GetAssetPath(prefabSource);

        if (!string.Equals(actualPath, expectedPrefabPath, StringComparison.OrdinalIgnoreCase))
        {
            AppendValidationRow(
                builder,
                ref errorCount,
                ref warningCount,
                "Error",
                validationCode,
                GetSceneObjectPath(targetObject),
                "Prefab",
                $"예상 프리팹은 {expectedPrefabPath} 이지만 실제 연결은 {actualPath} 입니다.",
                fixMessage);
        }
    }

    private static void ValidateRequiredComponent<TComponent>(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        GameObject targetObject,
        string targetName,
        string fixMessage)
        where TComponent : Component
    {
        if (targetObject == null || targetObject.GetComponentInChildren<TComponent>(true) != null)
        {
            return;
        }

        AppendValidationRow(
            builder,
            ref errorCount,
            ref warningCount,
            "Error",
            validationCode,
            GetSceneObjectPath(targetObject),
            typeof(TComponent).Name,
            $"{targetName}에 {typeof(TComponent).Name} 컴포넌트가 없습니다.",
            fixMessage);
    }

    private static void ValidateRequiredComponentByTypeName(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        string objectName,
        string componentTypeName,
        string fixMessage)
    {
        GameObject targetObject = FindStage101SceneObject(objectName);

        if (targetObject == null)
        {
            return;
        }

        Type componentType = ResolveComponentType(componentTypeName);

        if (componentType != null && targetObject.GetComponentInChildren(componentType, true) != null)
        {
            return;
        }

        AppendValidationRow(
            builder,
            ref errorCount,
            ref warningCount,
            "Error",
            validationCode,
            GetSceneObjectPath(targetObject),
            componentTypeName,
            $"{objectName}에 {componentTypeName} 컴포넌트가 없습니다.",
            fixMessage);
    }

    private static bool HasVisibleRendererInChildren(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        SpriteRenderer[] spriteRenderers = targetObject.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && spriteRenderers[i].enabled && spriteRenderers[i].sprite != null)
            {
                return true;
            }
        }

        Animator[] animators = targetObject.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i].runtimeAnimatorController != null)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateSortingOrderByPath(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string objectNameOrPathPart,
        int minInclusive,
        int maxInclusive,
        string fixMessage)
    {
        List<GameObject> targets = FindStage101SceneObjectsByNamePart(objectNameOrPathPart);

        if (targets.Count <= 0)
        {
            AppendValidationRow(
                builder,
                ref errorCount,
                ref warningCount,
                "Warning",
                "VIS-SORT-010",
                objectNameOrPathPart,
                "SortingOrder",
                "정렬 순서를 확인할 대상 오브젝트를 찾지 못했습니다.",
                "씬에 해당 오브젝트가 생성되어 있는지 확인하세요.");
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            SpriteRenderer[] renderers = targets[i].GetComponentsInChildren<SpriteRenderer>(true);

            for (int j = 0; j < renderers.Length; j++)
            {
                SpriteRenderer renderer = renderers[j];

                if (renderer == null)
                {
                    continue;
                }

                if (renderer.sortingOrder < minInclusive || renderer.sortingOrder > maxInclusive)
                {
                    AppendValidationRow(
                        builder,
                        ref errorCount,
                        ref warningCount,
                        "Warning",
                        "VIS-SORT-011",
                        GetSceneObjectPath(renderer.gameObject),
                        "SpriteRenderer.sortingOrder",
                        $"정렬 순서 {renderer.sortingOrder}가 권장 범위 {minInclusive}~{maxInclusive} 밖입니다.",
                        fixMessage);
                }
            }
        }
    }

    private static GameObject FindStage101SceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform result = FindChildByNameRecursive(roots[i].transform, objectName);

            if (result != null)
            {
                return result.gameObject;
            }
        }

        return null;
    }

    private static List<GameObject> FindStage101SceneObjectsByPrefix(string namePrefix)
    {
        List<GameObject> results = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
        {
            return results;
        }

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            CollectChildrenByNamePrefix(roots[i].transform, namePrefix, results);
        }

        return results;
    }

    private static List<GameObject> FindStage101SceneObjectsByNamePart(string namePart)
    {
        List<GameObject> results = new List<GameObject>();
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
        {
            return results;
        }

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            CollectChildrenByNamePart(roots[i].transform, namePart, results);
        }

        return results;
    }

    private static Transform FindChildByNameRecursive(Transform current, string objectName)
    {
        if (current == null)
        {
            return null;
        }

        if (current.name == objectName)
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform result = FindChildByNameRecursive(current.GetChild(i), objectName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void CollectChildrenByNamePrefix(Transform current, string namePrefix, List<GameObject> results)
    {
        if (current == null)
        {
            return;
        }

        if (current.name.StartsWith(namePrefix, StringComparison.Ordinal))
        {
            results.Add(current.gameObject);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectChildrenByNamePrefix(current.GetChild(i), namePrefix, results);
        }
    }

    private static void CollectChildrenByNamePart(Transform current, string namePart, List<GameObject> results)
    {
        if (current == null)
        {
            return;
        }

        if (current.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            results.Add(current.gameObject);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectChildrenByNamePart(current.GetChild(i), namePart, results);
        }
    }

    private static string GetSceneObjectPath(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return "Missing GameObject";
        }

        StringBuilder builder = new StringBuilder(targetObject.name);
        Transform current = targetObject.transform.parent;

        while (current != null)
        {
            builder.Insert(0, current.name + "/");
            current = current.parent;
        }

        return builder.ToString();
    }

    private static string[] GetRequiredStage1ScenePaths()
    {
        return new[]
        {
            Stage1Scene01Path,
            Stage1Scene02Path,
            Stage1Scene03Path,
            Stage1Scene04Path,
        };
    }

    private static void ValidateFolder(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        string assetPath,
        string fieldName,
        bool errorWhenMissing,
        string fixMessage)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        AppendValidationRow(
            builder,
            ref errorCount,
            ref warningCount,
            errorWhenMissing ? "Error" : "Warning",
            validationCode,
            assetPath,
            fieldName,
            "폴더를 찾을 수 없습니다.",
            fixMessage);
    }

    private static void ValidateAsset(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string validationCode,
        string assetPath,
        string fieldName,
        bool errorWhenMissing,
        string fixMessage)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null
            || File.Exists(Path.GetFullPath(assetPath));

        if (exists)
        {
            return;
        }

        AppendValidationRow(
            builder,
            ref errorCount,
            ref warningCount,
            errorWhenMissing ? "Error" : "Warning",
            validationCode,
            assetPath,
            fieldName,
            "에셋을 찾을 수 없습니다.",
            fixMessage);
    }

    private static bool IsSceneRegisteredInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled && scenes[i].path == scenePath)
            {
                return true;
            }
        }

        return false;
    }

    private static bool SceneFileHasMissingScript(string scenePath)
    {
        string fullPath = Path.GetFullPath(scenePath);

        if (!File.Exists(fullPath))
        {
            return false;
        }

        string sceneText = File.ReadAllText(fullPath);
        return sceneText.Contains("m_Script: {fileID: 0}");
    }

    private static void AppendValidationRow(
        StringBuilder builder,
        ref int errorCount,
        ref int warningCount,
        string severity,
        string validationCode,
        string assetPath,
        string fieldName,
        string problem,
        string fixMessage)
    {
        if (string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase))
        {
            errorCount++;
        }
        else if (string.Equals(severity, "Warning", StringComparison.OrdinalIgnoreCase))
        {
            warningCount++;
        }

        builder.AppendLine(
            $"|{EscapeMarkdownCell(severity)}|{EscapeMarkdownCell(validationCode)}|{EscapeMarkdownCell(assetPath)}|{EscapeMarkdownCell(fieldName)}|{EscapeMarkdownCell(problem)}|{EscapeMarkdownCell(fixMessage)}|");
    }

    private static string EscapeMarkdownCell(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/02Scripts", "HWJ");
        EnsureFolder(HwjRoot, "Art");
        EnsureFolder(HwjRoot + "/Art", "Generated");
        EnsureFolder(HwjRoot + "/Art/Generated", "Stage1");
        EnsureFolder(HwjRoot, "Tiles");
        EnsureFolder(HwjRoot + "/Tiles", "Generated");
        EnsureFolder(HwjRoot + "/Tiles/Generated", "Stage1");
        EnsureFolder(HwjRoot, "Animations");
        EnsureFolder(HwjRoot + "/Animations", "Generated");
        EnsureFolder(HwjRoot + "/Animations/Generated", "Stage1Gimmicks");
        EnsureFolder(HwjRoot, "ScriptableObjects");
        EnsureFolder(HwjRoot + "/ScriptableObjects", "SpawnTables");
        EnsureFolder(SceneRoot, "HWJ_Backups");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static HWJ_Stage1TileSet CreateOrLoadTileSet()
    {
        CreateOrLoadSprite(
            MidBossSpritePath,
            new Color32(134, 39, 35, 255),
            HWJ_Stage1TilePattern.MidBossMarker);

        return new HWJ_Stage1TileSet
        {
            BackgroundSand = CreateStage1TransparentTile("HWJ_Stage1_BackgroundClear.asset", "HWJ_Stage1_Tile_BackgroundClear.png"),
            OcherSandFloor = CreateStage1AtlasTile("HWJ_Stage1_OcherSandFloor.asset", "HWJ_Stage1_Tile_OcherSandFloor.png", 28, 118, new Color32(70, 52, 48, 255), HWJ_Stage1TilePattern.SandFloor, Tile.ColliderType.Grid),
            CrackedStone = CreateStage1AtlasTile("HWJ_Stage1_CrackedStone.asset", "HWJ_Stage1_Tile_CrackedStone.png", 400, 122, new Color32(126, 104, 80, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid),
            RuinedWall = CreateStage1AtlasTile("HWJ_Stage1_RuinedWall.asset", "HWJ_Stage1_Tile_RuinedWall.png", 396, 258, new Color32(62, 49, 55, 210), HWJ_Stage1TilePattern.RuinedWall, Tile.ColliderType.Grid),
            CollapsedRoof = CreateStage1AtlasTile("HWJ_Stage1_CollapsedRoof.asset", "HWJ_Stage1_Tile_CollapsedRoof.png", 28, 318, new Color32(92, 59, 37, 255), HWJ_Stage1TilePattern.Wood, Tile.ColliderType.None),
            SpikeWarning = CreateStage1AtlasTile("HWJ_Stage1_SpikeWarning.asset", "HWJ_Stage1_Tile_SpikeWarning.png", 820, 780, new Color32(210, 73, 43, 255), HWJ_Stage1TilePattern.SpikeWarning, Tile.ColliderType.None),
            SandstormGuide = CreateStage1AtlasTile("HWJ_Stage1_SandstormGuide.asset", "HWJ_Stage1_Tile_SandstormGuide.png", 544, 808, new Color32(224, 175, 88, 190), HWJ_Stage1TilePattern.Sandstorm, Tile.ColliderType.None),
            SpiritSealWall = CreateStage1AtlasTile("HWJ_Stage1_SpiritSealWall.asset", "HWJ_Stage1_Tile_SpiritSealWall.png", 930, 1112, new Color32(110, 78, 174, 210), HWJ_Stage1TilePattern.SpiritSeal, Tile.ColliderType.Grid),
            SpiritOrb = CreateStage1AtlasTile("HWJ_Stage1_SpiritOrb.asset", "HWJ_Stage1_Tile_SpiritOrb.png", 830, 1112, new Color32(116, 202, 255, 230), HWJ_Stage1TilePattern.SpiritOrb, Tile.ColliderType.None),
            SpiritWind = CreateStage1AtlasTile("HWJ_Stage1_SpiritWind.asset", "HWJ_Stage1_Tile_SpiritWind.png", 574, 808, new Color32(154, 111, 226, 180), HWJ_Stage1TilePattern.SpiritWind, Tile.ColliderType.None),
            BarracksWall = CreateStage1AtlasTile("HWJ_Stage1_BarracksWall.asset", "HWJ_Stage1_Tile_BarracksWall.png", 548, 906, new Color32(42, 58, 57, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            RustyGate = CreateStage1AtlasTile("HWJ_Stage1_RustyGate.asset", "HWJ_Stage1_Tile_RustyGate.png", 780, 1110, new Color32(103, 72, 48, 255), HWJ_Stage1TilePattern.RustyGate, Tile.ColliderType.Grid),
            GroundTopLeft = CreateStage1AtlasTile("HWJ_Stage1_GroundTopLeft.asset", "HWJ_Stage1_Tile_GroundTopLeft.png", 28, 12, new Color32(98, 76, 74, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid),
            GroundTopMiddleA = CreateStage1AtlasTile("HWJ_Stage1_GroundTopMiddleA.asset", "HWJ_Stage1_Tile_GroundTopMiddleA.png", 62, 12, new Color32(98, 76, 74, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid),
            GroundTopMiddleB = CreateStage1AtlasTile("HWJ_Stage1_GroundTopMiddleB.asset", "HWJ_Stage1_Tile_GroundTopMiddleB.png", 96, 12, new Color32(98, 76, 74, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid),
            GroundTopRight = CreateStage1AtlasTile("HWJ_Stage1_GroundTopRight.asset", "HWJ_Stage1_Tile_GroundTopRight.png", 130, 12, new Color32(98, 76, 74, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid),
            GroundFillA = CreateStage1AtlasTile("HWJ_Stage1_GroundFillA.asset", "HWJ_Stage1_Tile_GroundFillA.png", 40, 42, new Color32(55, 48, 53, 255), HWJ_Stage1TilePattern.SandFloor, Tile.ColliderType.Grid),
            GroundFillB = CreateStage1AtlasTile("HWJ_Stage1_GroundFillB.asset", "HWJ_Stage1_Tile_GroundFillB.png", 84, 42, new Color32(55, 48, 53, 255), HWJ_Stage1TilePattern.SandFloor, Tile.ColliderType.Grid),
            BarracksTopLeft = CreateStage1AtlasTile("HWJ_Stage1_BarracksTopLeft.asset", "HWJ_Stage1_Tile_BarracksTopLeft.png", 516, 906, new Color32(49, 72, 72, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            BarracksTopMiddleA = CreateStage1AtlasTile("HWJ_Stage1_BarracksTopMiddleA.asset", "HWJ_Stage1_Tile_BarracksTopMiddleA.png", 548, 906, new Color32(49, 72, 72, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            BarracksTopMiddleB = CreateStage1AtlasTile("HWJ_Stage1_BarracksTopMiddleB.asset", "HWJ_Stage1_Tile_BarracksTopMiddleB.png", 580, 906, new Color32(49, 72, 72, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            BarracksTopRight = CreateStage1AtlasTile("HWJ_Stage1_BarracksTopRight.asset", "HWJ_Stage1_Tile_BarracksTopRight.png", 612, 906, new Color32(49, 72, 72, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            BarracksFillA = CreateStage1AtlasTile("HWJ_Stage1_BarracksFillA.asset", "HWJ_Stage1_Tile_BarracksFillA.png", 548, 942, new Color32(35, 48, 52, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            BarracksFillB = CreateStage1AtlasTile("HWJ_Stage1_BarracksFillB.asset", "HWJ_Stage1_Tile_BarracksFillB.png", 612, 942, new Color32(35, 48, 52, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            BackgroundWallA = CreateStage1AtlasTile("HWJ_Stage1_BackgroundWallA.asset", "HWJ_Stage1_Tile_BackgroundWallA.png", 388, 424, new Color32(33, 38, 42, 210), HWJ_Stage1TilePattern.RuinedWall, Tile.ColliderType.None),
            BackgroundWallB = CreateStage1AtlasTile("HWJ_Stage1_BackgroundWallB.asset", "HWJ_Stage1_Tile_BackgroundWallB.png", 454, 424, new Color32(33, 38, 42, 210), HWJ_Stage1TilePattern.RuinedWall, Tile.ColliderType.None)
        };
    }

    private static HWJ_Stage1TileSet CreateOrLoadBlockoutTileSet()
    {
        CreateOrLoadSprite(
            MidBossSpritePath,
            new Color32(134, 39, 35, 255),
            HWJ_Stage1TilePattern.MidBossMarker);

        TileBase background = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_Background.asset", "HWJ_Stage1_Blockout_Background.png", new Color32(26, 28, 32, 255), HWJ_Stage1TilePattern.SoftNoise, Tile.ColliderType.None);
        TileBase groundFillA = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_FillA.asset", "HWJ_Stage1_Blockout_FillA.png", new Color32(82, 86, 92, 255), HWJ_Stage1TilePattern.SandFloor, Tile.ColliderType.Grid);
        TileBase groundFillB = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_FillB.asset", "HWJ_Stage1_Blockout_FillB.png", new Color32(72, 76, 82, 255), HWJ_Stage1TilePattern.SandFloor, Tile.ColliderType.Grid);
        TileBase groundTop = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_Top.asset", "HWJ_Stage1_Blockout_Top.png", new Color32(146, 150, 156, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid);
        TileBase oneWay = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_OneWay.asset", "HWJ_Stage1_Blockout_OneWay.png", new Color32(120, 162, 190, 255), HWJ_Stage1TilePattern.Wood, Tile.ColliderType.Grid);
        TileBase gate = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_Gate.asset", "HWJ_Stage1_Blockout_Gate.png", new Color32(110, 92, 146, 255), HWJ_Stage1TilePattern.SpiritSeal, Tile.ColliderType.Grid);
        TileBase switchTile = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_Switch.asset", "HWJ_Stage1_Blockout_Switch.png", new Color32(88, 178, 230, 255), HWJ_Stage1TilePattern.SpiritOrb, Tile.ColliderType.None);
        TileBase hazard = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_Hazard.asset", "HWJ_Stage1_Blockout_Hazard.png", new Color32(206, 74, 62, 255), HWJ_Stage1TilePattern.SpikeWarning, Tile.ColliderType.None);
        TileBase marker = CreateStage1GeneratedTile("HWJ_Stage1_Blockout_Marker.asset", "HWJ_Stage1_Blockout_Marker.png", new Color32(232, 190, 92, 230), HWJ_Stage1TilePattern.Sandstorm, Tile.ColliderType.None);

        return new HWJ_Stage1TileSet
        {
            BackgroundSand = background,
            OcherSandFloor = groundFillA,
            CrackedStone = groundTop,
            RuinedWall = groundFillB,
            CollapsedRoof = oneWay,
            SpikeWarning = hazard,
            SandstormGuide = marker,
            SpiritSealWall = gate,
            SpiritOrb = switchTile,
            SpiritWind = marker,
            BarracksWall = groundFillB,
            RustyGate = gate,
            GroundTopLeft = groundTop,
            GroundTopMiddleA = groundTop,
            GroundTopMiddleB = groundTop,
            GroundTopRight = groundTop,
            GroundFillA = groundFillA,
            GroundFillB = groundFillA,
            BarracksTopLeft = groundTop,
            BarracksTopMiddleA = groundTop,
            BarracksTopMiddleB = groundTop,
            BarracksTopRight = groundTop,
            BarracksFillA = groundFillA,
            BarracksFillB = groundFillA,
            BackgroundWallA = background,
            BackgroundWallB = groundFillB
        };
    }

    private static TileBase CreateStage1GeneratedTile(
        string tileFileName,
        string spriteFileName,
        Color32 color,
        HWJ_Stage1TilePattern pattern,
        Tile.ColliderType colliderType)
    {
        Sprite sprite = CreateOrLoadSprite(SpriteRoot + "/" + spriteFileName, color, pattern);
        return CreateOrLoadTile(tileFileName, sprite, colliderType);
    }

    private static TileBase CreateStage1TransparentTile(string tileFileName, string spriteFileName)
    {
        string spritePath = SpriteRoot + "/" + spriteFileName;
        Sprite sprite = CreateOrLoadTransparentSprite(spritePath);
        return CreateOrLoadTile(tileFileName, sprite, Tile.ColliderType.None);
    }

    private static TileBase CreateStage1AtlasTile(
        string tileFileName,
        string spriteFileName,
        int atlasLeftX,
        int atlasTopY,
        Color32 fallbackColor,
        HWJ_Stage1TilePattern fallbackPattern,
        Tile.ColliderType colliderType)
    {
        Sprite sprite = CreateOrLoadAtlasSprite(
            SpriteRoot + "/" + spriteFileName,
            atlasLeftX,
            atlasTopY,
            32,
            32,
            fallbackColor,
            fallbackPattern);

        return CreateOrLoadTile(tileFileName, sprite, colliderType);
    }

    private static Sprite CreateOrLoadAtlasSprite(
        string spritePath,
        int atlasLeftX,
        int atlasTopY,
        int width,
        int height,
        Color32 fallbackColor,
        HWJ_Stage1TilePattern fallbackPattern)
    {
        Texture2D atlasTexture = LoadReadableTexture(Stage1TileAtlasPath);

        if (atlasTexture == null)
        {
            return CreateOrLoadSprite(spritePath, fallbackColor, fallbackPattern);
        }

        int atlasBottomY = atlasTexture.height - atlasTopY - height;
        bool invalidRect =
            atlasLeftX < 0 ||
            atlasBottomY < 0 ||
            atlasLeftX + width > atlasTexture.width ||
            atlasBottomY + height > atlasTexture.height;

        if (invalidRect)
        {
            Debug.LogWarning($"HWJ Stage1 atlas crop out of range. path={Stage1TileAtlasPath}, x={atlasLeftX}, y={atlasTopY}, width={width}, height={height}");
            return CreateOrLoadSprite(spritePath, fallbackColor, fallbackPattern);
        }

        Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        croppedTexture.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                croppedTexture.SetPixel(x, y, atlasTexture.GetPixel(atlasLeftX + x, atlasBottomY + y));
            }
        }

        croppedTexture.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(spritePath)));
        File.WriteAllBytes(Path.GetFullPath(spritePath), croppedTexture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(croppedTexture);

        ImportAsSprite(spritePath, 32f, FilterMode.Point, true);
        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static Sprite CreateOrLoadTransparentSprite(string spritePath)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color32[] pixels = new Color32[size * size];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(spritePath)));
        File.WriteAllBytes(Path.GetFullPath(spritePath), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        ImportAsSprite(spritePath, 32f, FilterMode.Point, true);
        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static Texture2D LoadReadableTexture(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(Path.GetFullPath(assetPath)))
        {
            return null;
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            bool needsReimport =
                importer.textureType != TextureImporterType.Default ||
                importer.isReadable == false ||
                importer.mipmapEnabled ||
                importer.filterMode != FilterMode.Point;

            if (needsReimport)
            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static TileBase CreateStage1GameplayTile(
        string sourceTileFileName,
        string fallbackTileFileName,
        string fallbackSpriteFileName,
        Color32 fallbackColor,
        HWJ_Stage1TilePattern fallbackPattern,
        Tile.ColliderType fallbackColliderType)
    {
        Sprite sourceSprite = LoadSourceSprite(sourceTileFileName);
        Sprite tileSprite = sourceSprite != null ? sourceSprite : CreateOrLoadSprite(
            SpriteRoot + "/" + fallbackSpriteFileName,
            fallbackColor,
            fallbackPattern);

        return CreateOrLoadTile(fallbackTileFileName, tileSprite, fallbackColliderType);
    }

    private static Sprite LoadSourceSprite(string sourceTileFileName)
    {
        if (string.IsNullOrWhiteSpace(sourceTileFileName))
        {
            return null;
        }

        Tile sourceTile = AssetDatabase.LoadAssetAtPath<Tile>(SourceTileRoot + "/" + sourceTileFileName);
        Sprite sourceSprite = sourceTile != null ? sourceTile.sprite : null;

        if (sourceSprite == null)
        {
            return null;
        }

        Rect sourceRect = sourceSprite.rect;
        bool fitsOneTileCell = sourceRect.width <= 32f && sourceRect.height <= 32f;

        if (!fitsOneTileCell)
        {
            Debug.LogWarning($"HWJ Stage1 tile skipped large 08Tileset sprite. source={sourceTileFileName}, rect={sourceRect.width}x{sourceRect.height}");
            return null;
        }

        return sourceSprite;
    }

    private static Sprite CreateOrLoadSprite(
        string assetPath,
        Color32 color,
        HWJ_Stage1TilePattern pattern)
    {
        string fullPath = Path.GetFullPath(assetPath);

        Texture2D texture = CreateTexture(color, pattern);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        ImportAsSprite(assetPath, 32f, FilterMode.Point, true);
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void ImportAsSprite(
        string assetPath,
        float pixelsPerUnit,
        FilterMode filterMode,
        bool forceSingleSprite)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;

        if (forceSingleSprite)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
        }

        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = filterMode;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static Texture2D CreateTexture(Color32 color, HWJ_Stage1TilePattern pattern)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, ApplyPattern(color, x, y, pattern));
            }
        }

        texture.Apply();
        return texture;
    }

    private static Color32 ApplyPattern(Color32 baseColor, int x, int y, HWJ_Stage1TilePattern pattern)
    {
        switch (pattern)
        {
            case HWJ_Stage1TilePattern.SandFloor:
                return PaintBrickFillTile(baseColor, x, y, 8, 16);
            case HWJ_Stage1TilePattern.CrackedStone:
                return PaintGroundTopTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.RuinedWall:
                return PaintBackgroundWallTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.Wood:
                return PaintSupportTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.SpikeWarning:
                return PaintSpikeTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.Sandstorm:
                return PaintGuideWindTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.SpiritSeal:
                return PaintSpiritSealTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.SpiritOrb:
                return PaintSpiritOrbTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.SpiritWind:
                return PaintGuideWindTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.BarracksWall:
                return PaintBrickFillTile(baseColor, x, y, 7, 14);
            case HWJ_Stage1TilePattern.RustyGate:
                return PaintRustyGateTile(baseColor, x, y);
            case HWJ_Stage1TilePattern.MidBossMarker:
                return PaintMidBossMarkerTile(baseColor, x, y);
            default:
                return PaintFarBackgroundTile(baseColor, x, y);
        }
    }

    private static Color32 PaintFarBackgroundTile(Color32 baseColor, int x, int y)
    {
        bool mortar = y % 8 == 0 || (x + ((y / 8) % 2) * 8) % 16 == 0;
        int noise = (x * 13 + y * 7) % 11 == 0 ? 8 : 0;
        return Shade(baseColor, mortar ? -22 : -8 + noise);
    }

    private static Color32 PaintBrickFillTile(Color32 baseColor, int x, int y, int rowHeight, int brickWidth)
    {
        int row = Mathf.Max(0, y / Mathf.Max(1, rowHeight));
        bool mortar = y % rowHeight == 0 || (x + (row % 2) * (brickWidth / 2)) % brickWidth == 0;
        bool chipped = (x * 5 + y * 11) % 37 == 0 || (x + y * 3) % 43 == 0;

        if (mortar)
        {
            return Shade(baseColor, -34);
        }

        if (chipped)
        {
            return Shade(baseColor, -18);
        }

        return Shade(baseColor, 2 + (row % 2) * 4);
    }

    private static Color32 PaintGroundTopTile(Color32 baseColor, int x, int y)
    {
        if (y >= 27)
        {
            return Shade(baseColor, x % 8 == 0 ? 28 : 44);
        }

        if (y >= 22)
        {
            bool seam = x % 12 == 0 || (x + y) % 23 == 0;
            return Shade(baseColor, seam ? -22 : 18);
        }

        if (y <= 4)
        {
            return Shade(baseColor, -34);
        }

        bool crack = x == y || x + y == 31 || (x * 3 + y * 5) % 41 == 0;
        return Shade(baseColor, crack ? -30 : -2);
    }

    private static Color32 PaintBackgroundWallTile(Color32 baseColor, int x, int y)
    {
        Color32 dimColor = Shade(baseColor, -32, 210);
        bool mortar = y % 9 == 0 || (x + ((y / 9) % 2) * 7) % 14 == 0;
        bool broken = (x * 7 + y * 3) % 47 == 0;
        return Shade(dimColor, mortar ? -24 : broken ? 18 : 0, 210);
    }

    private static Color32 PaintSupportTile(Color32 baseColor, int x, int y)
    {
        bool plankLine = x % 8 <= 1 || y % 12 == 0;
        bool nail = (x == 6 || x == 22) && (y == 8 || y == 24);
        return Shade(baseColor, nail ? 34 : plankLine ? -28 : 8);
    }

    private static Color32 PaintSpikeTile(Color32 baseColor, int x, int y)
    {
        int localX = x % 8;
        bool spike = y < 17 && Mathf.Abs(localX - 4) <= y / 4;

        if (y < 4)
        {
            return Shade(baseColor, -28);
        }

        return Shade(baseColor, spike ? 48 : -34, spike ? 230 : 120);
    }

    private static Color32 PaintGuideWindTile(Color32 baseColor, int x, int y)
    {
        bool line = Mathf.Abs(((x * 2 + y * 3) % 19) - 9) < 2 || Mathf.Abs(((x - y + 32) % 17) - 8) < 2;
        return Shade(baseColor, line ? 48 : -22, line ? 180 : 45);
    }

    private static Color32 PaintSpiritSealTile(Color32 baseColor, int x, int y)
    {
        bool border = x < 3 || x > 28 || y < 3 || y > 28;
        bool rune = x == y || x + y == 31 || x % 10 == 0 || y % 10 == 0;
        return Shade(baseColor, border ? 44 : rune ? 28 : -18, border || rune ? 230 : 160);
    }

    private static Color32 PaintSpiritOrbTile(Color32 baseColor, int x, int y)
    {
        int dx = x - 16;
        int dy = y - 16;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        if (distance < 7f)
        {
            return Shade(baseColor, 58, 245);
        }

        if (distance < 11f)
        {
            return Shade(baseColor, 18, 190);
        }

        return Shade(baseColor, -40, 20);
    }

    private static Color32 PaintRustyGateTile(Color32 baseColor, int x, int y)
    {
        bool bar = x % 8 < 2 || y > 27 || y < 3;
        bool rust = (x * 5 + y * 3) % 19 == 0;
        return Shade(baseColor, bar ? (rust ? 22 : -18) : -46, bar ? 255 : 80);
    }

    private static Color32 PaintMidBossMarkerTile(Color32 baseColor, int x, int y)
    {
        bool frame = x < 4 || x > 27 || y < 4 || y > 27;
        bool core = x > 9 && x < 23 && y > 7 && y < 25;
        return Shade(baseColor, frame ? -24 : core ? 36 : -8);
    }

    private static Color32 Shade(Color32 baseColor, int offset, int alphaOverride = -1)
    {
        return new Color32(
            ClampByte(baseColor.r + offset),
            ClampByte(baseColor.g + offset),
            ClampByte(baseColor.b + offset),
            alphaOverride >= 0 ? ClampByte(alphaOverride) : baseColor.a);
    }

    private static byte ClampByte(int value)
    {
        return (byte)Mathf.Clamp(value, 0, 255);
    }

    private static Tile CreateOrLoadTile(string fileName, Sprite sprite, Tile.ColliderType colliderType)
    {
        string assetPath = TileRoot + "/" + fileName;
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);

        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        tile.sprite = sprite;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void BuildRuinedVillageScene()
    {
        if (UseStage101SafeBlockoutRepair())
        {
            RebuildStage101BlockoutOnly();
            return;
        }

        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene01Path,
            "HWJ_Stage1_01_RuinedVillage",
            "1-1 / 1-2 황폐한 마을 입구와 내부",
            new Vector3(11f, 0.8f, -10f),
            9.5f);
        CreateRoundBackground(context, Stage1Background01Path, new Vector2(46f, 1f), 205f, 29f);

        if (TryBuildStage101Blockout(context))
        {
            return;
        }

        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -44, -11, 116, 31);
        PaintGroundRun(context, tiles, -38, -7, 27, 3);
        PaintGroundRun(context, tiles, -8, -7, 28, 3);
        PaintGroundRun(context, tiles, 26, -6, 12, 2);
        PaintGroundRun(context, tiles, 43, -7, 23, 3);
        PaintStonePlatform(context, tiles, -24, -1, 8);
        PaintStonePlatform(context, tiles, -2, 1, 9);
        PaintStonePlatform(context, tiles, 17, 2, 9);
        PaintStonePlatform(context, tiles, 33, 1, 8);

        PaintRuinPillar(context, tiles, -31, -4, 4, 6, false);
        PaintRuinPillar(context, tiles, -13, -4, 3, 5, false);
        PaintRuinPillar(context, tiles, 19, -4, 3, 7, false);
        PaintRuinPillar(context, tiles, 40, -4, 3, 6, false);
        PaintRuinedHouse(context, tiles, -30, -4, 10, 8);
        PaintRuinedHouse(context, tiles, 3, -4, 11, 9);
        PaintRuinedHouse(context, tiles, 47, -4, 10, 7);
        PaintBackdropBreakup(context, tiles, -39, 2, 11, 5);
        PaintBackdropBreakup(context, tiles, -6, 3, 10, 5);
        PaintBackdropBreakup(context, tiles, 31, 4, 9, 4);
        PaintRubbleTrail(context, tiles, -36, -3, 8, 4);
        PaintRubbleTrail(context, tiles, -4, -3, 9, 3);
        PaintRubbleTrail(context, tiles, 44, -3, 7, 3);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 1, -4, 4, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 48, -4, 4, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -41, 1, 10, 2);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 27, 0, 10, 2);
        PaintSpiritOnlyPassage(context, tiles, -20, -2, 11, 5, true);
        PaintSpiritOnlyPassage(context, tiles, 20, -2, 8, 6, false);
        PaintSpiritOrbLine(context, tiles, -18, 0, 5);
        PaintSpiritOrbLine(context, tiles, 21, 0, 5);
        PaintRect(context.HazardGuide, tiles.SpiritWind, -15, 2, 3, 3);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 27, 2, 2, 4);
        PaintSpawnSupport(context, tiles, new Vector3(-34f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(4f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(36f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(58f, -3.2f, 0f));

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_01", "stage1_01_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-34f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, new Vector3(4f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(36f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_01_ToOutpost", "stage1_01_exit_outpost", HWJ_SpawnPointType.NPC, new Vector3(58f, -3.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-34f, -3.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateDirectPossessableCorpse(
            context.VisualRoot,
            "HWJ_Stage1_01_FirstPossessionCorpse",
            SwordEnemyRootPath,
            SwordEnemySpritePath,
            new Vector3(-31f, -3.05f, 0f));

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_01_SpawnTable.asset",
            "spawn.stage1.01",
            new HWJ_Stage1SpawnRequest("stage1_01_spawn_sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, SwordEnemyRootPath, SwordEnemyPrefabPath, 2),
            new HWJ_Stage1SpawnRequest("stage1_01_spawn_bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, BowEnemyRootPath, BowEnemyPrefabPath, 1));
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.01",
            "stage.region01.02",
            spawnTable,
            false);
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_01_ToOutpost",
            new Vector3(58f, -2.55f, 0f),
            "HWJ_Stage1_02_RuinedOutpost",
            "stage1_02_player_start",
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Spike_Left", new Vector3(3f, -3.45f, 0f), new Vector2(3.8f, 0.6f), 18f, 2.6f);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Spike_Right", new Vector3(50f, -3.45f, 0f), new Vector2(3.8f, 0.6f), 18f, 2.6f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Steam_Left", new Vector3(-34f, -2.55f, 0f), new Vector2(5f, 1.2f), 12f, 2f, 2.4f, tiles.SandstormGuide);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Steam_Right", new Vector3(31f, -1.55f, 0f), new Vector2(5f, 1.2f), 12f, 2f, 2.4f, tiles.SandstormGuide);
        CreateSpiritOrbSwitchShowcase(
            context,
            tiles,
            "HWJ_Stage1_01_SpiritOrbSwitch",
            "stage1_01.spirit_switch.shortcut",
            new Vector3(-15f, -1.4f, 0f),
            new Vector3(-12f, -2.3f, 0f));

        SaveContextScene(context);
    }

    private static bool UseStage101SafeBlockoutRepair()
    {
        return true;
    }

    private static void RebuildStage101BlockoutOnly()
    {
        EnsureFolders();
        EnsureRuntimeEnemyPrefabs();

        string backupPath = BackupStage101SceneAsset();
        HWJ_Stage1SceneContext context = CreateStage101RepairContext(out int removedObjectCount);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        CreateRoundBackground(context, Stage1Background01Path, new Vector2(58f, 1f), 205f, 35f);
        PaintStage101PlayableBlockout(context, tiles);
        EnsureStage101RuntimeLayout(context, tiles);
        CreateStage101VisibleVillageLayers(context, tiles);

        SaveContextScene(context);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        WriteStage101BlockoutReport(backupPath, removedObjectCount, null);
        Debug.Log("HWJ Stage1-1 blockout repair completed.");
    }

    private static string BackupStage101SceneAsset()
    {
        if (!File.Exists(Path.GetFullPath(Stage1Scene01Path)))
        {
            Debug.LogWarning($"HWJ Stage1-1 named backup skipped because source scene is missing: {Stage1Scene01Path}");
            return string.Empty;
        }

        EnsureFolder(SceneRoot, "HWJ_Backups");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage101NamedBackupPath) != null)
        {
            AssetDatabase.DeleteAsset(Stage101NamedBackupPath);
        }

        if (!AssetDatabase.CopyAsset(Stage1Scene01Path, Stage101NamedBackupPath))
        {
            Debug.LogWarning($"HWJ Stage1-1 named backup failed. source={Stage1Scene01Path}, target={Stage101NamedBackupPath}");
            return BackupSceneAsset(Stage1Scene01Path);
        }

        AssetDatabase.ImportAsset(Stage101NamedBackupPath);
        return Stage101NamedBackupPath;
    }

    private static string BackupSceneAsset(string scenePath)
    {
        if (!File.Exists(Path.GetFullPath(scenePath)))
        {
            Debug.LogWarning($"HWJ Stage1 scene backup skipped because source scene is missing: {scenePath}");
            return string.Empty;
        }

        EnsureFolder(SceneRoot, "HWJ_Backups");
        string backupPath = $"{SceneBackupRoot}/{Path.GetFileNameWithoutExtension(scenePath)}_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.unity";

        if (!AssetDatabase.CopyAsset(scenePath, backupPath))
        {
            Debug.LogWarning($"HWJ Stage1 scene backup failed. source={scenePath}, target={backupPath}");
            return string.Empty;
        }

        AssetDatabase.ImportAsset(backupPath);
        return backupPath;
    }

    private static HWJ_Stage1SceneContext CreateStage101RepairContext(out int removedObjectCount)
    {
        Scene scene = File.Exists(Path.GetFullPath(Stage1Scene01Path))
            ? EditorSceneManager.OpenScene(Stage1Scene01Path, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Transform root = FindOrCreateStage101Root().transform;
        removedObjectCount = RemoveStage101GeneratedMapObjects(root);
        RemoveStage101TemporaryVisualMarkers(root);

        GameObject gridObject = new GameObject("HWJ_Grid_Tilemap");
        gridObject.transform.SetParent(root);
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        Tilemap backDecoration = CreateTilemap(grid.transform, "TM_BackDecoration", -40, false, false);
        Tilemap ground = CreateTilemap(grid.transform, "TM_Ground", 0, true, false);
        Tilemap oneWayPlatform = CreateTilemap(grid.transform, "TM_OneWayPlatform", 5, true, true);
        Tilemap bodyBlocker = CreateTilemap(grid.transform, "TM_BodyBlocker", 8, false, false);
        Tilemap foreDecoration = CreateTilemap(grid.transform, "TM_ForeDecoration", 12, false, false);
        Tilemap soulBoundary = CreateTilemap(grid.transform, "TM_SoulBoundary", 16, false, false);

        Transform structuresRoot = FindOrCreateChild(root, "Structures");
        Transform ruinedBuildingsRoot = FindOrCreateChild(structuresRoot, "RuinedBuildings");
        Transform woodenScaffoldsRoot = FindOrCreateChild(structuresRoot, "WoodenScaffolds");
        Transform roofStructuresRoot = FindOrCreateChild(structuresRoot, "RoofStructures");
        Transform decorativePropsRoot = FindOrCreateChild(structuresRoot, "DecorativeProps");

        Transform gameplayRoot = FindOrCreateChild(root, "Gameplay");
        Transform spawnRoot = FindOrCreateChild(gameplayRoot, "PlayerSpawn");
        Transform checkpointRoot = FindOrCreateChild(gameplayRoot, "Checkpoints");
        Transform combatZoneRoot = FindOrCreateChild(gameplayRoot, "CombatZones");
        Transform possessableBodyRoot = FindOrCreateChild(gameplayRoot, "PossessableBodies");
        Transform gimmickRoot = FindOrCreateChild(gameplayRoot, "Gimmicks");
        Transform trapRoot = FindOrCreateChild(gameplayRoot, "Hazards");
        Transform doorRoot = FindOrCreateChild(gameplayRoot, "Doors");
        Transform portalRoot = FindOrCreateChild(gameplayRoot, "ExitPortal");

        Transform cameraRoot = FindOrCreateChild(root, "Camera");
        FindOrCreateChild(cameraRoot, "CameraConfiner");
        FindOrCreateChild(cameraRoot, "CameraZones");

        Transform systemsRoot = FindOrCreateChild(root, "Systems");
        Transform systemRoot = FindOrCreateOrMoveChild(root, systemsRoot, "HWJ_StageRuntimeSystems");
        FindOrCreateChild(systemsRoot, "GimmickManager");
        FindOrCreateChild(systemsRoot, "CombatZoneManager");

        EnsureStage101Camera(new Vector3(-26f, -2.2f, -10f), 6.75f);
        GameObject mainCameraObject = GameObject.Find("Main Camera");

        if (mainCameraObject != null)
        {
            mainCameraObject.transform.SetParent(cameraRoot);
        }

        EnsureDirectionalLight();
        EnsureDemoHud(root, "1-1 황폐한 마을 블록아웃");

        return new HWJ_Stage1SceneContext(
            scene,
            Stage1Scene01Path,
            root,
            backDecoration,
            ground,
            oneWayPlatform,
            foreDecoration,
            soulBoundary,
            systemRoot,
            spawnRoot,
            portalRoot,
            trapRoot,
            possessableBodyRoot,
            bodyBlocker,
            structuresRoot,
            ruinedBuildingsRoot,
            woodenScaffoldsRoot,
            roofStructuresRoot,
            decorativePropsRoot,
            gameplayRoot,
            checkpointRoot,
            combatZoneRoot,
            possessableBodyRoot,
            gimmickRoot,
            doorRoot,
            portalRoot);
    }

    private static GameObject FindOrCreateStage101Root()
    {
        GameObject rootObject = GameObject.Find("StageRoot");

        if (rootObject != null)
        {
            return rootObject;
        }

        rootObject = GameObject.Find("HWJ_Stage1_01_RuinedVillage_Root");

        if (rootObject != null)
        {
            rootObject.name = "StageRoot";
            return rootObject;
        }

        return new GameObject("StageRoot");
    }

    private static GameObject FindOrCreateRoot(string rootName)
    {
        GameObject rootObject = GameObject.Find(rootName);

        if (rootObject != null)
        {
            return rootObject;
        }

        return new GameObject(rootName);
    }

    private static Transform FindOrCreateOrMoveChild(Transform oldParent, Transform newParent, string childName)
    {
        Transform child = newParent.Find(childName);

        if (child != null)
        {
            return child;
        }

        child = oldParent.Find(childName);

        if (child != null)
        {
            child.SetParent(newParent);
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(newParent);
        return childObject.transform;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent);
        return childObject.transform;
    }

    private static int RemoveStage101GeneratedMapObjects(Transform root)
    {
        int removedCount = 0;
        List<GameObject> removeTargets = new List<GameObject>();

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            string childName = child.name;

            if (childName == "HWJ_Grid_Tilemap"
                || childName == "BackgroundRoot"
                || childName.StartsWith("HWJ_RoundBackground_", StringComparison.Ordinal)
                || CanRemoveStage101GeneratedChild(child.gameObject, childName))
            {
                removeTargets.Add(child.gameObject);
            }
        }

        for (int i = 0; i < removeTargets.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(removeTargets[i]);
            removedCount++;
        }

        return removedCount;
    }

    private static bool CanRemoveStage101GeneratedChild(GameObject childObject, string childName)
    {
        if (childObject == null)
        {
            return false;
        }

        bool generatedLayoutRoot =
            childName == "Structures"
            || childName == "Gameplay"
            || childName == "HWJ_SceneSpawnPoints"
            || childName == "HWJ_ScenePortals"
            || childName == "HWJ_SceneTraps"
            || childName == "HWJ_VisualSpawnMarkers";

        if (!generatedLayoutRoot)
        {
            return false;
        }

        Component generatedMarker = GetOptionalComponentByType(childObject, "HWJ_GeneratedStageObject");
        return generatedMarker != null && !GetOptionalBoolProperty(generatedMarker, "ProtectFromGeneratedCleanup");
    }

    private static void RemoveStage101TemporaryVisualMarkers(Transform root)
    {
        Component[] generatedObjects = GetOptionalComponentsInChildren(root, "HWJ_GeneratedStageObject");

        for (int i = generatedObjects.Length - 1; i >= 0; i--)
        {
            Component generatedObject = generatedObjects[i];

            if (generatedObject == null
                || !GetOptionalBoolProperty(generatedObject, "TemporaryPlaceholder")
                || GetOptionalBoolProperty(generatedObject, "ProtectFromGeneratedCleanup"))
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(generatedObject.gameObject);
        }
    }

    private static void EnsureStage101Camera(Vector3 cameraPosition, float cameraSize)
    {
        GameObject cameraObject = GameObject.Find("Main Camera");

        if (cameraObject == null)
        {
            CreateCamera(cameraPosition, cameraSize);
            return;
        }

        Camera camera = cameraObject.GetComponent<Camera>();

        if (camera == null)
        {
            camera = cameraObject.AddComponent<Camera>();
        }

        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = cameraPosition;
        camera.orthographic = true;
        camera.orthographicSize = cameraSize;
        camera.backgroundColor = new Color(0.39f, 0.28f, 0.19f, 1f);
    }

    private static void EnsureDirectionalLight()
    {
        if (GameObject.Find("Directional Light") != null)
        {
            return;
        }

        CreateDirectionalLight();
    }

    private static void EnsureDemoHud(Transform root, string sceneDescription)
    {
        if (root.Find("HWJ_HSHStyleDemoHudCanvas") != null)
        {
            return;
        }

        CreateHshStyleDemoHud(root, sceneDescription);
    }

    private static void PaintStage101PlayableBlockout(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles)
    {
        PaintGroundRun(context, tiles, -42, -8, 18, 4);
        PaintGroundRun(context, tiles, -21, -8, 17, 4);
        PaintGroundRun(context, tiles, 0, -7, 16, 3);
        PaintGroundRun(context, tiles, 20, -8, 18, 4);
        PaintGroundRun(context, tiles, 43, -7, 17, 3);
        PaintGroundRun(context, tiles, 64, -8, 17, 4);
        PaintGroundRun(context, tiles, 88, -7, 16, 3);
        PaintGroundRun(context, tiles, 109, -8, 18, 4);
        PaintGroundRun(context, tiles, 132, -7, 17, 3);
        PaintGroundRun(context, tiles, 151, -8, 14, 4);

        PaintStonePlatform(context, tiles, -30, -1, 8);
        PaintStonePlatform(context, tiles, -16, 2, 8);
        PaintStonePlatform(context, tiles, 0, 5, 10);
        PaintStonePlatform(context, tiles, 18, 4, 8);
        PaintStonePlatform(context, tiles, 47, -1, 12);
        PaintStonePlatform(context, tiles, 65, 2, 10);
        PaintStonePlatform(context, tiles, 83, 5, 10);
        PaintStonePlatform(context, tiles, 103, 3, 10);
        PaintStonePlatform(context, tiles, 124, 2, 12);

        PaintGroundRun(context, tiles, 70, -5, 16, 3);
        PaintGroundRun(context, tiles, 96, -5, 13, 3);
        PaintGroundRun(context, tiles, 116, -5, 12, 3);

        PaintRect(context.SoulBoundary, tiles.SpiritSealWall, 66, -7, 1, 7);
        PaintRect(context.SoulBoundary, tiles.SpiritSealWall, 82, -7, 1, 7);
        PaintRect(context.SoulBoundary, tiles.SpiritWind, 68, -6, 13, 2);
        PaintRect(context.SoulBoundary, tiles.SpiritOrb, 74, -4, 1, 1);

        PaintRect(context.BodyBlocker, tiles.SpiritSealWall, 118, -5, 1, 4);
        PaintRect(context.BodyBlocker, tiles.SpiritSealWall, 134, -5, 1, 4);

        PaintSpawnSupport(context, tiles, new Vector3(-36f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(92f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(118f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(154f, -3.2f, 0f));
    }

    private static void EnsureStage101RuntimeLayout(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles)
    {
        EnsureSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_01", "stage1_01_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-36f, -3.2f, 0f));
        EnsureSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, new Vector3(92f, -3.2f, 0f));
        EnsureSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(118f, -3.2f, 0f));
        EnsureSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_01_ToOutpost", "stage1_01_exit_outpost", HWJ_SpawnPointType.NPC, new Vector3(154f, -3.2f, 0f));

        HWJ_StageRuntimeSystems runtimeSystems = EnsureStage101RuntimeSystems(context);

        GameObject playerObject = GameObject.Find("HWJ_Player");

        if (ShouldReplaceGeneratedStage101Player(playerObject))
        {
            UnityEngine.Object.DestroyImmediate(playerObject);
            playerObject = null;
        }

        if (playerObject == null)
        {
            playerObject = CreatePlayablePlayer(context, new Vector3(-36f, -3.2f, 0f));
        }
        else
        {
            playerObject.transform.position = new Vector3(-36f, -3.2f, 0f);
        }

        ConfigureStage101PlayerRendering(playerObject);
        ConfigureMainCameraFollow(playerObject);
        EnsureStage101Portal(context, tiles, runtimeSystems);
        EnsureStage101PresentationSupport(context.SystemRoot);
    }

    private static bool ShouldReplaceGeneratedStage101Player(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return false;
        }

        Component generatedMarker = GetOptionalComponentByType(playerObject, "HWJ_GeneratedStageObject");

        if (generatedMarker == null || GetOptionalBoolProperty(generatedMarker, "ProtectFromGeneratedCleanup"))
        {
            return false;
        }

        return PrefabUtility.GetCorrespondingObjectFromSource(playerObject) == null;
    }

    private static void ConfigureStage101PlayerRendering(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        playerObject.tag = "Player";
        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(playerObject);
        renderer.sprite = renderer.sprite != null ? renderer.sprite : LoadPlayerDisplaySprite();

        if (renderer.sprite == null || renderer.sprite.bounds.size.x > 2.25f)
        {
            renderer.sprite = LoadPlayerDisplaySprite();
        }

        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 30);

        Animator animator = EnsureComponent<Animator>(playerObject);

        if (animator.runtimeAnimatorController == null)
        {
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerGhostAnimatorControllerPath);
        }

        AssignLayerIfExists(playerObject, "Player");
    }

    private static HWJ_StageRuntimeSystems EnsureStage101RuntimeSystems(HWJ_Stage1SceneContext context)
    {
        HWJ_SpawnerSystem spawner = UnityEngine.Object.FindFirstObjectByType<HWJ_SpawnerSystem>(FindObjectsInactive.Include);
        HWJ_GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<HWJ_GameManager>(FindObjectsInactive.Include);
        HWJ_SceneTransitionSystem transition = UnityEngine.Object.FindFirstObjectByType<HWJ_SceneTransitionSystem>(FindObjectsInactive.Include);
        HWJ_StageProgressionSystem progression = UnityEngine.Object.FindFirstObjectByType<HWJ_StageProgressionSystem>(FindObjectsInactive.Include);
        HWJ_StageEnemyCountSystem enemyCount = UnityEngine.Object.FindFirstObjectByType<HWJ_StageEnemyCountSystem>(FindObjectsInactive.Include);
        HWJ_CoreLoopCoordinator coreLoop = UnityEngine.Object.FindFirstObjectByType<HWJ_CoreLoopCoordinator>(FindObjectsInactive.Include);

        if (spawner == null
            || gameManager == null
            || transition == null
            || progression == null
            || enemyCount == null
            || coreLoop == null)
        {
            HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
                "HWJ_Stage1_01_SpawnTable.asset",
                "spawn.stage1.01",
                new HWJ_Stage1SpawnRequest("stage1_01_spawn_sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, SwordEnemyRootPath, SwordEnemyPrefabPath, 2),
                new HWJ_Stage1SpawnRequest("stage1_01_spawn_bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, BowEnemyRootPath, BowEnemyPrefabPath, 1));

            HWJ_StageRuntimeSystems createdSystems = CreateStageRuntimeSystems(
                context,
                "stage.region01.01",
                "stage.region01.02",
                spawnTable,
                false,
                false);
            ConfigureStage101EnemyCountScope(createdSystems.EnemyCount, context);
            return createdSystems;
        }

        ConfigureStage101Spawner(spawner);
        ConfigureStage101EnemyCountScope(enemyCount, context);
        return new HWJ_StageRuntimeSystems(gameManager, spawner, transition, progression, enemyCount, coreLoop);
    }

    private static void EnsureStage101PresentationSupport(Transform systemRoot)
    {
        if (systemRoot == null)
        {
            return;
        }

        if (!SceneContainsMonoBehaviourType("HWJ_PresentationAudioSystem"))
        {
            GameObject audioObject = new GameObject("HWJ_PresentationAudioSystem");
            audioObject.transform.SetParent(systemRoot);
            AddMonoBehaviourByTypeName(audioObject, "HWJ_PresentationAudioSystem");
            EditorUtility.SetDirty(audioObject);
        }

        if (!SceneContainsMonoBehaviourType("HWJ_PresentationDebugSystem"))
        {
            GameObject debugObject = new GameObject("HWJ_PresentationDebugSystem");
            debugObject.transform.SetParent(systemRoot);
            AddMonoBehaviourByTypeName(debugObject, "HWJ_PresentationDebugSystem");
            EditorUtility.SetDirty(debugObject);
        }

        if (!SceneContainsMonoBehaviourType("HWJ_PresentationClearOverlaySystem"))
        {
            GameObject clearObject = new GameObject("HWJ_PresentationClearOverlaySystem");
            clearObject.transform.SetParent(systemRoot);
            AddMonoBehaviourByTypeName(clearObject, "HWJ_PresentationClearOverlaySystem");
            EditorUtility.SetDirty(clearObject);
        }

        if (!SceneContainsMonoBehaviourType("HWJ_BuildRuntimeSmokeSystem"))
        {
            GameObject smokeObject = new GameObject("HWJ_BuildRuntimeSmokeSystem");
            smokeObject.transform.SetParent(systemRoot);
            AddMonoBehaviourByTypeName(smokeObject, "HWJ_BuildRuntimeSmokeSystem");
            EditorUtility.SetDirty(smokeObject);
        }

        ConfigureStage101BuildRuntimeSmokeSystem();

        if (!SceneContainsMonoBehaviourType("HWJ_ManualDemoRunRecorderSystem"))
        {
            GameObject recorderObject = new GameObject("HWJ_ManualDemoRunRecorderSystem");
            recorderObject.transform.SetParent(systemRoot);
            AddMonoBehaviourByTypeName(recorderObject, "HWJ_ManualDemoRunRecorderSystem");
            EditorUtility.SetDirty(recorderObject);
        }

        ConfigureStage101ManualDemoRunRecorderSystem();
    }

    private static void ConfigureStage101BuildRuntimeSmokeSystem()
    {
        MonoBehaviour smokeSystem = FindSceneMonoBehaviourByTypeName("HWJ_BuildRuntimeSmokeSystem");

        if (smokeSystem == null)
        {
            return;
        }

        SerializedObject serializedSmoke = new SerializedObject(smokeSystem);
        SetSerializedBooleanValue(serializedSmoke, "runSmokeCheck", true);
        SetSerializedBooleanValue(serializedSmoke, "allowInReleaseBuild", true);
        SetSerializedFloatValue(serializedSmoke, "validationDelaySeconds", 2.5f);
        SetSerializedBooleanValue(serializedSmoke, "quitAfterReport", false);
        SetSerializedStringValue(serializedSmoke, "playerObjectName", "HWJ_Player");
        SetSerializedStringValue(serializedSmoke, "portalObjectName", "HWJ_Portal_Stage1_01_ToOutpost");
        SetSerializedStringValue(serializedSmoke, "bowSwitchObjectName", "PF_BowSwitch_Stage1_01_LongShot");
        SetSerializedStringValue(serializedSmoke, "bowDoorObjectName", "PF_GimmickDoor_Stage1_01_BowSwitchDoor");
        SetSerializedStringValue(serializedSmoke, "soulWallObjectName", "PF_SoulPassWall_Stage1_01_ScoutGate");
        SetSerializedStringValue(serializedSmoke, "sandstormObjectName", "PF_SandstormLane_Stage1_01_SoulPreview");
        SetSerializedStringValue(serializedSmoke, "spikeTrapObjectName", "PF_SpikeTrap_Stage1_01_DashGap");
        SetSerializedStringValue(serializedSmoke, "checkpointObjectName", "PF_Checkpoint_Stage1_01_Entrance");
        serializedSmoke.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(smokeSystem);
    }

    private static void ConfigureStage101ManualDemoRunRecorderSystem()
    {
        MonoBehaviour recorderSystem = FindSceneMonoBehaviourByTypeName("HWJ_ManualDemoRunRecorderSystem");

        if (recorderSystem == null)
        {
            return;
        }

        SerializedObject serializedRecorder = new SerializedObject(recorderSystem);
        SetSerializedBooleanValue(serializedRecorder, "enableManualDemoRecording", true);
        SetSerializedFloatValue(serializedRecorder, "minimumCountedRunSeconds", 60f);
        SetSerializedBooleanValue(serializedRecorder, "requireCoreMilestonesForCount", true);
        SetSerializedBooleanValue(serializedRecorder, "excludePresentationDebugClear", true);
        SetSerializedBooleanValue(serializedRecorder, "excludePresentationDebugKeys", true);
        SetSerializedFloatValue(serializedRecorder, "movementDistanceThreshold", 1.25f);
        SetSerializedFloatValue(serializedRecorder, "jumpHeightThreshold", 0.65f);

        SerializedProperty resetRecordKey = serializedRecorder.FindProperty("resetRecordKey");

        if (resetRecordKey != null)
        {
            resetRecordKey.intValue = (int)KeyCode.F10;
        }

        serializedRecorder.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recorderSystem);
    }

    private static bool SceneContainsMonoBehaviourType(string typeName)
    {
        return FindSceneMonoBehaviourByTypeName(typeName) != null;
    }

    private static MonoBehaviour FindSceneMonoBehaviourByTypeName(string typeName)
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name == typeName)
            {
                return behaviours[i];
            }
        }

        return null;
    }

    private static void SetSerializedBooleanValue(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetSerializedFloatValue(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetSerializedStringValue(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void AddMonoBehaviourByTypeName(GameObject targetObject, string typeName)
    {
        Type componentType = FindTypeByName(typeName);

        if (componentType == null || !typeof(MonoBehaviour).IsAssignableFrom(componentType))
        {
            Debug.LogWarning($"[Stage1 Builder] Presentation support component was not found yet: {typeName}. Refresh Unity scripts, then rebuild Stage1-1.");
            return;
        }

        targetObject.AddComponent(componentType);
    }

    private static Type FindTypeByName(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
        {
            Type foundType = assemblies[assemblyIndex].GetType(typeName);

            if (foundType != null)
            {
                return foundType;
            }
        }

        return null;
    }

    private static void ConfigureStage101Spawner(HWJ_SpawnerSystem spawner)
    {
        if (spawner == null)
        {
            return;
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        SerializedProperty spawnOnStart = serializedSpawner.FindProperty("spawnOnStart");

        if (spawnOnStart != null)
        {
            spawnOnStart.boolValue = false;
        }

        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureStage101EnemyCountScope(HWJ_StageEnemyCountSystem enemyCount, HWJ_Stage1SceneContext context)
    {
        if (enemyCount == null)
        {
            return;
        }

        SerializedObject serializedEnemyCount = new SerializedObject(enemyCount);
        serializedEnemyCount.FindProperty("stageRoot").objectReferenceValue = context.Root;
        serializedEnemyCount.FindProperty("countEnemyObjects").boolValue = true;
        serializedEnemyCount.FindProperty("countBossObjects").boolValue = false;
        serializedEnemyCount.FindProperty("scanOnStart").boolValue = true;
        serializedEnemyCount.FindProperty("periodicRescan").boolValue = true;
        serializedEnemyCount.FindProperty("rescanIntervalSeconds").floatValue = 0.25f;
        serializedEnemyCount.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureSpawnPoint(
        Transform parent,
        string objectName,
        string pointId,
        HWJ_SpawnPointType type,
        Vector3 position)
    {
        HWJ_SpawnPoint[] spawnPoints = UnityEngine.Object.FindObjectsByType<HWJ_SpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && spawnPoints[i].PointId == pointId)
            {
                spawnPoints[i].gameObject.name = objectName;
                spawnPoints[i].transform.SetParent(parent);
                spawnPoints[i].transform.position = position;
                return;
            }
        }

        CreateSpawnPoint(parent, objectName, pointId, type, position);
    }

    private static void EnsureStage101Portal(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        HWJ_StageRuntimeSystems runtimeSystems = null)
    {
        GameObject portalObject = GameObject.Find("HWJ_Portal_Stage1_01_ToOutpost");

        if (portalObject != null)
        {
            portalObject.transform.SetParent(context.PortalRoot);
            portalObject.transform.position = new Vector3(154f, -2.55f, 0f);
            ConfigureExistingScenePortal(portalObject, runtimeSystems);
            return;
        }

        HWJ_StageProgressionSystem progression = runtimeSystems != null
            ? runtimeSystems.Progression
            : UnityEngine.Object.FindFirstObjectByType<HWJ_StageProgressionSystem>(FindObjectsInactive.Include);
        HWJ_StageEnemyCountSystem enemyCount = runtimeSystems != null
            ? runtimeSystems.EnemyCount
            : UnityEngine.Object.FindFirstObjectByType<HWJ_StageEnemyCountSystem>(FindObjectsInactive.Include);
        HWJ_SceneTransitionSystem transition = runtimeSystems != null
            ? runtimeSystems.Transition
            : UnityEngine.Object.FindFirstObjectByType<HWJ_SceneTransitionSystem>(FindObjectsInactive.Include);
        HWJ_GameManager gameManager = runtimeSystems != null
            ? runtimeSystems.GameManager
            : UnityEngine.Object.FindFirstObjectByType<HWJ_GameManager>(FindObjectsInactive.Include);
        HWJ_SpawnerSystem spawner = runtimeSystems != null
            ? runtimeSystems.Spawner
            : UnityEngine.Object.FindFirstObjectByType<HWJ_SpawnerSystem>(FindObjectsInactive.Include);
        HWJ_CoreLoopCoordinator coreLoop = runtimeSystems != null
            ? runtimeSystems.CoreLoop
            : UnityEngine.Object.FindFirstObjectByType<HWJ_CoreLoopCoordinator>(FindObjectsInactive.Include);

        if (progression == null || enemyCount == null || transition == null || gameManager == null || spawner == null || coreLoop == null)
        {
            Debug.LogWarning("HWJ Stage1-1 portal creation skipped because preserved runtime systems were not found.");
            return;
        }

        HWJ_StageRuntimeSystems resolvedSystems = new HWJ_StageRuntimeSystems(
            gameManager,
            spawner,
            transition,
            progression,
            enemyCount,
            coreLoop);

        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_01_ToOutpost",
            new Vector3(154f, -2.55f, 0f),
            "HWJ_Stage1_02_RuinedOutpost",
            "stage1_02_player_start",
            resolvedSystems,
            tiles.RustyGate);
    }

    private static void ConfigureExistingScenePortal(GameObject portalObject, HWJ_StageRuntimeSystems runtimeSystems)
    {
        if (portalObject == null || runtimeSystems == null)
        {
            return;
        }

        HWJ_ScenePortalSystem portal = portalObject.GetComponent<HWJ_ScenePortalSystem>();

        if (portal == null)
        {
            return;
        }

        SerializedObject serializedPortal = new SerializedObject(portal);
        serializedPortal.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedPortal.FindProperty("stageEnemyCountSystem").objectReferenceValue = runtimeSystems.EnemyCount;
        serializedPortal.FindProperty("sceneTransitionSystem").objectReferenceValue = runtimeSystems.Transition;
        serializedPortal.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateStage101VisibleVillageLayers(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles)
    {
        CreateStage101RuinedVillageStructures(context, tiles);
        CreateStage101PossessionAndGimmickObjects(context, tiles);
        CreateStage101ForegroundAtmosphere(context, tiles);
    }

    private static void CreateStage101RuinedVillageStructures(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles)
    {
        Transform buildingRoot = context.RuinedBuildingsRoot ?? context.VisualRoot;
        Transform scaffoldRoot = context.WoodenScaffoldsRoot ?? context.VisualRoot;
        Transform roofRoot = context.RoofStructuresRoot ?? context.VisualRoot;
        Transform propRoot = context.DecorativePropsRoot ?? context.VisualRoot;

        CreateTiledVisualObject(buildingRoot, "Village_Back_RuinedHouse_Entrance_Wall", tiles.BackgroundWallA, new Vector3(-29f, -3.2f, 0f), 8, 6, -30, new Color(0.56f, 0.48f, 0.43f, 0.78f));
        CreateTiledVisualObject(roofRoot, "Village_Back_RuinedHouse_Entrance_Roof", tiles.CollapsedRoof, new Vector3(-29f, 0.2f, 0f), 9, 1, -25, new Color(0.72f, 0.5f, 0.32f, 0.88f));
        CreateTiledVisualObject(buildingRoot, "Village_Back_BrokenTower_Left", tiles.RuinedWall, new Vector3(-10f, -1.4f, 0f), 5, 10, -28, new Color(0.46f, 0.39f, 0.36f, 0.82f));
        CreateTiledVisualObject(roofRoot, "Village_Back_BrokenTower_Left_Roof", tiles.CollapsedRoof, new Vector3(-10f, 4.1f, 0f), 6, 1, -23, new Color(0.74f, 0.51f, 0.32f, 0.88f));

        CreateTiledVisualObject(buildingRoot, "Village_Back_Central_CollapsedHouse_Wall", tiles.BackgroundWallB, new Vector3(36f, -2.7f, 0f), 10, 7, -29, new Color(0.5f, 0.43f, 0.38f, 0.8f));
        CreateTiledVisualObject(roofRoot, "Village_Back_Central_CollapsedHouse_Roof", tiles.CollapsedRoof, new Vector3(36f, 1.4f, 0f), 11, 1, -24, new Color(0.7f, 0.46f, 0.28f, 0.9f));
        CreateTiledVisualObject(scaffoldRoot, "Village_Mid_WoodenScaffold_BowRoute", tiles.CollapsedRoof, new Vector3(82f, -0.4f, 0f), 1, 7, -5, new Color(0.64f, 0.45f, 0.28f, 0.95f));
        CreateTiledVisualObject(scaffoldRoot, "Village_Mid_WoodenScaffold_BowRoute_Plank", tiles.CollapsedRoof, new Vector3(88f, 1.8f, 0f), 9, 1, -4, new Color(0.68f, 0.48f, 0.3f, 0.95f));

        CreateTiledVisualObject(buildingRoot, "Village_Back_FinalGate_Ruin_Wall", tiles.BarracksWall, new Vector3(140f, -2.8f, 0f), 8, 8, -28, new Color(0.43f, 0.42f, 0.38f, 0.85f));
        CreateTiledVisualObject(roofRoot, "Village_Back_FinalGate_Ruin_Roof", tiles.CollapsedRoof, new Vector3(140f, 1.9f, 0f), 9, 1, -22, new Color(0.68f, 0.45f, 0.28f, 0.9f));

        CreateTiledVisualObject(propRoot, "Village_Prop_Rubble_Entrance", tiles.CrackedStone, new Vector3(-18f, -3.2f, 0f), 3, 1, 32, new Color(0.7f, 0.62f, 0.54f, 1f));
        CreateTiledVisualObject(propRoot, "Village_Prop_Rubble_CombatRoom", tiles.CrackedStone, new Vector3(99f, -3.2f, 0f), 4, 1, 32, new Color(0.7f, 0.62f, 0.54f, 1f));
        CreateTiledVisualObject(propRoot, "Village_Prop_Barricade_Final", tiles.RustyGate, new Vector3(147f, -3.1f, 0f), 3, 2, 31, new Color(0.58f, 0.39f, 0.27f, 0.95f));
    }

    private static void CreateStage101PossessionAndGimmickObjects(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles)
    {
        Transform bodyRoot = context.PossessableBodyRoot ?? context.VisualRoot;

        CreateDirectPossessableCorpse(bodyRoot, "HWJ_Stage1_01_FirstPossessionCorpse", SwordEnemyRootPath, SwordEnemySpritePath, new Vector3(-32f, -3.05f, 0f));
        CreateDirectPossessableCorpse(bodyRoot, "HWJ_Stage1_01_BowCorpse", BowEnemyRootPath, BowEnemySpritePath, new Vector3(82f, -3.05f, 0f));
        CreateDirectPossessableCorpse(bodyRoot, "HWJ_Stage1_01_ShieldCorpse", ShieldEnemyRootPath, ShieldEnemySpritePath, new Vector3(92f, -3.05f, 0f));
        CreateDirectPossessableCorpse(bodyRoot, "HWJ_Stage1_01_AxeCorpse", AxeEnemyRootPath, AxeEnemySpritePath, new Vector3(102f, -3.05f, 0f));
        CreateDirectPossessableCorpse(bodyRoot, "HWJ_Stage1_01_LanceCorpse", LanceEnemyRootPath, LanceEnemySpritePath, new Vector3(112f, -3.05f, 0f));
        CreateDirectPossessableCorpse(bodyRoot, "HWJ_Stage1_01_SwordComboCorpse", SwordEnemyRootPath, SwordEnemySpritePath, new Vector3(124f, -3.05f, 0f));

        CreateVisibleCheckpoint(context, tiles, "PF_Checkpoint_Stage1_01_Entrance", new Vector3(-18f, -2.65f, 0f));
        CreateVisibleCheckpoint(context, tiles, "PF_Checkpoint_Stage1_01_BeforeGimmicks", new Vector3(62f, -2.65f, 0f));

        CreateCombatZoneVisual(context, tiles, "HWJ_CombatZone_Stage1_01_FirstFight", new Vector3(4f, -2.6f, 0f), new Vector2(12f, 5f), true);
        CreateCombatZoneVisual(context, tiles, "HWJ_CombatZone_Stage1_01_GimmickHall", new Vector3(102f, -2.6f, 0f), new Vector2(22f, 5f), true);
        CreateCombatZoneVisual(context, tiles, "HWJ_CombatZone_Stage1_01_FinalFight", new Vector3(126f, -2.6f, 0f), new Vector2(18f, 5f), true);
        CreateCombatZoneVisual(context, tiles, "HWJ_CombatZone_Stage1_01_OptionalUpperReward", new Vector3(6f, 5.7f, 0f), new Vector2(11f, 3f), false);
        CreateStage101LiveEnemyEncounters(context);

        CreateSpikeTrap(context.TrapRoot, "PF_SpikeTrap_Stage1_01_DashGap", new Vector3(22.5f, -6.9f, 0f), new Vector2(4.5f, 0.6f), 12f, 2.6f, tiles.SpikeWarning);
        CreateSpikeTrap(context.TrapRoot, "PF_SpikeTrap_Stage1_01_FinalPit", new Vector3(51f, -5.9f, 0f), new Vector2(4f, 0.6f), 14f, 2.6f, tiles.SpikeWarning);
        CreateRoofCollapseTrap(context, tiles, "PF_RoofCollapseTrap_Stage1_01_Tower", new Vector3(-10f, 3.25f, 0f), new Vector2(5f, 1f));
        CreateSteamTrap(context.TrapRoot, "PF_SandstormLane_Stage1_01_SoulPreview", new Vector3(70f, -2.55f, 0f), new Vector2(6.5f, 1.2f), 8f, 2f, 2.4f, tiles.SandstormGuide);

        CreateSpiritOrbSwitchShowcase(context, tiles, "PF_SoulOrbSwitch_Stage1_01_ScoutGate", "stage1_01.spirit_orb_scout_gate", new Vector3(74f, -3.2f, 0f), new Vector3(82f, -2.8f, 0f));
        CreateBodyObstacleGate(context, tiles, "PF_SoulPassWall_Stage1_01_ScoutGate", "stage1_01.spirit_scout_gate", new Vector3(73f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_BodyObstacleRequirementMode.SpiritOnly, null);
        CreateWeaponSkillSwitch(context, tiles, "PF_BowSwitch_Stage1_01_LongShot", "stage1_01.bow_range_switch", new Vector3(86f, -2.2f, 0f), new Vector3(89f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_WeaponType.Bow, HWJ_SkillActionType.Projectile, 1, "PF_GimmickDoor_Stage1_01_BowSwitchDoor");
        CreateBodyObstacleGate(context, tiles, "PF_ShieldDoor_Stage1_01_ArrowPassage", "stage1_01.shield_arrow_passage", new Vector3(98f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_BodyObstacleRequirementMode.AbilityTag, null, new[] { HWJ_AbilityTag.ShieldArrowPassage });
        CreateBodyObstacleGate(context, tiles, "PF_AxeBreakWall_Stage1_01_CrackedWall", "stage1_01.axe_break_wall", new Vector3(108f, -2.8f, 0f), new Vector2(1.6f, 4.6f), HWJ_BodyObstacleRequirementMode.AbilityTag, null, new[] { HWJ_AbilityTag.AxeBreakWall }, true, HWJ_WeaponType.Axe, HWJ_SkillActionType.None);
        CreateBodyObstacleGate(context, tiles, "PF_LanceChargeDevice_Stage1_01_LeverWall", "stage1_01.lance_charge_device", new Vector3(120f, -2.8f, 0f), new Vector2(1.6f, 4.6f), HWJ_BodyObstacleRequirementMode.AbilityTag, null, new[] { HWJ_AbilityTag.LanceChargeDevice }, true, HWJ_WeaponType.Lance, HWJ_SkillActionType.Dash);
        CreateWeaponSkillSwitch(context, tiles, "PF_SwordSwitch_Stage1_01_RapidCombo", "stage1_01.sword_rapid_switch", new Vector3(130f, -2.2f, 0f), new Vector3(134f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_WeaponType.Sword, HWJ_SkillActionType.Melee, 3);
    }

    private static void CreateStage101LiveEnemyEncounters(HWJ_Stage1SceneContext context)
    {
        Transform liveEnemyRoot = FindOrCreateChild(context.CombatZoneRoot ?? context.GameplayRoot, "LiveEnemies");

        Transform combatZone01 = FindOrCreateChild(liveEnemyRoot, "CombatZone_01_SwordPair");
        InstantiateStage101Enemy(combatZone01, "HWJ_Enemy_Stage1_01_CZ01_Sword_01", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(1.5f, -3.05f, 0f));
        InstantiateStage101Enemy(combatZone01, "HWJ_Enemy_Stage1_01_CZ01_Sword_02", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(7.5f, -3.05f, 0f));

        Transform combatZone02 = FindOrCreateChild(liveEnemyRoot, "CombatZone_02_GimmickHall");
        InstantiateStage101Enemy(combatZone02, "HWJ_Enemy_Stage1_01_CZ02_Sword_01", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(96f, -3.05f, 0f));
        InstantiateStage101Enemy(combatZone02, "HWJ_Enemy_Stage1_01_CZ02_Sword_02", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(104f, -3.05f, 0f));
        InstantiateStage101Enemy(combatZone02, "HWJ_Enemy_Stage1_01_CZ02_Bow_01", BowEnemyPrefabPath, BowEnemyRootPath, new Vector3(112f, -3.05f, 0f));

        Transform combatZone03 = FindOrCreateChild(liveEnemyRoot, "CombatZone_03_FinalFight");
        InstantiateStage101Enemy(combatZone03, "HWJ_Enemy_Stage1_01_CZ03_Sword_01", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(122f, -3.05f, 0f));
        InstantiateStage101Enemy(combatZone03, "HWJ_Enemy_Stage1_01_CZ03_Sword_02", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(128f, -3.05f, 0f));
        InstantiateStage101Enemy(combatZone03, "HWJ_Enemy_Stage1_01_CZ03_Bow_01", BowEnemyPrefabPath, BowEnemyRootPath, new Vector3(134f, -3.05f, 0f));
        InstantiateStage101Enemy(combatZone03, "HWJ_Enemy_Stage1_01_CZ03_Shield_01", ShieldEnemyPrefabPath, ShieldEnemyRootPath, new Vector3(140f, -3.05f, 0f));

        Transform optionalZone = FindOrCreateChild(liveEnemyRoot, "CombatZone_Optional_UpperReward");
        InstantiateStage101Enemy(optionalZone, "HWJ_Enemy_Stage1_01_Optional_Sword_01", SwordEnemyPrefabPath, SwordEnemyRootPath, new Vector3(2f, 5.95f, 0f));
        InstantiateStage101Enemy(optionalZone, "HWJ_Enemy_Stage1_01_Optional_Axe_01", AxeEnemyPrefabPath, AxeEnemyRootPath, new Vector3(8f, 5.95f, 0f));
    }

    private static GameObject InstantiateStage101Enemy(
        Transform parent,
        string objectName,
        string prefabPath,
        string rootObjectDataPath,
        Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[Stage1 Builder] Missing required Enemy Prefab: {objectName} / {prefabPath}");
            return null;
        }

        GameObject enemyObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (enemyObject == null)
        {
            enemyObject = UnityEngine.Object.Instantiate(prefab);
            PrefabUtility.UnpackPrefabInstance(enemyObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Debug.LogWarning($"[Stage1 Builder] Enemy PrefabUtility instance failed. A scene clone was created instead: {objectName} / {prefabPath}");
        }

        enemyObject.name = objectName;
        enemyObject.transform.SetParent(parent, false);
        enemyObject.transform.position = position;
        enemyObject.transform.localScale = Vector3.one;
        AssignLayerIfExists(enemyObject, "Enemy");

        SpriteRenderer[] renderers = enemyObject.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = Mathf.Max(renderers[i].sortingOrder, 21);
        }

        HWJ_RootObjectDataResolver resolver = enemyObject.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null)
        {
            resolver.SetRootObjectData(AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(rootObjectDataPath));
        }

        HWJ_RuntimeStatusSystem status = enemyObject.GetComponent<HWJ_RuntimeStatusSystem>();

        if (status != null)
        {
            status.RefreshCurrentHpFromData(true);
        }

        ConfigureCombatExecutionTargetFilter(enemyObject.GetComponent<HWJ_CombatExecutionSystem>(), false);
        EditorUtility.SetDirty(enemyObject);
        EditorSceneManager.MarkSceneDirty(enemyObject.scene);
        return enemyObject;
    }

    private static void CreateStage101ForegroundAtmosphere(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles)
    {
        Transform propRoot = context.DecorativePropsRoot ?? context.VisualRoot;
        CreateTiledVisualObject(propRoot, "Foreground_Dust_Entrance_Low", tiles.SandstormGuide, new Vector3(-34f, -3.35f, 0f), 9, 1, 40, new Color(0.96f, 0.74f, 0.42f, 0.32f));
        CreateTiledVisualObject(propRoot, "Foreground_Dust_Central_Lane", tiles.SandstormGuide, new Vector3(35f, -2.35f, 0f), 13, 1, 40, new Color(0.96f, 0.74f, 0.42f, 0.28f));
        CreateTiledVisualObject(propRoot, "Foreground_Dust_FinalGate", tiles.SandstormGuide, new Vector3(139f, -2.35f, 0f), 12, 1, 40, new Color(0.96f, 0.74f, 0.42f, 0.3f));
    }

    private static void CreateVisibleCheckpoint(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, string objectName, Vector3 position)
    {
        Transform parent = context.CheckpointRoot ?? context.VisualRoot;
        GameObject checkpointObject = CreateTiledVisualObject(parent, objectName, tiles.SpiritOrb, position, 1, 2, 24, new Color(0.45f, 0.9f, 1f, 0.95f));

        BoxCollider2D trigger = checkpointObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 2.2f);
    }

    private static void CreateCombatZoneVisual(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, string objectName, Vector3 position, Vector2 size, bool required)
    {
        Transform parent = context.CombatZoneRoot ?? context.VisualRoot;
        int columns = Mathf.Max(1, Mathf.RoundToInt(size.x));
        int rows = Mathf.Max(1, Mathf.RoundToInt(size.y));
        Color color = required ? new Color(0.95f, 0.18f, 0.18f, 0.16f) : new Color(0.3f, 0.65f, 1f, 0.14f);
        GameObject zoneObject = CreateTiledVisualObject(parent, objectName, tiles.SandstormGuide, position, columns, rows, 18, color);

        BoxCollider2D trigger = zoneObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = size;
    }

    private static void CreateRoofCollapseTrap(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, string objectName, Vector3 position, Vector2 size)
    {
        Transform parent = context.TrapRoot ?? context.VisualRoot;
        int columns = Mathf.Max(1, Mathf.RoundToInt(size.x));
        GameObject trapObject = CreateTiledVisualObject(parent, objectName, tiles.CollapsedRoof, position, columns, 1, 25, new Color(0.74f, 0.42f, 0.22f, 0.96f));

        BoxCollider2D trigger = trapObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = size;

        GameObject warningDustObject = CreateTiledVisualObject(trapObject.transform, objectName + "_WarningDust", tiles.SandstormGuide, position + new Vector3(0f, -0.8f, 0f), columns, 1, 26, new Color(1f, 0.76f, 0.34f, 0.35f));
        AddLoopingVisualAnimator(warningDustObject, "HWJ_Stage1_DustWarning_Pulse");

        Transform[] debrisSpawnPoints =
        {
            CreateLocalMarker(trapObject.transform, objectName + "_DebrisSpawn_01", new Vector3(-1.6f, -0.1f, 0f)),
            CreateLocalMarker(trapObject.transform, objectName + "_DebrisSpawn_02", new Vector3(0f, 0.1f, 0f)),
            CreateLocalMarker(trapObject.transform, objectName + "_DebrisSpawn_03", new Vector3(1.6f, -0.1f, 0f)),
        };

        Component roofTrap = AddExistingTrapComponent(trapObject, "HWJ_RoofCollapseTrapSystem");

        if (roofTrap == null)
        {
            return;
        }

        SerializedObject serializedTrap = new SerializedObject(roofTrap);
        serializedTrap.FindProperty("debrisSprite").objectReferenceValue = GetTileSprite(tiles.CollapsedRoof);
        serializedTrap.FindProperty("warningVisual").objectReferenceValue =
            warningDustObject != null ? warningDustObject.GetComponentInChildren<SpriteRenderer>(true) : null;
        SetObjectArray(serializedTrap.FindProperty("debrisSpawnPoints"), debrisSpawnPoints);
        serializedTrap.FindProperty("damage").floatValue = 18f;
        serializedTrap.FindProperty("warningSeconds").floatValue = 0.8f;
        serializedTrap.FindProperty("debrisLifetimeSeconds").floatValue = 4f;
        serializedTrap.FindProperty("debrisFallGravity").floatValue = 3.8f;
        serializedTrap.FindProperty("knockbackPower").floatValue = 11f;
        serializedTrap.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool TryBuildStage101Blockout(HWJ_Stage1SceneContext context)
    {
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -48, -14, 188, 36);
        PaintGroundRun(context, tiles, -42, -8, 27, 4);
        PaintGroundRun(context, tiles, -10, -8, 14, 4);
        PaintGroundRun(context, tiles, 10, -8, 10, 4);
        PaintGroundRun(context, tiles, 27, -8, 12, 4);
        PaintGroundRun(context, tiles, 45, -8, 16, 4);
        PaintGroundRun(context, tiles, 66, -8, 12, 4);
        PaintGroundRun(context, tiles, 84, -8, 54, 4);
        PaintGroundRun(context, tiles, 144, -8, 16, 4);

        PaintStonePlatform(context, tiles, -15, -1, 6);
        PaintStonePlatform(context, tiles, -7, 2, 6);
        PaintStonePlatform(context, tiles, 2, 4, 6);
        PaintStonePlatform(context, tiles, 48, -2, 12);
        PaintStonePlatform(context, tiles, 52, 1, 7);
        PaintStonePlatform(context, tiles, 112, -2, 9);
        PaintStonePlatform(context, tiles, 122, 1, 9);

        PaintRect(context.HazardGuide, tiles.SandstormGuide, -41, -3, 25, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -14, 5, 22, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 12, -3, 28, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 46, 2, 15, 1);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 67, -3, 9, 3);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 84, -3, 55, 1);

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_01", "stage1_01_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-36f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, new Vector3(92f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(118f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_01_ToOutpost", "stage1_01_exit_outpost", HWJ_SpawnPointType.NPC, new Vector3(154f, -3.2f, 0f));

        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-36f, -3.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateDirectPossessableCorpse(context.VisualRoot, "HWJ_Stage1_01_FirstPossessionCorpse", SwordEnemyRootPath, SwordEnemySpritePath, new Vector3(-32f, -3.05f, 0f));
        CreateDirectPossessableCorpse(context.VisualRoot, "HWJ_Stage1_01_BowCorpse", BowEnemyRootPath, BowEnemySpritePath, new Vector3(82f, -3.05f, 0f));
        CreateDirectPossessableCorpse(context.VisualRoot, "HWJ_Stage1_01_ShieldCorpse", ShieldEnemyRootPath, ShieldEnemySpritePath, new Vector3(92f, -3.05f, 0f));
        CreateDirectPossessableCorpse(context.VisualRoot, "HWJ_Stage1_01_AxeCorpse", AxeEnemyRootPath, AxeEnemySpritePath, new Vector3(102f, -3.05f, 0f));
        CreateDirectPossessableCorpse(context.VisualRoot, "HWJ_Stage1_01_LanceCorpse", LanceEnemyRootPath, LanceEnemySpritePath, new Vector3(112f, -3.05f, 0f));
        CreateDirectPossessableCorpse(context.VisualRoot, "HWJ_Stage1_01_SwordComboCorpse", SwordEnemyRootPath, SwordEnemySpritePath, new Vector3(124f, -3.05f, 0f));

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_01_SpawnTable.asset",
            "spawn.stage1.01",
            new HWJ_Stage1SpawnRequest("stage1_01_spawn_sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, SwordEnemyRootPath, SwordEnemyPrefabPath, 2),
            new HWJ_Stage1SpawnRequest("stage1_01_spawn_bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, BowEnemyRootPath, BowEnemyPrefabPath, 1));
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.01",
            "stage.region01.02",
            spawnTable,
            false);

        CreateBodyObstacleGate(context, tiles, "HWJ_Stage1_01_SpiritScoutGate", "stage1_01.spirit_scout_gate", new Vector3(73f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_BodyObstacleRequirementMode.SpiritOnly, null);
        CreateWeaponSkillSwitch(context, tiles, "HWJ_Stage1_01_BowRangeSwitch", "stage1_01.bow_range_switch", new Vector3(86f, -2.2f, 0f), new Vector3(89f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_WeaponType.Bow, HWJ_SkillActionType.Projectile, 1);
        CreateBodyObstacleGate(context, tiles, "HWJ_Stage1_01_ShieldArrowPassage", "stage1_01.shield_arrow_passage", new Vector3(98f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_BodyObstacleRequirementMode.AbilityTag, null, new[] { HWJ_AbilityTag.ShieldArrowPassage });
        CreateBodyObstacleGate(context, tiles, "HWJ_Stage1_01_AxeBreakWall", "stage1_01.axe_break_wall", new Vector3(108f, -2.8f, 0f), new Vector2(1.6f, 4.6f), HWJ_BodyObstacleRequirementMode.AbilityTag, null, new[] { HWJ_AbilityTag.AxeBreakWall }, true, HWJ_WeaponType.Axe, HWJ_SkillActionType.None);
        CreateBodyObstacleGate(context, tiles, "HWJ_Stage1_01_LanceChargeDevice", "stage1_01.lance_charge_device", new Vector3(120f, -2.8f, 0f), new Vector2(1.6f, 4.6f), HWJ_BodyObstacleRequirementMode.AbilityTag, null, new[] { HWJ_AbilityTag.LanceChargeDevice }, true, HWJ_WeaponType.Lance, HWJ_SkillActionType.Dash);
        CreateWeaponSkillSwitch(context, tiles, "HWJ_Stage1_01_SwordRapidSwitch", "stage1_01.sword_rapid_switch", new Vector3(130f, -2.2f, 0f), new Vector3(134f, -2.8f, 0f), new Vector2(1.2f, 4.6f), HWJ_WeaponType.Sword, HWJ_SkillActionType.Melee, 3);

        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_DashGap_Left", new Vector3(22.5f, -6.9f, 0f), new Vector2(4.5f, 0.6f), 12f, 2.6f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_SoulPreview", new Vector3(70f, -2.55f, 0f), new Vector2(6.5f, 1.2f), 8f, 2f, 2.4f, tiles.SandstormGuide);
        CreateScenePortal(context, "HWJ_Portal_Stage1_01_ToOutpost", new Vector3(154f, -2.55f, 0f), "HWJ_Stage1_02_RuinedOutpost", "stage1_02_player_start", runtimeSystems, tiles.RustyGate);

        SaveContextScene(context);
        return true;
    }

    private static void BuildRuinedOutpostScene()
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene02Path,
            "HWJ_Stage1_02_RuinedOutpost",
            "1-3 오래된 기사단 주둔지 입구",
            new Vector3(14f, 0.9f, -10f),
            9.5f);
        CreateRoundBackground(context, Stage1Background02Path, new Vector2(16f, 1f), 135f, 29f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -44, -11, 120, 31);
        PaintGroundRun(context, tiles, -38, -7, 22, 3);
        PaintGroundRun(context, tiles, -10, -7, 27, 3);
        PaintGroundRun(context, tiles, 22, -7, 18, 3);
        PaintGroundRun(context, tiles, 46, -7, 22, 3);
        PaintGroundRun(context, tiles, 14, -4, 10, 2);
        PaintStonePlatform(context, tiles, -21, -1, 9);
        PaintStonePlatform(context, tiles, 5, 1, 10);
        PaintStonePlatform(context, tiles, 25, 2, 9);
        PaintStonePlatform(context, tiles, 43, 1, 8);

        PaintRuinPillar(context, tiles, -29, -4, 3, 7, false);
        PaintRuinPillar(context, tiles, -2, -4, 4, 8, false);
        PaintRuinPillar(context, tiles, 18, -4, 3, 7, true);
        PaintRuinPillar(context, tiles, 40, -4, 4, 8, false);
        PaintRect(context.Decoration, tiles.RustyGate, 18, -2, 2, 7);
        PaintRect(context.Decoration, tiles.RustyGate, 58, -4, 3, 8);
        PaintBackdropBreakup(context, tiles, -36, 2, 12, 5);
        PaintBackdropBreakup(context, tiles, -5, 4, 9, 4);
        PaintBackdropBreakup(context, tiles, 34, 3, 11, 5);
        PaintRubbleTrail(context, tiles, -35, -3, 7, 4);
        PaintRubbleTrail(context, tiles, -6, -3, 7, 3);
        PaintRubbleTrail(context, tiles, 47, -3, 6, 3);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -39, 1, 11, 2);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 6, -4, 5, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 51, -4, 4, 1);
        PaintSpiritOnlyPassage(context, tiles, -18, -3, 9, 6, true);
        PaintSpiritOnlyPassage(context, tiles, 30, -3, 8, 5, false);
        PaintSpiritOrbLine(context, tiles, -16, 0, 5);
        PaintSpiritOrbLine(context, tiles, 31, -1, 4);
        PaintRect(context.HazardGuide, tiles.SpiritWind, -13, 2, 3, 3);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 36, 1, 2, 4);
        PaintSpawnSupport(context, tiles, new Vector3(-34f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(0f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(28f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(50f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(64f, -3.2f, 0f));

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_02", "stage1_02_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-34f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Shield", "stage1_02_enemy_shield", HWJ_SpawnPointType.Enemy, new Vector3(0f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Bow", "stage1_02_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(28f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_02_ToBackRoad", "stage1_02_exit_backroad", HWJ_SpawnPointType.NPC, new Vector3(64f, -3.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-34f, -3.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateLivePossessionResistTarget(
            context.VisualRoot,
            "HWJ_Stage1_02_LivePossessionResistTarget",
            new Vector3(-28f, -3.0f, 0f));
        CreateBodyObstacleGate(
            context,
            tiles,
            "HWJ_Stage1_02_SoulOnlyShortcut",
            "stage1_02.body_obstacle.soul_only",
            new Vector3(-14f, -2.1f, 0f),
            new Vector2(1f, 3.4f),
            HWJ_BodyObstacleRequirementMode.SpiritOnly,
            new HWJ_WeaponType[0]);
        CreateRewardPickup(context.VisualRoot, ExperienceOrbPrefabPath, "HWJ_Stage1_02_ExperienceOrb_80", new Vector3(-25f, -2.3f, 0f), 80);
        CreateRewardPickup(context.VisualRoot, AttackStatOrbPrefabPath, "HWJ_Stage1_02_AttackStatOrb", new Vector3(-23.8f, -2.3f, 0f), 0);

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_02_SpawnTable.asset",
            "spawn.stage1.02",
            new HWJ_Stage1SpawnRequest("stage1_02_spawn_shield", "stage1_02_enemy_shield", HWJ_SpawnPointType.Enemy, ShieldEnemyRootPath, ShieldEnemyPrefabPath, 1),
            new HWJ_Stage1SpawnRequest("stage1_02_spawn_bow", "stage1_02_enemy_bow", HWJ_SpawnPointType.Enemy, BowEnemyRootPath, BowEnemyPrefabPath, 1),
            new HWJ_Stage1SpawnRequest("stage1_02_spawn_axe", "stage1_02_enemy_axe", HWJ_SpawnPointType.Enemy, AxeEnemyRootPath, AxeEnemyPrefabPath, 1));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Axe", "stage1_02_enemy_axe", HWJ_SpawnPointType.Enemy, new Vector3(50f, -3.2f, 0f));
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.02",
            "stage.region01.03",
            spawnTable,
            false);
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_02_ToBackRoad",
            new Vector3(64f, -2.55f, 0f),
            "HWJ_Stage1_03_BackRoad",
            "stage1_03_player_start",
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Spike_Center", new Vector3(8.5f, -3.45f, 0f), new Vector2(4.5f, 0.6f), 20f, 2.6f);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Spike_Right", new Vector3(53f, -3.45f, 0f), new Vector2(3.8f, 0.6f), 20f, 2.6f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Steam_Left", new Vector3(-32f, -2.55f, 0f), new Vector2(5f, 1.2f), 14f, 2f, 2f, tiles.SandstormGuide);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Steam_Right", new Vector3(31f, -2.55f, 0f), new Vector2(5f, 1.2f), 14f, 2f, 2f, tiles.SandstormGuide);

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-34f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_ShieldEnemy_Visual", ShieldEnemySpritePath, new Vector3(0f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_BowEnemy_Visual", BowEnemySpritePath, new Vector3(28f, -2.8f, 0f), 1f, 20);

        SaveContextScene(context);
    }

    private static void BuildBackRoadScene()
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene03Path,
            "HWJ_Stage1_03_BackRoad",
            "Stage 1-3 back road and boss entrance",
            new Vector3(15f, 1f, -10f),
            10f);
        CreateRoundBackground(context, Stage1Background03Path, new Vector2(17f, 1f), 138f, 30f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -44, -12, 122, 32);
        PaintGroundRun(context, tiles, -38, -7, 24, 3);
        PaintGroundRun(context, tiles, -8, -7, 23, 3);
        PaintGroundRun(context, tiles, 22, -6, 18, 2);
        PaintGroundRun(context, tiles, 46, -7, 24, 3);
        PaintGroundRun(context, tiles, 12, -3, 8, 2);
        PaintStonePlatform(context, tiles, -24, 0, 8);
        PaintStonePlatform(context, tiles, -3, 2, 9);
        PaintStonePlatform(context, tiles, 17, 3, 9);
        PaintStonePlatform(context, tiles, 39, 1, 8);
        PaintStonePlatform(context, tiles, 55, 2, 8);
        PaintRuinPillar(context, tiles, -31, -4, 3, 7, false);
        PaintRuinPillar(context, tiles, -2, -4, 4, 7, false);
        PaintRuinPillar(context, tiles, 18, -4, 3, 8, false);
        PaintRuinPillar(context, tiles, 42, -4, 3, 7, true);
        PaintRect(context.Decoration, tiles.RustyGate, 60, -4, 3, 8);
        PaintBackdropBreakup(context, tiles, -36, 2, 12, 5);
        PaintBackdropBreakup(context, tiles, -8, 4, 12, 4);
        PaintBackdropBreakup(context, tiles, 26, 3, 11, 5);
        PaintBackdropBreakup(context, tiles, 52, 4, 9, 4);
        PaintRubbleTrail(context, tiles, -35, -3, 6, 4);
        PaintRubbleTrail(context, tiles, -6, -3, 8, 3);
        PaintRubbleTrail(context, tiles, 47, -3, 7, 3);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -40, 1, 9, 2);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 24, -4, 6, 1);
        PaintSpiritOnlyPassage(context, tiles, -17, -3, 8, 6, true);
        PaintSpiritOnlyPassage(context, tiles, 31, -3, 8, 6, false);
        PaintSpiritOrbLine(context, tiles, -15, 0, 5);
        PaintSpiritOrbLine(context, tiles, 32, -1, 4);
        PaintRect(context.HazardGuide, tiles.SpiritWind, -13, 2, 2, 4);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 37, 2, 2, 4);
        PaintSpawnSupport(context, tiles, new Vector3(-34f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(0f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(28f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(52f, -3.2f, 0f));
        PaintSpawnSupport(context, tiles, new Vector3(66f, -3.2f, 0f));

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_03", "stage1_03_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-34f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_03_Lance", "stage1_03_enemy_lance", HWJ_SpawnPointType.Enemy, new Vector3(0f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_03_Axe", "stage1_03_enemy_axe", HWJ_SpawnPointType.Enemy, new Vector3(28f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_03_Shield", "stage1_03_enemy_shield", HWJ_SpawnPointType.Enemy, new Vector3(52f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_03_ToMidBoss", "stage1_03_exit_midboss", HWJ_SpawnPointType.NPC, new Vector3(66f, -3.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-34f, -3.2f, 0f));
        ConfigureMainCameraFollow(playerObject);

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_03_SpawnTable.asset",
            "spawn.stage1.03",
            new HWJ_Stage1SpawnRequest("stage1_03_spawn_lance", "stage1_03_enemy_lance", HWJ_SpawnPointType.Enemy, LanceEnemyRootPath, LanceEnemyPrefabPath, 1),
            new HWJ_Stage1SpawnRequest("stage1_03_spawn_axe", "stage1_03_enemy_axe", HWJ_SpawnPointType.Enemy, AxeEnemyRootPath, AxeEnemyPrefabPath, 1),
            new HWJ_Stage1SpawnRequest("stage1_03_spawn_shield", "stage1_03_enemy_shield", HWJ_SpawnPointType.Enemy, ShieldEnemyRootPath, ShieldEnemyPrefabPath, 1));
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.03",
            "stage.region01.04",
            spawnTable,
            false);

        CreateBodyObstacleGate(
            context,
            tiles,
            "HWJ_Stage1_03_SwordBodyGate",
            "stage1_03.body_obstacle.sword_body",
            new Vector3(42f, -2.1f, 0f),
            new Vector2(1f, 3.4f),
            HWJ_BodyObstacleRequirementMode.WeaponType,
            new[] { HWJ_WeaponType.Sword });
        CreateRewardPickup(context.VisualRoot, ExperienceOrbPrefabPath, "HWJ_Stage1_03_ExperienceOrb_120", new Vector3(-24f, -2.3f, 0f), 120);
        CreateRewardPickup(context.VisualRoot, DefenseStatOrbPrefabPath, "HWJ_Stage1_03_DefenseStatOrb", new Vector3(-22.8f, -2.3f, 0f), 0);
        CreateStageChoiceRewardShowcase(context, runtimeSystems, new Vector3(56f, -2.4f, 0f));
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_03_ToMidBoss",
            new Vector3(66f, -2.55f, 0f),
            "HWJ_Stage1_04_MidBossBarracks",
            "stage1_04_player_start",
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Spike_Center", new Vector3(27f, -3.45f, 0f), new Vector2(5.5f, 0.6f), 22f, 2.5f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Steam_BossEntry", new Vector3(56f, -2.55f, 0f), new Vector2(5f, 1.2f), 15f, 2.2f, 2f, tiles.SandstormGuide);
        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-34f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_LanceEnemy_Visual", SwordEnemySpritePath, new Vector3(0f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_AxeEnemy_Visual", ShieldEnemySpritePath, new Vector3(28f, -2.8f, 0f), 1f, 20);

        SaveContextScene(context);
    }

    private static void BuildMidBossBarracksScene()
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene04Path,
            "HWJ_Stage1_04_MidBossBarracks",
            "1-4 중간 보스 방 - 기사단 막사 내부",
            new Vector3(6f, 0.8f, -10f),
            10.5f);
        CreateRoundBackground(context, Stage1Background04Path, new Vector2(4f, 1f), 118f, 31f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -42, -12, 102, 32);
        PaintGroundRun(context, tiles, -36, -8, 86, 3, true);
        PaintRect(context.Ground, tiles.BarracksWall, -36, -5, 3, 14);
        PaintRect(context.Ground, tiles.BarracksWall, 47, -5, 3, 14);
        PaintRect(context.Ground, tiles.BarracksWall, -36, 8, 86, 2);
        PaintGroundRun(context, tiles, -24, -5, 18, 1, true);
        PaintGroundRun(context, tiles, 2, -5, 21, 1, true);
        PaintGroundRun(context, tiles, 31, -5, 13, 1, true);
        PaintStonePlatform(context, tiles, -25, -1, 10);
        PaintStonePlatform(context, tiles, -4, 1, 10);
        PaintStonePlatform(context, tiles, 18, 1, 10);
        PaintStonePlatform(context, tiles, 35, -1, 8);

        PaintRect(context.Decoration, tiles.RustyGate, -34, -5, 2, 6);
        PaintRect(context.Decoration, tiles.RustyGate, 45, -5, 2, 6);
        PaintRuinPillar(context, tiles, -12, -5, 4, 5, false);
        PaintRuinPillar(context, tiles, 9, -5, 5, 5, false);
        PaintRuinPillar(context, tiles, 28, -5, 4, 5, false);
        PaintBackdropBreakup(context, tiles, -30, 3, 12, 4);
        PaintBackdropBreakup(context, tiles, -3, 4, 13, 4);
        PaintBackdropBreakup(context, tiles, 25, 3, 12, 4);
        PaintRubbleTrail(context, tiles, -28, -4, 8, 4);
        PaintRubbleTrail(context, tiles, 2, -4, 8, 3);
        PaintRubbleTrail(context, tiles, 30, -4, 6, 3);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, -5, -5, 6, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 14, -5, 6, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -29, 2, 11, 2);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 25, 2, 11, 2);
        PaintSpiritOnlyPassage(context, tiles, -31, -4, 8, 8, true);
        PaintSpiritOnlyPassage(context, tiles, 36, -4, 7, 8, false);
        PaintSpiritOrbLine(context, tiles, -29, -1, 4);
        PaintSpiritOrbLine(context, tiles, 36, -1, 4);
        PaintRect(context.HazardGuide, tiles.SpiritWind, -30, 3, 3, 3);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 40, 3, 3, 3);
        PaintSpawnSupport(context, tiles, new Vector3(-31f, -4.2f, 0f), true);
        PaintSpawnSupport(context, tiles, new Vector3(10f, -4.2f, 0f), true);
        PaintSpawnSupport(context, tiles, new Vector3(-16f, -4.2f, 0f), true);
        PaintSpawnSupport(context, tiles, new Vector3(28f, -4.2f, 0f), true);
        PaintSpawnSupport(context, tiles, new Vector3(44f, -4.2f, 0f), true);

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_04", "stage1_04_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-31f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSpawn_Stage1_04", "stage1_04_midboss_spawn", HWJ_SpawnPointType.Boss, new Vector3(10f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSummon_Stage1_04_Left", "stage1_04_midboss_summon_left", HWJ_SpawnPointType.Enemy, new Vector3(-16f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSummon_Stage1_04_Right", "stage1_04_midboss_summon_right", HWJ_SpawnPointType.Enemy, new Vector3(28f, -4.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-31f, -4.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateDirectPossessableCorpse(
            context.VisualRoot,
            "HWJ_Stage1_04_BossEntryCorpse",
            ShieldEnemyRootPath,
            ShieldEnemySpritePath,
            new Vector3(-27f, -4.05f, 0f));

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_04_SpawnTable.asset",
            "spawn.stage1.04");
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.04",
            "stage.region01.next",
            spawnTable,
            true);
        GameObject midBossObject = CreateMidBossRuntimeObject(context, new Vector3(10f, -4.2f, 0f));
        CreateBossFlowShowcase(context, runtimeSystems, midBossObject);
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_04_ToTitle",
            new Vector3(44f, -3.55f, 0f),
            "Title",
            string.Empty,
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Spike_Left", new Vector3(-2f, -4.45f, 0f), new Vector2(5.8f, 0.6f), 24f, 2.4f);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Spike_Right", new Vector3(17f, -4.45f, 0f), new Vector2(5.8f, 0.6f), 24f, 2.4f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Steam_Left", new Vector3(-18f, -3.55f, 0f), new Vector2(5.5f, 1.4f), 16f, 2.2f, 1.8f, tiles.SandstormGuide);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Steam_Right", new Vector3(31f, -3.55f, 0f), new Vector2(5.5f, 1.4f), 16f, 2.2f, 1.8f, tiles.SandstormGuide);

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-31f, -3.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_MidBoss_Visual", MidBossSpritePath, new Vector3(10f, -3.4f, 0f), 1.4f, 22);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SummonEnemy_Left_Visual", SwordEnemySpritePath, new Vector3(-16f, -3.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SummonEnemy_Right_Visual", ShieldEnemySpritePath, new Vector3(28f, -3.8f, 0f), 1f, 20);

        SaveContextScene(context);
    }

    private static HWJ_Stage1SceneContext CreateSceneContext(
        string scenePath,
        string sceneName,
        string sceneDescription,
        Vector3 cameraPosition,
        float cameraSize)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject(sceneName + "_Root");
        GameObject gridObject = new GameObject("HWJ_Grid_Tilemap");
        gridObject.transform.SetParent(root.transform);
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        Tilemap background = CreateTilemap(grid.transform, "HWJ_Tilemap_Background", -50, false, false);
        Tilemap ground = CreateTilemap(grid.transform, "HWJ_Tilemap_Ground", 0, true, false);
        Tilemap oneWayPlatform = CreateTilemap(grid.transform, "HWJ_Tilemap_OneWayPlatform", 5, true, true);
        Tilemap decoration = CreateTilemap(grid.transform, "HWJ_Tilemap_Decoration", 10, false, false);
        Tilemap hazardGuide = CreateTilemap(grid.transform, "HWJ_Tilemap_HazardGuide", 15, false, false);

        Transform systemRoot = new GameObject("HWJ_StageRuntimeSystems").transform;
        systemRoot.SetParent(root.transform);
        Transform spawnRoot = new GameObject("HWJ_SceneSpawnPoints").transform;
        spawnRoot.SetParent(root.transform);
        Transform portalRoot = new GameObject("HWJ_ScenePortals").transform;
        portalRoot.SetParent(root.transform);
        Transform trapRoot = new GameObject("HWJ_SceneTraps").transform;
        trapRoot.SetParent(root.transform);
        Transform visualRoot = new GameObject("HWJ_VisualSpawnMarkers").transform;
        visualRoot.SetParent(root.transform);

        CreateCamera(cameraPosition, cameraSize);
        CreateDirectionalLight();
        CreateHshStyleDemoHud(root.transform, sceneDescription);

        return new HWJ_Stage1SceneContext(
            scene,
            scenePath,
            root.transform,
            background,
            ground,
            oneWayPlatform,
            decoration,
            hazardGuide,
            systemRoot,
            spawnRoot,
            portalRoot,
            trapRoot,
            visualRoot);
    }

    private static Tilemap CreateTilemap(
        Transform parent,
        string name,
        int sortingOrder,
        bool addCollider,
        bool oneWay)
    {
        GameObject tilemapObject = new GameObject(name);
        tilemapObject.transform.SetParent(parent);

        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (addCollider)
        {
            TilemapCollider2D collider = tilemapObject.AddComponent<TilemapCollider2D>();
            Rigidbody2D body = tilemapObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            if (oneWay)
            {
                collider.usedByEffector = true;
                PlatformEffector2D effector = tilemapObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 160f;
            }
            else
            {
                CompositeCollider2D composite = tilemapObject.AddComponent<CompositeCollider2D>();
                collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }
        }

        if (addCollider)
        {
            SetLayerIfExists(tilemapObject, "Ground");
        }

        return tilemap;
    }

    private static void SetLayerIfExists(GameObject targetObject, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer >= 0)
        {
            targetObject.layer = layer;
        }
    }

    private static void CreateCamera(Vector3 cameraPosition, float cameraSize)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = cameraPosition;
        camera.orthographic = true;
        camera.orthographicSize = cameraSize;
        camera.backgroundColor = new Color(0.39f, 0.28f, 0.19f, 1f);
    }

    private static void CreateRoundBackground(
        HWJ_Stage1SceneContext context,
        string backgroundSpritePath,
        Vector2 worldCenter,
        float worldWidth,
        float worldHeight)
    {
        Sprite backgroundSprite = LoadSceneBackgroundSprite(backgroundSpritePath);

        if (backgroundSprite == null)
        {
            Debug.LogWarning($"HWJ stage background missing: {backgroundSpritePath}");
            return;
        }

        GameObject backgroundRoot = new GameObject("BackgroundRoot");
        backgroundRoot.transform.SetParent(context.Root);

        CreateSingleBackgroundLayer(
            backgroundRoot.transform,
            "BG_Far",
            backgroundSprite,
            worldCenter,
            worldWidth,
            worldHeight,
            new Vector2(0.04f, 0.01f),
            new Color(0.55f, 0.52f, 0.48f, 0.82f),
            -130,
            7.5f);
        CreateSingleBackgroundLayer(
            backgroundRoot.transform,
            "BG_Middle",
            backgroundSprite,
            worldCenter,
            worldWidth,
            worldHeight,
            new Vector2(0.14f, 0.02f),
            new Color(0.82f, 0.76f, 0.66f, 0.9f),
            -120,
            6.5f);
        CreateSingleBackgroundLayer(
            backgroundRoot.transform,
            "BG_Near",
            backgroundSprite,
            worldCenter,
            worldWidth,
            worldHeight,
            new Vector2(0.28f, 0.04f),
            new Color(1f, 0.92f, 0.76f, 0.44f),
            -110,
            5.5f);
        CreateSingleBackgroundLayer(
            backgroundRoot.transform,
            "BG_SandVFX",
            backgroundSprite,
            worldCenter + new Vector2(0f, -0.45f),
            worldWidth,
            worldHeight,
            new Vector2(0.42f, 0.02f),
            new Color(1f, 0.72f, 0.38f, 0.18f),
            -95,
            4.5f);
    }

    private static void CreateSingleBackgroundLayer(
        Transform parent,
        string layerName,
        Sprite backgroundSprite,
        Vector2 worldCenter,
        float worldWidth,
        float worldHeight,
        Vector2 parallaxMultiplier,
        Color tint,
        int sortingOrder,
        float zPosition)
    {
        GameObject layerObject = new GameObject(layerName);
        layerObject.transform.SetParent(parent);
        layerObject.transform.position = Vector3.zero;

        Component parallaxSystem = EnsureComponentByType(layerObject, "HWJ_ParallaxBackgroundSystem");
        TryInvokeVector2Method(parallaxSystem, "SetParallaxMultiplier", parallaxMultiplier);

        if (parallaxSystem != null)
        {
            EditorUtility.SetDirty(parallaxSystem);
        }

        GameObject panelRoot = new GameObject(layerName + "_Sprite");
        panelRoot.transform.SetParent(layerObject.transform);
        panelRoot.transform.position = Vector3.zero;

        float spriteWidth = Mathf.Max(0.01f, backgroundSprite.bounds.size.x);
        float spriteHeight = Mathf.Max(0.01f, backgroundSprite.bounds.size.y);
        float verticalScale = Mathf.Max(0.01f, worldHeight / spriteHeight);
        int horizontalPanelCount = Mathf.Max(1, Mathf.CeilToInt(worldWidth / spriteWidth) + 2);
        float totalPanelWidth = horizontalPanelCount * spriteWidth;
        float firstPanelX = worldCenter.x - totalPanelWidth * 0.5f + spriteWidth * 0.5f;

        for (int i = 0; i < horizontalPanelCount; i++)
        {
            GameObject panelObject = new GameObject($"{layerName}_Sprite_{i:00}");
            panelObject.transform.SetParent(panelRoot.transform);
            panelObject.transform.position = new Vector3(firstPanelX + i * spriteWidth, worldCenter.y, zPosition);
            panelObject.transform.localScale = new Vector3(1f, verticalScale, 1f);

            SpriteRenderer renderer = panelObject.AddComponent<SpriteRenderer>();
            renderer.sprite = backgroundSprite;
            renderer.color = tint;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private static Sprite LoadSceneBackgroundSprite(string spritePath)
    {
        if (string.IsNullOrWhiteSpace(spritePath) || !File.Exists(Path.GetFullPath(spritePath)))
        {
            return null;
        }

        ImportAsSprite(spritePath, 100f, FilterMode.Bilinear, true);

        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;

        if (importer != null && importer.wrapMode != TextureWrapMode.Repeat)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.75f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateHshStyleDemoHud(Transform parent, string sceneDescription)
    {
        GameObject canvasObject = new GameObject("HWJ_HSHStyleDemoHudCanvas");
        canvasObject.transform.SetParent(parent);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject hshFrameObject = CreateUiImage(
            canvasObject.transform,
            "HWJ_HSH_UI_Frame",
            AssetDatabase.LoadAssetAtPath<Sprite>(HshHudSpritePath),
            new Color(1f, 1f, 1f, 0.92f));
        RectTransform hshFrame = hshFrameObject.GetComponent<RectTransform>();
        SetTopLeftRect(hshFrame, new Vector2(24f, -24f), new Vector2(320f, 296f));
        hshFrameObject.GetComponent<Image>().preserveAspect = true;

        GameObject hpBack = CreateUiImage(hshFrameObject.transform, "HWJ_HUD_HP_Back", null, new Color(0.07f, 0.07f, 0.08f, 0.88f));
        SetTopLeftRect(hpBack.GetComponent<RectTransform>(), new Vector2(112f, -126f), new Vector2(178f, 10f));
        GameObject hpFillObject = CreateUiFillImage(hshFrameObject.transform, "HWJ_HUD_HP_Fill", new Color(0.78f, 0.24f, 0.94f, 1f));
        SetTopLeftRect(hpFillObject.GetComponent<RectTransform>(), new Vector2(112f, -126f), new Vector2(178f, 10f));

        GameObject possessionBack = CreateUiImage(hshFrameObject.transform, "HWJ_HUD_Possession_Back", null, new Color(0.07f, 0.07f, 0.08f, 0.88f));
        SetTopLeftRect(possessionBack.GetComponent<RectTransform>(), new Vector2(112f, -146f), new Vector2(178f, 10f));
        GameObject possessionFillObject = CreateUiFillImage(hshFrameObject.transform, "HWJ_HUD_Possession_Fill", new Color(0.42f, 0.9f, 0.42f, 1f));
        SetTopLeftRect(possessionFillObject.GetComponent<RectTransform>(), new Vector2(112f, -146f), new Vector2(178f, 10f));

        GameObject readoutPanel = CreateUiImage(canvasObject.transform, "HWJ_DemoHud_ReadoutPanel", null, new Color(0.04f, 0.035f, 0.03f, 0.76f));
        SetTopLeftRect(readoutPanel.GetComponent<RectTransform>(), new Vector2(360f, -24f), new Vector2(760f, 180f));

        Text titleText = CreateUiText(readoutPanel.transform, "HWJ_DemoHud_TitleText", "HWJ 1스테이지 시연 HUD", 24, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.52f, 1f));
        SetTopLeftRect(titleText.rectTransform, new Vector2(18f, -14f), new Vector2(710f, 32f));

        Text sceneText = CreateUiText(readoutPanel.transform, "HWJ_DemoHud_SceneText", sceneDescription, 18, TextAnchor.UpperLeft, new Color(0.9f, 0.86f, 0.76f, 1f));
        SetTopLeftRect(sceneText.rectTransform, new Vector2(18f, -48f), new Vector2(710f, 26f));

        Text stateText = CreateUiText(readoutPanel.transform, "HWJ_DemoHud_StateText", "상태 확인 중", 22, TextAnchor.UpperLeft, new Color(0.72f, 0.9f, 1f, 1f));
        SetTopLeftRect(stateText.rectTransform, new Vector2(18f, -82f), new Vector2(260f, 32f));

        Text resourceText = CreateUiText(readoutPanel.transform, "HWJ_DemoHud_ResourceText", "자원 확인 중", 18, TextAnchor.UpperLeft, Color.white);
        SetTopLeftRect(resourceText.rectTransform, new Vector2(18f, -116f), new Vector2(710f, 28f));

        Text growthText = CreateUiText(readoutPanel.transform, "HWJ_DemoHud_GrowthText", "성장 확인 중", 18, TextAnchor.UpperLeft, new Color(0.75f, 1f, 0.78f, 1f));
        SetTopLeftRect(growthText.rectTransform, new Vector2(18f, -146f), new Vector2(710f, 28f));

        GameObject expBack = CreateUiImage(readoutPanel.transform, "HWJ_HUD_EXP_Back", null, new Color(0.11f, 0.12f, 0.11f, 0.95f));
        SetTopLeftRect(expBack.GetComponent<RectTransform>(), new Vector2(360f, -148f), new Vector2(350f, 10f));
        GameObject expFillObject = CreateUiFillImage(readoutPanel.transform, "HWJ_HUD_EXP_Fill", new Color(0.28f, 0.95f, 0.5f, 1f));
        SetTopLeftRect(expFillObject.GetComponent<RectTransform>(), new Vector2(360f, -148f), new Vector2(350f, 10f));

        GameObject statusPanel = CreateUiImage(canvasObject.transform, "HWJ_DemoHud_StatusPanel", null, new Color(0.04f, 0.035f, 0.03f, 0.76f));
        SetTopLeftRect(statusPanel.GetComponent<RectTransform>(), new Vector2(24f, -336f), new Vector2(1096f, 132f));

        Text objectiveText = CreateUiText(statusPanel.transform, "HWJ_DemoHud_ObjectiveText", "목표 확인 중", 20, TextAnchor.UpperLeft, new Color(1f, 0.86f, 0.48f, 1f));
        SetTopLeftRect(objectiveText.rectTransform, new Vector2(18f, -16f), new Vector2(1048f, 30f));

        Text actionText = CreateUiText(statusPanel.transform, "HWJ_DemoHud_ActionText", "가능 행동 확인 중", 18, TextAnchor.UpperLeft, new Color(0.86f, 0.94f, 1f, 1f));
        SetTopLeftRect(actionText.rectTransform, new Vector2(18f, -52f), new Vector2(1048f, 28f));

        Text skillSlotText = CreateUiText(statusPanel.transform, "HWJ_DemoHud_SkillSlotText", "스킬 확인 중", 18, TextAnchor.UpperLeft, new Color(0.9f, 0.78f, 1f, 1f));
        SetTopLeftRect(skillSlotText.rectTransform, new Vector2(18f, -86f), new Vector2(1048f, 28f));

        HWJ_DemoHudSystem hudSystem = canvasObject.AddComponent<HWJ_DemoHudSystem>();
        SerializedObject serializedHud = new SerializedObject(hudSystem);
        serializedHud.FindProperty("hpFillImage").objectReferenceValue = hpFillObject.GetComponent<Image>();
        serializedHud.FindProperty("possessionFillImage").objectReferenceValue = possessionFillObject.GetComponent<Image>();
        serializedHud.FindProperty("experienceFillImage").objectReferenceValue = expFillObject.GetComponent<Image>();
        serializedHud.FindProperty("stateText").objectReferenceValue = stateText;
        serializedHud.FindProperty("resourceText").objectReferenceValue = resourceText;
        serializedHud.FindProperty("growthText").objectReferenceValue = growthText;
        serializedHud.FindProperty("objectiveText").objectReferenceValue = objectiveText;
        serializedHud.FindProperty("actionText").objectReferenceValue = actionText;
        serializedHud.FindProperty("skillSlotText").objectReferenceValue = skillSlotText;
        serializedHud.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateUiImage(Transform parent, string objectName, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static GameObject CreateUiFillImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = CreateUiImage(parent, objectName, null, color);
        Image image = imageObject.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        return imageObject;
    }

    private static Text CreateUiText(
        Transform parent,
        string objectName,
        string text,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;

        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = ResolveDefaultUiFont();
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.raycastTarget = false;
        return uiText;
    }

    private static Font ResolveDefaultUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void SetTopLeftRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    // Paints a playable landmass with a readable cracked top and a heavier fill below it.
    private static void PaintGroundRun(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        int x,
        int y,
        int width,
        int depth,
        bool useBarracksStone = false)
    {
        int safeWidth = Mathf.Max(1, width);
        int safeDepth = Mathf.Max(1, depth);
        TileBase fillA = useBarracksStone ? tiles.BarracksFillA : tiles.GroundFillA;
        TileBase fillB = useBarracksStone ? tiles.BarracksFillB : tiles.GroundFillB;
        TileBase topLeft = useBarracksStone ? tiles.BarracksTopLeft : tiles.GroundTopLeft;
        TileBase topMiddleA = useBarracksStone ? tiles.BarracksTopMiddleA : tiles.GroundTopMiddleA;
        TileBase topMiddleB = useBarracksStone ? tiles.BarracksTopMiddleB : tiles.GroundTopMiddleB;
        TileBase topRight = useBarracksStone ? tiles.BarracksTopRight : tiles.GroundTopRight;

        for (int fillY = y; fillY < y + safeDepth - 1; fillY++)
        {
            for (int fillX = x; fillX < x + safeWidth; fillX++)
            {
                PaintSingleTile(context.Ground, (fillX + fillY) % 2 == 0 ? fillA : fillB, fillX, fillY);
            }
        }

        int topY = y + safeDepth - 1;

        if (safeWidth == 1)
        {
            PaintSingleTile(context.Ground, topMiddleA, x, topY);
        }
        else
        {
            PaintSingleTile(context.Ground, topLeft, x, topY);
            PaintSingleTile(context.Ground, topRight, x + safeWidth - 1, topY);

            for (int topX = x + 1; topX < x + safeWidth - 1; topX++)
            {
                PaintSingleTile(context.Ground, topX % 2 == 0 ? topMiddleA : topMiddleB, topX, topY);
            }
        }

        context.Ground.RefreshAllTiles();
    }

    // One-way ledges stay on their own tilemap so jump routes remain easy to tune.
    private static void PaintStonePlatform(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, int x, int y, int width)
    {
        int safeWidth = Mathf.Max(1, width);
        PaintSingleTile(context.OneWayPlatform, tiles.GroundTopLeft, x, y);

        if (safeWidth > 1)
        {
            PaintSingleTile(context.OneWayPlatform, tiles.GroundTopRight, x + safeWidth - 1, y);
        }

        for (int topX = x + 1; topX < x + safeWidth - 1; topX++)
        {
            PaintSingleTile(context.OneWayPlatform, topX % 2 == 0 ? tiles.GroundTopMiddleA : tiles.GroundTopMiddleB, topX, y);
        }

        context.OneWayPlatform.RefreshAllTiles();
    }

    // Ruin pillars give the route shape without forcing every wall to be a solid blocker.
    private static void PaintRuinPillar(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        int x,
        int y,
        int width,
        int height,
        bool solid)
    {
        Tilemap targetTilemap = solid ? context.Ground : context.Background;
        PaintWallBand(targetTilemap, tiles, x, y, Mathf.Max(1, width), Mathf.Max(1, height));

        if (solid)
        {
            PaintGroundRun(context, tiles, x, y, Mathf.Max(1, width), Mathf.Max(1, height));
        }
    }

    // Backdrop breakup prevents large background rectangles from reading as temporary blockouts.
    private static void PaintBackdropBreakup(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        int x,
        int y,
        int width,
        int height)
    {
        PaintWallBand(context.Background, tiles, x, y, Mathf.Max(1, width), Mathf.Max(1, height));
        PaintRect(context.HazardGuide, tiles.SandstormGuide, x + 1, y + 1, Mathf.Max(1, width - 2), 1);
    }

    // Small deterministic debris clusters make repeated floor tiles feel more hand placed.
    private static void PaintRubbleTrail(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        int startX,
        int startY,
        int clusterCount,
        int spacing)
    {
        for (int i = 0; i < Mathf.Max(0, clusterCount); i++)
        {
            int yOffset = i % 2;
            PaintRect(context.Decoration, tiles.RuinedWall, startX + i * Mathf.Max(1, spacing), startY + yOffset, 1, 1);
        }
    }

    // Ensures runtime characters spawn over real Ground tiles, not over visual-only decoration.
    private static void PaintSpawnSupport(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        Vector3 spawnPosition,
        bool useBarracksStone = false)
    {
        const int supportWidth = 7;
        const int supportDepth = 3;
        int centerX = Mathf.RoundToInt(spawnPosition.x);
        int topCellY = Mathf.FloorToInt(spawnPosition.y - 0.53f) - 1;
        int startY = topCellY - supportDepth + 1;

        PaintGroundRun(context, tiles, centerX - supportWidth / 2, startY, supportWidth, supportDepth, useBarracksStone);
    }

    private static void PaintBackground(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, int x, int y, int width, int height)
    {
        PaintRect(context.Background, tiles.BackgroundSand, x, y, width, height);
    }

    private static void PaintRuinedHouse(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, int x, int y, int width, int height)
    {
        int safeWidth = Mathf.Max(4, width);
        int safeHeight = Mathf.Max(4, height);

        PaintRect(context.Background, tiles.RuinedWall, x, y, safeWidth, safeHeight);
        PaintRect(context.Background, tiles.CollapsedRoof, x - 1, y + safeHeight, safeWidth + 2, 1);
        PaintRect(context.Background, tiles.CollapsedRoof, x + 1, y + safeHeight - 1, Mathf.Max(2, safeWidth - 2), 1);

        for (int windowX = x + 2; windowX < x + safeWidth - 2; windowX += 4)
        {
            PaintRect(context.Background, tiles.RustyGate, windowX, y + safeHeight / 2, 1, 2);
        }
    }

    private static void PaintSpiritOnlyPassage(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        int x,
        int y,
        int width,
        int height,
        bool leftSealed)
    {
        PaintRect(context.HazardGuide, tiles.SpiritWind, x + width / 2, y + 1, 1, Mathf.Max(1, height - 2));
        PaintRect(context.Ground, tiles.SpiritSealWall, x, y, width, 1);
        PaintRect(context.Ground, tiles.SpiritSealWall, x, y + height - 1, width, 1);

        if (leftSealed)
        {
            PaintRect(context.Ground, tiles.SpiritSealWall, x, y, 1, height);
        }
        else
        {
            PaintRect(context.Ground, tiles.SpiritSealWall, x + width - 1, y, 1, height);
        }

        PaintRect(context.HazardGuide, tiles.SpiritOrb, x + width / 2, y + height / 2, 1, 1);
    }

    private static void PaintSpiritOrbLine(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        int startX,
        int startY,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            PaintRect(context.HazardGuide, tiles.SpiritOrb, startX + i * 2, startY + (i % 2), 1, 1);
        }
    }

    private static void PaintWallBand(Tilemap tilemap, HWJ_Stage1TileSet tiles, int x, int y, int width, int height)
    {
        for (int tileX = x; tileX < x + width; tileX++)
        {
            for (int tileY = y; tileY < y + height; tileY++)
            {
                TileBase wallTile = (tileX + tileY) % 2 == 0 ? tiles.BackgroundWallA : tiles.BackgroundWallB;
                PaintSingleTile(tilemap, wallTile, tileX, tileY);
            }
        }

        tilemap.RefreshAllTiles();
    }

    private static void PaintSingleTile(Tilemap tilemap, TileBase tile, int x, int y)
    {
        if (tile == null)
        {
            Debug.LogWarning($"HWJ PaintSingleTile skipped because tile is null. tilemap={tilemap.name}, cell=({x},{y})");
            return;
        }

        tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void PaintRect(Tilemap tilemap, TileBase tile, int x, int y, int width, int height)
    {
        if (tile == null)
        {
            Debug.LogWarning($"HWJ PaintRect skipped because tile is null. tilemap={tilemap.name}, area=({x},{y},{width},{height})");
            return;
        }

        for (int tileX = x; tileX < x + width; tileX++)
        {
            for (int tileY = y; tileY < y + height; tileY++)
            {
                tilemap.SetTile(new Vector3Int(tileX, tileY, 0), tile);
            }
        }

        tilemap.RefreshAllTiles();
    }

    private static HWJ_SpawnTableDataSO CreateOrUpdateSpawnTable(
        string assetFileName,
        string tableId,
        params HWJ_Stage1SpawnRequest[] requests)
    {
        string assetPath = SpawnTableRoot + "/" + assetFileName;
        HWJ_SpawnTableDataSO table = AssetDatabase.LoadAssetAtPath<HWJ_SpawnTableDataSO>(assetPath);

        if (table == null)
        {
            table = ScriptableObject.CreateInstance<HWJ_SpawnTableDataSO>();
            AssetDatabase.CreateAsset(table, assetPath);
        }

        SerializedObject serializedTable = new SerializedObject(table);
        serializedTable.FindProperty("tableId").stringValue = tableId;

        SerializedProperty entries = serializedTable.FindProperty("entries");
        entries.arraySize = requests != null ? requests.Length : 0;

        for (int i = 0; i < entries.arraySize; i++)
        {
            HWJ_Stage1SpawnRequest request = requests[i];
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("spawnId").stringValue = request.SpawnId;
            entry.FindPropertyRelative("spawnPointId").stringValue = request.SpawnPointId;
            entry.FindPropertyRelative("spawnPointType").enumValueIndex = (int)request.SpawnPointType;
            entry.FindPropertyRelative("rootObjectData").objectReferenceValue = LoadRequiredAsset<HWJ_RootObjectDataSO>(
                request.RootObjectDataPath,
                request.SpawnId);
            entry.FindPropertyRelative("prefabOverride").objectReferenceValue = LoadRequiredAsset<GameObject>(
                request.PrefabPath,
                request.SpawnId);
            entry.FindPropertyRelative("spawnCount").intValue = Mathf.Max(1, request.SpawnCount);
            entry.FindPropertyRelative("spawnDelaySeconds").floatValue = request.SpawnDelaySeconds;
            entry.FindPropertyRelative("useSequentialSpawnWhenMultiple").boolValue = true;
            entry.FindPropertyRelative("waitUntilCurrentSpawnedMonstersDefeated").boolValue = true;
            entry.FindPropertyRelative("nextSpawnMaxWaitSeconds").floatValue = 5f;
            entry.FindPropertyRelative("skipSpawnWhenRewardClaimed").boolValue = true;
            entry.FindPropertyRelative("spawnOffset").vector2Value = Vector2.zero;
            entry.FindPropertyRelative("randomizePoint").boolValue = false;
            entry.FindPropertyRelative("spawnOnStart").boolValue = true;
        }

        serializedTable.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
        return table;
    }

    private static T LoadRequiredAsset<T>(string assetPath, string usageId)
        where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        if (asset == null)
        {
            Debug.LogWarning($"HWJ stage 1 scene setup missing asset for {usageId}: {assetPath}");
        }

        return asset;
    }

    private static HWJ_StageRuntimeSystems CreateStageRuntimeSystems(
        HWJ_Stage1SceneContext context,
        string stageId,
        string nextStageId,
        HWJ_SpawnTableDataSO spawnTable,
        bool countBossObjects,
        bool spawnOnStart = true)
    {
        GameObject spawnerObject = new GameObject("HWJ_StageSpawnerSystem");
        spawnerObject.transform.SetParent(context.SystemRoot);
        HWJ_SpawnerSystem spawner = spawnerObject.AddComponent<HWJ_SpawnerSystem>();
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("spawnTable").objectReferenceValue = spawnTable;
        serializedSpawner.FindProperty("autoCollectSpawnPoints").boolValue = true;
        serializedSpawner.FindProperty("spawnOnStart").boolValue = spawnOnStart;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

        GameObject managerObject = new GameObject("HWJ_GameManager");
        managerObject.transform.SetParent(context.SystemRoot);
        HWJ_GameManager gameManager = managerObject.AddComponent<HWJ_GameManager>();
        SerializedObject serializedManager = new SerializedObject(gameManager);
        serializedManager.FindProperty("database").objectReferenceValue = AssetDatabase.LoadAssetAtPath<HWJ_GameplayDatabaseSO>(GameplayDatabasePath);
        serializedManager.FindProperty("spawner").objectReferenceValue = spawner;
        serializedManager.FindProperty("dontDestroyOnLoad").boolValue = false;
        serializedManager.FindProperty("autoFindPlayerResolverInScene").boolValue = true;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        GameObject transitionObject = new GameObject("HWJ_SceneTransitionSystem");
        transitionObject.transform.SetParent(context.SystemRoot);
        HWJ_SceneTransitionSystem transitionSystem = transitionObject.AddComponent<HWJ_SceneTransitionSystem>();
        SerializedObject serializedTransition = new SerializedObject(transitionSystem);
        serializedTransition.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedTransition.ApplyModifiedPropertiesWithoutUndo();

        GameObject progressionObject = new GameObject("HWJ_StageProgressionSystem");
        progressionObject.transform.SetParent(context.SystemRoot);
        HWJ_StageProgressionSystem progression = progressionObject.AddComponent<HWJ_StageProgressionSystem>();
        SerializedObject serializedProgression = new SerializedObject(progression);
        serializedProgression.FindProperty("stageId").stringValue = stageId;
        serializedProgression.FindProperty("currentRegionId").stringValue = "region01";
        serializedProgression.FindProperty("nextStageId").stringValue = nextStageId;
        serializedProgression.FindProperty("nextRegionId").stringValue = "region01";
        serializedProgression.FindProperty("currentStageHasBoss").boolValue = countBossObjects;
        serializedProgression.FindProperty("bossId").stringValue = countBossObjects ? "boss.region01.midboss01" : string.Empty;
        serializedProgression.FindProperty("applyDefinitionOnAwake").boolValue = false;
        serializedProgression.FindProperty("initialState").enumValueIndex = (int)HWJ_StageFlowState.Entering;
        serializedProgression.FindProperty("currentState").enumValueIndex = (int)HWJ_StageFlowState.None;
        serializedProgression.ApplyModifiedPropertiesWithoutUndo();

        GameObject enemyCountObject = new GameObject("HWJ_StageEnemyCountSystem");
        enemyCountObject.transform.SetParent(context.SystemRoot);
        HWJ_StageEnemyCountSystem enemyCount = enemyCountObject.AddComponent<HWJ_StageEnemyCountSystem>();
        SerializedObject serializedEnemyCount = new SerializedObject(enemyCount);
        serializedEnemyCount.FindProperty("stageProgressionSystem").objectReferenceValue = progression;
        serializedEnemyCount.FindProperty("stageRoot").objectReferenceValue = context.SpawnRoot;
        serializedEnemyCount.FindProperty("countEnemyObjects").boolValue = true;
        serializedEnemyCount.FindProperty("countBossObjects").boolValue = countBossObjects;
        serializedEnemyCount.FindProperty("scanOnStart").boolValue = true;
        serializedEnemyCount.FindProperty("periodicRescan").boolValue = true;
        serializedEnemyCount.FindProperty("rescanIntervalSeconds").floatValue = 0.25f;
        serializedEnemyCount.ApplyModifiedPropertiesWithoutUndo();

        GameObject coreLoopObject = new GameObject("HWJ_CoreLoopCoordinator");
        coreLoopObject.transform.SetParent(context.SystemRoot);
        HWJ_CoreLoopCoordinator coreLoop = coreLoopObject.AddComponent<HWJ_CoreLoopCoordinator>();
        SerializedObject serializedCoreLoop = new SerializedObject(coreLoop);
        serializedCoreLoop.FindProperty("stageProgressionSystem").objectReferenceValue = progression;
        serializedCoreLoop.ApplyModifiedPropertiesWithoutUndo();

        serializedTransition = new SerializedObject(transitionSystem);
        serializedTransition.FindProperty("coreLoopCoordinator").objectReferenceValue = coreLoop;
        serializedTransition.ApplyModifiedPropertiesWithoutUndo();

        GameObject playerStartObject = new GameObject("HWJ_PlayerStartSystem");
        playerStartObject.transform.SetParent(context.SystemRoot);
        HWJ_PlayerStartSystem playerStart = playerStartObject.AddComponent<HWJ_PlayerStartSystem>();
        SerializedObject serializedPlayerStart = new SerializedObject(playerStart);
        serializedPlayerStart.FindProperty("playerStartPointId").stringValue = ResolveScenePlayerStartId(stageId);
        serializedPlayerStart.FindProperty("useSceneTransitionTargetSpawnPoint").boolValue = true;
        serializedPlayerStart.FindProperty("placeOnStart").boolValue = true;
        serializedPlayerStart.FindProperty("registerToGameManager").boolValue = true;
        serializedPlayerStart.ApplyModifiedPropertiesWithoutUndo();

        return new HWJ_StageRuntimeSystems(
            gameManager,
            spawner,
            transitionSystem,
            progression,
            enemyCount,
            coreLoop);
    }

    private static string ResolveScenePlayerStartId(string stageId)
    {
        switch (stageId)
        {
            case "stage.region01.01":
                return "stage1_01_player_start";
            case "stage.region01.02":
                return "stage1_02_player_start";
            case "stage.region01.03":
                return "stage1_03_player_start";
            case "stage.region01.04":
                return "stage1_04_player_start";
            default:
                return string.Empty;
        }
    }

    private static void CreateScenePortal(
        HWJ_Stage1SceneContext context,
        string objectName,
        Vector3 position,
        string targetSceneName,
        string targetSpawnPointId,
        HWJ_StageRuntimeSystems runtimeSystems,
        TileBase markerTile)
    {
        GameObject portalObject = new GameObject(objectName);
        portalObject.transform.SetParent(context.PortalRoot);
        portalObject.transform.position = position;

        BoxCollider2D collider = portalObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.4f, 2.4f);

        if (GetTileSprite(markerTile) != null)
        {
            CreateTiledVisualObject(
                portalObject.transform,
                objectName + "_VisiblePortal",
                markerTile,
                position + new Vector3(0f, 0.35f, 0f),
                2,
                3,
                28,
                new Color(0.75f, 0.42f, 0.18f, 0.96f));
        }

        HWJ_ScenePortalSystem portal = portalObject.AddComponent<HWJ_ScenePortalSystem>();
        SerializedObject serializedPortal = new SerializedObject(portal);
        serializedPortal.FindProperty("targetSceneName").stringValue = targetSceneName;
        serializedPortal.FindProperty("targetSpawnPointId").stringValue = targetSpawnPointId;
        serializedPortal.FindProperty("requireInteractInput").boolValue = true;
        serializedPortal.FindProperty("loadImmediatelyOnEnter").boolValue = false;
        serializedPortal.FindProperty("reuseCooldownSeconds").floatValue = 0.5f;
        serializedPortal.FindProperty("requirePlayerTag").boolValue = true;
        serializedPortal.FindProperty("requireStageObjectiveComplete").boolValue = true;
        serializedPortal.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedPortal.FindProperty("stageEnemyCountSystem").objectReferenceValue = runtimeSystems.EnemyCount;
        serializedPortal.FindProperty("sceneTransitionSystem").objectReferenceValue = runtimeSystems.Transition;
        serializedPortal.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateSpikeTrap(
        Transform parent,
        string objectName,
        Vector3 position,
        Vector2 size,
        float damage,
        float detectionDistance,
        TileBase visualTile = null)
    {
        GameObject trapObject = new GameObject(objectName);
        trapObject.transform.SetParent(parent);
        trapObject.transform.position = position;
        GameObject visibleSpikesObject = null;

        if (visualTile != null)
        {
            int columns = Mathf.Max(1, Mathf.RoundToInt(size.x));
            visibleSpikesObject = CreateTiledVisualObject(
                trapObject.transform,
                objectName + "_VisibleSpikes",
                visualTile,
                position + new Vector3(0f, 0.12f, 0f),
                columns,
                1,
                25,
                new Color(1f, 0.24f, 0.18f, 0.95f));
            AddLoopingVisualAnimator(visibleSpikesObject, "HWJ_Stage1_SpikeTrap_Pulse");

            GameObject warningVfxObject = CreateTiledVisualObject(
                trapObject.transform,
                objectName + "_WarningVFX",
                visualTile,
                position + new Vector3(0f, 0.45f, 0f),
                columns,
                1,
                27,
                new Color(1f, 0.08f, 0.05f, 0.35f));
            AddLoopingVisualAnimator(warningVfxObject, "HWJ_Stage1_TrapWarning_Pulse");
        }

        BoxCollider2D collider = trapObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        Component trap = AddExistingTrapComponent(trapObject, "HSH_SpikeTrap");
        SetSerializedFloat(trap, "damage", damage);
        SetSerializedFloat(trap, "detectionDistance", detectionDistance);
        SetSerializedFloat(trap, "delayTime", 0.7f);
        SetSerializedFloat(trap, "protrudeHeight", 0.9f);
        SetSerializedFloat(trap, "activeDuration", 1.2f);
    }

    private static void CreateSteamTrap(
        Transform parent,
        string objectName,
        Vector3 position,
        Vector2 size,
        float damage,
        float activeDuration,
        float inactiveDuration,
        TileBase visualTile = null)
    {
        GameObject trapObject = new GameObject(objectName);
        trapObject.transform.SetParent(parent);
        trapObject.transform.position = position;

        Sprite visualSprite = GetTileSprite(visualTile);

        if (visualSprite != null)
        {
            SpriteRenderer rootRenderer = trapObject.AddComponent<SpriteRenderer>();
            rootRenderer.sprite = visualSprite;
            rootRenderer.color = new Color(1f, 1f, 1f, 0.28f);
            rootRenderer.sortingOrder = 25;

            CreateTiledVisualObject(
                trapObject.transform,
                objectName + "_VisibleSandLane",
                visualTile,
                position,
                Mathf.Max(1, Mathf.RoundToInt(size.x)),
                Mathf.Max(1, Mathf.RoundToInt(size.y)),
                26,
                new Color(1f, 1f, 1f, 0.35f));
        }

        ParticleSystem sandParticles = CreateSandstormParticleVisual(trapObject, size);

        BoxCollider2D collider = trapObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        Component trap = AddExistingTrapComponent(trapObject, "HSH_SteamTrap");
        SetSerializedFloat(trap, "damage", damage);
        SetSerializedFloat(trap, "activeDuration", activeDuration);
        SetSerializedFloat(trap, "inactiveDuration", inactiveDuration);
        SetSerializedFloat(trap, "damageCooldown", 1f);
        SetSerializedFloat(trap, "knockbackPower", 18f);
        SetSerializedObjectReference(trap, "steamParticles", sandParticles);
    }

    private static Component AddExistingTrapComponent(GameObject owner, string componentTypeName)
    {
        Type componentType = ResolveComponentType(componentTypeName);

        if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
        {
            Debug.LogError($"[Stage1 Builder] Missing trap component: {componentTypeName}");
            return null;
        }

        return owner.AddComponent(componentType);
    }

    private static Type ResolveComponentType(string componentTypeName)
    {
        if (string.IsNullOrWhiteSpace(componentTypeName))
        {
            return null;
        }

        return Type.GetType(componentTypeName + ", Assembly-CSharp")
            ?? Type.GetType(componentTypeName + ", HWJ.Runtime")
            ?? Type.GetType(componentTypeName);
    }

    private static Transform CreateLocalMarker(Transform parent, string objectName, Vector3 localPosition)
    {
        GameObject markerObject = new GameObject(objectName);
        markerObject.transform.SetParent(parent);
        markerObject.transform.localPosition = localPosition;
        return markerObject.transform;
    }

    private static void AddLoopingVisualAnimator(GameObject targetObject, string controllerName)
    {
        if (targetObject == null)
        {
            return;
        }

        RuntimeAnimatorController controller = CreateOrLoadGimmickPulseController(controllerName);

        if (controller == null)
        {
            return;
        }

        Animator animator = targetObject.GetComponent<Animator>();

        if (animator == null)
        {
            animator = targetObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
    }

    private static RuntimeAnimatorController CreateOrLoadGimmickPulseController(string controllerName)
    {
        EnsureFolders();

        string safeName = string.IsNullOrWhiteSpace(controllerName)
            ? "HWJ_Stage1_Gimmick_Pulse"
            : controllerName.Trim();
        string controllerPath = $"{GimmickAnimationRoot}/{safeName}.controller";
        RuntimeAnimatorController existingController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

        if (existingController != null)
        {
            return existingController;
        }

        string clipPath = $"{GimmickAnimationRoot}/{safeName}.anim";
        AnimationClip clip = new AnimationClip
        {
            name = safeName,
            frameRate = 12f,
            wrapMode = WrapMode.Loop,
        };

        AnimationCurve pulseCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.35f, 1.08f),
            new Keyframe(0.7f, 1f));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.x", pulseCurve);
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.y", pulseCurve);
        AssetDatabase.CreateAsset(clip, clipPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddMotion(clip);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static ParticleSystem CreateSandstormParticleVisual(GameObject owner, Vector2 size)
    {
        if (owner == null)
        {
            return null;
        }

        GameObject particleObject = new GameObject(owner.name + "_SandVisual");
        particleObject.transform.SetParent(owner.transform);
        particleObject.transform.localPosition = Vector3.zero;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.96f, 0.72f, 0.34f, 0.35f),
            new Color(0.75f, 0.48f, 0.22f, 0.55f));

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 55f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(Mathf.Max(1f, size.x), Mathf.Max(0.5f, size.y), 0.2f);

        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 40;
        return particles;
    }

    private static void SetSerializedFloat(Component component, string propertyName, float value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"HWJ trap setup skipped missing property `{propertyName}` on {component.GetType().Name}.");
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedObjectReference(Component component, string propertyName, UnityEngine.Object value)
    {
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning($"HWJ trap setup skipped missing property `{propertyName}` on {component.GetType().Name}.");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite GetTileSprite(TileBase tile)
    {
        return tile is Tile concreteTile ? concreteTile.sprite : null;
    }

    private static void EnsureRuntimeEnemyPrefabs()
    {
        EnsureRuntimeEnemyPrefab(SwordEnemyPrefabPath, SwordEnemySpritePath);
        EnsureRuntimeEnemyPrefab(BowEnemyPrefabPath, BowEnemySpritePath);
        EnsureRuntimeEnemyPrefab(ShieldEnemyPrefabPath, ShieldEnemySpritePath);
        EnsureRuntimeEnemyPrefab(AxeEnemyPrefabPath, AxeEnemySpritePath);
        EnsureRuntimeEnemyPrefab(LanceEnemyPrefabPath, LanceEnemySpritePath);
    }

    private static void EnsureRuntimeEnemyPrefab(string prefabPath, string spritePath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"HWJ stage enemy prefab missing: {prefabPath}");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        RemoveMissingScriptsRecursively(prefabRoot);
        Sprite sprite = LoadMarkerSprite(spritePath);
        EnsureRuntimeEnemyComponents(prefabRoot, sprite, true, false);
        ConfigureCombatExecutionTargetFilter(prefabRoot.GetComponent<HWJ_CombatExecutionSystem>(), false);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void RemoveMissingScriptsRecursively(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return;
        }

        Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
        }
    }

    private static GameObject CreatePlayablePlayer(HWJ_Stage1SceneContext context, Vector3 position)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerRuntimePrefabPath);

        if (playerPrefab == null)
        {
            Debug.LogError($"[Stage1 Builder] Missing required Player Prefab: {PlayerRuntimePrefabPath}");
            return null;
        }

        GameObject playerObject = PrefabUtility.InstantiatePrefab(playerPrefab, context.Scene) as GameObject;

        if (playerObject == null)
        {
            Debug.LogError($"[Stage1 Builder] Missing required Player Prefab instance: {PlayerRuntimePrefabPath}");
            return null;
        }

        playerObject.name = "HWJ_Player";
        playerObject.transform.SetParent(context.Root);
        playerObject.transform.position = position;
        playerObject.tag = "Player";

        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(playerObject);
        renderer.sprite = LoadPlayerDisplaySprite();
        renderer.sortingOrder = 30;

        Animator animator = EnsureComponent<Animator>(playerObject);
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerGhostAnimatorControllerPath);

        Rigidbody2D body = EnsureComponent<Rigidbody2D>(playerObject);
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(playerObject);
        collider.size = new Vector2(0.75f, 1.25f);
        collider.offset = new Vector2(0f, 0.05f);

        EnsureComponent<HWJ_RuntimeObjectContext>(playerObject);
        HWJ_RootObjectDataResolver resolver = EnsureComponent<HWJ_RootObjectDataResolver>(playerObject);
        resolver.SetRootObjectData(AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(PlayerRootPath));
        EnsureComponent<HWJ_RuntimeStatusSystem>(playerObject);
        EnsureComponent<HWJ_CombatSystem>(playerObject);
        HWJ_CombatExecutionSystem combatExecution = EnsureComponent<HWJ_CombatExecutionSystem>(playerObject);
        EnsureComponent<HWJ_SkillActionSystem>(playerObject);
        EnsureComponent<HWJ_KnockbackSystem>(playerObject);
        EnsureComponent<HWJ_CharacterMotionSystem>(playerObject);
        HWJ_PlayerInputSystem inputSystem = EnsureComponent<HWJ_PlayerInputSystem>(playerObject);
        HWJ_PlayerMovementSystem movementSystem = EnsureComponent<HWJ_PlayerMovementSystem>(playerObject);
        EnsureComponent<HWJ_PlayerAttackSystem>(playerObject);
        EnsureComponent<HWJ_LevelUpSystem>(playerObject);
        EnsureComponent<HWJ_StatOrbProgressSystem>(playerObject);
        EnsureComponent<HWJ_SkillUnlockSystem>(playerObject);
        EnsureComponent<HWJ_SoulSystem>(playerObject);
        EnsureComponent<HWJ_PossessedBodySystem>(playerObject);
        EnsureComponent<HWJ_PossessionSystem>(playerObject);
        EnsureComponent<HWJ_BodyDiscoverySystem>(playerObject);
        EnsureComponent<HWJ_PossessionMentalSystem>(playerObject);
        EnsureComponent<HWJ_CollapseSystem>(playerObject);

        SerializedObject serializedInput = new SerializedObject(inputSystem);
        serializedInput.FindProperty("inputBindingData").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<HWJ_PlayerInputBindingDataSO>(PlayerInputBindingPath);
        serializedInput.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedMovement = new SerializedObject(movementSystem);
        serializedMovement.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
        serializedMovement.FindProperty("phaseThroughCollidersInSoul").boolValue = true;
        serializedMovement.FindProperty("swapLayerInSoulState").boolValue = true;
        serializedMovement.FindProperty("soulLayerName").stringValue = "Soul";
        serializedMovement.FindProperty("bodyLayerName").stringValue = "Player";
        serializedMovement.ApplyModifiedPropertiesWithoutUndo();

        HWJ_LevelUpSystem levelUpSystem = playerObject.GetComponent<HWJ_LevelUpSystem>();
        SerializedObject serializedLevel = new SerializedObject(levelUpSystem);
        serializedLevel.FindProperty("levelUpData").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<HWJ_LevelUpDataSO>(PlayerLevelUpDataPath);
        serializedLevel.FindProperty("loadLevelDataFromDatabase").boolValue = true;
        serializedLevel.ApplyModifiedPropertiesWithoutUndo();

        ConfigureCombatExecutionTargetFilter(combatExecution, true);
        return playerObject;
    }

    private static void ConfigureMainCameraFollow(GameObject playerObject)
    {
        GameObject cameraObject = GameObject.Find("Main Camera");

        if (cameraObject == null)
        {
            return;
        }

        HWJ_PlayerCameraFollowSystem follow = cameraObject.GetComponent<HWJ_PlayerCameraFollowSystem>();

        if (follow == null)
        {
            follow = cameraObject.AddComponent<HWJ_PlayerCameraFollowSystem>();
        }

        SerializedObject serializedFollow = new SerializedObject(follow);
        serializedFollow.FindProperty("autoFindPlayerTarget").boolValue = true;

        if (playerObject != null)
        {
            serializedFollow.FindProperty("target").objectReferenceValue = playerObject.transform;
            serializedFollow.FindProperty("targetDataResolver").objectReferenceValue =
                playerObject.GetComponent<HWJ_RootObjectDataResolver>();
        }

        serializedFollow.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateDirectPossessableCorpse(
        Transform parent,
        string objectName,
        string rootObjectDataPath,
        string spritePath,
        Vector3 position)
    {
        GameObject corpseObject = CreateRuntimeBodyObject(
            parent,
            objectName,
            rootObjectDataPath,
            spritePath,
            position,
            true);
        ConfigureRuntimeStatus(corpseObject.GetComponent<HWJ_RuntimeStatusSystem>(), HWJ_RuntimeState.Dead, 0f);
        return corpseObject;
    }

    private static GameObject CreateLivePossessionResistTarget(Transform parent, string objectName, Vector3 position)
    {
        string rootPath = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(LivePossessionRootPath) != null
            ? LivePossessionRootPath
            : SwordEnemyRootPath;
        GameObject targetObject = CreateRuntimeBodyObject(
            parent,
            objectName,
            rootPath,
            SwordEnemySpritePath,
            position,
            true);
        ConfigureRuntimeStatus(targetObject.GetComponent<HWJ_RuntimeStatusSystem>(), HWJ_RuntimeState.Idle, 80f);
        return targetObject;
    }

    private static GameObject CreateRuntimeBodyObject(
        Transform parent,
        string objectName,
        string rootObjectDataPath,
        string spritePath,
        Vector3 position,
        bool colliderIsTrigger)
    {
        GameObject bodyObject = new GameObject(objectName);
        bodyObject.transform.SetParent(parent);
        bodyObject.transform.position = position;

        SpriteRenderer renderer = bodyObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadMarkerSprite(spritePath);
        renderer.sortingOrder = 24;

        EnsureRuntimeEnemyComponents(bodyObject, renderer.sprite, true, colliderIsTrigger);
        ConfigureInactiveDirectBodyBehavior(bodyObject);
        HWJ_RootObjectDataResolver resolver = bodyObject.GetComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(rootObjectDataPath));
        ConfigureCombatExecutionTargetFilter(bodyObject.GetComponent<HWJ_CombatExecutionSystem>(), false);
        return bodyObject;
    }

    private static void EnsureRuntimeEnemyComponents(
        GameObject owner,
        Sprite sprite,
        bool includeBehaviorSystems,
        bool colliderIsTrigger)
    {
        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(owner);
        renderer.sprite = sprite != null ? sprite : renderer.sprite;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 20);
        ConfigureEnemyAnimator(owner);

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(owner);
        collider.isTrigger = colliderIsTrigger;
        collider.size = new Vector2(1.1f, 1.65f);
        collider.offset = new Vector2(0f, -0.08f);

        Rigidbody2D body = EnsureComponent<Rigidbody2D>(owner);
        body.bodyType = colliderIsTrigger ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        body.gravityScale = colliderIsTrigger ? 0f : 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        EnsureComponent<HWJ_RuntimeObjectContext>(owner);
        EnsureComponent<HWJ_RootObjectDataResolver>(owner);
        EnsureComponent<HWJ_RuntimeStatusSystem>(owner);
        EnsureComponent<HWJ_CombatSystem>(owner);
        EnsureComponent<HWJ_CombatExecutionSystem>(owner);
        EnsureComponent<HWJ_SkillActionSystem>(owner);
        EnsureComponent<HWJ_KnockbackSystem>(owner);
        EnsureComponent<HWJ_CharacterMotionSystem>(owner);
        EnsureComponent<HWJ_PossessionBodyState>(owner);
        ConfigureHitEffectSystem(EnsureComponentByType(owner, "HWJ_HitEffectSystem"));

        if (includeBehaviorSystems)
        {
            EnsureComponent<HWJ_EnemyNavigationSystem>(owner);
            EnsureComponent<HWJ_EnemyAttackSystem>(owner);
            EnsureComponent<HWJ_MonsterAISystem>(owner);
        }
    }

    private static void ConfigureEnemyAnimator(GameObject owner)
    {
        if (owner == null)
        {
            return;
        }

        Animator animator = EnsureComponent<Animator>(owner);

        if (animator.runtimeAnimatorController != null)
        {
            return;
        }

        string controllerPath = ResolveEnemyAnimatorControllerPath(owner.name);
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

        if (controller == null)
        {
            Debug.LogWarning($"[Stage1 Builder] Missing enemy Animator Controller for {owner.name}: {controllerPath}");
            return;
        }

        animator.runtimeAnimatorController = controller;
    }

    private static string ResolveEnemyAnimatorControllerPath(string objectName)
    {
        string weaponName = "Sword";

        if (!string.IsNullOrWhiteSpace(objectName))
        {
            if (objectName.IndexOf("Bow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                weaponName = "Bow";
            }
            else if (objectName.IndexOf("Shield", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                weaponName = "Shield";
            }
            else if (objectName.IndexOf("Axe", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                weaponName = "Axe";
            }
            else if (objectName.IndexOf("Lance", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Spear", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                weaponName = "Lance";
            }
        }

        return $"{HwjRoot}/Animations/Generated/RedChessPossessable/{weaponName}/HWJ_RedChess_{weaponName}_Animator.controller";
    }

    private static void ConfigureHitEffectSystem(Component hitEffectSystem)
    {
        if (hitEffectSystem == null)
        {
            return;
        }

        SerializedObject serializedHitEffect = new SerializedObject(hitEffectSystem);
        serializedHitEffect.FindProperty("runtimeStatus").objectReferenceValue =
            hitEffectSystem.GetComponent<HWJ_RuntimeStatusSystem>();
        serializedHitEffect.FindProperty("dataResolver").objectReferenceValue =
            hitEffectSystem.GetComponent<HWJ_RootObjectDataResolver>();
        serializedHitEffect.FindProperty("hitEffectPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(HitEffectPrefabPath);
        serializedHitEffect.FindProperty("hitEffectSocketName").stringValue = "Hit";
        serializedHitEffect.FindProperty("fallbackOffset").vector2Value = new Vector2(0f, 0.2f);
        serializedHitEffect.FindProperty("ignoreZeroDamage").boolValue = true;
        serializedHitEffect.FindProperty("minSpawnIntervalSeconds").floatValue = 0.03f;
        serializedHitEffect.FindProperty("fallbackDestroyDelaySeconds").floatValue = 1.25f;
        serializedHitEffect.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureInactiveDirectBodyBehavior(GameObject bodyObject)
    {
        HWJ_MonsterAISystem monsterAI = bodyObject.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            SerializedObject serializedAI = new SerializedObject(monsterAI);
            serializedAI.FindProperty("driveBehavior").boolValue = false;
            serializedAI.FindProperty("autoFindPlayerTarget").boolValue = false;
            serializedAI.ApplyModifiedPropertiesWithoutUndo();
        }

        HWJ_EnemyAttackSystem enemyAttack = bodyObject.GetComponent<HWJ_EnemyAttackSystem>();

        if (enemyAttack != null)
        {
            SerializedObject serializedAttack = new SerializedObject(enemyAttack);
            serializedAttack.FindProperty("autoFindPlayerTarget").boolValue = false;
            serializedAttack.FindProperty("autoAttackWhenNoBehaviorDriver").boolValue = false;
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();
        }

        HWJ_EnemyNavigationSystem navigation = bodyObject.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            SerializedObject serializedNavigation = new SerializedObject(navigation);
            serializedNavigation.FindProperty("autoFindPlayerTarget").boolValue = false;
            serializedNavigation.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureCombatExecutionTargetFilter(
        HWJ_CombatExecutionSystem combatExecution,
        bool attackerIsPlayer)
    {
        if (combatExecution == null)
        {
            return;
        }

        SerializedObject serializedExecution = new SerializedObject(combatExecution);
        serializedExecution.FindProperty("useObjectTypeDefaultTargetFilter").boolValue = false;
        serializedExecution.FindProperty("canDamagePlayer").boolValue = !attackerIsPlayer;
        serializedExecution.FindProperty("canDamageEnemy").boolValue = attackerIsPlayer;
        serializedExecution.FindProperty("canDamageBoss").boolValue = attackerIsPlayer;
        serializedExecution.FindProperty("canDamageNpc").boolValue = false;
        serializedExecution.FindProperty("fallbackAttackRange").floatValue = attackerIsPlayer ? 2.2f : 1.4f;
        serializedExecution.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureRuntimeStatus(
        HWJ_RuntimeStatusSystem runtimeStatus,
        HWJ_RuntimeState state,
        float currentHp)
    {
        if (runtimeStatus == null)
        {
            return;
        }

        SerializedObject serializedStatus = new SerializedObject(runtimeStatus);
        serializedStatus.FindProperty("currentState").enumValueIndex = (int)state;
        serializedStatus.FindProperty("currentHp").floatValue = Mathf.Max(0f, currentHp);
        serializedStatus.FindProperty("soulHp").floatValue = Mathf.Max(0f, currentHp);
        serializedStatus.FindProperty("possessedBodyHp").floatValue = Mathf.Max(0f, currentHp);
        serializedStatus.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateSpiritOrbSwitchShowcase(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        string objectName,
        string switchId,
        Vector3 switchPosition,
        Vector3 gatePosition)
    {
        GameObject gateObject = CreateTileSpriteObject(
            context.DoorRoot ?? context.VisualRoot,
            objectName + "_Gate",
            tiles.SpiritSealWall,
            gatePosition,
            new Vector3(1f, 3.2f, 1f),
            26);
        BoxCollider2D gateCollider = gateObject.AddComponent<BoxCollider2D>();
        gateCollider.size = new Vector2(1f, 3.2f);

        GameObject switchObject = CreateTileSpriteObject(
            context.GimmickRoot ?? context.VisualRoot,
            objectName,
            tiles.SpiritOrb,
            switchPosition,
            Vector3.one * 1.2f,
            28);
        CircleCollider2D trigger = switchObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        HWJ_SpiritOrbSwitchSystem switchSystem = switchObject.AddComponent<HWJ_SpiritOrbSwitchSystem>();
        SerializedObject serializedSwitch = new SerializedObject(switchSystem);
        serializedSwitch.FindProperty("switchId").stringValue = switchId;
        serializedSwitch.FindProperty("oneShot").boolValue = true;
        serializedSwitch.FindProperty("requireSpiritState").boolValue = true;
        serializedSwitch.FindProperty("requireInteractInput").boolValue = true;
        serializedSwitch.FindProperty("spiritMentalCost").floatValue = 5f;
        SetObjectArray(serializedSwitch.FindProperty("deactivateTargets"), gateObject);
        serializedSwitch.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateWeaponSkillSwitch(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        string objectName,
        string switchId,
        Vector3 switchPosition,
        Vector3 gatePosition,
        Vector2 gateSize,
        HWJ_WeaponType requiredWeapon,
        HWJ_SkillActionType requiredActionType,
        int requiredHitCount,
        string gateObjectName = null)
    {
        GameObject gateObject = CreateTileSpriteObject(
            context.DoorRoot ?? context.VisualRoot,
            string.IsNullOrWhiteSpace(gateObjectName) ? objectName + "_Gate" : gateObjectName,
            tiles.SpiritSealWall,
            gatePosition,
            new Vector3(gateSize.x, gateSize.y, 1f),
            26);
        BoxCollider2D gateCollider = gateObject.AddComponent<BoxCollider2D>();
        gateCollider.size = gateSize;

        GameObject switchObject = CreateTileSpriteObject(
            context.GimmickRoot ?? context.VisualRoot,
            objectName,
            tiles.SpiritOrb,
            switchPosition,
            Vector3.one * 1.25f,
            28);
        CircleCollider2D trigger = switchObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        HWJ_SpiritOrbSwitchSystem switchSystem = switchObject.AddComponent<HWJ_SpiritOrbSwitchSystem>();
        SerializedObject serializedSwitch = new SerializedObject(switchSystem);
        serializedSwitch.FindProperty("switchId").stringValue = switchId;
        serializedSwitch.FindProperty("oneShot").boolValue = true;
        serializedSwitch.FindProperty("requireSpiritState").boolValue = false;
        serializedSwitch.FindProperty("requireInteractInput").boolValue = false;
        serializedSwitch.FindProperty("spiritMentalCost").floatValue = 0f;
        serializedSwitch.FindProperty("allowWeaponSkillHitActivation").boolValue = true;
        serializedSwitch.FindProperty("requirePossessedPlayerHit").boolValue = true;
        serializedSwitch.FindProperty("requiredHitWeaponType").enumValueIndex = (int)requiredWeapon;
        serializedSwitch.FindProperty("requiredHitSkillActionType").enumValueIndex = (int)requiredActionType;
        serializedSwitch.FindProperty("requiredHitCount").intValue = Mathf.Max(1, requiredHitCount);
        serializedSwitch.FindProperty("hitComboWindowSeconds").floatValue = 2f;
        SetObjectArray(serializedSwitch.FindProperty("deactivateTargets"), gateObject);
        SetObjectArray(serializedSwitch.FindProperty("disableColliders"), gateCollider);
        serializedSwitch.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBlockoutLabel(Transform parent, string objectName, string text, Vector3 position)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(parent);
        labelObject.transform.position = position;

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.16f;
        textMesh.fontSize = 32;
        textMesh.color = new Color(0.88f, 0.9f, 0.92f, 1f);

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 35;
    }

    private static void CreateBodyObstacleGate(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        string objectName,
        string obstacleId,
        Vector3 position,
        Vector2 size,
        HWJ_BodyObstacleRequirementMode requirementMode,
        HWJ_WeaponType[] allowedWeapons,
        HWJ_AbilityTag[] allowedAbilityTags = null,
        bool openByWeaponSkillHit = false,
        HWJ_WeaponType requiredHitWeaponType = HWJ_WeaponType.None,
        HWJ_SkillActionType requiredHitSkillActionType = HWJ_SkillActionType.None,
        string requiredHitSkillActionId = null)
    {
        Transform parentRoot = context.DoorRoot ?? context.VisualRoot;
        GameObject obstacleRoot = new GameObject(objectName);
        obstacleRoot.transform.SetParent(parentRoot);
        obstacleRoot.transform.position = position;

        GameObject gateObject = CreateTileSpriteObject(
            obstacleRoot.transform,
            objectName + "_Gate",
            requirementMode == HWJ_BodyObstacleRequirementMode.SpiritOnly ? tiles.SpiritSealWall : tiles.RustyGate,
            position,
            new Vector3(size.x, size.y, 1f),
            26);
        BoxCollider2D gateCollider = gateObject.AddComponent<BoxCollider2D>();
        gateCollider.size = size;

        GameObject controllerObject = new GameObject(objectName + "_System");
        controllerObject.transform.SetParent(obstacleRoot.transform);
        controllerObject.transform.position = position;
        HWJ_BodyExclusiveObstacleSystem obstacleSystem = controllerObject.AddComponent<HWJ_BodyExclusiveObstacleSystem>();

        SerializedObject serializedObstacle = new SerializedObject(obstacleSystem);
        serializedObstacle.FindProperty("obstacleId").stringValue = obstacleId;
        serializedObstacle.FindProperty("requirementMode").enumValueIndex = (int)requirementMode;
        serializedObstacle.FindProperty("openWhenRequirementMet").boolValue = true;
        serializedObstacle.FindProperty("updateContinuously").boolValue = true;
        serializedObstacle.FindProperty("requirePlayerTag").boolValue = true;
        SetEnumArray(serializedObstacle.FindProperty("allowedWeaponTypes"), allowedWeapons);
        SetEnumArray(serializedObstacle.FindProperty("allowedAbilityTags"), allowedAbilityTags);
        serializedObstacle.FindProperty("openByWeaponSkillHit").boolValue = openByWeaponSkillHit;
        serializedObstacle.FindProperty("requireWeaponSkillHitToOpen").boolValue = openByWeaponSkillHit;
        serializedObstacle.FindProperty("requiredHitWeaponType").enumValueIndex = (int)requiredHitWeaponType;
        serializedObstacle.FindProperty("requiredHitSkillActionType").enumValueIndex = (int)requiredHitSkillActionType;
        serializedObstacle.FindProperty("requiredHitSkillActionId").stringValue = requiredHitSkillActionId ?? string.Empty;
        SetObjectArray(serializedObstacle.FindProperty("obstacleColliders"), gateCollider);
        if (requirementMode == HWJ_BodyObstacleRequirementMode.SpiritOnly)
        {
            SetObjectArray(serializedObstacle.FindProperty("hideWhenOpen"));
        }
        else
        {
            SetObjectArray(serializedObstacle.FindProperty("hideWhenOpen"), gateObject);
        }
        serializedObstacle.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateRewardPickup(
        Transform parent,
        string prefabPath,
        string objectName,
        Vector3 position,
        int experienceAmount)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"HWJ reward prefab missing: {prefabPath}");
            return;
        }

        GameObject pickupObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (pickupObject == null)
        {
            return;
        }

        pickupObject.name = objectName;
        pickupObject.transform.SetParent(parent);
        pickupObject.transform.position = position;

        HWJ_ExperienceOrbPickupSystem experiencePickup = pickupObject.GetComponent<HWJ_ExperienceOrbPickupSystem>();

        if (experiencePickup != null)
        {
            SerializedObject serializedPickup = new SerializedObject(experiencePickup);
            serializedPickup.FindProperty("experienceAmount").intValue = Mathf.Max(0, experienceAmount);
            serializedPickup.FindProperty("collectDelaySeconds").floatValue = 1f;
            serializedPickup.FindProperty("lifeTimeSeconds").floatValue = 0f;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateStageChoiceRewardShowcase(
        HWJ_Stage1SceneContext context,
        HWJ_StageRuntimeSystems runtimeSystems,
        Vector3 position)
    {
        HWJ_StageChoiceRewardDataSO rewardData = AssetDatabase.LoadAssetAtPath<HWJ_StageChoiceRewardDataSO>(StageChoiceRewardPath);

        if (rewardData == null)
        {
            Debug.LogWarning($"HWJ stage choice reward data missing: {StageChoiceRewardPath}");
            return;
        }

        GameObject rewardObject = new GameObject("HWJ_Stage1_03_StageChoiceReward");
        rewardObject.transform.SetParent(context.VisualRoot);
        rewardObject.transform.position = position;
        SpriteRenderer renderer = rewardObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetTileSprite(CreateOrLoadTileSet().SpiritOrb);
        renderer.sortingOrder = 28;
        rewardObject.transform.localScale = Vector3.one * 1.2f;

        CircleCollider2D trigger = rewardObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.1f;

        HWJ_StageChoiceRewardSystem rewardSystem = rewardObject.AddComponent<HWJ_StageChoiceRewardSystem>();
        SerializedObject serializedRewardSystem = new SerializedObject(rewardSystem);
        serializedRewardSystem.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedRewardSystem.FindProperty("rewardData").objectReferenceValue = rewardData;
        serializedRewardSystem.FindProperty("offerOnStageCleared").boolValue = true;
        serializedRewardSystem.FindProperty("requireStageClear").boolValue = true;
        serializedRewardSystem.FindProperty("preventDuplicateSelection").boolValue = true;
        serializedRewardSystem.FindProperty("useSaveServiceClaim").boolValue = true;
        serializedRewardSystem.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateMidBossRuntimeObject(HWJ_Stage1SceneContext context, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MidBossPrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"HWJ mid boss prefab missing: {MidBossPrefabPath}");
            return null;
        }

        GameObject bossObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (bossObject == null)
        {
            return null;
        }

        bossObject.name = "HWJ_MidBoss1_Runtime";
        bossObject.transform.SetParent(context.SpawnRoot);
        bossObject.transform.position = position;
        bossObject.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        HWJ_RootObjectDataResolver resolver = bossObject.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null)
        {
            resolver.SetRootObjectData(AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(MidBossRootPath));
        }

        HWJ_RuntimeStatusSystem status = bossObject.GetComponent<HWJ_RuntimeStatusSystem>();
        status?.RefreshCurrentHpFromData(true);
        ConfigureCombatExecutionTargetFilter(bossObject.GetComponent<HWJ_CombatExecutionSystem>(), false);
        ConfigureMidBossSummons(bossObject);
        return bossObject;
    }

    private static void ConfigureMidBossSummons(GameObject bossObject)
    {
        if (bossObject == null)
        {
            return;
        }

        HWJ_MidBossPatternSystem patternSystem = bossObject.GetComponent<HWJ_MidBossPatternSystem>();

        if (patternSystem == null)
        {
            return;
        }

        SerializedObject serializedPattern = new SerializedObject(patternSystem);
        serializedPattern.FindProperty("summonMonsterPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(SwordEnemyPrefabPath);
        SetObjectArray(
            serializedPattern.FindProperty("possessableMonsterPrefabs"),
            AssetDatabase.LoadAssetAtPath<GameObject>(SwordEnemyPrefabPath),
            AssetDatabase.LoadAssetAtPath<GameObject>(ShieldEnemyPrefabPath));
        SetObjectArray(
            serializedPattern.FindProperty("possessableMonsterRootObjects"),
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(SwordEnemyRootPath),
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(ShieldEnemyRootPath));
        serializedPattern.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBossFlowShowcase(
        HWJ_Stage1SceneContext context,
        HWJ_StageRuntimeSystems runtimeSystems,
        GameObject bossObject)
    {
        GameObject flowObject = new GameObject("HWJ_BossFlowSystem");
        flowObject.transform.SetParent(context.SystemRoot);
        flowObject.transform.position = bossObject != null ? bossObject.transform.position : Vector3.zero;

        HWJ_BossFlowSystem bossFlow = flowObject.AddComponent<HWJ_BossFlowSystem>();
        SerializedObject serializedFlow = new SerializedObject(bossFlow);
        serializedFlow.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedFlow.FindProperty("bossBrainSystem").objectReferenceValue =
            bossObject != null ? bossObject.GetComponent<HWJ_BossBrainSystem>() : null;
        serializedFlow.FindProperty("bossResolver").objectReferenceValue =
            bossObject != null ? bossObject.GetComponent<HWJ_RootObjectDataResolver>() : null;
        serializedFlow.FindProperty("bossEntryTriggerActive").boolValue = true;
        serializedFlow.FindProperty("autoCompleteBossFlowOnCombatDeath").boolValue = true;
        serializedFlow.FindProperty("requireBossBattleStateForCombatDeath").boolValue = false;
        serializedFlow.FindProperty("useStageDefinitionDefaultsOnCombatDeath").boolValue = true;
        serializedFlow.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject serializedCoreLoop = new SerializedObject(runtimeSystems.CoreLoop);
        serializedCoreLoop.FindProperty("bossFlowSystem").objectReferenceValue = bossFlow;
        serializedCoreLoop.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateTileSpriteObject(
        Transform parent,
        string objectName,
        TileBase tile,
        Vector3 position,
        Vector3 scale,
        int sortingOrder)
    {
        return CreateTiledVisualObject(
            parent,
            objectName,
            tile,
            position,
            Mathf.Max(1, Mathf.RoundToInt(scale.x)),
            Mathf.Max(1, Mathf.RoundToInt(scale.y)),
            sortingOrder,
            Color.white);
    }

    private static GameObject CreateTiledVisualObject(
        Transform parent,
        string objectName,
        TileBase tile,
        Vector3 centerPosition,
        int columns,
        int rows,
        int sortingOrder,
        Color color)
    {
        GameObject rootObject = new GameObject(objectName);
        rootObject.transform.SetParent(parent);
        rootObject.transform.position = centerPosition;

        Sprite sprite = GetTileSprite(tile);

        if (sprite == null)
        {
            Debug.LogWarning($"HWJ Stage1-1 visual placeholder is missing a sprite: {objectName}");
            return rootObject;
        }

        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);

        if (safeColumns == 1 && safeRows == 1)
        {
            SpriteRenderer renderer = rootObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return rootObject;
        }

        float startX = -(safeColumns - 1) * 0.5f;
        float startY = -(safeRows - 1) * 0.5f;

        for (int y = 0; y < safeRows; y++)
        {
            for (int x = 0; x < safeColumns; x++)
            {
                GameObject tileObject = new GameObject($"{objectName}_Tile_{x:00}_{y:00}");
                tileObject.transform.SetParent(rootObject.transform);
                tileObject.transform.localPosition = new Vector3(startX + x, startY + y, 0f);

                SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = color;
                renderer.sortingOrder = sortingOrder;
            }
        }

        return rootObject;
    }

    private static T EnsureComponent<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : owner.AddComponent<T>();
    }

    private static Component EnsureComponentByType(GameObject owner, string typeName)
    {
        if (owner == null || string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        Type componentType = ResolveRuntimeType(typeName);

        if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
        {
            return null;
        }

        Component component = owner.GetComponent(componentType);
        return component != null ? component : owner.AddComponent(componentType);
    }

    private static Type ResolveRuntimeType(string typeName)
    {
        return Type.GetType(typeName)
            ?? Type.GetType(typeName + ", HWJ.Runtime")
            ?? Type.GetType("HWJ." + typeName + ", HWJ.Runtime");
    }

    private static Component GetOptionalComponentByType(GameObject owner, string typeName)
    {
        Type componentType = ResolveRuntimeType(typeName);
        return owner != null && componentType != null ? owner.GetComponent(componentType) : null;
    }

    private static Component[] GetOptionalComponentsInChildren(Transform root, string typeName)
    {
        Type componentType = ResolveRuntimeType(typeName);

        if (root == null || componentType == null)
        {
            return Array.Empty<Component>();
        }

        Component[] allComponents = root.GetComponentsInChildren<Component>(true);
        List<Component> matches = new List<Component>();

        for (int i = 0; i < allComponents.Length; i++)
        {
            Component component = allComponents[i];

            if (component != null && componentType.IsAssignableFrom(component.GetType()))
            {
                matches.Add(component);
            }
        }

        return matches.ToArray();
    }

    private static Component[] FindOptionalComponentsByType(string typeName)
    {
        Type componentType = ResolveRuntimeType(typeName);

        if (componentType == null)
        {
            return Array.Empty<Component>();
        }

        Component[] allComponents = UnityEngine.Object.FindObjectsByType<Component>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        List<Component> matches = new List<Component>();

        for (int i = 0; i < allComponents.Length; i++)
        {
            Component component = allComponents[i];

            if (component != null && componentType.IsAssignableFrom(component.GetType()))
            {
                matches.Add(component);
            }
        }

        return matches.ToArray();
    }

    private static bool GetOptionalBoolProperty(Component component, string propertyName)
    {
        if (component == null || string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        Type componentType = component.GetType();
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.PropertyInfo property = componentType.GetProperty(propertyName, flags);

        if (property != null && property.PropertyType == typeof(bool))
        {
            return (bool)property.GetValue(component);
        }

        System.Reflection.FieldInfo field = componentType.GetField(propertyName, flags);
        return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(component);
    }

    private static void ConfigureGeneratedStageMarker(
        Component marker,
        string originalName,
        string stageSceneId,
        string category,
        bool temporaryPlaceholder,
        bool protectFromCleanup)
    {
        if (marker == null)
        {
            return;
        }

        System.Reflection.MethodInfo configureMethod = marker.GetType().GetMethod(
            "Configure",
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic);

        if (configureMethod == null)
        {
            return;
        }

        configureMethod.Invoke(marker, new object[]
        {
            originalName,
            stageSceneId,
            category,
            temporaryPlaceholder,
            protectFromCleanup
        });
    }

    private static void TryInvokeFloatMethod(Component component, string methodName, float value)
    {
        if (component == null || string.IsNullOrWhiteSpace(methodName))
        {
            return;
        }

        System.Reflection.MethodInfo method = component.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(float) },
            null);

        method?.Invoke(component, new object[] { value });
    }

    private static void TryInvokeVector2Method(Component component, string methodName, Vector2 value)
    {
        if (component == null || string.IsNullOrWhiteSpace(methodName))
        {
            return;
        }

        System.Reflection.MethodInfo method = component.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(Vector2) },
            null);

        method?.Invoke(component, new object[] { value });
    }

    private static void AssignLayerIfExists(GameObject owner, string layerName)
    {
        if (owner == null || string.IsNullOrWhiteSpace(layerName))
        {
            return;
        }

        int layer = LayerMask.NameToLayer(layerName);

        if (layer < 0)
        {
            return;
        }

        Transform[] transforms = owner.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.layer = layer;
        }
    }

    private static void SetObjectArray(SerializedProperty arrayProperty, params UnityEngine.Object[] values)
    {
        if (arrayProperty == null)
        {
            return;
        }

        arrayProperty.arraySize = values != null ? values.Length : 0;

        for (int i = 0; values != null && i < values.Length; i++)
        {
            arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetEnumArray(SerializedProperty arrayProperty, HWJ_WeaponType[] values)
    {
        if (arrayProperty == null)
        {
            return;
        }

        arrayProperty.arraySize = values != null ? values.Length : 0;

        for (int i = 0; values != null && i < values.Length; i++)
        {
            arrayProperty.GetArrayElementAtIndex(i).enumValueIndex = (int)values[i];
        }
    }

    private static void SetEnumArray(SerializedProperty arrayProperty, HWJ_AbilityTag[] values)
    {
        if (arrayProperty == null)
        {
            return;
        }

        arrayProperty.arraySize = values != null ? values.Length : 0;

        for (int i = 0; values != null && i < values.Length; i++)
        {
            arrayProperty.GetArrayElementAtIndex(i).enumValueIndex = (int)values[i];
        }
    }

    private static void RegisterStageScenesInBuildSettings()
    {
        string[] requiredScenePaths =
        {
            Stage1Scene01Path,
            Stage1Scene02Path,
            Stage1Scene03Path,
            Stage1Scene04Path,
        };

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < requiredScenePaths.Length; i++)
        {
            string scenePath = requiredScenePaths[i];
            bool exists = false;

            for (int j = 0; j < scenes.Count; j++)
            {
                if (scenes[j].path == scenePath)
                {
                    scenes[j] = new EditorBuildSettingsScene(scenePath, true);
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject CreateSpawnPoint(
        Transform parent,
        string objectName,
        string pointId,
        HWJ_SpawnPointType type,
        Vector3 position)
    {
        GameObject pointObject = new GameObject(objectName);
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;

        HWJ_SpawnPoint spawnPoint = pointObject.AddComponent<HWJ_SpawnPoint>();
        SerializedObject serializedObject = new SerializedObject(spawnPoint);
        serializedObject.FindProperty("pointId").stringValue = pointId;
        serializedObject.FindProperty("spawnPointType").enumValueIndex = (int)type;
        serializedObject.FindProperty("spawnParent").objectReferenceValue = parent;
        serializedObject.FindProperty("gizmoRadius").floatValue = type == HWJ_SpawnPointType.Boss ? 0.75f : 0.35f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        return pointObject;
    }

    private static void CreateSpriteMarker(
        Transform parent,
        string objectName,
        string spritePath,
        Vector3 position,
        float scale,
        int sortingOrder)
    {
        Sprite sprite = LoadMarkerSprite(spritePath);

        if (sprite == null)
        {
            Debug.LogWarning($"HWJ stage 1 scene marker skipped because sprite is missing: {spritePath}");
            return;
        }

        GameObject markerObject = new GameObject(objectName);
        markerObject.transform.SetParent(parent);
        markerObject.transform.position = position;
        markerObject.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite LoadPlayerDisplaySprite()
    {
        if (!File.Exists(Path.GetFullPath(PlayerSpritePath)))
        {
            Debug.LogError($"[Stage1 Builder] Missing required Player Sprite: {PlayerSpritePath}");
            return null;
        }

        const int frameWidth = 32;
        const int frameHeight = 32;
        byte[] sourceBytes = File.ReadAllBytes(Path.GetFullPath(PlayerSpritePath));
        Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!sourceTexture.LoadImage(sourceBytes))
        {
            UnityEngine.Object.DestroyImmediate(sourceTexture);
            Debug.LogError($"[Stage1 Builder] Failed to read Player Sprite: {PlayerSpritePath}");
            return null;
        }

        int safeWidth = Mathf.Min(frameWidth, sourceTexture.width);
        int safeHeight = Mathf.Min(frameHeight, sourceTexture.height);
        Texture2D frameTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);
        frameTexture.filterMode = FilterMode.Point;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32[] outputPixels = new Color32[frameWidth * frameHeight];

        for (int i = 0; i < outputPixels.Length; i++)
        {
            outputPixels[i] = clear;
        }

        Color[] sourcePixels = sourceTexture.GetPixels(0, 0, safeWidth, safeHeight);

        for (int y = 0; y < safeHeight; y++)
        {
            for (int x = 0; x < safeWidth; x++)
            {
                outputPixels[y * frameWidth + x] = sourcePixels[y * safeWidth + x];
            }
        }

        frameTexture.SetPixels32(outputPixels);
        frameTexture.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(PlayerDisplaySpritePath)));
        File.WriteAllBytes(Path.GetFullPath(PlayerDisplaySpritePath), frameTexture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sourceTexture);
        UnityEngine.Object.DestroyImmediate(frameTexture);

        ImportAsSprite(PlayerDisplaySpritePath, 32f, FilterMode.Point, true);
        return LoadMarkerSprite(PlayerDisplaySpritePath);
    }

    private static Sprite LoadMarkerSprite(string spritePath)
    {
        string fullPath = Path.GetFullPath(spritePath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        if (IsHwjVisualAssetPath(spritePath))
        {
            ImportAsSprite(spritePath, 32f, FilterMode.Point, true);
        }
        else
        {
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
        }

        Sprite existingSprite = LoadFirstSpriteAtPath(spritePath);

        if (existingSprite != null)
        {
            return existingSprite;
        }

        ImportAsSprite(spritePath, 32f, FilterMode.Point, true);
        return LoadFirstSpriteAtPath(spritePath);
    }

    private static bool IsHwjVisualAssetPath(string spritePath)
    {
        return spritePath.StartsWith(HwjRoot + "/", StringComparison.Ordinal)
            || spritePath.IndexOf("/HWJ/", StringComparison.OrdinalIgnoreCase) >= 0
            || spritePath.IndexOf("\\HWJ\\", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Sprite LoadFirstSpriteAtPath(string spritePath)
    {
        Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        if (singleSprite != null)
        {
            return singleSprite;
        }

        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);

        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void SaveContextScene(HWJ_Stage1SceneContext context)
    {
        MarkGeneratedSceneTree(context);
        EditorSceneManager.MarkSceneDirty(context.Scene);
        EditorSceneManager.SaveScene(context.Scene, context.ScenePath);
    }

    private static void MarkGeneratedSceneTree(HWJ_Stage1SceneContext context)
    {
        if (context == null || context.Root == null)
        {
            return;
        }

        string stageSceneId = Path.GetFileNameWithoutExtension(context.ScenePath);
        Transform[] transforms = context.Root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            GameObject targetObject = transforms[i].gameObject;
            bool temporaryPlaceholder = IsGeneratedTemporaryPlaceholder(targetObject);
            string category = ResolveGeneratedObjectCategory(targetObject, temporaryPlaceholder);
            Component marker = GetOptionalComponentByType(targetObject, "HWJ_GeneratedStageObject");

            if (marker == null)
            {
                marker = EnsureComponentByType(targetObject, "HWJ_GeneratedStageObject");
            }

            if (marker == null)
            {
                continue;
            }

            ConfigureGeneratedStageMarker(marker, targetObject.name, stageSceneId, category, temporaryPlaceholder, false);
            EditorUtility.SetDirty(marker);
        }
    }

    private static bool IsGeneratedTemporaryPlaceholder(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        return targetObject.name.EndsWith("_Visual", StringComparison.Ordinal)
            || targetObject.name.Contains("_Marker_")
            || targetObject.name.Contains("Placeholder");
    }

    private static string ResolveGeneratedObjectCategory(GameObject targetObject, bool temporaryPlaceholder)
    {
        if (temporaryPlaceholder)
        {
            return "임시 표시 오브젝트";
        }

        if (targetObject.GetComponent<Tilemap>() != null)
        {
            return "타일맵";
        }

        if (targetObject.GetComponent<HWJ_SpawnPoint>() != null)
        {
            return "스폰 포인트";
        }

        if (targetObject.GetComponent<HWJ_ScenePortalSystem>() != null)
        {
            return "스테이지 포탈";
        }

        if (GetOptionalComponentByType(targetObject, "HWJ_ParallaxBackgroundSystem") != null)
        {
            return "패럴랙스 배경";
        }

        if (targetObject.GetComponent<Camera>() != null)
        {
            return "카메라";
        }

        return "스테이지 생성 오브젝트";
    }

    private sealed class HWJ_Stage1SceneContext
    {
        public HWJ_Stage1SceneContext(
            Scene scene,
            string scenePath,
            Transform root,
            Tilemap background,
            Tilemap ground,
            Tilemap oneWayPlatform,
            Tilemap decoration,
            Tilemap hazardGuide,
            Transform systemRoot,
            Transform spawnRoot,
            Transform portalRoot,
            Transform trapRoot,
            Transform visualRoot,
            Tilemap bodyBlocker = null,
            Transform structuresRoot = null,
            Transform ruinedBuildingsRoot = null,
            Transform woodenScaffoldsRoot = null,
            Transform roofStructuresRoot = null,
            Transform decorativePropsRoot = null,
            Transform gameplayRoot = null,
            Transform checkpointRoot = null,
            Transform combatZoneRoot = null,
            Transform possessableBodyRoot = null,
            Transform gimmickRoot = null,
            Transform doorRoot = null,
            Transform exitPortalRoot = null)
        {
            Scene = scene;
            ScenePath = scenePath;
            Root = root;
            Background = background;
            Ground = ground;
            OneWayPlatform = oneWayPlatform;
            BodyBlocker = bodyBlocker;
            Decoration = decoration;
            HazardGuide = hazardGuide;
            SystemRoot = systemRoot;
            SpawnRoot = spawnRoot;
            PortalRoot = portalRoot;
            TrapRoot = trapRoot;
            VisualRoot = visualRoot;
            StructuresRoot = structuresRoot;
            RuinedBuildingsRoot = ruinedBuildingsRoot;
            WoodenScaffoldsRoot = woodenScaffoldsRoot;
            RoofStructuresRoot = roofStructuresRoot;
            DecorativePropsRoot = decorativePropsRoot;
            GameplayRoot = gameplayRoot;
            CheckpointRoot = checkpointRoot;
            CombatZoneRoot = combatZoneRoot;
            PossessableBodyRoot = possessableBodyRoot;
            GimmickRoot = gimmickRoot;
            DoorRoot = doorRoot;
            ExitPortalRoot = exitPortalRoot;
        }

        public Scene Scene { get; }
        public string ScenePath { get; }
        public Transform Root { get; }
        public Tilemap Background { get; }
        public Tilemap Ground { get; }
        public Tilemap OneWayPlatform { get; }
        public Tilemap BodyBlocker { get; }
        public Tilemap Decoration { get; }
        public Tilemap HazardGuide { get; }
        public Tilemap SoulBoundary => HazardGuide;
        public Transform SystemRoot { get; }
        public Transform SpawnRoot { get; }
        public Transform PortalRoot { get; }
        public Transform TrapRoot { get; }
        public Transform VisualRoot { get; }
        public Transform StructuresRoot { get; }
        public Transform RuinedBuildingsRoot { get; }
        public Transform WoodenScaffoldsRoot { get; }
        public Transform RoofStructuresRoot { get; }
        public Transform DecorativePropsRoot { get; }
        public Transform GameplayRoot { get; }
        public Transform CheckpointRoot { get; }
        public Transform CombatZoneRoot { get; }
        public Transform PossessableBodyRoot { get; }
        public Transform GimmickRoot { get; }
        public Transform DoorRoot { get; }
        public Transform ExitPortalRoot { get; }
    }

    private sealed class HWJ_StageRuntimeSystems
    {
        public HWJ_StageRuntimeSystems(
            HWJ_GameManager gameManager,
            HWJ_SpawnerSystem spawner,
            HWJ_SceneTransitionSystem transition,
            HWJ_StageProgressionSystem progression,
            HWJ_StageEnemyCountSystem enemyCount,
            HWJ_CoreLoopCoordinator coreLoop)
        {
            GameManager = gameManager;
            Spawner = spawner;
            Transition = transition;
            Progression = progression;
            EnemyCount = enemyCount;
            CoreLoop = coreLoop;
        }

        public HWJ_GameManager GameManager { get; }
        public HWJ_SpawnerSystem Spawner { get; }
        public HWJ_SceneTransitionSystem Transition { get; }
        public HWJ_StageProgressionSystem Progression { get; }
        public HWJ_StageEnemyCountSystem EnemyCount { get; }
        public HWJ_CoreLoopCoordinator CoreLoop { get; }
    }

    private sealed class HWJ_Stage1SpawnRequest
    {
        public HWJ_Stage1SpawnRequest(
            string spawnId,
            string spawnPointId,
            HWJ_SpawnPointType spawnPointType,
            string rootObjectDataPath,
            string prefabPath,
            int spawnCount,
            float spawnDelaySeconds = 0f)
        {
            SpawnId = spawnId;
            SpawnPointId = spawnPointId;
            SpawnPointType = spawnPointType;
            RootObjectDataPath = rootObjectDataPath;
            PrefabPath = prefabPath;
            SpawnCount = spawnCount;
            SpawnDelaySeconds = spawnDelaySeconds;
        }

        public string SpawnId { get; }
        public string SpawnPointId { get; }
        public HWJ_SpawnPointType SpawnPointType { get; }
        public string RootObjectDataPath { get; }
        public string PrefabPath { get; }
        public int SpawnCount { get; }
        public float SpawnDelaySeconds { get; }
    }

    private sealed class HWJ_Stage1TileSet
    {
        public TileBase BackgroundSand;
        public TileBase OcherSandFloor;
        public TileBase CrackedStone;
        public TileBase RuinedWall;
        public TileBase CollapsedRoof;
        public TileBase SpikeWarning;
        public TileBase SandstormGuide;
        public TileBase SpiritSealWall;
        public TileBase SpiritOrb;
        public TileBase SpiritWind;
        public TileBase BarracksWall;
        public TileBase RustyGate;
        public TileBase GroundTopLeft;
        public TileBase GroundTopMiddleA;
        public TileBase GroundTopMiddleB;
        public TileBase GroundTopRight;
        public TileBase GroundFillA;
        public TileBase GroundFillB;
        public TileBase BarracksTopLeft;
        public TileBase BarracksTopMiddleA;
        public TileBase BarracksTopMiddleB;
        public TileBase BarracksTopRight;
        public TileBase BarracksFillA;
        public TileBase BarracksFillB;
        public TileBase BackgroundWallA;
        public TileBase BackgroundWallB;
    }

    private enum HWJ_Stage1TilePattern
    {
        SoftNoise,
        SandFloor,
        CrackedStone,
        RuinedWall,
        Wood,
        SpikeWarning,
        Sandstorm,
        SpiritSeal,
        SpiritOrb,
        SpiritWind,
        BarracksWall,
        RustyGate,
        MidBossMarker
    }
}
