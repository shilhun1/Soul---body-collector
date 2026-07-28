using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 발표자가 실제로 Stage1-1을 플레이했을 때 핵심 시연 흐름을 지나갔는지 기록합니다.
/// 자동 테스트가 아니라 수동 시연 증거용 시스템이므로, StageClear와 함께 입력/전투/빙의/기믹 이벤트를 같이 검사합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_ManualDemoRunRecorderSystem : MonoBehaviour
{
    private const string ReportPath = @"C:\Docs\Generated\HWJ_수동시연_3회검증.md";
    private const string FallbackReportName = "HWJ_수동시연_3회검증.md";
    private const string ConsecutiveSuccessKey = "HWJ.ManualDemo.ConsecutiveSuccessCount";
    private const string TotalSuccessKey = "HWJ.ManualDemo.TotalSuccessCount";
    private const string LastRunSummaryKey = "HWJ.ManualDemo.LastRunSummary";

    [Header("수동 시연 기록")]
    [Tooltip("켜져 있으면 StageClear 발생 시 수동 시연 마일스톤 리포트를 생성합니다.")]
    [SerializeField] private bool enableManualDemoRecording = true;
    [Tooltip("이 시간보다 짧은 클리어는 3~5분 시연 성공으로 인정하지 않습니다.")]
    [SerializeField] private float minimumCountedRunSeconds = 60f;
    [Tooltip("켜져 있으면 핵심 마일스톤을 모두 통과해야 1회 성공으로 인정합니다.")]
    [SerializeField] private bool requireCoreMilestonesForCount = true;
    [Tooltip("Presentation debug 메시지로 클리어된 판은 성공으로 인정하지 않습니다.")]
    [SerializeField] private bool excludePresentationDebugClear = true;
    [Tooltip("F5~F9 발표 복구 키를 사용한 판은 성공으로 인정하지 않습니다.")]
    [SerializeField] private bool excludePresentationDebugKeys = true;
    [Tooltip("F10을 누르면 누적 수동 시연 성공 횟수를 초기화합니다.")]
    [SerializeField] private KeyCode resetRecordKey = KeyCode.F10;

    [Space(8f)]
    [Header("마일스톤 판정 기준")]
    [SerializeField] private float movementDistanceThreshold = 1.25f;
    [SerializeField] private float jumpHeightThreshold = 0.65f;
    [SerializeField] private KeyCode[] presentationDebugKeys =
    {
        KeyCode.F5,
        KeyCode.F6,
        KeyCode.F7,
        KeyCode.F8,
        KeyCode.F9
    };

    [Space(8f)]
    [Header("누적 결과")]
    [SerializeField] private float sceneStartedAt;
    [SerializeField] private int consecutiveSuccessCount;
    [SerializeField] private int totalSuccessCount;
    [SerializeField] private string lastRunSummary;
    [SerializeField] private string lastReportPath;

    [Space(8f)]
    [Header("이번 시연 마일스톤")]
    [SerializeField] private bool moved;
    [SerializeField] private bool jumped;
    [SerializeField] private bool dashed;
    [SerializeField] private bool basicAttackUsed;
    [SerializeField] private bool skillUsed;
    [SerializeField] private bool damageDealt;
    [SerializeField] private bool playerDamaged;
    [SerializeField] private bool enemyDefeated;
    [SerializeField] private bool possessionTargetFound;
    [SerializeField] private bool bodyPossessed;
    [SerializeField] private bool possessionExited;
    [SerializeField] private bool possessionMentalChanged;
    [SerializeField] private bool bowSwitchActivated;
    [SerializeField] private bool doorOpened;
    [SerializeField] private bool spiritStateReached;
    [SerializeField] private bool soulGateOpened;
    [SerializeField] private bool hazardTouched;
    [SerializeField] private bool objectiveCompleted;
    [SerializeField] private bool stageCleared;
    [SerializeField] private bool presentationDebugKeyUsed;
    [SerializeField] private string lastMilestone;

    private HWJ_RootObjectDataResolver playerResolver;
    private HWJ_PlayerInputSystem playerInput;
    private Vector3 playerStartPosition;
    private bool hasPlayerStartPosition;
    private int defeatedEnemyCount;
    private readonly List<string> milestoneLog = new List<string>();

    private void OnEnable()
    {
        sceneStartedAt = Time.realtimeSinceStartup;
        ResetRunMilestones();
        LoadCounters();

        HWJ_GameplayEvents.DamageApplied += OnDamageApplied;
        HWJ_GameplayEvents.AbilityUsed += OnAbilityUsed;
        HWJ_GameplayEvents.EnemyDefeated += OnEnemyDefeated;
        HWJ_GameplayEvents.PlayerExistenceStateChanged += OnPlayerExistenceStateChanged;
        HWJ_GameplayEvents.PossessionTargetChanged += OnPossessionTargetChanged;
        HWJ_GameplayEvents.PossessionChanged += OnPossessionChanged;
        HWJ_GameplayEvents.PossessionMentalChanged += OnPossessionMentalChanged;
        HWJ_GameplayEvents.SpiritOrbSwitchChanged += OnSpiritOrbSwitchChanged;
        HWJ_GameplayEvents.BodyObstacleGateChanged += OnBodyObstacleGateChanged;
        HWJ_GameplayEvents.StageObjectiveChanged += OnStageObjectiveChanged;
        HWJ_GameplayEvents.StageCleared += OnStageCleared;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.DamageApplied -= OnDamageApplied;
        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;
        HWJ_GameplayEvents.EnemyDefeated -= OnEnemyDefeated;
        HWJ_GameplayEvents.PlayerExistenceStateChanged -= OnPlayerExistenceStateChanged;
        HWJ_GameplayEvents.PossessionTargetChanged -= OnPossessionTargetChanged;
        HWJ_GameplayEvents.PossessionChanged -= OnPossessionChanged;
        HWJ_GameplayEvents.PossessionMentalChanged -= OnPossessionMentalChanged;
        HWJ_GameplayEvents.SpiritOrbSwitchChanged -= OnSpiritOrbSwitchChanged;
        HWJ_GameplayEvents.BodyObstacleGateChanged -= OnBodyObstacleGateChanged;
        HWJ_GameplayEvents.StageObjectiveChanged -= OnStageObjectiveChanged;
        HWJ_GameplayEvents.StageCleared -= OnStageCleared;
    }

    private void Update()
    {
        if (!enableManualDemoRecording)
        {
            return;
        }

        ResolvePlayerReferences();
        PollMovementMilestones();
        PollDebugKeys();

        if (Input.GetKeyDown(resetRecordKey))
        {
            ResetCounters("F10 수동 초기화");
        }
    }

    private void ResolvePlayerReferences()
    {
        if (playerResolver == null)
        {
            playerResolver = ResolvePlayerResolver();
        }

        if (playerResolver == null)
        {
            return;
        }

        if (playerInput == null)
        {
            playerInput = playerResolver.GetComponent<HWJ_PlayerInputSystem>();
        }

        if (!hasPlayerStartPosition)
        {
            playerStartPosition = playerResolver.transform.position;
            hasPlayerStartPosition = true;
            RecordMilestone("시작 위치 기록", $"position={playerStartPosition}");
        }
    }

    private void PollMovementMilestones()
    {
        if (playerResolver == null || !hasPlayerStartPosition)
        {
            return;
        }

        Vector3 currentPosition = playerResolver.transform.position;

        if (!moved && Mathf.Abs(currentPosition.x - playerStartPosition.x) >= Mathf.Max(0.1f, movementDistanceThreshold))
        {
            moved = true;
            RecordMilestone("이동", $"x 이동거리={Mathf.Abs(currentPosition.x - playerStartPosition.x):0.00}");
        }

        if (!jumped && currentPosition.y - playerStartPosition.y >= Mathf.Max(0.1f, jumpHeightThreshold))
        {
            jumped = true;
            RecordMilestone("점프", $"y 상승={currentPosition.y - playerStartPosition.y:0.00}");
        }

        if (playerInput == null)
        {
            return;
        }

        if (!moved && Mathf.Abs(playerInput.MoveInput.x) > 0.2f)
        {
            moved = true;
            RecordMilestone("이동 입력", $"move={playerInput.MoveInput}");
        }

        if (!jumped && playerInput.JumpPressedThisFrame)
        {
            jumped = true;
            RecordMilestone("점프 입력", "JumpPressedThisFrame");
        }

        if (!dashed && playerInput.DashPressedThisFrame)
        {
            dashed = true;
            RecordMilestone("대쉬 입력", "DashPressedThisFrame");
        }
    }

    private void PollDebugKeys()
    {
        if (presentationDebugKeys == null)
        {
            return;
        }

        for (int i = 0; i < presentationDebugKeys.Length; i++)
        {
            KeyCode key = presentationDebugKeys[i];

            if (key != KeyCode.None && Input.GetKeyDown(key))
            {
                presentationDebugKeyUsed = true;
                RecordMilestone("발표 복구 키 사용", key.ToString());
            }
        }
    }

    private void OnDamageApplied(HWJ_DamageEvent damageEvent)
    {
        HWJ_RootObjectDataResolver targetResolver = ResolveResolver(damageEvent.TargetStatus);
        HWJ_RootObjectDataResolver sourceResolver = ResolveResolver(damageEvent.Source);

        if (targetResolver != null && targetResolver.ObjectType == HWJ_ObjectType.Enemy)
        {
            damageDealt = true;
            RecordMilestone("적에게 데미지", $"{targetResolver.name}, damage={damageEvent.Damage:0.##}");
        }

        if (targetResolver != null && targetResolver.ObjectType == HWJ_ObjectType.Player)
        {
            playerDamaged = true;
            RecordMilestone("플레이어 피격", $"damage={damageEvent.Damage:0.##}, remain={damageEvent.RemainingHp:0.##}");
        }

        if (sourceResolver != null && sourceResolver.ObjectType == HWJ_ObjectType.Player)
        {
            damageDealt = true;
        }

        if (IsHazardSource(damageEvent.Source))
        {
            hazardTouched = true;
            RecordMilestone("함정/환경 데미지", damageEvent.Source.GetType().Name);
        }
    }

    private void OnAbilityUsed(HWJ_AbilityUsedEvent abilityEvent)
    {
        if (abilityEvent.UserResolver == null || abilityEvent.UserResolver.ObjectType != HWJ_ObjectType.Player)
        {
            return;
        }

        if (abilityEvent.IsBasicAttack)
        {
            basicAttackUsed = true;
            RecordMilestone("기본 공격 사용", abilityEvent.AbilityId);
        }
        else
        {
            skillUsed = true;
            RecordMilestone("스킬 사용", abilityEvent.AbilityId);
        }
    }

    private void OnEnemyDefeated(HWJ_EnemyDefeatedEvent defeatedEvent)
    {
        enemyDefeated = true;
        defeatedEnemyCount++;
        RecordMilestone("몬스터 처치", $"{ResolveName(defeatedEvent.DefeatedResolver)} / total={defeatedEnemyCount}");
    }

    private void OnPlayerExistenceStateChanged(HWJ_PlayerExistenceStateChangedEvent stateEvent)
    {
        if (stateEvent.CurrentState == HWJ_PlayerExistenceState.Spirit)
        {
            spiritStateReached = true;
            RecordMilestone("영혼 상태", "PlayerExistenceState.Spirit");
        }
    }

    private void OnPossessionTargetChanged(HWJ_PossessionTargetChangedEvent targetChangedEvent)
    {
        if (targetChangedEvent.CurrentTarget != null)
        {
            possessionTargetFound = true;
            RecordMilestone("빙의 대상 발견", targetChangedEvent.CurrentTarget.name);
        }
    }

    private void OnPossessionChanged(HWJ_PossessionEvent possessionEvent)
    {
        if (possessionEvent.Possessed)
        {
            bodyPossessed = true;
            RecordMilestone("빙의 성공", ResolveName(possessionEvent.BodyResolver));
        }
        else
        {
            possessionExited = true;
            spiritStateReached = true;
            RecordMilestone("빙의 해제", possessionEvent.Message);
        }
    }

    private void OnPossessionMentalChanged(HWJ_BodyDecayChangedEvent mentalEvent)
    {
        if (Mathf.Abs(mentalEvent.CurrentValue - mentalEvent.PreviousValue) <= 0.001f)
        {
            return;
        }

        possessionMentalChanged = true;
        RecordMilestone("빙의 정신력 변화", $"{mentalEvent.PreviousValue:0.##} -> {mentalEvent.CurrentValue:0.##}");
    }

    private void OnSpiritOrbSwitchChanged(HWJ_SpiritOrbSwitchEvent switchEvent)
    {
        if (!switchEvent.Activated)
        {
            return;
        }

        string switchId = switchEvent.SwitchId ?? string.Empty;
        RecordMilestone("스위치 활성화", switchId);

        if (ContainsIgnoreCase(switchId, "bow") || ContainsIgnoreCase(switchId, "range"))
        {
            bowSwitchActivated = true;
        }
    }

    private void OnBodyObstacleGateChanged(HWJ_BodyObstacleGateEvent obstacleEvent)
    {
        if (!obstacleEvent.IsOpen)
        {
            return;
        }

        string obstacleId = obstacleEvent.ObstacleId ?? string.Empty;
        RecordMilestone("장애물/문 열림", obstacleId);

        if (ContainsIgnoreCase(obstacleId, "door")
            || ContainsIgnoreCase(obstacleId, "bow")
            || ContainsIgnoreCase(obstacleId, "switch"))
        {
            doorOpened = true;
        }

        if (ContainsIgnoreCase(obstacleId, "soul")
            || ContainsIgnoreCase(obstacleId, "spirit"))
        {
            soulGateOpened = true;
        }
    }

    private void OnStageObjectiveChanged(HWJ_StageProgressionEvent progressionEvent)
    {
        if (progressionEvent.ObjectiveComplete)
        {
            objectiveCompleted = true;
            RecordMilestone("스테이지 목표 완료", progressionEvent.Message);
        }
    }

    private void OnStageCleared(HWJ_StageProgressionEvent progressionEvent)
    {
        if (!enableManualDemoRecording)
        {
            return;
        }

        stageCleared = true;
        objectiveCompleted = objectiveCompleted || progressionEvent.ObjectiveComplete;
        RecordMilestone("스테이지 클리어", progressionEvent.Message);

        float elapsedSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - sceneStartedAt);
        bool debugClear = IsPresentationDebugClear(progressionEvent.Message);
        bool debugBlocked = excludePresentationDebugKeys && presentationDebugKeyUsed;
        bool milestonesComplete = AreRequiredMilestonesComplete(out string missingMilestones);
        bool counted = elapsedSeconds >= minimumCountedRunSeconds
            && !debugClear
            && !debugBlocked
            && (!requireCoreMilestonesForCount || milestonesComplete);
        string reason = ResolveRunReason(elapsedSeconds, debugClear, debugBlocked, milestonesComplete, missingMilestones, counted);

        if (counted)
        {
            consecutiveSuccessCount++;
            totalSuccessCount++;
        }
        else
        {
            consecutiveSuccessCount = 0;
        }

        lastRunSummary = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} / counted={counted} / elapsed={elapsedSeconds:F1}s / stage={progressionEvent.StageId} / reason={reason}";
        SaveCounters();
        WriteReport(progressionEvent, elapsedSeconds, counted, reason, missingMilestones);
    }

    private bool AreRequiredMilestonesComplete(out string missingMilestones)
    {
        List<string> missing = new List<string>();
        AddMissing(missing, moved, "이동");
        AddMissing(missing, jumped, "점프");
        AddMissing(missing, dashed, "대쉬");
        AddMissing(missing, basicAttackUsed, "기본 공격");
        AddMissing(missing, damageDealt, "적 데미지");
        AddMissing(missing, enemyDefeated, "몬스터 처치");
        AddMissing(missing, possessionTargetFound, "빙의 대상 발견");
        AddMissing(missing, bodyPossessed, "빙의 성공");
        AddMissing(missing, possessionExited, "빙의 해제");
        AddMissing(missing, possessionMentalChanged, "빙의 정신력 변화");
        AddMissing(missing, bowSwitchActivated, "활 스위치 활성화");
        AddMissing(missing, doorOpened, "문 열림");
        AddMissing(missing, spiritStateReached, "영혼 상태");
        AddMissing(missing, objectiveCompleted, "모든 적 처치 목표 완료");
        AddMissing(missing, stageCleared, "스테이지 클리어");

        missingMilestones = missing.Count == 0 ? "없음" : string.Join(", ", missing);
        return missing.Count == 0;
    }

    private static void AddMissing(List<string> missing, bool passed, string label)
    {
        if (!passed)
        {
            missing.Add(label);
        }
    }

    private bool IsPresentationDebugClear(string message)
    {
        if (!excludePresentationDebugClear || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return ContainsIgnoreCase(message, "Presentation debug")
            || ContainsIgnoreCase(message, "presentation_debug");
    }

    private string ResolveRunReason(
        float elapsedSeconds,
        bool debugClear,
        bool debugBlocked,
        bool milestonesComplete,
        string missingMilestones,
        bool counted)
    {
        if (counted)
        {
            return "수동 시연 성공으로 인정";
        }

        if (debugClear)
        {
            return "발표 복구/디버그 클리어라 3회 성공으로 인정하지 않음";
        }

        if (debugBlocked)
        {
            return "F5~F9 발표 복구 키를 사용해서 정상 수동 성공으로 인정하지 않음";
        }

        if (elapsedSeconds < minimumCountedRunSeconds)
        {
            return $"시연 시간이 너무 짧음. 최소 {minimumCountedRunSeconds:F0}초 필요";
        }

        if (requireCoreMilestonesForCount && !milestonesComplete)
        {
            return $"핵심 마일스톤 누락: {missingMilestones}";
        }

        return "성공 조건 미충족";
    }

    private void LoadCounters()
    {
        consecutiveSuccessCount = PlayerPrefs.GetInt(ConsecutiveSuccessKey, 0);
        totalSuccessCount = PlayerPrefs.GetInt(TotalSuccessKey, 0);
        lastRunSummary = PlayerPrefs.GetString(LastRunSummaryKey, string.Empty);
    }

    private void SaveCounters()
    {
        PlayerPrefs.SetInt(ConsecutiveSuccessKey, consecutiveSuccessCount);
        PlayerPrefs.SetInt(TotalSuccessKey, totalSuccessCount);
        PlayerPrefs.SetString(LastRunSummaryKey, lastRunSummary ?? string.Empty);
        PlayerPrefs.Save();
    }

    private void ResetCounters(string reason)
    {
        consecutiveSuccessCount = 0;
        totalSuccessCount = 0;
        lastRunSummary = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} / reset / {reason}";
        SaveCounters();
        ResetRunMilestones();
        WriteResetReport(reason);
    }

    private void ResetRunMilestones()
    {
        moved = false;
        jumped = false;
        dashed = false;
        basicAttackUsed = false;
        skillUsed = false;
        damageDealt = false;
        playerDamaged = false;
        enemyDefeated = false;
        possessionTargetFound = false;
        bodyPossessed = false;
        possessionExited = false;
        possessionMentalChanged = false;
        bowSwitchActivated = false;
        doorOpened = false;
        spiritStateReached = false;
        soulGateOpened = false;
        hazardTouched = false;
        objectiveCompleted = false;
        stageCleared = false;
        presentationDebugKeyUsed = false;
        defeatedEnemyCount = 0;
        lastMilestone = string.Empty;
        milestoneLog.Clear();
    }

    private void WriteReport(
        HWJ_StageProgressionEvent progressionEvent,
        float elapsedSeconds,
        bool counted,
        string reason,
        string missingMilestones)
    {
        string reportPath = ReportPath;
        string reportText = BuildReportText(progressionEvent, elapsedSeconds, counted, reason, missingMilestones);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, reportText, Encoding.UTF8);
        }
        catch (Exception)
        {
            reportPath = Path.Combine(Application.persistentDataPath, FallbackReportName);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, reportText, Encoding.UTF8);
        }

        lastReportPath = reportPath;
        Debug.Log($"[HWJ Manual Demo Recorder] counted={counted}, consecutive={consecutiveSuccessCount}, report={reportPath}");
    }

    private void WriteResetReport(string reason)
    {
        string reportPath = ReportPath;
        string reportText = BuildResetReportText(reason);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, reportText, Encoding.UTF8);
        }
        catch (Exception)
        {
            reportPath = Path.Combine(Application.persistentDataPath, FallbackReportName);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, reportText, Encoding.UTF8);
        }

        lastReportPath = reportPath;
        Debug.Log($"[HWJ Manual Demo Recorder] reset. report={reportPath}");
    }

    private string BuildReportText(
        HWJ_StageProgressionEvent progressionEvent,
        float elapsedSeconds,
        bool counted,
        string reason,
        string missingMilestones)
    {
        Scene scene = SceneManager.GetActiveScene();
        StringBuilder builder = new StringBuilder(4096);
        builder.AppendLine("# HWJ 수동 시연 3회 검증 기록");
        builder.AppendLine();
        builder.AppendLine($"- 기록 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 씬: {scene.name} / {scene.path}");
        builder.AppendLine($"- StageId: {progressionEvent.StageId}");
        builder.AppendLine($"- StageState: {progressionEvent.CurrentState}");
        builder.AppendLine($"- StageClear Message: {progressionEvent.Message}");
        builder.AppendLine($"- 이번 시연 시간: {elapsedSeconds:F1}초");
        builder.AppendLine($"- 이번 클리어 인정 여부: {(counted ? "인정" : "미인정")}");
        builder.AppendLine($"- 미인정/인정 사유: {reason}");
        builder.AppendLine($"- 누락 마일스톤: {missingMilestones}");
        builder.AppendLine($"- 연속 인정 성공 횟수: {consecutiveSuccessCount} / 3");
        builder.AppendLine($"- 총 인정 성공 횟수: {totalSuccessCount}");
        builder.AppendLine($"- 최근 기록: {lastRunSummary}");
        builder.AppendLine();
        AppendMilestoneTable(builder);
        builder.AppendLine();
        builder.AppendLine("## 마일스톤 로그");

        if (milestoneLog.Count == 0)
        {
            builder.AppendLine("- 없음");
        }
        else
        {
            for (int i = 0; i < milestoneLog.Count; i++)
            {
                builder.AppendLine($"- {milestoneLog[i]}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## 판정");
        builder.AppendLine(consecutiveSuccessCount >= 3
            ? "- 결과: 3회 연속 수동 시연 성공"
            : "- 결과: 3회 연속 수동 시연 미완료");
        builder.AppendLine();
        builder.AppendLine("## 인정 기준");
        builder.AppendLine($"- 최소 인정 시간: {minimumCountedRunSeconds:F0}초");
        builder.AppendLine("- F5~F9 발표 복구 키를 사용한 판은 정상 수동 성공으로 인정하지 않음");
        builder.AppendLine("- F10을 누르면 누적 기록을 초기화함");
        return builder.ToString();
    }

    private string BuildResetReportText(string reason)
    {
        Scene scene = SceneManager.GetActiveScene();
        StringBuilder builder = new StringBuilder(1024);
        builder.AppendLine("# HWJ 수동 시연 3회 검증 기록");
        builder.AppendLine();
        builder.AppendLine($"- 기록 시간: {DateTime.Now:O}");
        builder.AppendLine($"- 씬: {scene.name} / {scene.path}");
        builder.AppendLine($"- 초기화 사유: {reason}");
        builder.AppendLine("- 연속 인정 성공 횟수: 0 / 3");
        builder.AppendLine("- 총 인정 성공 횟수: 0");
        builder.AppendLine();
        builder.AppendLine("## 판정");
        builder.AppendLine("- 결과: 3회 연속 수동 시연 미완료");
        return builder.ToString();
    }

    private void AppendMilestoneTable(StringBuilder builder)
    {
        builder.AppendLine("## 마일스톤 체크");
        builder.AppendLine("|마일스톤|상태|");
        builder.AppendLine("|---|---:|");
        AppendMilestoneRow(builder, "이동", moved);
        AppendMilestoneRow(builder, "점프", jumped);
        AppendMilestoneRow(builder, "대쉬", dashed);
        AppendMilestoneRow(builder, "기본 공격", basicAttackUsed);
        AppendMilestoneRow(builder, "스킬 사용", skillUsed);
        AppendMilestoneRow(builder, "적 데미지", damageDealt);
        AppendMilestoneRow(builder, "플레이어 피격", playerDamaged);
        AppendMilestoneRow(builder, "몬스터 처치", enemyDefeated);
        AppendMilestoneRow(builder, "빙의 대상 발견", possessionTargetFound);
        AppendMilestoneRow(builder, "빙의 성공", bodyPossessed);
        AppendMilestoneRow(builder, "빙의 해제", possessionExited);
        AppendMilestoneRow(builder, "빙의 정신력 변화", possessionMentalChanged);
        AppendMilestoneRow(builder, "활 스위치 활성화", bowSwitchActivated);
        AppendMilestoneRow(builder, "문 열림", doorOpened);
        AppendMilestoneRow(builder, "영혼 상태", spiritStateReached);
        AppendMilestoneRow(builder, "영혼 통과/문", soulGateOpened);
        AppendMilestoneRow(builder, "함정/환경 피격", hazardTouched);
        AppendMilestoneRow(builder, "모든 적 처치 목표 완료", objectiveCompleted);
        AppendMilestoneRow(builder, "스테이지 클리어", stageCleared);
        AppendMilestoneRow(builder, "발표 복구 키 미사용", !presentationDebugKeyUsed);
    }

    private static void AppendMilestoneRow(StringBuilder builder, string label, bool passed)
    {
        builder.AppendLine($"|{label}|{(passed ? "통과" : "미통과")}|");
    }

    private void RecordMilestone(string label, string detail)
    {
        string log = $"{Time.realtimeSinceStartup - sceneStartedAt:0.0}s / {label} / {detail}";
        lastMilestone = log;
        milestoneLog.Add(log);

        if (milestoneLog.Count > 80)
        {
            milestoneLog.RemoveAt(0);
        }
    }

    private static bool IsHazardSource(Component source)
    {
        if (source == null)
        {
            return false;
        }

        string sourceName = source.name ?? string.Empty;
        string sourceType = source.GetType().Name;
        return ContainsIgnoreCase(sourceName, "Trap")
            || ContainsIgnoreCase(sourceName, "Spike")
            || ContainsIgnoreCase(sourceName, "Sandstorm")
            || ContainsIgnoreCase(sourceName, "Hazard")
            || ContainsIgnoreCase(sourceType, "Trap")
            || ContainsIgnoreCase(sourceType, "Hazard");
    }

    private static HWJ_RootObjectDataResolver ResolvePlayerResolver()
    {
        if (HWJ_GameAccess.PlayerResolver != null)
        {
            return HWJ_GameAccess.PlayerResolver;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i];
            }
        }

        return null;
    }

    private static HWJ_RootObjectDataResolver ResolveResolver(Component component)
    {
        return component != null ? component.GetComponentInParent<HWJ_RootObjectDataResolver>() : null;
    }

    private static string ResolveName(UnityEngine.Object targetObject)
    {
        return targetObject != null ? targetObject.name : "None";
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source)
            && !string.IsNullOrEmpty(value)
            && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
