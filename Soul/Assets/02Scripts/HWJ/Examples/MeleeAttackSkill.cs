using UnityEngine; // Physics2D와 Collider2D를 사용하기 위해 필요합니다.

public class MeleeAttackSkill : SkillBehaviourBase // 근접 공격 스킬입니다.
{
    public override void Execute(GameObject caster) // 스킬을 실행합니다.
    {
        if (caster == null || skillData == null) return; // 시전자나 스킬 데이터가 없으면 종료합니다.

        Collider2D[] hits = Physics2D.OverlapCircleAll(caster.transform.position, skillData.range); // 시전자 주변 범위 안의 콜라이더를 찾습니다.

        foreach (Collider2D hit in hits) // 찾은 콜라이더를 하나씩 확인합니다.
        {
            if (hit.gameObject == caster) continue; // 자기 자신은 공격하지 않습니다.

            CharacterBase target = hit.GetComponent<CharacterBase>(); // 공격 가능한 대상인지 확인합니다.

            if (target == null) continue; // CharacterBase가 없으면 공격하지 않습니다.

            Vector2 direction = (Vector2)(hit.transform.position - caster.transform.position); // 공격 방향을 계산합니다.
            DamageInfo damageInfo = new DamageInfo(skillData.damage, skillData.GetDamageType(), caster, direction.normalized); // 데미지 정보를 만듭니다.

            target.TakeDamage(damageInfo); // 대상에게 데미지를 줍니다.
        }
    }
}
