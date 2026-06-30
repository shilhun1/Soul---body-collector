using UnityEngine;

public class CharacterBase : MonoBehaviour // 플레이어, 적, 보스가 상속할 기본 캐릭터 클래스
{
    public int currentHp; // 현재 체력
    public StatData currentStats; // 현재 적용 중인 능력치

    public virtual void Init(StatData stats) // 캐릭터 능력치를 초기화
    {
        if(stats == null) // 전달된 능력치 없는지 확인
        {
            currentStats = new StatData(); // 빈 능력치 생성
        }
        else
        {
            currentStats = stats.Clone(); // 원본을 보호하기 위해 복사해서 저장
        }

        currentHp = Mathf.Max(1, currentStats.maxHp); //  현재 체력을 최대 체력으로 저장
    }

    public virtual void TakeDamage(DamageInfo damageInfo) // 데미지를 받습니다.
    {
        if(damageInfo == null) // 데미지 정보가 없는지 확인
        {
            Debug.LogError(name + "에게 null DamageInfo가 전달되었습니다."); // 오류 로그 출력
            return; // 함수를 종료
        }

        int finalDamage = CalculateDamage(damageInfo); // 최종 데미지 계산
        currentHp -= finalDamage; // 체력 감소 시킴

        if (currentHp <= 0) // 체력이 0 이하인지 확인
        {
            currentHp = 0; // 체력 0 고정
            //Die(); //사망처리
        }
    }

    protected virtual int CalculateDamage(DamageInfo damageInfo) // 방어력을 반영한 최종 데미지를 계산
    {
        if (damageInfo.damageType == DamageType.True) // 고정 데미지인지 확인
        {
            return Mathf.Max(1, damageInfo.amount); // 방어력을 무시하고 최소 1 데미지를 반환
        }

        int defense = currentStats != null ? currentStats.defense : 0; // 현재 방어력 가져옴
        return Mathf.Max(1, damageInfo.amount - defense); // 방어력을 뺀 최종 데미지를 반환
    }

    //public abstract void Die();
}
