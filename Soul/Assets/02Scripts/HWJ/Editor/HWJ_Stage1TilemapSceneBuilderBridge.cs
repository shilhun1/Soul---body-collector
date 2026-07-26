using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
    private const string SpriteRoot = HwjRoot + "/Art/Generated/Stage1";
    private const string TileRoot = HwjRoot + "/Tiles/Generated/Stage1";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunStage1TilemapBuild.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_Stage1TilemapSceneBuildReport.md";

    private const string PlayerSpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Player_Test.png";
    private const string SwordEnemySpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Enemy_Corpse_Sword.png";
    private const string BowEnemySpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Enemy_Corpse_Bow.png";
    private const string ShieldEnemySpritePath = HwjRoot + "/Art/Generated/Models/HWJ_Model_Enemy_Corpse_Shield.png";
    private const string MidBossSpritePath = SpriteRoot + "/HWJ_Stage1_Marker_MidBoss.png";

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

        HWJ_Stage1TileSet tiles = CreateOrLoadTileSet();

        BuildRuinedVillageScene(tiles);
        BuildRuinedOutpostScene(tiles);
        BuildMidBossBarracksScene(tiles);

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
        builder.AppendLine("  - Assets/01Scenes/HWJ_Stage1_01_RuinedVillage.unity");
        builder.AppendLine("  - Assets/01Scenes/HWJ_Stage1_02_RuinedOutpost.unity");
        builder.AppendLine("  - Assets/01Scenes/HWJ_Stage1_03_MidBossBarracks.unity");
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
        Sprite backgroundSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_BackgroundSand.png",
            new Color32(92, 67, 42, 255),
            HWJ_Stage1TilePattern.SoftNoise);
        Sprite sandFloorSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_OcherSandFloor.png",
            new Color32(178, 129, 63, 255),
            HWJ_Stage1TilePattern.SandFloor);
        Sprite crackedStoneSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_CrackedStone.png",
            new Color32(126, 104, 80, 255),
            HWJ_Stage1TilePattern.CrackedStone);
        Sprite villageWallSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_RuinedWall.png",
            new Color32(139, 96, 58, 255),
            HWJ_Stage1TilePattern.RuinedWall);
        Sprite roofWoodSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_CollapsedRoof.png",
            new Color32(92, 59, 37, 255),
            HWJ_Stage1TilePattern.Wood);
        Sprite warningSpikeSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_SpikeWarning.png",
            new Color32(210, 73, 43, 255),
            HWJ_Stage1TilePattern.SpikeWarning);
        Sprite sandstormSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_SandstormGuide.png",
            new Color32(224, 175, 88, 190),
            HWJ_Stage1TilePattern.Sandstorm);
        Sprite barracksWallSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_BarracksWall.png",
            new Color32(75, 64, 62, 255),
            HWJ_Stage1TilePattern.BarracksWall);
        Sprite gateSprite = CreateOrLoadSprite(
            SpriteRoot + "/HWJ_Stage1_Tile_RustyGate.png",
            new Color32(103, 72, 48, 255),
            HWJ_Stage1TilePattern.RustyGate);
        CreateOrLoadSprite(
            MidBossSpritePath,
            new Color32(134, 39, 35, 255),
            HWJ_Stage1TilePattern.MidBossMarker);

        return new HWJ_Stage1TileSet
        {
            BackgroundSand = CreateOrLoadTile("HWJ_Stage1_BackgroundSand.asset", backgroundSprite, Tile.ColliderType.None),
            OcherSandFloor = CreateOrLoadTile("HWJ_Stage1_OcherSandFloor.asset", sandFloorSprite, Tile.ColliderType.Grid),
            CrackedStone = CreateOrLoadTile("HWJ_Stage1_CrackedStone.asset", crackedStoneSprite, Tile.ColliderType.Grid),
            RuinedWall = CreateOrLoadTile("HWJ_Stage1_RuinedWall.asset", villageWallSprite, Tile.ColliderType.Grid),
            CollapsedRoof = CreateOrLoadTile("HWJ_Stage1_CollapsedRoof.asset", roofWoodSprite, Tile.ColliderType.None),
            SpikeWarning = CreateOrLoadTile("HWJ_Stage1_SpikeWarning.asset", warningSpikeSprite, Tile.ColliderType.None),
            SandstormGuide = CreateOrLoadTile("HWJ_Stage1_SandstormGuide.asset", sandstormSprite, Tile.ColliderType.None),
            BarracksWall = CreateOrLoadTile("HWJ_Stage1_BarracksWall.asset", barracksWallSprite, Tile.ColliderType.Grid),
            RustyGate = CreateOrLoadTile("HWJ_Stage1_RustyGate.asset", gateSprite, Tile.ColliderType.Grid)
        };
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

    private static void BuildRuinedVillageScene(HWJ_Stage1TileSet tiles)
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            "Assets/01Scenes/HWJ_Stage1_01_RuinedVillage.unity",
            "HWJ_Stage1_01_RuinedVillage",
            "1-1 / 1-2 황폐한 마을 입구와 내부",
            new Vector3(8f, 1.3f, -10f),
            8f);

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

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_01", "stage1_01_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-22f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Sword", "stage1_01_enemy_sword", HWJ_SpawnPointType.Enemy, new Vector3(5f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_01_Bow", "stage1_01_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(24f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_01_ToOutpost", "stage1_01_exit_outpost", HWJ_SpawnPointType.NPC, new Vector3(39f, -3.2f, 0f));

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-22f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SwordEnemy_Visual", SwordEnemySpritePath, new Vector3(5f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_BowEnemy_Visual", BowEnemySpritePath, new Vector3(24f, -2.8f, 0f), 1f, 20);

        CreateGuideLabel(context.LabelRoot, "HWJ_Label_Stage1_01_Title", "황폐한 마을 입구 / 내부", new Vector3(-24f, 5.3f, 0f));
        CreateGuideLabel(context.LabelRoot, "HWJ_Label_Stage1_01_Traps", "잔가시, 지붕 낙석, 모래폭풍 배치", new Vector3(16f, 5.3f, 0f));

        SaveContextScene(context);
    }

    private static void BuildRuinedOutpostScene(HWJ_Stage1TileSet tiles)
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            "Assets/01Scenes/HWJ_Stage1_02_RuinedOutpost.unity",
            "HWJ_Stage1_02_RuinedOutpost",
            "1-3 오래된 기사단 주둔지 입구",
            new Vector3(10f, 1.1f, -10f),
            8f);

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

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_02", "stage1_02_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-20f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Shield", "stage1_02_enemy_shield", HWJ_SpawnPointType.Enemy, new Vector3(6f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_EnemySpawn_Stage1_02_Bow", "stage1_02_enemy_bow", HWJ_SpawnPointType.Enemy, new Vector3(25f, -3.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_Exit_Stage1_02_ToMidBoss", "stage1_02_exit_midboss", HWJ_SpawnPointType.NPC, new Vector3(40f, -3.2f, 0f));

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-20f, -2.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_ShieldEnemy_Visual", ShieldEnemySpritePath, new Vector3(6f, -2.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_BowEnemy_Visual", BowEnemySpritePath, new Vector3(25f, -2.8f, 0f), 1f, 20);

        CreateGuideLabel(context.LabelRoot, "HWJ_Label_Stage1_02_Title", "오래된 기사단 주둔지 입구", new Vector3(-22f, 5.3f, 0f));
        CreateGuideLabel(context.LabelRoot, "HWJ_Label_Stage1_02_Gate", "녹슨 문과 낡은 표지판 구간", new Vector3(18f, 5.3f, 0f));

        SaveContextScene(context);
    }

    private static void BuildMidBossBarracksScene(HWJ_Stage1TileSet tiles)
    {
        HWJ_Stage1SceneContext context = CreateSceneContext(
            "Assets/01Scenes/HWJ_Stage1_03_MidBossBarracks.unity",
            "HWJ_Stage1_03_MidBossBarracks",
            "1-4 중간 보스 방 - 기사단 막사 내부",
            new Vector3(2f, 1.2f, -10f),
            8.5f);

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

        CreateSpawnPoint(context.SpawnRoot, "HWJ_PlayerStart_Stage1_03", "stage1_03_player_start", HWJ_SpawnPointType.PlayerStart, new Vector3(-20f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSpawn_Stage1_03", "stage1_03_midboss_spawn", HWJ_SpawnPointType.Boss, new Vector3(9f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSummon_Stage1_03_Left", "stage1_03_midboss_summon_left", HWJ_SpawnPointType.Enemy, new Vector3(-12f, -4.2f, 0f));
        CreateSpawnPoint(context.SpawnRoot, "HWJ_MidBossSummon_Stage1_03_Right", "stage1_03_midboss_summon_right", HWJ_SpawnPointType.Enemy, new Vector3(18f, -4.2f, 0f));

        CreateSpriteMarker(context.VisualRoot, "HWJ_PlayerStart_Visual", PlayerSpritePath, new Vector3(-20f, -3.7f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_MidBoss_Visual", MidBossSpritePath, new Vector3(9f, -3.4f, 0f), 1.4f, 22);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SummonEnemy_Left_Visual", SwordEnemySpritePath, new Vector3(-12f, -3.8f, 0f), 1f, 20);
        CreateSpriteMarker(context.VisualRoot, "HWJ_SummonEnemy_Right_Visual", ShieldEnemySpritePath, new Vector3(18f, -3.8f, 0f), 1f, 20);

        CreateGuideLabel(context.LabelRoot, "HWJ_Label_Stage1_03_Title", "기사단 막사 내부 - 중간보스 방", new Vector3(-21f, 5.4f, 0f));
        CreateGuideLabel(context.LabelRoot, "HWJ_Label_Stage1_03_Boss", "보스 전용 별도 씬 / 소환 위치 포함", new Vector3(5f, 5.4f, 0f));

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

        Transform spawnRoot = new GameObject("HWJ_SceneSpawnPoints").transform;
        spawnRoot.SetParent(root.transform);
        Transform visualRoot = new GameObject("HWJ_VisualSpawnMarkers").transform;
        visualRoot.SetParent(root.transform);
        Transform labelRoot = new GameObject("HWJ_SceneGuideLabels").transform;
        labelRoot.SetParent(root.transform);

        CreateCamera(cameraPosition, cameraSize);
        CreateDirectionalLight();
        CreateGuideLabel(labelRoot, "HWJ_Label_SceneDescription", sceneDescription, new Vector3(cameraPosition.x - 7f, cameraPosition.y + 5.2f, 0f));

        return new HWJ_Stage1SceneContext(
            scene,
            scenePath,
            background,
            ground,
            oneWayPlatform,
            decoration,
            hazardGuide,
            spawnRoot,
            visualRoot,
            labelRoot);
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

    private static void PaintRect(Tilemap tilemap, TileBase tile, int x, int y, int width, int height)
    {
        for (int tileX = x; tileX < x + width; tileX++)
        {
            for (int tileY = y; tileY < y + height; tileY++)
            {
                tilemap.SetTile(new Vector3Int(tileX, tileY, 0), tile);
            }
        }
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

    private static void CreateGuideLabel(Transform parent, string objectName, string text, Vector3 position)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(parent);
        labelObject.transform.position = position;

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.characterSize = 0.32f;
        textMesh.fontSize = 40;
        textMesh.color = new Color(0.98f, 0.86f, 0.62f, 1f);
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
            Tilemap background,
            Tilemap ground,
            Tilemap oneWayPlatform,
            Tilemap decoration,
            Tilemap hazardGuide,
            Transform spawnRoot,
            Transform visualRoot,
            Transform labelRoot)
        {
            Scene = scene;
            ScenePath = scenePath;
            Background = background;
            Ground = ground;
            OneWayPlatform = oneWayPlatform;
            Decoration = decoration;
            HazardGuide = hazardGuide;
            SpawnRoot = spawnRoot;
            VisualRoot = visualRoot;
            LabelRoot = labelRoot;
        }

        public Scene Scene { get; }
        public string ScenePath { get; }
        public Tilemap Background { get; }
        public Tilemap Ground { get; }
        public Tilemap OneWayPlatform { get; }
        public Tilemap Decoration { get; }
        public Tilemap HazardGuide { get; }
        public Transform SpawnRoot { get; }
        public Transform VisualRoot { get; }
        public Transform LabelRoot { get; }
    }

    private sealed class HWJ_Stage1TileSet
    {
        public Tile BackgroundSand;
        public Tile OcherSandFloor;
        public Tile CrackedStone;
        public Tile RuinedWall;
        public Tile CollapsedRoof;
        public Tile SpikeWarning;
        public Tile SandstormGuide;
        public Tile BarracksWall;
        public Tile RustyGate;
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
        BarracksWall,
        RustyGate,
        MidBossMarker
    }
}
