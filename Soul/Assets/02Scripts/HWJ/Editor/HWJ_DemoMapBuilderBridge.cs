using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 교수님 시연 영상을 위해 HWJ 씬에 보이는 데모 맵을 생성하는 에디터 도구입니다.
/// 사용 에셋은 Assets/02Scripts/HWJ 아래에 생성하거나 이미 저장된 HWJ 에셋만 참조합니다.
/// </summary>
public static class HWJ_DemoMapBuilderBridge
{
    private const string ScenePath = "Assets/01Scenes/HWJ.unity";
    private const string HwjAssetRoot = "Assets/02Scripts/HWJ";
    private const string MapSpriteRoot = HwjAssetRoot + "/Art/Generated/Map";
    private const string DemoMapRootName = "HWJ_VideoDemoMap";

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
        RepositionSceneGameplayObjects();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("HWJ video demo map build completed.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/02Scripts", "HWJ");
        EnsureFolder(HwjAssetRoot, "Art");
        EnsureFolder(HwjAssetRoot + "/Art", "Generated");
        EnsureFolder(HwjAssetRoot + "/Art/Generated", "Map");
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
            Background = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_Background.png",
                new Color32(30, 34, 46, 255),
                HWJ_DemoMapSpritePattern.SubtleNoise),
            Floor = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_Floor.png",
                new Color32(72, 76, 86, 255),
                HWJ_DemoMapSpritePattern.Ground),
            Platform = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_Platform.png",
                new Color32(88, 94, 108, 255),
                HWJ_DemoMapSpritePattern.Platform),
            SpiritGate = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_SpiritGate.png",
                new Color32(138, 84, 230, 180),
                HWJ_DemoMapSpritePattern.Energy),
            PossessionZone = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_PossessionZone.png",
                new Color32(55, 132, 105, 200),
                HWJ_DemoMapSpritePattern.Energy),
            BossRoom = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_BossRoom.png",
                new Color32(120, 42, 48, 220),
                HWJ_DemoMapSpritePattern.Boss),
            Hazard = CreateOrLoadSprite(
                MapSpriteRoot + "/HWJ_MapTile_Hazard.png",
                new Color32(196, 58, 58, 230),
                HWJ_DemoMapSpritePattern.Hazard)
        };
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
        CreateVisualRect(root, "HWJ_Map_Background_SpiritArea", sprites.SpiritGate, new Vector3(-12f, 0.9f, 0f), new Vector2(12f, 7f), -80, new Color(0.42f, 0.22f, 0.85f, 0.22f));
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
        CreateVisualRect(root, "HWJ_Map_PossessionZone_Marker", sprites.PossessionZone, new Vector3(8.5f, -2.55f, 0f), new Vector2(12f, 0.25f), 3, new Color(0.22f, 0.9f, 0.65f, 0.8f));
        CreateVisualRect(root, "HWJ_Map_BossDangerLine", sprites.Hazard, new Vector3(39f, -2.55f, 0f), new Vector2(20f, 0.22f), 3, new Color(1f, 0.25f, 0.22f, 0.82f));
    }

    private static void CreateDemoLandmarks(Transform root, HWJ_DemoMapSprites sprites)
    {
        CreateSign(root, "HWJ_Map_Label_Start", "영혼 탐색", new Vector3(-18f, -1.25f, 0f));
        CreateSign(root, "HWJ_Map_Label_SpiritGate", "영혼 전용 통로", new Vector3(-9.3f, 2.15f, 0f));
        CreateSign(root, "HWJ_Map_Label_Possession", "빙의 전투 구간", new Vector3(8.5f, -1.25f, 0f));
        CreateSign(root, "HWJ_Map_Label_Boss", "중간보스 방", new Vector3(35.5f, -1.25f, 0f));

        CreateVisualRect(root, "HWJ_Map_Possession_HeartMarker", sprites.SpiritGate, new Vector3(5f, -1.7f, 0f), new Vector2(0.35f, 0.35f), 18, new Color(0.95f, 0.45f, 1f, 0.92f));
        CreateVisualRect(root, "HWJ_Map_BossRoom_EntranceMarker", sprites.Hazard, new Vector3(27.5f, -1.7f, 0f), new Vector2(0.35f, 2.1f), 18, new Color(1f, 0.32f, 0.28f, 0.92f));
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

    private static void CreateSign(
        Transform parent,
        string name,
        string text,
        Vector3 position)
    {
        GameObject sign = new GameObject(name);
        sign.transform.SetParent(parent);
        sign.transform.position = position;

        TextMesh textMesh = sign.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.22f;
        textMesh.fontSize = 42;
        textMesh.color = new Color(0.92f, 0.94f, 1f, 0.95f);

        MeshRenderer renderer = sign.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = 40;
        }
    }

    private static void RepositionSceneGameplayObjects()
    {
        SetObjectPosition("HWJ_Player", new Vector3(-18f, -1.65f, 0f));
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
        public Sprite PossessionZone;
        public Sprite BossRoom;
        public Sprite Hazard;
    }

    private enum HWJ_DemoMapSpritePattern
    {
        SubtleNoise,
        Ground,
        Platform,
        Energy,
        Boss,
        Hazard
    }
}
