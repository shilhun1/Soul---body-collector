using System; // Enum.TryParse를 사용하기 위해 필요합니다.
using UnityEngine; // Debug.LogError를 사용하기 위해 필요합니다.

[Serializable] // JSON에서 스킬 데이터를 읽을 수 있게 합니다.
public class SkillJsonData // 스킬 수치와 실제 행동 ID를 담습니다.
{
    public string id; // 스킬 고유 ID입니다.
    public string name; // 스킬 이름입니다.
    public int damage; // 스킬 기본 데미지입니다.
    public string damageType; // 데미지 타입 문자열입니다.
    public float cooldown; // 스킬 쿨타임입니다.
    public float range; // 스킬 사거리입니다.
    public string behaviourId; // SkillBehaviourFactory에서 사용할 행동 ID입니다.

    public DamageType GetDamageType() // 문자열 damageType을 DamageType enum으로 바꿉니다.
    {
        if (string.IsNullOrEmpty(damageType)) // 값이 비어 있으면
        {
            return DamageType.Physical; // 기본값으로 물리 데미지를 사용합니다.
        }

        DamageType parsedType; // 변환 결과를 담을 변수입니다.

        if (Enum.TryParse(damageType, true, out parsedType)) // 대소문자를 무시하고 enum 변환을 시도합니다.
        {
            return parsedType; // 변환된 데미지 타입을 반환합니다.
        }

        Debug.LogError("Invalid damageType: " + damageType); // 잘못된 타입이면 오류를 출력합니다.
        return DamageType.Physical; // 오류가 있어도 기본값으로 물리 데미지를 반환합니다.
    }
}
