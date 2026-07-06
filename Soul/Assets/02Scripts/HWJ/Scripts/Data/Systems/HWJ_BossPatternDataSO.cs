using UnityEngine;

/// <summary>
/// 보스가 사용할 하나의 패턴 데이터를 정의하는 ScriptableObject입니다.
/// BossPatternSystem이 HP 조건, 쿨타임, 가중치를 기준으로 실행 가능한 패턴을 선택합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_BossPatternData", menuName = "HWJ/Data/System/Boss Pattern")]
public class HWJ_BossPatternDataSO : ScriptableObject
{
    [SerializeField] private string patternId;
    [SerializeField] private HWJ_BossPatternTrigger trigger;
    [SerializeField] private float hpRatio = 1f;
    [SerializeField] private float cooldownSeconds;
    [SerializeField] private int weight = 1;
    [SerializeField] private string animationId;
    [SerializeField] private HWJ_SkillActionDataSO[] skillActions;

    public string PatternId => patternId;
    public HWJ_BossPatternTrigger Trigger => trigger;
    public float HpRatio => hpRatio;
    public float CooldownSeconds => cooldownSeconds;
    public int Weight => weight;
    public string AnimationId => animationId;
    public HWJ_SkillActionDataSO[] SkillActions => skillActions;

    /// <summary>
    /// 현재 보스 HP 비율에서 이 패턴이 실행 가능한지 확인합니다.
    /// 실제 쿨타임 여부는 BossPatternSystem이 별도로 관리합니다.
    /// </summary>
    public bool IsHpConditionMatched(float currentHpRatio)
    {
        return trigger != HWJ_BossPatternTrigger.HpBelowRatio || currentHpRatio <= hpRatio;
    }
}
