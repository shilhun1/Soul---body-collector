using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
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
    private const string SpawnTableRoot = HwjRoot + "/ScriptableObjects/SpawnTables";
    private const string Stage1Scene01Path = SceneRoot + "/HWJ_Stage1_01_RuinedVillage.unity";
    private const string Stage1Scene02Path = SceneRoot + "/HWJ_Stage1_02_RuinedOutpost.unity";
    private const string Stage1Scene03Path = SceneRoot + "/HWJ_Stage1_03_BackRoad.unity";
    private const string Stage1Scene04Path = SceneRoot + "/HWJ_Stage1_04_MidBossBarracks.unity";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunStage1TilemapBuild.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_Stage1TilemapSceneBuildReport.md";

    private const string GameplayDatabasePath = HwjRoot + "/ScriptableObjects/Database/HWJ_GameplayDatabase.asset";
    private const string PlayerRootPath = HwjRoot + "/ScriptableObjects/RootObjects/HWJ_Player_Test_RootObjectData.asset";
    private const string PlayerInputBindingPath = HwjRoot + "/ScriptableObjects/Input/HWJ_DefaultPlayerInputBindings.asset";
    private const string PlayerLevelUpDataPath = HwjRoot + "/ScriptableObjects/LevelTables/HWJ_Player_Default_LevelUpData.asset";
    private const string PlayerSpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Player_Test.png";
    private const string SwordEnemySpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Enemy_Corpse_Sword.png";
    private const string BowEnemySpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Enemy_Corpse_Bow.png";
    private const string ShieldEnemySpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Enemy_Corpse_Shield.png";
    private const string MidBossSpritePath = SpriteRoot + "/HWJ_Stage1_Marker_MidBoss.png";
    private const string ExperienceOrbPrefabPath = HwjRoot + "/Prefabs/Generated/ExperienceOrbs/HWJ_ExperienceOrb_Default_Prefab.prefab";
    private const string AttackStatOrbPrefabPath = HwjRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackPowerSmall_Prefab.prefab";
    private const string DefenseStatOrbPrefabPath = HwjRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_DefenseSmall_Prefab.prefab";
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

    [MenuItem("Tools/HWJ/Scene/Create HWJ Stage 1 Tilemap Build Flag")]
    public static void CreateStage1TilemapBuildFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run", Encoding.UTF8);
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

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        File.Delete(FlagFilePath);
        RunBuildFromOpenEditor();
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

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/02Scripts", "HWJ");
        EnsureFolder(HwjRoot, "Art");
        EnsureFolder(HwjRoot + "/Art", "Generated");
        EnsureFolder(HwjRoot + "/Art/Generated", "Stage1");
        EnsureFolder(HwjRoot, "Tiles");
        EnsureFolder(HwjRoot + "/Tiles", "Generated");
        EnsureFolder(HwjRoot + "/Tiles/Generated", "Stage1");
        EnsureFolder(HwjRoot, "ScriptableObjects");
        EnsureFolder(HwjRoot + "/ScriptableObjects", "SpawnTables");
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
            BackgroundSand = Load08TileOrCreateFallback("main_lev_build_102.asset", "HWJ_Stage1_BackgroundSand.asset", "HWJ_Stage1_Tile_BackgroundSand.png", new Color32(92, 67, 42, 255), HWJ_Stage1TilePattern.SoftNoise, Tile.ColliderType.None),
            OcherSandFloor = Load08TileOrCreateFallback("main_lev_build_80.asset", "HWJ_Stage1_OcherSandFloor.asset", "HWJ_Stage1_Tile_OcherSandFloor.png", new Color32(178, 129, 63, 255), HWJ_Stage1TilePattern.SandFloor, Tile.ColliderType.Grid),
            CrackedStone = Load08TileOrCreateFallback("main_lev_build_77.asset", "HWJ_Stage1_CrackedStone.asset", "HWJ_Stage1_Tile_CrackedStone.png", new Color32(126, 104, 80, 255), HWJ_Stage1TilePattern.CrackedStone, Tile.ColliderType.Grid),
            RuinedWall = Load08TileOrCreateFallback("main_lev_build_96.asset", "HWJ_Stage1_RuinedWall.asset", "HWJ_Stage1_Tile_RuinedWall.png", new Color32(139, 96, 58, 255), HWJ_Stage1TilePattern.RuinedWall, Tile.ColliderType.Grid),
            CollapsedRoof = Load08TileOrCreateFallback("main_lev_build_113.asset", "HWJ_Stage1_CollapsedRoof.asset", "HWJ_Stage1_Tile_CollapsedRoof.png", new Color32(92, 59, 37, 255), HWJ_Stage1TilePattern.Wood, Tile.ColliderType.None),
            SpikeWarning = Load08TileOrCreateFallback("other_and_decorative_44.asset", "HWJ_Stage1_SpikeWarning.asset", "HWJ_Stage1_Tile_SpikeWarning.png", new Color32(210, 73, 43, 255), HWJ_Stage1TilePattern.SpikeWarning, Tile.ColliderType.None),
            SandstormGuide = Load08TileOrCreateFallback("other_and_decorative_0.asset", "HWJ_Stage1_SandstormGuide.asset", "HWJ_Stage1_Tile_SandstormGuide.png", new Color32(224, 175, 88, 190), HWJ_Stage1TilePattern.Sandstorm, Tile.ColliderType.None),
            SpiritSealWall = Load08TileOrCreateFallback("other_and_decorative_6.asset", "HWJ_Stage1_SpiritSealWall.asset", "HWJ_Stage1_Tile_SpiritSealWall.png", new Color32(110, 78, 174, 210), HWJ_Stage1TilePattern.SpiritSeal, Tile.ColliderType.Grid),
            SpiritOrb = Load08TileOrCreateFallback("other_and_decorative_31.asset", "HWJ_Stage1_SpiritOrb.asset", "HWJ_Stage1_Tile_SpiritOrb.png", new Color32(116, 202, 255, 230), HWJ_Stage1TilePattern.SpiritOrb, Tile.ColliderType.None),
            SpiritWind = Load08TileOrCreateFallback("other_and_decorative_34.asset", "HWJ_Stage1_SpiritWind.asset", "HWJ_Stage1_Tile_SpiritWind.png", new Color32(154, 111, 226, 180), HWJ_Stage1TilePattern.SpiritWind, Tile.ColliderType.None),
            BarracksWall = Load08TileOrCreateFallback("main_lev_build_97.asset", "HWJ_Stage1_BarracksWall.asset", "HWJ_Stage1_Tile_BarracksWall.png", new Color32(75, 64, 62, 255), HWJ_Stage1TilePattern.BarracksWall, Tile.ColliderType.Grid),
            RustyGate = Load08TileOrCreateFallback("main_lev_build_128.asset", "HWJ_Stage1_RustyGate.asset", "HWJ_Stage1_Tile_RustyGate.png", new Color32(103, 72, 48, 255), HWJ_Stage1TilePattern.RustyGate, Tile.ColliderType.Grid)
        };
    }

    private static TileBase Load08TileOrCreateFallback(
        string sourceTileFileName,
        string fallbackTileFileName,
        string fallbackSpriteFileName,
        Color32 fallbackColor,
        HWJ_Stage1TilePattern fallbackPattern,
        Tile.ColliderType fallbackColliderType)
    {
        string sourceTilePath = SourceTileRoot + "/" + sourceTileFileName;
        TileBase sourceTile = AssetDatabase.LoadAssetAtPath<TileBase>(sourceTilePath);

        if (sourceTile != null)
        {
            return sourceTile;
        }

        Debug.LogWarning($"HWJ stage 1 tile source missing. Fallback generated tile will be used: {sourceTilePath}");

        Sprite fallbackSprite = CreateOrLoadSprite(
            SpriteRoot + "/" + fallbackSpriteFileName,
            fallbackColor,
            fallbackPattern);

        return CreateOrLoadTile(fallbackTileFileName, fallbackSprite, fallbackColliderType);
    }

    private static Sprite CreateOrLoadSprite(
        string assetPath,
        Color32 color,
        HWJ_Stage1TilePattern pattern)
    {
        string fullPath = Path.GetFullPath(assetPath);

        if (!File.Exists(fullPath))
        {
            Texture2D texture = CreateTexture(color, pattern);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
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
        int offset = 0;

        switch (pattern)
        {
            case HWJ_Stage1TilePattern.SandFloor:
                offset = y > 24 ? 20 : ((x * 7 + y * 3) % 13 == 0 ? -18 : 0);
                break;
            case HWJ_Stage1TilePattern.CrackedStone:
                offset = x == y || x + y == 31 || (x + y) % 17 == 0 ? -28 : 8;
                break;
            case HWJ_Stage1TilePattern.RuinedWall:
                offset = x % 10 == 0 || y % 8 == 0 ? -22 : 7;
                break;
            case HWJ_Stage1TilePattern.Wood:
                offset = x % 7 < 2 ? -20 : (y % 11 == 0 ? 16 : 0);
                break;
            case HWJ_Stage1TilePattern.SpikeWarning:
                offset = x == y || x + y == 31 || y < 5 ? 36 : -12;
                break;
            case HWJ_Stage1TilePattern.Sandstorm:
                offset = (x + y * 2) % 9 < 4 ? 34 : -10;
                break;
            case HWJ_Stage1TilePattern.SpiritSeal:
                offset = x % 9 == 0 || y % 9 == 0 || x == y || x + y == 31 ? 38 : -22;
                break;
            case HWJ_Stage1TilePattern.SpiritOrb:
                int centerDistance = Mathf.Abs(x - 16) + Mathf.Abs(y - 16);
                offset = centerDistance < 9 ? 46 : (centerDistance < 13 ? 12 : -30);
                break;
            case HWJ_Stage1TilePattern.SpiritWind:
                offset = (x * 2 + y * 5) % 17 < 8 ? 42 : -18;
                break;
            case HWJ_Stage1TilePattern.BarracksWall:
                offset = x % 12 == 0 || y % 10 == 0 ? -18 : 5;
                break;
            case HWJ_Stage1TilePattern.RustyGate:
                offset = x % 8 < 2 || y % 8 < 2 ? -26 : 12;
                break;
            case HWJ_Stage1TilePattern.MidBossMarker:
                offset = x > 8 && x < 24 && y > 5 && y < 27 ? 18 : -20;
                break;
            default:
                offset = (x * 11 + y * 5) % 19 == 0 ? 10 : 0;
                break;
        }

        return new Color32(
            ClampByte(baseColor.r + offset),
            ClampByte(baseColor.g + offset),
            ClampByte(baseColor.b + offset),
            baseColor.a);
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
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene01Path,
            "HWJ_Stage1_01_RuinedVillage",
            "1-1 / 1-2 황폐한 마을 입구와 내부",
            new Vector3(8f, 1.3f, -10f),
            8f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -30, -8, 78, 22);
        PaintRect(context.Ground, tiles.OcherSandFloor, -24, -7, 66, 3);
        PaintRect(context.Ground, tiles.CrackedStone, -1, -4, 9, 1);
        PaintRect(context.Ground, tiles.CrackedStone, 14, -3, 8, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, -15, -1, 7, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 4, 1, 7, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 20, 2, 6, 1);

        PaintRuinedHouse(context, tiles, -20, -4, 8, 7);
        PaintRuinedHouse(context, tiles, 9, -4, 9, 8);
        PaintRuinedHouse(context, tiles, 29, -4, 7, 6);
        PaintRect(context.Decoration, tiles.CollapsedRoof, 14, 2, 9, 1);
        PaintRect(context.Decoration, tiles.CollapsedRoof, 24, 3, 5, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, -2, -4, 3, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 32, -4, 4, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -26, 1, 9, 2);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 22, 0, 10, 2);
        PaintSpiritOnlyPassage(context, tiles, -18, -2, 10, 5, true);
        PaintSpiritOnlyPassage(context, tiles, 2, -3, 7, 6, false);
        PaintSpiritOrbLine(context, tiles, -16, 0, 4);
        PaintSpiritOrbLine(context, tiles, 2, -1, 5);
        PaintRect(context.HazardGuide, tiles.SpiritWind, -13, 2, 3, 3);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 8, 1, 2, 4);

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_01", "stage1_01_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-22f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, new Vector3(5f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(24f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_01_ToOutpost", "stage1_01_exit_outpost", HWJ_SpawnPointType.NPC, new Vector3(39f, -3.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-22f, -3.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateDirectPossessableCorpse(
            context.VisualRoot,
            "HWJ_Stage1_01_FirstPossessionCorpse",
            SwordEnemyRootPath,
            SwordEnemySpritePath,
            new Vector3(-19.2f, -3.05f, 0f));

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
            new Vector3(39f, -2.55f, 0f),
            "HWJ_Stage1_02_RuinedOutpost",
            "stage1_02_player_start",
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Spike_Left", new Vector3(-0.5f, -3.45f, 0f), new Vector2(2.8f, 0.6f), 18f, 2.6f);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Spike_Right", new Vector3(33.5f, -3.45f, 0f), new Vector2(3.6f, 0.6f), 18f, 2.6f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Steam_Left", new Vector3(-22f, -2.55f, 0f), new Vector2(5f, 1.2f), 12f, 2f, 2.4f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_01_Steam_Right", new Vector3(27f, -2.55f, 0f), new Vector2(5f, 1.2f), 12f, 2f, 2.4f);
        CreateSpiritOrbSwitchShowcase(
            context,
            tiles,
            "HWJ_Stage1_01_SpiritOrbSwitch",
            "stage1_01.spirit_switch.shortcut",
            new Vector3(-11f, -1.4f, 0f),
            new Vector3(-9f, -2.3f, 0f));

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-22f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SwordEnemy_Visual", SwordEnemySpritePath, new Vector3(5f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_BowEnemy_Visual", BowEnemySpritePath, new Vector3(24f, -2.8f, 0f), 1f, 20);

        SaveContextScene(context);
    }

    private static void BuildRuinedOutpostScene()
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene02Path,
            "HWJ_Stage1_02_RuinedOutpost",
            "1-3 오래된 기사단 주둔지 입구",
            new Vector3(10f, 1.1f, -10f),
            8f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -28, -8, 80, 22);
        PaintRect(context.Ground, tiles.OcherSandFloor, -22, -7, 65, 3);
        PaintRect(context.Ground, tiles.CrackedStone, 6, -4, 12, 1);
        PaintRect(context.Ground, tiles.CrackedStone, 23, -4, 17, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, -7, -1, 8, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 12, 1, 9, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 30, 2, 8, 1);

        PaintRect(context.Decoration, tiles.RustyGate, 30, -4, 2, 7);
        PaintRect(context.Decoration, tiles.RustyGate, 20, -4, 1, 5);
        PaintRect(context.Decoration, tiles.RuinedWall, -16, -4, 8, 5);
        PaintRect(context.Decoration, tiles.RuinedWall, 36, -4, 5, 7);
        PaintRect(context.Decoration, tiles.CollapsedRoof, -10, 1, 8, 1);
        PaintRect(context.Decoration, tiles.CollapsedRoof, 11, 3, 9, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -20, 1, 10, 2);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 8, -4, 4, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 34, -4, 3, 1);
        PaintSpiritOnlyPassage(context, tiles, -4, -3, 9, 6, true);
        PaintSpiritOnlyPassage(context, tiles, 21, -3, 7, 5, false);
        PaintSpiritOrbLine(context, tiles, -2, 0, 5);
        PaintSpiritOrbLine(context, tiles, 22, -1, 4);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 1, 2, 3, 3);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 26, 1, 2, 4);

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_02", "stage1_02_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-20f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Shield", "stage1_02_enemy_shield", HWJ_SpawnPointType.Enemy, new Vector3(6f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Bow", "stage1_02_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(25f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_02_ToBackRoad", "stage1_02_exit_backroad", HWJ_SpawnPointType.NPC, new Vector3(40f, -3.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-20f, -3.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateLivePossessionResistTarget(
            context.VisualRoot,
            "HWJ_Stage1_02_LivePossessionResistTarget",
            new Vector3(-16.5f, -3.0f, 0f));
        CreateBodyObstacleGate(
            context,
            tiles,
            "HWJ_Stage1_02_SoulOnlyShortcut",
            "stage1_02.body_obstacle.soul_only",
            new Vector3(-4.5f, -2.1f, 0f),
            new Vector2(1f, 3.4f),
            HWJ_BodyObstacleRequirementMode.SpiritOnly,
            new HWJ_WeaponType[0]);
        CreateRewardPickup(context.VisualRoot, ExperienceOrbPrefabPath, "HWJ_Stage1_02_ExperienceOrb_80", new Vector3(-12.5f, -2.3f, 0f), 80);
        CreateRewardPickup(context.VisualRoot, AttackStatOrbPrefabPath, "HWJ_Stage1_02_AttackStatOrb", new Vector3(-11.4f, -2.3f, 0f), 0);

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_02_SpawnTable.asset",
            "spawn.stage1.02",
            new HWJ_Stage1SpawnRequest("stage1_02_spawn_shield", "stage1_02_enemy_shield", HWJ_SpawnPointType.Enemy, ShieldEnemyRootPath, ShieldEnemyPrefabPath, 1),
            new HWJ_Stage1SpawnRequest("stage1_02_spawn_bow", "stage1_02_enemy_bow", HWJ_SpawnPointType.Enemy, BowEnemyRootPath, BowEnemyPrefabPath, 1),
            new HWJ_Stage1SpawnRequest("stage1_02_spawn_axe", "stage1_02_enemy_axe", HWJ_SpawnPointType.Enemy, AxeEnemyRootPath, AxeEnemyPrefabPath, 1));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Axe", "stage1_02_enemy_axe", HWJ_SpawnPointType.Enemy, new Vector3(34f, -3.2f, 0f));
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.02",
            "stage.region01.03",
            spawnTable,
            false);
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_02_ToBackRoad",
            new Vector3(40f, -2.55f, 0f),
            "HWJ_Stage1_03_BackRoad",
            "stage1_03_player_start",
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Spike_Center", new Vector3(10f, -3.45f, 0f), new Vector2(3.8f, 0.6f), 20f, 2.6f);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Spike_Right", new Vector3(35.5f, -3.45f, 0f), new Vector2(3f, 0.6f), 20f, 2.6f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Steam_Left", new Vector3(-15f, -2.55f, 0f), new Vector2(5f, 1.2f), 14f, 2f, 2f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_02_Steam_Right", new Vector3(28f, -2.55f, 0f), new Vector2(5f, 1.2f), 14f, 2f, 2f);

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-20f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_ShieldEnemy_Visual", ShieldEnemySpritePath, new Vector3(6f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_BowEnemy_Visual", BowEnemySpritePath, new Vector3(25f, -2.8f, 0f), 1f, 20);

        SaveContextScene(context);
    }

    private static void BuildBackRoadScene()
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene03Path,
            "HWJ_Stage1_03_BackRoad",
            "Stage 1-3 back road and boss entrance",
            new Vector3(9f, 1.2f, -10f),
            8f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -28, -8, 78, 22);
        PaintRect(context.Ground, tiles.OcherSandFloor, -22, -7, 65, 3);
        PaintRect(context.Ground, tiles.CrackedStone, -2, -4, 9, 1);
        PaintRect(context.Ground, tiles.CrackedStone, 18, -3, 14, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, -12, 0, 7, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 7, 2, 8, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 27, 1, 8, 1);
        PaintRect(context.Decoration, tiles.RuinedWall, -18, -4, 9, 7);
        PaintRect(context.Decoration, tiles.RuinedWall, 2, -4, 8, 6);
        PaintRect(context.Decoration, tiles.RustyGate, 35, -4, 2, 7);
        PaintRect(context.Decoration, tiles.CollapsedRoof, -18, 3, 9, 1);
        PaintRect(context.Decoration, tiles.CollapsedRoof, 19, 2, 10, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -20, 1, 8, 2);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 18, -4, 5, 1);
        PaintSpiritOnlyPassage(context, tiles, -6, -3, 8, 6, true);
        PaintSpiritOnlyPassage(context, tiles, 22, -3, 7, 6, false);
        PaintSpiritOrbLine(context, tiles, -4, 0, 5);
        PaintSpiritOrbLine(context, tiles, 23, -1, 4);

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_03", "stage1_03_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-20f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_03_Lance", "stage1_03_enemy_lance", HWJ_SpawnPointType.Enemy, new Vector3(4f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_03_Axe", "stage1_03_enemy_axe", HWJ_SpawnPointType.Enemy, new Vector3(20f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_03_Shield", "stage1_03_enemy_shield", HWJ_SpawnPointType.Enemy, new Vector3(31f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_03_ToMidBoss", "stage1_03_exit_midboss", HWJ_SpawnPointType.NPC, new Vector3(40f, -3.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-20f, -3.2f, 0f));
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
            new Vector3(24f, -2.1f, 0f),
            new Vector2(1f, 3.4f),
            HWJ_BodyObstacleRequirementMode.WeaponType,
            new[] { HWJ_WeaponType.Sword });
        CreateRewardPickup(context.VisualRoot, ExperienceOrbPrefabPath, "HWJ_Stage1_03_ExperienceOrb_120", new Vector3(-12.5f, -2.3f, 0f), 120);
        CreateRewardPickup(context.VisualRoot, DefenseStatOrbPrefabPath, "HWJ_Stage1_03_DefenseStatOrb", new Vector3(-11.4f, -2.3f, 0f), 0);
        CreateStageChoiceRewardShowcase(context, runtimeSystems, new Vector3(34f, -2.4f, 0f));
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_03_ToMidBoss",
            new Vector3(40f, -2.55f, 0f),
            "HWJ_Stage1_04_MidBossBarracks",
            "stage1_04_player_start",
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Spike_Center", new Vector3(20.5f, -3.45f, 0f), new Vector2(4.5f, 0.6f), 22f, 2.5f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Steam_BossEntry", new Vector3(30f, -2.55f, 0f), new Vector2(5f, 1.2f), 15f, 2.2f, 2f);
        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-20f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_LanceEnemy_Visual", SwordEnemySpritePath, new Vector3(4f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_AxeEnemy_Visual", ShieldEnemySpritePath, new Vector3(20f, -2.8f, 0f), 1f, 20);

        SaveContextScene(context);
    }

    private static void BuildMidBossBarracksScene()
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            Stage1Scene04Path,
            "HWJ_Stage1_04_MidBossBarracks",
            "1-4 중간 보스 방 - 기사단 막사 내부",
            new Vector3(2f, 1.2f, -10f),
            8.5f);
        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        PaintBackground(context, tiles, -30, -9, 66, 23);
        PaintRect(context.Ground, tiles.BarracksWall, -24, -8, 54, 3);
        PaintRect(context.Ground, tiles.BarracksWall, -24, -5, 2, 12);
        PaintRect(context.Ground, tiles.BarracksWall, 28, -5, 2, 12);
        PaintRect(context.Ground, tiles.BarracksWall, -24, 7, 54, 2);
        PaintRect(context.Ground, tiles.CrackedStone, -13, -5, 28, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, -17, -1, 9, 1);
        PaintRect(context.OneWayPlatform, tiles.CrackedStone, 8, 0, 9, 1);

        PaintRect(context.Decoration, tiles.RustyGate, -23, -5, 1, 5);
        PaintRect(context.Decoration, tiles.RustyGate, 27, -5, 1, 5);
        PaintRect(context.Decoration, tiles.CollapsedRoof, -14, 4, 10, 1);
        PaintRect(context.Decoration, tiles.CollapsedRoof, 8, 4, 10, 1);
        PaintRect(context.Decoration, tiles.RuinedWall, -5, -5, 10, 4);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, -6, -5, 5, 1);
        PaintRect(context.HazardGuide, tiles.SpikeWarning, 8, -5, 5, 1);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, -18, 2, 12, 2);
        PaintRect(context.HazardGuide, tiles.SandstormGuide, 9, 2, 12, 2);
        PaintSpiritOnlyPassage(context, tiles, -20, -4, 8, 8, true);
        PaintSpiritOnlyPassage(context, tiles, 18, -4, 7, 8, false);
        PaintSpiritOrbLine(context, tiles, -18, -1, 4);
        PaintSpiritOrbLine(context, tiles, 18, -1, 4);
        PaintRect(context.HazardGuide, tiles.SpiritWind, -19, 3, 3, 3);
        PaintRect(context.HazardGuide, tiles.SpiritWind, 22, 3, 3, 3);

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_04", "stage1_04_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-20f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSpawn_Stage1_04", "stage1_04_midboss_spawn", HWJ_SpawnPointType.Boss, new Vector3(9f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSummon_Stage1_04_Left", "stage1_04_midboss_summon_left", HWJ_SpawnPointType.Enemy, new Vector3(-12f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSummon_Stage1_04_Right", "stage1_04_midboss_summon_right", HWJ_SpawnPointType.Enemy, new Vector3(18f, -4.2f, 0f));
        GameObject playerObject = CreatePlayablePlayer(context, new Vector3(-20f, -4.2f, 0f));
        ConfigureMainCameraFollow(playerObject);
        CreateDirectPossessableCorpse(
            context.VisualRoot,
            "HWJ_Stage1_04_BossEntryCorpse",
            ShieldEnemyRootPath,
            ShieldEnemySpritePath,
            new Vector3(-16.5f, -4.05f, 0f));

        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateSpawnTable(
            "HWJ_Stage1_04_SpawnTable.asset",
            "spawn.stage1.04");
        HWJ_StageRuntimeSystems runtimeSystems = CreateStageRuntimeSystems(
            context,
            "stage.region01.04",
            "stage.region01.next",
            spawnTable,
            true);
        GameObject midBossObject = CreateMidBossRuntimeObject(context, new Vector3(9f, -4.2f, 0f));
        CreateBossFlowShowcase(context, runtimeSystems, midBossObject);
        CreateScenePortal(
            context,
            "HWJ_Portal_Stage1_04_ToTitle",
            new Vector3(26f, -3.55f, 0f),
            "Title",
            string.Empty,
            runtimeSystems,
            tiles.RustyGate);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Spike_Left", new Vector3(-3.5f, -4.45f, 0f), new Vector2(4.8f, 0.6f), 24f, 2.4f);
        CreateSpikeTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Spike_Right", new Vector3(10.5f, -4.45f, 0f), new Vector2(4.8f, 0.6f), 24f, 2.4f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Steam_Left", new Vector3(-12f, -3.55f, 0f), new Vector2(5.5f, 1.4f), 16f, 2.2f, 1.8f);
        CreateSteamTrap(context.TrapRoot, "HWJ_Trap_Stage1_03_Steam_Right", new Vector3(16f, -3.55f, 0f), new Vector2(5.5f, 1.4f), 16f, 2.2f, 1.8f);

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-20f, -3.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_MidBoss_Visual", MidBossSpritePath, new Vector3(9f, -3.4f, 0f), 1.4f, 22);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SummonEnemy_Left_Visual", SwordEnemySpritePath, new Vector3(-12f, -3.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SummonEnemy_Right_Visual", ShieldEnemySpritePath, new Vector3(18f, -3.8f, 0f), 1f, 20);

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
        grid.cellSize = Vector3.one;

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

        SetLayerIfExists(tilemapObject, "Ground");
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

    private static void PaintBackground(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, int x, int y, int width, int height)
    {
        PaintRect(context.Background, tiles.BackgroundSand, x, y, width, height);
    }

    private static void PaintRuinedHouse(HWJ_Stage1SceneContext context, HWJ_Stage1TileSet tiles, int x, int y, int width, int height)
    {
        PaintRect(context.Decoration, tiles.RuinedWall, x, y, 1, height);
        PaintRect(context.Decoration, tiles.RuinedWall, x + width - 1, y, 1, height - 2);
        PaintRect(context.Decoration, tiles.CollapsedRoof, x, y + height - 1, width, 1);
        PaintRect(context.Decoration, tiles.CollapsedRoof, x + 2, y + height - 2, width - 4, 1);
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
        PaintRect(context.Decoration, tiles.SpiritWind, x + 1, y + 1, Mathf.Max(1, width - 2), Mathf.Max(1, height - 2));
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
        bool countBossObjects)
    {
        GameObject spawnerObject = new GameObject("HWJ_StageSpawnerSystem");
        spawnerObject.transform.SetParent(context.SystemRoot);
        HWJ_SpawnerSystem spawner = spawnerObject.AddComponent<HWJ_SpawnerSystem>();
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("spawnTable").objectReferenceValue = spawnTable;
        serializedSpawner.FindProperty("autoCollectSpawnPoints").boolValue = true;
        serializedSpawner.FindProperty("spawnOnStart").boolValue = true;
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

        Sprite markerSprite = GetTileSprite(markerTile);

        if (markerSprite != null)
        {
            SpriteRenderer renderer = portalObject.AddComponent<SpriteRenderer>();
            renderer.sprite = markerSprite;
            renderer.sortingOrder = 28;
            portalObject.transform.localScale = new Vector3(2f, 2.8f, 1f);
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
        float detectionDistance)
    {
        GameObject trapObject = new GameObject(objectName);
        trapObject.transform.SetParent(parent);
        trapObject.transform.position = position;

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
        float inactiveDuration)
    {
        GameObject trapObject = new GameObject(objectName);
        trapObject.transform.SetParent(parent);
        trapObject.transform.position = position;

        SpriteRenderer renderer = trapObject.AddComponent<SpriteRenderer>();
        renderer.color = new Color(1f, 1f, 1f, 0.2f);
        renderer.sortingOrder = 26;

        BoxCollider2D collider = trapObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        Component trap = AddExistingTrapComponent(trapObject, "HSH_SteamTrap");
        SetSerializedFloat(trap, "damage", damage);
        SetSerializedFloat(trap, "activeDuration", activeDuration);
        SetSerializedFloat(trap, "inactiveDuration", inactiveDuration);
        SetSerializedFloat(trap, "damageCooldown", 1f);
        SetSerializedFloat(trap, "knockbackPower", 18f);
    }

    private static Component AddExistingTrapComponent(GameObject owner, string componentTypeName)
    {
        Type componentType = Type.GetType(componentTypeName + ", Assembly-CSharp");

        if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
        {
            Debug.LogWarning($"HWJ stage 1 trap skipped because existing trap component was not found: {componentTypeName}");
            return null;
        }

        return owner.AddComponent(componentType);
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

    private static Sprite GetTileSprite(TileBase tile)
    {
        return tile is Tile concreteTile ? concreteTile.sprite : null;
    }

    private static void EnsureRuntimeEnemyPrefabs()
    {
        EnsureRuntimeEnemyPrefab(SwordEnemyPrefabPath, SwordEnemySpritePath);
        EnsureRuntimeEnemyPrefab(BowEnemyPrefabPath, BowEnemySpritePath);
        EnsureRuntimeEnemyPrefab(ShieldEnemyPrefabPath, ShieldEnemySpritePath);
        EnsureRuntimeEnemyPrefab(AxeEnemyPrefabPath, SwordEnemySpritePath);
        EnsureRuntimeEnemyPrefab(LanceEnemyPrefabPath, SwordEnemySpritePath);
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
        Sprite sprite = LoadMarkerSprite(spritePath);
        EnsureRuntimeEnemyComponents(prefabRoot, sprite, true, false);
        ConfigureCombatExecutionTargetFilter(prefabRoot.GetComponent<HWJ_CombatExecutionSystem>(), false);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static GameObject CreatePlayablePlayer(HWJ_Stage1SceneContext context, Vector3 position)
    {
        GameObject playerObject = new GameObject("HWJ_Player");
        playerObject.transform.SetParent(context.Root);
        playerObject.transform.position = position;
        playerObject.tag = "Player";

        SpriteRenderer renderer = playerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadMarkerSprite(PlayerSpritePath);
        renderer.sortingOrder = 30;

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.75f, 1.25f);
        collider.offset = new Vector2(0f, 0.05f);

        playerObject.AddComponent<HWJ_RuntimeObjectContext>();
        HWJ_RootObjectDataResolver resolver = playerObject.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(PlayerRootPath));
        playerObject.AddComponent<HWJ_RuntimeStatusSystem>();
        playerObject.AddComponent<HWJ_CombatSystem>();
        HWJ_CombatExecutionSystem combatExecution = playerObject.AddComponent<HWJ_CombatExecutionSystem>();
        playerObject.AddComponent<HWJ_SkillActionSystem>();
        playerObject.AddComponent<HWJ_KnockbackSystem>();
        playerObject.AddComponent<HWJ_CharacterMotionSystem>();
        HWJ_PlayerInputSystem inputSystem = playerObject.AddComponent<HWJ_PlayerInputSystem>();
        playerObject.AddComponent<HWJ_PlayerMovementSystem>();
        playerObject.AddComponent<HWJ_PlayerAttackSystem>();
        playerObject.AddComponent<HWJ_LevelUpSystem>();
        playerObject.AddComponent<HWJ_StatOrbProgressSystem>();
        playerObject.AddComponent<HWJ_SkillUnlockSystem>();
        playerObject.AddComponent<HWJ_SoulSystem>();
        playerObject.AddComponent<HWJ_PossessedBodySystem>();
        playerObject.AddComponent<HWJ_PossessionSystem>();
        playerObject.AddComponent<HWJ_BodyDiscoverySystem>();
        playerObject.AddComponent<HWJ_PossessionMentalSystem>();
        playerObject.AddComponent<HWJ_CollapseSystem>();

        SerializedObject serializedInput = new SerializedObject(inputSystem);
        serializedInput.FindProperty("inputBindingData").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<HWJ_PlayerInputBindingDataSO>(PlayerInputBindingPath);
        serializedInput.ApplyModifiedPropertiesWithoutUndo();

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

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(owner);
        collider.isTrigger = colliderIsTrigger;
        collider.size = new Vector2(0.82f, 1.18f);
        collider.offset = new Vector2(0f, 0.06f);

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

        if (includeBehaviorSystems)
        {
            EnsureComponent<HWJ_EnemyNavigationSystem>(owner);
            EnsureComponent<HWJ_EnemyAttackSystem>(owner);
            EnsureComponent<HWJ_MonsterAISystem>(owner);
        }
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
            context.VisualRoot,
            objectName + "_Gate",
            tiles.SpiritSealWall,
            gatePosition,
            new Vector3(1f, 3.2f, 1f),
            26);
        BoxCollider2D gateCollider = gateObject.AddComponent<BoxCollider2D>();
        gateCollider.size = Vector2.one;

        GameObject switchObject = CreateTileSpriteObject(
            context.VisualRoot,
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

    private static void CreateBodyObstacleGate(
        HWJ_Stage1SceneContext context,
        HWJ_Stage1TileSet tiles,
        string objectName,
        string obstacleId,
        Vector3 position,
        Vector2 size,
        HWJ_BodyObstacleRequirementMode requirementMode,
        HWJ_WeaponType[] allowedWeapons)
    {
        GameObject gateObject = CreateTileSpriteObject(
            context.VisualRoot,
            objectName + "_Gate",
            requirementMode == HWJ_BodyObstacleRequirementMode.SpiritOnly ? tiles.SpiritSealWall : tiles.RustyGate,
            position,
            new Vector3(size.x, size.y, 1f),
            26);
        BoxCollider2D gateCollider = gateObject.AddComponent<BoxCollider2D>();
        gateCollider.size = Vector2.one;

        GameObject controllerObject = new GameObject(objectName + "_System");
        controllerObject.transform.SetParent(context.SystemRoot);
        controllerObject.transform.position = position;
        HWJ_BodyExclusiveObstacleSystem obstacleSystem = controllerObject.AddComponent<HWJ_BodyExclusiveObstacleSystem>();

        SerializedObject serializedObstacle = new SerializedObject(obstacleSystem);
        serializedObstacle.FindProperty("obstacleId").stringValue = obstacleId;
        serializedObstacle.FindProperty("requirementMode").enumValueIndex = (int)requirementMode;
        serializedObstacle.FindProperty("openWhenRequirementMet").boolValue = true;
        serializedObstacle.FindProperty("updateContinuously").boolValue = true;
        serializedObstacle.FindProperty("requirePlayerTag").boolValue = true;
        SetEnumArray(serializedObstacle.FindProperty("allowedWeaponTypes"), allowedWeapons);
        SetObjectArray(serializedObstacle.FindProperty("obstacleColliders"), gateCollider);
        SetObjectArray(serializedObstacle.FindProperty("hideWhenOpen"), gateObject);
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
        GameObject tileObject = new GameObject(objectName);
        tileObject.transform.SetParent(parent);
        tileObject.transform.position = position;
        tileObject.transform.localScale = scale;

        SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetTileSprite(tile);
        renderer.sortingOrder = sortingOrder;
        return tileObject;
    }

    private static T EnsureComponent<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : owner.AddComponent<T>();
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

    private static Sprite LoadMarkerSprite(string spritePath)
    {
        string fullPath = Path.GetFullPath(spritePath);

        if (!File.Exists(fullPath))
        {
            return null;
        }

        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
    }

    private static void SaveContextScene(HWJ_Stage1SceneContext context)
    {
        EditorSceneManager.MarkSceneDirty(context.Scene);
        EditorSceneManager.SaveScene(context.Scene, context.ScenePath);
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
            Transform visualRoot)
        {
            Scene = scene;
            ScenePath = scenePath;
            Root = root;
            Background = background;
            Ground = ground;
            OneWayPlatform = oneWayPlatform;
            Decoration = decoration;
            HazardGuide = hazardGuide;
            SystemRoot = systemRoot;
            SpawnRoot = spawnRoot;
            PortalRoot = portalRoot;
            TrapRoot = trapRoot;
            VisualRoot = visualRoot;
        }

        public Scene Scene { get; }
        public string ScenePath { get; }
        public Transform Root { get; }
        public Tilemap Background { get; }
        public Tilemap Ground { get; }
        public Tilemap OneWayPlatform { get; }
        public Tilemap Decoration { get; }
        public Tilemap HazardGuide { get; }
        public Transform SystemRoot { get; }
        public Transform SpawnRoot { get; }
        public Transform PortalRoot { get; }
        public Transform TrapRoot { get; }
        public Transform VisualRoot { get; }
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
