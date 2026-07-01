using UnityEngine; // MonoBehaviour와 GameObject를 사용하기 위해 필요합니다.

public abstract class BossPatternBase : MonoBehaviour // 모든 보스 패턴 클래스의 부모입니다.
{
    protected string patternId; // 현재 패턴 ID입니다.

    public virtual void Init(string id) // 패턴을 초기화합니다.
    {
        patternId = id; // 패턴 ID를 저장합니다.
    }

    public abstract void ExecutePattern(GameObject boss); // 자식 패턴 클래스가 실제 패턴 실행을 구현해야 합니다.
}
