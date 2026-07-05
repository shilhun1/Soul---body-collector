using UnityEngine;

// 플레이어가 현재 어떤 행동 상태인지 구분하기 위한 값입니다.
public enum hys_PlayerState
{
    Idle,
    Move,
    Jump,
    Fall,
    Dash,
    Attack,
    Hit,
    Dead
}

public class hys_Player_State : MonoBehaviour
{
    // 현재 플레이어 상태입니다. 인스펙터에서 확인하기 쉽게 SerializeField로 둡니다.
    [SerializeField] private hys_PlayerState currentState = hys_PlayerState.Idle;

    // 상태가 바뀔 때 Unity Console에 로그를 찍을지 정합니다.
    [SerializeField] private bool useDebugLog = true;

    public hys_PlayerState CurrentState => currentState;

    // 피격/사망 상태에서는 기본 조작을 막습니다.
    public bool CanControl => currentState != hys_PlayerState.Hit && currentState != hys_PlayerState.Dead;

    // 공격 중에는 이동 스크립트가 상태를 덮어쓰지 않도록 이동 불가로 봅니다.
    public bool CanMove => CanControl && currentState != hys_PlayerState.Attack;

    // 대시/공격 중에는 중복 공격이 나가지 않게 막습니다.
    public bool CanAttack => CanControl && currentState != hys_PlayerState.Dash && currentState != hys_PlayerState.Attack;

    // 외부 스크립트가 플레이어 상태를 바꿀 때 사용하는 공통 함수입니다.
    public void SetState(hys_PlayerState nextState)
    {
        if (currentState == hys_PlayerState.Dead)
        {
            return;
        }

        if (currentState == nextState)
        {
            return;
        }

        hys_PlayerState previousState = currentState;
        currentState = nextState;
        LogStateChange(previousState, currentState);
    }

    // 사망 상태는 다른 상태 전환보다 우선되며, 한 번 죽으면 다시 덮어쓰지 않습니다.
    public void SetDead()
    {
        if (currentState == hys_PlayerState.Dead)
        {
            return;
        }

        hys_PlayerState previousState = currentState;
        currentState = hys_PlayerState.Dead;
        LogStateChange(previousState, currentState);
    }

    // 디버그용 상태 변경 로그입니다.
    private void LogStateChange(hys_PlayerState previousState, hys_PlayerState nextState)
    {
        if (!useDebugLog)
        {
            return;
        }

        Debug.Log($"[Player State] {previousState} -> {nextState}", this);
    }
}
