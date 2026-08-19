using UnityEngine;

/// <summary>
/// Animator의 Animation Event를 EnemyAttackSystem에 전달합니다.
/// 애니메이션 담당자는 타격 프레임에 ApplyBasicAttackHit, 종료 프레임에 CompleteBasicAttack을 등록합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_EnemyAttackAnimationRelay : MonoBehaviour
{
    [Header("공격 시스템")]
    [SerializeField] private HWJ_EnemyAttackSystem enemyAttackSystem;

    private void Awake()
    {
        ResolveAttackSystem();
    }

    public void ApplyBasicAttackHit()
    {
        ResolveAttackSystem();
        enemyAttackSystem?.ApplyPreparedBasicAttackHit();
    }

    public void CompleteBasicAttack()
    {
        ResolveAttackSystem();
        enemyAttackSystem?.CompletePreparedBasicAttack();
    }

    public void CancelBasicAttack()
    {
        ResolveAttackSystem();
        enemyAttackSystem?.CancelPreparedBasicAttack();
    }

    private void ResolveAttackSystem()
    {
        if (enemyAttackSystem == null)
        {
            enemyAttackSystem = GetComponentInParent<HWJ_EnemyAttackSystem>();
        }
    }
}
