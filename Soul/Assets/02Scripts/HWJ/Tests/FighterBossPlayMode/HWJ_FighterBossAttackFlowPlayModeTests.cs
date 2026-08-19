using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
/// <summary>
/// Verifies the complete animation-driven attack contract on the production boss prefab.
/// The test observes runtime state instead of invoking Animation Event receivers directly.
/// </summary>
public sealed class HWJ_FighterBossAttackFlowPlayModeTests
{
    private const string BossPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab";
    private const string ChargeClipPath =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Clips/Boss_P1_Attack_Charge.anim";
    private const string UppercutClipPath =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Clips/Boss_P1_Attack_Uppercut.anim";
    private const string ControllerPath =
        "Assets/02Scripts/HWJ/Art/Boss/_Generated/Controllers/HWJ_FighterBoss_Integrated.controller";
    private const string BossRootDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset";
    private const string UltimatePatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_Ultimate.asset";
    private const string ShadowComboPatternPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_P2_ShadowCombo.asset";

    private GameObject boss;
    private GameObject target;
    private readonly List<Object> auxiliaryObjects = new List<Object>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        HWJ_GameplayEvents.ClearAllSubscribers();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;

        if (boss != null)
        {
            Object.Destroy(boss);
        }

        if (target != null)
        {
            Object.Destroy(target);
        }

        for (int i = auxiliaryObjects.Count - 1; i >= 0; i--)
        {
            if (auxiliaryObjects[i] != null)
            {
                Object.Destroy(auxiliaryObjects[i]);
            }
        }

        auxiliaryObjects.Clear();

        HWJ_GameplayEvents.ClearAllSubscribers();
        yield return null;
    }

    [Test]
    public void ChargeAndUppercutClips_PutHitboxBeforeMovementAndRecoveryBeforeEnd()
    {
        AssertClipEventOrder(
            ChargeClipPath,
            "Anim_EnableHitbox",
            "Anim_ApplyMovement",
            "Anim_StopMovement",
            "Anim_DisableHitbox",
            "Anim_RecoveryStart",
            "Anim_AttackEnd");
        AssertClipEventOrder(
            UppercutClipPath,
            "Anim_EnableHitbox",
            "Anim_ApplyMovement",
            "Anim_DisableHitbox",
            "Anim_RecoveryStart",
            "Anim_AttackEnd");

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Assert.NotNull(controller);
        AnimatorStateMachine root = controller.layers[0].stateMachine;
        Assert.IsFalse(
            root.states.Any(child => child.state != null && child.state.name == "Entry"),
            "A synthetic Entry state creates an ignored zero-condition transition.");
        Assert.Greater(root.entryTransitions.Length, 0);
    }

    [UnityTest]
    public IEnumerator Charge_AnimationHitboxMovementRecoveryAndIdle_RunInOrder()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab, $"Production boss prefab is missing: {BossPrefabPath}");

        boss = Object.Instantiate(prefab);
        boss.name = "HWJ_FighterBoss_AttackFlow_Test";
        boss.transform.position = Vector3.zero;

        target = new GameObject("HWJ_FighterBoss_AttackFlow_Target");
        target.transform.position = new Vector3(12f, 0f, 0f);

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossChargeSystem charge = boss.GetComponent<HWJ_FighterBossChargeSystem>();
        HWJ_FighterBossAnimatorSystem animatorSystem =
            boss.GetComponent<HWJ_FighterBossAnimatorSystem>();
        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();

        Assert.NotNull(brain);
        Assert.NotNull(charge);
        Assert.NotNull(animatorSystem);
        Assert.NotNull(charge.ChargeHitbox);
        Assert.NotNull(body);

        // Horizontal movement is isolated from gravity so the assertion measures Charge only.
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        brain.ForcePhaseOne();
        brain.SetAIEnabled(false);
        brain.SetTarget(target.transform);
        yield return null;

        Vector2 startPosition = body.position;
        Assert.IsTrue(charge.TryStartCharge(target.transform));
        Assert.AreEqual(2, animatorSystem.CurrentAttackId);
        Assert.IsTrue(charge.IsCasting);
        Assert.AreEqual("P1_Cast_Charge", animatorSystem.CurrentStateName);
        Assert.IsFalse(charge.IsTelegraphVisible);
        Assert.IsFalse(charge.ChargeHitbox.IsArmed);
        Assert.That(charge.LastChargeDistance, Is.EqualTo(0f).Within(0.001f));

        yield return WaitUntilOrFail(
            () => animatorSystem.CurrentStateName == "P1_Attack_Charge",
            1.1f,
            "Charge attack animation did not start after its cast-wait motion.");

        Assert.IsFalse(charge.IsCasting);
        Assert.IsFalse(charge.ChargeHitbox.IsArmed);
        Assert.That(charge.LastChargeDistance, Is.EqualTo(0f).Within(0.001f));

        yield return WaitUntilOrFail(
            () => charge.ChargeHitbox.IsArmed,
            0.9f,
            "Animation Event did not arm the Charge hitbox.");

        // Movement is deliberately scheduled after the hitbox event, not in the same event tick.
        Assert.That(charge.LastChargeDistance, Is.EqualTo(0f).Within(0.001f));
        Assert.That(body.position.x, Is.EqualTo(startPosition.x).Within(0.02f));

        yield return WaitUntilOrFail(
            () => charge.LastChargeDistance > 0.05f,
            0.5f,
            "Charge did not move its Rigidbody after the hitbox became active.");

        Assert.IsTrue(charge.ChargeHitbox.IsArmed);
        Assert.Greater(body.position.x, startPosition.x + 0.05f);

        yield return WaitUntilOrFail(
            () => charge.IsRecovering,
            1f,
            "Charge did not enter its recovery window.");

        Assert.IsFalse(charge.ChargeHitbox.IsArmed);
        Assert.IsFalse(charge.ChargeHitbox.ColliderEnabled);
        Assert.Greater(charge.LastChargeDistance, 0.05f);

        yield return WaitUntilOrFail(
            () => !charge.IsPatternRunning,
            1.2f,
            "Charge did not finish after recovery.");
        yield return null;

        Assert.IsFalse(animatorSystem.IsAttacking);
        Assert.AreEqual(0, animatorSystem.CurrentAttackId);
        Assert.AreEqual("P1_Idle", animatorSystem.CurrentStateName);
        Assert.IsFalse(charge.ChargeHitbox.IsArmed);
        Assert.IsFalse(charge.ChargeHitbox.ColliderEnabled);
    }

    [UnityTest]
    public IEnumerator PatternAI_ExcludesTheTwoMostRecentlyExecutedPatterns()
    {
        InstantiateBossAndTarget(new Vector3(1.2f, 0f, 0f));
        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_BossPatternSystem patternSystem = boss.GetComponent<HWJ_BossPatternSystem>();
        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        brain.ForcePhaseOne();
        brain.SetAIEnabled(false);
        patternSystem.ResetPatternHistory();
        yield return null;

        List<string> selectedKeys = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(patternSystem.TryUseAvailablePattern(target.transform, 1, true));
            selectedKeys.Add(patternSystem.LastExecutedPatternKey);
            patternSystem.CancelActiveSpecialPatterns();
            yield return null;
        }

        Assert.AreEqual(3, selectedKeys.Distinct().Count());
        CollectionAssert.AreEqual(
            selectedKeys.Skip(1).ToArray(),
            patternSystem.RecentPatternKeys);
    }

    [UnityTest]
    public IEnumerator UltimateAndAdvancedPatterns_RespectHpTimeAndCooldownConditions()
    {
        InstantiateBossAndTarget(new Vector3(3f, 0f, 0f));
        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_RuntimeStatusSystem status = boss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_FighterBossPhaseTwoPatternSystem phaseTwo =
            boss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        HWJ_BossPatternDataSO ultimate =
            AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(UltimatePatternPath);
        HWJ_BossPatternDataSO shadowCombo =
            AssetDatabase.LoadAssetAtPath<HWJ_BossPatternDataSO>(ShadowComboPatternPath);
        Assert.NotNull(ultimate);
        Assert.NotNull(shadowCombo);

        brain.ForcePhaseTwo();
        brain.SetAIEnabled(false);
        yield return null;

        Assert.IsFalse(phaseTwo.CanUsePattern(ultimate, target.transform));
        Assert.IsFalse(phaseTwo.CanUsePattern(shadowCombo, target.transform));

        status.SetCurrentHpForDebug(status.MaxHp * 0.6f);
        Assert.IsTrue(phaseTwo.CanUsePattern(shadowCombo, target.transform));
        Assert.IsFalse(phaseTwo.CanUsePattern(ultimate, target.transform));

        status.SetCurrentHpForDebug(status.MaxHp * 0.3f);
        Assert.IsFalse(
            phaseTwo.CanUsePattern(ultimate, target.transform),
            "Ultimate must be blocked during the first ten seconds of phase two.");

        // The real ten-second gate was verified above. Disable only that gate on this
        // test instance so the Ultimate cooldown can be checked independently.
        SetPrivateField(phaseTwo, "ultimateMinimumPhaseSeconds", 0f);
        SetPrivateField(phaseTwo, "phaseTwoStartedAt", Time.time);
        SetPrivateField(phaseTwo, "nextUltimateUseTime", 0f);
        Assert.IsTrue(phaseTwo.CanUsePattern(ultimate, target.transform));

        SetPrivateField(phaseTwo, "nextUltimateUseTime", Time.time + 18f);
        Assert.IsFalse(
            phaseTwo.CanUsePattern(ultimate, target.transform),
            "Ultimate must respect its reuse cooldown.");
    }

    [UnityTest]
    public IEnumerator SoulBind_ReleasesOnPlayerDashAndMaximumDistance()
    {
        InstantiateBossAndTarget(new Vector3(2.5f, 0f, 0f));
        Rigidbody2D targetBody = target.AddComponent<Rigidbody2D>();
        targetBody.gravityScale = 0f;
        target.AddComponent<HWJ_RuntimeStatusSystem>();
        HWJ_PlayerMovementSystem playerMovement = target.AddComponent<HWJ_PlayerMovementSystem>();

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossPhaseTwoPatternSystem phaseTwo =
            boss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        brain.ForcePhaseTwo();
        brain.SetAIEnabled(false);
        Time.timeScale = 3f;
        yield return null;

        Assert.IsTrue(phaseTwo.TryStartPattern("P2_SoulBind", target.transform));
        yield return WaitUntilOrFail(
            () => phaseTwo.IsSoulBindActive,
            1f,
            "SoulBind did not connect after its warning.");
        SetPrivateField(playerMovement, "dashEndTime", Time.time + 1f);
        yield return WaitUntilOrFail(
            () => !phaseTwo.IsSoulBindActive,
            0.5f,
            "SoulBind did not release when the player dashed.");
        phaseTwo.CancelActivePattern();

        SetPrivateField(playerMovement, "dashEndTime", -1f);
        target.transform.position = new Vector3(2.5f, 0f, 0f);
        Assert.IsTrue(phaseTwo.TryStartPattern("P2_SoulBind", target.transform));
        yield return WaitUntilOrFail(
            () => phaseTwo.IsSoulBindActive,
            1f,
            "Second SoulBind did not connect after its warning.");
        target.transform.position = new Vector3(20f, 0f, 0f);
        yield return WaitUntilOrFail(
            () => !phaseTwo.IsSoulBindActive,
            0.5f,
            "SoulBind did not release beyond its maximum distance.");
        phaseTwo.CancelActivePattern();
        Time.timeScale = 1f;

        Assert.AreEqual(0, phaseTwo.ActiveProjectileCount);
        Assert.AreEqual(0, phaseTwo.ActiveLingeringHazardCount);
    }

    [UnityTest]
    public IEnumerator WholeFight_ProductionPrefab_CompletesThreeConsecutiveRuns()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        HWJ_RootObjectDataSO sourceRootData =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(BossRootDataPath);
        Assert.NotNull(prefab);
        Assert.NotNull(sourceRootData);
        Assert.IsInstanceOf<HWJ_BossTypeDataSO>(sourceRootData.SelectedTypeData);

        target = new GameObject("HWJ_FighterBoss_WholeFight_Target");
        target.transform.position = new Vector3(2f, 0f, 0f);
        CreateStaticBlock("WholeFight_Ground", new Vector2(0f, -0.5f), new Vector2(40f, 1f));
        Time.timeScale = 4f;

        for (int runIndex = 0; runIndex < 3; runIndex++)
        {
            HWJ_RootObjectDataSO runtimeRootData = Object.Instantiate(sourceRootData);
            HWJ_BossTypeDataSO runtimeBossType = Object.Instantiate(
                (HWJ_BossTypeDataSO)sourceRootData.SelectedTypeData);
            runtimeBossType.FSM.phaseTransitionSeconds = 0.2f;
            SetPrivateField(runtimeRootData, "selectedTypeData", runtimeBossType);
            auxiliaryObjects.Add(runtimeRootData);
            auxiliaryObjects.Add(runtimeBossType);

            boss = Object.Instantiate(prefab);
            boss.name = $"HWJ_FighterBoss_WholeFight_Run_{runIndex + 1}";
            boss.transform.position = Vector3.zero;
            SetFastDialogueTiming(boss);

            HWJ_RootObjectDataResolver resolver = boss.GetComponent<HWJ_RootObjectDataResolver>();
            HWJ_RuntimeStatusSystem status = boss.GetComponent<HWJ_RuntimeStatusSystem>();
            HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
            HWJ_FighterBossComboSystem combo = boss.GetComponent<HWJ_FighterBossComboSystem>();
            HWJ_FighterBossPhaseTwoPatternSystem phaseTwo =
                boss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
            HWJ_FighterBossDeathSystem death = boss.GetComponent<HWJ_FighterBossDeathSystem>();
            Rigidbody2D body = boss.GetComponent<Rigidbody2D>();

            Assert.NotNull(resolver);
            Assert.NotNull(status);
            Assert.NotNull(brain);
            Assert.NotNull(combo);
            Assert.NotNull(phaseTwo);
            Assert.NotNull(death);
            Assert.NotNull(body);

            brain.enabled = false;
            resolver.SetRootObjectData(runtimeRootData);
            status.RefreshCurrentHpFromData(true);
            brain.enabled = true;
            brain.ForcePhaseOne();
            brain.SetAIEnabled(false);
            brain.SetTarget(target.transform);
            body.gravityScale = 3f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            yield return null;

            Assert.IsTrue(combo.TryStartCombo(target.transform));
            yield return WaitUntilOrFail(
                () => !combo.IsPatternRunning,
                2f,
                $"Whole-fight run {runIndex + 1}: P1 Combo did not finish.");
            Assert.AreEqual(1, combo.CompletedComboCount);
            AssertAllHitboxesInactive(boss);

            status.ApplyDamage(status.MaxHp);
            Assert.AreEqual(HWJ_FighterBossPhase.Transition, brain.FighterPhase);
            Assert.IsFalse(status.IsDead, "P1 depletion must start transition, not final death.");
            Assert.AreEqual(0, death.BeginDeathCount);

            yield return WaitUntilOrFail(
                () => brain.FighterPhase == HWJ_FighterBossPhase.Phase2,
                3f,
                $"Whole-fight run {runIndex + 1}: phase transition did not finish.");
            Assert.AreEqual(status.MaxHp, status.CurrentHp, 0.001f);
            Assert.AreEqual(1, brain.CompletedTwoBarTransitionCount);

            status.SetCurrentHpForDebug(status.MaxHp * 0.3f);
            SetPrivateField(phaseTwo, "ultimateMinimumPhaseSeconds", 0f);
            SetPrivateField(phaseTwo, "nextUltimateUseTime", 0f);
            Assert.IsTrue(phaseTwo.TryStartPattern("P2_Ultimate", target.transform));
            yield return WaitUntilOrFail(
                () => !phaseTwo.IsPatternRunning,
                4f,
                $"Whole-fight run {runIndex + 1}: P2 Ultimate did not finish.");
            Assert.AreEqual(1, phaseTwo.GetCompletedCount("P2_Ultimate"));
            Assert.AreEqual(0, phaseTwo.ActiveProjectileCount);
            Assert.AreEqual(0, phaseTwo.ActiveLingeringHazardCount);
            AssertAllHitboxesInactive(boss);

            status.ApplyDamage(status.MaxHp);
            yield return WaitUntilOrFail(
                () => death.IsDeathComplete,
                2f,
                $"Whole-fight run {runIndex + 1}: final death did not finish.");

            Assert.AreEqual(HWJ_FighterBossPhase.Dead, brain.FighterPhase);
            Assert.AreEqual(1, death.BeginDeathCount);
            Assert.AreEqual(1, death.CompletedDeathCount);
            Assert.AreEqual(1, death.CompletionEventCount);
            Assert.IsFalse(death.BodySimulated);
            AssertAllHitboxesInactive(boss);
            TestContext.WriteLine(
                $"WholeFightRun={runIndex + 1}; Flow=P1->Transition->P2->Death; "
                + $"TransitionCount={brain.CompletedTwoBarTransitionCount}; "
                + $"DeathEvents={death.CompletionEventCount}");

            Object.Destroy(boss);
            boss = null;
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator AutomaticAI_RunsPhaseOneFor120SecondsAndPhaseTwoFor180Seconds()
    {
        InstantiateBossAndTarget(new Vector3(2f, 0f, 0f));
        Rigidbody2D targetBody = target.AddComponent<Rigidbody2D>();
        targetBody.bodyType = RigidbodyType2D.Kinematic;
        targetBody.gravityScale = 0f;
        CreateStaticBlock("AI_Soak_Ground", new Vector2(0f, -0.5f), new Vector2(40f, 1f));
        CreateStaticBlock("AI_Soak_LeftWall", new Vector2(-8f, 3f), new Vector2(1f, 8f));
        CreateStaticBlock("AI_Soak_RightWall", new Vector2(8f, 3f), new Vector2(1f, 8f));

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_RuntimeStatusSystem status = boss.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_BossPatternSystem patternSystem = boss.GetComponent<HWJ_BossPatternSystem>();
        HWJ_FighterBossComboSystem combo = boss.GetComponent<HWJ_FighterBossComboSystem>();
        HWJ_FighterBossChargeSystem charge = boss.GetComponent<HWJ_FighterBossChargeSystem>();
        HWJ_FighterBossUppercutSystem uppercut = boss.GetComponent<HWJ_FighterBossUppercutSystem>();
        HWJ_FighterBossGroundSlamSystem groundSlam =
            boss.GetComponent<HWJ_FighterBossGroundSlamSystem>();
        HWJ_FighterBossPhaseTwoPatternSystem phaseTwo =
            boss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        Rigidbody2D body = boss.GetComponent<Rigidbody2D>();

        Assert.NotNull(brain);
        Assert.NotNull(status);
        Assert.NotNull(patternSystem);
        Assert.NotNull(phaseTwo);
        Assert.NotNull(body);

        Time.timeScale = 10f;
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        brain.ForcePhaseOne();
        brain.SetTarget(target.transform);
        brain.SetAIEnabled(true);
        yield return null;

        float phaseOneEnd = Time.time + 120f;
        while (Time.time < phaseOneEnd)
        {
            yield return null;
        }

        int phaseOneCompletions = combo.CompletedComboCount
            + charge.CompletedChargeCount
            + uppercut.CompletedUppercutCount
            + groundSlam.CompletedGroundSlamCount;
        Assert.GreaterOrEqual(phaseOneCompletions, 5, "P1 AI did not execute enough patterns in 120 seconds.");

        brain.SetAIEnabled(false);
        AssertAllHitboxesInactive(boss);
        brain.ForcePhaseTwo();
        status.SetCurrentHpForDebug(status.MaxHp * 0.3f);
        brain.SetTarget(target.transform);
        brain.SetAIEnabled(true);
        yield return null;

        float phaseTwoStartedAt = Time.time;
        float phaseTwoEnd = phaseTwoStartedAt + 180f;
        while (Time.time < phaseTwoEnd)
        {
            // Cycle close/far, left/right, and airborne target conditions so the
            // situational scorer is exercised instead of remaining in one range bucket.
            int situationIndex = Mathf.FloorToInt((Time.time - phaseTwoStartedAt) / 15f) % 6;
            bool useFarRange = situationIndex % 2 == 1;
            float direction = situationIndex < 3 ? 1f : -1f;
            float distance = useFarRange ? 5f : 1.2f;
            float targetX = Mathf.Clamp(boss.transform.position.x + direction * distance, -6f, 6f);
            bool airborne = situationIndex == 2 || situationIndex == 5;
            target.transform.position = new Vector3(targetX, airborne ? 2f : 0f, 0f);
            targetBody.linearVelocity = airborne ? Vector2.up : Vector2.zero;
            yield return null;
        }

        string[] phaseTwoPatternIds =
        {
            "P2_EnhancedCombo",
            "P2_DoubleCharge",
            "P2_ThunderUppercut",
            "P2_DarkGroundSlam",
            "P2_ShadowCombo",
            "P2_LightningCast",
            "P2_DarkWave",
            "P2_SoulBind",
            "P2_Ultimate"
        };
        int phaseTwoCompletions = phaseTwoPatternIds.Sum(phaseTwo.GetCompletedCount);
        int distinctPhaseTwoPatterns = phaseTwoPatternIds.Count(
            patternId => phaseTwo.GetCompletedCount(patternId) > 0);

        brain.SetAIEnabled(false);
        yield return null;

        TestContext.WriteLine(
            $"AISoak=P1:120s/P2:180s; P1Completed={phaseOneCompletions}; "
            + $"P2Completed={phaseTwoCompletions}; P2Distinct={distinctPhaseTwoPatterns}; "
            + $"Recent={string.Join(",", patternSystem.RecentPatternKeys)}; "
            + $"Distribution={string.Join(",", phaseTwoPatternIds.Select(patternId => $"{patternId}:{phaseTwo.GetCompletedCount(patternId)}"))}");
        Assert.GreaterOrEqual(phaseTwoCompletions, 8, "P2 AI did not execute enough patterns in 180 seconds.");
        Assert.GreaterOrEqual(distinctPhaseTwoPatterns, 7, "P2 AI pattern selection was not varied enough.");
        Assert.IsFalse(patternSystem.IsSpecialPatternRunning);
        Assert.IsFalse(phaseTwo.IsPatternRunning);
        Assert.AreEqual(0, phaseTwo.ActiveProjectileCount);
        Assert.AreEqual(0, phaseTwo.ActiveLingeringHazardCount);
        AssertAllHitboxesInactive(boss);
    }

    private static IEnumerator WaitUntilOrFail(
        System.Func<bool> condition,
        float timeoutSeconds,
        string failureMessage)
    {
        float deadline = Time.realtimeSinceStartup + Mathf.Max(0.05f, timeoutSeconds);

        while (!condition() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.IsTrue(condition(), failureMessage);
    }

    private void InstantiateBossAndTarget(Vector3 targetPosition)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab);
        boss = Object.Instantiate(prefab);
        boss.name = "HWJ_FighterBoss_Condition_Test";
        boss.transform.position = Vector3.zero;
        SetFastDialogueTiming(boss);
        target = new GameObject("HWJ_FighterBoss_Condition_Target");
        target.transform.position = targetPosition;
    }

    private void CreateStaticBlock(string objectName, Vector2 position, Vector2 size)
    {
        GameObject block = new GameObject(objectName);
        block.transform.position = position;
        BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
        collider.size = size;
        auxiliaryObjects.Add(block);
        Physics2D.SyncTransforms();
    }

    private static void AssertAllHitboxesInactive(GameObject owner)
    {
        HWJ_FighterBossHitboxSystem[] hitboxes =
            owner.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);
        Assert.Greater(hitboxes.Length, 0);

        for (int i = 0; i < hitboxes.Length; i++)
        {
            Assert.IsFalse(hitboxes[i].IsArmed, $"Hitbox remained armed: {hitboxes[i].name}");
            Assert.IsFalse(
                hitboxes[i].ColliderEnabled,
                $"Hitbox collider remained enabled: {hitboxes[i].name}");
        }
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Private field was not found: {fieldName}");
        field.SetValue(instance, value);
    }

    private static void SetFastDialogueTiming(GameObject bossObject)
    {
        HWJ_BossDialogueBubbleSystem dialogue =
            bossObject.GetComponent<HWJ_BossDialogueBubbleSystem>();

        if (dialogue == null)
        {
            return;
        }

        // Runtime timing remains unchanged; automated combat tests shorten only their own copy.
        SetPrivateField(dialogue, "sequenceLineDurationSeconds", 0.01f);
        SetPrivateField(dialogue, "secondsPerCharacter", 0f);
        SetPrivateField(dialogue, "maximumLineDurationSeconds", 0.01f);
        SetPrivateField(dialogue, "sequenceGapSeconds", 0f);
    }

    private static void AssertClipEventOrder(string clipPath, params string[] eventNames)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        Assert.NotNull(clip, $"Animation clip is missing: {clipPath}");
        Assert.IsFalse(clip.isLooping, $"Attack clip must not loop: {clipPath}");

        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        float previousTime = -1f;

        for (int i = 0; i < eventNames.Length; i++)
        {
            AnimationEvent animationEvent = events.FirstOrDefault(
                candidate => candidate.functionName == eventNames[i]);
            Assert.NotNull(
                animationEvent,
                $"{eventNames[i]} is missing from {clipPath}.");
            Assert.GreaterOrEqual(
                animationEvent.time,
                previousTime,
                $"{eventNames[i]} is out of order in {clipPath}.");
            previousTime = animationEvent.time;
        }
    }
}
#endif
