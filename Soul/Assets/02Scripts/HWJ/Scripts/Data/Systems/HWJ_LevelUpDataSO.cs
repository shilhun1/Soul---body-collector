using UnityEngine;

/// <summary>
/// 레벨업에 필요한 경험치와 지급할 스킬 포인트를 관리하는 ScriptableObject입니다.
/// LevelUpSystem이 현재 레벨 기준 요구 경험치를 계산할 때 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_LevelUpData", menuName = "HWJ/Data/System/Level Up")]
public class HWJ_LevelUpDataSO : ScriptableObject
{
    [Header("레벨업 테이블")]
    [Tooltip("이 레벨업 테이블을 구분하는 고정 ID입니다.")]
    [InspectorName("테이블 ID")]
    [SerializeField] private string tableId;
    [Tooltip("도달 가능한 최대 레벨입니다.")]
    [InspectorName("최대 레벨")]
    [SerializeField] private int maxLevel = 1;
    [Tooltip("레벨업할 때마다 지급할 스킬 포인트입니다.")]
    [InspectorName("레벨당 스킬 포인트")]
    [SerializeField] private int skillPointPerLevel = 1;
    [Tooltip("각 레벨업 구간별로 지급할 스킬 포인트입니다. 비워두면 레벨당 스킬 포인트 값을 사용합니다.")]
    [InspectorName("레벨별 지급 스킬 포인트")]
    [SerializeField] private int[] skillPointRewardsByLevelUp;
    [Tooltip("각 레벨에서 다음 레벨로 가기 위해 필요한 경험치입니다.")]
    [InspectorName("다음 레벨 필요 경험치")]
    [SerializeField] private int[] experienceToNextLevel;

    public string TableId => tableId;
    public int MaxLevel => maxLevel;
    public int SkillPointPerLevel => Mathf.Max(0, skillPointPerLevel);

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 가기 위한 요구 경험치를 반환합니다.
    /// 배열 범위를 벗어나면 레벨업할 수 없는 상태로 false를 반환합니다.
    /// </summary>
    public bool TryGetRequiredExperience(int currentLevel, out int requiredExperience)
    {
        requiredExperience = 0;
        int index = currentLevel - 1;

        if (experienceToNextLevel == null || index < 0 || index >= experienceToNextLevel.Length)
        {
            return false;
        }

        requiredExperience = experienceToNextLevel[index];
        return requiredExperience > 0;
    }

    /// <summary>
    /// 새로 도달한 레벨을 기준으로 지급할 스킬 포인트를 반환합니다.
    /// 예를 들어 Lv.1 -> Lv.2 보상은 reachedLevel 2를 넘겨 조회합니다.
    /// </summary>
    public int GetSkillPointRewardForReachedLevel(int reachedLevel)
    {
        int index = reachedLevel - 2;

        if (skillPointRewardsByLevelUp != null
            && index >= 0
            && index < skillPointRewardsByLevelUp.Length)
        {
            return Mathf.Max(0, skillPointRewardsByLevelUp[index]);
        }

        return SkillPointPerLevel;
    }
}
