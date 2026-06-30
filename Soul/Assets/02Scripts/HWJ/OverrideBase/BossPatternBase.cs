using UnityEngine;

public abstract class BossPatternBase : MonoBehaviour // 보스 패턴 클래스들의 부모
{
    protected string patternId; // 현재 패턴 ID

    public virtual void Init(string id) // 보스 패턴 초기화
    {
        patternId = id; // 패턴 id 저장
    }

    public abstract void ExecutePattern(GameObject boss); // 자식 클래스가 하는 패턴 실행 함수
}
