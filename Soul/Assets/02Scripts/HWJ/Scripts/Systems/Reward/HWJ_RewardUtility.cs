using UnityEngine;

public enum HWJ_RewardGrantFailureCode
{
    None,
    MissingResolver,
    SourceNotPlayer,
    InvalidDefeatedObject,
    MissingRewardData,
    MissingRewardReceiver,
    MissingSaveIdentity,
    AlreadyClaimed
}

public readonly struct HWJ_RewardGrantResult
{
    public readonly bool Succeeded;
    public readonly HWJ_RewardGrantFailureCode FailureCode;
    public readonly HWJ_RootObjectDataResolver DefeatedResolver;
    public readonly HWJ_RootObjectDataResolver RewardSourceResolver;
    public readonly HWJ_RewardData RewardData;
    public readonly int ExperienceGranted;
    public readonly int SkillPointGranted;
    public readonly bool ExperienceOrbSpawned;
    public readonly bool ExperienceGrantedImmediately;
    public readonly bool StatOrbGranted;
    public readonly Vector3 RewardPosition;
    public readonly string RewardClaimId;
    public readonly string Message;

    private HWJ_RewardGrantResult(
        bool succeeded,
        HWJ_RewardGrantFailureCode failureCode,
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        HWJ_RewardData rewardData,
        int experienceGranted,
        int skillPointGranted,
        bool experienceOrbSpawned,
        bool experienceGrantedImmediately,
        bool statOrbGranted,
        Vector3 rewardPosition,
        string rewardClaimId,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        DefeatedResolver = defeatedResolver;
        RewardSourceResolver = rewardSourceResolver;
        RewardData = rewardData;
        ExperienceGranted = experienceGranted;
        SkillPointGranted = skillPointGranted;
        ExperienceOrbSpawned = experienceOrbSpawned;
        ExperienceGrantedImmediately = experienceGrantedImmediately;
        StatOrbGranted = statOrbGranted;
        RewardPosition = rewardPosition;
        RewardClaimId = rewardClaimId;
        Message = message;
    }

    public static HWJ_RewardGrantResult Success(
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        HWJ_RewardData rewardData,
        int experienceGranted,
        int skillPointGranted,
        bool experienceOrbSpawned,
        bool experienceGrantedImmediately,
        bool statOrbGranted,
        Vector3 rewardPosition,
        string rewardClaimId,
        string message)
    {
        return new HWJ_RewardGrantResult(
            true,
            HWJ_RewardGrantFailureCode.None,
            defeatedResolver,
            rewardSourceResolver,
            rewardData,
            experienceGranted,
            skillPointGranted,
            experienceOrbSpawned,
            experienceGrantedImmediately,
            statOrbGranted,
            rewardPosition,
            rewardClaimId,
            message);
    }

    public static HWJ_RewardGrantResult Fail(
        HWJ_RewardGrantFailureCode failureCode,
        string message,
        HWJ_RootObjectDataResolver defeatedResolver = null,
        HWJ_RootObjectDataResolver rewardSourceResolver = null,
        string rewardClaimId = null)
    {
        return new HWJ_RewardGrantResult(
            false,
            failureCode,
            defeatedResolver,
            rewardSourceResolver,
            null,
            0,
            0,
            false,
            false,
            false,
            Vector3.zero,
            rewardClaimId,
            message);
    }
}

public static class HWJ_RewardUtility
{
    public static bool TryGrantKillReward(
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        Vector3 rewardPosition,
        out string result)
    {
        HWJ_RewardGrantResult grantResult = TryGrantKillRewardDetailed(
            defeatedResolver,
            rewardSourceResolver,
            rewardPosition);
        result = grantResult.Message;
        return grantResult.Succeeded;
    }

    public static HWJ_RewardGrantResult TryGrantKillRewardDetailed(
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        Vector3 rewardPosition)
    {
        if (defeatedResolver == null || rewardSourceResolver == null)
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.MissingResolver,
                "Reward skipped: missing resolver.",
                defeatedResolver,
                rewardSourceResolver);
        }

        if (!IsPlayerRewardSource(rewardSourceResolver))
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.SourceNotPlayer,
                "Reward skipped: source is not player.",
                defeatedResolver,
                rewardSourceResolver);
        }

        if (defeatedResolver.ObjectType != HWJ_ObjectType.Enemy
            && defeatedResolver.ObjectType != HWJ_ObjectType.Boss)
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.InvalidDefeatedObject,
                "Reward skipped: defeated object is not enemy or boss.",
                defeatedResolver,
                rewardSourceResolver);
        }

        HWJ_RewardData rewardData = defeatedResolver.Reward;

        if (rewardData == null)
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.MissingRewardData,
                "Reward skipped: no reward data.",
                defeatedResolver,
                rewardSourceResolver);
        }

        HWJ_LevelUpSystem playerLevel = GetPlayerLevel(rewardSourceResolver);
        HWJ_RuntimeStatusSystem playerStatus = GetPlayerStatus(rewardSourceResolver);
        HWJ_SaveIdentityValidationResult saveIdentityValidation = HWJ_RuntimeSaveIdentity.ValidateRewardClaimIdentity(defeatedResolver);
        string rewardClaimId = saveIdentityValidation.RewardClaimId;

        // 저장 서비스가 켜진 전투에서는 영구 중복 지급 차단을 위해 안정 ID가 반드시 필요합니다.
        if (RequiresSavedProgressionRewardClaim() && !saveIdentityValidation.Succeeded)
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.MissingSaveIdentity,
                saveIdentityValidation.Message,
                defeatedResolver,
                rewardSourceResolver,
                rewardClaimId);
        }

        if (playerLevel == null && (rewardData.experienceReward > 0 || rewardData.skillPointReward > 0))
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.MissingRewardReceiver,
                "Reward skipped: missing player level system.",
                defeatedResolver,
                rewardSourceResolver,
                rewardClaimId);
        }

        if (IsRewardClaimedBySavedProgression(rewardClaimId))
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.AlreadyClaimed,
                $"Reward skipped: saved progression already claimed {rewardClaimId}.",
                defeatedResolver,
                rewardSourceResolver,
                rewardClaimId);
        }

        HWJ_RewardClaimState claimState = defeatedResolver.GetComponent<HWJ_RewardClaimState>();

        if (claimState == null)
        {
            claimState = defeatedResolver.gameObject.AddComponent<HWJ_RewardClaimState>();
        }

        if (!claimState.TryClaim())
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.AlreadyClaimed,
                "Reward skipped: already claimed.",
                defeatedResolver,
                rewardSourceResolver,
                rewardClaimId);
        }

        if (!TryClaimSavedProgressionReward(rewardClaimId, defeatedResolver))
        {
            return HWJ_RewardGrantResult.Fail(
                HWJ_RewardGrantFailureCode.AlreadyClaimed,
                $"Reward skipped: saved progression claim failed for {rewardClaimId}.",
                defeatedResolver,
                rewardSourceResolver,
                rewardClaimId);
        }

        bool experienceOrbSpawned = TrySpawnExperienceOrbReward(
            rewardData,
            playerLevel,
            rewardSourceResolver,
            rewardPosition);
        bool experienceGrantedImmediately = false;
        int experienceGranted = 0;
        int skillPointGranted = 0;

        if (!experienceOrbSpawned && playerLevel != null && rewardData.experienceReward > 0)
        {
            playerLevel.AddExperience(rewardData.experienceReward);
            experienceGranted = Mathf.Max(0, rewardData.experienceReward);
            experienceGrantedImmediately = true;
        }

        if (playerLevel != null && rewardData.skillPointReward > 0)
        {
            playerLevel.AddSkillPoint(rewardData.skillPointReward);
            skillPointGranted = Mathf.Max(0, rewardData.skillPointReward);
        }

        bool statOrbGranted = TryGrantStatOrbReward(
            rewardData,
            defeatedResolver,
            playerStatus,
            rewardSourceResolver,
            rewardPosition);

        string message = $"Reward granted. Exp:{experienceGranted}, SkillPoint:{skillPointGranted}, ExpOrb:{experienceOrbSpawned}, StatOrb:{statOrbGranted}.";
        HWJ_RewardGrantResult result = HWJ_RewardGrantResult.Success(
            defeatedResolver,
            rewardSourceResolver,
            rewardData,
            experienceGranted,
            skillPointGranted,
            experienceOrbSpawned,
            experienceGrantedImmediately,
            statOrbGranted,
            rewardPosition,
            rewardClaimId,
            message);
        HWJ_GameplayEvents.RaiseEnemyDefeated(
            new HWJ_EnemyDefeatedEvent(defeatedResolver, rewardSourceResolver, rewardPosition, defeatedResolver.ObjectType == HWJ_ObjectType.Boss));
        HWJ_GameplayEvents.RaiseRewardGranted(
            new HWJ_RewardGrantedEvent(result));
        return result;
    }

    private static bool RequiresSavedProgressionRewardClaim()
    {
        return HWJ_SaveService.TryGetActiveService(out _);
    }

    private static bool IsRewardClaimedBySavedProgression(string rewardClaimId)
    {
        return !string.IsNullOrEmpty(rewardClaimId)
            && HWJ_SaveService.TryGetActiveService(out HWJ_SaveService activeSaveService)
            && activeSaveService.IsRewardClaimed(rewardClaimId);
    }

    private static bool TryClaimSavedProgressionReward(
        string rewardClaimId,
        HWJ_RootObjectDataResolver defeatedResolver)
    {
        if (string.IsNullOrEmpty(rewardClaimId)
            || !HWJ_SaveService.TryGetActiveService(out HWJ_SaveService activeSaveService))
        {
            return true;
        }

        string defeatedObjectId = ResolveDefeatedObjectId(defeatedResolver);
        return activeSaveService.TryClaimReward(
            rewardClaimId,
            defeatedResolver != null && defeatedResolver.ObjectType == HWJ_ObjectType.Boss,
            defeatedObjectId);
    }

    private static bool TrySpawnExperienceOrbReward(
        HWJ_RewardData rewardData,
        HWJ_LevelUpSystem playerLevel,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        Vector3 rewardPosition)
    {
        if (rewardData == null
            || !rewardData.dropsExperienceOrb
            || rewardData.experienceReward <= 0
            || rewardData.experienceOrbPrefab == null
            || playerLevel == null)
        {
            return false;
        }

        Vector3 spawnPosition = rewardPosition + ResolveExperienceOrbOffset(rewardData.experienceOrbSpawnRadius);
        GameObject orbObject = HWJ_GameAccess.Spawn(rewardData.experienceOrbPrefab, spawnPosition, Quaternion.identity);

        if (orbObject == null)
        {
            orbObject = Object.Instantiate(rewardData.experienceOrbPrefab, spawnPosition, Quaternion.identity);
        }

        if (orbObject == null)
        {
            return false;
        }

        HWJ_ExperienceOrbPickupSystem experienceOrb = orbObject.GetComponent<HWJ_ExperienceOrbPickupSystem>();

        if (experienceOrb == null)
        {
            experienceOrb = orbObject.AddComponent<HWJ_ExperienceOrbPickupSystem>();
        }

        Transform targetTransform = rewardSourceResolver != null ? rewardSourceResolver.transform : playerLevel.transform;
        experienceOrb.Initialize(rewardData.experienceReward, playerLevel, targetTransform);
        return true;
    }

    private static Vector3 ResolveExperienceOrbOffset(float radius)
    {
        float clampedRadius = Mathf.Max(0f, radius);

        if (clampedRadius <= 0f)
        {
            return Vector3.zero;
        }

        Vector2 randomOffset = Random.insideUnitCircle * clampedRadius;
        return new Vector3(randomOffset.x, randomOffset.y, 0f);
    }

    private static bool TryGrantStatOrbReward(
        HWJ_RewardData rewardData,
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RuntimeStatusSystem playerStatus,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        Vector3 rewardPosition)
    {
        if (rewardData == null)
        {
            return false;
        }

        bool isGuaranteedDrop = IsGuaranteedStatOrbDrop(defeatedResolver);

        if (!rewardData.dropsStatOrb && !isGuaranteedDrop)
        {
            return false;
        }

        float statOrbDropChance = Mathf.Clamp01(rewardData.statOrbDropChance);

        if (!isGuaranteedDrop && statOrbDropChance <= 0f)
        {
            return false;
        }

        if (!isGuaranteedDrop && Random.value > statOrbDropChance)
        {
            return false;
        }

        if (!TrySelectStatOrbRewardData(
            rewardData,
            isGuaranteedDrop,
            playerStatus,
            out HWJ_StatOrbDataSO statOrbData)
            || statOrbData == null)
        {
            return false;
        }

        if (playerStatus != null && !playerStatus.CanApplyStatOrb(statOrbData))
        {
            return false;
        }

        if (statOrbData.OrbPrefab != null)
        {
            Vector3 spawnPosition = rewardPosition + ResolveRewardOrbOffset(rewardData.statOrbSpawnRadius);
            GameObject orbObject = HWJ_GameAccess.Spawn(statOrbData.OrbPrefab, spawnPosition, Quaternion.identity);

            if (orbObject == null)
            {
                orbObject = Object.Instantiate(statOrbData.OrbPrefab, spawnPosition, Quaternion.identity);
            }

            if (orbObject == null)
            {
                return false;
            }

            HWJ_StatOrbPickupSystem statOrbPickup = orbObject.GetComponent<HWJ_StatOrbPickupSystem>();

            if (statOrbPickup == null)
            {
                statOrbPickup = orbObject.AddComponent<HWJ_StatOrbPickupSystem>();
            }

            Transform targetTransform = rewardSourceResolver != null
                ? rewardSourceResolver.transform
                : playerStatus != null ? playerStatus.transform : null;
            statOrbPickup.Initialize(statOrbData, playerStatus, targetTransform, HWJ_GameAccess.ObjectPool);
            return true;
        }

        if (playerStatus == null)
        {
            return false;
        }

        return playerStatus.TryApplyStatOrb(statOrbData);
    }

    private static bool IsGuaranteedStatOrbDrop(HWJ_RootObjectDataResolver defeatedResolver)
    {
        return defeatedResolver != null
            && defeatedResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyTypeData)
            && enemyTypeData != null
            && enemyTypeData.Role != null
            && enemyTypeData.Role.guaranteesStatOrb;
    }

    private static bool TrySelectStatOrbRewardData(
        HWJ_RewardData rewardData,
        bool useAllRegisteredFallback,
        HWJ_RuntimeStatusSystem playerStatus,
        out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;

        if (TrySelectWeightedStatOrbCandidate(rewardData, playerStatus, out statOrbData))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(rewardData.statOrbId)
            && HWJ_GameAccess.TryGetStatOrb(rewardData.statOrbId, out statOrbData)
            && statOrbData != null
            && (playerStatus == null || playerStatus.CanApplyStatOrb(statOrbData)))
        {
            return true;
        }

        if (useAllRegisteredFallback || rewardData.useAllRegisteredStatOrbsWhenEmpty)
        {
            return TrySelectRegisteredStatOrb(playerStatus, out statOrbData);
        }

        statOrbData = null;
        return false;
    }

    private static bool TrySelectWeightedStatOrbCandidate(
        HWJ_RewardData rewardData,
        HWJ_RuntimeStatusSystem playerStatus,
        out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;

        if (rewardData.statOrbCandidates == null || rewardData.statOrbCandidates.Length == 0)
        {
            return false;
        }

        int totalWeight = 0;

        for (int i = 0; i < rewardData.statOrbCandidates.Length; i++)
        {
            HWJ_StatOrbRewardEntry candidate = rewardData.statOrbCandidates[i];

            if (!TryResolveEligibleStatOrbCandidate(candidate, playerStatus, out _))
            {
                continue;
            }

            totalWeight += Mathf.Max(0, candidate.weight);
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int selectedWeight = Random.Range(0, totalWeight);

        for (int i = 0; i < rewardData.statOrbCandidates.Length; i++)
        {
            HWJ_StatOrbRewardEntry candidate = rewardData.statOrbCandidates[i];

            if (!TryResolveEligibleStatOrbCandidate(candidate, playerStatus, out HWJ_StatOrbDataSO candidateData))
            {
                continue;
            }

            selectedWeight -= Mathf.Max(0, candidate.weight);

            if (selectedWeight < 0)
            {
                statOrbData = candidateData;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveEligibleStatOrbCandidate(
        HWJ_StatOrbRewardEntry candidate,
        HWJ_RuntimeStatusSystem playerStatus,
        out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;

        if (candidate == null
            || candidate.weight <= 0
            || string.IsNullOrWhiteSpace(candidate.statOrbId)
            || !HWJ_GameAccess.TryGetStatOrb(candidate.statOrbId, out statOrbData)
            || statOrbData == null)
        {
            return false;
        }

        return playerStatus == null || playerStatus.CanApplyStatOrb(statOrbData);
    }

    private static bool TrySelectRegisteredStatOrb(
        HWJ_RuntimeStatusSystem playerStatus,
        out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;
        HWJ_GameplayDatabaseSO database = HWJ_GameAccess.Database;

        if (database == null || database.StatOrbs == null || database.StatOrbs.Length == 0)
        {
            return false;
        }

        int eligibleCount = 0;

        for (int i = 0; i < database.StatOrbs.Length; i++)
        {
            HWJ_StatOrbDataSO candidate = database.StatOrbs[i];

            if (candidate != null && (playerStatus == null || playerStatus.CanApplyStatOrb(candidate)))
            {
                eligibleCount++;
            }
        }

        if (eligibleCount <= 0)
        {
            return false;
        }

        int selectedIndex = Random.Range(0, eligibleCount);

        for (int i = 0; i < database.StatOrbs.Length; i++)
        {
            HWJ_StatOrbDataSO candidate = database.StatOrbs[i];

            if (candidate == null || (playerStatus != null && !playerStatus.CanApplyStatOrb(candidate)))
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                statOrbData = candidate;
                return true;
            }

            selectedIndex--;
        }

        return false;
    }

    private static Vector3 ResolveRewardOrbOffset(float radius)
    {
        float clampedRadius = Mathf.Max(0f, radius);

        if (clampedRadius <= 0f)
        {
            return Vector3.zero;
        }

        Vector2 randomOffset = Random.insideUnitCircle * clampedRadius;
        return new Vector3(randomOffset.x, randomOffset.y, 0f);
    }

    private static string ResolveDefeatedObjectId(HWJ_RootObjectDataResolver defeatedResolver)
    {
        if (defeatedResolver == null || defeatedResolver.RootObjectData == null)
        {
            return null;
        }

        if (defeatedResolver.RootObjectData.Identity != null
            && !string.IsNullOrEmpty(defeatedResolver.RootObjectData.Identity.objectId))
        {
            return defeatedResolver.RootObjectData.Identity.objectId;
        }

        return defeatedResolver.RootObjectData.name;
    }

    private static bool IsPlayerRewardSource(HWJ_RootObjectDataResolver rewardSourceResolver)
    {
        if (rewardSourceResolver == null)
        {
            return false;
        }

        if (rewardSourceResolver.ObjectType == HWJ_ObjectType.Player)
        {
            return true;
        }

        return HWJ_GameAccess.HasManager
            && HWJ_GameAccess.Manager.PlayerResolver == rewardSourceResolver;
    }

    private static HWJ_LevelUpSystem GetPlayerLevel(HWJ_RootObjectDataResolver fallbackResolver)
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerLevel != null)
        {
            return HWJ_GameAccess.Manager.PlayerLevel;
        }

        return fallbackResolver != null ? fallbackResolver.GetComponent<HWJ_LevelUpSystem>() : null;
    }

    private static HWJ_RuntimeStatusSystem GetPlayerStatus(HWJ_RootObjectDataResolver fallbackResolver)
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerStatus != null)
        {
            return HWJ_GameAccess.Manager.PlayerStatus;
        }

        return fallbackResolver != null ? fallbackResolver.GetComponent<HWJ_RuntimeStatusSystem>() : null;
    }
}
