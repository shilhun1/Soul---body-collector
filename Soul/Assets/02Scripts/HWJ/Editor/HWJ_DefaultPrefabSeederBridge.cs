using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates validation-safe HWJ default prefabs and wires them into ScriptableObject data.
/// These are functional defaults for testing until final art prefabs replace them.
/// </summary>
[InitializeOnLoad]
public static class HWJ_DefaultPrefabSeederBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunDefaultPrefabSeeder.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_DefaultPrefabSeederReport.md";
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string PrefabRoot = AssetRoot + "/Prefabs/Generated";
    private const string SpriteRoot = AssetRoot + "/Art/Generated";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_DefaultPrefabSeederBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
    }

    [MenuItem("Tools/HWJ/Data/Create Default Prefabs And Wire Data")]
    public static void RunFromMenu()
    {
        RunSeeder("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Data/Create Default Prefab Seeder Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"HWJ default prefab seeder flag created: {FlagFilePath}");
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
        RunSeeder("flag file");
    }

    private static void RunSeeder(string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ default prefab seeder is already running.");
            return;
        }

        isRunning = true;
        List<string> reportLines = new List<string>();

        try
        {
            EnsureFolders();

            Dictionary<string, GameObject> modelPrefabs = CreateModelPrefabs(reportLines);
            Dictionary<string, GameObject> projectilePrefabs = CreateProjectilePrefabs(reportLines);
            Dictionary<string, GameObject> statOrbPrefabs = CreateStatOrbPrefabs(reportLines);

            int wiredModels = WireRootObjectModels(modelPrefabs, reportLines);
            int wiredProjectiles = WireProjectileSkills(projectilePrefabs, reportLines);
            int wiredStatOrbs = WireStatOrbPrefabs(statOrbPrefabs, reportLines);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WriteReport(source, wiredModels, wiredProjectiles, wiredStatOrbs, reportLines);
        }
        catch (Exception exception)
        {
            WriteExceptionReport(source, exception);
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
        EnsureFolder(AssetRoot, "Prefabs");
        EnsureFolder(AssetRoot + "/Prefabs", "Generated");
        EnsureFolder(PrefabRoot, "Models");
        EnsureFolder(PrefabRoot, "Projectiles");
        EnsureFolder(PrefabRoot, "StatOrbs");
        EnsureFolder(AssetRoot, "Art");
        EnsureFolder(AssetRoot + "/Art", "Generated");
        EnsureFolder(SpriteRoot, "Models");
        EnsureFolder(SpriteRoot, "Projectiles");
        EnsureFolder(SpriteRoot, "StatOrbs");
    }

    private static Dictionary<string, GameObject> CreateModelPrefabs(List<string> reportLines)
    {
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        RegisterModel(prefabs, "player_test", "HWJ_Model_Player_Test", new Color32(235, 245, 255, 255), reportLines);
        RegisterModel(prefabs, "root_default", "HWJ_Model_Root_Default", new Color32(210, 210, 220, 255), reportLines);
        RegisterModel(prefabs, "stage1_boss_archer", "HWJ_Model_Stage1_Boss_Archer", new Color32(130, 85, 190, 255), reportLines);
        RegisterModel(prefabs, "enemy_corpse_sword", "HWJ_Model_Enemy_Corpse_Sword", new Color32(220, 60, 60, 255), reportLines);
        RegisterModel(prefabs, "enemy_corpse_axe", "HWJ_Model_Enemy_Corpse_Axe", new Color32(200, 70, 35, 255), reportLines);
        RegisterModel(prefabs, "enemy_corpse_bow", "HWJ_Model_Enemy_Corpse_Bow", new Color32(80, 165, 85, 255), reportLines);
        RegisterModel(prefabs, "enemy_corpse_lance", "HWJ_Model_Enemy_Corpse_Lance", new Color32(75, 130, 210, 255), reportLines);
        RegisterModel(prefabs, "enemy_corpse_shield", "HWJ_Model_Enemy_Corpse_Shield", new Color32(190, 160, 55, 255), reportLines);
        RegisterModel(prefabs, "enemy_nocorpse_sword", "HWJ_Model_Enemy_NoCorpse_Sword", new Color32(180, 45, 45, 255), reportLines);
        RegisterModel(prefabs, "enemy_nocorpse_axe", "HWJ_Model_Enemy_NoCorpse_Axe", new Color32(170, 65, 35, 255), reportLines);
        RegisterModel(prefabs, "enemy_nocorpse_bow", "HWJ_Model_Enemy_NoCorpse_Bow", new Color32(55, 135, 75, 255), reportLines);
        RegisterModel(prefabs, "enemy_nocorpse_lance", "HWJ_Model_Enemy_NoCorpse_Lance", new Color32(55, 105, 180, 255), reportLines);
        RegisterModel(prefabs, "enemy_nocorpse_shield", "HWJ_Model_Enemy_NoCorpse_Shield", new Color32(155, 135, 45, 255), reportLines);
        return prefabs;
    }

    private static Dictionary<string, GameObject> CreateProjectilePrefabs(List<string> reportLines)
    {
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        prefabs["bow_air_arrow_shot"] = CreateProjectilePrefab("HWJ_Projectile_Bow_AirArrowShot", new Color32(255, 235, 125, 255), reportLines);
        prefabs["bow_rapid_shot"] = CreateProjectilePrefab("HWJ_Projectile_Bow_RapidShot", new Color32(255, 215, 95, 255), reportLines);
        prefabs["bow_low_charge_shot"] = CreateProjectilePrefab("HWJ_Projectile_Bow_LowChargeShot", new Color32(255, 185, 75, 255), reportLines);
        prefabs["sword_wave"] = CreateProjectilePrefab("HWJ_Projectile_Sword_Wave", new Color32(95, 220, 255, 255), reportLines);
        return prefabs;
    }

    private static Dictionary<string, GameObject> CreateStatOrbPrefabs(List<string> reportLines)
    {
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        prefabs["stat_attack_power_small"] = CreateStatOrbPrefab("HWJ_StatOrb_AttackPowerSmall_Prefab", "stat_attack_power_small", new Color32(255, 90, 90, 255), reportLines);
        prefabs["stat_attack_speed_small"] = CreateStatOrbPrefab("HWJ_StatOrb_AttackSpeedSmall_Prefab", "stat_attack_speed_small", new Color32(255, 230, 85, 255), reportLines);
        prefabs["stat_defense_small"] = CreateStatOrbPrefab("HWJ_StatOrb_DefenseSmall_Prefab", "stat_defense_small", new Color32(110, 165, 255, 255), reportLines);
        prefabs["stat_max_hp_small"] = CreateStatOrbPrefab("HWJ_StatOrb_MaxHpSmall_Prefab", "stat_max_hp_small", new Color32(120, 255, 150, 255), reportLines);
        prefabs["stat_move_speed_small"] = CreateStatOrbPrefab("HWJ_StatOrb_MoveSpeedSmall_Prefab", "stat_move_speed_small", new Color32(180, 120, 255, 255), reportLines);
        return prefabs;
    }

    private static void RegisterModel(
        Dictionary<string, GameObject> prefabs,
        string objectId,
        string prefabName,
        Color32 color,
        List<string> reportLines)
    {
        prefabs[objectId] = CreateModelPrefab(prefabName, color, reportLines);
    }

    private static GameObject CreateModelPrefab(string prefabName, Color32 color, List<string> reportLines)
    {
        Sprite sprite = CreateOrLoadSprite($"{SpriteRoot}/Models/{prefabName}.png", color, HWJ_GeneratedSpriteShape.Body);
        GameObject root = new GameObject(prefabName);
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 10;
        root.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1f);
        AddSocket(root.transform, "Heart", new Vector3(0f, 0.15f, 0f));
        AddSocket(root.transform, "Hit", new Vector3(0f, 0.05f, 0f));
        AddSocket(root.transform, "Eyes", new Vector3(0f, 0.35f, 0f));
        return SavePrefab(root, $"{PrefabRoot}/Models/{prefabName}.prefab", reportLines);
    }

    private static GameObject CreateProjectilePrefab(string prefabName, Color32 color, List<string> reportLines)
    {
        Sprite sprite = CreateOrLoadSprite($"{SpriteRoot}/Projectiles/{prefabName}.png", color, HWJ_GeneratedSpriteShape.Projectile);
        GameObject root = new GameObject(prefabName);
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 120;
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.7f, 0.18f);
        return SavePrefab(root, $"{PrefabRoot}/Projectiles/{prefabName}.prefab", reportLines);
    }

    private static GameObject CreateStatOrbPrefab(
        string prefabName,
        string statOrbId,
        Color32 color,
        List<string> reportLines)
    {
        Sprite sprite = CreateOrLoadSprite($"{SpriteRoot}/StatOrbs/{prefabName}.png", color, HWJ_GeneratedSpriteShape.Orb);
        GameObject root = new GameObject(prefabName);
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 80;
        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.35f;
        HWJ_StatOrbPickupSystem pickupSystem = root.AddComponent<HWJ_StatOrbPickupSystem>();

        if (TryLoadStatOrbById(statOrbId, out HWJ_StatOrbDataSO statOrbData))
        {
            SerializedObject pickupObject = new SerializedObject(pickupSystem);
            pickupObject.FindProperty("statOrbData").objectReferenceValue = statOrbData;
            pickupObject.ApplyModifiedPropertiesWithoutUndo();
        }

        return SavePrefab(root, $"{PrefabRoot}/StatOrbs/{prefabName}.prefab", reportLines);
    }

    private static int WireRootObjectModels(Dictionary<string, GameObject> prefabs, List<string> reportLines)
    {
        int wiredCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:HWJ_RootObjectDataSO", new[] { AssetRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_RootObjectDataSO rootObject = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(path);

            if (rootObject == null || rootObject.Identity == null)
            {
                continue;
            }

            if (!prefabs.TryGetValue(rootObject.Identity.objectId, out GameObject prefab) || prefab == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(rootObject);
            SerializedProperty property = serializedObject.FindProperty("model").FindPropertyRelative("modelPrefab");
            property.objectReferenceValue = prefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rootObject);
            wiredCount++;
            reportLines.Add($"- Model wired: `{rootObject.Identity.objectId}` -> `{AssetDatabase.GetAssetPath(prefab)}`");
        }

        return wiredCount;
    }

    private static int WireProjectileSkills(Dictionary<string, GameObject> prefabs, List<string> reportLines)
    {
        int wiredCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:HWJ_SkillActionDataSO", new[] { AssetRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_SkillActionDataSO skillAction = AssetDatabase.LoadAssetAtPath<HWJ_SkillActionDataSO>(path);

            if (skillAction == null || string.IsNullOrWhiteSpace(skillAction.SkillActionId))
            {
                continue;
            }

            if (!prefabs.TryGetValue(skillAction.SkillActionId, out GameObject prefab) || prefab == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(skillAction);
            serializedObject.FindProperty("projectilePrefab").objectReferenceValue = prefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skillAction);
            wiredCount++;
            reportLines.Add($"- Projectile wired: `{skillAction.SkillActionId}` -> `{AssetDatabase.GetAssetPath(prefab)}`");
        }

        return wiredCount;
    }

    private static int WireStatOrbPrefabs(Dictionary<string, GameObject> prefabs, List<string> reportLines)
    {
        int wiredCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:HWJ_StatOrbDataSO", new[] { AssetRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_StatOrbDataSO statOrb = AssetDatabase.LoadAssetAtPath<HWJ_StatOrbDataSO>(path);

            if (statOrb == null || string.IsNullOrWhiteSpace(statOrb.OrbId))
            {
                continue;
            }

            if (!prefabs.TryGetValue(statOrb.OrbId, out GameObject prefab) || prefab == null)
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(statOrb);
            serializedObject.FindProperty("orbPrefab").objectReferenceValue = prefab;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(statOrb);
            wiredCount++;
            reportLines.Add($"- Stat orb wired: `{statOrb.OrbId}` -> `{AssetDatabase.GetAssetPath(prefab)}`");
        }

        return wiredCount;
    }

    private static Sprite CreateOrLoadSprite(string assetPath, Color32 color, HWJ_GeneratedSpriteShape shape)
    {
        string fullPath = ToFullPath(assetPath);

        if (!File.Exists(fullPath))
        {
            Texture2D texture = CreateSpriteTexture(color, shape);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath);
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static Texture2D CreateSpriteTexture(Color32 color, HWJ_GeneratedSpriteShape shape)
    {
        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 edge = new Color32(25, 25, 30, 255);
        Color32 highlight = new Color32(
            (byte)Mathf.Clamp(color.r + 30, 0, 255),
            (byte)Mathf.Clamp(color.g + 30, 0, 255),
            (byte)Mathf.Clamp(color.b + 30, 0, 255),
            255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool filled = IsPixelFilled(x, y, shape);
                bool outline = IsOutlinePixel(x, y, shape);
                texture.SetPixel(x, y, outline ? edge : filled ? color : clear);
            }
        }

        texture.SetPixel(6, 11, highlight);
        texture.SetPixel(7, 12, highlight);
        texture.Apply();
        return texture;
    }

    private static bool IsPixelFilled(int x, int y, HWJ_GeneratedSpriteShape shape)
    {
        switch (shape)
        {
            case HWJ_GeneratedSpriteShape.Projectile:
                return y >= 6 && y <= 9 && x >= 2 && x <= 13;
            case HWJ_GeneratedSpriteShape.Orb:
                return (x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f) <= 25f;
            default:
                return x >= 4 && x <= 11 && y >= 2 && y <= 13;
        }
    }

    private static bool IsOutlinePixel(int x, int y, HWJ_GeneratedSpriteShape shape)
    {
        switch (shape)
        {
            case HWJ_GeneratedSpriteShape.Projectile:
                return (y == 5 || y == 10) && x >= 2 && x <= 13 || (x == 1 || x == 14) && y >= 6 && y <= 9;
            case HWJ_GeneratedSpriteShape.Orb:
                float distance = (x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f);
                return distance > 25f && distance <= 36f;
            default:
                return (x == 3 || x == 12) && y >= 2 && y <= 13 || (y == 1 || y == 14) && x >= 4 && x <= 11;
        }
    }

    private static GameObject SavePrefab(GameObject root, string assetPath, List<string> reportLines)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        UnityEngine.Object.DestroyImmediate(root);
        reportLines.Add($"- Prefab created: `{assetPath}`");
        return prefab;
    }

    private static void AddSocket(Transform parent, string socketName, Vector3 localPosition)
    {
        GameObject socket = new GameObject(socketName);
        socket.transform.SetParent(parent, false);
        socket.transform.localPosition = localPosition;
    }

    private static bool TryLoadStatOrbById(string statOrbId, out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;
        string[] guids = AssetDatabase.FindAssets("t:HWJ_StatOrbDataSO", new[] { AssetRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_StatOrbDataSO candidate = AssetDatabase.LoadAssetAtPath<HWJ_StatOrbDataSO>(path);

            if (candidate != null && candidate.OrbId == statOrbId)
            {
                statOrbData = candidate;
                return true;
            }
        }

        return false;
    }

    private static void WriteReport(
        string source,
        int wiredModels,
        int wiredProjectiles,
        int wiredStatOrbs,
        List<string> reportLines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ 기본 프리팹 생성 보고서");
        builder.AppendLine();
        builder.AppendLine($"- 실행 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 실행 방식: {source}");
        builder.AppendLine($"- 모델 연결: {wiredModels}");
        builder.AppendLine($"- 투사체 연결: {wiredProjectiles}");
        builder.AppendLine($"- 스탯 오브 연결: {wiredStatOrbs}");
        builder.AppendLine();
        builder.AppendLine("## 상세");
        builder.AppendLine();

        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.AppendLine(reportLines[i]);
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
        Debug.Log($"HWJ default prefab seeder finished. Report={ReportFilePath}");
    }

    private static void WriteExceptionReport(string source, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        File.WriteAllText(
            ReportFilePath,
            $"# HWJ 기본 프리팹 생성 실패\n\n- 실행 시간: {DateTime.Now:O}\n- 실행 방식: {source}\n\n```text\n{exception}\n```\n",
            Encoding.UTF8);
    }

    private static void EnsureFolder(string parentPath, string childName)
    {
        string childPath = $"{parentPath}/{childName}";

        if (!AssetDatabase.IsValidFolder(childPath))
        {
            AssetDatabase.CreateFolder(parentPath, childName);
        }
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
    }

    private enum HWJ_GeneratedSpriteShape
    {
        Body,
        Projectile,
        Orb
    }
}
