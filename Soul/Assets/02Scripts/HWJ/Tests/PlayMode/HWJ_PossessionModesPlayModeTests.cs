using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

/// <summary>
/// 생체 R 미니게임, 시체 E 즉시 빙의, 생체 정신력, 시체 부패, 영혼 게임오버 규칙을
/// 기획 요구사항 단위로 검증합니다.
/// </summary>
public class HWJ_PossessionModesPlayModeTests
{
    private readonly List<Object> createdObjects = new List<Object>();

#if ENABLE_INPUT_SYSTEM
    private Keyboard testKeyboard;
#endif

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

#if ENABLE_INPUT_SYSTEM
        if (testKeyboard != null && testKeyboard.added)
        {
            InputSystem.RemoveDevice(testKeyboard);
        }

        testKeyboard = null;
#endif

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.Destroy(createdObjects[i]);
            }
        }

        createdObjects.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator RMinigame_SuccessCostsTenEachTime_AndPreservesLiveBodyHp()
    {
        GameObject player = CreatePlayer("LivePossessionPlayer", 20f, 20f);
        GameObject enemy = CreateEnemy(
            "LivePossessionEnemy",
            true,
            100f,
            10f,
            100f,
            1f);
        enemy.transform.position = new Vector3(1f, 0f, 0f);
        HWJ_RuntimeStatusSystem enemyStatus = enemy.GetComponent<HWJ_RuntimeStatusSystem>();
        enemyStatus.SetCurrentHpForDebug(32f);
        Physics2D.SyncTransforms();

        HWJ_PossessionInteractionController interaction =
            player.GetComponent<HWJ_PossessionInteractionController>();
        HWJ_LivePossessionMinigameController minigame =
            player.GetComponent<HWJ_LivePossessionMinigameController>();
        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_LivePossessionMentalState enemyMental =
            enemy.GetComponent<HWJ_LivePossessionMentalState>();

        Assert.IsTrue(minigame.HasConfiguredUi);
        Assert.IsTrue(interaction.TryStartLivePossessionFromInput());
        Assert.IsTrue(minigame.IsRunning);
        Assert.IsTrue(minigame.IsUiVisible);

        for (int i = 0; i < 20 && minigame.CurrentGauge < 1f; i++)
        {
            minigame.AddGaugeFromUiButton();
        }

        // UI 입력은 게이지만 변경하고 성공 판정은 Update에서 처리하므로 다음 판정 프레임을 기다립니다.
        for (int i = 0; i < 10 && minigame.IsRunning; i++)
        {
            yield return null;
        }

        Assert.IsFalse(minigame.IsRunning);
        Assert.IsFalse(minigame.IsUiVisible);
        Assert.IsTrue(possession.IsLivePossessionActive);
        Assert.AreEqual(90f, enemyMental.CurrentMentalValue, 0.001f);
        Assert.AreEqual(
            32f,
            player.GetComponent<HWJ_RuntimeStatusSystem>().CurrentHp,
            0.001f);
        Assert.IsTrue(possession.TryGetPossessedBodyRuntimeState(out HWJ_PossessedBodyRuntimeState firstBody));
        Assert.AreEqual(32f, firstBody.CurrentHp, 0.001f);

        Assert.IsTrue(possession.TryExitPossessedBodyToSoul());
        yield return new WaitForSeconds(0.03f);
        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, player.GetComponent<HWJ_SoulSystem>().CurrentState);

        Assert.IsTrue(possession.CompleteLivePossessionFromMinigame(
            enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        Assert.AreEqual(80f, enemyMental.CurrentMentalValue, 0.001f);
    }

    [UnityTest]
    public IEnumerator RMinigame_ProgressesWithUnscaledTimeWhileWorldIsPaused()
    {
        GameObject player = CreatePlayer("PausedMinigamePlayer", 20f, 20f);
        GameObject enemy = CreateEnemy(
            "PausedMinigameEnemy",
            true,
            100f,
            10f,
            100f,
            1f);
        enemy.transform.position = new Vector3(1f, 0f, 0f);
        Physics2D.SyncTransforms();

        HWJ_LivePossessionMinigameController minigame =
            player.GetComponent<HWJ_LivePossessionMinigameController>();
        SetPrivateField(minigame, "pauseEntireWorldDuringMinigame", true);
        SetPrivateField(minigame, "progressLogIntervalSeconds", 0.1f);

        Assert.IsTrue(minigame.BeginMinigame(
            enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        float startGauge = minigame.CurrentGauge;

        Assert.AreEqual(0f, Time.timeScale, 0.001f);
        Assert.IsTrue(minigame.IsUiVisible);

        yield return new WaitForSecondsRealtime(0.25f);

        Assert.IsTrue(minigame.IsRunning);
        Assert.Less(minigame.CurrentGauge, startGauge);
        Assert.Greater(minigame.RemainingSeconds, 0f);

        minigame.enabled = false;
        yield return null;

        Assert.IsFalse(minigame.IsRunning);
        Assert.AreEqual(1f, Time.timeScale, 0.001f);
    }

    [UnityTest]
    public IEnumerator LiveMentalZero_ReleasesToSpirit_RestoresHostileWithRemainingHp_AndBlocksForever()
    {
        GameObject player = CreatePlayer("MentalReleasePlayer", 20f, 20f);
        GameObject enemy = CreateEnemy(
            "MentalReleaseEnemy",
            true,
            12f,
            10f,
            0.01f,
            2f);
        HWJ_RuntimeStatusSystem enemyStatus = enemy.GetComponent<HWJ_RuntimeStatusSystem>();
        enemyStatus.SetCurrentHpForDebug(27f);

        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();
        HWJ_LivePossessionMentalState enemyMental = enemy.GetComponent<HWJ_LivePossessionMentalState>();

        Assert.IsTrue(possession.CompleteLivePossessionFromMinigame(enemyResolver));
        Assert.AreEqual(2f, enemyMental.CurrentMentalValue, 0.001f);

        yield return new WaitForSeconds(0.04f);

        Assert.IsFalse(possession.HasActivePossessedBody);
        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, player.GetComponent<HWJ_SoulSystem>().CurrentState);
        Assert.IsTrue(enemy.activeSelf);
        Assert.AreEqual(27f, enemyStatus.CurrentHp, 0.001f);
        Assert.AreNotEqual(HWJ_RuntimeState.Dead, enemyStatus.CurrentState);
        Assert.IsTrue(enemy.GetComponent<HWJ_MonsterAISystem>().enabled);
        Assert.IsTrue(enemy.GetComponent<HWJ_EnemyAttackSystem>().enabled);
        Assert.IsTrue(enemyMental.IsPermanentlyBlocked);
        Assert.IsFalse(possession.CanStartLivePossessionMinigame(enemyResolver));
    }

    [UnityTest]
    public IEnumerator ECorpsePossession_IsImmediate_UsesDecay_AndGhostDoesNotDecay()
    {
        GameObject player = CreatePlayer("CorpsePossessionPlayer", 10f, 20f);
        HWJ_BodyDecaySystem decay = player.GetComponent<HWJ_BodyDecaySystem>();

        yield return new WaitForSeconds(0.04f);

        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, player.GetComponent<HWJ_SoulSystem>().CurrentState);
        Assert.IsFalse(decay.IsDecaying);
        Assert.AreEqual(0f, decay.CurrentDecayValue, 0.001f);

        GameObject corpse = CreateEnemy(
            "ImmediateCorpse",
            false,
            100f,
            10f,
            1f,
            1f);
        corpse.transform.position = new Vector3(1f, 0f, 0f);
        Physics2D.SyncTransforms();

        HWJ_PossessionInteractionController interaction =
            player.GetComponent<HWJ_PossessionInteractionController>();
        Assert.IsTrue(interaction.TryPossessCorpseFromInput());
        Assert.AreEqual(
            HWJ_PossessionKind.Corpse,
            player.GetComponent<HWJ_PossessionSystem>().CurrentPossessionKind);

        yield return new WaitForSeconds(0.04f);

        Assert.IsTrue(decay.IsDecaying);
        Assert.Greater(decay.CurrentDecayValue, 0f);
    }

    [UnityTest]
    public IEnumerator LiveBodyHpZero_EjectsPlayerToSpiritAndLeavesDeadMonster()
    {
        GameObject player = CreatePlayer("LiveHpZeroPlayer", 10f, 20f);
        GameObject enemy = CreateEnemy(
            "LiveHpZeroEnemy",
            true,
            100f,
            10f,
            100f,
            1f);
        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();
        HWJ_RootObjectDataResolver enemyResolver = enemy.GetComponent<HWJ_RootObjectDataResolver>();

        Assert.IsTrue(possession.CompleteLivePossessionFromMinigame(enemyResolver));
        HWJ_RuntimeStatusSystem playerStatus = player.GetComponent<HWJ_RuntimeStatusSystem>();
        float bodyHp = playerStatus.CurrentHp;
        playerStatus.ApplyDamage(bodyHp);

        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();

        for (int i = 0; i < 10 && soul.CurrentState != HWJ_SoulRuntimeState.Soul; i++)
        {
            yield return null;
        }

        Assert.IsFalse(possession.HasActivePossessedBody);
        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, soul.CurrentState);
        Assert.IsTrue(enemy.activeSelf);
        Assert.AreEqual(HWJ_RuntimeState.Dead, enemy.GetComponent<HWJ_RuntimeStatusSystem>().CurrentState);
        Assert.AreEqual(0f, enemy.GetComponent<HWJ_RuntimeStatusSystem>().CurrentHp, 0.001f);
        Assert.IsTrue(enemy.GetComponent<HWJ_LivePossessionMentalState>().BecameCorpseAfterLivePossession);
    }

    [UnityTest]
    public IEnumerator SpiritMentalZero_EntersGameOverDeadState()
    {
        GameObject player = CreatePlayer("SpiritGameOverPlayer", 10f, 20f);
        HWJ_RuntimeStatusSystem status = player.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();

        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, soul.CurrentState);
        Assert.IsTrue(status.TryApplySpiritMentalCost(status.CurrentSpiritMentalValue));
        yield return null;

        Assert.AreEqual(0f, status.CurrentSpiritMentalValue, 0.001f);
        Assert.AreEqual(HWJ_SoulRuntimeState.Dead, soul.CurrentState);
        Assert.AreEqual(HWJ_PlayerExistenceState.Dead, soul.CurrentExistenceState);
    }

    [UnityTest]
    public IEnumerator SpiritState_IgnoresCombatDamage_AndDiesOnlyWhenMentalReachesZero()
    {
        GameObject player = CreatePlayer("SpiritDamageImmunePlayer", 10f, 20f);
        HWJ_RuntimeStatusSystem status = player.GetComponent<HWJ_RuntimeStatusSystem>();
        HWJ_SoulSystem soul = player.GetComponent<HWJ_SoulSystem>();
        float mentalBeforeHit = status.CurrentSpiritMentalValue;

        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, soul.CurrentState);

        status.ApplyDamage(999f);
        yield return null;

        Assert.AreEqual(mentalBeforeHit, status.CurrentSpiritMentalValue, 0.001f);
        Assert.AreEqual(mentalBeforeHit, status.CurrentHp, 0.001f);
        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, soul.CurrentState);
        Assert.IsFalse(status.IsDead);

        Assert.IsTrue(status.TryApplySpiritMentalCost(mentalBeforeHit - 1f, "test_mental_cost"));
        Assert.AreEqual(1f, status.CurrentSpiritMentalValue, 0.001f);
        Assert.AreEqual(HWJ_SoulRuntimeState.Soul, soul.CurrentState);

        Assert.IsTrue(status.TryApplySpiritMentalCost(1f, "test_mental_depleted"));
        yield return null;

        Assert.AreEqual(0f, status.CurrentSpiritMentalValue, 0.001f);
        Assert.AreEqual(HWJ_SoulRuntimeState.Dead, soul.CurrentState);
        Assert.IsTrue(status.IsDead);
    }

    [UnityTest]
    public IEnumerator PossessionTransition_RestoresBodyPhysics_AndAllowsJump()
    {
        GameObject player = CreatePlayer("PossessionJumpPlayer", 20f, 20f);
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.gravityScale = 3f;
        HWJ_PlayerMovementSystem movement = player.AddComponent<HWJ_PlayerMovementSystem>();

        yield return new WaitForFixedUpdate();

        Assert.AreEqual(0f, playerBody.gravityScale, 0.001f);

        GameObject enemy = CreateEnemy(
            "PossessionJumpEnemy",
            true,
            100f,
            10f,
            100f,
            1f);
        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();

        Assert.IsTrue(possession.CompleteLivePossessionFromMinigame(
            enemy.GetComponent<HWJ_RootObjectDataResolver>()));

        // Movement observes the Soul -> Body transition in Update and restores body control immediately.
        yield return null;

        Assert.AreEqual(HWJ_SoulRuntimeState.Body, player.GetComponent<HWJ_SoulSystem>().CurrentState);
        Assert.AreEqual(3f, playerBody.gravityScale, 0.001f);

        movement.SetGrounded(true);
        MethodInfo tryJump = typeof(HWJ_PlayerMovementSystem).GetMethod(
            "TryJump",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            System.Type.EmptyTypes,
            null);

        Assert.NotNull(tryJump);
        Assert.IsTrue((bool)tryJump.Invoke(movement, null));
        Assert.Greater(playerBody.linearVelocity.y, 0f);
    }

    [UnityTest]
    public IEnumerator PossessionTransition_ReleasesMashGate_AndAcceptsNextSpaceJump()
    {
#if ENABLE_INPUT_SYSTEM
        GameObject player = CreatePlayer("PossessionPhysicalJumpPlayer", 20f, 20f);
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.gravityScale = 3f;
        player.AddComponent<HWJ_PlayerInputSystem>();
        HWJ_PlayerMovementSystem movement = player.AddComponent<HWJ_PlayerMovementSystem>();
        GameObject ground = Track(new GameObject("PossessionPhysicalJumpGround"));
        ground.transform.position = new Vector3(0f, -1f, 0f);
        BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(10f, 1f);
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();

        GameObject enemy = CreateEnemy(
            "PossessionPhysicalJumpEnemy",
            true,
            100f,
            10f,
            100f,
            1f);
        HWJ_PossessionSystem possession = player.GetComponent<HWJ_PossessionSystem>();

        testKeyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Space));
        InputSystem.Update();

        Assert.IsTrue(possession.CompleteLivePossessionFromMinigame(
            enemy.GetComponent<HWJ_RootObjectDataResolver>()));
        InvokePrivate(movement, "ObserveSoulStateTransition");
        Assert.IsTrue(GetPrivateField<bool>(movement, "waitForJumpReleaseAfterBodyEntry"));

        // Releasing the minigame's fixed Space key must clear the possession gate.
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
        InputSystem.Update();
        InvokePrivate(movement, "UpdateJumpReleaseGate");
        Assert.IsFalse(GetPrivateField<bool>(movement, "waitForJumpReleaseAfterBodyEntry"));

        movement.SetGrounded(true);
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
        InvokePrivate(movement, "Update");

        Assert.Greater(playerBody.linearVelocity.y, 0f);
        Assert.AreEqual(0, GetPrivateField<int>(movement, "usedDoubleJumpCount"));
#else
        yield return null;
        Assert.Ignore("The Unity Input System is not enabled for this project.");
#endif
    }

    private GameObject CreatePlayer(string name, float maxDecay, float spiritMental)
    {
        HWJ_PlayerTypeDataSO playerData = Track(
            ScriptableObject.CreateInstance<HWJ_PlayerTypeDataSO>());
        SetPrivateField(playerData, "objectType", HWJ_ObjectType.Player);
        playerData.Possession.canPossess = true;
        playerData.Possession.possessionRange = 2.5f;
        playerData.BodyDecay.initialDecayValue = 0f;
        playerData.BodyDecay.maxDecayValue = maxDecay;
        playerData.BodyDecay.decayTickSeconds = 0.01f;
        playerData.BodyDecay.decayAmountPerTick = 1f;
        playerData.BodyDecay.startDecayOnEnterBody = true;
        playerData.BodyDecay.enterSoulStateWhenMaxed = true;
        playerData.SoulState.startAsSoul = true;
        playerData.SoulState.startSoulDeadlineImmediately = false;

        GameObject player = Track(new GameObject(name));
        player.SetActive(false);
        player.tag = "Player";
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        player.AddComponent<BoxCollider2D>();
        HWJ_RootObjectDataResolver resolver = player.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(CreateRootData(
            name + ".data",
            HWJ_ObjectType.Player,
            HWJ_Faction.Player,
            spiritMental,
            playerData));
        player.AddComponent<HWJ_RuntimeObjectContext>();
        player.AddComponent<HWJ_RuntimeStatusSystem>();
        HWJ_SoulSystem soul = player.AddComponent<HWJ_SoulSystem>();
        SetPrivateField(soul, "bodyToSoulTransitionSeconds", 0.01f);
        player.AddComponent<HWJ_PossessedBodySystem>();
        player.AddComponent<HWJ_PossessionSystem>();
        player.AddComponent<HWJ_PossessionMentalSystem>();
        player.AddComponent<HWJ_CollapseSystem>();
        player.AddComponent<HWJ_LivePossessionMinigameController>();
        player.AddComponent<HWJ_PossessionInteractionController>();
        player.SetActive(true);
        return player;
    }

    private GameObject CreateEnemy(
        string name,
        bool alive,
        float maxMental,
        float successCost,
        float drainInterval,
        float drainAmount)
    {
        HWJ_EnemyTypeDataSO enemyData = Track(
            ScriptableObject.CreateInstance<HWJ_EnemyTypeDataSO>());
        SetPrivateField(enemyData, "objectType", HWJ_ObjectType.Enemy);
        enemyData.Role.leavesCorpseOnDeath = true;
        enemyData.Role.isPossessableBody = true;
        enemyData.PossessionBody.canBePossessed = true;
        enemyData.PossessionBody.requiresDefeatedState = false;
        enemyData.PossessionBody.transfersControlToBody = true;
        enemyData.PossessionBody.loadsBodyStatsToPlayer = true;
        enemyData.PossessionBody.livePossessionMaxMental = maxMental;
        enemyData.PossessionBody.livePossessionMentalCostOnSuccess = successCost;
        enemyData.PossessionBody.livePossessionMentalDrainInterval = drainInterval;
        enemyData.PossessionBody.livePossessionMentalDrainAmount = drainAmount;

        GameObject enemy = Track(new GameObject(name));
        enemy.SetActive(false);
        Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        enemy.AddComponent<BoxCollider2D>();
        HWJ_RootObjectDataResolver resolver = enemy.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(CreateRootData(
            name + ".data",
            HWJ_ObjectType.Enemy,
            HWJ_Faction.Monster,
            50f,
            enemyData));
        enemy.AddComponent<HWJ_RuntimeObjectContext>();
        HWJ_RuntimeStatusSystem status = enemy.AddComponent<HWJ_RuntimeStatusSystem>();
        enemy.AddComponent<HWJ_PossessionBodyState>();

        if (alive)
        {
            enemy.AddComponent<HWJ_EnemyNavigationSystem>();
            enemy.AddComponent<HWJ_EnemyAttackSystem>();
            enemy.AddComponent<HWJ_MonsterAISystem>();
            enemy.AddComponent<HWJ_LivePossessionMentalState>();
        }

        enemy.SetActive(true);

        if (!alive)
        {
            status.SetCurrentHpForDebug(0f);
            status.SetState(HWJ_RuntimeState.Dead);
        }

        return enemy;
    }

    private HWJ_RootObjectDataSO CreateRootData(
        string objectId,
        HWJ_ObjectType objectType,
        HWJ_Faction faction,
        float maxHp,
        HWJ_ObjectTypeDataSO typeData)
    {
        HWJ_RootObjectDataSO rootData = Track(
            ScriptableObject.CreateInstance<HWJ_RootObjectDataSO>());
        SetPrivateField(rootData, "identity", new HWJ_IdentityData
        {
            objectId = objectId,
            displayName = objectId,
            objectType = objectType,
            faction = faction
        });
        SetPrivateField(rootData, "status", new HWJ_StatusData
        {
            maxHp = maxHp,
            moveSpeed = 4f,
            attackPower = 5f,
            attackSpeed = 1f,
            bodyWeight = 1f
        });
        SetPrivateField(rootData, "damage", new HWJ_DamageData
        {
            damageType = HWJ_DamageType.Physical,
            baseDamage = 5f
        });
        SetPrivateField(rootData, "receivedDamage", new HWJ_ReceivedDamageData());
        SetPrivateField(rootData, "selectedTypeData", typeData);
        return rootData;
    }

    private T Track<T>(T createdObject) where T : Object
    {
        createdObjects.Add(createdObject);
        return createdObject;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        System.Type currentType = target.GetType();

        while (field == null && currentType != null)
        {
            field = currentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            currentType = currentType.BaseType;
        }

        Assert.NotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = FindPrivateField(target, fieldName);
        Assert.NotNull(field, $"Missing field {fieldName} on {target.GetType().Name}.");
        return (T)field.GetValue(target);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, $"Missing method {methodName} on {target.GetType().Name}.");
        method.Invoke(target, null);
    }

    private static FieldInfo FindPrivateField(object target, string fieldName)
    {
        FieldInfo field = null;
        System.Type currentType = target.GetType();

        while (field == null && currentType != null)
        {
            field = currentType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            currentType = currentType.BaseType;
        }

        return field;
    }
}
