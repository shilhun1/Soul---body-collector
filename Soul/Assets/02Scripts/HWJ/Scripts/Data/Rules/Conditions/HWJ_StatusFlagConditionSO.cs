using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_StatusFlagCondition", menuName = "HWJ/Data/Rules/Conditions/Status Flag")]
public class HWJ_StatusFlagConditionSO : HWJ_GameplayConditionSO
{
    [Header("상태 플래그 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽의 상태 플래그를 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Target;
    [Tooltip("검사할 상태 플래그입니다.")]
    [InspectorName("상태 플래그")]
    [SerializeField] private HWJ_StatusFlag flag = HWJ_StatusFlag.Alive;
    [Tooltip("해당 플래그가 이 값과 같아야 조건을 통과합니다.")]
    [InspectorName("기대값")]
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
