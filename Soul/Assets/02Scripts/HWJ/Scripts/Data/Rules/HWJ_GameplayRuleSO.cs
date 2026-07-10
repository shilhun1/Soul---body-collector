using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_GameplayRule", menuName = "HWJ/Data/Rules/Gameplay Rule")]
public class HWJ_GameplayRuleSO : ScriptableObject
{
    [SerializeField] private string ruleId;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private bool passWhenNoCondition;
    [SerializeField] private HWJ_GameplayConditionSO rootCondition;

    public string RuleId => ruleId;
    public string Description => description;
    public HWJ_GameplayConditionSO RootCondition => rootCondition;

    public bool IsSatisfied(HWJ_GameplayContext context)
    {
        return rootCondition != null ? rootCondition.IsMet(context) : passWhenNoCondition;
    }

    public bool CanExecute(HWJ_GameplayContext context)
    {
        return IsSatisfied(context);
    }
}
