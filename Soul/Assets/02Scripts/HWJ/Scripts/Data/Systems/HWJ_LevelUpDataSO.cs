using UnityEngine;

/// <summary>
/// 레벨업에 필요한 경험치와 지급할 스킬 포인트를 관리하는 ScriptableObject입니다.
/// LevelUpSystem이 현재 레벨 기준 요구 경험치를 계산할 때 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_LevelUpData", menuName = "HWJ/Data/System/Level Up")]
public class HWJ_LevelUpDataSO : ScriptableObject
{
    [SerializeField] private string tableId;
    [SerializeField] private int maxLevel = 1;
    [SerializeField] private int skillPointPerLevel = 1;
    [SerializeField] private int[] experienceToNextLevel;

    public string TableId => tableId;
    public int MaxLevel => maxLevel;
    public int SkillPointPerLevel => skillPointPerLevel;

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
}
