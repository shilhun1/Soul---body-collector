using UnityEngine;

/// <summary>
/// 경험치, 레벨, 스킬 포인트를 관리하는 기본 레벨업 시스템입니다.
/// LevelUpDataSO의 경험치 테이블을 읽고, 레벨업 시 스킬 포인트를 지급합니다.
/// </summary>
public class HWJ_LevelUpSystem : MonoBehaviour
{
    [SerializeField] private HWJ_LevelUpDataSO levelUpData;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience;
    [SerializeField] private int skillPoint;

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int SkillPoint => skillPoint;

    /// <summary>
    /// 경험치를 추가하고 가능한 만큼 레벨업을 반복합니다.
    /// 적/보스 처치 보상의 RewardData.experienceReward를 넘겨 사용하는 흐름을 권장합니다.
    /// </summary>
    public void AddExperience(int amount)
    {
        if (amount <= 0 || levelUpData == null)
        {
            return;
        }

        currentExperience += amount;

        while (currentLevel < levelUpData.MaxLevel
            && levelUpData.TryGetRequiredExperience(currentLevel, out int requiredExperience)
            && currentExperience >= requiredExperience)
        {
            currentExperience -= requiredExperience;
            currentLevel++;
            skillPoint += levelUpData.SkillPointPerLevel;
        }
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

        skillPoint -= amount;
        return true;
    }
}
