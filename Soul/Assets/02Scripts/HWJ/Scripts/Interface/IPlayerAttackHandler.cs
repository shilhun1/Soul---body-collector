using UnityEngine;
public interface IPlayerAttackHandler
{
    // 공격 버튼을 처음 눌렀을 때 호출
    bool OnAttackPressed();

    // 공격 버튼을 뗐을 때 호출
    void OnAttackReleased();

    // 피격, 사망, 빙의 전환 등으로 공격을 취소할 때 호출
    void CancelAttack();
}
 