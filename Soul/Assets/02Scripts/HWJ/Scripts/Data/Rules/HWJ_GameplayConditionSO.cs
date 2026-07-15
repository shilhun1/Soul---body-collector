using UnityEngine;

public abstract class HWJ_GameplayConditionSO : ScriptableObject
{
    [Header("조건 기본 정보")]
    [Tooltip("조건을 구분하는 고정 ID입니다. 규칙과 데이터베이스에서 참조할 수 있습니다.")]
    [InspectorName("조건 ID")]
    [SerializeField] private string conditionId;
    [Tooltip("이 조건이 무엇을 검사하는지 기획자가 읽을 수 있게 적습니다.")]
    [InspectorName("조건 설명")]
    [SerializeField] [TextArea] private string description;
    [Tooltip("켜면 조건 결과를 반대로 뒤집습니다. 참이면 거짓, 거짓이면 참이 됩니다.")]
    [InspectorName("결과 반전")]
    [SerializeField] private bool invertResult;
    [Tooltip("검사할 대상 정보가 없을 때 통과로 처리할지 정합니다.")]
    [InspectorName("컨텍스트 없음 시 통과")]
    [SerializeField] private bool passWhenContextMissing;

    public string ConditionId => conditionId;
    public string Description => description;
    public bool InvertResult => invertResult;

    public bool IsMet(HWJ_GameplayContext context)
    {
        bool result = context != null ? Evaluate(context) : passWhenContextMissing;
        return invertResult ? !result : result;
    }

    public virtual bool TryEvaluate(
        HWJ_GameplayContext context,
        out HWJ_RuleEvaluationResult result)
    {
        bool rawResult = context != null ? Evaluate(context) : passWhenContextMissing;
        bool passed = invertResult ? !rawResult : rawResult;
        string message = passed
            ? $"{GetConditionLabel()} passed."
            : $"{GetConditionLabel()} failed.";

        result = passed
            ? HWJ_RuleEvaluationResult.Pass(null, conditionId, message)
            : HWJ_RuleEvaluationResult.Fail(null, conditionId, message);
        return passed;
    }

    protected string GetConditionLabel()
    {
        if (!string.IsNullOrEmpty(conditionId))
        {
            return conditionId;
        }

        return name;
    }

    protected abstract bool Evaluate(HWJ_GameplayContext context);
}
