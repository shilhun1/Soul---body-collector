using UnityEngine;

/// <summary>
/// 경험치, 레벨, 스킬 포인트를 관리하는 기본 레벨업 시스템입니다.
/// LevelUpDataSO의 경험치 테이블을 읽고, 레벨업 시 스킬 포인트를 지급합니다.
/// </summary>
public class HWJ_LevelUpSystem : MonoBehaviour
{
    [SerializeField] private HWJ_LevelUpDataSO levelUpData;
    [SerializeField] private string levelTableId = "player_default_level";
    [SerializeField] private bool loadLevelDataFromDatabase = true;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience;
    [SerializeField] private int skillPoint;

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int SkillPoint => skillPoint;
    public float CurrentExperienceRatio
    {
        get
        {
            return TryGetRequiredExperienceForCurrentLevel(out int requiredExperience)
                ? Mathf.Clamp01((float)currentExperience / requiredExperience)
                : 1f;
        }
    }

    private void Awake()
    {
        EnsureLevelData();
    }

    /// <summary>
    /// 경험치를 추가하고 가능한 만큼 레벨업을 반복합니다.
    /// 적/보스 처치 보상의 RewardData.experienceReward를 넘겨 사용하는 흐름을 권장합니다.
    /// </summary>
    public void AddExperience(int amount)
    {
        EnsureLevelData();

        if (amount <= 0 || levelUpData == null)
        {
            return;
        }

        int previousExperience = currentExperience;
        int previousLevel = currentLevel;
        int previousSkillPoint = skillPoint;
        currentExperience += amount;

        while (currentLevel < levelUpData.MaxLevel
            && levelUpData.TryGetRequiredExperience(currentLevel, out int requiredExperience)
            && currentExperience >= requiredExperience)
        {
            currentExperience -= requiredExperience;
            currentLevel++;
            skillPoint += levelUpData.GetSkillPointRewardForReachedLevel(currentLevel);
        }

        HWJ_GameplayEvents.RaiseExperienceChanged(
            new HWJ_ExperienceChangedEvent(this, previousExperience, currentExperience, amount));

        if (previousLevel != currentLevel)
        {
            RaiseSkillPointChangedIfNeeded(previousSkillPoint, HWJ_SkillPointChangeReason.LevelUpReward);
            HWJ_GameplayEvents.RaisePlayerLevelChanged(
                new HWJ_PlayerLevelChangedEvent(this, previousLevel, currentLevel, skillPoint));
        }
    }

    public void AddSkillPoint(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previousSkillPoint = skillPoint;
        skillPoint += amount;
        RaiseSkillPointChangedIfNeeded(previousSkillPoint, HWJ_SkillPointChangeReason.DirectReward);
    }

    public void AddReward(HWJ_RewardData rewardData)
    {
        if (rewardData == null)
        {
            return;
        }

        AddExperience(rewardData.experienceReward);
        AddSkillPoint(rewardData.skillPointReward);
    }

    public bool TryGetRequiredExperienceForCurrentLevel(out int requiredExperience)
    {
        EnsureLevelData();

        if (levelUpData == null)
        {
            requiredExperience = 0;
            return false;
        }

        return levelUpData.TryGetRequiredExperience(currentLevel, out requiredExperience);
    }

    /// <summary>
    /// 스킬 해금이나 강화에 스킬 포인트를 사용합니다.
    /// 포인트가 부족하면 false를 반환합니다.
    /// </summary>
    public bool TrySpendSkillPoint(int amount)
    {
        if (amount <= 0 || skillPoint < amount)
        {
            return false;
        }

        int previousSkillPoint = skillPoint;
        skillPoint -= amount;
        RaiseSkillPointChangedIfNeeded(previousSkillPoint, HWJ_SkillPointChangeReason.SkillUnlockSpend);
        return true;
    }

    public void SetLevelUpData(HWJ_LevelUpDataSO levelUpData)
    {
        this.levelUpData = levelUpData;
    }

    public void RestoreProgress(int level, int experience, int skillPoint)
    {
        EnsureLevelData();

        int maxLevel = levelUpData != null ? Mathf.Max(1, levelUpData.MaxLevel) : int.MaxValue;
        int previousSkillPoint = this.skillPoint;
        currentLevel = Mathf.Clamp(level, 1, maxLevel);
        currentExperience = Mathf.Max(0, experience);
        this.skillPoint = Mathf.Max(0, skillPoint);
        RaiseSkillPointChangedIfNeeded(previousSkillPoint, HWJ_SkillPointChangeReason.Restore);
    }

    private void RaiseSkillPointChangedIfNeeded(int previousSkillPoint, HWJ_SkillPointChangeReason reason)
    {
        if (previousSkillPoint == skillPoint)
        {
            return;
        }

        HWJ_GameplayEvents.RaiseSkillPointChanged(
            new HWJ_SkillPointChangedEvent(this, previousSkillPoint, skillPoint, reason));
    }

    private void EnsureLevelData()
    {
        if (!loadLevelDataFromDatabase || levelUpData != null)
        {
            return;
        }

        HWJ_GameplayDatabaseSO database = HWJ_GameAccess.Database;

        if (database == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(levelTableId)
            && database.TryGetLevelTable(levelTableId, out HWJ_LevelUpDataSO foundById))
        {
            levelUpData = foundById;
            return;
        }

        if (database.LevelTables != null && database.LevelTables.Length > 0)
        {
            levelUpData = database.LevelTables[0];
        }
    }
}
