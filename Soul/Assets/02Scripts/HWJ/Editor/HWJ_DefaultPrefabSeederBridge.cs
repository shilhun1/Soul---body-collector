using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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

/// <summary>
/// Pixel art Chess Knights pack의 빨간 몬스터 프레임을 HWJ 빙의 가능 몬스터 5종에 연결합니다.
/// 생성되는 AnimationClip/AnimatorController는 HWJ 폴더 안에만 저장하고, 원본 에셋은 참조만 합니다.
/// </summary>
[InitializeOnLoad]
public static class HWJ_RedChessPossessableAssetBridge
{
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string SourceRoot = "Assets/Pixel art Chess Knights pack";
    private const string AnimationRoot = AssetRoot + "/Animations/Generated/RedChessPossessable";
    private const string ModelPrefabRoot = AssetRoot + "/Prefabs/Generated/Models";
    private const string RootObjectRoot = AssetRoot + "/ScriptableObjects/RootObjects/Enemies";
    private const string TypeDataRoot = AssetRoot + "/ScriptableObjects/TypeData/Enemy/Possessable";
    private const string MotionProfileRoot = AssetRoot + "/ScriptableObjects/MotionProfiles";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunRedChessPossessableAssetWire.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_RedChessPossessableAssetReport.md";
    private const float FrameRate = 10f;

    private static bool isRunning;
    private static double nextFlagCheckTime;

    private static readonly RedChessBinding[] Bindings =
    {
        // 기본 빨간 체스 몬스터가 왼쪽을 보고 있어서 prefab에서는 flipX=true를 오른쪽 기준으로 저장합니다.
        new RedChessBinding(
            "Sword",
            HWJ_WeaponType.Sword,
            SourceRoot + "/03_Knight/Red_Knight",
            "R_Knight",
            new[] { "Sword_DiagonalSlash", "Sword_UpDiagonalSlash", "Sword_SwordWave" }),
        new RedChessBinding(
            "Axe",
            HWJ_WeaponType.Axe,
            SourceRoot + "/02_rook/Red_rook",
            "R_rook",
            new[] { "Axe_Swing", "Axe_BodyCharge", "Axe_SpinCharge" }),
        new RedChessBinding(
            "Lance",
            HWJ_WeaponType.Lance,
            SourceRoot + "/04_Bishop/Red_bishop",
            "R_bishop",
            new[]
            {
                "Lance_ChargeThrust",
                "Lance_ThrustCombo1",
                "Lance_ThrustCombo2",
                "Lance_ThrustCombo3",
                "Lance_ThrustCombo4",
                "Lance_FinisherThrust"
            }),
        new RedChessBinding(
            "Shield",
            HWJ_WeaponType.Shield,
            SourceRoot + "/01_pawn/RED_pawn",
            "R_pawn",
            new[] { "Shield_Guard", "Shield_Bash" }),
        new RedChessBinding(
            "Bow",
            HWJ_WeaponType.Bow,
            SourceRoot + "/05_Queen/Red_Queen",
            "R_Queen",
            new[] { "Bow_Draw", "Bow_Shoot" })
    };

    static HWJ_RedChessPossessableAssetBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
        EditorApplication.delayCall -= RunPendingFlagAfterReload;
        EditorApplication.delayCall += RunPendingFlagAfterReload;
    }

    [MenuItem("Tools/HWJ/Assets/Wire Red Chess Possessable Monsters")]
    public static void WireFromMenu()
    {
        WireRedChessPossessableMonsters();
    }

    [MenuItem("Tools/HWJ/Assets/Create Red Chess Possessable Wiring Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"HWJ red chess possessable wiring flag created: {FlagFilePath}");
    }

    public static void WireRedChessPossessableMonsters()
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ red chess possessable monster wiring is already running.");
            return;
        }

        isRunning = true;

        try
        {
            List<string> reportLines = new List<string>();
            EnsureFolderPath(AnimationRoot);

            for (int i = 0; i < Bindings.Length; i++)
            {
                WireBinding(Bindings[i], reportLines);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(reportLines);
            Debug.Log($"HWJ red chess possessable monster wiring finished. Report={ReportFilePath}");
        }
        finally
        {
            isRunning = false;
        }
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
        WireRedChessPossessableMonsters();
    }

    private static void RunPendingFlagAfterReload()
    {
        if (File.Exists(FlagFilePath))
        {
            Debug.Log("HWJ red chess possessable wiring flag found after reload.");
            PollFlagFile();
        }
    }

    private static void WireBinding(RedChessBinding binding, List<string> reportLines)
    {
        EnsureFolderPath(binding.GeneratedFolderPath);

        Sprite[] idleFrames = LoadFrames(binding.SourceFolder, binding.SourcePrefix + "_idle");
        Sprite[] attackFrames = LoadFrames(binding.SourceFolder, binding.SourcePrefix + "_atk");
        Sprite[] deadFrames = LoadFrames(binding.SourceFolder, binding.SourcePrefix + "_die");

        if (idleFrames.Length == 0 || attackFrames.Length == 0 || deadFrames.Length == 0)
        {
            throw new InvalidOperationException(
                $"{binding.WeaponName} red chess frames are incomplete. " +
                $"Idle={idleFrames.Length}, Attack={attackFrames.Length}, Dead={deadFrames.Length}");
        }

        AnimationClip idleClip = CreateOrUpdateSpriteClip(binding.IdleClipPath, idleFrames, true);
        AnimationClip attackClip = CreateOrUpdateSpriteClip(binding.AttackClipPath, attackFrames, false);
        AnimationClip deadClip = CreateOrUpdateSpriteClip(binding.DeadClipPath, deadFrames, false);
        RuntimeAnimatorController controller = CreateController(binding, idleClip, attackClip, deadClip);

        WireMotionProfile(binding, controller);
        WireTypeData(binding);
        WireRootObjectData(binding, controller);
        WirePrefab(binding, idleFrames[0], controller);

        reportLines.Add($"- {binding.WeaponName}: `{binding.SourceFolder}` -> `{binding.ControllerPath}`");
    }

    private static Sprite[] LoadFrames(string sourceFolder, string prefix)
    {
        string fullFolder = ToFullPath(sourceFolder);

        if (!Directory.Exists(fullFolder))
        {
            throw new DirectoryNotFoundException($"Missing red chess source folder: {sourceFolder}");
        }

        string[] files = Directory.GetFiles(fullFolder, prefix + "*.png");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        List<Sprite> frames = new List<Sprite>();

        for (int i = 0; i < files.Length; i++)
        {
            string assetPath = ToAssetPath(files[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (sprite == null)
            {
                throw new InvalidOperationException($"{assetPath} is not imported as a Sprite.");
            }

            frames.Add(sprite);
        }

        return frames.ToArray();
    }

    private static AnimationClip CreateOrUpdateSpriteClip(string clipPath, Sprite[] sprites, bool loop)
    {
        EnsureHwjAssetPath(clipPath);
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = FrameRate;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / FrameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static RuntimeAnimatorController CreateController(
        RedChessBinding binding,
        AnimationClip idleClip,
        AnimationClip attackClip,
        AnimationClip deadClip)
    {
        EnsureHwjAssetPath(binding.ControllerPath);

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(binding.ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(binding.ControllerPath);
        }

        UnityEditor.Animations.AnimatorController controller =
            UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(binding.ControllerPath);

        AddBaseParameters(controller);

        for (int i = 0; i < binding.AttackTriggers.Length; i++)
        {
            AddParameter(controller, binding.AttackTriggers[i], AnimatorControllerParameterType.Trigger);
        }

        UnityEditor.Animations.AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState idleState = stateMachine.AddState("Idle");
        UnityEditor.Animations.AnimatorState attackState = stateMachine.AddState("Attack");
        UnityEditor.Animations.AnimatorState deadState = stateMachine.AddState("Dead");

        idleState.motion = idleClip;
        attackState.motion = attackClip;
        deadState.motion = deadClip;
        stateMachine.defaultState = idleState;

        AddAnyStateTriggerTransition(stateMachine, attackState, "AttackTrigger");
        AddAnyStateTriggerTransition(stateMachine, attackState, "JumpTrigger");
        AddAnyStateTriggerTransition(stateMachine, attackState, "DoubleJumpTrigger");
        AddAnyStateTriggerTransition(stateMachine, attackState, "DropJumpTrigger");
        AddAnyStateTriggerTransition(stateMachine, attackState, "DashTrigger");
        AddAnyStateTriggerTransition(stateMachine, attackState, "HitTrigger");

        for (int i = 0; i < binding.AttackTriggers.Length; i++)
        {
            AddAnyStateTriggerTransition(stateMachine, attackState, binding.AttackTriggers[i]);
        }

        AddAnyStateTriggerTransition(stateMachine, deadState, "DeadTrigger");
        AddAnyStateBoolTransition(stateMachine, deadState, "IsDead");

        UnityEditor.Animations.AnimatorStateTransition attackReturn = attackState.AddTransition(idleState);
        attackReturn.hasExitTime = true;
        attackReturn.exitTime = 1f;
        attackReturn.duration = 0f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddBaseParameters(UnityEditor.Animations.AnimatorController controller)
    {
        AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "HorizontalSpeed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsPossessed", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsSoul", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "RuntimeState", AnimatorControllerParameterType.Int);
        AddParameter(controller, "WeaponType", AnimatorControllerParameterType.Int);
        AddParameter(controller, "AttackTrigger", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "JumpTrigger", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "DoubleJumpTrigger", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "DropJumpTrigger", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "DashTrigger", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "HitTrigger", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "DeadTrigger", AnimatorControllerParameterType.Trigger);
    }

    private static void AddParameter(
        UnityEditor.Animations.AnimatorController controller,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = controller.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == parameterType)
            {
                return;
            }
        }

        controller.AddParameter(parameterName, parameterType);
    }

    private static void AddAnyStateTriggerTransition(
        UnityEditor.Animations.AnimatorStateMachine stateMachine,
        UnityEditor.Animations.AnimatorState targetState,
        string triggerName)
    {
        UnityEditor.Animations.AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(targetState);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void AddAnyStateBoolTransition(
        UnityEditor.Animations.AnimatorStateMachine stateMachine,
        UnityEditor.Animations.AnimatorState targetState,
        string boolParameterName)
    {
        UnityEditor.Animations.AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(targetState);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, boolParameterName);
    }

    private static void WirePrefab(RedChessBinding binding, Sprite idleSprite, RuntimeAnimatorController controller)
    {
        EnsureHwjAssetPath(binding.PrefabPath);
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(binding.PrefabPath);

        try
        {
            SpriteRenderer spriteRenderer = prefabContents.GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer = prefabContents.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = idleSprite;
            spriteRenderer.flipX = true;
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 20);

            Animator animator = prefabContents.GetComponentInChildren<Animator>();

            if (animator == null)
            {
                animator = prefabContents.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            HWJ_RootObjectDataSO rootObjectData =
                AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(binding.RootObjectDataPath);
            HWJ_RootObjectDataResolver resolver = prefabContents.GetComponent<HWJ_RootObjectDataResolver>();

            if (resolver != null && rootObjectData != null)
            {
                SerializedObject resolverObject = new SerializedObject(resolver);
                SetObjectReference(resolverObject, "rootObjectData", rootObjectData);
                resolverObject.ApplyModifiedPropertiesWithoutUndo();
            }

            HWJ_CharacterMotionSystem motionSystem = prefabContents.GetComponent<HWJ_CharacterMotionSystem>();

            if (motionSystem != null)
            {
                HWJ_MotionProfileSO motionProfile =
                    AssetDatabase.LoadAssetAtPath<HWJ_MotionProfileSO>(binding.MotionProfilePath);
                SerializedObject motionObject = new SerializedObject(motionSystem);
                SetObjectReference(motionObject, "animator", animator);
                SetObjectReference(motionObject, "spriteRenderer", spriteRenderer);
                SetObjectReference(motionObject, "body", prefabContents.GetComponent<Rigidbody2D>());
                SetObjectReference(motionObject, "dataResolver", resolver);
                SetObjectReference(motionObject, "runtimeStatus", prefabContents.GetComponent<HWJ_RuntimeStatusSystem>());
                SetObjectReference(motionObject, "fallbackMotionProfile", motionProfile);

                SerializedProperty weaponProfiles = motionObject.FindProperty("weaponMotionProfiles");

                if (weaponProfiles != null)
                {
                    weaponProfiles.arraySize = motionProfile != null ? 1 : 0;

                    if (motionProfile != null)
                    {
                        weaponProfiles.GetArrayElementAtIndex(0).objectReferenceValue = motionProfile;
                    }
                }

                motionObject.ApplyModifiedPropertiesWithoutUndo();
                motionSystem.RefreshFacingBaseline();
                EditorUtility.SetDirty(motionSystem);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, binding.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void WireMotionProfile(RedChessBinding binding, RuntimeAnimatorController controller)
    {
        HWJ_MotionProfileSO motionProfile =
            AssetDatabase.LoadAssetAtPath<HWJ_MotionProfileSO>(binding.MotionProfilePath);

        if (motionProfile == null)
        {
            throw new FileNotFoundException($"Missing motion profile: {binding.MotionProfilePath}");
        }

        SerializedObject motionObject = new SerializedObject(motionProfile);
        SetObjectReference(motionObject, "animatorController", controller);
        motionObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(motionProfile);
    }

    private static void WireTypeData(RedChessBinding binding)
    {
        HWJ_EnemyTypeDataSO typeData =
            AssetDatabase.LoadAssetAtPath<HWJ_EnemyTypeDataSO>(binding.TypeDataPath);

        if (typeData == null)
        {
            throw new FileNotFoundException($"Missing type data: {binding.TypeDataPath}");
        }

        SerializedObject typeObject = new SerializedObject(typeData);
        SetEnumValue(typeObject, "defaultWeaponType", binding.WeaponType);
        SerializedProperty role = typeObject.FindProperty("role");

        if (role != null)
        {
            SerializedProperty weaponType = role.FindPropertyRelative("weaponType");

            if (weaponType != null)
            {
                weaponType.enumValueIndex = (int)binding.WeaponType;
            }
        }

        typeObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(typeData);
    }

    private static void WireRootObjectData(RedChessBinding binding, RuntimeAnimatorController controller)
    {
        HWJ_RootObjectDataSO rootObjectData =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(binding.RootObjectDataPath);
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(binding.PrefabPath);

        if (rootObjectData == null)
        {
            throw new FileNotFoundException($"Missing root object data: {binding.RootObjectDataPath}");
        }

        if (modelPrefab == null)
        {
            throw new FileNotFoundException($"Missing model prefab: {binding.PrefabPath}");
        }

        SerializedObject rootObject = new SerializedObject(rootObjectData);
        SerializedProperty identity = rootObject.FindProperty("identity");

        if (identity != null)
        {
            SerializedProperty weaponType = identity.FindPropertyRelative("weaponType");

            if (weaponType != null)
            {
                weaponType.enumValueIndex = (int)binding.WeaponType;
            }
        }

        SerializedProperty model = rootObject.FindProperty("model");

        if (model != null)
        {
            SerializedProperty modelPrefabProperty = model.FindPropertyRelative("modelPrefab");
            SerializedProperty animatorControllerProperty = model.FindPropertyRelative("animatorController");

            if (modelPrefabProperty != null)
            {
                modelPrefabProperty.objectReferenceValue = modelPrefab;
            }

            if (animatorControllerProperty != null)
            {
                animatorControllerProperty.objectReferenceValue = controller;
            }
        }

        rootObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rootObjectData);
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetEnumValue<TEnum>(SerializedObject serializedObject, string propertyName, TEnum value)
        where TEnum : Enum
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.enumValueIndex = Convert.ToInt32(value);
        }
    }

    private static void EnsureFolderPath(string folderPath)
    {
        EnsureHwjAssetPath(folderPath);
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void EnsureHwjAssetPath(string assetPath)
    {
        if (!assetPath.StartsWith(AssetRoot + "/", StringComparison.Ordinal)
            && !string.Equals(assetPath, AssetRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"HWJ bridge can only write under {AssetRoot}: {assetPath}");
        }
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
    }

    private static string ToAssetPath(string fullPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string relativePath = fullPath.Substring(projectRoot.Length + 1);
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static void WriteReport(List<string> reportLines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Red Chess Possessable Asset Wiring");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine("- Source: `Assets/Pixel art Chess Knights pack` red monsters");
        builder.AppendLine("- Red King is intentionally unused here and can be reserved for a boss or special enemy.");
        builder.AppendLine();
        builder.AppendLine("## Mapping");
        builder.AppendLine();

        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.AppendLine(reportLines[i]);
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private sealed class RedChessBinding
    {
        public RedChessBinding(
            string weaponName,
            HWJ_WeaponType weaponType,
            string sourceFolder,
            string sourcePrefix,
            string[] attackTriggers)
        {
            WeaponName = weaponName;
            WeaponType = weaponType;
            SourceFolder = sourceFolder;
            SourcePrefix = sourcePrefix;
            AttackTriggers = attackTriggers;
        }

        public string WeaponName { get; }
        public HWJ_WeaponType WeaponType { get; }
        public string SourceFolder { get; }
        public string SourcePrefix { get; }
        public string[] AttackTriggers { get; }
        public string GeneratedFolderPath => $"{AnimationRoot}/{WeaponName}";
        public string IdleClipPath => $"{GeneratedFolderPath}/HWJ_RedChess_{WeaponName}_Idle.anim";
        public string AttackClipPath => $"{GeneratedFolderPath}/HWJ_RedChess_{WeaponName}_Attack.anim";
        public string DeadClipPath => $"{GeneratedFolderPath}/HWJ_RedChess_{WeaponName}_Dead.anim";
        public string ControllerPath => $"{GeneratedFolderPath}/HWJ_RedChess_{WeaponName}_Animator.controller";
        public string PrefabPath => $"{ModelPrefabRoot}/HWJ_Model_Enemy_Corpse_{WeaponName}.prefab";
        public string RootObjectDataPath => $"{RootObjectRoot}/HWJ_EnemyCorpse_{WeaponName}_RootObjectData.asset";
        public string TypeDataPath => $"{TypeDataRoot}/HWJ_EnemyCorpse_{WeaponName}_TypeData.asset";
        public string MotionProfilePath => $"{MotionProfileRoot}/HWJ_{WeaponName}MotionProfile.asset";
    }
}

/// <summary>
/// Slices the 4x4 slash sheets in Art/effect/Slashes and wires the resulting one-shot effect prefabs to player attacks.
/// The source sheets stay untouched; generated frames, clips, controllers, and prefabs are written under Assets/02Scripts/HWJ.
/// </summary>
[InitializeOnLoad]
public static class HWJ_PlayerAttackEffectAssetBridge
{
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string SourceRoot = AssetRoot + "/Art/effect/Slashes";
    private const string GeneratedFrameRoot = AssetRoot + "/Art/Generated/Effects/PlayerAttackSlashes";
    private const string GeneratedAnimationRoot = AssetRoot + "/Animations/Generated/PlayerAttackEffects";
    private const string GeneratedPrefabRoot = AssetRoot + "/Prefabs/Generated/Effects";
    private const string SkillActionRoot = AssetRoot + "/ScriptableObjects/SkillActions";
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunPlayerAttackEffectWire.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_PlayerAttackEffectAssetReport.md";
    private const int GridSize = 4;
    private const float FrameRate = 16f;
    private const float SpritePixelsPerUnit = 256f;

    private static readonly PlayerAttackEffectBinding[] Bindings =
    {
        new PlayerAttackEffectBinding("Sword", HWJ_WeaponType.Sword, "jettelly_slash_4x4_01.png"),
        new PlayerAttackEffectBinding("Axe", HWJ_WeaponType.Axe, "jettelly_slash_4x4_02.png"),
        new PlayerAttackEffectBinding("Lance", HWJ_WeaponType.Lance, "jettelly_slash_4x4_03.png"),
        new PlayerAttackEffectBinding("Shield", HWJ_WeaponType.Shield, "jettelly_slash_4x4_04.png"),
        new PlayerAttackEffectBinding("Bow", HWJ_WeaponType.Bow, "jettelly_slash_4x4_05.png")
    };

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_PlayerAttackEffectAssetBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
        EditorApplication.delayCall -= RunPendingFlagAfterReload;
        EditorApplication.delayCall += RunPendingFlagAfterReload;
    }

    [MenuItem("Tools/HWJ/Assets/Wire Player Attack Slash Effects")]
    public static void WireFromMenu()
    {
        WirePlayerAttackEffects();
    }

    [MenuItem("Tools/HWJ/Assets/Create Player Attack Effect Wiring Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"HWJ player attack effect wiring flag created: {FlagFilePath}");
    }

    public static void WirePlayerAttackEffects()
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ player attack effect wiring is already running.");
            return;
        }

        isRunning = true;

        try
        {
            EnsureFolderPath(GeneratedFrameRoot);
            EnsureFolderPath(GeneratedAnimationRoot);
            EnsureFolderPath(GeneratedPrefabRoot);

            Dictionary<HWJ_WeaponType, GameObject> effectPrefabs = new Dictionary<HWJ_WeaponType, GameObject>();
            List<string> reportLines = new List<string>();

            for (int i = 0; i < Bindings.Length; i++)
            {
                GameObject prefab = CreateOrUpdateEffectPrefab(Bindings[i]);
                effectPrefabs[Bindings[i].WeaponType] = prefab;
                reportLines.Add($"- {Bindings[i].WeaponName}: `{Bindings[i].SourcePath}` -> `{Bindings[i].PrefabPath}`");
            }

            int wiredSkillActions = WireSkillActions(effectPrefabs, reportLines);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(wiredSkillActions, reportLines);
            Debug.Log($"HWJ player attack effect wiring finished. Report={ReportFilePath}");
        }
        finally
        {
            isRunning = false;
        }
    }

    private static GameObject CreateOrUpdateEffectPrefab(PlayerAttackEffectBinding binding)
    {
        EnsureFolderPath(binding.FrameFolderPath);
        EnsureFolderPath(binding.AnimationFolderPath);

        Sprite[] frames = SliceSheetToSprites(binding);
        AnimationClip clip = CreateOrUpdateSpriteClip(binding.ClipPath, frames);
        RuntimeAnimatorController controller = CreateOrUpdateController(binding.ControllerPath, clip);
        return CreateOrUpdatePrefab(binding, frames[0], controller, frames.Length / FrameRate + 0.05f);
    }

    private static Sprite[] SliceSheetToSprites(PlayerAttackEffectBinding binding)
    {
        string fullSourcePath = ToFullPath(binding.SourcePath);

        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException($"Missing slash sheet: {binding.SourcePath}");
        }

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        try
        {
            if (!source.LoadImage(File.ReadAllBytes(fullSourcePath)))
            {
                throw new InvalidOperationException($"Could not load slash sheet: {binding.SourcePath}");
            }

            int frameWidth = source.width / GridSize;
            int frameHeight = source.height / GridSize;
            List<Sprite> frames = new List<Sprite>(GridSize * GridSize);

            for (int row = 0; row < GridSize; row++)
            {
                for (int column = 0; column < GridSize; column++)
                {
                    int frameIndex = row * GridSize + column;
                    int x = column * frameWidth;
                    int y = source.height - frameHeight * (row + 1);
                    Texture2D frame = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);

                    try
                    {
                        frame.SetPixels(source.GetPixels(x, y, frameWidth, frameHeight));
                        frame.Apply();

                        string framePath = binding.GetFramePath(frameIndex);
                        EnsureHwjAssetPath(framePath);
                        File.WriteAllBytes(ToFullPath(framePath), frame.EncodeToPNG());
                        AssetDatabase.ImportAsset(framePath, ImportAssetOptions.ForceUpdate);
                        ConfigureSpriteImporter(framePath);

                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);

                        if (sprite == null)
                        {
                            throw new InvalidOperationException($"Generated frame is not a sprite: {framePath}");
                        }

                        frames.Add(sprite);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                }
            }

            return frames.ToArray();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = SpritePixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateOrUpdateSpriteClip(string clipPath, Sprite[] sprites)
    {
        EnsureHwjAssetPath(clipPath);
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = FrameRate;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / FrameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static RuntimeAnimatorController CreateOrUpdateController(string controllerPath, AnimationClip clip)
    {
        EnsureHwjAssetPath(controllerPath);

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        UnityEditor.Animations.AnimatorController controller =
            UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        UnityEditor.Animations.AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState playState = stateMachine.AddState("Play");
        playState.motion = clip;
        stateMachine.defaultState = playState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateOrUpdatePrefab(
        PlayerAttackEffectBinding binding,
        Sprite initialSprite,
        RuntimeAnimatorController controller,
        float lifetimeSeconds)
    {
        EnsureHwjAssetPath(binding.PrefabPath);

        GameObject root = new GameObject($"HWJ_Effect_PlayerAttack_{binding.WeaponName}Slash");
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = initialSprite;
        spriteRenderer.sortingOrder = 160;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        HWJ_PoolableObject poolableObject = root.AddComponent<HWJ_PoolableObject>();
        HWJ_EffectAutoReturnSystem autoReturn = root.AddComponent<HWJ_EffectAutoReturnSystem>();
        SerializedObject autoReturnObject = new SerializedObject(autoReturn);
        SetObjectReference(autoReturnObject, "animator", animator);
        SetObjectReference(autoReturnObject, "poolableObject", poolableObject);
        SerializedProperty lifetimeProperty = autoReturnObject.FindProperty("lifetimeSeconds");

        if (lifetimeProperty != null)
        {
            lifetimeProperty.floatValue = Mathf.Max(0.01f, lifetimeSeconds);
        }

        autoReturnObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, binding.PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static int WireSkillActions(
        Dictionary<HWJ_WeaponType, GameObject> effectPrefabs,
        List<string> reportLines)
    {
        int wiredCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:HWJ_SkillActionDataSO", new[] { SkillActionRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_SkillActionDataSO skillAction = AssetDatabase.LoadAssetAtPath<HWJ_SkillActionDataSO>(path);

            if (skillAction == null || !ShouldWireSkillAction(path, skillAction))
            {
                continue;
            }

            HWJ_WeaponType weaponType = skillAction.RequiredWeaponType;

            if (weaponType == HWJ_WeaponType.None)
            {
                weaponType = HWJ_WeaponType.Sword;
            }

            if (!effectPrefabs.TryGetValue(weaponType, out GameObject effectPrefab) || effectPrefab == null)
            {
                continue;
            }

            SerializedObject skillObject = new SerializedObject(skillAction);
            SetObjectReference(skillObject, "actionEffectPrefab", effectPrefab);
            skillObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skillAction);
            wiredCount++;
            reportLines.Add($"- Skill action wired: `{skillAction.SkillActionId}` -> `{AssetDatabase.GetAssetPath(effectPrefab)}`");
        }

        return wiredCount;
    }

    private static bool ShouldWireSkillAction(string assetPath, HWJ_SkillActionDataSO skillAction)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        if (fileName.StartsWith("HWJ_PlayerSkill_", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(skillAction.SkillActionId, "Player_Basic_Attack", StringComparison.Ordinal))
        {
            return true;
        }

        // Possessed player attacks use the body skill IDs directly, so these also need visible player-facing effects.
        return fileName.StartsWith("HWJ_Skill_Sword_", StringComparison.Ordinal)
            || fileName.StartsWith("HWJ_Skill_Axe_", StringComparison.Ordinal)
            || fileName.StartsWith("HWJ_Skill_Lance_", StringComparison.Ordinal)
            || fileName.StartsWith("HWJ_Skill_Shield_", StringComparison.Ordinal)
            || fileName.StartsWith("HWJ_Skill_Bow_", StringComparison.Ordinal);
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
        WirePlayerAttackEffects();
    }

    private static void RunPendingFlagAfterReload()
    {
        if (File.Exists(FlagFilePath))
        {
            PollFlagFile();
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void EnsureFolderPath(string folderPath)
    {
        EnsureHwjAssetPath(folderPath);
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void EnsureHwjAssetPath(string assetPath)
    {
        if (!assetPath.StartsWith(AssetRoot + "/", StringComparison.Ordinal)
            && !string.Equals(assetPath, AssetRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"HWJ player attack effect bridge can only write under {AssetRoot}: {assetPath}");
        }
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
    }

    private static void WriteReport(int wiredSkillActions, List<string> reportLines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Player Attack Effect Asset Wiring");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Wired Skill Actions: {wiredSkillActions}");
        builder.AppendLine("- Source Sheets: `Assets/02Scripts/HWJ/Art/effect/Slashes`");
        builder.AppendLine("- Each 2048x2048 sheet is sliced into 16 frames of 512x512.");
        builder.AppendLine();
        builder.AppendLine("## Details");
        builder.AppendLine();

        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.AppendLine(reportLines[i]);
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private sealed class PlayerAttackEffectBinding
    {
        public PlayerAttackEffectBinding(string weaponName, HWJ_WeaponType weaponType, string sourceFileName)
        {
            WeaponName = weaponName;
            WeaponType = weaponType;
            SourceFileName = sourceFileName;
        }

        public string WeaponName { get; }
        public HWJ_WeaponType WeaponType { get; }
        public string SourceFileName { get; }
        public string SourcePath => $"{SourceRoot}/{SourceFileName}";
        public string FrameFolderPath => $"{GeneratedFrameRoot}/{WeaponName}";
        public string AnimationFolderPath => $"{GeneratedAnimationRoot}/{WeaponName}";
        public string ClipPath => $"{AnimationFolderPath}/HWJ_PlayerAttackEffect_{WeaponName}Slash.anim";
        public string ControllerPath => $"{AnimationFolderPath}/HWJ_PlayerAttackEffect_{WeaponName}Slash.controller";
        public string PrefabPath => $"{GeneratedPrefabRoot}/HWJ_Effect_PlayerAttack_{WeaponName}Slash.prefab";

        public string GetFramePath(int frameIndex)
        {
            return $"{FrameFolderPath}/HWJ_PlayerAttackEffect_{WeaponName}Slash_{frameIndex:00}.png";
        }
    }
}

/// <summary>
/// Builds the shared hit-impact GIF frames into a Unity one-shot effect prefab and wires it to damage receivers.
/// Runtime spawning is handled by HWJ_HitEffectSystem, so attacks do not need per-skill hit VFX code.
/// </summary>
[InitializeOnLoad]
public static class HWJ_HitEffectAssetBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunHitEffectWire.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_HitEffectAssetReport.md";
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string FrameRoot = AssetRoot + "/Art/Generated/Effects/HitImpact";
    private const string AnimationRoot = AssetRoot + "/Animations/Generated/HitEffects";
    private const string PrefabRoot = AssetRoot + "/Prefabs/Generated/Effects";
    private const string GeneratedPrefabRoot = AssetRoot + "/Prefabs/Generated";
    private const string ClipPath = AnimationRoot + "/HWJ_HitImpact.anim";
    private const string ControllerPath = AnimationRoot + "/HWJ_HitImpact.controller";
    private const string PrefabPath = PrefabRoot + "/HWJ_Effect_HitImpact.prefab";
    private const float FrameRate = 60f;
    private const float PixelsPerUnit = 384f;

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_HitEffectAssetBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
        EditorApplication.delayCall -= RunPendingFlagAfterReload;
        EditorApplication.delayCall += RunPendingFlagAfterReload;
    }

    [MenuItem("Tools/HWJ/Art/Wire Hit Impact Effect")]
    public static void RunFromMenu()
    {
        WireHitEffect();
    }

    [MenuItem("Tools/HWJ/Art/Create Hit Impact Effect Wire Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"HWJ hit impact effect flag created: {FlagFilePath}");
    }

    private static void WireHitEffect()
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ hit impact effect bridge is already running.");
            return;
        }

        isRunning = true;
        List<string> reportLines = new List<string>();

        try
        {
            EnsureFolderPath(FrameRoot);
            EnsureFolderPath(AnimationRoot);
            EnsureFolderPath(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Sprite[] frames = LoadAndConfigureFrames(reportLines);

            if (frames.Length == 0)
            {
                throw new InvalidOperationException($"No hit impact frames were found in {FrameRoot}.");
            }

            AnimationClip clip = CreateOrUpdateClip(frames);
            RuntimeAnimatorController controller = CreateOrUpdateController(ControllerPath, clip);
            GameObject effectPrefab = CreateOrUpdatePrefab(frames[0], controller, frames.Length / FrameRate + 0.05f);
            int wiredPrefabs = WireRuntimePrefabs(effectPrefab, reportLines);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(frames.Length, wiredPrefabs, reportLines);
        }
        catch (Exception exception)
        {
            WriteExceptionReport(exception);
            Debug.LogException(exception);
        }
        finally
        {
            isRunning = false;
        }
    }

    private static Sprite[] LoadAndConfigureFrames(List<string> reportLines)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { FrameRoot });
        List<string> framePaths = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(assetPath).StartsWith("HWJ_HitImpact_", StringComparison.Ordinal))
            {
                framePaths.Add(assetPath);
            }
        }

        framePaths.Sort(StringComparer.Ordinal);
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < framePaths.Count; i++)
        {
            ConfigureSpriteImporter(framePaths[i]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePaths[i]);

            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        reportLines.Add($"- Configured hit impact PNG frames: {sprites.Count}");
        return sprites.ToArray();
    }

    private static void ConfigureSpriteImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    private static AnimationClip CreateOrUpdateClip(Sprite[] sprites)
    {
        EnsureHwjAssetPath(ClipPath);
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);

        if (clip == null)
        {
            clip = new AnimationClip
            {
                frameRate = FrameRate,
                wrapMode = WrapMode.Once
            };

            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        clip.frameRate = FrameRate;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / FrameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static RuntimeAnimatorController CreateOrUpdateController(string controllerPath, AnimationClip clip)
    {
        EnsureHwjAssetPath(controllerPath);

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        UnityEditor.Animations.AnimatorController controller =
            UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        UnityEditor.Animations.AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState playState = stateMachine.AddState("Play");
        playState.motion = clip;
        stateMachine.defaultState = playState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreateOrUpdatePrefab(
        Sprite initialSprite,
        RuntimeAnimatorController controller,
        float lifetimeSeconds)
    {
        EnsureHwjAssetPath(PrefabPath);

        GameObject root = new GameObject("HWJ_Effect_HitImpact");
        SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = initialSprite;
        spriteRenderer.sortingOrder = 220;

        Animator animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        HWJ_PoolableObject poolableObject = root.AddComponent<HWJ_PoolableObject>();
        HWJ_EffectAutoReturnSystem autoReturn = root.AddComponent<HWJ_EffectAutoReturnSystem>();
        SerializedObject autoReturnObject = new SerializedObject(autoReturn);
        SetObjectReference(autoReturnObject, "animator", animator);
        SetObjectReference(autoReturnObject, "poolableObject", poolableObject);
        SerializedProperty lifetimeProperty = autoReturnObject.FindProperty("lifetimeSeconds");

        if (lifetimeProperty != null)
        {
            lifetimeProperty.floatValue = Mathf.Max(0.01f, lifetimeSeconds);
        }

        autoReturnObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static int WireRuntimePrefabs(GameObject effectPrefab, List<string> reportLines)
    {
        int wiredCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { GeneratedPrefabRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (prefabPath.StartsWith(PrefabRoot + "/", StringComparison.Ordinal)
                || prefabPath.Contains("/Projectiles/", StringComparison.Ordinal)
                || prefabPath.Contains("/StatOrbs/", StringComparison.Ordinal)
                || prefabPath.Contains("/ExperienceOrbs/", StringComparison.Ordinal))
            {
                continue;
            }

            int wiredInPrefab = WireRuntimePrefab(prefabPath, effectPrefab);

            if (wiredInPrefab <= 0)
            {
                continue;
            }

            wiredCount += wiredInPrefab;
            reportLines.Add($"- Wired hit effect to `{prefabPath}` ({wiredInPrefab} receiver(s))");
        }

        return wiredCount;
    }

    private static int WireRuntimePrefab(string prefabPath, GameObject effectPrefab)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        bool changed = RemoveMissingScriptsRecursively(prefabRoot) > 0;
        int wiredCount = 0;

        try
        {
            HWJ_RuntimeStatusSystem[] statuses = prefabRoot.GetComponentsInChildren<HWJ_RuntimeStatusSystem>(true);

            for (int i = 0; i < statuses.Length; i++)
            {
                HWJ_RuntimeStatusSystem status = statuses[i];
                HWJ_HitEffectSystem hitEffectSystem = status.GetComponent<HWJ_HitEffectSystem>();

                if (hitEffectSystem == null)
                {
                    hitEffectSystem = status.gameObject.AddComponent<HWJ_HitEffectSystem>();
                    changed = true;
                }

                SerializedObject hitEffectObject = new SerializedObject(hitEffectSystem);
                SetObjectReference(hitEffectObject, "runtimeStatus", status);
                SetObjectReference(hitEffectObject, "dataResolver", status.GetComponent<HWJ_RootObjectDataResolver>());
                SetObjectReference(hitEffectObject, "hitEffectPrefab", effectPrefab);
                SetString(hitEffectObject, "hitEffectSocketName", "Hit");
                SetVector2(hitEffectObject, "fallbackOffset", new Vector2(0f, 0.2f));
                SetFloat(hitEffectObject, "minSpawnIntervalSeconds", 0.03f);
                SetFloat(hitEffectObject, "fallbackDestroyDelaySeconds", 1.25f);
                hitEffectObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hitEffectSystem);
                changed = true;
                wiredCount++;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return wiredCount;
    }

    private static int RemoveMissingScriptsRecursively(GameObject rootObject)
    {
        if (rootObject == null)
        {
            return 0;
        }

        int removedCount = 0;
        Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
        }

        return removedCount;
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
        WireHitEffect();
    }

    private static void RunPendingFlagAfterReload()
    {
        if (File.Exists(FlagFilePath))
        {
            PollFlagFile();
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.vector2Value = value;
        }
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void EnsureFolderPath(string folderPath)
    {
        EnsureHwjAssetPath(folderPath);
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void EnsureHwjAssetPath(string assetPath)
    {
        if (!assetPath.StartsWith(AssetRoot + "/", StringComparison.Ordinal)
            && !string.Equals(assetPath, AssetRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"HWJ hit impact effect bridge can only write under {AssetRoot}: {assetPath}");
        }
    }

    private static void WriteReport(int frameCount, int wiredPrefabs, List<string> reportLines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Hit Impact Effect Asset Wiring");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Source GIF: `C:/Users/user/Documents/lyd2fc.gif`");
        builder.AppendLine($"- Generated Frames: {frameCount}");
        builder.AppendLine($"- Wired Damage Receivers: {wiredPrefabs}");
        builder.AppendLine($"- Effect Prefab: `{PrefabPath}`");
        builder.AppendLine();
        builder.AppendLine("## Details");
        builder.AppendLine();

        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.AppendLine(reportLines[i]);
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteExceptionReport(Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Hit Impact Effect Asset Wiring Failed");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine(exception.ToString());
        builder.AppendLine("```");
        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }
}

/// <summary>
/// Converts the provided HWJ UI art into ready-to-use title and game-over UI prefabs, then registers their data assets.
/// The generated prefabs stay under Assets/02Scripts/HWJ so scene builders can reference them without touching other folders.
/// </summary>
[InitializeOnLoad]
public static class HWJ_UiAssetBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunUiAssetWire.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_UiAssetReport.md";
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string UiArtRoot = AssetRoot + "/Art/UI";
    private const string PrefabRoot = AssetRoot + "/Prefabs/Generated/UI";
    private const string SystemDataRoot = AssetRoot + "/ScriptableObjects/Systems";
    private const string DatabasePath = AssetRoot + "/ScriptableObjects/Database/HWJ_GameplayDatabase.asset";
    private const string TitlePrefabPath = PrefabRoot + "/HWJ_UI_TitleScreen.prefab";
    private const string GameOverPrefabPath = PrefabRoot + "/HWJ_UI_GameOverWindow.prefab";
    private const string TitleDataPath = SystemDataRoot + "/HWJ_TitleScreenData_Default.asset";
    private const string GameOverDataPath = SystemDataRoot + "/HWJ_GameOverData_Default.asset";

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_UiAssetBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
        EditorApplication.delayCall -= RunPendingFlagAfterReload;
        EditorApplication.delayCall += RunPendingFlagAfterReload;
    }

    [MenuItem("Tools/HWJ/Art/Wire Title And GameOver UI")]
    public static void RunFromMenu()
    {
        WireUiAssets("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Art/Create Title And GameOver UI Wire Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"HWJ UI asset wire flag created: {FlagFilePath}");
    }

    private static void WireUiAssets(string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ UI asset bridge is already running.");
            return;
        }

        isRunning = true;
        List<string> reportLines = new List<string>();

        try
        {
            EnsureFolderPath(PrefabRoot);
            EnsureFolderPath(SystemDataRoot);
            ConfigureUiSprite($"{UiArtRoot}/Main.png", 2048);
            ConfigureUiSprite($"{UiArtRoot}/logo.png", 2048);
            ConfigureUiSprite($"{UiArtRoot}/Main_Tile.png", 512);
            ConfigureUiSprite($"{UiArtRoot}/gameover.png", 2048);
            ConfigureUiSprite($"{UiArtRoot}/gameover_ui.png", 1024);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            HWJ_TitleScreenDataSO titleData = CreateOrUpdateTitleData(reportLines);
            HWJ_GameOverDataSO gameOverData = CreateOrUpdateGameOverData(reportLines);
            GameObject titlePrefab = CreateOrUpdateTitlePrefab(titleData, reportLines);
            GameObject gameOverPrefab = CreateOrUpdateGameOverPrefab(gameOverData, reportLines);
            WireTitleDataPrefab(titleData, titlePrefab);
            WireGameOverDataPrefab(gameOverData, gameOverPrefab);
            WireDatabase(titleData, gameOverData, reportLines);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(source, reportLines);
            Debug.Log($"HWJ UI asset wiring finished. Report={ReportFilePath}");
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

    private static HWJ_TitleScreenDataSO CreateOrUpdateTitleData(List<string> reportLines)
    {
        HWJ_TitleScreenDataSO data = AssetDatabase.LoadAssetAtPath<HWJ_TitleScreenDataSO>(TitleDataPath);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<HWJ_TitleScreenDataSO>();
            AssetDatabase.CreateAsset(data, TitleDataPath);
            reportLines.Add($"- Created title data: `{TitleDataPath}`");
        }

        SerializedObject dataObject = new SerializedObject(data);
        SetString(dataObject, "titleScreenId", "default_title");
        SetString(dataObject, "firstGameplaySceneName", string.Empty);
        SetBool(dataObject, "pauseGameWhileShown", true);
        dataObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static HWJ_GameOverDataSO CreateOrUpdateGameOverData(List<string> reportLines)
    {
        HWJ_GameOverDataSO data = AssetDatabase.LoadAssetAtPath<HWJ_GameOverDataSO>(GameOverDataPath);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<HWJ_GameOverDataSO>();
            AssetDatabase.CreateAsset(data, GameOverDataPath);
            reportLines.Add($"- Created game-over data: `{GameOverDataPath}`");
        }

        SerializedObject dataObject = new SerializedObject(data);
        SetString(dataObject, "gameOverId", "default_game_over");
        SetString(dataObject, "titleText", "Game Over");
        SetString(dataObject, "restartSceneName", string.Empty);
        dataObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static GameObject CreateOrUpdateTitlePrefab(HWJ_TitleScreenDataSO titleData, List<string> reportLines)
    {
        Sprite backgroundSprite = LoadSprite($"{UiArtRoot}/Main.png");
        Sprite logoSprite = LoadSprite($"{UiArtRoot}/logo.png");
        Sprite buttonSprite = LoadSprite($"{UiArtRoot}/Main_Tile.png");

        GameObject root = CreateCanvasRoot("HWJ_UI_TitleScreen", 1000);
        Image background = CreateImage(root.transform, "Background", backgroundSprite, new Color(1f, 1f, 1f, 1f));
        StretchRect(background.rectTransform);

        Image logo = CreateImage(root.transform, "Logo", logoSprite, Color.white);
        SetCenteredRect(logo.rectTransform, new Vector2(760f, 632f), new Vector2(0f, 150f));
        logo.preserveAspect = true;

        Button startButton = CreateButton(root.transform, "StartButton", buttonSprite, "START", new Vector2(420f, 75f), new Vector2(0f, -305f));
        Button quitButton = CreateButton(root.transform, "QuitButton", buttonSprite, "QUIT", new Vector2(420f, 75f), new Vector2(0f, -395f));

        HWJ_TitleScreenWindowSystem titleWindow = root.AddComponent<HWJ_TitleScreenWindowSystem>();
        SerializedObject titleWindowObject = new SerializedObject(titleWindow);
        SetObjectReference(titleWindowObject, "titleScreenData", titleData);
        SetObjectReference(titleWindowObject, "windowRoot", root);
        SetObjectReference(titleWindowObject, "startButton", startButton);
        SetObjectReference(titleWindowObject, "quitButton", quitButton);
        SetBool(titleWindowObject, "showOnEnable", true);
        titleWindowObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TitlePrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        reportLines.Add($"- Created title prefab: `{TitlePrefabPath}`");
        return prefab;
    }

    private static GameObject CreateOrUpdateGameOverPrefab(HWJ_GameOverDataSO gameOverData, List<string> reportLines)
    {
        Sprite gameOverSprite = LoadSprite($"{UiArtRoot}/gameover.png");
        Sprite buttonSprite = LoadSprite($"{UiArtRoot}/gameover_ui.png");

        GameObject root = CreateCanvasRoot("HWJ_UI_GameOverWindow", 1100);
        Image dim = CreateImage(root.transform, "DimOverlay", null, new Color(0f, 0f, 0f, 0.88f));
        StretchRect(dim.rectTransform);

        Image title = CreateImage(root.transform, "GameOverTitle", gameOverSprite, Color.white);
        SetCenteredRect(title.rectTransform, new Vector2(980f, 505f), new Vector2(0f, 165f));
        title.preserveAspect = true;

        Button retryButton = CreateButton(root.transform, "RetryButton", buttonSprite, "RETRY", new Vector2(560f, 145f), new Vector2(0f, -260f));
        Button quitButton = CreateButton(root.transform, "QuitButton", buttonSprite, "QUIT", new Vector2(560f, 145f), new Vector2(0f, -405f));

        HWJ_GameOverUiInputSystem inputSystem = root.AddComponent<HWJ_GameOverUiInputSystem>();
        SerializedObject inputObject = new SerializedObject(inputSystem);
        SetObjectReference(inputObject, "gameOverData", gameOverData);
        SetObjectReference(inputObject, "restartButton", retryButton);
        SetObjectReference(inputObject, "quitButton", quitButton);
        inputObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GameOverPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        reportLines.Add($"- Created game-over prefab: `{GameOverPrefabPath}`");
        return prefab;
    }

    private static GameObject CreateCanvasRoot(string objectName, int sortingOrder)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        return root;
    }

    private static Image CreateImage(Transform parent, string objectName, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button CreateButton(
        Transform parent,
        string objectName,
        Sprite sprite,
        string label,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.72f, 1f);
        colors.pressedColor = new Color(0.72f, 0.18f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        SetCenteredRect(rectTransform, size, anchoredPosition);

        Text text = CreateButtonText(buttonObject.transform, label);
        StretchRect(text.rectTransform);
        return button;
    }

    private static Text CreateButtonText(Transform parent, string label)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = ResolveBuiltinFont();
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.78f, 0.72f, 0.66f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static Font ResolveBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    private static void WireTitleDataPrefab(HWJ_TitleScreenDataSO data, GameObject prefab)
    {
        SerializedObject dataObject = new SerializedObject(data);
        SetObjectReference(dataObject, "windowPrefab", prefab);
        dataObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void WireGameOverDataPrefab(HWJ_GameOverDataSO data, GameObject prefab)
    {
        SerializedObject dataObject = new SerializedObject(data);
        SetObjectReference(dataObject, "windowPrefab", prefab);
        dataObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void WireDatabase(
        HWJ_TitleScreenDataSO titleData,
        HWJ_GameOverDataSO gameOverData,
        List<string> reportLines)
    {
        HWJ_GameplayDatabaseSO database = AssetDatabase.LoadAssetAtPath<HWJ_GameplayDatabaseSO>(DatabasePath);

        if (database == null)
        {
            reportLines.Add($"- Database not found, skipped DB wiring: `{DatabasePath}`");
            return;
        }

        SerializedObject databaseObject = new SerializedObject(database);
        bool changed = EnsureArrayContains(databaseObject, "titleScreens", titleData);
        changed |= EnsureArrayContains(databaseObject, "gameOverWindows", gameOverData);

        if (changed)
        {
            databaseObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            reportLines.Add($"- Wired UI data into database: `{DatabasePath}`");
        }
        else
        {
            databaseObject.ApplyModifiedPropertiesWithoutUndo();
            reportLines.Add($"- Database already contained UI data: `{DatabasePath}`");
        }
    }

    private static bool EnsureArrayContains(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null || !property.isArray || value == null)
        {
            return false;
        }

        for (int i = 0; i < property.arraySize; i++)
        {
            if (property.GetArrayElementAtIndex(i).objectReferenceValue == value)
            {
                return false;
            }
        }

        int newIndex = property.arraySize;
        property.InsertArrayElementAtIndex(newIndex);
        property.GetArrayElementAtIndex(newIndex).objectReferenceValue = value;
        return true;
    }

    private static void ConfigureUiSprite(string assetPath, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = maxTextureSize;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (sprite == null)
        {
            throw new InvalidOperationException($"UI sprite could not be loaded: {assetPath}");
        }

        return sprite;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetCenteredRect(RectTransform rectTransform, Vector2 size, Vector2 anchoredPosition)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
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
        WireUiAssets("flag file");
    }

    private static void RunPendingFlagAfterReload()
    {
        if (File.Exists(FlagFilePath))
        {
            PollFlagFile();
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void EnsureFolderPath(string folderPath)
    {
        EnsureHwjAssetPath(folderPath);
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void EnsureHwjAssetPath(string assetPath)
    {
        if (!assetPath.StartsWith(AssetRoot + "/", StringComparison.Ordinal)
            && !string.Equals(assetPath, AssetRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"HWJ UI asset bridge can only write under {AssetRoot}: {assetPath}");
        }
    }

    private static void WriteReport(string source, List<string> reportLines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ UI Asset Wiring");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Source: {source}");
        builder.AppendLine($"- Title Prefab: `{TitlePrefabPath}`");
        builder.AppendLine($"- GameOver Prefab: `{GameOverPrefabPath}`");
        builder.AppendLine($"- Title Data: `{TitleDataPath}`");
        builder.AppendLine($"- GameOver Data: `{GameOverDataPath}`");
        builder.AppendLine();
        builder.AppendLine("## Details");
        builder.AppendLine();

        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.AppendLine(reportLines[i]);
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteExceptionReport(string source, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ UI Asset Wiring Failed");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Source: {source}");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine(exception.ToString());
        builder.AppendLine("```");
        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }
}

/// <summary>
/// Slices the EXP orb sprite sheet into looping pickup animations.
/// Experience orbs keep the source color, while stat orbs reuse the same motion with stat-specific recolored frames.
/// </summary>
[InitializeOnLoad]
public static class HWJ_RewardOrbVisualBridge
{
    private const string FlagFilePath = @"C:\Docs\Generated\HWJ_RunRewardOrbVisualWire.flag";
    private const string ReportFilePath = @"C:\Docs\Generated\HWJ_RewardOrbVisualReport.md";
    private const string AssetRoot = "Assets/02Scripts/HWJ";
    private const string SourceRoot = AssetRoot + "/Art/exp";
    private const string GeneratedFrameRoot = AssetRoot + "/Art/Generated/Orbs";
    private const string GeneratedAnimationRoot = AssetRoot + "/Animations/Generated/Orbs";
    private const string ExperiencePrefabPath = AssetRoot + "/Prefabs/Generated/ExperienceOrbs/HWJ_ExperienceOrb_Default_Prefab.prefab";
    private const string StatOrbPrefabRoot = AssetRoot + "/Prefabs/Generated/StatOrbs";
    private const string StatOrbDataRoot = AssetRoot + "/ScriptableObjects/StatOrbs";
    private const int CellSize = 260;
    private const int FrameCount = 8;
    private const float FrameRate = 12f;
    private const float PixelsPerUnit = 260f;

    private static readonly int[] CellLefts = { 239, 497, 758, 1027 };
    private static readonly int[] CellTopYs = { 280, 504 };

    private static bool isRunning;
    private static double nextFlagCheckTime;

    static HWJ_RewardOrbVisualBridge()
    {
        EditorApplication.update -= PollFlagFile;
        EditorApplication.update += PollFlagFile;
        EditorApplication.delayCall -= RunPendingFlagAfterReload;
        EditorApplication.delayCall += RunPendingFlagAfterReload;
    }

    [MenuItem("Tools/HWJ/Art/Wire Reward Orb Animations")]
    public static void RunFromMenu()
    {
        WireRewardOrbVisuals("Unity Editor menu");
    }

    [MenuItem("Tools/HWJ/Art/Create Reward Orb Animation Wire Flag")]
    public static void CreateRunFlag()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FlagFilePath));
        File.WriteAllText(FlagFilePath, DateTime.Now.ToString("O"), Encoding.UTF8);
        Debug.Log($"HWJ reward orb visual wire flag created: {FlagFilePath}");
    }

    private static void WireRewardOrbVisuals(string source)
    {
        if (isRunning)
        {
            Debug.LogWarning("HWJ reward orb visual bridge is already running.");
            return;
        }

        isRunning = true;
        List<string> reportLines = new List<string>();

        try
        {
            EnsureFolderPath(GeneratedFrameRoot);
            EnsureFolderPath(GeneratedAnimationRoot);

            string sourceSheetPath = ResolveSourceSheetPath();
            Texture2D sourceTexture = LoadReadableSourceTexture(sourceSheetPath);
            reportLines.Add($"- Source texture import size: {sourceTexture.width}x{sourceTexture.height}");

            RewardOrbVariant experienceVariant = RewardOrbVariant.Experience();
            Sprite[] experienceSprites = CreateVariantFrames(sourceTexture, experienceVariant, true, reportLines);
            RuntimeAnimatorController experienceController = CreateOrUpdateAnimationAssets(experienceVariant, experienceSprites);
            WireExperienceOrbPrefab(experienceVariant, experienceSprites[0], experienceController, reportLines);

            RewardOrbVariant[] statVariants = RewardOrbVariant.StatVariants();

            for (int i = 0; i < statVariants.Length; i++)
            {
                RewardOrbVariant variant = statVariants[i];
                Sprite[] sprites = CreateVariantFrames(sourceTexture, variant, false, reportLines);
                RuntimeAnimatorController controller = CreateOrUpdateAnimationAssets(variant, sprites);
                WireStatOrbPrefab(variant, sprites[0], controller, reportLines);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteReport(source, sourceSheetPath, reportLines);
            Debug.Log($"HWJ reward orb visual wiring finished. Report={ReportFilePath}");
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

    private static Sprite[] CreateVariantFrames(
        Texture2D sourceTexture,
        RewardOrbVariant variant,
        bool preserveSourceColor,
        List<string> reportLines)
    {
        EnsureFolderPath(variant.FrameFolderPath);
        List<string> framePaths = new List<string>();

        for (int i = 0; i < FrameCount; i++)
        {
            int column = i % CellLefts.Length;
            int row = i / CellLefts.Length;
            Texture2D frameTexture = CreateFrameTexture(sourceTexture, CellLefts[column], CellTopYs[row], variant.Tint, preserveSourceColor);
            string framePath = variant.GetFramePath(i);
            File.WriteAllBytes(ToFullPath(framePath), frameTexture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(frameTexture);
            framePaths.Add(framePath);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        for (int i = 0; i < framePaths.Count; i++)
        {
            ConfigureGeneratedSprite(framePaths[i]);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        Sprite[] sprites = new Sprite[framePaths.Count];

        for (int i = 0; i < framePaths.Count; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(framePaths[i]);

            if (sprites[i] == null)
            {
                throw new InvalidOperationException($"Generated orb frame could not be loaded: {framePaths[i]}");
            }
        }

        reportLines.Add($"- Generated {variant.DisplayName} orb frames: {sprites.Length}");
        return sprites;
    }

    private static Texture2D CreateFrameTexture(
        Texture2D sourceTexture,
        int left,
        int top,
        Color tint,
        bool preserveSourceColor)
    {
        int sourceBottom = sourceTexture.height - top - CellSize;
        Color[] sourcePixels = sourceTexture.GetPixels(left, sourceBottom, CellSize, CellSize);
        Texture2D frameTexture = new Texture2D(CellSize, CellSize, TextureFormat.RGBA32, false);
        Color[] outputPixels = new Color[sourcePixels.Length];

        for (int i = 0; i < sourcePixels.Length; i++)
        {
            outputPixels[i] = ProcessOrbPixel(sourcePixels[i], tint, preserveSourceColor);
        }

        frameTexture.SetPixels(outputPixels);
        frameTexture.Apply();
        return frameTexture;
    }

    private static Color ProcessOrbPixel(Color pixel, Color tint, bool preserveSourceColor)
    {
        if (pixel.a <= 0.18f)
        {
            return Color.clear;
        }

        float maxChannel = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));

        // The source sheet contains a dark preview tile behind the orb. Key out that tile so pickups render cleanly in-world.
        if (pixel.r < 0.12f && pixel.g < 0.18f && pixel.b < 0.3f && maxChannel < 0.3f)
        {
            return Color.clear;
        }

        if (preserveSourceColor)
        {
            return pixel;
        }

        float luminance = Mathf.Clamp01((pixel.r * 0.25f) + (pixel.g * 0.45f) + (pixel.b * 0.3f));
        float sparkle = Mathf.Clamp01((luminance - 0.72f) / 0.28f);
        Color colored = new Color(
            Mathf.Clamp01(tint.r * Mathf.Lerp(0.45f, 1.45f, luminance)),
            Mathf.Clamp01(tint.g * Mathf.Lerp(0.45f, 1.45f, luminance)),
            Mathf.Clamp01(tint.b * Mathf.Lerp(0.45f, 1.45f, luminance)),
            pixel.a);
        Color finalColor = Color.Lerp(colored, Color.white, sparkle * 0.65f);
        finalColor.a = pixel.a;
        return finalColor;
    }

    private static RuntimeAnimatorController CreateOrUpdateAnimationAssets(RewardOrbVariant variant, Sprite[] sprites)
    {
        EnsureFolderPath(variant.AnimationFolderPath);
        AnimationClip clip = CreateOrUpdateClip(variant.ClipPath, sprites);
        return CreateOrUpdateController(variant.ControllerPath, clip);
    }

    private static AnimationClip CreateOrUpdateClip(string clipPath, Sprite[] sprites)
    {
        EnsureHwjAssetPath(clipPath);
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip
            {
                frameRate = FrameRate,
                wrapMode = WrapMode.Loop
            };

            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = FrameRate;
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / FrameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static RuntimeAnimatorController CreateOrUpdateController(string controllerPath, AnimationClip clip)
    {
        EnsureHwjAssetPath(controllerPath);

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        UnityEditor.Animations.AnimatorController controller =
            UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        UnityEditor.Animations.AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState playState = stateMachine.AddState("Loop");
        playState.motion = clip;
        stateMachine.defaultState = playState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void WireExperienceOrbPrefab(
        RewardOrbVariant variant,
        Sprite initialSprite,
        RuntimeAnimatorController controller,
        List<string> reportLines)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(ExperiencePrefabPath);

        try
        {
            SpriteRenderer spriteRenderer = EnsureComponent<SpriteRenderer>(prefabRoot);
            spriteRenderer.sprite = initialSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 85;

            CircleCollider2D collider = EnsureComponent<CircleCollider2D>(prefabRoot);
            collider.isTrigger = true;
            collider.radius = 0.35f;

            Animator animator = EnsureComponent<Animator>(prefabRoot);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, ExperiencePrefabPath);
            reportLines.Add($"- Wired experience orb animation: `{ExperiencePrefabPath}` -> `{variant.ControllerPath}`");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void WireStatOrbPrefab(
        RewardOrbVariant variant,
        Sprite initialSprite,
        RuntimeAnimatorController controller,
        List<string> reportLines)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(variant.PrefabPath);

        try
        {
            SpriteRenderer spriteRenderer = EnsureComponent<SpriteRenderer>(prefabRoot);
            spriteRenderer.sprite = initialSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 80;

            CircleCollider2D collider = EnsureComponent<CircleCollider2D>(prefabRoot);
            collider.isTrigger = true;
            collider.radius = 0.35f;

            Animator animator = EnsureComponent<Animator>(prefabRoot);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            HWJ_StatOrbPickupSystem pickupSystem = EnsureComponent<HWJ_StatOrbPickupSystem>(prefabRoot);
            HWJ_StatOrbDataSO statOrbData = LoadStatOrbDataById(variant.StatOrbId);

            if (statOrbData != null)
            {
                SerializedObject pickupObject = new SerializedObject(pickupSystem);
                SetObjectReference(pickupObject, "statOrbData", statOrbData);
                pickupObject.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject dataObject = new SerializedObject(statOrbData);
                SetObjectReference(dataObject, "orbPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(variant.PrefabPath));
                dataObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(statOrbData);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, variant.PrefabPath);
            reportLines.Add($"- Wired stat orb animation: `{variant.PrefabPath}` -> `{variant.ControllerPath}`");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Texture2D LoadReadableSourceTexture(string sourceSheetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(sourceSheetPath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException($"EXP orb sheet importer was not found: {sourceSheetPath}");
        }

        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        // Keep the source sheet at its real pixel size so the hard-coded slice coordinates stay accurate.
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourceSheetPath);

        if (texture == null)
        {
            throw new InvalidOperationException($"EXP orb sheet could not be loaded: {sourceSheetPath}");
        }

        return texture;
    }

    private static string ResolveSourceSheetPath()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceRoot });

        if (guids == null || guids.Length == 0)
        {
            throw new InvalidOperationException($"No EXP orb sprite sheet was found under {SourceRoot}.");
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        throw new InvalidOperationException($"No PNG EXP orb sprite sheet was found under {SourceRoot}.");
    }

    private static void ConfigureGeneratedSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();
    }

    private static HWJ_StatOrbDataSO LoadStatOrbDataById(string statOrbId)
    {
        string[] guids = AssetDatabase.FindAssets("t:HWJ_StatOrbDataSO", new[] { StatOrbDataRoot });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            HWJ_StatOrbDataSO data = AssetDatabase.LoadAssetAtPath<HWJ_StatOrbDataSO>(path);

            if (data != null && data.OrbId == statOrbId)
            {
                return data;
            }
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();

        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
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
        WireRewardOrbVisuals("flag file");
    }

    private static void RunPendingFlagAfterReload()
    {
        if (File.Exists(FlagFilePath))
        {
            PollFlagFile();
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void EnsureFolderPath(string folderPath)
    {
        EnsureHwjAssetPath(folderPath);
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }

        Directory.CreateDirectory(ToFullPath(folderPath));
    }

    private static void EnsureHwjAssetPath(string assetPath)
    {
        if (!assetPath.StartsWith(AssetRoot + "/", StringComparison.Ordinal)
            && !string.Equals(assetPath, AssetRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"HWJ reward orb visual bridge can only write under {AssetRoot}: {assetPath}");
        }
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, assetPath).Replace("/", Path.DirectorySeparatorChar.ToString());
    }

    private static void WriteReport(string source, string sourceSheetPath, List<string> reportLines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Reward Orb Visual Wiring");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Source: {source}");
        builder.AppendLine($"- Source Sheet: `{sourceSheetPath}`");
        builder.AppendLine($"- Frame Count: {FrameCount}");
        builder.AppendLine($"- Experience Color: source sprite color");
        builder.AppendLine($"- Stat Orb Colors: generated recolor variants");
        builder.AppendLine();
        builder.AppendLine("## Details");
        builder.AppendLine();

        for (int i = 0; i < reportLines.Count; i++)
        {
            builder.AppendLine(reportLines[i]);
        }

        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteExceptionReport(string source, Exception exception)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportFilePath));
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# HWJ Reward Orb Visual Wiring Failed");
        builder.AppendLine();
        builder.AppendLine($"- Executed At: {DateTime.Now:O}");
        builder.AppendLine($"- Source: {source}");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine(exception.ToString());
        builder.AppendLine("```");
        File.WriteAllText(ReportFilePath, builder.ToString(), Encoding.UTF8);
    }

    private sealed class RewardOrbVariant
    {
        private RewardOrbVariant(
            string key,
            string displayName,
            Color tint,
            string prefabPath,
            string statOrbId)
        {
            Key = key;
            DisplayName = displayName;
            Tint = tint;
            PrefabPath = prefabPath;
            StatOrbId = statOrbId;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public Color Tint { get; }
        public string PrefabPath { get; }
        public string StatOrbId { get; }
        public string FrameFolderPath => $"{GeneratedFrameRoot}/{Key}";
        public string AnimationFolderPath => $"{GeneratedAnimationRoot}/{Key}";
        public string ClipPath => $"{AnimationFolderPath}/HWJ_Orb_{Key}.anim";
        public string ControllerPath => $"{AnimationFolderPath}/HWJ_Orb_{Key}.controller";

        public string GetFramePath(int frameIndex)
        {
            return $"{FrameFolderPath}/HWJ_Orb_{Key}_{frameIndex:00}.png";
        }

        public static RewardOrbVariant Experience()
        {
            return new RewardOrbVariant(
                "Experience",
                "Experience",
                Color.white,
                ExperiencePrefabPath,
                null);
        }

        public static RewardOrbVariant[] StatVariants()
        {
            return new[]
            {
                new RewardOrbVariant("Stat_AttackPower", "AttackPower Stat", new Color(1f, 0.24f, 0.18f, 1f), $"{StatOrbPrefabRoot}/HWJ_StatOrb_AttackPowerSmall_Prefab.prefab", "stat_attack_power_small"),
                new RewardOrbVariant("Stat_AttackSpeed", "AttackSpeed Stat", new Color(1f, 0.82f, 0.18f, 1f), $"{StatOrbPrefabRoot}/HWJ_StatOrb_AttackSpeedSmall_Prefab.prefab", "stat_attack_speed_small"),
                new RewardOrbVariant("Stat_Defense", "Defense Stat", new Color(0.28f, 0.58f, 1f, 1f), $"{StatOrbPrefabRoot}/HWJ_StatOrb_DefenseSmall_Prefab.prefab", "stat_defense_small"),
                new RewardOrbVariant("Stat_MaxHp", "MaxHp Stat", new Color(0.25f, 1f, 0.42f, 1f), $"{StatOrbPrefabRoot}/HWJ_StatOrb_MaxHpSmall_Prefab.prefab", "stat_max_hp_small"),
                new RewardOrbVariant("Stat_MoveSpeed", "MoveSpeed Stat", new Color(0.75f, 0.44f, 1f, 1f), $"{StatOrbPrefabRoot}/HWJ_StatOrb_MoveSpeedSmall_Prefab.prefab", "stat_move_speed_small")
            };
        }
    }
}
