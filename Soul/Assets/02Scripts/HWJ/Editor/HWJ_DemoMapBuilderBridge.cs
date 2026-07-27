using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 교수님 시연 영상을 위해 HWJ 씬에 보이는 데모 맵을 생성하는 에디터 도구입니다.
/// 사용 에셋은 Assets/02Scripts/HWJ 아래에 생성하거나 이미 저장된 HWJ 에셋만 참조합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_DemoMapBuilderBridge
{
    private const string ScenePath = "Assets/01Scenes/HWJ.unity";
    private const string HwjAssetRoot = "Assets/02Scripts/HWJ";
    private const string SourceTileRoot = "Assets/08Tileset";
    private const string MapSpriteRoot = HwjAssetRoot + "/Art/Generated/Map";
    private const string ShowcaseAssetRoot = HwjAssetRoot + "/ScriptableObjects/Showcase";
    private const string ShowcaseRewardRoot = ShowcaseAssetRoot + "/Rewards";
    private const string ShowcaseSpawnTableRoot = ShowcaseAssetRoot + "/SpawnTables";
    private const string ShowcaseEnemyTypeRoot = ShowcaseAssetRoot + "/TypeData/Enemy";
    private const string ShowcaseEnemyRootObjectRoot = ShowcaseAssetRoot + "/RootObjects/Enemies";
    private const string ShowcasePrefabRoot = HwjAssetRoot + "/Prefabs/Generated/Showcase";
    private const string ShowcaseStageChoiceRewardPath = ShowcaseRewardRoot + "/HWJ_ProfessorDemo_StageChoiceReward.asset";
    private const string ShowcaseSpawnTablePath = ShowcaseSpawnTableRoot + "/HWJ_ProfessorDemo_SpawnTable.asset";
    private const string ShowcaseEnemyRuntimePrefabPath = ShowcasePrefabRoot + "/HWJ_ProfessorDemo_EnemyRuntime_Prefab.prefab";
    private const string LivePossessionEnemyTypePath = ShowcaseEnemyTypeRoot + "/HWJ_ProfessorDemo_LivePossessionResist_TypeData.asset";
    private const string LivePossessionRootObjectPath = ShowcaseEnemyRootObjectRoot + "/HWJ_ProfessorDemo_LivePossessionResist_RootObjectData.asset";
    private const string DemoMapRootName = "HWJ_VideoDemoMap";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunVideoDemoMapBuild.flag";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_DemoMapBuilderBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Scene/Build HWJ Video Demo Map")]
    public static void BuildDemoMapForRecording()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolders();

        HWJ_DemoMapSprites sprites = CreateOrLoadMapSprites();
        DeleteExistingDemoMapRoot();

        GameObject root = new GameObject(DemoMapRootName);
        CreateBackground(root.transform, sprites);
        CreateRouteGeometry(root.transform, sprites);
        CreateDemoLandmarks(root.transform, sprites);
        CreateDemoGameplayAdditions(root.transform, sprites);
        RepositionSceneGameplayObjects();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("HWJ video demo map build completed.");
    }

    [MenuItem("Tools/HWJ/Scene/Create HWJ Video Demo Map Build Flag")]
    public static void CreateVideoDemoMapBuildFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, "run");
        Debug.Log($"HWJ video demo map build flag created: {FlagFilePath}");
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
            Debug.LogWarning("HWJ video demo map build is already running.");
            return;
        }

        isRunning = true;

        try
        {
            BuildDemoMapForRecording();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/02Scripts", "HWJ");
        EnsureFolder(HwjAssetRoot, "Art");
        EnsureFolder(HwjAssetRoot + "/Art", "Generated");
        EnsureFolder(HwjAssetRoot + "/Art/Generated", "Map");
        EnsureFolder(HwjAssetRoot, "ScriptableObjects");
        EnsureFolder(HwjAssetRoot + "/ScriptableObjects", "Showcase");
        EnsureFolder(ShowcaseAssetRoot, "Rewards");
        EnsureFolder(ShowcaseAssetRoot, "SpawnTables");
        EnsureFolder(ShowcaseAssetRoot, "TypeData");
        EnsureFolder(ShowcaseAssetRoot + "/TypeData", "Enemy");
        EnsureFolder(ShowcaseAssetRoot, "RootObjects");
        EnsureFolder(ShowcaseAssetRoot + "/RootObjects", "Enemies");
        EnsureFolder(HwjAssetRoot, "Prefabs");
        EnsureFolder(HwjAssetRoot + "/Prefabs", "Generated");
        EnsureFolder(HwjAssetRoot + "/Prefabs/Generated", "Showcase");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static HWJ_DemoMapSprites CreateOrLoadMapSprites()
    {
        return new HWJ_DemoMapSprites
        {
            Background = Load08TileSpriteOrCreateFallback(
                "main_lev_build_102.asset",
                MapSpriteRoot + "/HWJ_MapTile_Background.png",
                new Color32(30, 34, 46, 255),
                HWJ_DemoMapSpritePattern.SubtleNoise),
            Floor = Load08TileSpriteOrCreateFallback(
                "main_lev_build_80.asset",
                MapSpriteRoot + "/HWJ_MapTile_Floor.png",
                new Color32(72, 76, 86, 255),
                HWJ_DemoMapSpritePattern.Ground),
            Platform = Load08TileSpriteOrCreateFallback(
                "main_lev_build_113.asset",
                MapSpriteRoot + "/HWJ_MapTile_Platform.png",
                new Color32(88, 94, 108, 255),
                HWJ_DemoMapSpritePattern.Platform),
            SpiritGate = Load08TileSpriteOrCreateFallback(
                "other_and_decorative_6.asset",
                MapSpriteRoot + "/HWJ_MapTile_SpiritGate.png",
                new Color32(138, 84, 230, 180),
                HWJ_DemoMapSpritePattern.Energy),
            SpiritOrb = Load08TileSpriteOrCreateFallback(
                "other_and_decorative_31.asset",
                MapSpriteRoot + "/HWJ_MapTile_SpiritOrb.png",
                new Color32(116, 202, 255, 230),
                HWJ_DemoMapSpritePattern.SpiritOrb),
            SpiritSeal = Load08TileSpriteOrCreateFallback(
                "other_and_decorative_34.asset",
                MapSpriteRoot + "/HWJ_MapTile_SpiritSeal.png",
                new Color32(104, 72, 174, 210),
                HWJ_DemoMapSpritePattern.SpiritSeal),
            PossessionZone = Load08TileSpriteOrCreateFallback(
                "other_and_decorative_18.asset",
                MapSpriteRoot + "/HWJ_MapTile_PossessionZone.png",
                new Color32(55, 132, 105, 200),
                HWJ_DemoMapSpritePattern.Energy),
            BossRoom = Load08TileSpriteOrCreateFallback(
                "main_lev_build_97.asset",
                MapSpriteRoot + "/HWJ_MapTile_BossRoom.png",
                new Color32(120, 42, 48, 220),
                HWJ_DemoMapSpritePattern.Boss),
            Hazard = Load08TileSpriteOrCreateFallback(
                "other_and_decorative_44.asset",
                MapSpriteRoot + "/HWJ_MapTile_Hazard.png",
                new Color32(196, 58, 58, 230),
                HWJ_DemoMapSpritePattern.Hazard)
        };
    }

    private static Sprite Load08TileSpriteOrCreateFallback(
        string sourceTileFileName,
        string fallbackSpritePath,
        Color32 fallbackColor,
        HWJ_DemoMapSpritePattern fallbackPattern)
    {
        string sourceTilePath = SourceTileRoot + "/" + sourceTileFileName;
        Tile sourceTile = AssetDatabase.LoadAssetAtPath<Tile>(sourceTilePath);

        if (sourceTile != null && sourceTile.sprite != null)
        {
            return sourceTile.sprite;
        }

        Debug.LogWarning($"HWJ demo map tile source missing. Fallback generated sprite will be used: {sourceTilePath}");
        return CreateOrLoadSprite(fallbackSpritePath, fallbackColor, fallbackPattern);
    }

    private static Sprite CreateOrLoadSprite(
        string assetPath,
        Color32 color,
        HWJ_DemoMapSpritePattern pattern)
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

    private static Texture2D CreateTexture(Color32 color, HWJ_DemoMapSpritePattern pattern)
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 pixel = ApplyPattern(color, x, y, pattern);
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private static Color32 ApplyPattern(
        Color32 baseColor,
        int x,
        int y,
        HWJ_DemoMapSpritePattern pattern)
    {
        int offset = pattern switch
        {
            HWJ_DemoMapSpritePattern.Ground => y > 24 ? 18 : (x + y) % 7 == 0 ? -10 : 0,
            HWJ_DemoMapSpritePattern.Platform => y is 0 or 31 ? 24 : (x % 8 == 0 ? -8 : 0),
            HWJ_DemoMapSpritePattern.Energy => (x + y) % 9 == 0 ? 36 : -6,
            HWJ_DemoMapSpritePattern.SpiritOrb => Mathf.Abs(x - 16) + Mathf.Abs(y - 16) < 10 ? 44 : -24,
            HWJ_DemoMapSpritePattern.SpiritSeal => x == y || x + y == 31 || x % 9 == 0 ? 34 : -14,
            HWJ_DemoMapSpritePattern.Boss => x % 10 < 2 || y % 10 < 2 ? 18 : -8,
            HWJ_DemoMapSpritePattern.Hazard => x == y || x + y == 31 ? 45 : -12,
            _ => (x * 17 + y * 11) % 13 == 0 ? 8 : 0
        };

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

    private static void DeleteExistingDemoMapRoot()
    {
        GameObject existingRoot = GameObject.Find(DemoMapRootName);

        if (existingRoot != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRoot);
        }
    }

    private static void CreateBackground(Transform root, HWJ_DemoMapSprites sprites)
    {
        CreateVisualRect(root, "HWJ_Map_Background_Main", sprites.Background, new Vector3(15f, 1f, 0f), new Vector2(68f, 18f), -100);
        CreateVisualRect(root, "HWJ_Map_Background_SpiritArea", sprites.SpiritGate, new Vector3(-12f, 0.9f, 0f), new Vector2(14f, 8f), -80, new Color(0.42f, 0.22f, 0.85f, 0.22f));
        CreateVisualRect(root, "HWJ_Map_Background_SpiritArea_Second", sprites.SpiritGate, new Vector3(21f, 0.8f, 0f), new Vector2(9f, 6f), -80, new Color(0.42f, 0.22f, 0.85f, 0.18f));
        CreateVisualRect(root, "HWJ_Map_Background_CombatArea", sprites.PossessionZone, new Vector3(8f, 0.3f, 0f), new Vector2(18f, 6.5f), -79, new Color(0.18f, 0.55f, 0.43f, 0.2f));
        CreateVisualRect(root, "HWJ_Map_Background_BossRoom", sprites.BossRoom, new Vector3(39f, 0.6f, 0f), new Vector2(24f, 8f), -78, new Color(0.55f, 0.16f, 0.16f, 0.23f));
    }

    private static void CreateRouteGeometry(Transform root, HWJ_DemoMapSprites sprites)
    {
        CreateSolidRect(root, "HWJ_Map_Floor_Start_To_Combat", sprites.Floor, new Vector3(2f, -3.2f, 0f), new Vector2(43f, 1f));
        CreateSolidRect(root, "HWJ_Map_Floor_BossRoom", sprites.Floor, new Vector3(39f, -3.2f, 0f), new Vector2(23f, 1f));
        CreateSolidRect(root, "HWJ_Map_Wall_BossRoom_Left_Upper", sprites.Floor, new Vector3(27.5f, 1.3f, 0f), new Vector2(0.7f, 4.5f));
        CreateSolidRect(root, "HWJ_Map_Wall_BossRoom_Right", sprites.Floor, new Vector3(50.5f, 0.5f, 0f), new Vector2(0.7f, 7f));
        CreateSolidRect(root, "HWJ_Map_Ceiling_BossRoom", sprites.Floor, new Vector3(39f, 4.1f, 0f), new Vector2(23f, 0.55f));

        CreatePlatform(root, "HWJ_Map_Platform_SpiritUpper", sprites.Platform, new Vector3(-12.5f, 1.1f, 0f), new Vector2(8f, 0.35f));
        CreatePlatform(root, "HWJ_Map_Platform_CombatLow", sprites.Platform, new Vector3(6f, -0.7f, 0f), new Vector2(5.8f, 0.35f));
        CreatePlatform(root, "HWJ_Map_Platform_CombatHigh", sprites.Platform, new Vector3(14f, 1.2f, 0f), new Vector2(5.2f, 0.35f));
        CreatePlatform(root, "HWJ_Map_Platform_BossEntry", sprites.Platform, new Vector3(24f, -1.15f, 0f), new Vector2(5f, 0.35f));

        CreateVisualRect(root, "HWJ_Map_SpiritOnly_Passage", sprites.SpiritGate, new Vector3(-8.3f, -0.6f, 0f), new Vector2(0.8f, 4.8f), 2, new Color(0.68f, 0.38f, 1f, 0.7f));
        CreateSpiritOnlyMechanicCluster(root, sprites, new Vector3(-14.5f, 0.4f, 0f), "Left");
        CreateSpiritOnlyMechanicCluster(root, sprites, new Vector3(-6.2f, 1.25f, 0f), "Upper");
        CreateSpiritOnlyMechanicCluster(root, sprites, new Vector3(21.5f, 0.2f, 0f), "BossEntry");
        CreateVisualRect(root, "HWJ_Map_PossessionZone_Marker", sprites.PossessionZone, new Vector3(8.5f, -2.55f, 0f), new Vector2(12f, 0.25f), 3, new Color(0.22f, 0.9f, 0.65f, 0.8f));
        CreateVisualRect(root, "HWJ_Map_BossDangerLine", sprites.Hazard, new Vector3(39f, -2.55f, 0f), new Vector2(20f, 0.22f), 3, new Color(1f, 0.25f, 0.22f, 0.82f));
    }

    private static void CreateDemoLandmarks(Transform root, HWJ_DemoMapSprites sprites)
    {
        CreateVisualRect(root, "HWJ_Map_Possession_HeartMarker", sprites.SpiritGate, new Vector3(5f, -1.7f, 0f), new Vector2(0.35f, 0.35f), 18, new Color(0.95f, 0.45f, 1f, 0.92f));
        CreateVisualRect(root, "HWJ_Map_BossRoom_EntranceMarker", sprites.Hazard, new Vector3(27.5f, -1.7f, 0f), new Vector2(0.35f, 2.1f), 18, new Color(1f, 0.32f, 0.28f, 0.92f));
    }

    private static void CreateDemoGameplayAdditions(Transform root, HWJ_DemoMapSprites sprites)
    {
        HWJ_DemoRuntimeSystems runtimeSystems = CreateDemoRuntimeSystems(root);
        CreateDemoPortal(root, sprites, runtimeSystems);
        CreateProfessorShowcaseObjects(root, sprites, runtimeSystems);

        CreateDemoSpikeTrap(root, "HWJ_DemoTrap_Spike_StartPit", new Vector3(-1.4f, -2.65f, 0f), new Vector2(3.8f, 0.55f), 18f, 3f);
        CreateDemoSpikeTrap(root, "HWJ_DemoTrap_Spike_BossEntry", new Vector3(24f, -2.65f, 0f), new Vector2(4f, 0.55f), 20f, 3f);
        CreateDemoSteamTrap(root, "HWJ_DemoTrap_Steam_LeftRoute", new Vector3(-14f, -2.25f, 0f), new Vector2(4.5f, 1.1f), 12f, 2f, 2.4f);
        CreateDemoSteamTrap(root, "HWJ_DemoTrap_Steam_CombatRoute", new Vector3(13f, -2.25f, 0f), new Vector2(4.5f, 1.1f), 12f, 2f, 2.4f);

        CreateVisualRect(root, "HWJ_DemoTrap_Spike_StartPit_Visual", sprites.Hazard, new Vector3(-1.4f, -2.55f, 0f), new Vector2(3.8f, 0.2f), 20, new Color(1f, 0.18f, 0.16f, 0.84f));
        CreateVisualRect(root, "HWJ_DemoTrap_Spike_BossEntry_Visual", sprites.Hazard, new Vector3(24f, -2.55f, 0f), new Vector2(4f, 0.2f), 20, new Color(1f, 0.18f, 0.16f, 0.84f));
    }

    private static void CreateProfessorShowcaseObjects(
        Transform root,
        HWJ_DemoMapSprites sprites,
        HWJ_DemoRuntimeSystems runtimeSystems)
    {
        GameObject enemyRuntimePrefab = CreateOrUpdateShowcaseEnemyRuntimePrefab(sprites.PossessionZone);
        HWJ_SpawnTableDataSO spawnTable = CreateOrUpdateProfessorDemoSpawnTable(enemyRuntimePrefab);
        ConfigureDemoSpawner(spawnTable);

        CreateDeadPossessionCorpse(root, sprites);
        CreateLivePossessionResistTarget(root, sprites);
        CreateSpiritOrbSwitchShowcase(root, sprites);
        CreateBodyExclusiveObstacleShowcase(root, sprites);
        CreateRewardPickupShowcase(root);
        CreateStageChoiceRewardShowcase(root, sprites, runtimeSystems);
        GameObject midBossObject = CreateMidBossShowcase(root);
        HWJ_BossFlowSystem bossFlowSystem = CreateBossFlowShowcase(root, runtimeSystems, midBossObject);
        CreateCoreLoopShowcase(root, runtimeSystems, bossFlowSystem);
    }

    private static void CreateDeadPossessionCorpse(Transform root, HWJ_DemoMapSprites sprites)
    {
        HWJ_RootObjectDataSO corpseData = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(
            HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Sword_RootObjectData.asset");

        GameObject corpse = CreateVisualRect(
            root,
            "HWJ_DemoShowcase_DeadPossessableSwordCorpse",
            sprites.PossessionZone,
            new Vector3(-15.2f, -2.05f, 0f),
            new Vector2(0.85f, 1.15f),
            31,
            new Color(0.5f, 0.85f, 0.68f, 0.95f));

        EnsureRuntimeEnemyComponents(corpse, sprites.PossessionZone, true, true);
        ConfigureDirectShowcaseEnemyBehavior(corpse);
        HWJ_RootObjectDataResolver resolver = corpse.GetComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(corpseData);
        ConfigureRuntimeStatus(corpse.GetComponent<HWJ_RuntimeStatusSystem>(), HWJ_RuntimeState.Dead, 0f);

        CreateVisualRect(
            corpse.transform,
            "HWJ_DemoShowcase_DeadCorpseHeartMarker",
            sprites.SpiritGate,
            new Vector3(0f, 0.52f, 0f),
            new Vector2(0.22f, 0.22f),
            34,
            new Color(0.95f, 0.35f, 1f, 0.95f));
    }

    private static void CreateLivePossessionResistTarget(Transform root, HWJ_DemoMapSprites sprites)
    {
        HWJ_RootObjectDataSO liveRootData = CreateOrUpdateLivePossessionRootData();

        GameObject liveTarget = CreateVisualRect(
            root,
            "HWJ_DemoShowcase_LivePossessionResistTarget",
            sprites.Hazard,
            new Vector3(-11.8f, -2.0f, 0f),
            new Vector2(0.9f, 1.25f),
            31,
            new Color(1f, 0.42f, 0.35f, 0.95f));

        EnsureRuntimeEnemyComponents(liveTarget, sprites.Hazard, true, false);
        ConfigureDirectShowcaseEnemyBehavior(liveTarget);
        HWJ_RootObjectDataResolver resolver = liveTarget.GetComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(liveRootData);
        ConfigureRuntimeStatus(liveTarget.GetComponent<HWJ_RuntimeStatusSystem>(), HWJ_RuntimeState.Idle, 80f);

        CreateVisualRect(
            liveTarget.transform,
            "HWJ_DemoShowcase_LivePossessionResistAura",
            sprites.SpiritSeal,
            new Vector3(0f, 0.05f, 0f),
            new Vector2(1.25f, 1.55f),
            30,
            new Color(0.7f, 0.24f, 1f, 0.32f));
    }

    private static void CreateSpiritOrbSwitchShowcase(Transform root, HWJ_DemoMapSprites sprites)
    {
        GameObject lockedGate = CreateSolidRect(
            root,
            "HWJ_DemoShowcase_SpiritOrbLockedGate",
            sprites.SpiritGate,
            new Vector3(-6.9f, -1.0f, 0f),
            new Vector2(0.55f, 3.2f));

        SpriteRenderer gateRenderer = lockedGate.GetComponent<SpriteRenderer>();
        gateRenderer.sortingOrder = 28;
        gateRenderer.color = new Color(0.65f, 0.26f, 1f, 0.75f);

        GameObject orb = CreateVisualRect(
            root,
            "HWJ_DemoShowcase_SpiritOrbSwitch",
            sprites.SpiritOrb,
            new Vector3(-9.4f, -1.2f, 0f),
            new Vector2(0.75f, 0.75f),
            33,
            new Color(0.35f, 0.9f, 1f, 0.95f));

        BoxCollider2D trigger = orb.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.6f, 1.6f);

        HWJ_SpiritOrbSwitchSystem switchSystem = orb.AddComponent<HWJ_SpiritOrbSwitchSystem>();
        SerializedObject serializedSwitch = new SerializedObject(switchSystem);
        serializedSwitch.FindProperty("switchId").stringValue = "demo.spirit_orb_switch.open_gate";
        serializedSwitch.FindProperty("oneShot").boolValue = true;
        serializedSwitch.FindProperty("requireSpiritState").boolValue = true;
        serializedSwitch.FindProperty("requireInteractInput").boolValue = true;
        serializedSwitch.FindProperty("spiritMentalCost").floatValue = 5f;
        SetObjectArray(serializedSwitch.FindProperty("deactivateTargets"), lockedGate);
        serializedSwitch.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateBodyExclusiveObstacleShowcase(Transform root, HWJ_DemoMapSprites sprites)
    {
        CreateBodyObstacleGate(
            root,
            sprites,
            "HWJ_DemoShowcase_SoulOnlyGate",
            "demo.body_obstacle.soul_only",
            new Vector3(-3.4f, -1.0f, 0f),
            new Vector2(0.55f, 3.2f),
            HWJ_BodyObstacleRequirementMode.SpiritOnly,
            new HWJ_WeaponType[0],
            new Color(0.35f, 0.85f, 1f, 0.72f));

        CreateBodyObstacleGate(
            root,
            sprites,
            "HWJ_DemoShowcase_SwordBodyGate",
            "demo.body_obstacle.sword_body",
            new Vector3(23.2f, -1.0f, 0f),
            new Vector2(0.65f, 3.3f),
            HWJ_BodyObstacleRequirementMode.WeaponType,
            new[] { HWJ_WeaponType.Sword },
            new Color(0.95f, 0.68f, 0.2f, 0.8f));
    }

    private static void CreateBodyObstacleGate(
        Transform root,
        HWJ_DemoMapSprites sprites,
        string gateName,
        string obstacleId,
        Vector3 position,
        Vector2 size,
        HWJ_BodyObstacleRequirementMode requirementMode,
        HWJ_WeaponType[] allowedWeapons,
        Color gateColor)
    {
        GameObject gate = CreateSolidRect(root, gateName + "_Barrier", sprites.Hazard, position, size);
        SpriteRenderer renderer = gate.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 29;
        renderer.color = gateColor;

        BoxCollider2D gateCollider = gate.GetComponent<BoxCollider2D>();

        GameObject controllerObject = new GameObject(gateName + "_System");
        controllerObject.transform.SetParent(root);
        controllerObject.transform.position = position;

        HWJ_BodyExclusiveObstacleSystem obstacle = controllerObject.AddComponent<HWJ_BodyExclusiveObstacleSystem>();
        SerializedObject serializedObstacle = new SerializedObject(obstacle);
        serializedObstacle.FindProperty("obstacleId").stringValue = obstacleId;
        serializedObstacle.FindProperty("requirementMode").enumValueIndex = (int)requirementMode;
        serializedObstacle.FindProperty("openWhenRequirementMet").boolValue = true;
        serializedObstacle.FindProperty("updateContinuously").boolValue = true;
        serializedObstacle.FindProperty("requirePlayerTag").boolValue = true;
        SetEnumArray(serializedObstacle.FindProperty("allowedWeaponTypes"), allowedWeapons);
        SetObjectArray(serializedObstacle.FindProperty("obstacleColliders"), gateCollider);
        SetObjectArray(serializedObstacle.FindProperty("hideWhenOpen"), gate);
        serializedObstacle.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateRewardPickupShowcase(Transform root)
    {
        GameObject experienceOrb = InstantiatePrefabIfAvailable(
            HwjAssetRoot + "/Prefabs/Generated/ExperienceOrbs/HWJ_ExperienceOrb_Default_Prefab.prefab",
            root,
            "HWJ_DemoShowcase_ExperienceOrb_60",
            new Vector3(-12.9f, -1.35f, 0f));

        if (experienceOrb != null)
        {
            HWJ_ExperienceOrbPickupSystem pickup = experienceOrb.GetComponent<HWJ_ExperienceOrbPickupSystem>();

            if (pickup != null)
            {
                SerializedObject serializedPickup = new SerializedObject(pickup);
                serializedPickup.FindProperty("collectDelaySeconds").floatValue = 1f;
                serializedPickup.FindProperty("experienceAmount").intValue = 60;
                serializedPickup.FindProperty("lifeTimeSeconds").floatValue = 0f;
                serializedPickup.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        InstantiatePrefabIfAvailable(
            HwjAssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_AttackPowerSmall_Prefab.prefab",
            root,
            "HWJ_DemoShowcase_StatOrb_AttackPower",
            new Vector3(-12f, -1.15f, 0f));

        InstantiatePrefabIfAvailable(
            HwjAssetRoot + "/Prefabs/Generated/StatOrbs/HWJ_StatOrb_DefenseSmall_Prefab.prefab",
            root,
            "HWJ_DemoShowcase_StatOrb_Defense",
            new Vector3(-11.1f, -1.15f, 0f));
    }

    private static void CreateStageChoiceRewardShowcase(
        Transform root,
        HWJ_DemoMapSprites sprites,
        HWJ_DemoRuntimeSystems runtimeSystems)
    {
        HWJ_StageChoiceRewardDataSO rewardData = CreateOrUpdateStageChoiceRewardData();

        GameObject rewardObject = CreateVisualRect(
            root,
            "HWJ_DemoShowcase_StageChoiceRewardAltar",
            sprites.SpiritOrb,
            new Vector3(27.0f, -1.55f, 0f),
            new Vector2(1.0f, 1.0f),
            32,
            new Color(0.25f, 0.9f, 0.95f, 0.94f));

        CircleCollider2D trigger = rewardObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

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

    private static GameObject CreateMidBossShowcase(Transform root)
    {
        GameObject bossObject = InstantiatePrefabIfAvailable(
            HwjAssetRoot + "/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab",
            root,
            "HWJ_DemoShowcase_MidBoss1_Runtime",
            new Vector3(40f, -1.85f, 0f));

        if (bossObject == null)
        {
            return null;
        }

        bossObject.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
        return bossObject;
    }

    private static HWJ_BossFlowSystem CreateBossFlowShowcase(
        Transform root,
        HWJ_DemoRuntimeSystems runtimeSystems,
        GameObject bossObject)
    {
        GameObject flowObject = new GameObject("HWJ_DemoShowcase_BossFlowSystem");
        flowObject.transform.SetParent(root);
        flowObject.transform.position = new Vector3(37.5f, 0.5f, 0f);

        HWJ_BossFlowSystem bossFlow = flowObject.AddComponent<HWJ_BossFlowSystem>();
        HWJ_BossBrainSystem bossBrain = bossObject != null
            ? bossObject.GetComponentInChildren<HWJ_BossBrainSystem>(true)
            : null;
        HWJ_RootObjectDataResolver bossResolver = bossObject != null
            ? bossObject.GetComponentInChildren<HWJ_RootObjectDataResolver>(true)
            : null;
        HWJ_RootObjectDataResolver playerResolver = FindFirstSceneComponent<HWJ_RootObjectDataResolver>(
            resolver => resolver != null && resolver.ObjectType == HWJ_ObjectType.Player);

        SerializedObject serializedFlow = new SerializedObject(bossFlow);
        serializedFlow.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedFlow.FindProperty("bossBrainSystem").objectReferenceValue = bossBrain;
        serializedFlow.FindProperty("bossResolver").objectReferenceValue = bossResolver;
        serializedFlow.FindProperty("bossEntryTriggerActive").boolValue = true;
        serializedFlow.FindProperty("autoCompleteBossFlowOnCombatDeath").boolValue = true;
        serializedFlow.FindProperty("requireBossBattleStateForCombatDeath").boolValue = false;
        serializedFlow.FindProperty("useStageDefinitionDefaultsOnCombatDeath").boolValue = true;
        serializedFlow.FindProperty("playerResolver").objectReferenceValue = playerResolver;

        if (playerResolver != null)
        {
            serializedFlow.FindProperty("playerSoulSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_SoulSystem>();
            serializedFlow.FindProperty("playerPossessionSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_PossessionSystem>();
            serializedFlow.FindProperty("playerCollapseSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_CollapseSystem>();
            serializedFlow.FindProperty("playerStatusSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_RuntimeStatusSystem>();
            serializedFlow.FindProperty("playerBodyDecaySystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_BodyDecaySystem>();
            serializedFlow.FindProperty("playerSkillUnlockSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_SkillUnlockSystem>();
        }

        serializedFlow.ApplyModifiedPropertiesWithoutUndo();
        return bossFlow;
    }

    private static void CreateCoreLoopShowcase(
        Transform root,
        HWJ_DemoRuntimeSystems runtimeSystems,
        HWJ_BossFlowSystem bossFlowSystem)
    {
        GameObject coordinatorObject = new GameObject("HWJ_DemoShowcase_CoreLoopCoordinator");
        coordinatorObject.transform.SetParent(root);
        coordinatorObject.transform.position = new Vector3(36.5f, 1.2f, 0f);

        HWJ_CoreLoopCoordinator coordinator = coordinatorObject.AddComponent<HWJ_CoreLoopCoordinator>();
        HWJ_RootObjectDataResolver playerResolver = FindFirstSceneComponent<HWJ_RootObjectDataResolver>(
            resolver => resolver != null && resolver.ObjectType == HWJ_ObjectType.Player);

        SerializedObject serializedCoordinator = new SerializedObject(coordinator);
        serializedCoordinator.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedCoordinator.FindProperty("bossFlowSystem").objectReferenceValue = bossFlowSystem;

        if (playerResolver != null)
        {
            serializedCoordinator.FindProperty("playerSoulSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_SoulSystem>();
            serializedCoordinator.FindProperty("bodyDiscoverySystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_BodyDiscoverySystem>();
            serializedCoordinator.FindProperty("possessionSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_PossessionSystem>();
            serializedCoordinator.FindProperty("collapseSystem").objectReferenceValue =
                playerResolver.GetComponent<HWJ_CollapseSystem>();
        }

        serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateOrUpdateShowcaseEnemyRuntimePrefab(Sprite sprite)
    {
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(ShowcaseEnemyRuntimePrefabPath) != null;
        GameObject prefabRoot = prefabExists
            ? PrefabUtility.LoadPrefabContents(ShowcaseEnemyRuntimePrefabPath)
            : new GameObject("HWJ_ProfessorDemo_EnemyRuntime_Prefab");

        prefabRoot.name = "HWJ_ProfessorDemo_EnemyRuntime_Prefab";
        prefabRoot.transform.localPosition = Vector3.zero;
        prefabRoot.transform.localRotation = Quaternion.identity;
        prefabRoot.transform.localScale = Vector3.one;
        EnsureRuntimeEnemyComponents(prefabRoot, sprite, true, false);
        ConfigureEnemyCombatExecution(prefabRoot.GetComponent<HWJ_CombatExecutionSystem>());

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, ShowcaseEnemyRuntimePrefabPath);

        if (prefabExists)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(prefabRoot);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(ShowcaseEnemyRuntimePrefabPath);
    }

    private static HWJ_SpawnTableDataSO CreateOrUpdateProfessorDemoSpawnTable(GameObject enemyRuntimePrefab)
    {
        HWJ_SpawnTableDataSO spawnTable = LoadOrCreateAsset<HWJ_SpawnTableDataSO>(ShowcaseSpawnTablePath);
        SerializedObject serializedTable = new SerializedObject(spawnTable);
        serializedTable.FindProperty("tableId").stringValue = "spawn.professor_demo.hwj";

        SerializedProperty entries = serializedTable.FindProperty("entries");
        entries.arraySize = 5;
        SetSpawnEntry(entries.GetArrayElementAtIndex(0), "demo_enemy_sword_wave", "enemy_Sword", HWJ_SpawnPointType.Enemy, HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Sword_RootObjectData.asset", enemyRuntimePrefab, 2, 0f, 6f);
        SetSpawnEntry(entries.GetArrayElementAtIndex(1), "demo_enemy_shield_gate", "enemy_Shield", HWJ_SpawnPointType.Enemy, HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Shield_RootObjectData.asset", enemyRuntimePrefab, 1, 0.5f, 5f);
        SetSpawnEntry(entries.GetArrayElementAtIndex(2), "demo_enemy_lance_route", "enemy_Lance", HWJ_SpawnPointType.Enemy, HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Lance_RootObjectData.asset", enemyRuntimePrefab, 1, 1f, 5f);
        SetSpawnEntry(entries.GetArrayElementAtIndex(3), "demo_enemy_bow_range", "enemy_Bow", HWJ_SpawnPointType.Enemy, HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Bow_RootObjectData.asset", enemyRuntimePrefab, 1, 1.5f, 5f);
        SetSpawnEntry(entries.GetArrayElementAtIndex(4), "demo_enemy_axe_power", "enemy_Axe", HWJ_SpawnPointType.Enemy, HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Axe_RootObjectData.asset", enemyRuntimePrefab, 1, 2f, 5f);

        serializedTable.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawnTable);
        return spawnTable;
    }

    private static void ConfigureDemoSpawner(HWJ_SpawnTableDataSO spawnTable)
    {
        EnsureSpawnPoint("HWJ_SpawnPoint_Sword", "enemy_Sword", HWJ_SpawnPointType.Enemy, new Vector3(5f, -2.1f, 0f));
        EnsureSpawnPoint("HWJ_SpawnPoint_Shield", "enemy_Shield", HWJ_SpawnPointType.Enemy, new Vector3(8.5f, -2.1f, 0f));
        EnsureSpawnPoint("HWJ_SpawnPoint_Lance", "enemy_Lance", HWJ_SpawnPointType.Enemy, new Vector3(12f, -2.1f, 0f));
        EnsureSpawnPoint("HWJ_SpawnPoint_Bow", "enemy_Bow", HWJ_SpawnPointType.Enemy, new Vector3(16f, -2.1f, 0f));
        EnsureSpawnPoint("HWJ_SpawnPoint_Axe", "enemy_Axe", HWJ_SpawnPointType.Enemy, new Vector3(20f, -2.1f, 0f));

        GameObject spawnerObject = GameObject.Find("HWJ_MonsterSpawner");

        if (spawnerObject == null)
        {
            spawnerObject = new GameObject("HWJ_MonsterSpawner");
        }

        HWJ_SpawnerSystem spawner = EnsureComponent<HWJ_SpawnerSystem>(spawnerObject);
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("spawnTable").objectReferenceValue = spawnTable;
        serializedSpawner.FindProperty("autoCollectSpawnPoints").boolValue = true;
        serializedSpawner.FindProperty("spawnOnStart").boolValue = true;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HWJ_SpawnPoint EnsureSpawnPoint(
        string objectName,
        string pointId,
        HWJ_SpawnPointType pointType,
        Vector3 position)
    {
        GameObject pointObject = GameObject.Find(objectName);

        if (pointObject == null)
        {
            pointObject = new GameObject(objectName);
        }

        pointObject.transform.position = position;
        HWJ_SpawnPoint point = EnsureComponent<HWJ_SpawnPoint>(pointObject);
        SerializedObject serializedPoint = new SerializedObject(point);
        serializedPoint.FindProperty("pointId").stringValue = pointId;
        serializedPoint.FindProperty("spawnPointType").enumValueIndex = (int)pointType;
        serializedPoint.ApplyModifiedPropertiesWithoutUndo();
        return point;
    }

    private static HWJ_RootObjectDataSO CreateOrUpdateLivePossessionRootData()
    {
        HWJ_EnemyTypeDataSO sourceType = AssetDatabase.LoadAssetAtPath<HWJ_EnemyTypeDataSO>(
            HwjAssetRoot + "/ScriptableObjects/TypeData/Enemy/Possessable/HWJ_EnemyCorpse_Sword_TypeData.asset");
        HWJ_RootObjectDataSO sourceRoot = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(
            HwjAssetRoot + "/ScriptableObjects/RootObjects/Enemies/HWJ_EnemyCorpse_Sword_RootObjectData.asset");

        HWJ_EnemyTypeDataSO liveType = LoadOrCreateAssetCopy(LivePossessionEnemyTypePath, sourceType);
        SerializedObject serializedType = new SerializedObject(liveType);
        serializedType.FindProperty("objectType").enumValueIndex = (int)HWJ_ObjectType.Enemy;
        serializedType.FindProperty("typeId").stringValue = "enemy.professor_demo.live_resist_sword";
        serializedType.FindProperty("displayName").stringValue = "시연용 살아있는 빙의 저항 검사 대상";
        serializedType.FindProperty("defaultWeaponType").enumValueIndex = (int)HWJ_WeaponType.Sword;
        serializedType.FindProperty("description").stringValue = "살아있는 대상 빙의 저항, 실패 정신력 비용, 성공 시 조작권 이전을 시연하기 위한 데이터입니다.";

        SerializedProperty role = serializedType.FindProperty("role");
        role.FindPropertyRelative("weaponType").enumValueIndex = (int)HWJ_WeaponType.Sword;
        role.FindPropertyRelative("isPossessableBody").boolValue = true;
        role.FindPropertyRelative("leavesCorpseOnDeath").boolValue = true;

        SerializedProperty tracking = serializedType.FindProperty("tracking");
        tracking.FindPropertyRelative("trackingRange").floatValue = 7f;

        SerializedProperty ai = serializedType.FindProperty("ai");
        ai.FindPropertyRelative("defaultSkillCooldownSeconds").floatValue = 3f;
        ai.FindPropertyRelative("basicAttackIntervalSeconds").floatValue = 1.5f;
        ai.FindPropertyRelative("skillCycleCount").intValue = 3;
        ai.FindPropertyRelative("skillCycleResetDelaySeconds").floatValue = 5f;

        SerializedProperty navigation = serializedType.FindProperty("navigation");
        navigation.FindPropertyRelative("stoppingDistance").floatValue = 1.25f;
        navigation.FindPropertyRelative("avoidLedges").boolValue = true;

        SerializedProperty state = serializedType.FindProperty("state");
        state.FindPropertyRelative("attackRange").floatValue = 1.35f;

        SerializedProperty possessionBody = serializedType.FindProperty("possessionBody");
        possessionBody.FindPropertyRelative("canBePossessed").boolValue = true;
        possessionBody.FindPropertyRelative("possessionRange").floatValue = 2.2f;
        possessionBody.FindPropertyRelative("spiritMentalCostOnPossession").floatValue = 8f;
        possessionBody.FindPropertyRelative("livePossessionSuccessChance").floatValue = 0.45f;
        possessionBody.FindPropertyRelative("livePossessionFailureSpiritMentalCost").floatValue = 14f;
        possessionBody.FindPropertyRelative("livePossessionFailureControlLockSeconds").floatValue = 0.6f;
        possessionBody.FindPropertyRelative("livePossessionFailureKnockbackPower").floatValue = 4.2f;
        possessionBody.FindPropertyRelative("livePossessionFailureKnockbackSeconds").floatValue = 0.25f;
        possessionBody.FindPropertyRelative("livePossessionFailureMessage").stringValue = "살아있는 적의 정신력 저항에 밀려 빙의가 실패했다.";
        possessionBody.FindPropertyRelative("requiresDefeatedState").boolValue = false;
        possessionBody.FindPropertyRelative("overrideBodyDecayOnPossession").boolValue = true;

        SerializedProperty bodyMental = possessionBody.FindPropertyRelative("possessedBodyDecayOverride");
        bodyMental.FindPropertyRelative("resourceDisplayName").stringValue = "빙의체 정신력";
        bodyMental.FindPropertyRelative("initialDecayValue").floatValue = 0f;
        bodyMental.FindPropertyRelative("maxDecayValue").floatValue = 150f;
        bodyMental.FindPropertyRelative("decayTickSeconds").floatValue = 0.5f;
        bodyMental.FindPropertyRelative("decayAmountPerTick").floatValue = 0.7f;
        bodyMental.FindPropertyRelative("hitDecayPenalty").floatValue = 0f;
        bodyMental.FindPropertyRelative("depletedMessage").stringValue = "이 몸은 더 이상 못 쓰겠다.";
        serializedType.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(liveType);

        HWJ_RootObjectDataSO liveRoot = LoadOrCreateAssetCopy(LivePossessionRootObjectPath, sourceRoot);
        SerializedObject serializedRoot = new SerializedObject(liveRoot);
        SerializedProperty identity = serializedRoot.FindProperty("identity");
        identity.FindPropertyRelative("objectId").stringValue = "enemy.professor_demo.live_resist_sword";
        identity.FindPropertyRelative("displayName").stringValue = "시연용 살아있는 빙의 저항 대상";
        identity.FindPropertyRelative("objectType").enumValueIndex = (int)HWJ_ObjectType.Enemy;
        identity.FindPropertyRelative("faction").enumValueIndex = (int)HWJ_Faction.Monster;
        identity.FindPropertyRelative("weaponType").enumValueIndex = (int)HWJ_WeaponType.Sword;

        SerializedProperty status = serializedRoot.FindProperty("status");
        status.FindPropertyRelative("maxHp").floatValue = 80f;
        status.FindPropertyRelative("moveSpeed").floatValue = 2.5f;
        status.FindPropertyRelative("attackPower").floatValue = 8f;
        status.FindPropertyRelative("defense").floatValue = 3f;
        status.FindPropertyRelative("bodyWeight").floatValue = 1.2f;

        SerializedProperty damage = serializedRoot.FindProperty("damage");
        damage.FindPropertyRelative("baseDamage").floatValue = 5f;
        damage.FindPropertyRelative("knockbackPower").floatValue = 4f;

        SerializedProperty reward = serializedRoot.FindProperty("reward");
        reward.FindPropertyRelative("experienceReward").intValue = 20;
        reward.FindPropertyRelative("dropsStatOrb").boolValue = true;
        reward.FindPropertyRelative("statOrbId").stringValue = "stat_attack_power_small";

        serializedRoot.FindProperty("selectedTypeData").objectReferenceValue = liveType;
        serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(liveRoot);
        return liveRoot;
    }

    private static HWJ_StageChoiceRewardDataSO CreateOrUpdateStageChoiceRewardData()
    {
        HWJ_StageChoiceRewardDataSO rewardData = LoadOrCreateAsset<HWJ_StageChoiceRewardDataSO>(ShowcaseStageChoiceRewardPath);
        SerializedObject serializedReward = new SerializedObject(rewardData);
        serializedReward.FindProperty("rewardSetId").stringValue = "reward.professor_demo.stage_choice";
        serializedReward.FindProperty("stageId").stringValue = "stage.demo.hwj";
        serializedReward.FindProperty("displayTitle").stringValue = "스테이지 선택 보상";

        SerializedProperty options = serializedReward.FindProperty("options");
        options.arraySize = 3;
        SetRewardOption(options.GetArrayElementAtIndex(0), "reward_attack_power", "공격 강화", "공격력 스탯 구슬과 경험치를 지급합니다.", 35, 1, "stat_attack_power_small", HwjAssetRoot + "/ScriptableObjects/StatOrbs/HWJ_StatOrb_AttackPowerSmall.asset");
        SetRewardOption(options.GetArrayElementAtIndex(1), "reward_defense", "방어 강화", "방어력 스탯 구슬과 경험치를 지급합니다.", 35, 1, "stat_defense_small", HwjAssetRoot + "/ScriptableObjects/StatOrbs/HWJ_StatOrb_DefenseSmall.asset");
        SetRewardOption(options.GetArrayElementAtIndex(2), "reward_speed", "이동 강화", "이동 속도 스탯 구슬과 경험치를 지급합니다.", 35, 1, "stat_move_speed_small", HwjAssetRoot + "/ScriptableObjects/StatOrbs/HWJ_StatOrb_MoveSpeedSmall.asset");

        serializedReward.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rewardData);
        return rewardData;
    }

    private static void EnsureRuntimeEnemyComponents(
        GameObject enemyObject,
        Sprite sprite,
        bool includeBehaviorSystems,
        bool colliderIsTrigger)
    {
        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(enemyObject);
        renderer.sprite = sprite;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 31);

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(enemyObject);
        collider.isTrigger = colliderIsTrigger;
        collider.size = new Vector2(0.82f, 1.18f);
        collider.offset = new Vector2(0f, 0.06f);

        Rigidbody2D body = EnsureComponent<Rigidbody2D>(enemyObject);
        body.bodyType = colliderIsTrigger ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        body.gravityScale = colliderIsTrigger ? 0f : 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        EnsureComponent<HWJ_RuntimeObjectContext>(enemyObject);
        EnsureComponent<HWJ_RootObjectDataResolver>(enemyObject);
        EnsureComponent<HWJ_RuntimeStatusSystem>(enemyObject);
        EnsureComponent<HWJ_CombatSystem>(enemyObject);
        EnsureComponent<HWJ_CombatExecutionSystem>(enemyObject);
        EnsureComponent<HWJ_SkillActionSystem>(enemyObject);
        EnsureComponent<HWJ_KnockbackSystem>(enemyObject);
        EnsureComponent<HWJ_CharacterMotionSystem>(enemyObject);
        EnsureComponent<HWJ_PossessionBodyState>(enemyObject);

        if (!includeBehaviorSystems)
        {
            return;
        }

        EnsureComponent<HWJ_EnemyNavigationSystem>(enemyObject);
        EnsureComponent<HWJ_EnemyAttackSystem>(enemyObject);
        EnsureComponent<HWJ_MonsterAISystem>(enemyObject);
    }

    private static void ConfigureEnemyCombatExecution(HWJ_CombatExecutionSystem combatExecution)
    {
        if (combatExecution == null)
        {
            return;
        }

        SerializedObject serializedExecution = new SerializedObject(combatExecution);
        serializedExecution.FindProperty("useObjectTypeDefaultTargetFilter").boolValue = false;
        serializedExecution.FindProperty("canDamagePlayer").boolValue = true;
        serializedExecution.FindProperty("canDamageEnemy").boolValue = false;
        serializedExecution.FindProperty("canDamageBoss").boolValue = false;
        serializedExecution.FindProperty("canDamageNpc").boolValue = false;
        serializedExecution.FindProperty("fallbackAttackRange").floatValue = 1.4f;
        serializedExecution.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDirectShowcaseEnemyBehavior(GameObject enemyObject)
    {
        HWJ_MonsterAISystem monsterAI = enemyObject.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            SerializedObject serializedAI = new SerializedObject(monsterAI);
            serializedAI.FindProperty("driveBehavior").boolValue = false;
            serializedAI.FindProperty("autoFindPlayerTarget").boolValue = false;
            serializedAI.ApplyModifiedPropertiesWithoutUndo();
        }

        HWJ_EnemyAttackSystem enemyAttack = enemyObject.GetComponent<HWJ_EnemyAttackSystem>();

        if (enemyAttack != null)
        {
            SerializedObject serializedAttack = new SerializedObject(enemyAttack);
            serializedAttack.FindProperty("autoFindPlayerTarget").boolValue = false;
            serializedAttack.FindProperty("autoAttackWhenNoBehaviorDriver").boolValue = false;
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();
        }

        HWJ_EnemyNavigationSystem navigation = enemyObject.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            SerializedObject serializedNavigation = new SerializedObject(navigation);
            serializedNavigation.FindProperty("autoFindPlayerTarget").boolValue = false;
            serializedNavigation.ApplyModifiedPropertiesWithoutUndo();
        }
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

    private static GameObject InstantiatePrefabIfAvailable(
        string prefabPath,
        Transform parent,
        string instanceName,
        Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"HWJ professor demo skipped missing prefab: {prefabPath}");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            return null;
        }

        instance.name = instanceName;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        return instance;
    }

    private static T EnsureComponent<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : owner.AddComponent<T>();
    }

    private static T FindFirstSceneComponent<T>(Func<T, bool> predicate = null) where T : Component
    {
        T[] components = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];

            if (component != null && (predicate == null || predicate(component)))
            {
                return component;
            }
        }

        return null;
    }

    private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static T LoadOrCreateAssetCopy<T>(string assetPath, T sourceAsset) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

        if (asset != null)
        {
            return asset;
        }

        asset = sourceAsset != null
            ? UnityEngine.Object.Instantiate(sourceAsset)
            : ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void SetSpawnEntry(
        SerializedProperty entry,
        string spawnId,
        string pointId,
        HWJ_SpawnPointType pointType,
        string rootObjectPath,
        GameObject prefabOverride,
        int spawnCount,
        float firstDelay,
        float nextSpawnMaxWaitSeconds)
    {
        entry.FindPropertyRelative("spawnId").stringValue = spawnId;
        entry.FindPropertyRelative("spawnPointId").stringValue = pointId;
        entry.FindPropertyRelative("spawnPointType").enumValueIndex = (int)pointType;
        entry.FindPropertyRelative("rootObjectData").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(rootObjectPath);
        entry.FindPropertyRelative("prefabOverride").objectReferenceValue = prefabOverride;
        entry.FindPropertyRelative("spawnCount").intValue = Mathf.Max(1, spawnCount);
        entry.FindPropertyRelative("spawnDelaySeconds").floatValue = Mathf.Max(0f, firstDelay);
        entry.FindPropertyRelative("useSequentialSpawnWhenMultiple").boolValue = true;
        entry.FindPropertyRelative("waitUntilCurrentSpawnedMonstersDefeated").boolValue = true;
        entry.FindPropertyRelative("nextSpawnMaxWaitSeconds").floatValue = Mathf.Max(0f, nextSpawnMaxWaitSeconds);
        entry.FindPropertyRelative("skipSpawnWhenRewardClaimed").boolValue = false;
        entry.FindPropertyRelative("spawnOffset").vector2Value = Vector2.zero;
        entry.FindPropertyRelative("randomizePoint").boolValue = false;
        entry.FindPropertyRelative("spawnOnStart").boolValue = true;
    }

    private static void SetRewardOption(
        SerializedProperty option,
        string optionId,
        string displayName,
        string description,
        int experienceReward,
        int skillPointReward,
        string statOrbId,
        string statOrbAssetPath)
    {
        option.FindPropertyRelative("optionId").stringValue = optionId;
        option.FindPropertyRelative("displayName").stringValue = displayName;
        option.FindPropertyRelative("description").stringValue = description;

        SerializedProperty reward = option.FindPropertyRelative("reward");
        reward.FindPropertyRelative("experienceReward").intValue = Mathf.Max(0, experienceReward);
        reward.FindPropertyRelative("skillPointReward").intValue = Mathf.Max(0, skillPointReward);
        reward.FindPropertyRelative("dropsExperienceOrb").boolValue = false;
        reward.FindPropertyRelative("dropsStatOrb").boolValue = true;
        reward.FindPropertyRelative("statOrbDropChance").floatValue = 1f;
        reward.FindPropertyRelative("statOrbId").stringValue = statOrbId;
        option.FindPropertyRelative("directStatOrb").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<HWJ_StatOrbDataSO>(statOrbAssetPath);
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

    private static HWJ_DemoRuntimeSystems CreateDemoRuntimeSystems(Transform root)
    {
        GameObject transitionObject = new GameObject("HWJ_DemoSceneTransitionSystem");
        transitionObject.transform.SetParent(root);
        HWJ_SceneTransitionSystem transitionSystem = transitionObject.AddComponent<HWJ_SceneTransitionSystem>();

        GameObject progressionObject = new GameObject("HWJ_DemoStageProgressionSystem");
        progressionObject.transform.SetParent(root);
        HWJ_StageProgressionSystem progression = progressionObject.AddComponent<HWJ_StageProgressionSystem>();
        SerializedObject serializedProgression = new SerializedObject(progression);
        serializedProgression.FindProperty("stageId").stringValue = "stage.demo.hwj";
        serializedProgression.FindProperty("currentRegionId").stringValue = "region01";
        serializedProgression.FindProperty("nextStageId").stringValue = "stage.region01.01";
        serializedProgression.FindProperty("nextRegionId").stringValue = "region01";
        serializedProgression.FindProperty("applyDefinitionOnAwake").boolValue = false;
        serializedProgression.FindProperty("initialState").enumValueIndex = (int)HWJ_StageFlowState.Entering;
        serializedProgression.FindProperty("currentState").enumValueIndex = (int)HWJ_StageFlowState.None;
        serializedProgression.ApplyModifiedPropertiesWithoutUndo();

        GameObject enemyCountObject = new GameObject("HWJ_DemoStageEnemyCountSystem");
        enemyCountObject.transform.SetParent(root);
        HWJ_StageEnemyCountSystem enemyCount = enemyCountObject.AddComponent<HWJ_StageEnemyCountSystem>();
        SerializedObject serializedEnemyCount = new SerializedObject(enemyCount);
        serializedEnemyCount.FindProperty("stageProgressionSystem").objectReferenceValue = progression;
        serializedEnemyCount.FindProperty("stageRoot").objectReferenceValue = null;
        serializedEnemyCount.FindProperty("countEnemyObjects").boolValue = true;
        serializedEnemyCount.FindProperty("countBossObjects").boolValue = false;
        serializedEnemyCount.FindProperty("scanOnStart").boolValue = true;
        serializedEnemyCount.FindProperty("periodicRescan").boolValue = true;
        serializedEnemyCount.FindProperty("rescanIntervalSeconds").floatValue = 0.25f;
        serializedEnemyCount.ApplyModifiedPropertiesWithoutUndo();

        return new HWJ_DemoRuntimeSystems(transitionSystem, progression, enemyCount);
    }

    private static void CreateDemoPortal(
        Transform root,
        HWJ_DemoMapSprites sprites,
        HWJ_DemoRuntimeSystems runtimeSystems)
    {
        GameObject portalObject = CreateVisualRect(
            root,
            "HWJ_DemoPortal_ToStage1_01",
            sprites.SpiritGate,
            new Vector3(50f, -1.6f, 0f),
            new Vector2(1.2f, 2.6f),
            30,
            new Color(0.7f, 0.38f, 1f, 0.9f));

        BoxCollider2D collider = portalObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        HWJ_ScenePortalSystem portal = portalObject.AddComponent<HWJ_ScenePortalSystem>();
        SerializedObject serializedPortal = new SerializedObject(portal);
        serializedPortal.FindProperty("targetSceneName").stringValue = "HWJ_Stage1_01_RuinedVillage";
        serializedPortal.FindProperty("targetSpawnPointId").stringValue = "stage1_01_player_start";
        serializedPortal.FindProperty("requireInteractInput").boolValue = true;
        serializedPortal.FindProperty("loadImmediatelyOnEnter").boolValue = false;
        serializedPortal.FindProperty("requirePlayerTag").boolValue = true;
        serializedPortal.FindProperty("requireStageObjectiveComplete").boolValue = true;
        serializedPortal.FindProperty("stageProgressionSystem").objectReferenceValue = runtimeSystems.Progression;
        serializedPortal.FindProperty("stageEnemyCountSystem").objectReferenceValue = runtimeSystems.EnemyCount;
        serializedPortal.FindProperty("sceneTransitionSystem").objectReferenceValue = runtimeSystems.Transition;
        serializedPortal.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateDemoSpikeTrap(
        Transform root,
        string objectName,
        Vector3 position,
        Vector2 size,
        float damage,
        float detectionDistance)
    {
        GameObject trapObject = new GameObject(objectName);
        trapObject.transform.SetParent(root);
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

    private static void CreateDemoSteamTrap(
        Transform root,
        string objectName,
        Vector3 position,
        Vector2 size,
        float damage,
        float activeDuration,
        float inactiveDuration)
    {
        GameObject trapObject = new GameObject(objectName);
        trapObject.transform.SetParent(root);
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
            Debug.LogWarning($"HWJ demo trap skipped because existing trap component was not found: {componentTypeName}");
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
            Debug.LogWarning($"HWJ demo trap setup skipped missing property `{propertyName}` on {component.GetType().Name}.");
            return;
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateSpiritOnlyMechanicCluster(
        Transform root,
        HWJ_DemoMapSprites sprites,
        Vector3 center,
        string suffix)
    {
        CreateVisualRect(root, "HWJ_Map_SpiritSeal_" + suffix + "_A", sprites.SpiritSeal, center + new Vector3(-1.4f, 0f, 0f), new Vector2(0.35f, 3.8f), 7, new Color(0.58f, 0.32f, 1f, 0.72f));
        CreateVisualRect(root, "HWJ_Map_SpiritSeal_" + suffix + "_B", sprites.SpiritSeal, center + new Vector3(1.4f, 0f, 0f), new Vector2(0.35f, 3.8f), 7, new Color(0.58f, 0.32f, 1f, 0.72f));
        CreateVisualRect(root, "HWJ_Map_SpiritOrb_" + suffix + "_Core", sprites.SpiritOrb, center, new Vector2(0.55f, 0.55f), 19, new Color(0.48f, 0.9f, 1f, 0.92f));
        CreateVisualRect(root, "HWJ_Map_SpiritOrb_" + suffix + "_Top", sprites.SpiritOrb, center + new Vector3(0f, 1.35f, 0f), new Vector2(0.35f, 0.35f), 19, new Color(0.48f, 0.9f, 1f, 0.8f));
        CreateVisualRect(root, "HWJ_Map_SpiritOrb_" + suffix + "_Bottom", sprites.SpiritOrb, center + new Vector3(0f, -1.35f, 0f), new Vector2(0.35f, 0.35f), 19, new Color(0.48f, 0.9f, 1f, 0.8f));
    }

    private static GameObject CreateSolidRect(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 position,
        Vector2 size)
    {
        GameObject rect = CreateVisualRect(parent, name, sprite, position, size, 0);
        BoxCollider2D collider = rect.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        return rect;
    }

    private static GameObject CreatePlatform(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 position,
        Vector2 size)
    {
        GameObject platform = CreateSolidRect(parent, name, sprite, position, size);

        if (platform.GetComponent<HWJ_OneWayPlatformSystem>() == null)
        {
            platform.AddComponent<HWJ_OneWayPlatformSystem>();
        }

        return platform;
    }

    private static GameObject CreateVisualRect(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 position,
        Vector2 size,
        int sortingOrder)
    {
        return CreateVisualRect(parent, name, sprite, position, size, sortingOrder, Color.white);
    }

    private static GameObject CreateVisualRect(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 position,
        Vector2 size,
        int sortingOrder,
        Color color)
    {
        GameObject rect = new GameObject(name);
        rect.transform.SetParent(parent);
        rect.transform.position = position;
        rect.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = rect.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return rect;
    }

    private static void RepositionSceneGameplayObjects()
    {
        SetObjectPosition("HWJ_Player", new Vector3(-18f, -1.65f, 0f));
        SetObjectTag("HWJ_Player", "Player");
        SetObjectPosition("HWJ_PlayerVisual", Vector3.zero);
        SetObjectPosition("PlayerStartPoint", new Vector3(-18f, -1.65f, 0f));
        SetObjectPosition("CinemachineCamera", new Vector3(-18f, -1.65f, -10f));

        SetObjectPosition("HWJ_SpawnPoint_Sword", new Vector3(5f, -2.1f, 0f));
        SetObjectPosition("HWJ_SpawnPoint_Shield", new Vector3(8.5f, -2.1f, 0f));
        SetObjectPosition("HWJ_SpawnPoint_Lance", new Vector3(12f, -2.1f, 0f));
        SetObjectPosition("HWJ_SpawnPoint_Bow", new Vector3(16f, -2.1f, 0f));
        SetObjectPosition("HWJ_SpawnPoint_Axe", new Vector3(20f, -2.1f, 0f));
        SetObjectPosition("HWJ_MidBoss1_Runtime_Prefab", new Vector3(40f, -1.85f, 0f));
    }

    private static void SetObjectTag(string objectName, string tagName)
    {
        GameObject target = GameObject.Find(objectName);

        if (target == null)
        {
            return;
        }

        try
        {
            target.tag = tagName;
        }
        catch (UnityException)
        {
            Debug.LogWarning($"HWJ demo map could not assign tag `{tagName}` to `{objectName}`.");
        }
    }

    private static void SetObjectPosition(string objectName, Vector3 position)
    {
        GameObject target = GameObject.Find(objectName);

        if (target != null)
        {
            target.transform.position = position;
        }
    }

    private sealed class HWJ_DemoMapSprites
    {
        public Sprite Background;
        public Sprite Floor;
        public Sprite Platform;
        public Sprite SpiritGate;
        public Sprite SpiritOrb;
        public Sprite SpiritSeal;
        public Sprite PossessionZone;
        public Sprite BossRoom;
        public Sprite Hazard;
    }

    private sealed class HWJ_DemoRuntimeSystems
    {
        public HWJ_DemoRuntimeSystems(
            HWJ_SceneTransitionSystem transition,
            HWJ_StageProgressionSystem progression,
            HWJ_StageEnemyCountSystem enemyCount)
        {
            Transition = transition;
            Progression = progression;
            EnemyCount = enemyCount;
        }

        public HWJ_SceneTransitionSystem Transition { get; }
        public HWJ_StageProgressionSystem Progression { get; }
        public HWJ_StageEnemyCountSystem EnemyCount { get; }
    }

    private enum HWJ_DemoMapSpritePattern
    {
        SubtleNoise,
        Ground,
        Platform,
        Energy,
        SpiritOrb,
        SpiritSeal,
        Boss,
        Hazard
    }
}
