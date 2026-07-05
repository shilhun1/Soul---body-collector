using UnityEngine; // MonoBehaviour와 Mathf를 사용하기 위해 필요합니다.

public abstract class CharacterBase : MonoBehaviour // 플레이어, 몬스터, 보스가 공통으로 상속할 캐릭터 기본 클래스입니다.
{
    public int currentHp; // 현재 체력입니다.
    public StatData currentStats; // 현재 적용 중인 최종 능력치입니다.

    public virtual void Init(StatData stats) // 캐릭터 능력치를 초기화합니다.
    {
        currentStats = stats != null ? stats.Clone() : new StatData(); // 원본 능력치를 보호하기 위해 복사본을 저장합니다.
        currentHp = Mathf.Max(1, currentStats.maxHp); // 현재 체력을 최대 체력으로 설정하되 최소 1을 보장합니다.
    }

    public virtual void TakeDamage(DamageInfo damageInfo) // 데미지를 받는 함수입니다.
    {
        if (damageInfo == null) // 데미지 정보가 없으면
        {
            Debug.LogError(name + "에게 null DamageInfo가 전달되었습니다."); // 오류를 출력합니다.
            return; // 처리하지 않습니다.
        }

        int finalDamage = CalculateDamage(damageInfo); // 방어력과 타입을 반영한 최종 데미지를 계산합니다.
        currentHp -= finalDamage; // 현재 체력을 감소시킵니다.

        if (currentHp <= 0) // 체력이 0 이하가 되면
        {
            currentHp = 0; // 체력을 0으로 고정합니다.
            Die(); // 자식 클래스의 사망 처리를 실행합니다.
        }
    }

    protected virtual int CalculateDamage(DamageInfo damageInfo) // 최종 데미지를 계산합니다.
    {
        if (damageInfo.damageType == DamageType.True) // 고정 데미지라면
        {
            return Mathf.Max(1, damageInfo.amount); // 방어력을 무시하고 최소 1 데미지를 보장합니다.
        }

        int defense = currentStats != null ? currentStats.defense : 0; // 현재 방어력을 가져옵니다.
        return Mathf.Max(1, damageInfo.amount - defense); // 방어력을 뺀 값을 반환하되 최소 1 데미지를 보장합니다.
    }

    public abstract void Die(); // 플레이어와 적이 각자 다른 사망 처리를 구현해야 합니다.
}
