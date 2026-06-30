using UnityEngine;


// 역할 : 데미지 종류 구분.
// 사용 위치 : skill, damage, 공격 판정 코드 등
public enum DamageType
{
    Physical, // 물리 데미지, 거의 모든 공격에 사용
    Magic, // 마법 데미지, 최종보스에서만 사용
    True // 방어력을 무시하는 고정뎀
}
