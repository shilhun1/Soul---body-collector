using System;
using UnityEngine;

/// <summary>
/// NPC와 상호작용할 때 필요한 범위와 보상성 버프 정보를 관리합니다.
/// 대화 시스템, 요정 버프, 조사 상호작용에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_NPCInteractionData
{
    [Header("NPC 상호작용")]
    [InspectorName("상호작용 ID")]
    [Tooltip("이 상호작용을 구분하는 고정 ID입니다.")]
    public string interactionId;
    [InspectorName("상호작용 거리")]
    [Tooltip("플레이어가 이 거리 안에 있어야 상호작용할 수 있습니다.")]
    public float interactionRange;
    [InspectorName("임시 스탯 버프 지급")]
    [Tooltip("켜면 상호작용 시 임시 스탯 버프를 지급합니다.")]
    public bool givesTemporaryStatBuff;
    [InspectorName("스탯 버프 ID")]
    [Tooltip("지급할 버프 또는 StatOrbData ID입니다.")]
    public string statBuffId;
    [InspectorName("스탯 버프 지속 시간")]
    [Tooltip("임시 스탯 버프가 유지되는 시간입니다.")]
    public float statBuffDurationSeconds;
}
