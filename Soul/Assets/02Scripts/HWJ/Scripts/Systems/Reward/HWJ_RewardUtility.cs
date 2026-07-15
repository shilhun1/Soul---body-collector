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
        StatOrbGranted = statOrbGranted;
        RewardPosition = rewardPosition;
        RewardClaimId = rewardClaimId;
        Message = message;
    }

    public static HWJ_RewardGrantResult Success(
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        HWJ_RewardData rewardData,
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
            rewardData != null ? Mathf.Max(0, rewardData.experienceReward) : 0,
            rewardData != null ? Mathf.Max(0, rewardData.skillPointReward) : 0,
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

        if (playerLevel != null)
        {
            playerLevel.AddReward(rewardData);
        }

        bool statOrbGranted = TryGrantStatOrbReward(rewardData, playerStatus, rewardPosition);

        string message = $"Reward granted. Exp:{rewardData.experienceReward}, SkillPoint:{rewardData.skillPointReward}, StatOrb:{statOrbGranted}.";
        HWJ_RewardGrantResult result = HWJ_RewardGrantResult.Success(
            defeatedResolver,
            rewardSourceResolver,
            rewardData,
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

    private static bool TryGrantStatOrbReward(
        HWJ_RewardData rewardData,
        HWJ_RuntimeStatusSystem playerStatus,
        Vector3 rewardPosition)
    {
        if (rewardData == null || !rewardData.dropsStatOrb || string.IsNullOrEmpty(rewardData.statOrbId))
        {
            return false;
        }

        if (!HWJ_GameAccess.TryGetStatOrb(rewardData.statOrbId, out HWJ_StatOrbDataSO statOrbData)
            || statOrbData == null)
        {
            return false;
        }

        if (statOrbData.OrbPrefab != null)
        {
            GameObject orbObject = HWJ_GameAccess.Spawn(statOrbData.OrbPrefab, rewardPosition, Quaternion.identity);

            if (orbObject != null)
            {
                return true;
            }
        }

        if (playerStatus == null)
        {
            return false;
        }

        playerStatus.ApplyStatOrb(statOrbData);
        return true;
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
