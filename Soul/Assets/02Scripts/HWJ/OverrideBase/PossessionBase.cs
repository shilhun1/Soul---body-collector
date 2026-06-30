using UnityEngine;

public class PossessionBase : MonoBehaviour // 모든 빙의 행동 클래스의 부모
{
    protected PossessionBodyJsonData bodyData; // 현재 빙의체 데이터 저장
    protected float remainDuration; // 남은 빙의 시간 저장

    public virtual void OnPossess(PossessionBodyJsonData data) // 빙의가 시작될 때 호출
    {
        bodyData = data; // 빙의체 데이터 저장
        remainDuration = data != null ? data.duration : 0f; // 지속 시간 설정
    }

    public virtual void OnRelease() // 빙의 해제할 때 호출
    {
        bodyData = null; // 빙의체 데이터 비우기
        remainDuration = 0f; // 남은 시간 0으로 만듬
    }

    public virtual void TickDecay(float deltaTime) //매 프레임 빙의 시간 감소
    {
        remainDuration -= deltaTime; // 지난 시간만큼 남은 시간 줄임
    }

    public bool IsExpired() // 빙의 시간이 끝났는지 확인
    {
        return remainDuration <= 0f; // 남은 시간이 0 이하면 true 반환
    }

    //public abstract void UsePossesionSkill(int skillIndex); // 빙의 스킬 사용 함수
}
