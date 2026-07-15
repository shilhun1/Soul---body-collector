using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_StatusFlagCondition", menuName = "HWJ/Data/Rules/Conditions/Status Flag")]
public class HWJ_StatusFlagConditionSO : HWJ_GameplayConditionSO
{
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [SerializeField] private HWJ_StatusFlag flag = HWJ_StatusFlag.Alive;
    [SerializeField] private bool expectedValue = true;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        HWJ_RuntimeStatusSystem status = context.GetStatus(actor);

        if (status == null)
        {
            return false;
        }

        bool value;

        switch (flag)
        {
            case HWJ_StatusFlag.Alive:
                value = !status.IsDead;
                break;
            case HWJ_StatusFlag.Dead:
                value = status.IsDead;
                break;
            case HWJ_StatusFlag.CanMove:
                value = status.CanMove;
                break;
            case HWJ_StatusFlag.CanAttack:
                value = status.CanAttack;
                break;
            case HWJ_StatusFlag.CanDash:
                value = status.CanDash;
                break;
            case HWJ_StatusFlag.HitStunned:
                value = status.IsHitStunned;
                break;
            case HWJ_StatusFlag.TemporarilyInvincible:
                value = status.IsTemporarilyInvincible;
                break;
            case HWJ_StatusFlag.HitReactionImmune:
                value = status.IsHitReactionImmune;
                break;
            case HWJ_StatusFlag.HitReactionLimited:
                value = status.IsHitReactionLimited;
                break;
            case HWJ_StatusFlag.SuperArmor:
                value = status.HasSuperArmor;
                break;
            case HWJ_StatusFlag.UsesHp:
                value = status.UsesHp;
                break;
            case HWJ_StatusFlag.HasHpRemaining:
                value = !status.UsesHp || status.CurrentHp > 0f;
                break;
            case HWJ_StatusFlag.IgnoreKnockback:
                value = status.ShouldIgnoreKnockback;
                break;
            default:
                value = false;
                break;
        }

        return value == expectedValue;
    }
}
