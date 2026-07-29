using System.Collections;
using System.Reflection;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
public class HWJ_FighterBossFirstPassPlayModeTests
{
    private const string BossPrefabPath =
        "Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab";
    private const string BossRootDataPath =
        "Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset";

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
        HWJ_GameplayEvents.ClearAllSubscribers();

        HWJ_FighterBossComboSystem[] combos = Object.FindObjectsByType<HWJ_FighterBossComboSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < combos.Length; i++)
        {
            if (combos[i] != null)
            {
                Object.Destroy(combos[i].gameObject);
            }
        }

        HWJ_RootObjectDataResolver[] resolvers = Object.FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null)
            {
                Object.Destroy(resolvers[i].gameObject);
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator P1AttackCombo_ExecutesFiveTimes_AndNeverLeavesHitboxEnabled()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab, $"Boss prefab is missing: {BossPrefabPath}");

        GameObject boss = Object.Instantiate(prefab);
        boss.name = "FighterBoss_ComboFiveRun_Test";
        boss.transform.position = Vector3.zero;

        Rigidbody2D bossBody = boss.GetComponent<Rigidbody2D>();
        bossBody.gravityScale = 0f;
        bossBody.constraints = RigidbodyConstraints2D.FreezeAll;

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossComboSystem combo = boss.GetComponent<HWJ_FighterBossComboSystem>();
        HWJ_FighterBossHitboxSystem hitbox =
            boss.GetComponentInChildren<HWJ_FighterBossHitboxSystem>(true);
        Animator animator = boss.GetComponent<Animator>();

        Assert.NotNull(brain);
        Assert.NotNull(combo);
        Assert.NotNull(hitbox);
        Assert.NotNull(animator);
        Assert.NotNull(animator.runtimeAnimatorController);

        // 자동 AI가 검증용 강제 실행과 경쟁하지 않도록 Brain만 멈추고 실제 Animator/Event/Hitbox 경로는 유지합니다.
        brain.enabled = false;

        HWJ_RootObjectDataSO targetRootData;
        HWJ_PlayerTypeDataSO targetTypeData;
        GameObject target = CreateDurablePlayerTarget(out targetRootData, out targetTypeData);
        target.transform.position = new Vector3(1.05f, 0.95f, 0f);
        Physics2D.SyncTransforms();
        yield return null;

        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        float startingHp = targetStatus.CurrentHp;

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            Assert.IsTrue(combo.TryStartCombo(target.transform), $"Combo run {runIndex + 1} did not start.");
            Assert.IsTrue(combo.LastComboUsedAnimator, "Combo must use the generated Animator state and Animation Events.");

            float timeout = Time.time + 3f;

            while (combo.IsPatternRunning && Time.time < timeout)
            {
                yield return null;
            }

            Assert.IsFalse(combo.IsPatternRunning, $"Combo run {runIndex + 1} exceeded the watchdog.");
            Assert.AreEqual(runIndex + 1, combo.CompletedComboCount);
            Assert.IsFalse(hitbox.IsArmed, $"Combo run {runIndex + 1} left the Hitbox armed.");
            Assert.IsFalse(hitbox.ColliderEnabled, $"Combo run {runIndex + 1} left Collider2D enabled.");
            Assert.IsFalse(combo.IsTelegraphVisible, $"Combo run {runIndex + 1} left the telegraph visible.");
            Assert.AreEqual((runIndex + 1) * 3, hitbox.TotalArmCount);

            yield return null;

            Assert.IsFalse(hitbox.ColliderEnabled, "Hitbox re-enabled on the frame after attack completion.");
        }

        Assert.AreEqual(15, hitbox.TotalSuccessfulHits, "Each of five Combo runs must land three independent strikes.");
        Assert.Less(targetStatus.CurrentHp, startingHp, "The real CombatSystem damage path was not reached.");
        Assert.IsFalse(hitbox.ColliderEnabled);
        TestContext.WriteLine(
            $"ComboRuns={combo.CompletedComboCount}; "
            + $"HitboxWindows={hitbox.TotalArmCount}; "
            + $"SuccessfulHits={hitbox.TotalSuccessfulHits}; "
            + $"HitboxEnabledAfterEnd={hitbox.ColliderEnabled}");

        Object.Destroy(boss);
        Object.Destroy(target);
        Object.Destroy(targetRootData);
        Object.Destroy(targetTypeData);
        yield return null;
    }

    [UnityTest]
    public IEnumerator P1AttackCharge_ExecutesFiveTimes_StopsAtWall_AndClearsHitbox()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab, $"Boss prefab is missing: {BossPrefabPath}");

        GameObject boss = Object.Instantiate(prefab);
        boss.name = "FighterBoss_ChargeFiveRun_Test";
        boss.transform.position = Vector3.zero;

        Rigidbody2D bossBody = boss.GetComponent<Rigidbody2D>();
        bossBody.gravityScale = 0f;
        bossBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossChargeSystem charge = boss.GetComponent<HWJ_FighterBossChargeSystem>();
        Assert.NotNull(brain);
        Assert.NotNull(charge);
        Assert.NotNull(charge.ChargeHitbox);
        brain.enabled = false;

        HWJ_RootObjectDataSO targetRootData;
        HWJ_PlayerTypeDataSO targetTypeData;
        GameObject target = CreateDurablePlayerTarget(out targetRootData, out targetTypeData);
        target.transform.position = new Vector3(2.5f, 0.95f, 0f);

        GameObject wall = new GameObject("Charge_Test_Wall");
        wall.transform.position = new Vector3(6f, 1f, 0f);
        BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(1f, 4f);

        yield return null;

        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        float startingHp = targetStatus.CurrentHp;

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            boss.transform.position = Vector3.zero;
            bossBody.position = Vector2.zero;
            bossBody.linearVelocity = Vector2.zero;
            target.transform.position = new Vector3(2.5f, 0.95f, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(charge.TryStartCharge(target.transform), $"Charge run {runIndex + 1} did not start.");
            Assert.IsTrue(
                charge.LastChargeUsedAnimator,
                "Charge must use the generated Animator state and Animation Events.");

            float timeout = Time.time + 3f;

            while (charge.IsPatternRunning && Time.time < timeout)
            {
                yield return null;
            }

            TestContext.WriteLine(
                $"ChargeRun={runIndex + 1}; "
                + $"BossX={bossBody.position.x:F3}; "
                + $"Distance={charge.LastChargeDistance:F3}; "
                + $"StoppedByWall={charge.LastChargeStoppedByWall}; "
                + $"Completed={charge.CompletedChargeCount}; "
                + $"HitboxWindows={charge.ChargeHitbox.TotalArmCount}; "
                + $"SuccessfulHits={charge.ChargeHitbox.TotalSuccessfulHits}");
            Assert.IsFalse(charge.IsPatternRunning, $"Charge run {runIndex + 1} exceeded the watchdog.");
            Assert.AreEqual(runIndex + 1, charge.CompletedChargeCount);
            Assert.AreEqual(runIndex + 1, charge.ChargeHitbox.TotalArmCount);
            Assert.IsFalse(charge.ChargeHitbox.IsArmed);
            Assert.IsFalse(charge.ChargeHitbox.ColliderEnabled);
            Assert.IsFalse(charge.IsTelegraphVisible);
            Assert.IsTrue(charge.LastChargeStoppedByWall, "Charge must stop at the wall collider.");
            Assert.Greater(charge.LastChargeDistance, 3f);
            Assert.Less(charge.LastChargeDistance, 6f);
            yield return null;
            Assert.IsFalse(charge.ChargeHitbox.ColliderEnabled);
        }

        Assert.AreEqual(5, charge.ChargeHitbox.TotalSuccessfulHits);
        Assert.Less(targetStatus.CurrentHp, startingHp);
        TestContext.WriteLine(
            $"ChargeRuns={charge.CompletedChargeCount}; "
            + $"HitboxWindows={charge.ChargeHitbox.TotalArmCount}; "
            + $"SuccessfulHits={charge.ChargeHitbox.TotalSuccessfulHits}; "
            + $"StoppedByWall={charge.LastChargeStoppedByWall}; "
            + $"HitboxEnabledAfterEnd={charge.ChargeHitbox.ColliderEnabled}");

        Object.Destroy(boss);
        Object.Destroy(target);
        Object.Destroy(wall);
        Object.Destroy(targetRootData);
        Object.Destroy(targetTypeData);
        yield return null;
    }

    [UnityTest]
    public IEnumerator P1AttackUppercut_ExecutesFiveTimes_HitsOnce_AndClearsHitbox()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab, $"Boss prefab is missing: {BossPrefabPath}");

        GameObject boss = Object.Instantiate(prefab);
        boss.name = "FighterBoss_UppercutFiveRun_Test";
        boss.transform.position = Vector3.zero;

        Rigidbody2D bossBody = boss.GetComponent<Rigidbody2D>();
        bossBody.gravityScale = 0f;
        bossBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossUppercutSystem uppercut =
            boss.GetComponent<HWJ_FighterBossUppercutSystem>();
        Assert.NotNull(brain);
        Assert.NotNull(uppercut);
        Assert.NotNull(uppercut.UppercutHitbox);
        brain.enabled = false;

        HWJ_RootObjectDataSO targetRootData;
        HWJ_PlayerTypeDataSO targetTypeData;
        GameObject target = CreateDurablePlayerTarget(out targetRootData, out targetTypeData);
        target.transform.position = new Vector3(0.9f, 1.45f, 0f);
        Physics2D.SyncTransforms();
        yield return null;

        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        float startingHp = targetStatus.CurrentHp;

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            Assert.IsTrue(
                uppercut.TryStartUppercut(target.transform),
                $"Uppercut run {runIndex + 1} did not start.");
            Assert.IsTrue(
                uppercut.LastUppercutUsedAnimator,
                "Uppercut must use the generated Animator state and Animation Events.");

            float timeout = Time.time + 2.5f;

            while (uppercut.IsPatternRunning && Time.time < timeout)
            {
                yield return null;
            }

            TestContext.WriteLine(
                $"UppercutRun={runIndex + 1}; "
                + $"Completed={uppercut.CompletedUppercutCount}; "
                + $"HitboxWindows={uppercut.UppercutHitbox.TotalArmCount}; "
                + $"SuccessfulHits={uppercut.UppercutHitbox.TotalSuccessfulHits}");
            Assert.IsFalse(
                uppercut.IsPatternRunning,
                $"Uppercut run {runIndex + 1} exceeded the watchdog.");
            Assert.AreEqual(runIndex + 1, uppercut.CompletedUppercutCount);
            Assert.AreEqual(runIndex + 1, uppercut.UppercutHitbox.TotalArmCount);
            Assert.AreEqual(runIndex + 1, uppercut.UppercutHitbox.TotalSuccessfulHits);
            Assert.IsFalse(uppercut.UppercutHitbox.IsArmed);
            Assert.IsFalse(uppercut.UppercutHitbox.ColliderEnabled);
            Assert.IsFalse(uppercut.IsTelegraphVisible);
            yield return null;
            Assert.IsFalse(uppercut.UppercutHitbox.ColliderEnabled);
        }

        Assert.Less(targetStatus.CurrentHp, startingHp);
        TestContext.WriteLine(
            $"UppercutRuns={uppercut.CompletedUppercutCount}; "
            + $"HitboxWindows={uppercut.UppercutHitbox.TotalArmCount}; "
            + $"SuccessfulHits={uppercut.UppercutHitbox.TotalSuccessfulHits}; "
            + $"HitboxEnabledAfterEnd={uppercut.UppercutHitbox.ColliderEnabled}");

        Object.Destroy(boss);
        Object.Destroy(target);
        Object.Destroy(targetRootData);
        Object.Destroy(targetTypeData);
        yield return null;
    }

    [UnityTest]
    public IEnumerator P1AttackGroundSlam_ExecutesFiveTimes_SpawnsPulse_AndClearsHitbox()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab, $"Boss prefab is missing: {BossPrefabPath}");

        GameObject boss = Object.Instantiate(prefab);
        boss.name = "FighterBoss_GroundSlamFiveRun_Test";
        boss.transform.position = Vector3.zero;

        Rigidbody2D bossBody = boss.GetComponent<Rigidbody2D>();
        bossBody.gravityScale = 0f;
        bossBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossGroundSlamSystem groundSlam =
            boss.GetComponent<HWJ_FighterBossGroundSlamSystem>();
        Assert.NotNull(brain);
        Assert.NotNull(groundSlam);
        Assert.NotNull(groundSlam.SlamHitbox);
        brain.enabled = false;

        HWJ_RootObjectDataSO targetRootData;
        HWJ_PlayerTypeDataSO targetTypeData;
        GameObject target = CreateDurablePlayerTarget(out targetRootData, out targetTypeData);
        target.transform.position = new Vector3(2f, 0.7f, 0f);
        Physics2D.SyncTransforms();
        yield return null;

        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        float startingHp = targetStatus.CurrentHp;

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            Assert.IsTrue(
                groundSlam.TryStartGroundSlam(target.transform),
                $"GroundSlam run {runIndex + 1} did not start.");
            Assert.IsTrue(
                groundSlam.LastGroundSlamUsedAnimator,
                "GroundSlam must use the generated Animator state and Animation Events.");

            float timeout = Time.time + 2.7f;

            while (groundSlam.IsPatternRunning && Time.time < timeout)
            {
                yield return null;
            }

            TestContext.WriteLine(
                $"GroundSlamRun={runIndex + 1}; "
                + $"Completed={groundSlam.CompletedGroundSlamCount}; "
                + $"GroundPulses={groundSlam.SpawnedGroundHazardCount}; "
                + $"HitboxWindows={groundSlam.SlamHitbox.TotalArmCount}; "
                + $"SuccessfulHits={groundSlam.SlamHitbox.TotalSuccessfulHits}");
            Assert.IsFalse(
                groundSlam.IsPatternRunning,
                $"GroundSlam run {runIndex + 1} exceeded the watchdog.");
            Assert.AreEqual(runIndex + 1, groundSlam.CompletedGroundSlamCount);
            Assert.AreEqual(runIndex + 1, groundSlam.SpawnedGroundHazardCount);
            Assert.AreEqual(runIndex + 1, groundSlam.SlamHitbox.TotalArmCount);
            Assert.AreEqual(runIndex + 1, groundSlam.SlamHitbox.TotalSuccessfulHits);
            Assert.IsFalse(groundSlam.SlamHitbox.IsArmed);
            Assert.IsFalse(groundSlam.SlamHitbox.ColliderEnabled);
            Assert.IsFalse(groundSlam.IsTelegraphVisible);
            yield return null;
            Assert.IsFalse(groundSlam.SlamHitbox.ColliderEnabled);
        }

        Assert.Less(targetStatus.CurrentHp, startingHp);
        TestContext.WriteLine(
            $"GroundSlamRuns={groundSlam.CompletedGroundSlamCount}; "
            + $"GroundPulses={groundSlam.SpawnedGroundHazardCount}; "
            + $"SuccessfulHits={groundSlam.SlamHitbox.TotalSuccessfulHits}; "
            + $"HitboxEnabledAfterEnd={groundSlam.SlamHitbox.ColliderEnabled}");

        Object.Destroy(boss);
        Object.Destroy(target);
        Object.Destroy(targetRootData);
        Object.Destroy(targetTypeData);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PhaseTransition_ExecutesFiveTimes_RefillsSecondBar_AndCompletesAnimationSteps()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        HWJ_RootObjectDataSO sourceRootData =
            AssetDatabase.LoadAssetAtPath<HWJ_RootObjectDataSO>(BossRootDataPath);
        Assert.NotNull(prefab);
        Assert.NotNull(sourceRootData);
        Assert.IsInstanceOf<HWJ_BossTypeDataSO>(sourceRootData.SelectedTypeData);

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            HWJ_RootObjectDataSO runtimeRootData = Object.Instantiate(sourceRootData);
            HWJ_BossTypeDataSO runtimeBossType =
                Object.Instantiate((HWJ_BossTypeDataSO)sourceRootData.SelectedTypeData);
            runtimeBossType.FSM.phaseTransitionSeconds = 0.2f;
            SetPrivateField(runtimeRootData, "selectedTypeData", runtimeBossType);

            GameObject boss = Object.Instantiate(prefab);
            boss.name = $"FighterBoss_PhaseTransition_Run_{runIndex + 1}";
            Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            HWJ_RootObjectDataResolver resolver = boss.GetComponent<HWJ_RootObjectDataResolver>();
            HWJ_RuntimeStatusSystem status = boss.GetComponent<HWJ_RuntimeStatusSystem>();
            HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
            Animator animator = boss.GetComponent<Animator>();
            brain.enabled = false;
            resolver.SetRootObjectData(runtimeRootData);
            status.RefreshCurrentHpFromData(true);
            SetPrivateField(brain, "autoFindPlayerTarget", false);
            SetPrivateField(brain, "useTwoBarPhaseHealth", true);
            SetPrivateField(brain, "fighterPhase", HWJ_FighterBossPhase.Phase1);
            SetPrivateField(brain, "currentPhaseNumber", 1);
            SetPrivateField(brain, "currentPhaseIndex", 0);
            brain.enabled = true;
            yield return null;

            float phaseOneMaxHp = status.MaxHp;
            status.ApplyDamage(phaseOneMaxHp);

            Assert.AreEqual(HWJ_FighterBossPhase.Transition, brain.FighterPhase);
            Assert.AreEqual(HWJ_BossFSMState.PhaseTransition, brain.CurrentState);
            Assert.AreEqual(0, brain.TransitionAnimationStep);
            Assert.AreEqual(0f, status.CurrentHp, 0.001f);
            Assert.IsFalse(status.IsDead);
            Assert.IsTrue(status.IsTemporarilyInvincible);

            float timeout = Time.time + 2f;

            while (brain.FighterPhase == HWJ_FighterBossPhase.Transition
                && Time.time < timeout)
            {
                yield return null;
            }

            TestContext.WriteLine(
                $"PhaseTransitionRun={runIndex + 1}; "
                + $"Phase={brain.FighterPhase}; "
                + $"AnimationStep={brain.TransitionAnimationStep}; "
                + $"Completed={brain.CompletedTwoBarTransitionCount}; "
                + $"RefilledHp={status.CurrentHp:F1}");
            Assert.AreEqual(HWJ_FighterBossPhase.Phase2, brain.FighterPhase);
            Assert.AreEqual(HWJ_BossFSMState.Idle, brain.CurrentState);
            Assert.AreEqual(2, brain.CurrentPhaseNumber);
            Assert.AreEqual(4, brain.TransitionAnimationStep);
            Assert.AreEqual(1, brain.CompletedTwoBarTransitionCount);
            Assert.AreEqual(status.MaxHp, status.CurrentHp, 0.001f);
            Assert.AreEqual(phaseOneMaxHp, status.MaxHp, 0.001f);
            Assert.IsFalse(status.IsDead);
            Assert.IsTrue(
                animator.HasState(0, Animator.StringToHash("Base Layer.Phase2_Start")));

            Object.Destroy(boss);
            Object.Destroy(runtimeRootData);
            Object.Destroy(runtimeBossType);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator P2EnhancedCombo_ExecutesFiveTimes_WithFourHitWindows()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Enhanced_Combo",
            new Vector3(1.1f, 0.95f, 0f),
            4,
            0,
            false);
    }

    [UnityTest]
    public IEnumerator P2EnhancedCharge_ExecutesFiveTimes_AndStopsAtWall()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Enhanced_Charge",
            new Vector3(2.5f, 0.95f, 0f),
            1,
            0,
            true);
    }

    [UnityTest]
    public IEnumerator P2EnhancedUppercut_ExecutesFiveTimes_WithOneHitWindow()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Enhanced_Uppercut",
            new Vector3(0.9f, 1.5f, 0f),
            1,
            0,
            false);
    }

    [UnityTest]
    public IEnumerator P2EnhancedGroundSlam_ExecutesFiveTimes_WithTwoGroundPulses()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Enhanced_GroundSlam",
            new Vector3(2f, 0.7f, 0f),
            2,
            2,
            false);
    }

    [UnityTest]
    public IEnumerator P2Shockwave_ExecutesFiveTimes_WithProjectileHook()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Attack_Shockwave",
            new Vector3(2.5f, 0.8f, 0f),
            1,
            0,
            false,
            expectedProjectilesPerRun: 1);
    }

    [UnityTest]
    public IEnumerator P2AerialDive_ExecutesFiveTimes_TeleportsAndSlamsTarget()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Attack_AerialDive",
            new Vector3(3f, 0.7f, 0f),
            1,
            1,
            false,
            expectedTeleportOutPerRun: 1,
            expectedTeleportInPerRun: 1,
            expectLandingAtTarget: true);
    }

    [UnityTest]
    public IEnumerator P2CrossSlash_ExecutesFiveTimes_WithTwoHitWindows()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Attack_CrossSlash",
            new Vector3(1.1f, 1f, 0f),
            2,
            0,
            false);
    }

    [UnityTest]
    public IEnumerator P2PhantomRush_ExecutesFiveTimes_TeleportsMovesAndHitsThreeTimes()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Attack_PhantomRush",
            new Vector3(2.5f, 1f, 0f),
            3,
            0,
            false,
            expectedTeleportOutPerRun: 1,
            expectedTeleportInPerRun: 1,
            expectMovement: true);
    }

    [UnityTest]
    public IEnumerator P2Execution_ExecutesFiveTimes_WithLongTelegraphAndImpact()
    {
        yield return RunPhaseTwoPatternFiveTimes(
            "P2_Attack_Execution",
            new Vector3(1f, 1f, 0f),
            1,
            0,
            false);
    }

    [UnityTest]
    public IEnumerator FinalDeath_ExecutesFiveTimes_CancelsCombatAndFreezesTerminalBody()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab);

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            GameObject boss = Object.Instantiate(prefab);
            boss.name = $"FighterBoss_FinalDeath_Run_{runIndex + 1}";
            Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            HWJ_RuntimeStatusSystem status = boss.GetComponent<HWJ_RuntimeStatusSystem>();
            HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
            HWJ_FighterBossDeathSystem death = boss.GetComponent<HWJ_FighterBossDeathSystem>();
            Animator animator = boss.GetComponent<Animator>();
            Assert.NotNull(status);
            Assert.NotNull(brain);
            Assert.NotNull(death);
            SetPrivateField(brain, "autoFindPlayerTarget", false);
            SetPrivateField(brain, "useTwoBarPhaseHealth", true);
            SetPrivateField(brain, "fighterPhase", HWJ_FighterBossPhase.Phase2);
            SetPrivateField(brain, "currentPhaseNumber", 2);
            yield return null;

            status.ApplyDamage(status.MaxHp);

            Assert.AreEqual(HWJ_FighterBossPhase.Dead, brain.FighterPhase);
            Assert.AreEqual(HWJ_BossFSMState.Dead, brain.CurrentState);
            Assert.IsTrue(status.IsDead);
            Assert.IsTrue(death.IsDeathRunning);
            Assert.AreEqual(1, death.BeginDeathCount);
            Assert.IsFalse(death.BodyColliderEnabled);

            float timeout = Time.time + 3f;

            while (!death.IsDeathComplete && Time.time < timeout)
            {
                yield return null;
            }

            HWJ_FighterBossHitboxSystem[] hitboxes =
                boss.GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

            for (int hitboxIndex = 0; hitboxIndex < hitboxes.Length; hitboxIndex++)
            {
                Assert.IsFalse(hitboxes[hitboxIndex].IsArmed);
                Assert.IsFalse(hitboxes[hitboxIndex].ColliderEnabled);
            }

            TestContext.WriteLine(
                $"FinalDeathRun={runIndex + 1}; "
                + $"Begin={death.BeginDeathCount}; "
                + $"Completed={death.CompletedDeathCount}; "
                + $"CameraHook={death.CameraShakeHookCount}; "
                + $"SfxHook={death.SfxHookCount}; "
                + $"BodySimulated={death.BodySimulated}; "
                + $"Hitboxes={hitboxes.Length}");
            Assert.IsTrue(death.IsDeathComplete);
            Assert.IsFalse(death.IsDeathRunning);
            Assert.AreEqual(1, death.CompletedDeathCount);
            Assert.AreEqual(1, death.CameraShakeHookCount);
            Assert.AreEqual(1, death.SfxHookCount);
            Assert.IsFalse(death.BodyColliderEnabled);
            Assert.IsFalse(death.BodySimulated);
            Assert.IsTrue(
                animator.HasState(0, Animator.StringToHash("Base Layer.P2_Death")));

            Object.Destroy(boss);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator TwoBarHealth_Phase1TransitionsAndRefills_ThenPhase2Dies()
    {
        HWJ_BossTypeDataSO bossTypeData = ScriptableObject.CreateInstance<HWJ_BossTypeDataSO>();
        SetPrivateField(bossTypeData, "objectType", HWJ_ObjectType.Boss);
        bossTypeData.FSM.autoStartWhenPlayerEntersRoom = false;
        bossTypeData.FSM.phaseTransitionSeconds = 0.15f;

        HWJ_RootObjectDataSO rootData = CreateRootObjectData(
            HWJ_ObjectType.Boss,
            HWJ_Faction.Monster,
            100f,
            10f,
            5f,
            bossTypeData);

        GameObject boss = new GameObject("FighterBoss_TwoBar_Test");
        boss.SetActive(false);
        HWJ_RootObjectDataResolver resolver = boss.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(rootData);
        boss.AddComponent<HWJ_RuntimeObjectContext>();
        HWJ_RuntimeStatusSystem status = boss.AddComponent<HWJ_RuntimeStatusSystem>();
        HWJ_BossBrainSystem brain = boss.AddComponent<HWJ_BossBrainSystem>();
        SetPrivateField(brain, "dataResolver", resolver);
        SetPrivateField(brain, "runtimeStatus", status);
        SetPrivateField(brain, "autoFindPlayerTarget", false);
        SetPrivateField(brain, "useTwoBarPhaseHealth", true);
        SetPrivateField(brain, "fighterPhase", HWJ_FighterBossPhase.Phase1);
        SetPrivateField(status, "bossBrain", brain);
        boss.SetActive(true);

        yield return null;

        Assert.AreEqual(100f, status.CurrentHp, 0.001f);
        status.ApplyDamage(100f);

        Assert.AreEqual(HWJ_FighterBossPhase.Transition, brain.FighterPhase);
        Assert.AreEqual(HWJ_BossFSMState.PhaseTransition, brain.CurrentState);
        Assert.AreEqual(0f, status.CurrentHp, 0.001f);
        Assert.IsFalse(status.IsDead, "Phase1 depletion must not invoke final death.");

        float transitionTimeout = Time.time + 2f;

        while (brain.FighterPhase == HWJ_FighterBossPhase.Transition && Time.time < transitionTimeout)
        {
            yield return null;
        }

        Assert.AreEqual(HWJ_FighterBossPhase.Phase2, brain.FighterPhase);
        Assert.AreEqual(2, brain.CurrentPhaseNumber);
        Assert.AreEqual(status.MaxHp, status.CurrentHp, 0.001f);
        Assert.IsFalse(status.IsDead);

        yield return new WaitForSeconds(0.1f);
        status.ApplyDamage(status.MaxHp);
        yield return null;

        Assert.AreEqual(HWJ_FighterBossPhase.Dead, brain.FighterPhase);
        Assert.AreEqual(HWJ_BossFSMState.Dead, brain.CurrentState);
        Assert.IsTrue(status.IsDead, "Phase2 depletion must invoke final death.");
        TestContext.WriteLine(
            "PhaseFlow=Phase1->Transition->Phase2->Dead; "
            + $"Phase2Refill={status.MaxHp:0.###}; FinalDead={status.IsDead}");

        Object.Destroy(boss);
        Object.Destroy(rootData);
        Object.Destroy(bossTypeData);
        yield return null;
    }

    private static IEnumerator RunPhaseTwoPatternFiveTimes(
        string patternId,
        Vector3 targetPosition,
        int expectedHitWindowsPerRun,
        int expectedGroundHazardsPerRun,
        bool useWall,
        int expectedProjectilesPerRun = 0,
        int expectedTeleportOutPerRun = 0,
        int expectedTeleportInPerRun = 0,
        int expectedSuccessfulHitsPerRun = -1,
        bool expectMovement = false,
        bool expectLandingAtTarget = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Assert.NotNull(prefab);

        GameObject boss = Object.Instantiate(prefab);
        boss.name = $"FighterBoss_{patternId}_FiveRun_Test";
        boss.transform.position = Vector3.zero;
        Rigidbody2D bossBody = boss.GetComponent<Rigidbody2D>();
        bossBody.gravityScale = 0f;
        bossBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        HWJ_BossBrainSystem brain = boss.GetComponent<HWJ_BossBrainSystem>();
        HWJ_FighterBossPhaseTwoPatternSystem phaseTwo =
            boss.GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        Assert.NotNull(brain);
        Assert.NotNull(phaseTwo);
        SetPrivateField(brain, "fighterPhase", HWJ_FighterBossPhase.Phase2);
        SetPrivateField(brain, "currentPhaseNumber", 2);
        brain.enabled = false;

        HWJ_FighterBossHitboxSystem hitbox = phaseTwo.GetHitbox(patternId);
        Assert.NotNull(hitbox, $"{patternId} has no wired hitbox profile.");

        HWJ_RootObjectDataSO targetRootData;
        HWJ_PlayerTypeDataSO targetTypeData;
        GameObject target = CreateDurablePlayerTarget(out targetRootData, out targetTypeData);
        target.transform.position = targetPosition;

        GameObject wall = null;

        if (useWall)
        {
            wall = new GameObject($"{patternId}_Test_Wall");
            wall.transform.position = new Vector3(6f, 1f, 0f);
            BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(1f, 4f);
        }

        Physics2D.SyncTransforms();
        yield return null;

        HWJ_RuntimeStatusSystem targetStatus = target.GetComponent<HWJ_RuntimeStatusSystem>();
        float startingHp = targetStatus.CurrentHp;

        for (int runIndex = 0; runIndex < 5; runIndex++)
        {
            boss.transform.position = Vector3.zero;
            bossBody.position = Vector2.zero;
            bossBody.linearVelocity = Vector2.zero;
            target.transform.position = targetPosition;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(
                phaseTwo.TryStartPattern(patternId, target.transform),
                $"{patternId} run {runIndex + 1} did not start.");
            Assert.IsTrue(
                phaseTwo.LastPatternUsedAnimator,
                $"{patternId} must use its Animator state and Animation Events.");

            float timeout = Time.time + 3f;

            while (phaseTwo.IsPatternRunning && Time.time < timeout)
            {
                yield return null;
            }

            int expectedWindows = (runIndex + 1) * expectedHitWindowsPerRun;
            int expectedHazards = (runIndex + 1) * expectedGroundHazardsPerRun;
            int successfulHitsPerRun = expectedSuccessfulHitsPerRun >= 0
                ? expectedSuccessfulHitsPerRun
                : expectedHitWindowsPerRun;
            int expectedSuccessfulHits = (runIndex + 1) * successfulHitsPerRun;
            int expectedProjectiles = (runIndex + 1) * expectedProjectilesPerRun;
            int expectedTeleportOuts = (runIndex + 1) * expectedTeleportOutPerRun;
            int expectedTeleportIns = (runIndex + 1) * expectedTeleportInPerRun;
            TestContext.WriteLine(
                $"Pattern={patternId}; Run={runIndex + 1}; "
                + $"Completed={phaseTwo.GetCompletedCount(patternId)}; "
                + $"HitboxWindows={hitbox.TotalArmCount}; "
                + $"SuccessfulHits={hitbox.TotalSuccessfulHits}; "
                + $"GroundHazards={phaseTwo.GetGroundHazardCount(patternId)}; "
                + $"Projectiles={phaseTwo.GetProjectileCount(patternId)}; "
                + $"TeleportOut={phaseTwo.GetTeleportOutCount(patternId)}; "
                + $"TeleportIn={phaseTwo.GetTeleportInCount(patternId)}; "
                + $"MovementDistance={phaseTwo.LastMovementDistance:F3}; "
                + $"StoppedByWall={phaseTwo.LastMovementStoppedByWall}");
            Assert.IsFalse(
                phaseTwo.IsPatternRunning,
                $"{patternId} run {runIndex + 1} exceeded the watchdog.");
            Assert.AreEqual(runIndex + 1, phaseTwo.GetCompletedCount(patternId));
            Assert.AreEqual(expectedWindows, hitbox.TotalArmCount);
            Assert.AreEqual(expectedSuccessfulHits, hitbox.TotalSuccessfulHits);
            Assert.AreEqual(expectedHazards, phaseTwo.GetGroundHazardCount(patternId));
            Assert.AreEqual(expectedProjectiles, phaseTwo.GetProjectileCount(patternId));
            Assert.AreEqual(expectedTeleportOuts, phaseTwo.GetTeleportOutCount(patternId));
            Assert.AreEqual(expectedTeleportIns, phaseTwo.GetTeleportInCount(patternId));
            Assert.AreEqual(runIndex + 1, phaseTwo.CameraShakeHookCount);
            Assert.AreEqual(runIndex + 1, phaseTwo.SfxHookCount);
            Assert.IsFalse(hitbox.IsArmed);
            Assert.IsFalse(hitbox.ColliderEnabled);
            Assert.IsFalse(phaseTwo.IsTelegraphVisible(patternId));

            if (useWall)
            {
                Assert.IsTrue(phaseTwo.LastMovementStoppedByWall);
                Assert.Greater(phaseTwo.LastMovementDistance, 3f);
                Assert.Less(phaseTwo.LastMovementDistance, 6f);
            }
            else if (expectMovement)
            {
                Assert.Greater(phaseTwo.LastMovementDistance, 0.5f);
            }

            if (expectLandingAtTarget)
            {
                Assert.AreEqual(
                    targetPosition.x,
                    phaseTwo.LastTeleportLandingPosition.x,
                    0.05f);
            }

            Assert.IsTrue(bossBody.simulated);

            yield return null;
            Assert.IsFalse(hitbox.ColliderEnabled);
        }

        Assert.Less(targetStatus.CurrentHp, startingHp);
        Object.Destroy(boss);
        Object.Destroy(target);
        Object.Destroy(wall);
        Object.Destroy(targetRootData);
        Object.Destroy(targetTypeData);
        yield return null;
    }

    private static GameObject CreateDurablePlayerTarget(
        out HWJ_RootObjectDataSO rootData,
        out HWJ_PlayerTypeDataSO typeData)
    {
        typeData = ScriptableObject.CreateInstance<HWJ_PlayerTypeDataSO>();
        SetPrivateField(typeData, "objectType", HWJ_ObjectType.Player);
        rootData = CreateRootObjectData(
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            50000f,
            1f,
            1f,
            typeData);

        GameObject target = new GameObject("FighterBoss_Combo_Target");
        target.SetActive(false);
        HWJ_RootObjectDataResolver resolver = target.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(rootData);
        target.AddComponent<HWJ_RuntimeObjectContext>();
        target.AddComponent<HWJ_RuntimeStatusSystem>();
        target.AddComponent<HWJ_CombatSystem>();
        BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 1.2f);
        target.SetActive(true);
        return target;
    }

    private static HWJ_RootObjectDataSO CreateRootObjectData(
        HWJ_ObjectType objectType,
        HWJ_Faction faction,
        float maxHp,
        float attackPower,
        float baseDamage,
        HWJ_ObjectTypeDataSO typeData)
    {
        HWJ_RootObjectDataSO rootData = ScriptableObject.CreateInstance<HWJ_RootObjectDataSO>();
        SetPrivateField(rootData, "identity", new HWJ_IdentityData
        {
            objectId = $"test.{objectType}",
            displayName = objectType.ToString(),
            objectType = objectType,
            faction = faction
        });
        SetPrivateField(rootData, "status", new HWJ_StatusData
        {
            maxHp = maxHp,
            attackPower = attackPower,
            moveSpeed = 5f,
            defense = 0f,
            attackSpeed = 1f,
            bodyWeight = 1f
        });
        SetPrivateField(rootData, "damage", new HWJ_DamageData
        {
            damageType = HWJ_DamageType.Physical,
            baseDamage = baseDamage,
            sameTargetHitCooldownSeconds = 0f
        });
        SetPrivateField(rootData, "receivedDamage", new HWJ_ReceivedDamageData
        {
            damageMultiplier = 1f,
            invincibleSecondsAfterHit = 0f
        });
        SetPrivateField(rootData, "selectedTypeData", typeData);
        return rootData;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        System.Type currentType = target.GetType();

        while (currentType != null && field == null)
        {
            field = currentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            currentType = currentType.BaseType;
        }

        Assert.NotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
#endif
