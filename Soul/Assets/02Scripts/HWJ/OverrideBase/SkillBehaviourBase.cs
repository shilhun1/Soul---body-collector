using UnityEngine; // MonoBehaviour와 GameObject를 사용하기 위해 필요합니다.

public abstract class SkillBehaviourBase : MonoBehaviour // 모든 스킬 행동 클래스의 부모입니다.
{
    protected SkillJsonData skillData; // 실행할 스킬 데이터를 저장합니다.

    public virtual void Init(SkillJsonData data) // 스킬 데이터를 초기화합니다.
    {
        skillData = data; // 전달받은 스킬 데이터를 저장합니다.
    }

    public abstract void Execute(GameObject caster); // 자식 스킬 클래스가 실제 스킬 실행을 구현해야 합니다.
}
