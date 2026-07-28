using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_StatOrbProgressSystem : MonoBehaviour
{
    [Header("능력치 구슬 진행도")]
    [Tooltip("능력치 보너스를 실제 런타임 능력치에 적용할 대상입니다. 비어 있으면 같은 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [Tooltip("플레이어가 지금까지 획득한 능력치 구슬 ID와 중첩 횟수입니다. SO 원본 데이터가 아니라 런타임/저장 데이터입니다.")]
    [SerializeField] private List<HWJ_StatOrbStackRuntimeData> statOrbStacks = new List<HWJ_StatOrbStackRuntimeData>();

    public int StackEntryCount => statOrbStacks != null ? statOrbStacks.Count : 0;

    private void Awake()
    {
        ResolveReferences();
        EnsureStackList();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public bool TryApplyStatOrb(HWJ_StatOrbDataSO statOrbData, out string resultMessage)
    {
        ResolveReferences();
        return TryApplyStatOrb(statOrbData, runtimeStatus, out resultMessage);
    }

    public bool TryApplyStatOrb(
        HWJ_StatOrbDataSO statOrbData,
        HWJ_RuntimeStatusSystem targetStatus,
        out string resultMessage)
    {
        EnsureStackList();

        if (statOrbData == null)
        {
            resultMessage = "Stat orb data is missing.";
            return false;
        }

        if (targetStatus == null)
        {
            resultMessage = "Runtime status target is missing.";
            return false;
        }

        string stableOrbId = NormalizeOrbId(statOrbData.OrbId);

        if (string.IsNullOrEmpty(stableOrbId))
        {
            resultMessage = "Stat orb id is empty.";
            return false;
        }

        if (!statOrbData.IsPermanent)
        {
            targetStatus.ApplyStatOrbBonus(statOrbData, false);
            resultMessage = $"Temporary stat orb applied: {stableOrbId}.";
            return true;
        }

        int currentStackCount = GetStackCount(stableOrbId);
        int maxStackCount = statOrbData.MaxStackCount;

        if (currentStackCount >= maxStackCount)
        {
            resultMessage = $"Stat orb stack is already maxed: {stableOrbId} ({currentStackCount}/{maxStackCount}).";
            return false;
        }

        SetStackCount(stableOrbId, currentStackCount + 1);
        targetStatus.ApplyStatOrbBonus(statOrbData, false);
        resultMessage = $"Stat orb applied: {stableOrbId} ({currentStackCount + 1}/{maxStackCount}).";
        RaiseStatOrbStackChanged(
            statOrbData,
            stableOrbId,
            currentStackCount,
            currentStackCount + 1,
            maxStackCount,
            false,
            resultMessage);
        return true;
    }

    public bool CanApplyStatOrb(HWJ_StatOrbDataSO statOrbData)
    {
        if (statOrbData == null)
        {
            return false;
        }

        if (!statOrbData.IsPermanent)
        {
            return true;
        }

        string stableOrbId = NormalizeOrbId(statOrbData.OrbId);
        return !string.IsNullOrEmpty(stableOrbId)
            && GetStackCount(stableOrbId) < statOrbData.MaxStackCount;
    }

    public int GetStackCount(string orbId)
    {
        EnsureStackList();
        string stableOrbId = NormalizeOrbId(orbId);

        if (string.IsNullOrEmpty(stableOrbId))
        {
            return 0;
        }

        for (int i = 0; i < statOrbStacks.Count; i++)
        {
            HWJ_StatOrbStackRuntimeData stackData = statOrbStacks[i];

            if (stackData != null && stackData.OrbId == stableOrbId)
            {
                return stackData.StackCount;
            }
        }

        return 0;
    }

    public HWJ_RuntimeStatOrbStackSnapshot[] CreateSnapshot()
    {
        EnsureStackList();
        List<HWJ_RuntimeStatOrbStackSnapshot> snapshots = new List<HWJ_RuntimeStatOrbStackSnapshot>();

        for (int i = 0; i < statOrbStacks.Count; i++)
        {
            HWJ_StatOrbStackRuntimeData stackData = statOrbStacks[i];

            if (stackData == null || string.IsNullOrEmpty(stackData.OrbId) || stackData.StackCount <= 0)
            {
                continue;
            }

            snapshots.Add(new HWJ_RuntimeStatOrbStackSnapshot
            {
                statOrbId = stackData.OrbId,
                stackCount = stackData.StackCount
            });
        }

        return snapshots.ToArray();
    }

    public void RestoreStatOrbStacks(
        IEnumerable<HWJ_RuntimeStatOrbStackSnapshot> stackSnapshots,
        HWJ_RuntimeStatusSystem targetStatus,
        HWJ_GameplayDatabaseSO gameplayDatabase = null)
    {
        EnsureStackList();
        statOrbStacks.Clear();

        if (targetStatus == null)
        {
            targetStatus = runtimeStatus;
        }

        if (targetStatus != null)
        {
            targetStatus.ClearStatOrbBonuses();
        }

        if (stackSnapshots == null)
        {
            return;
        }

        foreach (HWJ_RuntimeStatOrbStackSnapshot stackSnapshot in stackSnapshots)
        {
            string stableOrbId = NormalizeOrbId(stackSnapshot.statOrbId);
            int savedStackCount = Mathf.Max(0, stackSnapshot.stackCount);

            if (string.IsNullOrEmpty(stableOrbId) || savedStackCount <= 0)
            {
                continue;
            }

            HWJ_StatOrbDataSO statOrbData = ResolveStatOrbData(stableOrbId, gameplayDatabase);
            int restoredStackCount = statOrbData != null
                ? Mathf.Min(savedStackCount, statOrbData.MaxStackCount)
                : savedStackCount;

            SetStackCount(stableOrbId, restoredStackCount);
            RaiseStatOrbStackChanged(
                statOrbData,
                stableOrbId,
                0,
                restoredStackCount,
                statOrbData != null ? statOrbData.MaxStackCount : restoredStackCount,
                true,
                $"Stat orb stack restored: {stableOrbId} ({restoredStackCount}).");

            if (statOrbData == null || targetStatus == null)
            {
                continue;
            }

            for (int i = 0; i < restoredStackCount; i++)
            {
                targetStatus.ApplyStatOrbBonus(statOrbData, true);
            }
        }
    }

    public void RestoreStatOrbStacks(
        IEnumerable<HWJ_SaveStatOrbStackData> saveStackData,
        HWJ_RuntimeStatusSystem targetStatus,
        HWJ_GameplayDatabaseSO gameplayDatabase = null)
    {
        if (saveStackData == null)
        {
            RestoreStatOrbStacks((IEnumerable<HWJ_RuntimeStatOrbStackSnapshot>)null, targetStatus, gameplayDatabase);
            return;
        }

        List<HWJ_RuntimeStatOrbStackSnapshot> snapshots = new List<HWJ_RuntimeStatOrbStackSnapshot>();

        foreach (HWJ_SaveStatOrbStackData saveStack in saveStackData)
        {
            if (saveStack == null)
            {
                continue;
            }

            snapshots.Add(new HWJ_RuntimeStatOrbStackSnapshot
            {
                statOrbId = saveStack.statOrbId,
                stackCount = saveStack.stackCount
            });
        }

        RestoreStatOrbStacks(snapshots, targetStatus, gameplayDatabase);
    }

    private void ResolveReferences()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }

    private void EnsureStackList()
    {
        if (statOrbStacks == null)
        {
            statOrbStacks = new List<HWJ_StatOrbStackRuntimeData>();
        }
    }

    private void SetStackCount(string orbId, int stackCount)
    {
        EnsureStackList();
        string stableOrbId = NormalizeOrbId(orbId);

        if (string.IsNullOrEmpty(stableOrbId))
        {
            return;
        }

        for (int i = 0; i < statOrbStacks.Count; i++)
        {
            HWJ_StatOrbStackRuntimeData stackData = statOrbStacks[i];

            if (stackData != null && stackData.OrbId == stableOrbId)
            {
                stackData.SetStackCount(stackCount);
                return;
            }
        }

        statOrbStacks.Add(new HWJ_StatOrbStackRuntimeData(stableOrbId, stackCount));
    }

    private void RaiseStatOrbStackChanged(
        HWJ_StatOrbDataSO statOrbData,
        string statOrbId,
        int previousStackCount,
        int currentStackCount,
        int maxStackCount,
        bool restoredFromSave,
        string message)
    {
        HWJ_GameplayEvents.RaiseStatOrbStackChanged(
            new HWJ_StatOrbStackChangedEvent(
                this,
                statOrbData,
                statOrbId,
                previousStackCount,
                currentStackCount,
                maxStackCount,
                restoredFromSave,
                message));
    }

    private static HWJ_StatOrbDataSO ResolveStatOrbData(string orbId, HWJ_GameplayDatabaseSO gameplayDatabase)
    {
        if (gameplayDatabase != null && gameplayDatabase.TryGetStatOrb(orbId, out HWJ_StatOrbDataSO databaseStatOrb))
        {
            return databaseStatOrb;
        }

        return HWJ_GameAccess.TryGetStatOrb(orbId, out HWJ_StatOrbDataSO managerStatOrb)
            ? managerStatOrb
            : null;
    }

    private static string NormalizeOrbId(string orbId)
    {
        return string.IsNullOrWhiteSpace(orbId) ? null : orbId.Trim();
    }
}

[Serializable]
public class HWJ_StatOrbStackRuntimeData
{
    [SerializeField] private string orbId;
    [SerializeField] private int stackCount;

    public string OrbId => orbId;
    public int StackCount => stackCount;

    public HWJ_StatOrbStackRuntimeData(string orbId, int stackCount)
    {
        this.orbId = orbId;
        this.stackCount = Mathf.Max(0, stackCount);
    }

    public void SetStackCount(int value)
    {
        stackCount = Mathf.Max(0, value);
    }
}
