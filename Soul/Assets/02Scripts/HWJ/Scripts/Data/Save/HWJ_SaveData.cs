using System;
using System.Collections.Generic;

public static class HWJ_SaveSchema
{
    public const int CurrentVersion = 3;
}

/// <summary>
/// 저장 파일의 최상위 DTO입니다.
/// UnityEngine.Object 참조를 직접 넣지 않고 문자열 ID와 직렬화 가능한 값만 보관합니다.
/// </summary>
[Serializable]
public class HWJ_GameSaveData
{
    public int schemaVersion = HWJ_SaveSchema.CurrentVersion;
    public string saveId;
    public string createdUtc;
    public string savedUtc;
    public HWJ_SavePlayerRuntimeData player = new HWJ_SavePlayerRuntimeData();
    public HWJ_SaveStageRuntimeData stage = new HWJ_SaveStageRuntimeData();
    public HWJ_SaveProgressionData progression = new HWJ_SaveProgressionData();
    public HWJ_SaveSettingsData settings = new HWJ_SaveSettingsData();

    public void EnsureDefaults()
    {
        if (player == null)
        {
            player = new HWJ_SavePlayerRuntimeData();
        }

        if (stage == null)
        {
            stage = new HWJ_SaveStageRuntimeData();
        }

        if (progression == null)
        {
            progression = new HWJ_SaveProgressionData();
        }

        if (settings == null)
        {
            settings = new HWJ_SaveSettingsData();
        }

        player.EnsureDefaults();
        progression.EnsureLists();
        settings.EnsureDefaults();
    }
}

[Serializable]
public class HWJ_SavePlayerRuntimeData
{
    public string playerRootObjectId;
    public HWJ_ObjectType objectType;
    public HWJ_Faction faction;
    public HWJ_WeaponType weaponType;
    public HWJ_SaveRuntimeStatData stats = new HWJ_SaveRuntimeStatData();
    public HWJ_SaveBodyRuntimeData body = new HWJ_SaveBodyRuntimeData();
    public HWJ_SaveGrowthRuntimeData growth = new HWJ_SaveGrowthRuntimeData();

    public void EnsureDefaults()
    {
        if (stats == null)
        {
            stats = new HWJ_SaveRuntimeStatData();
        }

        if (body == null)
        {
            body = new HWJ_SaveBodyRuntimeData();
        }

        if (growth == null)
        {
            growth = new HWJ_SaveGrowthRuntimeData();
        }

        growth.EnsureLists();
    }
}

[Serializable]
public class HWJ_SaveRuntimeStatData
{
    public HWJ_RuntimeState runtimeState;
    public float currentHp;
    public float maxHp;
    public float soulHp;
    public float soulMaxHp;
    public float possessedBodyHp;
    public float moveSpeed;
    public float attackPower;
    public float defense;
    public float attackSpeed;
    public bool usesHp;
    public bool isDead;
    public bool canMove;
    public bool canAttack;
    public bool canDash;
    public bool isHitStunned;
    public bool isInvincible;
    public bool isHitReactionLimited;
    public bool hasSuperArmor;
    public bool shouldIgnoreKnockback;
}

[Serializable]
public class HWJ_SaveBodyRuntimeData
{
    public HWJ_SoulRuntimeState soulState;
    public HWJ_PlayerExistenceState playerExistenceState;
    public float soulDeadlineTimer;
    public float bodyToSoulTransitionTimer;
    public bool hasActivePossessedBody;
    public bool hasPossessedBodyRuntimeState;
    public string possessedBodyRuntimeInstanceId;
    public string possessedBodyDefinitionDataId;
    public float possessedBodyCurrentHp;
    public float possessedBodyMaxHp;
    public bool possessedBodyCollapsed;
    public HWJ_WeaponType possessedWeaponType;
    public float currentDecayValue;
    public float maxDecayValue;
    public float currentDecayRatio;
    public float remainingDecayValue;
    public float remainingDecayRatio;
    public HWJ_DecayDangerLevel decayDangerLevel;
    public bool isDecaying;

    public HWJ_RuntimeBodySnapshot ToRuntimeSnapshot()
    {
        return new HWJ_RuntimeBodySnapshot
        {
            soulState = soulState,
            playerExistenceState = playerExistenceState,
            soulDeadlineTimer = soulDeadlineTimer,
            bodyToSoulTransitionTimer = bodyToSoulTransitionTimer,
            hasActivePossessedBody = hasActivePossessedBody,
            hasPossessedBodyRuntimeState = hasPossessedBodyRuntimeState,
            possessedBodyRuntimeInstanceId = possessedBodyRuntimeInstanceId,
            possessedBodyDefinitionDataId = possessedBodyDefinitionDataId,
            possessedBodyCurrentHp = possessedBodyCurrentHp,
            possessedBodyMaxHp = possessedBodyMaxHp,
            possessedBodyCollapsed = possessedBodyCollapsed,
            possessedWeaponType = possessedWeaponType,
            currentDecayValue = currentDecayValue,
            maxDecayValue = maxDecayValue,
            currentDecayRatio = currentDecayRatio,
            remainingDecayValue = remainingDecayValue,
            remainingDecayRatio = remainingDecayRatio,
            decayDangerLevel = decayDangerLevel,
            isDecaying = isDecaying
        };
    }
}

[Serializable]
public class HWJ_SaveStatOrbStackData
{
    public string statOrbId;
    public int stackCount;
}

[Serializable]
public class HWJ_SaveGrowthRuntimeData
{
    public int currentLevel = 1;
    public int currentExperience;
    public int skillPoint;
    public List<string> unlockedSkillIds = new List<string>();
    public List<string> unlockedSkillNodeIds = new List<string>();
    public List<HWJ_SaveStatOrbStackData> statOrbStacks = new List<HWJ_SaveStatOrbStackData>();

    public void EnsureLists()
    {
        if (unlockedSkillIds == null)
        {
            unlockedSkillIds = new List<string>();
        }

        if (unlockedSkillNodeIds == null)
        {
            unlockedSkillNodeIds = new List<string>();
        }

        if (statOrbStacks == null)
        {
            statOrbStacks = new List<HWJ_SaveStatOrbStackData>();
        }
    }
}

[Serializable]
public class HWJ_SaveStageRuntimeData
{
    public string stageId;
    public string nextRegionId;
    public HWJ_StageFlowState currentState;
    public bool objectiveComplete;
    public bool bossUnlocked;
    public bool bossBattleStarted;
    public bool bossDefeated;
    public bool regionUnlocked;
    public bool transitionLocked;

    public HWJ_RuntimeStageFlowSnapshot ToRuntimeSnapshot()
    {
        return new HWJ_RuntimeStageFlowSnapshot
        {
            stageId = stageId,
            nextRegionId = nextRegionId,
            currentState = currentState,
            objectiveComplete = objectiveComplete,
            bossUnlocked = bossUnlocked,
            bossBattleStarted = bossBattleStarted,
            bossDefeated = bossDefeated,
            regionUnlocked = regionUnlocked,
            transitionLocked = transitionLocked
        };
    }
}

[Serializable]
public class HWJ_SaveProgressionData
{
    public List<string> defeatedEnemyRewardIds = new List<string>();
    public List<string> defeatedBossIds = new List<string>();
    public List<string> clearedStageIds = new List<string>();
    public List<string> unlockedRegionIds = new List<string>();

    public void EnsureLists()
    {
        if (defeatedEnemyRewardIds == null)
        {
            defeatedEnemyRewardIds = new List<string>();
        }

        if (defeatedBossIds == null)
        {
            defeatedBossIds = new List<string>();
        }

        if (clearedStageIds == null)
        {
            clearedStageIds = new List<string>();
        }

        if (unlockedRegionIds == null)
        {
            unlockedRegionIds = new List<string>();
        }
    }

    public bool AddDefeatedEnemyRewardId(string rewardClaimId)
    {
        EnsureLists();
        return AddUniqueId(defeatedEnemyRewardIds, rewardClaimId);
    }

    public bool AddDefeatedBossId(string bossId)
    {
        EnsureLists();
        return AddUniqueId(defeatedBossIds, bossId);
    }

    public bool AddClearedStageId(string stageId)
    {
        EnsureLists();
        return AddUniqueId(clearedStageIds, stageId);
    }

    public bool AddUnlockedRegionId(string regionId)
    {
        EnsureLists();
        return AddUniqueId(unlockedRegionIds, regionId);
    }

    public HWJ_SaveProgressionData Clone()
    {
        EnsureLists();

        return new HWJ_SaveProgressionData
        {
            defeatedEnemyRewardIds = new List<string>(defeatedEnemyRewardIds),
            defeatedBossIds = new List<string>(defeatedBossIds),
            clearedStageIds = new List<string>(clearedStageIds),
            unlockedRegionIds = new List<string>(unlockedRegionIds)
        };
    }

    private static bool AddUniqueId(List<string> idList, string id)
    {
        if (idList == null || string.IsNullOrEmpty(id) || idList.Contains(id))
        {
            return false;
        }

        idList.Add(id);
        return true;
    }
}

[Serializable]
public class HWJ_SaveSettingsData
{
    public HWJ_SaveInputBindingData inputBindings = new HWJ_SaveInputBindingData();

    public void EnsureDefaults()
    {
        if (inputBindings == null)
        {
            inputBindings = new HWJ_SaveInputBindingData();
        }

        inputBindings.EnsureLists();
    }
}

[Serializable]
public class HWJ_SaveInputBindingData
{
    public List<HWJ_SaveInputBindingOverrideData> bindingOverrides = new List<HWJ_SaveInputBindingOverrideData>();

    public int OverrideCount => bindingOverrides != null ? bindingOverrides.Count : 0;

    public void EnsureLists()
    {
        if (bindingOverrides == null)
        {
            bindingOverrides = new List<HWJ_SaveInputBindingOverrideData>();
        }
    }

    public void Clear()
    {
        EnsureLists();
        bindingOverrides.Clear();
    }

    public bool AddOrReplace(HWJ_SaveInputBindingOverrideData bindingOverride)
    {
        if (bindingOverride == null)
        {
            return false;
        }

        EnsureLists();

        for (int i = 0; i < bindingOverrides.Count; i++)
        {
            HWJ_SaveInputBindingOverrideData existing = bindingOverrides[i];

            if (existing != null && existing.actionId == bindingOverride.actionId)
            {
                bindingOverrides[i] = bindingOverride;
                return true;
            }
        }

        bindingOverrides.Add(bindingOverride);
        return true;
    }
}

[Serializable]
public class HWJ_SaveInputBindingOverrideData
{
    public HWJ_PlayerInputActionId actionId;
    public bool hasKeyboard;
    public int keyboardKeyCode;
    public bool hasMouse;
    public HWJ_InputMouseButton mouseButton;
}

public static class HWJ_SaveDataFactory
{
    /// <summary>
    /// 런타임 스냅샷에서 저장 전용 DTO를 생성합니다.
    /// Snapshot 안의 ScriptableObject 참조는 저장하지 않고 안정적인 ID만 추출합니다.
    /// </summary>
    public static HWJ_GameSaveData CreateFromSnapshots(
        string saveId,
        HWJ_RuntimeObjectSnapshot playerSnapshot,
        HWJ_RuntimeStageFlowSnapshot stageSnapshot,
        HWJ_SaveProgressionData progressionTemplate)
    {
        string utcNow = DateTime.UtcNow.ToString("O");
        HWJ_GameSaveData saveData = new HWJ_GameSaveData
        {
            schemaVersion = HWJ_SaveSchema.CurrentVersion,
            saveId = saveId,
            createdUtc = utcNow,
            savedUtc = utcNow,
            player = CreatePlayerData(playerSnapshot),
            stage = CreateStageData(stageSnapshot),
            progression = progressionTemplate != null
                ? progressionTemplate.Clone()
                : new HWJ_SaveProgressionData()
        };
        saveData.EnsureDefaults();
        return saveData;
    }

    public static void RefreshSavedTime(HWJ_GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(saveData.createdUtc))
        {
            saveData.createdUtc = DateTime.UtcNow.ToString("O");
        }

        saveData.savedUtc = DateTime.UtcNow.ToString("O");
    }

    private static HWJ_SavePlayerRuntimeData CreatePlayerData(HWJ_RuntimeObjectSnapshot snapshot)
    {
        return new HWJ_SavePlayerRuntimeData
        {
            playerRootObjectId = ResolveRootObjectId(snapshot.rootObjectData),
            objectType = snapshot.objectType,
            faction = snapshot.faction,
            weaponType = snapshot.weaponType,
            stats = CreateStatData(snapshot.runtimeStats),
            body = CreateBodyData(snapshot.runtimeBody),
            growth = CreateGrowthData(snapshot.runtimeGrowth)
        };
    }

    private static HWJ_SaveRuntimeStatData CreateStatData(HWJ_RuntimeStatSnapshot snapshot)
    {
        return new HWJ_SaveRuntimeStatData
        {
            runtimeState = snapshot.runtimeState,
            currentHp = snapshot.currentHp,
            maxHp = snapshot.maxHp,
            soulHp = snapshot.soulHp,
            soulMaxHp = snapshot.soulMaxHp,
            possessedBodyHp = snapshot.possessedBodyHp,
            moveSpeed = snapshot.moveSpeed,
            attackPower = snapshot.attackPower,
            defense = snapshot.defense,
            attackSpeed = snapshot.attackSpeed,
            usesHp = snapshot.usesHp,
            isDead = snapshot.isDead,
            canMove = snapshot.canMove,
            canAttack = snapshot.canAttack,
            canDash = snapshot.canDash,
            isHitStunned = snapshot.isHitStunned,
            isInvincible = snapshot.isInvincible,
            isHitReactionLimited = snapshot.isHitReactionLimited,
            hasSuperArmor = snapshot.hasSuperArmor,
            shouldIgnoreKnockback = snapshot.shouldIgnoreKnockback
        };
    }

    private static HWJ_SaveBodyRuntimeData CreateBodyData(HWJ_RuntimeBodySnapshot snapshot)
    {
        return new HWJ_SaveBodyRuntimeData
        {
            soulState = snapshot.soulState,
            playerExistenceState = snapshot.playerExistenceState,
            soulDeadlineTimer = snapshot.soulDeadlineTimer,
            bodyToSoulTransitionTimer = snapshot.bodyToSoulTransitionTimer,
            hasActivePossessedBody = snapshot.hasActivePossessedBody,
            hasPossessedBodyRuntimeState = snapshot.hasPossessedBodyRuntimeState,
            possessedBodyRuntimeInstanceId = snapshot.possessedBodyRuntimeInstanceId,
            possessedBodyDefinitionDataId = snapshot.possessedBodyDefinitionDataId,
            possessedBodyCurrentHp = snapshot.possessedBodyCurrentHp,
            possessedBodyMaxHp = snapshot.possessedBodyMaxHp,
            possessedBodyCollapsed = snapshot.possessedBodyCollapsed,
            possessedWeaponType = snapshot.possessedWeaponType,
            currentDecayValue = snapshot.currentDecayValue,
            maxDecayValue = snapshot.maxDecayValue,
            currentDecayRatio = snapshot.currentDecayRatio,
            remainingDecayValue = snapshot.remainingDecayValue,
            remainingDecayRatio = snapshot.remainingDecayRatio,
            decayDangerLevel = snapshot.decayDangerLevel,
            isDecaying = snapshot.isDecaying
        };
    }

    private static HWJ_SaveGrowthRuntimeData CreateGrowthData(HWJ_RuntimeGrowthSnapshot snapshot)
    {
        HWJ_SaveGrowthRuntimeData growthData = new HWJ_SaveGrowthRuntimeData
        {
            currentLevel = snapshot.currentLevel,
            currentExperience = snapshot.currentExperience,
            skillPoint = snapshot.skillPoint,
            unlockedSkillIds = snapshot.unlockedSkillIds != null
                ? new List<string>(snapshot.unlockedSkillIds)
                : new List<string>(),
            unlockedSkillNodeIds = snapshot.unlockedSkillNodeIds != null
                ? new List<string>(snapshot.unlockedSkillNodeIds)
                : new List<string>(),
            statOrbStacks = CreateStatOrbStackData(snapshot.statOrbStacks)
        };
        growthData.EnsureLists();
        return growthData;
    }

    private static List<HWJ_SaveStatOrbStackData> CreateStatOrbStackData(HWJ_RuntimeStatOrbStackSnapshot[] snapshots)
    {
        List<HWJ_SaveStatOrbStackData> saveStacks = new List<HWJ_SaveStatOrbStackData>();

        if (snapshots == null)
        {
            return saveStacks;
        }

        for (int i = 0; i < snapshots.Length; i++)
        {
            HWJ_RuntimeStatOrbStackSnapshot snapshot = snapshots[i];

            if (string.IsNullOrEmpty(snapshot.statOrbId) || snapshot.stackCount <= 0)
            {
                continue;
            }

            saveStacks.Add(new HWJ_SaveStatOrbStackData
            {
                statOrbId = snapshot.statOrbId,
                stackCount = snapshot.stackCount
            });
        }

        return saveStacks;
    }

    private static HWJ_SaveStageRuntimeData CreateStageData(HWJ_RuntimeStageFlowSnapshot snapshot)
    {
        return new HWJ_SaveStageRuntimeData
        {
            stageId = snapshot.stageId,
            nextRegionId = snapshot.nextRegionId,
            currentState = snapshot.currentState,
            objectiveComplete = snapshot.objectiveComplete,
            bossUnlocked = snapshot.bossUnlocked,
            bossBattleStarted = snapshot.bossBattleStarted,
            bossDefeated = snapshot.bossDefeated,
            regionUnlocked = snapshot.regionUnlocked,
            transitionLocked = snapshot.transitionLocked
        };
    }

    private static string ResolveRootObjectId(HWJ_RootObjectDataSO rootObjectData)
    {
        if (rootObjectData == null)
        {
            return null;
        }

        if (rootObjectData.Identity != null && !string.IsNullOrEmpty(rootObjectData.Identity.objectId))
        {
            return rootObjectData.Identity.objectId;
        }

        return rootObjectData.name;
    }
}
