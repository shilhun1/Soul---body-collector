using UnityEngine; // MonoBehaviour를 사용하기 위해 필요합니다.

public abstract class PossessionBase : MonoBehaviour // 모든 빙의 행동 클래스의 부모입니다.
{
    protected PossessionBodyJsonData bodyData; // 현재 빙의체 데이터를 저장합니다.
    protected float remainDuration; // 남은 빙의 시간을 저장합니다.

    public float RemainDuration // 외부에서 남은 빙의 시간을 읽을 수 있게 합니다.
    {
        get { return remainDuration; } // 남은 시간을 반환합니다.
    }

    public virtual void OnPossess(PossessionBodyJsonData data) // 빙의가 시작될 때 호출됩니다.
    {
        bodyData = data; // 빙의체 데이터를 저장합니다.
        remainDuration = data != null ? data.duration : 0f; // JSON의 지속 시간을 남은 시간으로 설정합니다.
    }

    public virtual void OnRelease() // 빙의가 해제될 때 호출됩니다.
    {
        bodyData = null; // 빙의체 데이터를 비웁니다.
        remainDuration = 0f; // 남은 시간을 0으로 만듭니다.
    }

    public virtual void TickDecay(float deltaTime) // 매 프레임 빙의 시간을 감소시킵니다.
    {
        if (remainDuration <= 0f) // 이미 시간이 끝났으면
        {
            return; // 더 줄이지 않습니다.
        }

        remainDuration -= deltaTime; // 지난 시간만큼 감소시킵니다.

        if (remainDuration < 0f) // 음수가 되면
        {
            remainDuration = 0f; // 0으로 고정합니다.
        }
    }

    public bool IsExpired() // 빙의 시간이 끝났는지 확인합니다.
    {
        return remainDuration <= 0f; // 남은 시간이 0 이하이면 끝난 상태입니다.
    }

    public abstract void UsePossessionSkill(int skillIndex); // 자식 빙의 클래스가 빙의 스킬 사용 방식을 구현해야 합니다.
}
