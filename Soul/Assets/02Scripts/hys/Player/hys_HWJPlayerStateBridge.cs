using UnityEngine;

// HWJ의 실제 체력/영혼 상태를 hys 애니메이션 상태로 전달합니다.
[DisallowMultipleComponent]
public class hys_HWJPlayerStateBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private hys_Player_State playerState;

    [Header("Sync Options")]
    [SerializeField] private bool syncHitAnimation = true;
    [SerializeField] private bool syncDeathAnimation = true;
    [SerializeField] private bool useDebugLog;

    private float previousObservedHp;
    private HWJ_SoulRuntimeState previousSoulState;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        previousObservedHp = runtimeStatus != null ? runtimeStatus.CurrentHp : 0f;
        previousSoulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
    }

    private void Update()
    {
        CacheReferences();
        ObserveRuntimeState();
    }

    private void CacheReferences()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (playerState == null)
        {
            playerState = GetComponent<hys_Player_State>();
        }
    }

#if HYS_USE_HWJ_GAMEPLAY_EVENTS
    private void HandleDamageApplied(HWJ_DamageEvent damageEvent)
    {
        if (!syncHitAnimation || playerState == null || damageEvent.TargetStatus != runtimeStatus)
        {
            return;
        }

        // 치명타는 BodyToSoul 이벤트에서 Dead로 처리하므로 여기서는 일반 피격만 넘깁니다.
        if (damageEvent.RemainingHp <= 0f || soulSystem == null ||
            soulSystem.CurrentState != HWJ_SoulRuntimeState.Body ||
            playerState.CurrentState == hys_PlayerState.Dead)
        {
            return;
        }

        playerState.SetState(hys_PlayerState.Hit);
        Log("HWJ 피해 이벤트를 hys Hit 상태로 전달했습니다.");
    }

    private void HandleSoulStateChanged(HWJ_SoulStateChangedEvent stateEvent)
    {
        if (!syncDeathAnimation || playerState == null || runtimeStatus == null ||
            stateEvent.SoulSystem != soulSystem)
        {
            return;
        }

        bool startedBodyToSoul = stateEvent.PreviousState == HWJ_SoulRuntimeState.Body &&
            stateEvent.CurrentState == HWJ_SoulRuntimeState.BodyToSoul;

        if (!startedBodyToSoul || runtimeStatus.CurrentHp > 0f)
        {
            return;
        }

        // 실제 HWJ 체력이 0이 된 전환만 플레이어 사망 애니메이션으로 연결합니다.
        playerState.SetDead();
        Log("HWJ HP 0 전환을 hys Dead 상태로 전달했습니다.");
    }

#endif

    private void ObserveRuntimeState()
    {
        if (runtimeStatus == null || soulSystem == null) return;

        float currentHp = runtimeStatus.CurrentHp;
        HWJ_SoulRuntimeState currentSoulState = soulSystem.CurrentState;

        // HWJ 이벤트 타입이 없는 이전 브랜치에서도 동작하도록 HP와 영혼 상태 변화를 직접 감시합니다.
        bool receivedNonFatalDamage = syncHitAnimation && playerState != null
            && currentHp < previousObservedHp && currentHp > 0f
            && currentSoulState == HWJ_SoulRuntimeState.Body
            && playerState.CurrentState != hys_PlayerState.Dead;
        if (receivedNonFatalDamage)
        {
            playerState.SetState(hys_PlayerState.Hit);
            Log("HWJ 피해 변화를 hys Hit 상태로 전달했습니다.");
        }

        bool startedBodyToSoul = previousSoulState == HWJ_SoulRuntimeState.Body
            && currentSoulState == HWJ_SoulRuntimeState.BodyToSoul;
        if (syncDeathAnimation && playerState != null && startedBodyToSoul && currentHp <= 0f)
        {
            playerState.SetDead();
            Log("HWJ HP 0 전환을 hys Dead 상태로 전달했습니다.");
        }

        previousObservedHp = currentHp;
        previousSoulState = currentSoulState;
    }

    private void Log(string message)
    {
        if (useDebugLog)
        {
            Debug.Log("[hys HWJ State Bridge] " + message, this);
        }
    }
}
