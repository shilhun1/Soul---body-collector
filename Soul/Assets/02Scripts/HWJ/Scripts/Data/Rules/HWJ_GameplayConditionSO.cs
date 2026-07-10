using UnityEngine;

public abstract class HWJ_GameplayConditionSO : ScriptableObject
{
    [SerializeField] private string conditionId;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private bool invertResult;
    [SerializeField] private bool passWhenContextMissing;

    public string ConditionId => conditionId;
    public string Description => description;
    public bool InvertResult => invertResult;

    public bool IsMet(HWJ_GameplayContext context)
    {
        bool result = context != null ? Evaluate(context) : passWhenContextMissing;
        return invertResult ? !result : result;
    }

    protected abstract bool Evaluate(HWJ_GameplayContext context);
}
