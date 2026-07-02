using System;

/// <summary>
/// NPC와 상호작용할 때 필요한 범위와 보상성 버프 정보를 관리합니다.
/// 대화 시스템, 요정 버프, 조사 상호작용에서 사용합니다.
/// </summary>
[Serializable]
public class HWJ_NPCInteractionData
{
    public string interactionId;
    public float interactionRange;
    public bool givesTemporaryStatBuff;
    public string statBuffId;
    public float statBuffDurationSeconds;
}
