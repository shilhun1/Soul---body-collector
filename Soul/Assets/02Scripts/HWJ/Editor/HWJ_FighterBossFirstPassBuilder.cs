using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 기존 MidBoss 프리팹을 백업한 뒤 격투가 보스 1차 검증 구조를 생성합니다.
/// P1_Attack_Combo 이외의 기존 보스 패턴은 프리팹에서 비활성화합니다.
/// </summary>
public static class HWJ_FighterBossFirstPassBuilder
{
    private const string BossPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab";
    private const string BossBackupPath =
        "Assets/02Scripts/HWJ/Prefabs/Backups/Bosses/HWJ_MidBoss1_Runtime_Prefab_PreFighterBossFirstPass.prefab";
    private const string BossRootDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset";
    private const string BossArtFolder =
        "Assets/02Scripts/HWJ/Art/Boss/HWJ_Blue_Knight";
    private const string AnimationFolder =
        "Assets/02Scripts/HWJ/Animations/Boss/Fighter";
    private const string ControllerPath =
        AnimationFolder + "/HWJ_FighterBoss.controller";
    private const string PatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P1_Attack_Combo.asset";
    private const string ChargePatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P1_Attack_Charge.asset";
    private const string UppercutPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P1_Attack_Uppercut.asset";
    private const string GroundSlamPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P1_Attack_GroundSlam.asset";
    private const string EnhancedComboPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Enhanced_Combo.asset";
    private const string EnhancedChargePatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Enhanced_Charge.asset";
    private const string EnhancedUppercutPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Enhanced_Uppercut.asset";
    private const string EnhancedGroundSlamPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Enhanced_GroundSlam.asset";
    private const string ShockwavePatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Attack_Shockwave.asset";
    private const string AerialDivePatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Attack_AerialDive.asset";
    private const string CrossSlashPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Attack_CrossSlash.asset";
    private const string PhantomRushPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Attack_PhantomRush.asset";
    private const string ExecutionPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Attack_Execution.asset";
    private const string TelegraphMaterialPath =
        "Assets/02Scripts/HWJ/Art/Generated/Boss/HWJ_FighterBoss_DevLine.mat";

    [MenuItem("Tools/HWJ/Boss/Build Fighter Boss First Pass")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void BuildBatch()
    {
        try
        {
            Build();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Build()
    {
        EnsureAssetFolder("Assets/02Scripts/HWJ/Prefabs/Backups/Bosses");
        EnsureAssetFolder(AnimationFolder);
        EnsureAssetFolder("Assets/02Scripts/HWJ/Art/Generated/Boss");

        BackupBossPrefab();
        ConfigureBossSpriteImporters();

        Sprite[] idleSprites = LoadSpriteSequence("B_Knight_idle", 5);
        Sprite[] attackSprites = LoadSpriteSequence("B_Knight_atk", 13);
        Sprite[] deathSprites = LoadSpriteSequence("B_Knight_die", 10);

        AnimationClip idleClip = CreateOrUpdateClip(
            "Idle",
            idleSprites,
            0.6f,
            true,
            null);
        AnimationClip moveClip = CreateOrUpdateClip(
            "Move",
            idleSprites,
            0.42f,
            true,
            null);
        AnimationClip hurtClip = CreateOrUpdateClip(
            "Hurt",
            Slice(attackSprites, 0, 2),
            0.2f,
            false,
            null);
        AnimationClip comboClip = CreateOrUpdateClip(
            "P1_Attack_Combo",
            attackSprites,
            1.75f,
            false,
            CreateComboAnimationEvents());
        AnimationClip chargeClip = CreateOrUpdateClip(
            "P1_Attack_Charge",
            attackSprites,
            1.95f,
            false,
            CreateChargeAnimationEvents());
        AnimationClip uppercutClip = CreateOrUpdateClip(
            "P1_Attack_Uppercut",
            attackSprites,
            1.4f,
            false,
            CreateUppercutAnimationEvents());
        AnimationClip groundSlamClip = CreateOrUpdateClip(
            "P1_Attack_GroundSlam",
            attackSprites,
            1.7f,
            false,
            CreateGroundSlamAnimationEvents());
        AnimationClip enhancedComboClip = CreateOrUpdateClip(
            "P2_Enhanced_Combo",
            attackSprites,
            1.55f,
            false,
            CreateEnhancedComboAnimationEvents());
        AnimationClip enhancedChargeClip = CreateOrUpdateClip(
            "P2_Enhanced_Charge",
            attackSprites,
            1.6f,
            false,
            CreateEnhancedChargeAnimationEvents());
        AnimationClip enhancedUppercutClip = CreateOrUpdateClip(
            "P2_Enhanced_Uppercut",
            attackSprites,
            1.2f,
            false,
            CreateEnhancedUppercutAnimationEvents());
        AnimationClip enhancedGroundSlamClip = CreateOrUpdateClip(
            "P2_Enhanced_GroundSlam",
            attackSprites,
            1.45f,
            false,
            CreateEnhancedGroundSlamAnimationEvents());
        AnimationClip shockwaveClip = CreateOrUpdateClip(
            "P2_Attack_Shockwave",
            attackSprites,
            1.5f,
            false,
            CreateShockwaveAnimationEvents());
        AnimationClip aerialDiveClip = CreateOrUpdateClip(
            "P2_Attack_AerialDive",
            attackSprites,
            1.8f,
            false,
            CreateAerialDiveAnimationEvents());
        AnimationClip crossSlashClip = CreateOrUpdateClip(
            "P2_Attack_CrossSlash",
            attackSprites,
            1.4f,
            false,
            CreateCrossSlashAnimationEvents());
        AnimationClip phantomRushClip = CreateOrUpdateClip(
            "P2_Attack_PhantomRush",
            attackSprites,
            1.55f,
            false,
            CreatePhantomRushAnimationEvents());
        AnimationClip executionClip = CreateOrUpdateClip(
            "P2_Attack_Execution",
            attackSprites,
            1.9f,
            false,
            CreateExecutionAnimationEvents());
        AnimationClip downClip = CreateOrUpdateClip(
            "PhaseBreak_Down",
            Slice(deathSprites, 0, 3),
            0.6f,
            false,
            null);
        AnimationClip prayerClip = CreateOrUpdateClip(
            "PhaseBreak_Prayer",
            idleSprites,
            0.7f,
            true,
            null);
        AnimationClip lightningClip = CreateOrUpdateClip(
            "PhaseBreak_LightningHit",
            Slice(attackSprites, 4, 3),
            0.45f,
            false,
            null);
        AnimationClip transformClip = CreateOrUpdateClip(
            "PhaseBreak_Transform",
            Slice(attackSprites, 7, 4),
            0.7f,
            false,
            null);
        AnimationClip phaseTwoStartClip = CreateOrUpdateClip(
            "Phase2_Start",
            Slice(attackSprites, 9, 4),
            0.65f,
            false,
            null);
        AnimationClip deathClip = CreateOrUpdateClip(
            "P2_Death",
            deathSprites,
            1.2f,
            false,
            CreateDeathAnimationEvents());

        AnimatorController controller = CreateOrUpdateController(
            idleClip,
            moveClip,
            hurtClip,
            comboClip,
            chargeClip,
            uppercutClip,
            groundSlamClip,
            enhancedComboClip,
            enhancedChargeClip,
            enhancedUppercutClip,
            enhancedGroundSlamClip,
            shockwaveClip,
            aerialDiveClip,
            crossSlashClip,
            phantomRushClip,
            executionClip,
            downClip,
            prayerClip,
            lightningClip,
            transformClip,
            phaseTwoStartClip,
            deathClip);
        HWJ_BossPatternDataSO comboPattern = CreateOrUpdateComboPattern();
        HWJ_BossPatternDataSO chargePattern = CreateOrUpdateChargePattern();
        HWJ_BossPatternDataSO uppercutPattern = CreateOrUpdateUppercutPattern();
        HWJ_BossPatternDataSO groundSlamPattern = CreateOrUpdateGroundSlamPattern();
        HWJ_BossPatternDataSO enhancedComboPattern = CreateOrUpdatePattern(
            EnhancedComboPatternPath,
            "P2_Enhanced_Combo",
            5,
            HWJ_BossPatternRangeMode.Close,
            false,
            true,
            2.4f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO enhancedChargePattern = CreateOrUpdatePattern(
            EnhancedChargePatternPath,
            "P2_Enhanced_Charge",
            6,
            HWJ_BossPatternRangeMode.Far,
            false,
            true,
            2.8f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO enhancedUppercutPattern = CreateOrUpdatePattern(
            EnhancedUppercutPatternPath,
            "P2_Enhanced_Uppercut",
            7,
            HWJ_BossPatternRangeMode.Close,
            false,
            true,
            2.6f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO enhancedGroundSlamPattern = CreateOrUpdatePattern(
            EnhancedGroundSlamPatternPath,
            "P2_Enhanced_GroundSlam",
            8,
            HWJ_BossPatternRangeMode.Close,
            false,
            true,
            3.2f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO shockwavePattern = CreateOrUpdatePattern(
            ShockwavePatternPath,
            "P2_Attack_Shockwave",
            9,
            HWJ_BossPatternRangeMode.Far,
            false,
            true,
            3f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO aerialDivePattern = CreateOrUpdatePattern(
            AerialDivePatternPath,
            "P2_Attack_AerialDive",
            10,
            HWJ_BossPatternRangeMode.Far,
            false,
            true,
            4f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO crossSlashPattern = CreateOrUpdatePattern(
            CrossSlashPatternPath,
            "P2_Attack_CrossSlash",
            11,
            HWJ_BossPatternRangeMode.Close,
            false,
            true,
            2.7f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO phantomRushPattern = CreateOrUpdatePattern(
            PhantomRushPatternPath,
            "P2_Attack_PhantomRush",
            12,
            HWJ_BossPatternRangeMode.Far,
            false,
            true,
            4.2f,
            "fighter_boss_phase_two");
        HWJ_BossPatternDataSO executionPattern = CreateOrUpdatePattern(
            ExecutionPatternPath,
            "P2_Attack_Execution",
            13,
            HWJ_BossPatternRangeMode.Close,
            false,
            true,
            4.5f,
            "fighter_boss_phase_two");
        Material devLineMaterial = CreateOrLoadDevLineMaterial();

        ConfigureBossPrefab(
            controller,
            idleSprites[0],
            comboPattern,
            chargePattern,
            uppercutPattern,
            groundSlamPattern,
            enhancedComboPattern,
            enhancedChargePattern,
            enhancedUppercutPattern,
            enhancedGroundSlamPattern,
            shockwavePattern,
            aerialDivePattern,
            crossSlashPattern,
            phantomRushPattern,
            executionPattern,
            devLineMaterial);
        ConfigureRootObjectAnimator(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateGeneratedAssets();

        Debug.Log(
            "HWJ Fighter Boss first pass built. "
            + $"Backup={BossBackupPath}, Prefab={BossPrefabPath}, Controller={ControllerPath}, Pattern={PatternPath}");
    }

    private static void BackupBossPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) == null)
        {
            throw new InvalidOperationException($"Boss prefab is missing: {BossPrefabPath}");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BossBackupPath) != null)
        {
            return;
        }

        if (!AssetDatabase.CopyAsset(BossPrefabPath, BossBackupPath))
        {
            throw new InvalidOperationException($"Failed to back up boss prefab: {BossBackupPath}");
        }

        AssetDatabase.ImportAsset(BossBackupPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ConfigureBossSpriteImporters()
    {
        string[] spritePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { BossArtFolder });

        if (spritePaths.Length == 0)
        {
            throw new InvalidOperationException($"No fighter boss sprites were found in {BossArtFolder}.");
        }

        for (int i = 0; i < spritePaths.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(spritePaths[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;

            TextureImporterSettings textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(textureSettings);
            importer.SaveAndReimport();
        }
    }

    private static Sprite[] LoadSpriteSequence(string prefix, int count)
    {
        Sprite[] sprites = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            string path = $"{BossArtFolder}/{prefix}{i:00}.png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprites[i] == null)
            {
                throw new InvalidOperationException($"Required fighter boss sprite is missing or not imported: {path}");
            }
        }

        return sprites;
    }

    private static Sprite[] Slice(Sprite[] source, int startIndex, int count)
    {
        int safeCount = Mathf.Clamp(count, 1, source.Length);
        Sprite[] result = new Sprite[safeCount];

        for (int i = 0; i < safeCount; i++)
        {
            result[i] = source[Mathf.Clamp(startIndex + i, 0, source.Length - 1)];
        }

        return result;
    }

    private static AnimationClip CreateOrUpdateClip(
        string clipName,
        Sprite[] sprites,
        float duration,
        bool loop,
        AnimationEvent[] animationEvents)
    {
        string path = $"{AnimationFolder}/HWJ_FighterBoss_{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = clipName;
        clip.frameRate = 12f;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length + 1];
        float safeDuration = Mathf.Max(0.05f, duration);
        float frameStep = safeDuration / Mathf.Max(1, sprites.Length);

        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameStep,
                value = sprites[i]
            };
        }

        keyframes[keyframes.Length - 1] = new ObjectReferenceKeyframe
        {
            time = safeDuration,
            value = sprites[sprites.Length - 1]
        };

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        AnimationUtility.SetAnimationEvents(clip, animationEvents ?? Array.Empty<AnimationEvent>());

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationEvent[] CreateComboAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.05f, "TelegraphStart"),
            CreateAnimationEvent(0.28f, "EnableHitbox", 1),
            CreateAnimationEvent(0.38f, "DisableHitbox"),
            CreateAnimationEvent(0.55f, "EnableHitbox", 2),
            CreateAnimationEvent(0.66f, "DisableHitbox"),
            CreateAnimationEvent(0.86f, "EnableHitbox", 3),
            CreateAnimationEvent(1.00f, "DisableHitbox"),
            CreateAnimationEvent(1.02f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.72f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateChargeAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.05f, "TelegraphStart"),
            CreateAnimationEvent(0.55f, "ApplyMovement"),
            CreateAnimationEvent(0.56f, "EnableHitbox", 1),
            CreateAnimationEvent(1.07f, "StopMovement"),
            CreateAnimationEvent(1.08f, "DisableHitbox"),
            CreateAnimationEvent(1.10f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.90f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateUppercutAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.05f, "TelegraphStart"),
            CreateAnimationEvent(0.42f, "EnableHitbox", 1),
            CreateAnimationEvent(0.59f, "DisableHitbox"),
            CreateAnimationEvent(0.60f, "CameraShakeHook"),
            CreateAnimationEvent(0.61f, "SFXHook"),
            CreateAnimationEvent(0.64f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.37f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateGroundSlamAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.05f, "TelegraphStart"),
            CreateAnimationEvent(0.60f, "SpawnGroundHazard"),
            CreateAnimationEvent(0.61f, "EnableHitbox", 1),
            CreateAnimationEvent(0.79f, "DisableHitbox"),
            CreateAnimationEvent(0.80f, "CameraShakeHook"),
            CreateAnimationEvent(0.81f, "SFXHook"),
            CreateAnimationEvent(0.84f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.67f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateEnhancedComboAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.22f, "EnableHitbox", 1),
            CreateAnimationEvent(0.30f, "DisableHitbox"),
            CreateAnimationEvent(0.43f, "EnableHitbox", 2),
            CreateAnimationEvent(0.51f, "DisableHitbox"),
            CreateAnimationEvent(0.64f, "EnableHitbox", 3),
            CreateAnimationEvent(0.72f, "DisableHitbox"),
            CreateAnimationEvent(0.85f, "EnableHitbox", 4),
            CreateAnimationEvent(0.97f, "DisableHitbox"),
            CreateAnimationEvent(0.98f, "CameraShakeHook"),
            CreateAnimationEvent(0.99f, "SFXHook"),
            CreateAnimationEvent(1.00f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.52f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateEnhancedChargeAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.36f, "ApplyMovement"),
            CreateAnimationEvent(0.37f, "EnableHitbox", 1),
            CreateAnimationEvent(0.92f, "StopMovement"),
            CreateAnimationEvent(0.93f, "DisableHitbox"),
            CreateAnimationEvent(0.94f, "CameraShakeHook"),
            CreateAnimationEvent(0.95f, "SFXHook"),
            CreateAnimationEvent(0.96f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.57f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateEnhancedUppercutAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.30f, "EnableHitbox", 1),
            CreateAnimationEvent(0.48f, "DisableHitbox"),
            CreateAnimationEvent(0.49f, "CameraShakeHook"),
            CreateAnimationEvent(0.50f, "SFXHook"),
            CreateAnimationEvent(0.52f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.17f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateEnhancedGroundSlamAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.40f, "SpawnGroundHazard"),
            CreateAnimationEvent(0.41f, "EnableHitbox", 1),
            CreateAnimationEvent(0.55f, "DisableHitbox"),
            CreateAnimationEvent(0.68f, "SpawnGroundHazard"),
            CreateAnimationEvent(0.69f, "EnableHitbox", 2),
            CreateAnimationEvent(0.83f, "DisableHitbox"),
            CreateAnimationEvent(0.84f, "CameraShakeHook"),
            CreateAnimationEvent(0.85f, "SFXHook"),
            CreateAnimationEvent(0.86f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.42f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateShockwaveAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.45f, "SpawnProjectile"),
            CreateAnimationEvent(0.46f, "EnableHitbox", 1),
            CreateAnimationEvent(0.66f, "DisableHitbox"),
            CreateAnimationEvent(0.67f, "CameraShakeHook"),
            CreateAnimationEvent(0.68f, "SFXHook"),
            CreateAnimationEvent(0.70f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.47f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateAerialDiveAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.38f, "TeleportOut"),
            CreateAnimationEvent(0.72f, "TeleportIn"),
            CreateAnimationEvent(0.73f, "SpawnGroundHazard"),
            CreateAnimationEvent(0.74f, "EnableHitbox", 1),
            CreateAnimationEvent(0.95f, "DisableHitbox"),
            CreateAnimationEvent(0.96f, "CameraShakeHook"),
            CreateAnimationEvent(0.97f, "SFXHook"),
            CreateAnimationEvent(0.98f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.77f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateCrossSlashAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.32f, "EnableHitbox", 1),
            CreateAnimationEvent(0.44f, "DisableHitbox"),
            CreateAnimationEvent(0.58f, "EnableHitbox", 2),
            CreateAnimationEvent(0.72f, "DisableHitbox"),
            CreateAnimationEvent(0.73f, "CameraShakeHook"),
            CreateAnimationEvent(0.74f, "SFXHook"),
            CreateAnimationEvent(0.75f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.37f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreatePhantomRushAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.28f, "TeleportOut"),
            CreateAnimationEvent(0.34f, "TeleportIn"),
            CreateAnimationEvent(0.35f, "ApplyMovement"),
            CreateAnimationEvent(0.36f, "EnableHitbox", 1),
            CreateAnimationEvent(0.46f, "DisableHitbox"),
            CreateAnimationEvent(0.53f, "EnableHitbox", 2),
            CreateAnimationEvent(0.63f, "DisableHitbox"),
            CreateAnimationEvent(0.70f, "EnableHitbox", 3),
            CreateAnimationEvent(0.80f, "DisableHitbox"),
            CreateAnimationEvent(0.82f, "StopMovement"),
            CreateAnimationEvent(0.83f, "CameraShakeHook"),
            CreateAnimationEvent(0.84f, "SFXHook"),
            CreateAnimationEvent(0.85f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.52f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateExecutionAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "AttackStart"),
            CreateAnimationEvent(0.04f, "TelegraphStart"),
            CreateAnimationEvent(0.78f, "EnableHitbox", 1),
            CreateAnimationEvent(1.02f, "DisableHitbox"),
            CreateAnimationEvent(1.03f, "CameraShakeHook"),
            CreateAnimationEvent(1.04f, "SFXHook"),
            CreateAnimationEvent(1.08f, "AttackRecoveryStart"),
            CreateAnimationEvent(1.87f, "AttackEnd")
        };
    }

    private static AnimationEvent[] CreateDeathAnimationEvents()
    {
        return new[]
        {
            CreateAnimationEvent(0.01f, "DeathStart"),
            CreateAnimationEvent(0.55f, "DeathCameraShakeHook"),
            CreateAnimationEvent(0.56f, "DeathSFXHook"),
            CreateAnimationEvent(1.17f, "DeathEnd")
        };
    }

    private static AnimationEvent CreateAnimationEvent(float time, string functionName, int intParameter = 0)
    {
        return new AnimationEvent
        {
            time = time,
            functionName = functionName,
            intParameter = intParameter
        };
    }

    private static AnimatorController CreateOrUpdateController(params AnimationClip[] clips)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        EnsureAnimatorParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        EnsureAnimatorParameter(controller, "HitTrigger", AnimatorControllerParameterType.Trigger);
        EnsureAnimatorParameter(controller, "DeadTrigger", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            AnimatorState state = EnsureState(stateMachine, clip.name);
            state.motion = clip;
            states[clip.name] = state;
        }

        stateMachine.defaultState = states["Idle"];
        EnsureBoolTransition(states["Idle"], states["Move"], "IsMoving", true);
        EnsureBoolTransition(states["Move"], states["Idle"], "IsMoving", false);
        EnsureTriggerTransition(stateMachine, states["Hurt"], "HitTrigger");
        EnsureTriggerTransition(stateMachine, states["P2_Death"], "DeadTrigger");
        EnsureExitTransition(states["Hurt"], states["Idle"]);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState EnsureState(AnimatorStateMachine stateMachine, string stateName)
    {
        ChildAnimatorState[] childStates = stateMachine.states;

        for (int i = 0; i < childStates.Length; i++)
        {
            if (childStates[i].state != null && childStates[i].state.name == stateName)
            {
                return childStates[i].state;
            }
        }

        return stateMachine.AddState(stateName);
    }

    private static void EnsureAnimatorParameter(
        AnimatorController controller,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
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

    private static void EnsureBoolTransition(
        AnimatorState source,
        AnimatorState destination,
        string parameterName,
        bool expectedValue)
    {
        if (HasTransition(source.transitions, destination, parameterName))
        {
            return;
        }

        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(
            expectedValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
            0f,
            parameterName);
    }

    private static void EnsureTriggerTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        string parameterName)
    {
        if (HasTransition(stateMachine.anyStateTransitions, destination, parameterName))
        {
            return;
        }

        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);
    }

    private static void EnsureExitTransition(AnimatorState source, AnimatorState destination)
    {
        if (HasTransition(source.transitions, destination, null))
        {
            return;
        }

        AnimatorStateTransition transition = source.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.duration = 0.03f;
    }

    private static bool HasTransition(
        AnimatorStateTransition[] transitions,
        AnimatorState destination,
        string parameterName)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            AnimatorStateTransition transition = transitions[i];

            if (transition == null || transition.destinationState != destination)
            {
                continue;
            }

            if (string.IsNullOrEmpty(parameterName))
            {
                return transition.conditions.Length == 0;
            }

            AnimatorCondition[] conditions = transition.conditions;

            for (int conditionIndex = 0; conditionIndex < conditions.Length; conditionIndex++)
            {
                if (conditions[conditionIndex].parameter == parameterName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static HWJ_BossPatternDataSO CreateOrUpdateComboPattern()
    {
        HWJ_BossPatternDataSO pattern = AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(PatternPath);

        if (pattern == null)
        {
            pattern = ScriptableObject.CreateInstance<HWJ_BossPatternDataSO>();
            AssetDatabase.CreateAsset(pattern, PatternPath);
        }

        SerializedObject serializedPattern = new SerializedObject(pattern);
        serializedPattern.FindProperty("patternId").stringValue = "P1_Attack_Combo";
        serializedPattern.FindProperty("patternNumber").intValue = 1;
        serializedPattern.FindProperty("trigger").enumValueIndex = (int)HWJ_BossPatternTrigger.Always;
        serializedPattern.FindProperty("rangeMode").enumValueIndex = (int)HWJ_BossPatternRangeMode.Close;
        serializedPattern.FindProperty("usableInPhase1").boolValue = true;
        serializedPattern.FindProperty("usableInPhase2").boolValue = false;
        serializedPattern.FindProperty("hpRatio").floatValue = 1f;
        serializedPattern.FindProperty("cooldownSeconds").floatValue = 2.8f;
        serializedPattern.FindProperty("defaultCooldownSeconds").floatValue = 2.8f;
        serializedPattern.FindProperty("weight").intValue = 1;
        serializedPattern.FindProperty("animationId").stringValue = "P1_Attack_Combo";
        serializedPattern.FindProperty("useStageOneSpecialExecution").boolValue = false;
        serializedPattern.FindProperty("useCustomPatternExecutor").boolValue = true;
        serializedPattern.FindProperty("customPatternExecutorKey").stringValue = "fighter_boss_combo";
        serializedPattern.FindProperty("skillActions").arraySize = 0;
        serializedPattern.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pattern);
        return pattern;
    }

    private static HWJ_BossPatternDataSO CreateOrUpdateChargePattern()
    {
        HWJ_BossPatternDataSO pattern =
            AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(ChargePatternPath);

        if (pattern == null)
        {
            pattern = ScriptableObject.CreateInstance<HWJ_BossPatternDataSO>();
            AssetDatabase.CreateAsset(pattern, ChargePatternPath);
        }

        SerializedObject serializedPattern = new SerializedObject(pattern);
        serializedPattern.FindProperty("patternId").stringValue = "P1_Attack_Charge";
        serializedPattern.FindProperty("patternNumber").intValue = 2;
        serializedPattern.FindProperty("trigger").enumValueIndex = (int)HWJ_BossPatternTrigger.Always;
        serializedPattern.FindProperty("rangeMode").enumValueIndex = (int)HWJ_BossPatternRangeMode.Far;
        serializedPattern.FindProperty("usableInPhase1").boolValue = true;
        serializedPattern.FindProperty("usableInPhase2").boolValue = false;
        serializedPattern.FindProperty("hpRatio").floatValue = 1f;
        serializedPattern.FindProperty("cooldownSeconds").floatValue = 3.2f;
        serializedPattern.FindProperty("defaultCooldownSeconds").floatValue = 3.2f;
        serializedPattern.FindProperty("weight").intValue = 1;
        serializedPattern.FindProperty("animationId").stringValue = "P1_Attack_Charge";
        serializedPattern.FindProperty("useStageOneSpecialExecution").boolValue = false;
        serializedPattern.FindProperty("useCustomPatternExecutor").boolValue = true;
        serializedPattern.FindProperty("customPatternExecutorKey").stringValue = "fighter_boss_charge";
        serializedPattern.FindProperty("skillActions").arraySize = 0;
        serializedPattern.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pattern);
        return pattern;
    }

    private static HWJ_BossPatternDataSO CreateOrUpdateUppercutPattern()
    {
        return CreateOrUpdatePattern(
            UppercutPatternPath,
            "P1_Attack_Uppercut",
            3,
            HWJ_BossPatternRangeMode.Close,
            true,
            false,
            3.1f,
            "fighter_boss_uppercut");
    }

    private static HWJ_BossPatternDataSO CreateOrUpdateGroundSlamPattern()
    {
        return CreateOrUpdatePattern(
            GroundSlamPatternPath,
            "P1_Attack_GroundSlam",
            4,
            HWJ_BossPatternRangeMode.Close,
            true,
            false,
            3.8f,
            "fighter_boss_ground_slam");
    }

    private static HWJ_BossPatternDataSO CreateOrUpdatePattern(
        string assetPath,
        string patternId,
        int patternNumber,
        HWJ_BossPatternRangeMode rangeMode,
        bool usableInPhase1,
        bool usableInPhase2,
        float cooldownSeconds,
        string executorKey)
    {
        HWJ_BossPatternDataSO pattern =
            AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(assetPath);

        if (pattern == null)
        {
            pattern = ScriptableObject.CreateInstance<HWJ_BossPatternDataSO>();
            AssetDatabase.CreateAsset(pattern, assetPath);
        }

        SerializedObject serializedPattern = new SerializedObject(pattern);
        serializedPattern.FindProperty("patternId").stringValue = patternId;
        serializedPattern.FindProperty("patternNumber").intValue = patternNumber;
        serializedPattern.FindProperty("trigger").enumValueIndex = (int)HWJ_BossPatternTrigger.Always;
        serializedPattern.FindProperty("rangeMode").enumValueIndex = (int)rangeMode;
        serializedPattern.FindProperty("usableInPhase1").boolValue = usableInPhase1;
        serializedPattern.FindProperty("usableInPhase2").boolValue = usableInPhase2;
        serializedPattern.FindProperty("hpRatio").floatValue = 1f;
        serializedPattern.FindProperty("cooldownSeconds").floatValue = cooldownSeconds;
        serializedPattern.FindProperty("defaultCooldownSeconds").floatValue = cooldownSeconds;
        serializedPattern.FindProperty("weight").intValue = 1;
        serializedPattern.FindProperty("animationId").stringValue = patternId;
        serializedPattern.FindProperty("useStageOneSpecialExecution").boolValue = false;
        serializedPattern.FindProperty("useCustomPatternExecutor").boolValue = true;
        serializedPattern.FindProperty("customPatternExecutorKey").stringValue = executorKey;
        serializedPattern.FindProperty("skillActions").arraySize = 0;
        serializedPattern.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pattern);
        return pattern;
    }

    private static Material CreateOrLoadDevLineMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(TelegraphMaterialPath);

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        }

        if (shader == null)
        {
            throw new InvalidOperationException("A sprite-compatible shader is required for boss debug lines.");
        }

        material = new Material(shader)
        {
            name = "HWJ_FighterBoss_DevLine"
        };
        AssetDatabase.CreateAsset(material, TelegraphMaterialPath);
        return material;
    }

    private static void ConfigureBossPrefab(
        AnimatorController controller,
        Sprite idleSprite,
        HWJ_BossPatternDataSO comboPattern,
        HWJ_BossPatternDataSO chargePattern,
        HWJ_BossPatternDataSO uppercutPattern,
        HWJ_BossPatternDataSO groundSlamPattern,
        HWJ_BossPatternDataSO enhancedComboPattern,
        HWJ_BossPatternDataSO enhancedChargePattern,
        HWJ_BossPatternDataSO enhancedUppercutPattern,
        HWJ_BossPatternDataSO enhancedGroundSlamPattern,
        HWJ_BossPatternDataSO shockwavePattern,
        HWJ_BossPatternDataSO aerialDivePattern,
        HWJ_BossPatternDataSO crossSlashPattern,
        HWJ_BossPatternDataSO phantomRushPattern,
        HWJ_BossPatternDataSO executionPattern,
        Material lineMaterial)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);

        try
        {
            HWJ_RootObjectDataResolver resolver = GetOrAdd<HWJ_RootObjectDataResolver>(root);
            HWJ_RuntimeStatusSystem status = GetOrAdd<HWJ_RuntimeStatusSystem>(root);
            HWJ_CombatSystem combat = GetOrAdd<HWJ_CombatSystem>(root);
            HWJ_BossPatternSystem patternSystem = GetOrAdd<HWJ_BossPatternSystem>(root);
            HWJ_BossBrainSystem brain = GetOrAdd<HWJ_BossBrainSystem>(root);
            // 씬에 같은 보스 프리팹이 겹쳐 배치되어도 전투와 UI가 이중 실행되지 않게 합니다.
            GetOrAdd<HWJ_BossDuplicateGuardSystem>(root);
            HWJ_CharacterMotionSystem motion = GetOrAdd<HWJ_CharacterMotionSystem>(root);
            HWJ_FighterBossComboSystem combo = GetOrAdd<HWJ_FighterBossComboSystem>(root);
            HWJ_FighterBossChargeSystem charge = GetOrAdd<HWJ_FighterBossChargeSystem>(root);
            HWJ_FighterBossUppercutSystem uppercut = GetOrAdd<HWJ_FighterBossUppercutSystem>(root);
            HWJ_FighterBossGroundSlamSystem groundSlam =
                GetOrAdd<HWJ_FighterBossGroundSlamSystem>(root);
            HWJ_FighterBossPhaseTwoPatternSystem phaseTwoPatterns =
                GetOrAdd<HWJ_FighterBossPhaseTwoPatternSystem>(root);
            HWJ_FighterBossDeathSystem deathSystem =
                GetOrAdd<HWJ_FighterBossDeathSystem>(root);
            HWJ_FighterBossAnimationEvents animationEvents = GetOrAdd<HWJ_FighterBossAnimationEvents>(root);
            Animator animator = GetOrAdd<Animator>(root);
            SpriteRenderer spriteRenderer = GetOrAdd<SpriteRenderer>(root);
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(root);
            Collider2D bodyCollider = root.GetComponent<Collider2D>();

            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 10;

            Transform attackRoot = GetOrCreateChild(root.transform, "AttackRoot");
            Transform hitboxTransform = GetOrCreateChild(attackRoot, "ComboHitbox");
            hitboxTransform.localPosition = new Vector3(1.05f, 0.95f, 0f);
            hitboxTransform.localRotation = Quaternion.identity;
            hitboxTransform.localScale = Vector3.one;
            BoxCollider2D hitboxCollider = GetOrAdd<BoxCollider2D>(hitboxTransform.gameObject);
            hitboxCollider.isTrigger = true;
            hitboxCollider.size = new Vector2(1.25f, 0.9f);
            hitboxCollider.offset = Vector2.zero;
            hitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem hitbox = GetOrAdd<HWJ_FighterBossHitboxSystem>(
                hitboxTransform.gameObject);

            Transform chargeHitboxTransform = GetOrCreateChild(attackRoot, "ChargeShoulderHitbox");
            chargeHitboxTransform.localPosition = new Vector3(0.92f, 1.02f, 0f);
            chargeHitboxTransform.localRotation = Quaternion.identity;
            chargeHitboxTransform.localScale = Vector3.one;
            BoxCollider2D chargeHitboxCollider = GetOrAdd<BoxCollider2D>(
                chargeHitboxTransform.gameObject);
            chargeHitboxCollider.isTrigger = true;
            chargeHitboxCollider.size = new Vector2(1.1f, 1.25f);
            chargeHitboxCollider.offset = Vector2.zero;
            chargeHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem chargeHitbox = GetOrAdd<HWJ_FighterBossHitboxSystem>(
                chargeHitboxTransform.gameObject);

            Transform uppercutHitboxTransform = GetOrCreateChild(attackRoot, "UppercutHitbox");
            uppercutHitboxTransform.localPosition = new Vector3(0.78f, 1.55f, 0f);
            uppercutHitboxTransform.localRotation = Quaternion.identity;
            uppercutHitboxTransform.localScale = Vector3.one;
            BoxCollider2D uppercutHitboxCollider = GetOrAdd<BoxCollider2D>(
                uppercutHitboxTransform.gameObject);
            uppercutHitboxCollider.isTrigger = true;
            uppercutHitboxCollider.size = new Vector2(1.4f, 2.3f);
            uppercutHitboxCollider.offset = Vector2.zero;
            uppercutHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem uppercutHitbox = GetOrAdd<HWJ_FighterBossHitboxSystem>(
                uppercutHitboxTransform.gameObject);

            Transform groundSlamHitboxTransform = GetOrCreateChild(attackRoot, "GroundSlamHitbox");
            groundSlamHitboxTransform.localPosition = new Vector3(0f, 0.45f, 0f);
            groundSlamHitboxTransform.localRotation = Quaternion.identity;
            groundSlamHitboxTransform.localScale = Vector3.one;
            BoxCollider2D groundSlamHitboxCollider = GetOrAdd<BoxCollider2D>(
                groundSlamHitboxTransform.gameObject);
            groundSlamHitboxCollider.isTrigger = true;
            groundSlamHitboxCollider.size = new Vector2(5.5f, 1.4f);
            groundSlamHitboxCollider.offset = Vector2.zero;
            groundSlamHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem groundSlamHitbox = GetOrAdd<HWJ_FighterBossHitboxSystem>(
                groundSlamHitboxTransform.gameObject);

            Transform enhancedComboHitboxTransform =
                GetOrCreateChild(attackRoot, "P2EnhancedComboHitbox");
            enhancedComboHitboxTransform.localPosition = new Vector3(1.15f, 0.95f, 0f);
            enhancedComboHitboxTransform.localRotation = Quaternion.identity;
            enhancedComboHitboxTransform.localScale = Vector3.one;
            BoxCollider2D enhancedComboHitboxCollider =
                GetOrAdd<BoxCollider2D>(enhancedComboHitboxTransform.gameObject);
            enhancedComboHitboxCollider.isTrigger = true;
            enhancedComboHitboxCollider.size = new Vector2(1.5f, 1f);
            enhancedComboHitboxCollider.offset = Vector2.zero;
            enhancedComboHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem enhancedComboHitbox =
                GetOrAdd<HWJ_FighterBossHitboxSystem>(enhancedComboHitboxTransform.gameObject);

            Transform enhancedChargeHitboxTransform =
                GetOrCreateChild(attackRoot, "P2EnhancedChargeHitbox");
            enhancedChargeHitboxTransform.localPosition = new Vector3(1f, 1f, 0f);
            enhancedChargeHitboxTransform.localRotation = Quaternion.identity;
            enhancedChargeHitboxTransform.localScale = Vector3.one;
            BoxCollider2D enhancedChargeHitboxCollider =
                GetOrAdd<BoxCollider2D>(enhancedChargeHitboxTransform.gameObject);
            enhancedChargeHitboxCollider.isTrigger = true;
            enhancedChargeHitboxCollider.size = new Vector2(1.3f, 1.4f);
            enhancedChargeHitboxCollider.offset = Vector2.zero;
            enhancedChargeHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem enhancedChargeHitbox =
                GetOrAdd<HWJ_FighterBossHitboxSystem>(enhancedChargeHitboxTransform.gameObject);

            Transform enhancedUppercutHitboxTransform =
                GetOrCreateChild(attackRoot, "P2EnhancedUppercutHitbox");
            enhancedUppercutHitboxTransform.localPosition = new Vector3(0.82f, 1.65f, 0f);
            enhancedUppercutHitboxTransform.localRotation = Quaternion.identity;
            enhancedUppercutHitboxTransform.localScale = Vector3.one;
            BoxCollider2D enhancedUppercutHitboxCollider =
                GetOrAdd<BoxCollider2D>(enhancedUppercutHitboxTransform.gameObject);
            enhancedUppercutHitboxCollider.isTrigger = true;
            enhancedUppercutHitboxCollider.size = new Vector2(1.6f, 2.6f);
            enhancedUppercutHitboxCollider.offset = Vector2.zero;
            enhancedUppercutHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem enhancedUppercutHitbox =
                GetOrAdd<HWJ_FighterBossHitboxSystem>(enhancedUppercutHitboxTransform.gameObject);

            Transform enhancedGroundSlamHitboxTransform =
                GetOrCreateChild(attackRoot, "P2EnhancedGroundSlamHitbox");
            enhancedGroundSlamHitboxTransform.localPosition = new Vector3(0f, 0.45f, 0f);
            enhancedGroundSlamHitboxTransform.localRotation = Quaternion.identity;
            enhancedGroundSlamHitboxTransform.localScale = Vector3.one;
            BoxCollider2D enhancedGroundSlamHitboxCollider =
                GetOrAdd<BoxCollider2D>(enhancedGroundSlamHitboxTransform.gameObject);
            enhancedGroundSlamHitboxCollider.isTrigger = true;
            enhancedGroundSlamHitboxCollider.size = new Vector2(6.5f, 1.6f);
            enhancedGroundSlamHitboxCollider.offset = Vector2.zero;
            enhancedGroundSlamHitboxCollider.enabled = false;
            HWJ_FighterBossHitboxSystem enhancedGroundSlamHitbox =
                GetOrAdd<HWJ_FighterBossHitboxSystem>(enhancedGroundSlamHitboxTransform.gameObject);

            CreatePatternHitbox(
                attackRoot,
                "P2ShockwaveHitbox",
                new Vector3(2.8f, 0.8f, 0f),
                new Vector2(5.6f, 1.2f),
                out Transform shockwaveHitboxTransform,
                out BoxCollider2D shockwaveHitboxCollider,
                out HWJ_FighterBossHitboxSystem shockwaveHitbox);
            CreatePatternHitbox(
                attackRoot,
                "P2AerialDiveHitbox",
                new Vector3(0f, 0.45f, 0f),
                new Vector2(4.5f, 1.5f),
                out Transform aerialDiveHitboxTransform,
                out BoxCollider2D aerialDiveHitboxCollider,
                out HWJ_FighterBossHitboxSystem aerialDiveHitbox);
            CreatePatternHitbox(
                attackRoot,
                "P2CrossSlashHitbox",
                new Vector3(1.1f, 1f, 0f),
                new Vector2(2f, 2f),
                out Transform crossSlashHitboxTransform,
                out BoxCollider2D crossSlashHitboxCollider,
                out HWJ_FighterBossHitboxSystem crossSlashHitbox);
            CreatePatternHitbox(
                attackRoot,
                "P2PhantomRushHitbox",
                new Vector3(1.2f, 1f, 0f),
                new Vector2(6.5f, 1.4f),
                out Transform phantomRushHitboxTransform,
                out BoxCollider2D phantomRushHitboxCollider,
                out HWJ_FighterBossHitboxSystem phantomRushHitbox);
            CreatePatternHitbox(
                attackRoot,
                "P2ExecutionHitbox",
                new Vector3(1f, 1f, 0f),
                new Vector2(1.8f, 2f),
                out Transform executionHitboxTransform,
                out BoxCollider2D executionHitboxCollider,
                out HWJ_FighterBossHitboxSystem executionHitbox);

            Transform telegraphTransform = GetOrCreateChild(root.transform, "TEMP_TELEGRAPH_P1_Combo");
            LineRenderer telegraph = GetOrAdd<LineRenderer>(telegraphTransform.gameObject);
            ConfigureLineRenderer(telegraph, lineMaterial, 0.08f, 220);
            telegraph.startColor = new Color(1f, 0.12f, 0.05f, 0.9f);
            telegraph.endColor = new Color(1f, 0.55f, 0.1f, 0.45f);
            telegraph.positionCount = 2;
            telegraph.SetPosition(0, new Vector3(0.15f, 0.95f, 0f));
            telegraph.SetPosition(1, new Vector3(2.1f, 0.95f, 0f));
            telegraph.enabled = false;

            Transform chargeTelegraphTransform =
                GetOrCreateChild(root.transform, "TEMP_TELEGRAPH_P1_Charge");
            LineRenderer chargeTelegraph = GetOrAdd<LineRenderer>(chargeTelegraphTransform.gameObject);
            ConfigureLineRenderer(chargeTelegraph, lineMaterial, 0.12f, 221);
            chargeTelegraph.startColor = new Color(1f, 0.08f, 0.02f, 0.95f);
            chargeTelegraph.endColor = new Color(1f, 0.65f, 0.05f, 0.35f);
            chargeTelegraph.positionCount = 2;
            chargeTelegraph.SetPosition(0, new Vector3(0.2f, 0.25f, 0f));
            chargeTelegraph.SetPosition(1, new Vector3(12f, 0.25f, 0f));
            chargeTelegraph.enabled = false;

            Transform uppercutTelegraphTransform =
                GetOrCreateChild(root.transform, "TEMP_TELEGRAPH_P1_Uppercut");
            LineRenderer uppercutTelegraph =
                GetOrAdd<LineRenderer>(uppercutTelegraphTransform.gameObject);
            ConfigureLineRenderer(uppercutTelegraph, lineMaterial, 0.1f, 222);
            uppercutTelegraph.startColor = new Color(1f, 0.82f, 0.05f, 0.95f);
            uppercutTelegraph.endColor = new Color(1f, 0.15f, 0.03f, 0.5f);
            uppercutTelegraph.positionCount = 3;
            uppercutTelegraph.SetPosition(0, new Vector3(0.2f, 0.15f, 0f));
            uppercutTelegraph.SetPosition(1, new Vector3(0.85f, 1.25f, 0f));
            uppercutTelegraph.SetPosition(2, new Vector3(0.55f, 2.75f, 0f));
            uppercutTelegraph.enabled = false;

            Transform groundSlamTelegraphTransform =
                GetOrCreateChild(root.transform, "TEMP_TELEGRAPH_P1_GroundSlam");
            LineRenderer groundSlamTelegraph =
                GetOrAdd<LineRenderer>(groundSlamTelegraphTransform.gameObject);
            ConfigureLineRenderer(groundSlamTelegraph, lineMaterial, 0.11f, 223);
            groundSlamTelegraph.startColor = new Color(1f, 0.05f, 0.02f, 0.95f);
            groundSlamTelegraph.endColor = new Color(1f, 0.55f, 0.02f, 0.65f);
            groundSlamTelegraph.loop = true;
            groundSlamTelegraph.positionCount = 32;
            groundSlamTelegraph.enabled = false;

            LineRenderer enhancedComboTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_EnhancedCombo",
                lineMaterial,
                new Color(0.1f, 0.8f, 1f, 0.95f),
                new Color(0.75f, 0.2f, 1f, 0.55f),
                0.09f,
                224);
            LineRenderer enhancedChargeTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_EnhancedCharge",
                lineMaterial,
                new Color(0.05f, 0.9f, 1f, 0.95f),
                new Color(0.7f, 0.1f, 1f, 0.45f),
                0.13f,
                225);
            LineRenderer enhancedUppercutTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_EnhancedUppercut",
                lineMaterial,
                new Color(0.2f, 1f, 0.85f, 0.95f),
                new Color(0.85f, 0.15f, 1f, 0.5f),
                0.11f,
                226);
            LineRenderer enhancedGroundSlamTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_EnhancedGroundSlam",
                lineMaterial,
                new Color(0.05f, 0.75f, 1f, 0.95f),
                new Color(0.85f, 0.1f, 1f, 0.65f),
                0.12f,
                227);
            LineRenderer shockwaveTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_Shockwave",
                lineMaterial,
                new Color(0.15f, 0.95f, 1f, 0.95f),
                new Color(0.1f, 0.35f, 1f, 0.45f),
                0.1f,
                228);
            LineRenderer aerialDiveTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_AerialDive",
                lineMaterial,
                new Color(1f, 0.15f, 0.75f, 0.95f),
                new Color(0.3f, 0.8f, 1f, 0.55f),
                0.12f,
                229);
            LineRenderer crossSlashTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_CrossSlash",
                lineMaterial,
                new Color(0.95f, 0.2f, 1f, 0.95f),
                new Color(0.1f, 0.9f, 1f, 0.5f),
                0.1f,
                230);
            LineRenderer phantomRushTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_PhantomRush",
                lineMaterial,
                new Color(0.65f, 0.1f, 1f, 0.95f),
                new Color(0.05f, 0.95f, 1f, 0.45f),
                0.13f,
                231);
            LineRenderer executionTelegraph = CreatePatternTelegraph(
                root.transform,
                "TEMP_TELEGRAPH_P2_Execution",
                lineMaterial,
                new Color(1f, 0.05f, 0.25f, 0.98f),
                new Color(0.65f, 0.05f, 1f, 0.6f),
                0.14f,
                232);

            HWJ_FighterBossHealthBarSystem healthBar = ConfigureBossHealthBar(
                root,
                resolver,
                status,
                brain,
                lineMaterial);

            SetObjectReference(hitbox, "ownerCombat", combat);
            SetObjectReference(hitbox, "hitboxCollider", hitboxCollider);
            SetObjectReference(chargeHitbox, "ownerCombat", combat);
            SetObjectReference(chargeHitbox, "hitboxCollider", chargeHitboxCollider);
            SetObjectReference(uppercutHitbox, "ownerCombat", combat);
            SetObjectReference(uppercutHitbox, "hitboxCollider", uppercutHitboxCollider);
            SetObjectReference(groundSlamHitbox, "ownerCombat", combat);
            SetObjectReference(groundSlamHitbox, "hitboxCollider", groundSlamHitboxCollider);
            SetObjectReference(enhancedComboHitbox, "ownerCombat", combat);
            SetObjectReference(enhancedComboHitbox, "hitboxCollider", enhancedComboHitboxCollider);
            SetObjectReference(enhancedChargeHitbox, "ownerCombat", combat);
            SetObjectReference(enhancedChargeHitbox, "hitboxCollider", enhancedChargeHitboxCollider);
            SetObjectReference(enhancedUppercutHitbox, "ownerCombat", combat);
            SetObjectReference(
                enhancedUppercutHitbox,
                "hitboxCollider",
                enhancedUppercutHitboxCollider);
            SetObjectReference(enhancedGroundSlamHitbox, "ownerCombat", combat);
            SetObjectReference(
                enhancedGroundSlamHitbox,
                "hitboxCollider",
                enhancedGroundSlamHitboxCollider);
            SetObjectReference(shockwaveHitbox, "ownerCombat", combat);
            SetObjectReference(shockwaveHitbox, "hitboxCollider", shockwaveHitboxCollider);
            SetObjectReference(aerialDiveHitbox, "ownerCombat", combat);
            SetObjectReference(aerialDiveHitbox, "hitboxCollider", aerialDiveHitboxCollider);
            SetObjectReference(crossSlashHitbox, "ownerCombat", combat);
            SetObjectReference(crossSlashHitbox, "hitboxCollider", crossSlashHitboxCollider);
            SetObjectReference(phantomRushHitbox, "ownerCombat", combat);
            SetObjectReference(phantomRushHitbox, "hitboxCollider", phantomRushHitboxCollider);
            SetObjectReference(executionHitbox, "ownerCombat", combat);
            SetObjectReference(executionHitbox, "hitboxCollider", executionHitboxCollider);

            SetObjectReference(combo, "bossBrain", brain);
            SetObjectReference(combo, "runtimeStatus", status);
            SetObjectReference(combo, "comboHitbox", hitbox);
            SetObjectReference(combo, "animator", animator);
            SetObjectReference(combo, "facingRenderer", spriteRenderer);
            SetObjectReference(combo, "telegraphRenderer", telegraph);
            SetObjectReference(combo, "attackRoot", hitboxTransform);

            SetObjectReference(charge, "bossBrain", brain);
            SetObjectReference(charge, "runtimeStatus", status);
            SetObjectReference(charge, "chargeHitbox", chargeHitbox);
            SetObjectReference(charge, "animator", animator);
            SetObjectReference(charge, "facingRenderer", spriteRenderer);
            SetObjectReference(charge, "body", body);
            SetObjectReference(charge, "bodyCollider", bodyCollider);
            SetObjectReference(charge, "shoulderRoot", chargeHitboxTransform);
            SetObjectReference(charge, "telegraphRenderer", chargeTelegraph);

            SetObjectReference(uppercut, "bossBrain", brain);
            SetObjectReference(uppercut, "runtimeStatus", status);
            SetObjectReference(uppercut, "uppercutHitbox", uppercutHitbox);
            SetObjectReference(uppercut, "animator", animator);
            SetObjectReference(uppercut, "facingRenderer", spriteRenderer);
            SetObjectReference(uppercut, "uppercutRoot", uppercutHitboxTransform);
            SetObjectReference(uppercut, "telegraphRenderer", uppercutTelegraph);

            SetObjectReference(groundSlam, "bossBrain", brain);
            SetObjectReference(groundSlam, "runtimeStatus", status);
            SetObjectReference(groundSlam, "slamHitbox", groundSlamHitbox);
            SetObjectReference(groundSlam, "animator", animator);
            SetObjectReference(groundSlam, "telegraphRenderer", groundSlamTelegraph);

            SetObjectReference(phaseTwoPatterns, "bossBrain", brain);
            SetObjectReference(phaseTwoPatterns, "runtimeStatus", status);
            SetObjectReference(phaseTwoPatterns, "animator", animator);
            SetObjectReference(phaseTwoPatterns, "facingRenderer", spriteRenderer);
            SetObjectReference(phaseTwoPatterns, "body", body);
            SetObjectReference(phaseTwoPatterns, "bodyCollider", bodyCollider);
            SetObjectReference(deathSystem, "animator", animator);
            SetObjectReference(deathSystem, "body", body);
            SetObjectReference(deathSystem, "bodyCollider", bodyCollider);

            SerializedObject serializedPhaseTwoPatterns = new SerializedObject(phaseTwoPatterns);
            SerializedProperty phaseTwoProfiles = serializedPhaseTwoPatterns.FindProperty("profiles");
            phaseTwoProfiles.arraySize = 9;
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(0),
                "P2_Enhanced_Combo",
                HWJ_FighterBossPhaseTwoPatternKind.EnhancedCombo,
                enhancedComboHitbox,
                enhancedComboHitboxTransform,
                enhancedComboTelegraph,
                1.15f,
                0.95f,
                1.35f,
                7f,
                2f,
                0f,
                0f,
                0.45f,
                2.5f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(1),
                "P2_Enhanced_Charge",
                HWJ_FighterBossPhaseTwoPatternKind.DoubleCharge,
                enhancedChargeHitbox,
                enhancedChargeHitboxTransform,
                enhancedChargeTelegraph,
                1f,
                1f,
                1.65f,
                19f,
                2.05f,
                42f,
                0.55f,
                0.55f,
                14f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(2),
                "P2_Enhanced_Uppercut",
                HWJ_FighterBossPhaseTwoPatternKind.DoubleCharge,
                enhancedUppercutHitbox,
                enhancedUppercutHitboxTransform,
                enhancedUppercutTelegraph,
                0.82f,
                1.65f,
                1.95f,
                17f,
                1.65f,
                0f,
                0f,
                0.45f,
                3.1f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(3),
                "P2_Enhanced_GroundSlam",
                HWJ_FighterBossPhaseTwoPatternKind.DoubleCharge,
                enhancedGroundSlamHitbox,
                enhancedGroundSlamHitboxTransform,
                enhancedGroundSlamTelegraph,
                0f,
                0.45f,
                1.75f,
                20f,
                1.9f,
                0f,
                0f,
                0.45f,
                3.25f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(4),
                "P2_Attack_Shockwave",
                HWJ_FighterBossPhaseTwoPatternKind.DarkGroundSlam,
                shockwaveHitbox,
                shockwaveHitboxTransform,
                shockwaveTelegraph,
                2.8f,
                0.8f,
                1.7f,
                14f,
                2f,
                0f,
                0f,
                0.45f,
                7f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(5),
                "P2_Attack_AerialDive",
                HWJ_FighterBossPhaseTwoPatternKind.ThunderUppercut,
                aerialDiveHitbox,
                aerialDiveHitboxTransform,
                aerialDiveTelegraph,
                0f,
                0.45f,
                2.1f,
                20f,
                2.25f,
                0f,
                0f,
                0.45f,
                2.4f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(6),
                "P2_Attack_CrossSlash",
                HWJ_FighterBossPhaseTwoPatternKind.DarkWave,
                crossSlashHitbox,
                crossSlashHitboxTransform,
                crossSlashTelegraph,
                1.1f,
                1f,
                1.55f,
                8f,
                1.9f,
                0f,
                0f,
                0.45f,
                2.2f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(7),
                "P2_Attack_PhantomRush",
                HWJ_FighterBossPhaseTwoPatternKind.ShadowCombo,
                phantomRushHitbox,
                phantomRushHitboxTransform,
                phantomRushTelegraph,
                1.2f,
                1f,
                1.45f,
                12f,
                2f,
                6f,
                0.45f,
                0.3f,
                8f);
            ConfigurePhaseTwoProfile(
                phaseTwoProfiles.GetArrayElementAtIndex(8),
                "P2_Attack_Execution",
                HWJ_FighterBossPhaseTwoPatternKind.Ultimate,
                executionHitbox,
                executionHitboxTransform,
                executionTelegraph,
                1f,
                1f,
                2.6f,
                24f,
                2.4f,
                0f,
                0f,
                0.45f,
                1.8f);
            serializedPhaseTwoPatterns.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(phaseTwoPatterns);

            SetObjectReference(animationEvents, "comboSystem", combo);
            SetObjectReference(brain, "fighterComboSystem", combo);
            SetObjectReference(brain, "fighterDeathSystem", deathSystem);
            SetObjectReference(brain, "animator", animator);
            SetBool(brain, "useTwoBarPhaseHealth", true);
            SetEnum(brain, "fighterPhase", (int)HWJ_FighterBossPhase.Phase1);
            SetObjectReference(motion, "animator", animator);
            SetObjectReference(motion, "spriteRenderer", spriteRenderer);

            SerializedObject serializedPatternSystem = new SerializedObject(patternSystem);
            SerializedProperty patterns = serializedPatternSystem.FindProperty("patterns");
            patterns.arraySize = 13;
            patterns.GetArrayElementAtIndex(0).objectReferenceValue = comboPattern;
            patterns.GetArrayElementAtIndex(1).objectReferenceValue = chargePattern;
            patterns.GetArrayElementAtIndex(2).objectReferenceValue = uppercutPattern;
            patterns.GetArrayElementAtIndex(3).objectReferenceValue = groundSlamPattern;
            patterns.GetArrayElementAtIndex(4).objectReferenceValue = enhancedComboPattern;
            patterns.GetArrayElementAtIndex(5).objectReferenceValue = enhancedChargePattern;
            patterns.GetArrayElementAtIndex(6).objectReferenceValue = enhancedUppercutPattern;
            patterns.GetArrayElementAtIndex(7).objectReferenceValue = enhancedGroundSlamPattern;
            patterns.GetArrayElementAtIndex(8).objectReferenceValue = shockwavePattern;
            patterns.GetArrayElementAtIndex(9).objectReferenceValue = aerialDivePattern;
            patterns.GetArrayElementAtIndex(10).objectReferenceValue = crossSlashPattern;
            patterns.GetArrayElementAtIndex(11).objectReferenceValue = phantomRushPattern;
            patterns.GetArrayElementAtIndex(12).objectReferenceValue = executionPattern;
            SerializedProperty executors = serializedPatternSystem.FindProperty("specialPatternExecutors");
            executors.arraySize = 5;
            executors.GetArrayElementAtIndex(0).objectReferenceValue = combo;
            executors.GetArrayElementAtIndex(1).objectReferenceValue = charge;
            executors.GetArrayElementAtIndex(2).objectReferenceValue = uppercut;
            executors.GetArrayElementAtIndex(3).objectReferenceValue = groundSlam;
            executors.GetArrayElementAtIndex(4).objectReferenceValue = phaseTwoPatterns;
            serializedPatternSystem.FindProperty("stageOnePatternSystem").objectReferenceValue = null;
            serializedPatternSystem.FindProperty("autoUsePatterns").boolValue = false;
            serializedPatternSystem.FindProperty("preventSamePatternRepeat").boolValue = false;
            serializedPatternSystem.ApplyModifiedPropertiesWithoutUndo();

            HWJ_MidBossPatternSystem oldPatternSystem = root.GetComponent<HWJ_MidBossPatternSystem>();

            if (oldPatternSystem != null)
            {
                oldPatternSystem.CancelActivePattern();
                oldPatternSystem.enabled = false;
                EditorUtility.SetDirty(oldPatternSystem);
            }

            SetObjectReference(status, "bossBrain", brain);
            SetObjectReference(brain, "dataResolver", resolver);
            SetObjectReference(brain, "runtimeStatus", status);
            SetObjectReference(brain, "patternSystem", patternSystem);
            SetObjectReference(brain, "motionSystem", motion);
            SetObjectReference(brain, "body", body);

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(spriteRenderer);
            EditorUtility.SetDirty(healthBar);
            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static HWJ_FighterBossHealthBarSystem ConfigureBossHealthBar(
        GameObject bossRoot,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RuntimeStatusSystem status,
        HWJ_BossBrainSystem brain,
        Material lineMaterial)
    {
        Transform barRoot = GetOrCreateChild(bossRoot.transform, "DEV_PLACEHOLDER_BossHealthBar");
        barRoot.localPosition = new Vector3(0f, 2.45f, 0f);
        barRoot.localRotation = Quaternion.identity;
        barRoot.localScale = Vector3.one;

        Transform backgroundTransform = GetOrCreateChild(barRoot, "Background");
        LineRenderer background = GetOrAdd<LineRenderer>(backgroundTransform.gameObject);
        ConfigureLineRenderer(background, lineMaterial, 0.18f, 230);
        background.startColor = new Color(0.05f, 0.05f, 0.07f, 0.95f);
        background.endColor = background.startColor;
        background.positionCount = 2;
        background.SetPosition(0, new Vector3(-2f, 0f, 0f));
        background.SetPosition(1, new Vector3(2f, 0f, 0f));

        Transform fillTransform = GetOrCreateChild(barRoot, "Fill");
        LineRenderer fill = GetOrAdd<LineRenderer>(fillTransform.gameObject);
        ConfigureLineRenderer(fill, lineMaterial, 0.12f, 231);
        fill.positionCount = 2;
        fill.SetPosition(0, new Vector3(-2f, 0f, 0f));
        fill.SetPosition(1, new Vector3(2f, 0f, 0f));

        TextMesh bossName = ConfigureTextMesh(
            GetOrCreateChild(barRoot, "BossName").gameObject,
            new Vector3(0f, 0.32f, 0f),
            0.12f,
            TextAnchor.MiddleCenter);
        TextMesh phaseLabel = ConfigureTextMesh(
            GetOrCreateChild(barRoot, "PhaseLabel").gameObject,
            new Vector3(0f, -0.3f, 0f),
            0.1f,
            TextAnchor.MiddleCenter);

        HWJ_FighterBossHealthBarSystem healthBar = GetOrAdd<HWJ_FighterBossHealthBarSystem>(
            barRoot.gameObject);
        SetObjectReference(healthBar, "runtimeStatus", status);
        SetObjectReference(healthBar, "bossBrain", brain);
        SetObjectReference(healthBar, "dataResolver", resolver);
        SetObjectReference(healthBar, "backgroundRenderer", background);
        SetObjectReference(healthBar, "fillRenderer", fill);
        SetObjectReference(healthBar, "bossNameText", bossName);
        SetObjectReference(healthBar, "phaseText", phaseLabel);
        SetFloat(healthBar, "barWidth", 4f);
        return healthBar;
    }

    private static void CreatePatternHitbox(
        Transform parent,
        string childName,
        Vector3 localPosition,
        Vector2 size,
        out Transform hitboxTransform,
        out BoxCollider2D hitboxCollider,
        out HWJ_FighterBossHitboxSystem hitbox)
    {
        hitboxTransform = GetOrCreateChild(parent, childName);
        hitboxTransform.localPosition = localPosition;
        hitboxTransform.localRotation = Quaternion.identity;
        hitboxTransform.localScale = Vector3.one;
        hitboxCollider = GetOrAdd<BoxCollider2D>(hitboxTransform.gameObject);
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = size;
        hitboxCollider.offset = Vector2.zero;
        hitboxCollider.enabled = false;
        hitbox = GetOrAdd<HWJ_FighterBossHitboxSystem>(hitboxTransform.gameObject);
    }

    private static void ConfigurePhaseTwoProfile(
        SerializedProperty profile,
        string patternId,
        HWJ_FighterBossPhaseTwoPatternKind patternKind,
        HWJ_FighterBossHitboxSystem hitbox,
        Transform hitboxRoot,
        LineRenderer telegraph,
        float hitboxLocalX,
        float hitboxLocalY,
        float damageMultiplier,
        float extraKnockbackPower,
        float watchdogSeconds,
        float movementSpeed,
        float movementDuration,
        float arenaWidthTravelRatio,
        float telegraphRange)
    {
        profile.FindPropertyRelative("patternId").stringValue = patternId;
        profile.FindPropertyRelative("animatorState").stringValue = patternId;
        profile.FindPropertyRelative("patternKind").enumValueIndex = (int)patternKind;
        profile.FindPropertyRelative("hitbox").objectReferenceValue = hitbox;
        profile.FindPropertyRelative("hitboxRoot").objectReferenceValue = hitboxRoot;
        profile.FindPropertyRelative("telegraphRenderer").objectReferenceValue = telegraph;
        profile.FindPropertyRelative("hitboxLocalX").floatValue = hitboxLocalX;
        profile.FindPropertyRelative("hitboxLocalY").floatValue = hitboxLocalY;
        profile.FindPropertyRelative("damageMultiplier").floatValue = damageMultiplier;
        profile.FindPropertyRelative("extraKnockbackPower").floatValue = extraKnockbackPower;
        profile.FindPropertyRelative("watchdogSeconds").floatValue = watchdogSeconds;
        profile.FindPropertyRelative("movementSpeed").floatValue = movementSpeed;
        profile.FindPropertyRelative("movementDuration").floatValue = movementDuration;
        profile.FindPropertyRelative("arenaWidthTravelRatio").floatValue = arenaWidthTravelRatio;
        profile.FindPropertyRelative("telegraphRange").floatValue = telegraphRange;
    }

    private static LineRenderer CreatePatternTelegraph(
        Transform root,
        string childName,
        Material material,
        Color startColor,
        Color endColor,
        float width,
        int sortingOrder)
    {
        Transform telegraphTransform = GetOrCreateChild(root, childName);
        LineRenderer line = GetOrAdd<LineRenderer>(telegraphTransform.gameObject);
        ConfigureLineRenderer(line, material, width, sortingOrder);
        line.startColor = startColor;
        line.endColor = endColor;
        line.loop = false;
        line.positionCount = 2;
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, Vector3.right);
        line.enabled = false;
        return line;
    }

    private static void ConfigureLineRenderer(
        LineRenderer lineRenderer,
        Material material,
        float width,
        int sortingOrder)
    {
        lineRenderer.sharedMaterial = material;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.sortingOrder = sortingOrder;
    }

    private static TextMesh ConfigureTextMesh(
        GameObject gameObject,
        Vector3 localPosition,
        float characterSize,
        TextAnchor anchor)
    {
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;
        TextMesh textMesh = GetOrAdd<TextMesh>(gameObject);
        textMesh.anchor = anchor;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 32;
        textMesh.color = Color.white;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font != null)
        {
            textMesh.font = font;
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = font.material;
                renderer.sortingOrder = 232;
            }
        }

        return textMesh;
    }

    private static void ConfigureRootObjectAnimator(AnimatorController controller)
    {
        HWJ_RootObjectDataSO rootData = AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(BossRootDataPath);

        if (rootData == null)
        {
            return;
        }

        SerializedObject serializedRootData = new SerializedObject(rootData);
        SerializedProperty animatorController = serializedRootData.FindProperty("model.animatorController");

        if (animatorController != null)
        {
            animatorController.objectReferenceValue = controller;
            serializedRootData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rootData);
        }
    }

    private static void ValidateGeneratedAssets()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        GameObject backup = AssetDatabase.LoadAssetAtPath<GameObject>(BossBackupPath);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (prefab == null || backup == null || controller == null)
        {
            throw new InvalidOperationException("Fighter boss generation did not create every required asset.");
        }

        if (prefab.GetComponent<Animator>() == null
            || prefab.GetComponent<HWJ_BossDuplicateGuardSystem>() == null
            || prefab.GetComponent<HWJ_FighterBossComboSystem>() == null
            || prefab.GetComponent<HWJ_FighterBossChargeSystem>() == null
            || prefab.GetComponent<HWJ_FighterBossUppercutSystem>() == null
            || prefab.GetComponent<HWJ_FighterBossGroundSlamSystem>() == null
            || prefab.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>() == null
            || prefab.GetComponent<HWJ_FighterBossDeathSystem>() == null
            || prefab.GetComponentInChildren<HWJ_FighterBossHitboxSystem>(true) == null
            || prefab.GetComponentInChildren<HWJ_FighterBossHealthBarSystem>(true) == null)
        {
            throw new InvalidOperationException("Fighter boss prefab wiring is incomplete.");
        }

        HWJ_FighterBossHitboxSystem hitbox =
            prefab.GetComponentInChildren<HWJ_FighterBossHitboxSystem>(true);

        if (hitbox.ColliderEnabled || hitbox.IsArmed)
        {
            throw new InvalidOperationException("Combo Hitbox must be disabled in the saved prefab.");
        }
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} was not found.");
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} was not found.");
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} was not found.");
        }

        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetEnum(UnityEngine.Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} was not found.");
        }

        property.enumValueIndex = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] segments = folderPath.Split('/');
        string current = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }

            current = next;
        }
    }
}
