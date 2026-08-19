using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode control panel for forcing every fighter-boss phase and pattern.
/// It operates on the existing scene instance and never creates a duplicate boss.
/// </summary>
public sealed class HWJ_FighterBossTestWindow : EditorWindow
{
    private GameObject bossPrefabAsset;
    private HWJ_BossBrainSystem runtimeBoss;
    private Vector2 scroll;

    [MenuItem("Tools/HWJ/Boss/Fighter Boss Test Window")]
    public static void Open()
    {
        GetWindow<HWJ_FighterBossTestWindow>("Fighter Boss Test");
    }

    private void OnEnable()
    {
        bossPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            HWJ_FighterBossIntegrationBuilder.BossPrefabPath);
        FindRuntimeBoss();
    }

    private void OnInspectorUpdate()
    {
        if (EditorApplication.isPlaying && runtimeBoss == null)
        {
            FindRuntimeBoss();
        }

        Repaint();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Actual Boss", EditorStyles.boldLabel);
        bossPrefabAsset = (GameObject)EditorGUILayout.ObjectField(
            "Prefab Asset",
            bossPrefabAsset,
            typeof(GameObject),
            false);
        runtimeBoss = (HWJ_BossBrainSystem)EditorGUILayout.ObjectField(
            "Play Mode Instance",
            runtimeBoss,
            typeof(HWJ_BossBrainSystem),
            true);

        if (GUILayout.Button("Find Scene Boss"))
        {
            FindRuntimeBoss();
        }

        DrawRuntimeStatus();
        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || runtimeBoss == null))
        {
            DrawEncounterControls();
            EditorGUILayout.Space(8f);
            DrawPhaseOneButtons();
            EditorGUILayout.Space(8f);
            DrawPhaseTwoButtons();
        }

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Force controls are available only in Play Mode.",
                MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawRuntimeStatus()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Live State", EditorStyles.boldLabel);

        if (runtimeBoss == null)
        {
            EditorGUILayout.LabelField("Animator State", "-");
            EditorGUILayout.LabelField("Phase", "-");
            EditorGUILayout.LabelField("Health", "-");
            EditorGUILayout.LabelField("AttackId", "-");
            EditorGUILayout.LabelField("Active Hitboxes", "-");
            return;
        }

        HWJ_RuntimeStatusSystem status =
            runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_FighterBossAnimatorSystem animator =
            runtimeBoss.GetComponent<HWJ_FighterBossAnimatorSystem>();
        HWJ_FighterBossHitboxSystem[] hitboxes =
            runtimeBoss.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);
        string activeHitboxes = string.Join(
            ", ",
            hitboxes.Where(hitbox => hitbox != null && hitbox.IsArmed)
                .Select(hitbox => hitbox.name));

        EditorGUILayout.LabelField(
            "Animator State",
            animator != null && !string.IsNullOrEmpty(animator.CurrentStateName)
                ? animator.CurrentStateName
                : "-");
        EditorGUILayout.LabelField("Phase", runtimeBoss.FighterPhase.ToString());
        EditorGUILayout.LabelField(
            "Health",
            status != null ? $"{status.CurrentHp:0.##} / {status.MaxHp:0.##}" : "-");
        EditorGUILayout.LabelField(
            "AttackId",
            animator != null ? animator.CurrentAttackId.ToString() : "-");
        EditorGUILayout.LabelField(
            "Active Hitboxes",
            string.IsNullOrEmpty(activeHitboxes) ? "None" : activeHitboxes);
        EditorGUILayout.LabelField("AI", runtimeBoss.AIEnabled ? "On" : "Off");
    }

    private void DrawEncounterControls()
    {
        EditorGUILayout.LabelField("Encounter", EditorStyles.boldLabel);
        bool nextAi = EditorGUILayout.Toggle("AI Enabled", runtimeBoss.AIEnabled);

        if (nextAi != runtimeBoss.AIEnabled)
        {
            runtimeBoss.SetAIEnabled(nextAi);
        }

        DrawTwoButtons(
            "Start Phase1",
            () => runtimeBoss.ForcePhaseOne(),
            "Force Phase2",
            () => runtimeBoss.ForcePhaseTwo());
        DrawTwoButtons(
            "Set Health To 1",
            SetHealthToOne,
            "Clear Attack Objects",
            CleanupAttackObjects);
        DrawTwoButtons(
            "Reset Boss",
            () => runtimeBoss.ForcePhaseOne(),
            "Phase Transition",
            () => runtimeBoss.ForcePhaseTransition());

        if (GUILayout.Button("P2 Death"))
        {
            runtimeBoss.ForcePhaseTwo();
            runtimeBoss.ForceFinalDeath();
        }
    }

    private void DrawPhaseOneButtons()
    {
        EditorGUILayout.LabelField("Phase 1 Forced Patterns", EditorStyles.boldLabel);
        DrawTwoButtons(
            "P1 Combo",
            () => ForcePhaseOnePattern<HWJ_FighterBossComboSystem>(
                system => system.TryStartCombo(ResolveTarget())),
            "P1 Charge",
            () => ForcePhaseOnePattern<HWJ_FighterBossChargeSystem>(
                system => system.TryStartCharge(ResolveTarget())));
        DrawTwoButtons(
            "P1 Uppercut",
            () => ForcePhaseOnePattern<HWJ_FighterBossUppercutSystem>(
                system => system.TryStartUppercut(ResolveTarget())),
            "P1 GroundSlam",
            () => ForcePhaseOnePattern<HWJ_FighterBossGroundSlamSystem>(
                system => system.TryStartGroundSlam(ResolveTarget())));
    }

    private void DrawPhaseTwoButtons()
    {
        EditorGUILayout.LabelField("Phase 2 Forced Patterns", EditorStyles.boldLabel);
        DrawTwoButtons(
            "EnhancedCombo",
            () => ForcePhaseTwoPattern("P2_EnhancedCombo"),
            "DoubleCharge",
            () => ForcePhaseTwoPattern("P2_DoubleCharge"));
        DrawTwoButtons(
            "ThunderUppercut",
            () => ForcePhaseTwoPattern("P2_ThunderUppercut"),
            "DarkGroundSlam",
            () => ForcePhaseTwoPattern("P2_DarkGroundSlam"));
        DrawTwoButtons(
            "ShadowCombo",
            () => ForcePhaseTwoPattern("P2_ShadowCombo"),
            "LightningCast",
            () => ForcePhaseTwoPattern("P2_LightningCast"));
        DrawTwoButtons(
            "DarkWave",
            () => ForcePhaseTwoPattern("P2_DarkWave"),
            "SoulBind",
            () => ForcePhaseTwoPattern("P2_SoulBind"));

        if (GUILayout.Button("Ultimate"))
        {
            ForcePhaseTwoPattern("P2_Ultimate");
        }
    }

    private void ForcePhaseOnePattern<T>(Func<T, bool> execute) where T : Component
    {
        CleanupAttackObjects();

        if (runtimeBoss.FighterPhase != HWJ_FighterBossPhase.Phase1)
        {
            runtimeBoss.ForcePhaseOne();
        }

        T system = runtimeBoss.GetComponent<T>();

        if (system == null || !execute(system))
        {
            Debug.LogWarning($"[HWJ] Could not force {typeof(T).Name}.");
        }
    }

    private void ForcePhaseTwoPattern(string patternId)
    {
        CleanupAttackObjects();

        if (runtimeBoss.FighterPhase != HWJ_FighterBossPhase.Phase2)
        {
            runtimeBoss.ForcePhaseTwo();
        }

        HWJ_FighterBossPhaseTwoPatternSystem system =
            runtimeBoss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();

        if (system == null || !system.TryStartPattern(patternId, ResolveTarget()))
        {
            Debug.LogWarning($"[HWJ] Could not force {patternId}.");
        }
    }

    private void SetHealthToOne()
    {
        runtimeBoss.GetComponent<HWJ_RuntimeStatusSystem>()?.SetCurrentHpForDebug(1f);
    }

    private void CleanupAttackObjects()
    {
        if (runtimeBoss == null)
        {
            return;
        }

        runtimeBoss.GetComponent<HWJ_FighterBossComboSystem>()?.CancelActivePattern();
        runtimeBoss.GetComponent<HWJ_FighterBossChargeSystem>()?.CancelActivePattern();
        runtimeBoss.GetComponent<HWJ_FighterBossUppercutSystem>()?.CancelActivePattern();
        runtimeBoss.GetComponent<HWJ_FighterBossGroundSlamSystem>()?.CancelActivePattern();
        runtimeBoss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>()?.CancelActivePattern();

        HWJ_FighterBossHitboxSystem[] hitboxes =
            runtimeBoss.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i]?.Disarm();
        }
    }

    private Transform ResolveTarget()
    {
        if (runtimeBoss != null && runtimeBoss.Target != null)
        {
            return runtimeBoss.Target;
        }

        HWJ_RootObjectDataResolver[] resolvers =
            FindObjectsByType<HWJ_RootObjectDataResolver>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                runtimeBoss?.SetTarget(resolvers[i].transform);
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void FindRuntimeBoss()
    {
        HWJ_BossBrainSystem[] bosses = FindObjectsByType<HWJ_BossBrainSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        runtimeBoss = bosses.FirstOrDefault(
            boss => boss != null
                && boss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>() != null);
    }

    private static void DrawTwoButtons(
        string leftLabel,
        Action leftAction,
        string rightLabel,
        Action rightAction)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(leftLabel))
        {
            leftAction?.Invoke();
        }

        if (GUILayout.Button(rightLabel))
        {
            rightAction?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }
}
