using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_GameplayRule", menuName = "HWJ/Data/Rules/Gameplay Rule")]
public class HWJ_GameplayRuleSO : ScriptableObject
{
    [Header("게임플레이 규칙")]
    [Tooltip("규칙을 구분하는 고정 ID입니다.")]
    [InspectorName("규칙 ID")]
    [SerializeField] private string ruleId;
    [Tooltip("이 규칙이 어떤 상황을 허용/차단하는지 적습니다.")]
    [InspectorName("규칙 설명")]
    [SerializeField] [TextArea] private string description;
    [Tooltip("조건이 비어 있을 때 통과로 처리할지 정합니다.")]
    [InspectorName("조건 없음 시 통과")]
    [SerializeField] private bool passWhenNoCondition;
    [Tooltip("이 규칙의 최상위 조건입니다. 조건 그룹을 넣으면 여러 조건을 묶을 수 있습니다.")]
    [InspectorName("최상위 조건")]
    [SerializeField] private HWJ_GameplayConditionSO rootCondition;

    public string RuleId => ruleId;
    public string Description => description;
    public HWJ_GameplayConditionSO RootCondition => rootCondition;

    public bool IsSatisfied(HWJ_GameplayContext context)
    {
        return rootCondition != null ? rootCondition.IsMet(context) : passWhenNoCondition;
    }

    public bool TryEvaluate(HWJ_GameplayContext context, out HWJ_RuleEvaluationResult result)
    {
        if (rootCondition == null)
        {
            result = passWhenNoCondition
                ? HWJ_RuleEvaluationResult.Pass(ruleId, null, "Rule passed because no root condition is set.")
                : HWJ_RuleEvaluationResult.Fail(ruleId, null, "Rule failed because no root condition is set.");
            return passWhenNoCondition;
        }

        bool passed = rootCondition.TryEvaluate(context, out result);
        result.RuleId = ruleId;
        return passed;
    }

    public bool CanExecute(HWJ_GameplayContext context)
    {
        return IsSatisfied(context);
    }
}
