using System.Collections;
using UnityEngine;

/// <summary>
/// Custom pattern executor for reusable physical mid bosses.
/// Visual art and animation can be added later; this system owns timing, movement,
/// temporary warnings, summons, damage checks, and groggy entry.
/// </summary>
public class HWJ_MidBossPatternSystem : MonoBehaviour, HWJ_IBossSpecialPatternExecutor
{
    [Header("References")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Executor")]
    [SerializeField] private string executorKey = "mid_boss_physical";

    [Header("Common Timing")]
    [SerializeField] private float dashSeconds = 0.18f;
    [SerializeField] private float slashWarningSeconds = 0.35f;
    [SerializeField] private float recoverySeconds = 0.25f;
    [SerializeField] private float horizontalSlashRange = 3f;
    [SerializeField] private float verticalSlashRadius = 1.35f;
    [SerializeField] private float shockwaveHeight = 1.2f;
    [SerializeField] private float warningLineWidth = 0.06f;
    [SerializeField] private Color warningColor = new Color(1f, 0.1f, 0.05f, 0.85f);

    [Header("Pattern 1 Summon")]
    [SerializeField] private float pattern1HpLossIntervalRatio = 0.15f;
    [SerializeField] private int pattern1SummonCount = 4;
    [SerializeField] private float pattern1WhistleSeconds = 2f;
    [SerializeField] private float pattern1SummonIntervalSeconds = 0.15f;
    [SerializeField] private float pattern1EdgePadding = 1f;
    [SerializeField] private string pattern1SummonSaveIdPrefix = "mid_boss_summon";
    [SerializeField] private GameObject[] possessableMonsterPrefabs;
    [SerializeField] private HWJ_RootObjectDataSO[] possessableMonsterRootObjects;

    [Header("Pattern 3")]
    [SerializeField] private float pattern3ChargeSeconds = 1.5f;
    [SerializeField] private float pattern3WaveHeight = 1.5f;
    [SerializeField] private float pattern3DamageMultiplier = 1.25f;

    [Header("Pattern 4")]
    [SerializeField] private float pattern4AimSeconds = 0.65f;
    [SerializeField] private float pattern4FixedMaxHpDamageRatio = 0.4f;
    [SerializeField] private float pattern4GroggySeconds = 3f;

    [Header("Pattern 5")]
    [SerializeField] private float pattern5BuffHoldSeconds = 0.6f;
    [SerializeField] private float pattern5ComboIntervalSeconds = 0.35f;
    [SerializeField] private float pattern5ShockwaveWidthRatio = 0.5f;

    [Header("Pattern 6")]
    [SerializeField] private float pattern6VanishDelaySeconds = 2f;
    [SerializeField] private float pattern6BehindOffset = 1.2f;
    [SerializeField] private float pattern6ReturnDelaySeconds = 0.25f;

    [Header("Pattern 7")]
    [SerializeField] private float pattern7JumpHeight = 4f;
    [SerializeField] private float pattern7AirHoldSeconds = 0.65f;
    [SerializeField] private float pattern7ImpactRadius = 1.6f;
    [SerializeField] private float pattern7ShockwaveWidthRatio = 0.5f;

    private Coroutine activePatternRoutine;
    private int pendingPattern1Charges;
    private float nextPattern1HpRatioThreshold;
    private float trackedMaxHp;
    private int summonedMonsterSequence;

    public string ExecutorKey => executorKey;
    public bool IsPatternRunning => activePatternRoutine != null;

    private void Awake()
    {
        CacheReferences();
        ResetPattern1Tracking();
    }

    public bool CanUsePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        CacheReferences();

        if (!MatchesPatternExecutor(pattern)
            || activePatternRoutine != null
            || runtimeStatus != null && runtimeStatus.IsDead)
        {
            return false;
        }

        RefreshPattern1Charges();

        if (pattern.PatternNumber == 1)
        {
            return pendingPattern1Charges > 0 && !HasPossessableCorpseInRoom();
        }

        return pattern.PatternNumber >= 2 && pattern.PatternNumber <= 7;
    }

    public bool TryExecutePattern(HWJ_BossPatternDataSO pattern, Transform target)
    {
        if (!CanUsePattern(pattern, target))
        {
            return false;
        }

        switch (pattern.PatternNumber)
        {
            case 1:
                pendingPattern1Charges = Mathf.Max(0, pendingPattern1Charges - 1);
                activePatternRoutine = StartCoroutine(Pattern1SummonRoutine(target));
                return true;
            case 2:
                activePatternRoutine = StartCoroutine(Pattern2DashDoubleSlashRoutine(target));
                return true;
            case 3:
                activePatternRoutine = StartCoroutine(Pattern3SlashAndFullWaveRoutine(target));
                return true;
            case 4:
                activePatternRoutine = StartCoroutine(Pattern4FixedDamageDashRoutine(target));
                return true;
            case 5:
                activePatternRoutine = StartCoroutine(Pattern5RedSwordComboRoutine(target));
                return true;
            case 6:
                activePatternRoutine = StartCoroutine(Pattern6VanishBackstabRoutine(target));
                return true;
            case 7:
                activePatternRoutine = StartCoroutine(Pattern7JumpSlamRoutine(target));
                return true;
            default:
                return false;
        }
    }

    public void CancelActivePattern()
    {
        if (activePatternRoutine != null)
        {
            StopCoroutine(activePatternRoutine);
            activePatternRoutine = null;
        }

        SetSpriteVisible(true);
        skillActionSystem?.CancelCurrentAction();
        StopMovement();
    }

    private IEnumerator Pattern1SummonRoutine(Transform target)
    {
        float totalSeconds = Mathf.Max(0f, dashSeconds)
            + Mathf.Max(0f, pattern1WhistleSeconds)
            + Mathf.Max(0, pattern1SummonCount) * Mathf.Max(0f, pattern1SummonIntervalSeconds)
            + Mathf.Max(0f, recoverySeconds);
        skillActionSystem?.BlockNavigationForSkill(totalSeconds, true);

        Vector3 edgePosition = ResolveFarEdgePosition(target, pattern1EdgePadding);
        yield return DashToPosition(edgePosition, dashSeconds);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern1WhistleSeconds));

        int spawnCount = Mathf.Max(0, pattern1SummonCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnPossessableMonster(i);

            if (i < spawnCount - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, pattern1SummonIntervalSeconds));
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern2DashDoubleSlashRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            dashSeconds + slashWarningSeconds * 2f + 0.5f + recoverySeconds,
            true);

        yield return DashToTargetSide(target, 0.9f);
        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);

        yield return new WaitForSeconds(0.5f);
        ShowCircleWarning(transform.position, verticalSlashRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, verticalSlashRadius, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern3SlashAndFullWaveRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            dashSeconds * 2f + slashWarningSeconds + pattern3ChargeSeconds + recoverySeconds,
            true);

        yield return DashToTargetSide(target, 0.75f);
        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);

        yield return DashToPosition(ResolveFarEdgePosition(target, pattern1EdgePadding), dashSeconds);
        ShowRoomWideHorizontalWarning(pattern3WaveHeight, pattern3ChargeSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern3ChargeSeconds));
        DamageTargetIfInsideRoomHorizontalBand(target, pattern3WaveHeight, pattern3DamageMultiplier);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern4FixedDamageDashRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(pattern4AimSeconds + dashSeconds + pattern4GroggySeconds, true);

        float direction = GetDirectionToTarget(target);
        HWJ_SkillWarningIndicator.ShowArrowPath(
            transform.position,
            direction,
            Mathf.Max(1f, horizontalSlashRange * 1.5f),
            0.8f,
            pattern4AimSeconds,
            warningColor,
            warningLineWidth);

        yield return new WaitForSeconds(Mathf.Max(0.01f, pattern4AimSeconds));
        yield return DashToTargetSide(target, 0.35f);
        DamageTargetByFixedMaxHpRatio(target, pattern4FixedMaxHpDamageRatio);
        bossBrain?.ForceGroggy(pattern4GroggySeconds);
        activePatternRoutine = null;
    }

    private IEnumerator Pattern5RedSwordComboRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern5BuffHoldSeconds + pattern5ComboIntervalSeconds * 4f + recoverySeconds,
            true);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern5BuffHoldSeconds));

        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern5ComboIntervalSeconds));

        ShowCircleWarning(transform.position, verticalSlashRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, verticalSlashRadius, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern5ComboIntervalSeconds));

        ShowCircleWarning(transform.position, pattern7ImpactRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideCircle(target, transform.position, pattern7ImpactRadius, 1f);

        ShowHalfRoomShockwaveWarning(pattern5ShockwaveWidthRatio, shockwaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideHalfRoomShockwave(target, pattern5ShockwaveWidthRatio, shockwaveHeight, 1.15f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern6VanishBackstabRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(
            pattern6VanishDelaySeconds + dashSeconds + slashWarningSeconds + pattern6ReturnDelaySeconds + recoverySeconds,
            true);

        Vector3 returnPosition = transform.position;
        yield return new WaitForSeconds(Mathf.Max(0f, pattern6VanishDelaySeconds));
        SetSpriteVisible(false);
        SetPosition(ResolveBehindTargetPosition(target));
        SetSpriteVisible(true);

        ShowForwardSlashWarning(target, horizontalSlashRange, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideForwardRange(target, horizontalSlashRange, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, pattern6ReturnDelaySeconds));
        yield return DashToPosition(returnPosition, dashSeconds);
        ShowRoomWideHorizontalWarning(pattern3WaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideRoomHorizontalBand(target, pattern3WaveHeight, 1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private IEnumerator Pattern7JumpSlamRoutine(Transform target)
    {
        skillActionSystem?.BlockNavigationForSkill(pattern7AirHoldSeconds + slashWarningSeconds + recoverySeconds, true);

        Vector3 startPosition = transform.position;
        Vector3 airPosition = startPosition + Vector3.up * Mathf.Max(0f, pattern7JumpHeight);
        SetPosition(airPosition);
        yield return new WaitForSeconds(Mathf.Max(0f, pattern7AirHoldSeconds));

        Vector3 landingPosition = target != null
            ? new Vector3(target.position.x, startPosition.y, startPosition.z)
            : startPosition;
        ShowCircleWarning(landingPosition, pattern7ImpactRadius, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        SetPosition(landingPosition);
        DamageTargetIfInsideCircle(target, landingPosition, pattern7ImpactRadius, 1.2f);

        ShowHalfRoomShockwaveWarning(pattern7ShockwaveWidthRatio, shockwaveHeight, slashWarningSeconds);
        yield return new WaitForSeconds(Mathf.Max(0.01f, slashWarningSeconds));
        DamageTargetIfInsideHalfRoomShockwave(target, pattern7ShockwaveWidthRatio, shockwaveHeight, 1.1f);

        yield return new WaitForSeconds(Mathf.Max(0f, recoverySeconds));
        activePatternRoutine = null;
    }

    private void RefreshPattern1Charges()
    {
        if (runtimeStatus == null || runtimeStatus.MaxHp <= 0f)
        {
            return;
        }

        if (!Mathf.Approximately(trackedMaxHp, runtimeStatus.MaxHp) || nextPattern1HpRatioThreshold <= 0f)
        {
            ResetPattern1Tracking();
        }

        float interval = Mathf.Clamp(pattern1HpLossIntervalRatio, 0.01f, 1f);
        float currentRatio = Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp);

        while (nextPattern1HpRatioThreshold > 0f && currentRatio <= nextPattern1HpRatioThreshold)
        {
            pendingPattern1Charges++;
            nextPattern1HpRatioThreshold -= interval;
        }
    }

    private void ResetPattern1Tracking()
    {
        trackedMaxHp = runtimeStatus != null ? runtimeStatus.MaxHp : 0f;
        float interval = Mathf.Clamp(pattern1HpLossIntervalRatio, 0.01f, 1f);
        nextPattern1HpRatioThreshold = 1f - interval;
        pendingPattern1Charges = 0;
    }

    private bool MatchesPatternExecutor(HWJ_BossPatternDataSO pattern)
    {
        if (pattern == null || !pattern.UseCustomPatternExecutor)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(pattern.CustomPatternExecutorKey)
            || string.IsNullOrWhiteSpace(executorKey)
            || pattern.CustomPatternExecutorKey == executorKey;
    }

    private bool HasPossessableCorpseInRoom()
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (IsPossessableCorpse(resolvers[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPossessableCorpse(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.gameObject == gameObject)
        {
            return false;
        }

        if (resolver.ObjectType != HWJ_ObjectType.Enemy && resolver.ObjectType != HWJ_ObjectType.Boss)
        {
            return false;
        }

        if (!IsInsideBossRoom(resolver.transform.position))
        {
            return false;
        }

        if (!TryGetPossessionBodyData(resolver, out HWJ_PossessionData possessionData)
            || !possessionData.canBePossessed)
        {
            return false;
        }

        HWJ_PossessionBodyState bodyState = resolver.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState != null && bodyState.IsConsumed)
        {
            return false;
        }

        if (!possessionData.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null && targetStatus.IsDead;
    }

    private bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver resolver,
        out HWJ_PossessionData possessionData)
    {
        possessionData = null;

        if (resolver == null)
        {
            return false;
        }

        if (resolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            possessionData = enemyData.PossessionBody;
        }
        else if (resolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionData = bossData.PossessionBody;
        }

        return possessionData != null;
    }

    private void SpawnPossessableMonster(int spawnIndex)
    {
        HWJ_RootObjectDataSO rootData = GetPossessableMonsterRootObject(spawnIndex);
        GameObject prefab = GetPossessableMonsterPrefab(spawnIndex, rootData);

        if (prefab == null)
        {
            return;
        }

        GameObject instance = HWJ_GameAccess.HasManager
            ? HWJ_GameAccess.Spawn(prefab, transform.position, Quaternion.identity)
            : Instantiate(prefab, transform.position, Quaternion.identity);

        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = instance.GetComponent<HWJ_RootObjectDataResolver>();

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        EnsureSummonedMonsterRuntimeComponents(instance, rootData, spawnIndex);

        HWJ_PossessionBodyState bodyState = instance.GetComponent<HWJ_PossessionBodyState>();

        if (bodyState != null)
        {
            bodyState.ResetConsumed();
        }

        HWJ_RuntimeStatusSystem spawnedStatus = instance.GetComponent<HWJ_RuntimeStatusSystem>();

        if (spawnedStatus != null)
        {
            spawnedStatus.RefreshCurrentHpFromData(true);
        }

        AssignSummonedMonsterSaveIdentity(instance, resolver, rootData, spawnIndex);

        HWJ_MonsterAISystem monsterAI = instance.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.RefreshData();
            monsterAI.SetTarget(FindPlayerTarget());
        }

        HWJ_EnemyNavigationSystem navigation = instance.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.SetTarget(FindPlayerTarget());
        }

        HWJ_EnemyAttackSystem attackSystem = instance.GetComponent<HWJ_EnemyAttackSystem>();

        if (attackSystem != null)
        {
            attackSystem.SetTarget(FindPlayerTarget());
        }
    }

    private GameObject GetPossessableMonsterPrefab(int index, HWJ_RootObjectDataSO rootData)
    {
        if (possessableMonsterPrefabs != null && possessableMonsterPrefabs.Length > 0)
        {
            int safeIndex = Mathf.Abs(index) % possessableMonsterPrefabs.Length;
            return possessableMonsterPrefabs[safeIndex];
        }

        return rootData != null && rootData.Model != null
            ? rootData.Model.modelPrefab
            : null;
    }

    private HWJ_RootObjectDataSO GetPossessableMonsterRootObject(int index)
    {
        if (possessableMonsterRootObjects == null || possessableMonsterRootObjects.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Abs(index) % possessableMonsterRootObjects.Length;
        return possessableMonsterRootObjects[safeIndex];
    }

    private void EnsureSummonedMonsterRuntimeComponents(
        GameObject instance,
        HWJ_RootObjectDataSO rootData,
        int spawnIndex)
    {
        if (instance == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = EnsureComponent<HWJ_RootObjectDataResolver>(instance);

        if (resolver != null && rootData != null)
        {
            resolver.SetRootObjectData(rootData);
        }

        Rigidbody2D summonedBody = EnsureComponent<Rigidbody2D>(instance);

        if (summonedBody != null)
        {
            summonedBody.freezeRotation = true;
            summonedBody.gravityScale = Mathf.Approximately(summonedBody.gravityScale, 0f)
                ? 1f
                : summonedBody.gravityScale;
        }

        if (instance.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D boxCollider = instance.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(0.8f, 1f);
        }

        EnsureComponent<HWJ_PossessionBodyState>(instance);
        EnsureComponent<HWJ_KnockbackSystem>(instance);
        EnsureComponent<HWJ_RuntimeStatusSystem>(instance);
        EnsureComponent<HWJ_CombatSystem>(instance);
        EnsureComponent<HWJ_CombatExecutionSystem>(instance);
        EnsureComponent<HWJ_SkillActionSystem>(instance);
        EnsureComponent<HWJ_CharacterMotionSystem>(instance);
        EnsureComponent<HWJ_EnemyAttackSystem>(instance);
        EnsureComponent<HWJ_EnemyNavigationSystem>(instance);
        EnsureComponent<HWJ_MonsterAISystem>(instance);
    }

    private void AssignSummonedMonsterSaveIdentity(
        GameObject instance,
        HWJ_RootObjectDataResolver resolver,
        HWJ_RootObjectDataSO rootData,
        int spawnIndex)
    {
        if (instance == null)
        {
            return;
        }

        string rootObjectId = ResolveRootObjectId(rootData, resolver);

        if (string.IsNullOrEmpty(rootObjectId))
        {
            return;
        }

        HWJ_RuntimeSaveIdentity saveIdentity = EnsureComponent<HWJ_RuntimeSaveIdentity>(instance);

        if (saveIdentity == null || saveIdentity.HasStableInstanceId)
        {
            return;
        }

        string bossId = ResolveRootObjectId(
            null,
            bossBrain != null ? bossBrain.GetComponent<HWJ_RootObjectDataResolver>() : GetComponent<HWJ_RootObjectDataResolver>());
        string sourceId = string.IsNullOrEmpty(bossId) ? pattern1SummonSaveIdPrefix : bossId + "_" + pattern1SummonSaveIdPrefix;
        string stableId = $"{sourceId}_{summonedMonsterSequence:000}_{Mathf.Max(0, spawnIndex):00}";
        summonedMonsterSequence++;
        saveIdentity.SetManualIdentity(stableId, rootObjectId);
    }

    private static string ResolveRootObjectId(
        HWJ_RootObjectDataSO rootData,
        HWJ_RootObjectDataResolver resolver)
    {
        if (rootData != null && rootData.Identity != null && !string.IsNullOrEmpty(rootData.Identity.objectId))
        {
            return rootData.Identity.objectId;
        }

        return resolver != null && resolver.Identity != null
            ? resolver.Identity.objectId
            : null;
    }

    private static T EnsureComponent<T>(GameObject instance) where T : Component
    {
        if (instance == null)
        {
            return null;
        }

        T component = instance.GetComponent<T>();
        return component != null ? component : instance.AddComponent<T>();
    }

    private IEnumerator DashToTargetSide(Transform target, float stopOffset)
    {
        if (target == null)
        {
            yield break;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 destination = target.position - Vector3.right * direction * Mathf.Max(0f, stopOffset);
        destination.y = transform.position.y;
        destination.z = transform.position.z;
        yield return DashToPosition(destination, dashSeconds);
    }

    private IEnumerator DashToPosition(Vector3 destination, float seconds)
    {
        Vector3 start = transform.position;
        float duration = Mathf.Max(0.01f, seconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetPosition(Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetPosition(destination);
        StopMovement();
    }

    private Vector3 ResolveFarEdgePosition(Transform target, float padding)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float leftX = roomCenter.x - roomSize.x * 0.5f + Mathf.Max(0f, padding);
        float rightX = roomCenter.x + roomSize.x * 0.5f - Mathf.Max(0f, padding);
        float targetX = target != null ? target.position.x : roomCenter.x;
        float destinationX = Mathf.Abs(targetX - leftX) > Mathf.Abs(targetX - rightX) ? leftX : rightX;
        return new Vector3(destinationX, transform.position.y, transform.position.z);
    }

    private Vector3 ResolveBehindTargetPosition(Transform target)
    {
        if (target == null)
        {
            return transform.position;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 position = target.position + Vector3.right * direction * Mathf.Max(0f, pattern6BehindOffset);
        position.y = transform.position.y;
        position.z = transform.position.z;
        return position;
    }

    private void ShowForwardSlashWarning(Transform target, float range, float seconds)
    {
        float direction = GetDirectionToTarget(target);
        HWJ_SkillWarningIndicator.ShowForwardArc(
            transform.position,
            direction,
            Mathf.Max(0.1f, range),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowCircleWarning(Vector3 center, float radius, float seconds)
    {
        HWJ_SkillWarningIndicator.ShowCircle(
            center,
            Mathf.Max(0.1f, radius),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowRoomWideHorizontalWarning(float height, float seconds)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        HWJ_SkillWarningIndicator.ShowRectangle(
            new Vector3(roomCenter.x, transform.position.y, transform.position.z),
            new Vector2(Mathf.Max(1f, roomSize.x), Mathf.Max(0.1f, height)),
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void ShowHalfRoomShockwaveWarning(float widthRatio, float height, float seconds)
    {
        Rect rect = ResolveHalfRoomShockwaveRect(widthRatio, height);
        HWJ_SkillWarningIndicator.ShowRectangle(
            rect.center,
            rect.size,
            seconds,
            warningColor,
            warningLineWidth);
    }

    private void DamageTargetIfInsideForwardRange(Transform target, float range, float damageMultiplier)
    {
        if (target == null)
        {
            return;
        }

        float direction = GetDirectionToTarget(target);
        Vector3 delta = target.position - transform.position;

        if (Mathf.Sign(delta.x) != Mathf.Sign(direction)
            || Mathf.Abs(delta.x) > Mathf.Max(0.1f, range)
            || Mathf.Abs(delta.y) > Mathf.Max(0.1f, verticalSlashRadius))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideCircle(Transform target, Vector3 center, float radius, float damageMultiplier)
    {
        if (target == null || Vector2.Distance(target.position, center) > Mathf.Max(0.1f, radius))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideRoomHorizontalBand(Transform target, float height, float damageMultiplier)
    {
        if (target == null || Mathf.Abs(target.position.y - transform.position.y) > Mathf.Max(0.1f, height) * 0.5f)
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetIfInsideHalfRoomShockwave(Transform target, float widthRatio, float height, float damageMultiplier)
    {
        if (target == null)
        {
            return;
        }

        Rect rect = ResolveHalfRoomShockwaveRect(widthRatio, height);

        if (!rect.Contains((Vector2)target.position))
        {
            return;
        }

        DamageTarget(target, damageMultiplier);
    }

    private void DamageTargetByFixedMaxHpRatio(Transform target, float damageRatio)
    {
        HWJ_RuntimeStatusSystem targetStatus = target != null
            ? target.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;

        if (targetStatus == null || targetStatus.IsDead || targetStatus.MaxHp <= 0f)
        {
            return;
        }

        targetStatus.ApplyDamage(targetStatus.MaxHp * Mathf.Clamp01(damageRatio), combatSystem, null);
    }

    private void DamageTarget(Transform target, float damageMultiplier)
    {
        HWJ_RootObjectDataResolver targetResolver = target != null
            ? target.GetComponentInParent<HWJ_RootObjectDataResolver>()
            : null;

        combatSystem?.TryDealDamageTo(targetResolver, damageMultiplier, out _);
    }

    private Rect ResolveHalfRoomShockwaveRect(float widthRatio, float height)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        float direction = Mathf.Sign(GetDirectionToTarget(bossBrain != null ? bossBrain.Target : null));
        float width = Mathf.Max(1f, roomSize.x * Mathf.Clamp01(widthRatio));
        float centerX = transform.position.x + direction * width * 0.5f;
        return new Rect(
            centerX - width * 0.5f,
            transform.position.y - Mathf.Max(0.1f, height) * 0.5f,
            width,
            Mathf.Max(0.1f, height));
    }

    private bool IsInsideBossRoom(Vector3 position)
    {
        Vector2 roomCenter = GetRoomCenter();
        Vector2 roomSize = GetRoomSize();
        Vector2 halfSize = roomSize * 0.5f;
        return Mathf.Abs(position.x - roomCenter.x) <= halfSize.x
            && Mathf.Abs(position.y - roomCenter.y) <= halfSize.y;
    }

    private Vector2 GetRoomCenter()
    {
        return bossBrain != null ? bossBrain.BossRoomCenter : (Vector2)transform.position;
    }

    private Vector2 GetRoomSize()
    {
        return bossBrain != null ? bossBrain.BossRoomSize : new Vector2(28f, 14f);
    }

    private float GetDirectionToTarget(Transform target)
    {
        if (target == null)
        {
            return transform.localScale.x < 0f ? -1f : 1f;
        }

        float deltaX = target.position.x - transform.position.x;
        return Mathf.Abs(deltaX) <= 0.001f ? 1f : Mathf.Sign(deltaX);
    }

    private Transform FindPlayerTarget()
    {
        if (bossBrain != null && bossBrain.Target != null)
        {
            return bossBrain.Target;
        }

        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void SetPosition(Vector3 position)
    {
        if (body != null)
        {
            body.position = (Vector2)position;
            return;
        }

        transform.position = position;
    }

    private void StopMovement()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = visible;
            }
        }
    }

    private void CacheReferences()
    {
        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }
}
