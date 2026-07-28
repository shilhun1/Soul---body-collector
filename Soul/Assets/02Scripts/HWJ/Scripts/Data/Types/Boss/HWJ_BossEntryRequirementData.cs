using System;
using UnityEngine;

[Serializable]
// Stored on BossTypeDataSO so each boss can define entry gates without mutating runtime state.
public class HWJ_BossEntryRequirementData
{
    [Header("스테이지 조건")]
    [InspectorName("스테이지 목표 완료 필요")]
    [Tooltip("켜면 스테이지 목표가 완료되어야 보스전에 들어갈 수 있습니다.")]
    public bool requireObjectiveComplete = true;
    [InspectorName("보스 해금 필요")]
    [Tooltip("켜면 보스가 해금된 상태여야 보스전에 들어갈 수 있습니다.")]
    public bool requireBossUnlocked = true;
    [InspectorName("미처치 보스 필요")]
    [Tooltip("켜면 이미 처치한 보스전에는 다시 들어갈 수 없습니다.")]
    public bool requireBossNotDefeated = true;
    [InspectorName("입장 트리거 활성화 필요")]
    [Tooltip("켜면 보스 입장 트리거가 활성화되어 있어야 합니다.")]
    public bool requireEntryTriggerActive = true;

    [Header("빙의한 몸 조건")]
    [InspectorName("빙의 상태 필요")]
    [Tooltip("켜면 플레이어가 몸에 빙의한 상태여야 보스전에 들어갈 수 있습니다.")]
    public bool requirePossessedBody = true;
    [InspectorName("붕괴 중이면 금지")]
    [Tooltip("켜면 현재 몸이 붕괴 중일 때 보스전에 들어갈 수 없습니다.")]
    public bool requireBodyNotCollapsing = true;
    [InspectorName("몸 오브젝트 유형 검사")]
    [Tooltip("켜면 아래 오브젝트 유형과 일치하는 몸만 허용합니다.")]
    public bool requireBodyObjectType;
    [InspectorName("필요 몸 유형")]
    [Tooltip("허용할 빙의 몸의 오브젝트 유형입니다.")]
    public HWJ_ObjectType requiredBodyObjectType = HWJ_ObjectType.Enemy;
    [InspectorName("필요 무기")]
    [Tooltip("보스전에 입장하기 위해 필요한 빙의 몸의 무기입니다. None이면 무기 제한이 없습니다.")]
    public HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.None;
    [InspectorName("최소 현재 HP")]
    [Tooltip("빙의한 몸의 현재 HP가 이 값 이상이어야 합니다.")]
    public float minimumCurrentHp = 1f;
    [InspectorName("최대 정신력 소모 비율")]
    [Tooltip("빙의한 몸의 정신력 소모 비율이 이 값 이하여야 합니다. 0.3이면 정신력을 30% 이하로만 소모한 상태여야 합니다.")]
    [Range(0f, 1f)] public float maximumCurrentDecayRatio = 1f;

    [Header("스킬 조건")]
    [InspectorName("필요 해금 스킬 ID")]
    [Tooltip("보스전에 입장하기 위해 해금되어 있어야 하는 스킬 ID 목록입니다.")]
    public string[] requiredUnlockedSkillIds;

    [Header("몸 ID 필터")]
    [InspectorName("허용 몸 오브젝트 ID")]
    [Tooltip("비어 있으면 제한 없음. 값이 있으면 이 ID의 몸만 허용합니다.")]
    public string[] allowedBodyObjectIds;
    [InspectorName("금지 몸 오브젝트 ID")]
    [Tooltip("이 ID의 몸은 보스전 입장을 막습니다.")]
    public string[] blockedBodyObjectIds;

    [Header("추가 규칙")]
    [InspectorName("추가 규칙 실행 코어")]
    [Tooltip("보스전 입장 조건을 더 검사할 RuleExecutionCore SO입니다.")]
    public HWJ_RuleExecutionCoreSO additionalRuleExecutionCore;
    [InspectorName("추가 규칙 실행 코어 ID")]
    [Tooltip("SO 직접 참조가 없을 때 데이터베이스에서 찾을 RuleExecutionCore ID입니다.")]
    public string additionalRuleExecutionCoreId = "boss_entry_execution";
}
