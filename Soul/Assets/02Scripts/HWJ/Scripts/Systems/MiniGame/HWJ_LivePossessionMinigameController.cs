using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 생체 빙의 버튼 연타 미니게임입니다.
///
/// 시작 게이지: 기본 50%
/// 성공: 게이지 100%
/// 실패: 게이지 0% 또는 제한 시간 초과
/// 입력: Space 연타
///
/// 성공 시:
/// HWJ_PossessionSystem.CompleteLivePossessionFromMinigame 호출
///
/// 실패 시:
/// HWJ_PossessionSystem.ApplyLivePossessionMinigameFailure 호출
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Systems/Live Possession Minigame Controller")]
public sealed partial class HWJ_LivePossessionMinigameController : MonoBehaviour
{
    [Header("핵심 참조")]
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

    [Space(8f)]
    [Header("UI")]
    [Tooltip("미니게임 UI 전체를 감싸는 오브젝트입니다.")]
    [SerializeField] private GameObject minigameRoot;
    [SerializeField] private Slider mentalGaugeSlider;
    [SerializeField] private Text timerText;
    [SerializeField] private Text instructionText;
    [SerializeField] private Text resultText;

    [Space(8f)]
    [Header("Runtime UI Fallback")]
    [Tooltip("Creates a simple runtime gauge when scene or prefab UI references are missing.")]
    [SerializeField] private bool createFallbackUiWhenMissing = true;

    [Space(8f)]
    [Header("미니게임 규칙")]
    [SerializeField, Range(0f, 1f)] private float baseStartGauge = 0.5f;
    [SerializeField, Min(0.1f)] private float limitSeconds = 5f;

    [Tooltip("초당 자동으로 감소하는 게이지입니다.")]
    [SerializeField, Min(0f)] private float passiveGaugeLossPerSecond = 0.12f;

    [Tooltip("Space를 한 번 누를 때 증가하는 게이지입니다.")]
    [SerializeField, Min(0.001f)] private float gaugeGainPerPress = 0.08f;

    [Space(8f)]
    [Header("빙의 숙련도")]
    [SerializeField, Min(0)] private int proficiencyLevel;

    [Tooltip("숙련도 1당 시작 게이지에 추가되는 값입니다. 0.05는 5%입니다.")]
    [SerializeField, Range(0f, 1f)] private float startGaugeBonusPerLevel = 0.05f;

    [Tooltip("성공할 때 숙련도를 1 증가시킵니다.")]
    [SerializeField] private bool increaseProficiencyOnSuccess = true;

    [Space(8f)]
    [Header("정지 방식")]
    [Tooltip("켜면 전체 시간을 멈춥니다. 기본값은 끄고 플레이어와 대상 몬스터만 정지합니다.")]
    [SerializeField] private bool pauseEntireWorldDuringMinigame;

    [Space(8f)]
    [Header("디버그")]
    [SerializeField] private bool isRunning;
    [SerializeField] private float currentGauge;
    [SerializeField] private float elapsedSeconds;
    [SerializeField] private HWJ_RootObjectDataResolver activeTarget;
    [SerializeField] private string runtimeMessage;

    [Space(8f)]
    [Header("Lifecycle Log")]
    [Tooltip("Logs minigame start, progress, input, and the final result to the Unity Console.")]
    [SerializeField] private bool logLifecycle = true;
    [Tooltip("Interval for RUNNING heartbeat logs. Input presses are logged immediately.")]
    [SerializeField, Min(0.1f)] private float progressLogIntervalSeconds = 1f;

    private float previousTimeScale = 1f;
    private Behaviour[] pausedBehaviours;
    private bool[] pausedBehaviourStates;
    private Rigidbody2D pausedPlayerBody;
    private Rigidbody2D pausedTargetBody;
    private RigidbodyConstraints2D pausedPlayerConstraints;
    private RigidbodyConstraints2D pausedTargetConstraints;
    private float nextProgressLogElapsedSeconds;
    private int mashPressCount;

    public bool IsRunning => isRunning;
    public float CurrentGauge => currentGauge;
    public float RemainingSeconds =>
        Mathf.Max(0f, limitSeconds - elapsedSeconds);
    public int ProficiencyLevel => proficiencyLevel;
    public string RuntimeMessage => runtimeMessage;
    public bool HasConfiguredUi => minigameRoot != null
        && mentalGaugeSlider != null
        && timerText != null
        && instructionText != null
        && resultText != null;
    public bool IsUiVisible => minigameRoot != null && minigameRoot.activeSelf;

    private void Reset()
    {
        possessionSystem = GetComponent<HWJ_PossessionSystem>();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureRuntimeUi();
        ConfigureUi();
        SetUiVisible(false);
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        elapsedSeconds += deltaTime;

        currentGauge -=
            Mathf.Max(0f, passiveGaugeLossPerSecond) * deltaTime;

        if (WasMashButtonPressed())
        {
            currentGauge += Mathf.Max(0.001f, gaugeGainPerPress);
            mashPressCount++;
            LogLifecycle(
                "INPUT",
                $"press={mashPressCount}, gauge={Mathf.Clamp01(currentGauge) * 100f:0.0}%, " +
                $"remaining={RemainingSeconds:0.00}s");
        }

        currentGauge = Mathf.Clamp01(currentGauge);
        RefreshUi();
        LogProgressIfDue();

        if (currentGauge >= 1f)
        {
            FinishSuccess();
            return;
        }

        if (currentGauge <= 0f || elapsedSeconds >= limitSeconds)
        {
            FinishFailure();
        }
    }

    /// <summary>
    /// R 입력 처리기가 생체 빙의 가능 검사를 마친 뒤 호출합니다.
    /// </summary>
    public bool BeginMinigame(
        HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (isRunning)
        {
            runtimeMessage = "이미 빙의 미니게임이 진행 중입니다.";
            return false;
        }

        if (possessionSystem == null || targetDataResolver == null)
        {
            runtimeMessage =
                "미니게임 시작 실패: PossessionSystem 또는 대상이 없습니다.";
            return false;
        }

        // 호출 직전에 대상 상태를 다시 검사합니다.
        if (!possessionSystem.CanStartLivePossessionMinigame(
                targetDataResolver))
        {
            runtimeMessage = string.IsNullOrWhiteSpace(
                possessionSystem.LastPossessionResult)
                ? "생체 빙의 미니게임을 시작할 수 없습니다."
                : possessionSystem.LastPossessionResult;
            return false;
        }

        activeTarget = targetDataResolver;
        elapsedSeconds = 0f;
        mashPressCount = 0;
        nextProgressLogElapsedSeconds = Mathf.Max(0.1f, progressLogIntervalSeconds);

        float proficiencyBonus =
            Mathf.Max(0, proficiencyLevel)
            * Mathf.Clamp01(startGaugeBonusPerLevel);

        currentGauge = Mathf.Clamp01(
            baseStartGauge + proficiencyBonus);

        isRunning = true;
        runtimeMessage = "Space를 연타하여 정신력 게이지를 100%로 만드십시오.";

        SetUiVisible(true);
        RefreshUi();
        PauseActorsForMinigame();

        LogLifecycle(
            "START",
            $"target={GetTargetName(activeTarget)}, gauge={currentGauge * 100f:0.0}%, " +
            $"limit={limitSeconds:0.00}s, passiveLoss={passiveGaugeLossPerSecond:0.###}/s, " +
            $"gainPerPress={gaugeGainPerPress:0.###}, pauseWorld={pauseEntireWorldDuringMinigame}");

        return true;
    }

    public void SetProficiencyLevel(int level)
    {
        proficiencyLevel = Mathf.Max(0, level);
    }

    /// <summary>
    /// 외부 UI 버튼으로 연타 입력을 구현할 때 사용할 수 있습니다.
    /// </summary>
    public void AddGaugeFromUiButton()
    {
        if (!isRunning)
        {
            return;
        }

        currentGauge = Mathf.Clamp01(
            currentGauge + Mathf.Max(0.001f, gaugeGainPerPress));
        mashPressCount++;
        LogLifecycle(
            "INPUT",
            $"source=UI, press={mashPressCount}, gauge={currentGauge * 100f:0.0}%, " +
            $"remaining={RemainingSeconds:0.00}s");

        RefreshUi();

        // UI 버튼 입력은 Update 바깥에서 들어오므로 100% 도달 즉시 성공 처리해야
        // 다음 프레임의 자연 감소 때문에 성공선을 다시 밑도는 문제를 막을 수 있습니다.
        if (currentGauge >= 1f)
        {
            FinishSuccess();
        }
    }

    private void FinishSuccess()
    {
        HWJ_RootObjectDataResolver completedTarget = activeTarget;
        HWJ_LivePossessionMentalState completedMentalState = completedTarget != null
            ? completedTarget.GetComponent<HWJ_LivePossessionMentalState>()
            : null;
        float mentalBeforePossession = completedMentalState != null
            ? completedMentalState.CurrentMentalValue
            : -1f;
        float finishedGauge = currentGauge;
        float finishedElapsedSeconds = elapsedSeconds;
        int finishedMashPressCount = mashPressCount;

        EndSession();

        bool possessed = possessionSystem != null
            && possessionSystem.CompleteLivePossessionFromMinigame(
                completedTarget);

        if (!possessed)
        {
            runtimeMessage = possessionSystem != null
                && !string.IsNullOrWhiteSpace(
                    possessionSystem.LastPossessionResult)
                    ? possessionSystem.LastPossessionResult
                    : "미니게임은 성공했지만 대상 상태가 변경되어 빙의하지 못했습니다.";

            LogLifecycleWarning(
                "END",
                $"result=POSSESSION_REJECTED, target={GetTargetName(completedTarget)}, " +
                $"gauge={finishedGauge * 100f:0.0}%, elapsed={finishedElapsedSeconds:0.00}s, " +
                $"presses={finishedMashPressCount}, message={runtimeMessage}");
            return;
        }

        if (increaseProficiencyOnSuccess)
        {
            proficiencyLevel++;
        }

        runtimeMessage =
            $"생체 빙의 성공. 현재 숙련도: {proficiencyLevel}";

        if (resultText != null)
        {
            resultText.text = runtimeMessage;
        }

        LogLifecycle(
            "END",
            $"result=SUCCESS, target={GetTargetName(completedTarget)}, " +
            $"gauge={finishedGauge * 100f:0.0}%, elapsed={finishedElapsedSeconds:0.00}s, " +
            $"presses={finishedMashPressCount}, proficiency={proficiencyLevel}, " +
            $"possessed={possessionSystem != null && possessionSystem.HasActivePossessedBody}, " +
            $"targetMental={FormatMentalChange(mentalBeforePossession, completedMentalState)}");
    }

    private void FinishFailure()
    {
        HWJ_RootObjectDataResolver failedTarget = activeTarget;
        float finishedGauge = currentGauge;
        float finishedElapsedSeconds = elapsedSeconds;
        int finishedMashPressCount = mashPressCount;
        string failureReason = currentGauge <= 0f ? "GAUGE_EMPTY" : "TIME_LIMIT";

        EndSession();

        possessionSystem?.ApplyLivePossessionMinigameFailure(
            failedTarget);

        runtimeMessage = possessionSystem != null
            && !string.IsNullOrWhiteSpace(
                possessionSystem.LastPossessionResult)
                ? possessionSystem.LastPossessionResult
                : "생체 빙의 미니게임에 실패했습니다.";

        if (resultText != null)
        {
            resultText.text = runtimeMessage;
        }

        LogLifecycleWarning(
            "END",
            $"result=FAILURE, reason={failureReason}, target={GetTargetName(failedTarget)}, " +
            $"gauge={finishedGauge * 100f:0.0}%, elapsed={finishedElapsedSeconds:0.00}s, " +
            $"presses={finishedMashPressCount}, message={runtimeMessage}");
    }

    private void EndSession()
    {
        isRunning = false;
        activeTarget = null;
        RestorePausedActors();
        SetUiVisible(false);
    }

    private void PauseActorsForMinigame()
    {
        if (pauseEntireWorldDuringMinigame)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            return;
        }

        pausedBehaviours = new Behaviour[]
        {
            GetComponent<HWJ_PlayerInputSystem>(),
            GetComponent<HWJ_PlayerMovementSystem>(),
            GetComponent<HWJ_PlayerAttackSystem>(),
            activeTarget != null ? activeTarget.GetComponent<HWJ_EnemyNavigationSystem>() : null,
            activeTarget != null ? activeTarget.GetComponent<HWJ_MonsterAISystem>() : null,
            activeTarget != null ? activeTarget.GetComponent<HWJ_EnemyAttackSystem>() : null
        };
        pausedBehaviourStates = new bool[pausedBehaviours.Length];

        for (int i = 0; i < pausedBehaviours.Length; i++)
        {
            Behaviour behaviour = pausedBehaviours[i];
            pausedBehaviourStates[i] = behaviour != null && behaviour.enabled;

            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }

        pausedPlayerBody = GetComponent<Rigidbody2D>();
        pausedTargetBody = activeTarget != null
            ? activeTarget.GetComponent<Rigidbody2D>()
            : null;
        FreezeBody(pausedPlayerBody, out pausedPlayerConstraints);
        FreezeBody(pausedTargetBody, out pausedTargetConstraints);
    }

    private void RestorePausedActors()
    {
        if (pauseEntireWorldDuringMinigame)
        {
            Time.timeScale = previousTimeScale;
            return;
        }

        if (pausedBehaviours != null && pausedBehaviourStates != null)
        {
            int count = Mathf.Min(pausedBehaviours.Length, pausedBehaviourStates.Length);

            for (int i = 0; i < count; i++)
            {
                if (pausedBehaviours[i] != null)
                {
                    pausedBehaviours[i].enabled = pausedBehaviourStates[i];
                }
            }
        }

        RestoreBody(pausedPlayerBody, pausedPlayerConstraints);
        RestoreBody(pausedTargetBody, pausedTargetConstraints);
        pausedBehaviours = null;
        pausedBehaviourStates = null;
        pausedPlayerBody = null;
        pausedTargetBody = null;
    }

    private static void FreezeBody(
        Rigidbody2D body,
        out RigidbodyConstraints2D previousConstraints)
    {
        previousConstraints = body != null
            ? body.constraints
            : RigidbodyConstraints2D.None;

        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private static void RestoreBody(
        Rigidbody2D body,
        RigidbodyConstraints2D previousConstraints)
    {
        if (body == null)
        {
            return;
        }

        body.constraints = previousConstraints;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void ConfigureUi()
    {
        if (mentalGaugeSlider != null)
        {
            mentalGaugeSlider.minValue = 0f;
            mentalGaugeSlider.maxValue = 1f;
            mentalGaugeSlider.wholeNumbers = false;
        }

        if (instructionText != null)
        {
            instructionText.text =
                "SPACE 연타: 정신력 게이지를 100%까지 올리세요.";
        }
    }

    private void RefreshUi()
    {
        if (mentalGaugeSlider != null)
        {
            mentalGaugeSlider.value = currentGauge;
        }

        if (timerText != null)
        {
            timerText.text =
                $"남은 시간: {RemainingSeconds:0.0}";
        }

        if (resultText != null)
        {
            resultText.text =
                $"게이지: {currentGauge * 100f:0}%";
        }
    }

    private void SetUiVisible(bool visible)
    {
        if (minigameRoot != null)
        {
            minigameRoot.SetActive(visible);
        }
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    private void LogProgressIfDue()
    {
        if (!logLifecycle || elapsedSeconds < nextProgressLogElapsedSeconds)
        {
            return;
        }

        LogLifecycle(
            "RUNNING",
            $"target={GetTargetName(activeTarget)}, gauge={currentGauge * 100f:0.0}%, " +
            $"remaining={RemainingSeconds:0.00}s, presses={mashPressCount}");

        float interval = Mathf.Max(0.1f, progressLogIntervalSeconds);

        do
        {
            nextProgressLogElapsedSeconds += interval;
        }
        while (elapsedSeconds >= nextProgressLogElapsedSeconds);
    }

    private void LogLifecycle(string stage, string details)
    {
        if (logLifecycle)
        {
            Debug.Log($"[HWJ][PossessionMinigame][{stage}] {details}", this);
        }
    }

    private void LogLifecycleWarning(string stage, string details)
    {
        if (logLifecycle)
        {
            Debug.LogWarning($"[HWJ][PossessionMinigame][{stage}] {details}", this);
        }
    }

    private static string GetTargetName(HWJ_RootObjectDataResolver target)
    {
        return target != null ? target.name : "<none>";
    }

    private static string FormatMentalChange(
        float before,
        HWJ_LivePossessionMentalState mentalState)
    {
        return mentalState != null && before >= 0f
            ? $"{before:0.##}->{mentalState.CurrentMentalValue:0.##}/{mentalState.MaxMentalValue:0.##}"
            : "unavailable";
    }

    private static bool WasMashButtonPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
            && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void OnDisable()
    {
        if (!isRunning)
        {
            return;
        }

        HWJ_RootObjectDataResolver cancelledTarget = activeTarget;
        float cancelledGauge = currentGauge;
        float cancelledElapsedSeconds = elapsedSeconds;
        int cancelledMashPressCount = mashPressCount;

        isRunning = false;
        activeTarget = null;
        RestorePausedActors();
        LogLifecycleWarning(
            "END",
            $"result=CANCELLED, target={GetTargetName(cancelledTarget)}, " +
            $"gauge={cancelledGauge * 100f:0.0}%, elapsed={cancelledElapsedSeconds:0.00}s, " +
            $"presses={cancelledMashPressCount}, reason=controller_disabled");
    }
}
