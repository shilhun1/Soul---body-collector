using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_HpRatioCondition", menuName = "HWJ/Data/Rules/Conditions/HP Ratio")]
public class HWJ_HpRatioConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [SerializeField] private HWJ_FloatCompareMode compareMode = HWJ_FloatCompareMode.LessOrEqual;
    [SerializeField] [Range(0f, 1f)] private float hpRatio = 0.5f;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RuntimeStatusSystem status = context.GetStatus(actor);

        if (status == null || status.MaxHp <= 0f)
        {
            return false;
        }

        float currentRatio = Mathf.Clamp01(status.CurrentHp / status.MaxHp);
        return HWJ_ConditionUtility.Compare(currentRatio, compareMode, hpRatio);
    }
}
