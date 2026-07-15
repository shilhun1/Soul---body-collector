using System;
using UnityEngine;

/// <summary>
/// 하나의 스킬 슬롯에 들어가는 데이터입니다.
/// 플레이어 스킬트리, 적 스킬 사이클, 보스 페이즈 스킬 목록에서 공통으로 사용합니다.
/// </summary>
[Serializable]
public class HWJ_SkillEntryData
{
    [Header("스킬 항목")]
    [InspectorName("스킬 ID")]
    [Tooltip("실제로 실행할 SkillActionDataSO의 ID입니다.")]
    public string skillId;
    [InspectorName("필요 무기")]
    [Tooltip("None이면 어떤 무기든 사용할 수 있습니다.")]
    public HWJ_WeaponType requiredWeaponType;
    [InspectorName("해금 레벨")]
    [Tooltip("플레이어 성장에서 이 스킬을 해금할 레벨입니다.")]
    public int unlockLevel;
    [InspectorName("필요 스킬 포인트")]
    [Tooltip("해금에 필요한 스킬 포인트입니다.")]
    public int requiredSkillPoint;
    [InspectorName("처음부터 사용 가능")]
    [Tooltip("켜면 별도 해금 없이 바로 사용할 수 있습니다.")]
    public bool startsUnlocked;
    [InspectorName("스킬 쿨타임")]
    [Tooltip("이 스킬 항목 전용 쿨타임입니다. 0이면 SkillActionDataSO 또는 몬스터 기본값을 사용합니다.")]
    public float cooldownSeconds;
    [InspectorName("AI 사용 간격")]
    [Tooltip("몬스터/보스 AI가 이 스킬 사용 후 다음 공격 판단까지 기다리는 시간입니다. 스킬 쿨타임과는 별도입니다.")]
    public float useIntervalSeconds;
}

/// <summary>
/// 여러 스킬을 하나의 세트로 묶는 데이터입니다.
/// TypeData에서 스킬 목록을 배열로 관리할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_SkillSetData
{
    [Header("스킬 목록")]
    [InspectorName("스킬들")]
    [Tooltip("이 유형이 사용할 수 있는 스킬 ID 목록입니다.")]
    public HWJ_SkillEntryData[] skills;
}

/// <summary>
/// 플레이어 성장과 스킬트리 해금에 필요한 데이터입니다.
/// 경험치, 레벨, 스킬 포인트 시스템이 PlayerTypeDataSO.Growth를 통해 사용합니다.
/// </summary>
[Serializable]
public class HWJ_GrowthData
{
    [Header("성장")]
    [InspectorName("시작 레벨")]
    [Tooltip("처음 시작할 레벨입니다.")]
    public int startLevel = 1;
    [InspectorName("최대 레벨")]
    [Tooltip("도달 가능한 최대 레벨입니다.")]
    public int maxLevel;
    [InspectorName("시작 스킬 포인트")]
    [Tooltip("처음부터 보유할 스킬 포인트입니다.")]
    public int startSkillPoint;
    [InspectorName("스킬 트리 ID")]
    [Tooltip("사용할 스킬 트리 데이터의 ID입니다.")]
    public string skillTreeId;
    [InspectorName("레벨업 필요 경험치")]
    [Tooltip("현재 레벨에서 다음 레벨로 가기 위해 필요한 경험치 배열입니다.")]
    public int[] experienceToLevelUp;
}
