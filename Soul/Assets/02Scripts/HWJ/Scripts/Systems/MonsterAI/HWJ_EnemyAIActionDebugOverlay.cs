using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HWJ_EnemyAIActionDebugEntry
{
    [SerializeField] private string monsterName;
    [SerializeField] private bool succeeded;
    [SerializeField] private HWJ_EnemyAIActionType actionType;
    [SerializeField] private HWJ_EnemyAIActionFailureCode failureCode;
    [SerializeField] private HWJ_MonsterAIState state;
    [SerializeField] private HWJ_MonsterAIState nextState;
    [SerializeField] private float targetDistance;
    [SerializeField] private string message;

    public string MonsterName => monsterName;
    public bool Succeeded => succeeded;
    public HWJ_EnemyAIActionType ActionType => actionType;
    public HWJ_EnemyAIActionFailureCode FailureCode => failureCode;
    public HWJ_MonsterAIState State => state;
    public HWJ_MonsterAIState NextState => nextState;
    public float TargetDistance => targetDistance;
    public string Message => message;

    public HWJ_EnemyAIActionDebugEntry(
        string monsterName,
        HWJ_EnemyAIActionResult actionResult)
    {
        this.monsterName = monsterName;
        succeeded = actionResult.Succeeded;
        actionType = actionResult.ActionType;
        failureCode = actionResult.FailureCode;
        state = actionResult.State;
        nextState = actionResult.NextState;
        targetDistance = actionResult.TargetDistance;
        message = actionResult.Message;
    }
}

/// <summary>
/// 적 AI 행동 결과 이벤트를 개발 중 화면에서 확인하는 IMGUI 오버레이입니다.
/// 이 컴포넌트는 이벤트 표시만 담당하고 AI 상태를 변경하지 않습니다.
/// </summary>
public class HWJ_EnemyAIActionDebugOverlay : MonoBehaviour
{
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private bool onlyFailures = true;
    [SerializeField] private int maxEntries = 8;
    [SerializeField] private int maxMessageCharacters = 96;
    [SerializeField] private Rect overlayRect = new Rect(12f, 12f, 680f, 260f);
    [SerializeField] private List<HWJ_EnemyAIActionDebugEntry> entries = new List<HWJ_EnemyAIActionDebugEntry>();

    public bool ShowOverlay => showOverlay;
    public bool OnlyFailures => onlyFailures;
    public int EntryCount => entries.Count;

    private void OnEnable()
    {
        HWJ_GameplayEvents.EnemyAIActionResolved += HandleEnemyAIActionResolved;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.EnemyAIActionResolved -= HandleEnemyAIActionResolved;
    }

    private void OnGUI()
    {
        if (!showOverlay)
        {
            return;
        }

        GUILayout.BeginArea(overlayRect, GUI.skin.box);
        GUILayout.Label("HWJ Enemy AI Actions");

        if (entries.Count == 0)
        {
            GUILayout.Label("No action events.");
        }

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            HWJ_EnemyAIActionDebugEntry entry = entries[i];
            GUILayout.Label(FormatEntry(entry));
        }

        GUILayout.EndArea();
    }

    public void SetOverlayVisible(bool visible)
    {
        showOverlay = visible;
    }

    public void SetOnlyFailures(bool value)
    {
        onlyFailures = value;
    }

    public HWJ_EnemyAIActionDebugEntry GetEntry(int index)
    {
        return index >= 0 && index < entries.Count ? entries[index] : null;
    }

    public void Clear()
    {
        entries.Clear();
    }

    private void HandleEnemyAIActionResolved(HWJ_EnemyAIActionEvent actionEvent)
    {
        HWJ_EnemyAIActionResult actionResult = actionEvent.ActionResult;

        if (onlyFailures && actionResult.Succeeded)
        {
            return;
        }

        string monsterName = actionEvent.MonsterAI != null
            ? actionEvent.MonsterAI.gameObject.name
            : "Missing MonsterAI";

        entries.Add(new HWJ_EnemyAIActionDebugEntry(monsterName, actionResult));

        int safeMaxEntries = Mathf.Max(1, maxEntries);

        while (entries.Count > safeMaxEntries)
        {
            entries.RemoveAt(0);
        }
    }

    private string FormatEntry(HWJ_EnemyAIActionDebugEntry entry)
    {
        if (entry == null)
        {
            return "Missing action entry.";
        }

        string message = entry.Message;

        if (!string.IsNullOrEmpty(message) && message.Length > maxMessageCharacters)
        {
            message = message.Substring(0, Mathf.Max(0, maxMessageCharacters)) + "...";
        }

        return $"{entry.MonsterName} | {entry.ActionType} | Success:{entry.Succeeded} | Failure:{entry.FailureCode} | {entry.State}->{entry.NextState} | Dist:{entry.TargetDistance:0.##} | {message}";
    }
}
