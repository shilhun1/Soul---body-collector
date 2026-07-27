using UnityEngine;

/// <summary>
/// 플레이어의 현재 상태를 나타냅니다.
/// Body는 육신 조작, Soul은 영혼 조작, Dead는 게임오버/리스타트 흐름에 사용합니다.
/// </summary>
public enum HWJ_SoulRuntimeState
{
    Body,
    BodyToSoul,
    Soul,
    Dead
}

/// <summary>
/// 플레이어의 육신/영혼/사망 상태를 관리하는 컴포넌트입니다.
/// 플레이어 오브젝트에 붙이고 PlayerTypeDataSO.SoulState의 10초 데드라인 타이머를 사용합니다.
/// </summary>
public class HWJ_SoulSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private bool initializeStateFromPlayerData = true;
    [SerializeField] private HWJ_SoulRuntimeState currentState = HWJ_SoulRuntimeState.Body;
    [SerializeField] private HWJ_PlayerExistenceState currentExistenceState = HWJ_PlayerExistenceState.Possessed;
    [SerializeField] private float bodyToSoulTransitionSeconds = 2.5f;
    [SerializeField] private float soulDeadlineTimer;

    private float bodyToSoulTransitionTimer;
    private bool refillSoulHpAfterTransition;

    public HWJ_SoulRuntimeState CurrentState => currentState;
    public HWJ_PlayerExistenceState CurrentExistenceState => currentExistenceState;
    public float SoulDeadlineTimer => soulDeadlineTimer;
    public float BodyToSoulTransitionTimer => bodyToSoulTransitionTimer;
    public bool IsControlLocked => currentState == HWJ_SoulRuntimeState.BodyToSoul
        || currentState == HWJ_SoulRuntimeState.Dead;
    public bool IsSpiritExistence => currentExistenceState == HWJ_PlayerExistenceState.Spirit;
    public bool IsPossessedExistence => currentExistenceState == HWJ_PlayerExistenceState.Possessed;
    public bool IsTransitioningExistence => currentExistenceState == HWJ_PlayerExistenceState.Possessing
        || currentExistenceState == HWJ_PlayerExistenceState.Collapsing
        || currentExistenceState == HWJ_PlayerExistenceState.Transitioning;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        InitializeStartStateFromData();
        SyncExistenceStateFromSoulState(false);

        if (currentState == HWJ_SoulRuntimeState.Soul && soulDeadlineTimer <= 0f && ShouldResetSoulDeadlineOnAwake())
        {
            ResetSoulDeadlineTimer();
        }
    }

    private void Start()
    {
        ApplyRuntimeState();
    }

    /// <summary>
    /// 영혼 상태에서 데드라인 타이머를 감소시키고, 시간이 끝나면 사망 상태로 전환합니다.
    /// 실제 게임오버 UI나 리스타트 시퀀스는 Dead 상태를 감지하는 다른 시스템에서 연결합니다.
    /// </summary>
    private void Update()
    {
        if (currentState == HWJ_SoulRuntimeState.BodyToSoul)
        {
            bodyToSoulTransitionTimer -= Time.deltaTime;

            if (bodyToSoulTransitionTimer <= 0f)
            {
                CompleteBodyToSoulTransition();
            }

            return;
        }

        if (currentState != HWJ_SoulRuntimeState.Soul)
        {
            return;
        }

        if (!TryGetSoulStateData(out HWJ_SoulStateData soulState) || soulDeadlineTimer <= 0f)
        {
            return;
        }

        soulDeadlineTimer -= Time.deltaTime;

        if (soulDeadlineTimer <= 0f && soulState.blocksControlOnDeadline)
        {
            EnterDeadState();
        }
    }

    /// <summary>
    /// 현재 플레이어를 영혼 상태로 전환하고 데드라인 타이머를 초기화합니다.
    /// 빙의체 정신력이 0이 되었거나 HP가 0이 되었을 때 호출합니다.
    /// </summary>
    public void EnterSoulState(
        bool refillSoulHp = false,
        HWJ_PossessedBodyExitReason bodyExitReason = HWJ_PossessedBodyExitReason.MentalDepleted)
    {
        runtimeStatus?.CacheCurrentHpForActiveState();
        possessionSystem?.ClearPossessedBody(false, false, true, bodyExitReason);
        SetSoulState(HWJ_SoulRuntimeState.BodyToSoul);
        bodyToSoulTransitionTimer = bodyToSoulTransitionSeconds;
        soulDeadlineTimer = 0f;
        refillSoulHpAfterTransition = refillSoulHp;
        ApplyRuntimeState();
    }

    /// <summary>
    /// 현재 플레이어를 육신 상태로 전환하고 영혼 데드라인 타이머를 정지합니다.
    /// 빙의 성공 시 PossessionSystem에서 호출합니다.
    /// </summary>
    public void EnterBodyState()
    {
        runtimeStatus?.CacheCurrentHpForActiveState();
        SetSoulState(HWJ_SoulRuntimeState.Body);
        bodyToSoulTransitionTimer = 0f;
        soulDeadlineTimer = 0f;
        ApplyRuntimeState();
    }

    /// <summary>
    /// 현재 플레이어를 사망 상태로 전환합니다.
    /// 영혼 상태에서 제한 시간 안에 빙의하지 못했을 때 사용합니다.
    /// </summary>
    public void EnterDeadState()
    {
        runtimeStatus?.CacheCurrentHpForActiveState();
        SetSoulState(HWJ_SoulRuntimeState.Dead);
        bodyToSoulTransitionTimer = 0f;
        soulDeadlineTimer = 0f;
        ApplyRuntimeState();
    }

    /// <summary>
    /// 저장 데이터에서 읽은 영혼/육신 상태를 복원합니다.
    /// PlayerExistenceState는 별도 저장값을 직접 신뢰하지 않고 SoulRuntimeState에서 다시 계산합니다.
    /// </summary>
    public void RestoreSoulSnapshot(HWJ_RuntimeBodySnapshot snapshot)
    {
        SetSoulState(snapshot.soulState);
        bodyToSoulTransitionTimer = Mathf.Max(0f, snapshot.bodyToSoulTransitionTimer);
        soulDeadlineTimer = Mathf.Max(0f, snapshot.soulDeadlineTimer);
        refillSoulHpAfterTransition = false;
        ApplyRuntimeState();
    }

    private void CompleteBodyToSoulTransition()
    {
        SetSoulState(HWJ_SoulRuntimeState.Soul);
        bodyToSoulTransitionTimer = 0f;
        ResetSoulDeadlineTimer();
        runtimeStatus?.RefreshCurrentHpFromData(refillSoulHpAfterTransition);
        refillSoulHpAfterTransition = false;
        ApplyRuntimeState();
    }

    private void InitializeStartStateFromData()
    {
        if (!initializeStateFromPlayerData || !TryGetSoulStateData(out HWJ_SoulStateData soulState))
        {
            return;
        }

        if (!soulState.startAsSoul || currentState != HWJ_SoulRuntimeState.Body)
        {
            return;
        }

        currentState = HWJ_SoulRuntimeState.Soul;
        bodyToSoulTransitionTimer = 0f;
        soulDeadlineTimer = 0f;

        if (soulState.startSoulDeadlineImmediately)
        {
            ResetSoulDeadlineTimer();
        }
    }

    private bool ShouldResetSoulDeadlineOnAwake()
    {
        return !TryGetSoulStateData(out HWJ_SoulStateData soulState)
            || soulState.startSoulDeadlineImmediately;
    }

    /// <summary>
    /// Resolver에 연결된 PlayerTypeDataSO에서 영혼 상태 데이터를 가져옵니다.
    /// 플레이어 데이터가 아닌 오브젝트에서는 false를 반환합니다.
    /// </summary>
    private bool TryGetSoulStateData(out HWJ_SoulStateData soulState)
    {
        soulState = null;

        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        soulState = playerData.SoulState;
        return soulState != null;
    }

    private void ResetSoulDeadlineTimer()
    {
        if (TryGetSoulStateData(out HWJ_SoulStateData soulState))
        {
            soulDeadlineTimer = soulState.possessionDeadlineSeconds;
        }
    }

    private void ApplyRuntimeState()
    {
        if (runtimeStatus == null)
        {
            return;
        }

        switch (currentState)
        {
            case HWJ_SoulRuntimeState.Body:
                runtimeStatus.SetState(HWJ_RuntimeState.Possessed);
                break;
            case HWJ_SoulRuntimeState.BodyToSoul:
                runtimeStatus.SetState(HWJ_RuntimeState.Hit);
                break;
            case HWJ_SoulRuntimeState.Soul:
                runtimeStatus.SetState(HWJ_RuntimeState.Soul);
                break;
            case HWJ_SoulRuntimeState.Dead:
                runtimeStatus.SetState(HWJ_RuntimeState.Dead);
                break;
        }
    }

    private void SetSoulState(HWJ_SoulRuntimeState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        HWJ_SoulRuntimeState previousState = currentState;
        currentState = nextState;
        HWJ_GameplayEvents.RaiseSoulStateChanged(
            new HWJ_SoulStateChangedEvent(this, previousState, currentState));
        SyncExistenceStateFromSoulState(true);
    }

    private void SyncExistenceStateFromSoulState(bool raiseEvent)
    {
        SetExistenceState(MapSoulStateToExistenceState(currentState), raiseEvent);
    }

    private static HWJ_PlayerExistenceState MapSoulStateToExistenceState(HWJ_SoulRuntimeState soulState)
    {
        switch (soulState)
        {
            case HWJ_SoulRuntimeState.Body:
                return HWJ_PlayerExistenceState.Possessed;
            case HWJ_SoulRuntimeState.BodyToSoul:
                return HWJ_PlayerExistenceState.Collapsing;
            case HWJ_SoulRuntimeState.Soul:
                return HWJ_PlayerExistenceState.Spirit;
            case HWJ_SoulRuntimeState.Dead:
                return HWJ_PlayerExistenceState.Dead;
            default:
                return HWJ_PlayerExistenceState.None;
        }
    }

    private void SetExistenceState(HWJ_PlayerExistenceState nextState, bool raiseEvent)
    {
        if (currentExistenceState == nextState)
        {
            return;
        }

        HWJ_PlayerExistenceState previousState = currentExistenceState;
        currentExistenceState = nextState;

        if (!raiseEvent)
        {
            return;
        }

        HWJ_GameplayEvents.RaisePlayerExistenceStateChanged(
            new HWJ_PlayerExistenceStateChangedEvent(this, previousState, currentExistenceState));
    }
}
