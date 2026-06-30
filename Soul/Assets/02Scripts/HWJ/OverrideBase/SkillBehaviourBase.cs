using UnityEngine;

public class SkillBehaviourBase : MonoBehaviour // 모든 스킬 행동 클래스의 부모
{
    protected SkillJsonData skillData; // 실행할 스킬 데이터 저장

    public virtual void Init(SkillJsonData data) // 스킬 데이터 초기화
    {
        skillData = data; // 전달받은 스킬 데이터 저장
    }

    //public abstract void Execute(GameObject caster); // 자식 클래스가 구현해야 하는 스킬 실행 함수
}
