using UnityEngine;

/// <summary>
/// 적 AI 행동 결과 이벤트를 씬에서 확인하기 위한 선택형 디버그 컴포넌트입니다.
/// 실제 AI 상태를 바꾸지 않고 마지막 행동 결과와 수신 횟수만 기록합니다.
/// </summary>
public class HWJ_EnemyAIActionDebugLogger : MonoBehaviour
{
    [SerializeField] private bool logToConsole;
    [SerializeField] private bool onlyFailures = true;
    [SerializeField] private int receivedEventCount;
    [SerializeField] private string lastMonsterName;
    [SerializeField] private HWJ_EnemyAIActionType lastActionType;
    [SerializeField] private HWJ_EnemyAIActionFailureCode lastFailureCode;
    [SerializeField] private HWJ_MonsterAIState lastState;
    [SerializeField] private HWJ_MonsterAIState lastNextState;
    [SerializeField] private string lastMessage;

    public int ReceivedEventCount => receivedEventCount;
    public string LastMonsterName => lastMonsterName;
    public HWJ_EnemyAIActionType LastActionType => lastActionType;
    public HWJ_EnemyAIActionFailureCode LastFailureCode => lastFailureCode;
    public HWJ_MonsterAIState LastState => lastState;
    public HWJ_MonsterAIState LastNextState => lastNextState;
    public string LastMessage => lastMessage;

    private void OnEnable()
    {
        HWJ_GameplayEvents.EnemyAIActionResolved += HandleEnemyAIActionResolved;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.EnemyAIActionResolved -= HandleEnemyAIActionResolved;
    }

    public void Clear()
    {
        receivedEventCount = 0;
        lastMonsterName = null;
        lastActionType = HWJ_EnemyAIActionType.None;
        lastFailureCode = HWJ_EnemyAIActionFailureCode.None;
        lastState = HWJ_MonsterAIState.Idle;
        lastNextState = HWJ_MonsterAIState.Idle;
        lastMessage = null;
    }

    private void HandleEnemyAIActionResolved(HWJ_EnemyAIActionEvent actionEvent)
    {
        HWJ_EnemyAIActionResult actionResult = actionEvent.ActionResult;

        if (onlyFailures && actionResult.Succeeded)
        {
            return;
        }

        receivedEventCount++;
        lastMonsterName = actionEvent.MonsterAI != null
            ? actionEvent.MonsterAI.gameObject.name
            : "Missing MonsterAI";
        lastActionType = actionResult.ActionType;
        lastFailureCode = actionResult.FailureCode;
        lastState = actionResult.State;
        lastNextState = actionResult.NextState;
        lastMessage = actionResult.Message;

        if (logToConsole)
        {
            Debug.Log(
                $"Enemy AI action resolved. Monster:'{lastMonsterName}' Action:{lastActionType} Success:{actionResult.Succeeded} Failure:{lastFailureCode} State:{lastState}->{lastNextState} Message:{lastMessage}",
                this);
        }
    }
}
