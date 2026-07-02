using System;

/// <summary>
/// 하나의 스킬 슬롯에 들어가는 데이터입니다.
/// 플레이어 스킬트리, 적 스킬 사이클, 보스 페이즈 스킬 목록에서 공통으로 사용합니다.
/// </summary>
[Serializable]
public class HWJ_SkillEntryData
{
    public string skillId;
    public HWJ_WeaponType requiredWeaponType;
    public int unlockLevel;
    public int requiredSkillPoint;
    public bool startsUnlocked;
    public float cooldownSeconds;
    public float useIntervalSeconds;
}

/// <summary>
/// 여러 스킬을 하나의 세트로 묶는 데이터입니다.
/// TypeData에서 스킬 목록을 배열로 관리할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_SkillSetData
{
    public HWJ_SkillEntryData[] skills;
}

/// <summary>
/// 플레이어 성장과 스킬트리 해금에 필요한 데이터입니다.
/// 경험치, 레벨, 스킬 포인트 시스템이 PlayerTypeDataSO.Growth를 통해 사용합니다.
/// </summary>
[Serializable]
public class HWJ_GrowthData
{
    public int startLevel = 1;
    public int maxLevel;
    public int startSkillPoint;
    public string skillTreeId;
    public int[] experienceToLevelUp;
}
