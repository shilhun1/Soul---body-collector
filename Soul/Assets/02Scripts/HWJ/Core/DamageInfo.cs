using UnityEngine; // GameObject와 Vector2를 사용하기 위해 필요합니다.

public class DamageInfo // 공격 한 번에 필요한 데미지 정보를 담는 클래스입니다.
{
    public int amount; // 최종 계산 전 기본 데미지 수치입니다.
    public DamageType damageType; // 물리, 마법, 고정 데미지 중 어떤 타입인지 저장합니다.
    public GameObject attacker; // 공격을 실행한 오브젝트입니다.
    public Vector2 attackDirection; // 공격이 들어온 방향입니다.

    public DamageInfo(int amount, DamageType damageType, GameObject attacker, Vector2 attackDirection) // 데미지 정보를 생성합니다.
    {
        this.amount = amount; // 데미지 수치를 저장합니다.
        this.damageType = damageType; // 데미지 타입을 저장합니다.
        this.attacker = attacker; // 공격자 오브젝트를 저장합니다.
        this.attackDirection = attackDirection; // 공격 방향을 저장합니다.
    }
}
