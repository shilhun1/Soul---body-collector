using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// 애니메이션 테스트용으로 숫자키를 눌러 플레이어 상태를 강제로 바꾸는 스크립트입니다.
public class hys_PlayerStateKeyboardTester : MonoBehaviour
{
    [Header("References")]
    // 비워두면 같은 오브젝트에서 hys_Player_State를 자동으로 찾습니다.
    [SerializeField] private hys_Player_State playerState;

    [Header("Options")]
    // 테스트가 끝나면 체크를 꺼서 실수로 상태가 바뀌지 않게 할 수 있습니다.
    [SerializeField] private bool enableKeyboardTest = true;
    // Dead를 제외한 테스트 상태는 지정 시간 뒤 Idle로 되돌립니다.
    [SerializeField] private bool autoReturnToIdle = true;
    [SerializeField] private float autoReturnSeconds = 0.5f;

    private float returnToIdleTime;
    private bool waitingReturnToIdle;

    private void Awake()
    {
        if (playerState == null)
        {
            playerState = GetComponent<hys_Player_State>();
        }
    }

    private void Update()
    {
        if (!enableKeyboardTest || playerState == null || Keyboard.current == null)
        {
            return;
        }

        TickAutoReturn();

        if (WasPressed(Keyboard.current.digit0Key, Keyboard.current.numpad0Key))
        {
            SetPlayerState(hys_PlayerState.Idle);
        }
        else if (WasPressed(Keyboard.current.digit1Key, Keyboard.current.numpad1Key))
        {
            SetPlayerState(hys_PlayerState.Move);
        }
        else if (WasPressed(Keyboard.current.digit2Key, Keyboard.current.numpad2Key))
        {
            SetPlayerState(hys_PlayerState.Jump);
        }
        else if (WasPressed(Keyboard.current.digit3Key, Keyboard.current.numpad3Key))
        {
            SetPlayerState(hys_PlayerState.Fall);
        }
        else if (WasPressed(Keyboard.current.digit4Key, Keyboard.current.numpad4Key))
        {
            SetPlayerState(hys_PlayerState.Dash);
        }
        else if (WasPressed(Keyboard.current.digit5Key, Keyboard.current.numpad5Key))
        {
            SetPlayerState(hys_PlayerState.Attack);
        }
        else if (WasPressed(Keyboard.current.digit6Key, Keyboard.current.numpad6Key))
        {
            SetPlayerState(hys_PlayerState.Hit);
        }
        else if (WasPressed(Keyboard.current.digit7Key, Keyboard.current.numpad7Key))
        {
            SetPlayerState(hys_PlayerState.Dead);
        }
    }

    private bool WasPressed(KeyControl topNumberKey, KeyControl numpadKey)
    {
        return (topNumberKey != null && topNumberKey.wasPressedThisFrame) ||
            (numpadKey != null && numpadKey.wasPressedThisFrame);
    }

    private void SetPlayerState(hys_PlayerState nextState)
    {
        // Dead는 전용 함수로 보내고, Dead에서 다른 상태로 돌아갈 때는 테스트용 초기화를 사용합니다.
        if (nextState == hys_PlayerState.Dead)
        {
            waitingReturnToIdle = false;
            playerState.SetDead();
            return;
        }

        if (playerState.CurrentState == hys_PlayerState.Dead)
        {
            playerState.ResetFromDead(nextState);
        }
        else
        {
            playerState.SetState(nextState);
        }

        StartAutoReturnTimer(nextState);
    }

    private void StartAutoReturnTimer(hys_PlayerState state)
    {
        // Idle과 Dead는 자동 복귀 대상이 아닙니다.
        if (!autoReturnToIdle || state == hys_PlayerState.Idle || state == hys_PlayerState.Dead)
        {
            waitingReturnToIdle = false;
            return;
        }

        waitingReturnToIdle = true;
        returnToIdleTime = Time.time + autoReturnSeconds;
    }

    private void TickAutoReturn()
    {
        if (!waitingReturnToIdle || Time.time < returnToIdleTime)
        {
            return;
        }

        waitingReturnToIdle = false;

        if (playerState != null && playerState.CurrentState != hys_PlayerState.Dead)
        {
            playerState.SetState(hys_PlayerState.Idle);
        }
    }
}
