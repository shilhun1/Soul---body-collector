using UnityEngine;

public class DamageInfo
{
    public int amount; //데미지 수치
    public DamageType damageType; // 데미지 타입
    public GameObject attacker; // 공격한 오브젝트
    public Vector2 attackDirection; // 공격 방향

    public DamageInfo(int amount, DamageType damageType, GameObject attacker, Vector2 attackDirection) // 데미지 정보 만드는 생성자
    {
        this.amount = amount; // 전달받은 데미지 수치 저장
        this.damageType = damageType; // 전달받은 데미지 타입 저장
        this.attacker = attacker; // 전달받은 공격자 오브젝트 저장
        this.attackDirection = attackDirection; // 전달받은 공격 방향 저장
    }
}
