using UnityEngine;

public static class HWJ_RewardUtility
{
    public static bool TryGrantKillReward(
        HWJ_RootObjectDataResolver defeatedResolver,
        HWJ_RootObjectDataResolver rewardSourceResolver,
        Vector3 rewardPosition,
        out string result)
    {
        result = "Reward skipped.";

        if (defeatedResolver == null || rewardSourceResolver == null)
        {
            result = "Reward skipped: missing resolver.";
            return false;
        }

        if (!IsPlayerRewardSource(rewardSourceResolver))
        {
            result = "Reward skipped: source is not player.";
            return false;
        }

        if (defeatedResolver.ObjectType != HWJ_ObjectType.Enemy
            && defeatedResolver.ObjectType != HWJ_ObjectType.Boss)
        {
            result = "Reward skipped: defeated object is not enemy or boss.";
            return false;
        }

        HWJ_RewardClaimState claimState = defeatedResolver.GetComponent<HWJ_RewardClaimState>();

        if (claimState == null)
        {
            claimState = defeatedResolver.gameObject.AddComponent<HWJ_RewardClaimState>();
        }

        if (!claimState.TryClaim())
        {
            result = "Reward skipped: already claimed.";
            return false;
        }

        HWJ_RewardData rewardData = defeatedResolver.Reward;

        if (rewardData == null)
        {
            result = "Reward skipped: no reward data.";
            return false;
        }

        HWJ_LevelUpSystem playerLevel = GetPlayerLevel(rewardSourceResolver);
        HWJ_RuntimeStatusSystem playerStatus = GetPlayerStatus(rewardSourceResolver);

        if (playerLevel != null)
        {
            playerLevel.AddReward(rewardData);
        }

        bool statOrbGranted = TryGrantStatOrbReward(rewardData, playerStatus, rewardPosition);

        result = $"Reward granted. Exp:{rewardData.experienceReward}, SkillPoint:{rewardData.skillPointReward}, StatOrb:{statOrbGranted}.";
        return true;
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
