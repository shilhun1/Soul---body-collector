using System;
using UnityEngine;

[Serializable]
public class HWJ_BossFSMData
{
    public bool autoStartWhenPlayerEntersRoom = true;
    public bool useBossRoomBounds = true;
    public Vector2 bossRoomOffset;
    public Vector2 bossRoomSize = new Vector2(28f, 14f);
    public float attackStartRange = 5f;
    public float optimalAttackDistance = 3.5f;
    public float closeSkillRange = 4f;
    public float chaseMoveSpeedMultiplier = 1f;
    public float phaseTwoHpRatio = 0.5f;
    public float phaseTransitionSeconds = 1.5f;
    public float cameraFocusSeconds = 2f;
    public float soulReturnCenterStoppingDistance = 0.2f;
    public bool superArmorDuringAttack = true;
    public bool superArmorDuringPhaseTransition = true;
    public int groggyHitCountThreshold = 5;
    public float groggyHitWindowSeconds = 3f;
    public float groggyDurationSeconds = 2.5f;
    public float groggyDamageMultiplier = 1.5f;
    public bool knockbackOnGroggyEnd = true;
    public float groggyEndKnockbackPower = 8f;
}

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
