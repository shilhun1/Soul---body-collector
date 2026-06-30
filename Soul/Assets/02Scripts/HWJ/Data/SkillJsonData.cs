using UnityEngine;
using System;


[Serializable]
public class SkillJsonData
{
    public string id; // 스킬 고유 ID
    public string name; // 스킬 이름
    public int damage; // 스킬 기본 데미지
    public string damageType; // 데미지 타입 문자열
    public float cooldown; // 스킬 쿨타임
    public float range; // 스킬 사거리
    public string behaviourId; // 실제 스킬 행동 클래스와 연결할 ID

    public DamageType GetDamageType() // 문자열 damageType을 DamageType enum으로 변환
    {
        if (string.IsNullOrEmpty(damageType)) // 데미지 타입 문자열이 비어 있는지확인
        {
            return DamageType.Physical; // 기본값으로 물리 뎀지 반환
        }

        DamageType parsedType; // 변환된 데미지 타입을 저장할 변수

        if (Enum.TryParse(damageType, true, out parsedType)) // 문자열을 enum으로 변환
        {
            return parsedType; // 변환에 성공하면 변환된 타입 반환
        }

        Debug.LogError("Invalid damageType : " + damageType); // 잘못된 데미지 타입을 로그로 출력
        return DamageType.Physical; // 오류가 있어도 기본값으로 물리 뎀지 반환
    }
}
