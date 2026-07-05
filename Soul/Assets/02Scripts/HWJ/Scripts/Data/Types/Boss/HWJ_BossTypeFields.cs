using System;

/// <summary>
/// 보스의 HP 비율별 페이즈 데이터를 관리합니다.
/// 보스 전투 시스템이 현재 HP 비율을 기준으로 스킬 세트나 스테이지 이벤트를 바꿀 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_BossPhaseData
{
    public string phaseId;
    public float startHpRatio = 1f;
    public HWJ_SkillSetData skillSet = new HWJ_SkillSetData();
    public string stageEventId;
    public bool changesStage;
}

/// <summary>
/// 보스가 특정 HP 비율에서 변신하는 규칙을 관리합니다.
/// 최종 보스 2페이즈처럼 모델, 애니메이션, 패턴 전환이 필요할 때 사용합니다.
/// </summary>
[Serializable]
public class HWJ_BossTransformData
{
    public bool canTransform;
    public float transformHpRatio = 0.5f;
    public string transformedModelId;
    public string transformAnimationId;
}
