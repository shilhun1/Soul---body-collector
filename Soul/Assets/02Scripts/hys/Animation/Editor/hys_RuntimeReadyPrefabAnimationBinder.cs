#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HWJ RuntimeReady 플레이어·몬스터 프리팹을 hys 전용 Animator와 모션 프로필에 연결합니다.
/// 플레이어와 몬스터가 같은 프로필을 공유하지 않게 분리하여 빙의 후에도 올바른 Controller를 유지합니다.
/// </summary>
[InitializeOnLoad]
public static class hys_RuntimeReadyPrefabAnimationBinder
{
    private const string SessionKey = "hys.RuntimeReadyPrefabAnimationBinder.v1";
    private const string RuntimeReadyRoot =
        "Assets/02Scripts/HWJ/Prefabs/Generated/RuntimeReady";
    private const string ProfileRoot =
        RuntimeReadyRoot + "/hys_Animation_RuntimeReady/Profiles";

    private sealed class WeaponSpec
    {
        public string Name;
        public HWJ_WeaponType WeaponType;
        public string PlayerControllerPath;
        public string MonsterControllerPath;
        public string DefaultAttackKey;
        public MotionSpec[] PlayerMotions;
        public MotionSpec[] MonsterMotions;
    }

    private sealed class MotionSpec
    {
        public string Key;
        public string Trigger;
        public string State;
        public bool UseTrigger;
        public bool UseCrossFade;
    }

    private static readonly WeaponSpec[] WeaponSpecs =
    {
        Weapon(
            "Sword",
            HWJ_WeaponType.Sword,
            "Assets/05Anims/Player_Anims/hys_Player_Sword.controller",
            "Assets/05Anims/hys_Enemy_Anims/Sword/hys_Monster_Sword.controller",
            "Sword_DiagonalSlash",
            PlayerMotions(
                "Sword",
                State("Sword_DiagonalSlash", "hys_Sword_Attack1"),
                State("Sword_UpDiagonalSlash", "hys_Sword_Attack2"),
                State("Sword_SwordWave", "hys_Sword_Attack2"),
                Trigger("PlayerSkill_Sword_ReapSlash"),
                Trigger("PlayerSkill_Sword_DashSlash"),
                Trigger("PlayerSkill_Sword_ForceSlash"),
                Trigger("PlayerSkill_Sword_FinalSlash")),
            MonsterMotions(
                "Sword",
                Trigger("Sword_DiagonalSlash", "M_sword_1"),
                Trigger("Sword_UpDiagonalSlash", "M_sword_2"),
                Trigger("Sword_SwordWave", "M_sword_3"))),
        Weapon(
            "Axe",
            HWJ_WeaponType.Axe,
            "Assets/05Anims/hys_Player_Anims/Axe/hys_Player_Axe.controller",
            "Assets/05Anims/hys_Enemy_Anims/Axe/hys_Monster_Axe.controller",
            "Axe_Swing",
            PlayerMotions(
                "Axe",
                State("Axe_Swing", "hys_Axe_Attack1"),
                State("Axe_BodyCharge", "hys_Axe_Attack2"),
                State("Axe_SpinCharge", "hys_Axe_Attack2"),
                Trigger("PlayerSkill_Axe_SlashAxe"),
                Trigger("PlayerSkill_Axe_FlameSlash"),
                Trigger("PlayerSkill_Axe_Whirlwind"),
                Trigger("PlayerSkill_Axe_EarthBreaker")),
            MonsterMotions(
                "Axe",
                Trigger("Axe_Swing", "M_axe_1"),
                Trigger("Axe_BodyCharge", "M_axe_2"),
                Trigger("Axe_SpinCharge", "M_axe_3"))),
        Weapon(
            "Bow",
            HWJ_WeaponType.Bow,
            "Assets/05Anims/hys_Player_Anims/Bow/hys_Player_Bow.controller",
            "Assets/05Anims/hys_Enemy_Anims/Bow/hys_Monster_Bow.controller",
            "Bow_Shoot",
            PlayerMotions(
                "Bow",
                State("Bow_Draw", "hys_Bow_Attack1"),
                State("Bow_Shoot", "hys_Bow_Attack2"),
                Trigger("PlayerSkill_Bow_EvasionTriple"),
                Trigger("PlayerSkill_Bow_PentaStrike"),
                Trigger("PlayerSkill_Bow_GrandPierce"),
                Trigger("PlayerSkill_Bow_ArrowsRain")),
            MonsterMotions(
                "Bow",
                Trigger("Bow_Draw", "M_bow_1"),
                Trigger("Bow_Shoot", "M_bow_2"),
                Trigger("Bow_RapidShot", "M_bow_3"))),
        Weapon(
            "Lance",
            HWJ_WeaponType.Lance,
            "Assets/05Anims/hys_Player_Anims/Lance/hys_Player_Lance.controller",
            "Assets/05Anims/hys_Enemy_Anims/Lance/hys_Monster_Lance.controller",
            "Lance_ChargeThrust",
            PlayerMotions(
                "Lance",
                State("Lance_ChargeThrust", "hys_Lance_Attack1"),
                State("Lance_ThrustCombo1", "hys_Lance_Attack1"),
                State("Lance_ThrustCombo2", "hys_Lance_Attack2"),
                State("Lance_ThrustCombo3", "hys_Lance_Attack1"),
                State("Lance_ThrustCombo4", "hys_Lance_Attack2"),
                State("Lance_FinisherThrust", "hys_Lance_Attack2"),
                Trigger("PlayerSkill_Lance_PiercingDrive"),
                Trigger("PlayerSkill_Lance_RapidStinger"),
                Trigger("PlayerSkill_Lance_RisingSpear"),
                Trigger("PlayerSkill_Lance_BurstLance")),
            MonsterMotions(
                "Lance",
                Trigger("Lance_ChargeThrust", "M_lance_1"),
                Trigger("Lance_ThrustCombo1", "M_lance_2"),
                Trigger("Lance_FinisherThrust", "M_lance_3"))),
        Weapon(
            "Shield",
            HWJ_WeaponType.Shield,
            "Assets/05Anims/hys_Player_Anims/Shield/hys_Player_Shield.controller",
            "Assets/05Anims/hys_Enemy_Anims/Shield/hys_Monster_Shield.controller",
            "Shield_Bash",
            PlayerMotions(
                "Shield",
                State("Shield_Guard", "hys_Shield_Attack1"),
                State("Shield_Bash", "hys_Shield_Attack2"),
                Trigger("PlayerSkill_Shield_ShieldSlam"),
                Trigger("PlayerSkill_Shield_GroundStrike"),
                Trigger("PlayerSkill_Shield_DarkBarrier"),
                Trigger("PlayerSkill_Shield_GroundQuake")),
            MonsterMotions(
                "Shield",
                Trigger("Shield_Guard", "M_shield_1"),
                Trigger("Shield_Bash", "M_shield_2"),
                Trigger("Shield_Charge", "M_shield_3")))
    };

    static hys_RuntimeReadyPrefabAnimationBinder()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            EditorApplication.delayCall += GenerateOnce;
        }
    }

    [MenuItem("Tools/hys/Animation/RuntimeReady 프리팹 애니메이션 통합")]
    public static void Generate()
    {
        EnsureFolder(ProfileRoot);
        Dictionary<string, HWJ_MotionProfileSO> playerProfiles = new();
        Dictionary<string, HWJ_MotionProfileSO> monsterProfiles = new();

        foreach (WeaponSpec weapon in WeaponSpecs)
        {
            RuntimeAnimatorController playerController = LoadController(weapon.PlayerControllerPath);
            RuntimeAnimatorController monsterController = LoadController(weapon.MonsterControllerPath);
            playerProfiles[weapon.Name] = CreateOrUpdateProfile(
                $"{ProfileRoot}/hys_RuntimeReady_Player_{weapon.Name}MotionProfile.asset",
                weapon.WeaponType,
                playerController,
                weapon.DefaultAttackKey,
                weapon.PlayerMotions);
            monsterProfiles[weapon.Name] = CreateOrUpdateProfile(
                $"{ProfileRoot}/hys_RuntimeReady_Monster_{weapon.Name}MotionProfile.asset",
                weapon.WeaponType,
                monsterController,
                weapon.DefaultAttackKey,
                weapon.MonsterMotions);
        }

        WirePlayerPrefab(playerProfiles);
        foreach (WeaponSpec weapon in WeaponSpecs)
        {
            WireEnemyPrefab(weapon, monsterProfiles[weapon.Name], true);
            WireEnemyPrefab(weapon, monsterProfiles[weapon.Name], false);
            WireCorpsePrefab(weapon);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[hys RuntimeReady] 플레이어 1종, 몬스터 10종, 시체 5종에 전용 Animator와 모션 프로필을 연결했습니다.");
    }

    private static void GenerateOnce()
    {
        EditorApplication.delayCall -= GenerateOnce;
        try
        {
            Generate();
            SessionState.SetBool(SessionKey, true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void WirePlayerPrefab(Dictionary<string, HWJ_MotionProfileSO> profiles)
    {
        string prefabPath = $"{RuntimeReadyRoot}/HWJ_Runtime_Player_Soul.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>(true);
            HWJ_CharacterMotionSystem motionSystem = root.GetComponent<HWJ_CharacterMotionSystem>();
            HWJ_SoulSystem soulSystem = root.GetComponent<HWJ_SoulSystem>();
            HWJ_PossessionSystem possessionSystem = root.GetComponent<HWJ_PossessionSystem>();
            RuntimeAnimatorController ghostController = LoadController(
                "Assets/05Anims/Soul_Anims/hys_Ghost_Animation.controller");

            animator.runtimeAnimatorController = ghostController;
            SetMotionProfiles(motionSystem, null, ValuesInWeaponOrder(profiles));

            if (animator.GetComponent<hys_PlayerSkillEffectPlayer>() == null)
            {
                animator.gameObject.AddComponent<hys_PlayerSkillEffectPlayer>();
            }

            if (animator.GetComponent<hys_Ghost_Animator>() == null)
            {
                animator.gameObject.AddComponent<hys_Ghost_Animator>();
            }

            hys_Player_Animator playerAnimator = root.GetComponent<hys_Player_Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = root.AddComponent<hys_Player_Animator>();
            }

            SerializedObject serialized = new SerializedObject(playerAnimator);
            SetObject(serialized, "animator", animator);
            SetObject(serialized, "spriteRenderer", renderer);
            SetObject(serialized, "rb", root.GetComponent<Rigidbody2D>());
            SetObject(serialized, "soulSystem", soulSystem);
            SetObject(serialized, "possessionSystem", possessionSystem);
            SetObject(serialized, "bodyAnimatorController", LoadController(WeaponSpecs[0].PlayerControllerPath));
            SetObject(serialized, "swordAnimatorController", LoadController(WeaponSpecs[0].PlayerControllerPath));
            SetObject(serialized, "axeAnimatorController", LoadController(WeaponSpecs[1].PlayerControllerPath));
            SetObject(serialized, "bowAnimatorController", LoadController(WeaponSpecs[2].PlayerControllerPath));
            SetObject(serialized, "lanceAnimatorController", LoadController(WeaponSpecs[3].PlayerControllerPath));
            SetObject(serialized, "shieldAnimatorController", LoadController(WeaponSpecs[4].PlayerControllerPath));
            SetObject(serialized, "ghostAnimatorController", ghostController);
            SetObject(serialized, "swordGhostPossessionClip", LoadClip(
                "Assets/05Anims/Soul_Anims/hys_Ghost_possession.anim"));
            SetObject(serialized, "axeGhostPossessionClip", LoadClip(
                "Assets/05Anims/hys_Player_Anims/Axe/Clips/hys_Ghost_Possession_Axe.anim"));
            SetObject(serialized, "bowGhostPossessionClip", LoadClip(
                "Assets/05Anims/hys_Player_Anims/Bow/Clips/hys_Ghost_Possession_Bow_Bishop.anim"));
            SetObject(serialized, "lanceGhostPossessionClip", LoadClip(
                "Assets/05Anims/hys_Player_Anims/Lance/Clips/hys_Ghost_Possession_Lance.anim"));
            SetObject(serialized, "shieldGhostPossessionClip", LoadClip(
                "Assets/05Anims/hys_Player_Anims/Shield/Clips/hys_Ghost_Possession_Shield.anim"));
            serialized.FindProperty("useAnimationOnlyAttackCombo").boolValue = false;
            serialized.FindProperty("forceBodyLocomotionStateSync").boolValue = false;
            serialized.FindProperty("autoReturnFinishedActionStates").boolValue = false;
            serialized.FindProperty("autoReturnByStateTime").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireEnemyPrefab(
        WeaponSpec weapon,
        HWJ_MotionProfileSO profile,
        bool possessable)
    {
        string kind = possessable ? "Possessable" : "NoCorpse";
        string prefabPath = $"{RuntimeReadyRoot}/Enemies/HWJ_Runtime_Enemy_{kind}_{weapon.Name}.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = LoadController(weapon.MonsterControllerPath);
            SetMotionProfiles(root.GetComponent<HWJ_CharacterMotionSystem>(), profile, new[] { profile });
            EnsureMonsterLifecycle(root, animator);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void WireCorpsePrefab(WeaponSpec weapon)
    {
        string prefabPath = $"{RuntimeReadyRoot}/Corpses/HWJ_Runtime_Corpse_{weapon.Name}.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = LoadController(weapon.MonsterControllerPath);
            EnsureMonsterLifecycle(root, animator);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureMonsterLifecycle(GameObject root, Animator animator)
    {
        hys_MonsterAnimatorLifecycle lifecycle = root.GetComponent<hys_MonsterAnimatorLifecycle>();
        if (lifecycle == null)
        {
            lifecycle = root.AddComponent<hys_MonsterAnimatorLifecycle>();
        }

        SerializedObject serialized = new SerializedObject(lifecycle);
        SetObject(serialized, "animator", animator);
        SetObject(serialized, "runtimeStatus", root.GetComponent<HWJ_RuntimeStatusSystem>());
        SetObject(serialized, "body", root.GetComponent<Rigidbody2D>());
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HWJ_MotionProfileSO CreateOrUpdateProfile(
        string assetPath,
        HWJ_WeaponType weaponType,
        RuntimeAnimatorController controller,
        string defaultAttackKey,
        MotionSpec[] motions)
    {
        HWJ_MotionProfileSO profile = AssetDatabase.LoadAssetAtPath<HWJ_MotionProfileSO>(assetPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<HWJ_MotionProfileSO>();
            profile.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(profile, assetPath);
        }

        SerializedObject serialized = new SerializedObject(profile);
        serialized.FindProperty("weaponType").intValue = (int)weaponType;
        serialized.FindProperty("animatorController").objectReferenceValue = controller;
        serialized.FindProperty("defaultAttackMotionKey").stringValue = defaultAttackKey;
        SerializedProperty motionArray = serialized.FindProperty("motions");
        motionArray.arraySize = motions.Length;
        for (int i = 0; i < motions.Length; i++)
        {
            SerializedProperty motion = motionArray.GetArrayElementAtIndex(i);
            motion.FindPropertyRelative("motionKey").stringValue = motions[i].Key;
            motion.FindPropertyRelative("animatorTriggerName").stringValue = motions[i].Trigger ?? string.Empty;
            motion.FindPropertyRelative("animatorStateName").stringValue = motions[i].State ?? string.Empty;
            motion.FindPropertyRelative("layerIndex").intValue = 0;
            motion.FindPropertyRelative("transitionSeconds").floatValue = 0.03f;
            motion.FindPropertyRelative("useTrigger").boolValue = motions[i].UseTrigger;
            motion.FindPropertyRelative("useCrossFade").boolValue = motions[i].UseCrossFade;
            motion.FindPropertyRelative("resetTriggerBeforeSet").boolValue = true;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void SetMotionProfiles(
        HWJ_CharacterMotionSystem motionSystem,
        HWJ_MotionProfileSO fallback,
        HWJ_MotionProfileSO[] profiles)
    {
        SerializedObject serialized = new SerializedObject(motionSystem);
        serialized.FindProperty("fallbackMotionProfile").objectReferenceValue = fallback;
        SerializedProperty array = serialized.FindProperty("weaponMotionProfiles");
        array.arraySize = profiles.Length;
        for (int i = 0; i < profiles.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HWJ_MotionProfileSO[] ValuesInWeaponOrder(
        Dictionary<string, HWJ_MotionProfileSO> profiles)
    {
        HWJ_MotionProfileSO[] values = new HWJ_MotionProfileSO[WeaponSpecs.Length];
        for (int i = 0; i < WeaponSpecs.Length; i++)
        {
            values[i] = profiles[WeaponSpecs[i].Name];
        }

        return values;
    }

    private static MotionSpec[] PlayerMotions(string weapon, params MotionSpec[] attacksAndSkills)
    {
        List<MotionSpec> motions = new List<MotionSpec>(attacksAndSkills)
        {
            State("Jump", $"hys_{weapon}_Jump_Start"),
            State("DoubleJump", $"hys_{weapon}_Jump_Start"),
            State("DropJump", $"hys_{weapon}_Jump_Fall"),
            State("Dash", $"hys_{weapon}_Dash"),
            State("Hit", $"hys_{weapon}_Hit"),
            State("Dead", $"hys_{weapon}_Die")
        };
        return motions.ToArray();
    }

    private static MotionSpec[] MonsterMotions(string weapon, params MotionSpec[] attacks)
    {
        List<MotionSpec> motions = new List<MotionSpec>(attacks)
        {
            State("Hit", $"hys_Monster_{weapon}_Hit"),
            State("Dead", $"hys_Monster_{weapon}_Death")
        };
        if (weapon == "Sword")
        {
            motions.Add(State("Dash", "hys_Monster_Sword_Dash"));
            motions.Add(State("Jump", "hys_Monster_Sword_Jump_Start"));
            motions.Add(State("DoubleJump", "hys_Monster_Sword_Jump_Start"));
            motions.Add(State("DropJump", "hys_Monster_Sword_Jump_Fall"));
        }

        return motions.ToArray();
    }

    private static MotionSpec Trigger(string key, string trigger = null)
    {
        return new MotionSpec
        {
            Key = key,
            Trigger = trigger ?? key,
            UseTrigger = true
        };
    }

    private static MotionSpec State(string key, string state)
    {
        return new MotionSpec
        {
            Key = key,
            State = state,
            UseCrossFade = true
        };
    }

    private static WeaponSpec Weapon(
        string name,
        HWJ_WeaponType weaponType,
        string playerControllerPath,
        string monsterControllerPath,
        string defaultAttackKey,
        MotionSpec[] playerMotions,
        MotionSpec[] monsterMotions)
    {
        return new WeaponSpec
        {
            Name = name,
            WeaponType = weaponType,
            PlayerControllerPath = playerControllerPath,
            MonsterControllerPath = monsterControllerPath,
            DefaultAttackKey = defaultAttackKey,
            PlayerMotions = playerMotions,
            MonsterMotions = monsterMotions
        };
    }

    private static RuntimeAnimatorController LoadController(string path)
    {
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (controller == null)
        {
            throw new InvalidOperationException($"Animator Controller를 찾지 못했습니다: {path}");
        }

        return controller;
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            throw new InvalidOperationException($"Animation Clip을 찾지 못했습니다: {path}");
        }

        return clip;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException($"직렬화 필드를 찾지 못했습니다: {propertyName}");
        }

        property.objectReferenceValue = value;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
#endif
