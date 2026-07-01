using UnityEngine; // Physics2D와 RaycastHit2D를 사용하기 위해 필요합니다.

public class MagicProjectileSkill : SkillBehaviourBase // 마법 투사체 스킬입니다.
{
    public override void Execute(GameObject caster) // 스킬을 실행합니다.
    {
        if (caster == null || skillData == null) return; // 시전자나 스킬 데이터가 없으면 종료합니다.

        Vector2 origin = caster.transform.position; // 발사 시작 위치입니다.
        Vector2 direction = (Vector2)caster.transform.right; // 발사 방향입니다.

        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, skillData.range); // 발사 방향으로 레이캐스트합니다.

        if (hit.collider == null) return; // 맞은 대상이 없으면 종료합니다.

        CharacterBase target = hit.collider.GetComponent<CharacterBase>(); // 맞은 대상이 데미지를 받을 수 있는지 확인합니다.

        if (target == null) return; // CharacterBase가 없으면 종료합니다.

        DamageInfo damageInfo = new DamageInfo(skillData.damage, skillData.GetDamageType(), caster, direction.normalized); // 데미지 정보를 만듭니다.

        target.TakeDamage(damageInfo); // 대상에게 데미지를 줍니다.
    }
}
