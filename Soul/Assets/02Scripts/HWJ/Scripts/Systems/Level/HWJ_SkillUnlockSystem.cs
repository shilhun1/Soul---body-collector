using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 해금 실패 원인을 호출자가 분기 처리할 수 있게 만드는 코드입니다.
/// </summary>
public enum HWJ_SkillUnlockFailureCode
{
    None,
    MissingPlayerData,
    MissingSkillId,
    SkillNotFound,
    AlreadyUnlocked,
    LevelTooLow,
    MissingLevelSystem,
    NotEnoughSkillPoint,
    MissingGameplayDatabase,
    MissingSkillNodeId,
    SkillNodeNotFound,
    PreviousSkillNodeLocked
}

/// <summary>
/// 스킬 해금 요청 결과입니다.
/// 성공 여부, 실패 코드, 소비 포인트, 남은 포인트를 UI/저장/테스트에서 그대로 읽을 수 있게 합니다.
/// </summary>
public readonly struct HWJ_SkillUnlockResult
{
    public readonly bool Succeeded;
    public readonly HWJ_SkillUnlockFailureCode FailureCode;
    public readonly string SkillId;
    public readonly HWJ_SkillEntryData DefinitionEntry;
    public readonly HWJ_SkillNodeDataSO SkillNode;
    public readonly int SkillPointCost;
    public readonly int RemainingSkillPoint;
    public readonly string Message;

    private HWJ_SkillUnlockResult(
        bool succeeded,
        HWJ_SkillUnlockFailureCode failureCode,
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        HWJ_SkillNodeDataSO skillNode,
        int skillPointCost,
        int remainingSkillPoint,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        SkillId = skillId;
        DefinitionEntry = definitionEntry;
        SkillNode = skillNode;
        SkillPointCost = skillPointCost;
        RemainingSkillPoint = remainingSkillPoint;
        Message = message;
    }

    public static HWJ_SkillUnlockResult Success(
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        int skillPointCost,
        int remainingSkillPoint,
        string message,
        HWJ_SkillNodeDataSO skillNode = null)
    {
        return new HWJ_SkillUnlockResult(
            true,
            HWJ_SkillUnlockFailureCode.None,
            skillId,
            definitionEntry,
            skillNode,
            skillPointCost,
            remainingSkillPoint,
            message);
    }

    public static HWJ_SkillUnlockResult Fail(
        HWJ_SkillUnlockFailureCode failureCode,
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        int skillPointCost,
        int remainingSkillPoint,
        string message,
        HWJ_SkillNodeDataSO skillNode = null)
    {
        return new HWJ_SkillUnlockResult(
            false,
            failureCode,
            skillId,
            definitionEntry,
            skillNode,
            skillPointCost,
            remainingSkillPoint,
            message);
    }
}

/// <summary>
/// 플레이어의 런타임 스킬 해금 상태를 관리합니다.
/// PlayerTypeDataSO의 SkillSet은 정의 데이터로만 읽고, 해금된 스킬 ID는 이 시스템이 별도로 보관합니다.
/// </summary>
public class HWJ_SkillUnlockSystem : MonoBehaviour
{
    [Header("기존 스킬 해금")]
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_LevelUpSystem playerLevelProgress;
    [SerializeField] private bool initializeStartingSkillsOnAwake = true;
    [SerializeField] private bool autoUnlockLevelSkills = true;
    [SerializeField] private bool autoUnlockOnlyFreeSkills = true;

    [Header("스킬 노드 해금")]
    [SerializeField] private HWJ_GameplayDatabaseSO gameplayDatabase;
    [SerializeField] private bool useSkillNodeProgress = true;
    [SerializeField] private bool autoUnlockFreeStartingSkillNodes = true;
    [SerializeField] private int cachedCommonUnlockedSkillStep;
    [SerializeField] private string lastSkillNodeMessage;

    [Header("저장 가능한 해금 ID")]
    [SerializeField] private List<string> unlockedSkillIds = new List<string>();
    [SerializeField] private List<string> unlockedSkillNodeIds = new List<string>();
    [SerializeField] private string lastUnlockMessage;

    public string LastUnlockMessage => lastUnlockMessage;
    public string LastSkillNodeMessage => lastSkillNodeMessage;
    public int UnlockedSkillCount => unlockedSkillIds.Count;
    public int UnlockedSkillNodeCount => unlockedSkillNodeIds.Count;
    public int CommonUnlockedSkillStep => GetUnlockedCommonSkillNodeStep();

    private void Awake()
    {
        ResolveReferences();

        if (initializeStartingSkillsOnAwake)
        {
            RefreshStartingAndLevelUnlockedSkills();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        HWJ_GameplayEvents.PlayerLevelChanged += OnPlayerLevelChanged;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.PlayerLevelChanged -= OnPlayerLevelChanged;
    }

    public bool IsSkillUnlocked(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        if (unlockedSkillIds.Contains(skillId))
        {
            return true;
        }

        return IsSkillNodeUnlocked(skillId) || IsSkillActionUnlockedBySkillNodeProgress(skillId);
    }

    public bool HasSkillDefinition(string skillId)
    {
        return TryFindSkillEntry(skillId, out _);
    }

    public bool HasSkillNodeDefinitionForSkillAction(string skillActionId)
    {
        return TryGetSkillNodeForSkillAction(skillActionId, out _);
    }

    public bool TryGetSkillNodeForSkillAction(string skillActionId, out HWJ_SkillNodeDataSO skillNode)
    {
        skillNode = null;

        if (string.IsNullOrEmpty(skillActionId) || !ResolveGameplayDatabase())
        {
            return false;
        }

        return gameplayDatabase.TryGetSkillNodeBySkillAction(skillActionId, out skillNode);
    }

    public bool IsSkillActionUnlockedBySkillNodeProgress(string skillActionId)
    {
        if (!useSkillNodeProgress || !TryGetSkillNodeForSkillAction(skillActionId, out HWJ_SkillNodeDataSO skillNode))
        {
            return false;
        }

        return IsSkillNodeUnlocked(skillNode);
    }

    public bool IsSkillEntryUnlocked(HWJ_SkillEntryData skillEntry)
    {
        if (skillEntry == null || string.IsNullOrEmpty(skillEntry.skillId))
        {
            return false;
        }

        if (HasSkillNodeDefinitionForSkillAction(skillEntry.skillId))
        {
            return IsSkillActionUnlockedBySkillNodeProgress(skillEntry.skillId);
        }

        if (HasSkillDefinition(skillEntry.skillId))
        {
            return IsSkillUnlocked(skillEntry.skillId);
        }

        return skillEntry.startsUnlocked;
    }

    public HWJ_SkillNodeDataSO[] GetSkillNodesForWeapon(HWJ_WeaponType weaponType)
    {
        if (!TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList))
        {
            return new HWJ_SkillNodeDataSO[0];
        }

        List<HWJ_SkillNodeDataSO> matchedNodes = new List<HWJ_SkillNodeDataSO>();

        for (int i = 0; i < skillNodeList.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodeList[i];

            if (skillNode != null && skillNode.MatchesWeapon(weaponType))
            {
                matchedNodes.Add(skillNode);
            }
        }

        return matchedNodes.ToArray();
    }

    public bool IsSkillNodeUnlocked(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return false;
        }

        if (unlockedSkillNodeIds.Contains(nodeId))
        {
            return true;
        }

        if (!useSkillNodeProgress || !ResolveGameplayDatabase())
        {
            return false;
        }

        return gameplayDatabase.TryGetSkillNode(nodeId, out HWJ_SkillNodeDataSO skillNode)
            && IsSkillNodeUnlocked(skillNode);
    }

    public bool IsSkillNodeUnlocked(HWJ_SkillNodeDataSO skillNode)
    {
        if (skillNode == null || string.IsNullOrEmpty(skillNode.NodeId))
        {
            return false;
        }

        if (unlockedSkillNodeIds.Contains(skillNode.NodeId))
        {
            return true;
        }

        if (!useSkillNodeProgress)
        {
            return false;
        }

        int skillStep = skillNode.SkillStep;
        return skillStep > 0 && skillStep <= GetUnlockedCommonSkillNodeStep();
    }

    /// <summary>
    /// 저장 시스템이 Unity Object 참조 없이 문자열 ID 목록만 가져갈 때 사용합니다.
    /// </summary>
    public string[] GetUnlockedSkillIds()
    {
        return unlockedSkillIds.ToArray();
    }

    public string[] GetUnlockedSkillNodeIds()
    {
        return unlockedSkillNodeIds.ToArray();
    }

    /// <summary>
    /// 저장 데이터에서 복원한 스킬 ID 목록을 런타임 상태에 다시 넣습니다.
    /// </summary>
    public void RestoreUnlockedSkills(IEnumerable<string> skillIds)
    {
        RestoreUnlockedSkills(skillIds, null);
    }

    public void RestoreUnlockedSkills(IEnumerable<string> skillIds, IEnumerable<string> skillNodeIds)
    {
        unlockedSkillIds.Clear();
        unlockedSkillNodeIds.Clear();

        RestoreUnlockedSkillIds(skillIds, true);
        RestoreUnlockedSkillNodeIds(skillNodeIds);
        cachedCommonUnlockedSkillStep = GetUnlockedCommonSkillNodeStep();

        int restoredSkillCount = unlockedSkillIds.Count;
        int restoredSkillNodeCount = unlockedSkillNodeIds.Count;

        if (restoredSkillCount <= 0 && restoredSkillNodeCount <= 0)
        {
            lastUnlockMessage = "Restored empty unlocked skill list.";
            return;
        }

        lastUnlockMessage = $"Restored {restoredSkillCount} skills and {restoredSkillNodeCount} skill nodes.";
    }

    /// <summary>
    /// 시작 해금 스킬과 현재 레벨 조건을 만족하는 무료 스킬을 런타임 해금 목록에 반영합니다.
    /// </summary>
    public int RefreshStartingAndLevelUnlockedSkills()
    {
        ResolveReferences();

        int unlockedCount = 0;

        if (TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData)
            && playerTypeData.SkillSet != null
            && playerTypeData.SkillSet.skills != null)
        {
            for (int i = 0; i < playerTypeData.SkillSet.skills.Length; i++)
            {
                HWJ_SkillEntryData definitionEntry = playerTypeData.SkillSet.skills[i];

                if (!IsAutoUnlockEligible(definitionEntry))
                {
                    continue;
                }

                if (UnlockWithoutCost(definitionEntry, "Skill unlocked by starting data or level.").Succeeded)
                {
                    unlockedCount++;
                }
            }
        }
        else
        {
            lastUnlockMessage = "Skill refresh skipped: missing player skill set.";
        }

        unlockedCount += RefreshStartingAndLevelUnlockedSkillNodes();
        return unlockedCount;
    }

    public int RefreshStartingAndLevelUnlockedSkillNodes()
    {
        ResolveReferences();

        if (!useSkillNodeProgress || !autoUnlockFreeStartingSkillNodes)
        {
            return 0;
        }

        if (!TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList))
        {
            lastSkillNodeMessage = "Skill node refresh skipped: missing gameplay database or skill nodes.";
            return 0;
        }

        int unlockedCount = 0;

        for (int i = 0; i < skillNodeList.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodeList[i];

            if (!IsAutoUnlockEligible(skillNode))
            {
                continue;
            }

            if (AddUnlockedSkillNodeId(skillNode.NodeId))
            {
                unlockedCount++;
                RaiseUnlocked(
                    skillNode.NodeId,
                    null,
                    0,
                    "Skill node unlocked by starting data.",
                    skillNode);
            }
        }

        cachedCommonUnlockedSkillStep = GetUnlockedCommonSkillNodeStep();
        lastSkillNodeMessage = $"Refreshed starting skill nodes. Added {unlockedCount}.";
        return unlockedCount;
    }

    /// <summary>
    /// 스킬 포인트 소비를 포함해 플레이어 스킬을 해금합니다.
    /// 실패해도 내부 상태를 바꾸지 않고 실패 코드를 반환합니다.
    /// </summary>
    public HWJ_SkillUnlockResult TryUnlockSkill(string skillId)
    {
        return TryUnlockSkill(skillId, true);
    }

    public HWJ_SkillUnlockResult TryUnlockSkillNode(string nodeId)
    {
        return TryUnlockSkillNode(nodeId, true);
    }

    public HWJ_SkillUnlockResult TryUnlockSkillNode(string nodeId, bool spendSkillPoint)
    {
        ResolveReferences();

        if (string.IsNullOrEmpty(nodeId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillNodeId,
                nodeId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Skill node unlock failed: missing node id."));
        }

        if (!ResolveGameplayDatabase())
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingGameplayDatabase,
                nodeId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Skill node unlock failed: missing gameplay database."));
        }

        if (!gameplayDatabase.TryGetSkillNode(nodeId, out HWJ_SkillNodeDataSO skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.SkillNodeNotFound,
                nodeId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} is not registered.",
                null));
        }

        if (IsSkillNodeUnlocked(skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                nodeId,
                null,
                skillNode.SkillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} is already unlocked.",
                skillNode));
        }

        if (GetCurrentLevel() < skillNode.RequiredLevel)
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.LevelTooLow,
                nodeId,
                null,
                skillNode.SkillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} requires level {skillNode.RequiredLevel}.",
                skillNode));
        }

        if (!IsPreviousSkillNodeRequirementMet(skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.PreviousSkillNodeLocked,
                nodeId,
                null,
                skillNode.SkillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} requires previous node {skillNode.PrerequisiteNodeId}.",
                skillNode));
        }

        int skillPointCost = skillNode.SkillPointCost;

        if (spendSkillPoint && skillPointCost > 0)
        {
            if (playerLevelProgress == null)
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.MissingLevelSystem,
                    nodeId,
                    null,
                    skillPointCost,
                    0,
                    $"Skill node unlock failed: {nodeId} requires a level system.",
                    skillNode));
            }

            if (!playerLevelProgress.TrySpendSkillPoint(skillPointCost))
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.NotEnoughSkillPoint,
                    nodeId,
                    null,
                    skillPointCost,
                    playerLevelProgress.SkillPoint,
                    $"Skill node unlock failed: {nodeId} requires {skillPointCost} skill points.",
                    skillNode));
            }
        }

        if (!AddUnlockedSkillNodeId(skillNode.NodeId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                nodeId,
                null,
                skillPointCost,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: {nodeId} is already unlocked.",
                skillNode));
        }

        cachedCommonUnlockedSkillStep = GetUnlockedCommonSkillNodeStep();
        lastSkillNodeMessage = $"Skill node unlocked: {nodeId}. Common step {cachedCommonUnlockedSkillStep}.";

        return RaiseUnlocked(
            skillNode.NodeId,
            null,
            skillPointCost,
            skillPointCost > 0 ? "Skill node unlocked by spending skill points." : "Skill node unlocked.",
            skillNode);
    }

    public HWJ_SkillUnlockResult TryUnlockSkillNodeForSkillAction(string skillActionId, bool spendSkillPoint = true)
    {
        if (!TryGetSkillNodeForSkillAction(skillActionId, out HWJ_SkillNodeDataSO skillNode))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.SkillNodeNotFound,
                skillActionId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Skill node unlock failed: no node is linked to {skillActionId}."));
        }

        return TryUnlockSkillNode(skillNode.NodeId, spendSkillPoint);
    }

    public HWJ_SkillUnlockResult TryUnlockSkill(string skillId, bool spendSkillPoint)
    {
        ResolveReferences();

        if (string.IsNullOrEmpty(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillId,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Skill unlock failed: missing skill id."));
        }

        if (!TryFindSkillEntry(skillId, out HWJ_SkillEntryData definitionEntry))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.SkillNotFound,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {skillId} is not in player skill set."));
        }

        if (IsSkillUnlocked(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                skillId,
                definitionEntry,
                Mathf.Max(0, definitionEntry.requiredSkillPoint),
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {skillId} is already unlocked."));
        }

        if (!IsLevelRequirementMet(definitionEntry))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.LevelTooLow,
                skillId,
                definitionEntry,
                Mathf.Max(0, definitionEntry.requiredSkillPoint),
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {skillId} requires level {definitionEntry.unlockLevel}."));
        }

        int skillPointCost = Mathf.Max(0, definitionEntry.requiredSkillPoint);

        if (spendSkillPoint && skillPointCost > 0)
        {
            if (playerLevelProgress == null)
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.MissingLevelSystem,
                    skillId,
                    definitionEntry,
                    skillPointCost,
                    0,
                    $"Skill unlock failed: {skillId} requires a level system."));
            }

            if (!playerLevelProgress.TrySpendSkillPoint(skillPointCost))
            {
                return SetLastResult(HWJ_SkillUnlockResult.Fail(
                    HWJ_SkillUnlockFailureCode.NotEnoughSkillPoint,
                    skillId,
                    definitionEntry,
                    skillPointCost,
                    playerLevelProgress.SkillPoint,
                    $"Skill unlock failed: {skillId} requires {skillPointCost} skill points."));
            }
        }

        return UnlockWithoutCost(
            definitionEntry,
            skillPointCost > 0 ? "Skill unlocked by spending skill points." : "Skill unlocked.");
    }

    /// <summary>
    /// 보상, 디버그, 이벤트 연출처럼 조건을 우회해야 하는 경우에만 사용합니다.
    /// </summary>
    public HWJ_SkillUnlockResult ForceUnlockSkill(string skillId, string reason = null)
    {
        ResolveReferences();

        if (string.IsNullOrEmpty(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillId,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                "Force unlock failed: missing skill id."));
        }

        if (IsSkillUnlocked(skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                skillId,
                null,
                0,
                GetRemainingSkillPoint(),
                $"Force unlock failed: {skillId} is already unlocked."));
        }

        AddUnlockedSkillId(skillId);
        return RaiseUnlocked(skillId, null, 0, reason ?? "Skill force unlocked.");
    }

    private HWJ_SkillUnlockResult UnlockWithoutCost(HWJ_SkillEntryData definitionEntry, string message)
    {
        if (definitionEntry == null || string.IsNullOrEmpty(definitionEntry.skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.MissingSkillId,
                null,
                definitionEntry,
                0,
                GetRemainingSkillPoint(),
                "Skill unlock failed: missing skill entry."));
        }

        if (!AddUnlockedSkillId(definitionEntry.skillId))
        {
            return SetLastResult(HWJ_SkillUnlockResult.Fail(
                HWJ_SkillUnlockFailureCode.AlreadyUnlocked,
                definitionEntry.skillId,
                definitionEntry,
                Mathf.Max(0, definitionEntry.requiredSkillPoint),
                GetRemainingSkillPoint(),
                $"Skill unlock failed: {definitionEntry.skillId} is already unlocked."));
        }

        return RaiseUnlocked(
            definitionEntry.skillId,
            definitionEntry,
            Mathf.Max(0, definitionEntry.requiredSkillPoint),
            message);
    }

    private HWJ_SkillUnlockResult RaiseUnlocked(
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        int skillPointCost,
        string message,
        HWJ_SkillNodeDataSO skillNode = null)
    {
        HWJ_SkillUnlockResult unlockResult = HWJ_SkillUnlockResult.Success(
            skillId,
            definitionEntry,
            skillPointCost,
            GetRemainingSkillPoint(),
            message,
            skillNode);
        SetLastResult(unlockResult);

        HWJ_GameplayEvents.RaiseSkillUnlocked(
            new HWJ_SkillUnlockedEvent(
                this,
                skillId,
                GetCurrentLevel(),
                unlockResult.RemainingSkillPoint,
                skillPointCost > 0,
                message));

        return unlockResult;
    }

    private HWJ_SkillUnlockResult SetLastResult(HWJ_SkillUnlockResult result)
    {
        lastUnlockMessage = result.Message;
        return result;
    }

    private bool AddUnlockedSkillId(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || unlockedSkillIds.Contains(skillId))
        {
            return false;
        }

        unlockedSkillIds.Add(skillId);
        return true;
    }

    private bool AddUnlockedSkillNodeId(string skillNodeId)
    {
        if (string.IsNullOrEmpty(skillNodeId) || unlockedSkillNodeIds.Contains(skillNodeId))
        {
            return false;
        }

        unlockedSkillNodeIds.Add(skillNodeId);
        return true;
    }

    private int RestoreUnlockedSkillIds(IEnumerable<string> skillIds, bool classifyKnownSkillNodes)
    {
        if (skillIds == null)
        {
            return 0;
        }

        int restoredCount = 0;

        foreach (string skillId in skillIds)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                continue;
            }

            // Older saves stored skill node IDs in the legacy skill list. Runtime separates them here.
            if (classifyKnownSkillNodes && IsKnownSkillNodeId(skillId))
            {
                if (AddUnlockedSkillNodeId(skillId))
                {
                    restoredCount++;
                }

                continue;
            }

            if (AddUnlockedSkillId(skillId))
            {
                restoredCount++;
            }
        }

        return restoredCount;
    }

    private int RestoreUnlockedSkillNodeIds(IEnumerable<string> skillNodeIds)
    {
        if (skillNodeIds == null)
        {
            return 0;
        }

        int restoredCount = 0;

        foreach (string skillNodeId in skillNodeIds)
        {
            if (AddUnlockedSkillNodeId(skillNodeId))
            {
                restoredCount++;
            }
        }

        return restoredCount;
    }

    private bool IsKnownSkillNodeId(string skillNodeId)
    {
        return !string.IsNullOrEmpty(skillNodeId)
            && ResolveGameplayDatabase()
            && gameplayDatabase.TryGetSkillNode(skillNodeId, out _);
    }

    public int GetUnlockedCommonSkillNodeStep()
    {
        if (!useSkillNodeProgress || !TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList))
        {
            cachedCommonUnlockedSkillStep = 0;
            return cachedCommonUnlockedSkillStep;
        }

        int highestUnlockedStep = 0;

        for (int i = 0; i < skillNodeList.Length; i++)
        {
            HWJ_SkillNodeDataSO skillNode = skillNodeList[i];

            if (skillNode == null || string.IsNullOrEmpty(skillNode.NodeId))
            {
                continue;
            }

            bool nodeDirectlyUnlocked = unlockedSkillNodeIds.Contains(skillNode.NodeId);
            bool nodeStartsUnlocked = IsAutoUnlockedStartingSkillNode(skillNode);

            if (!nodeDirectlyUnlocked && !nodeStartsUnlocked)
            {
                continue;
            }

            highestUnlockedStep = Mathf.Max(highestUnlockedStep, skillNode.SkillStep);
        }

        cachedCommonUnlockedSkillStep = highestUnlockedStep;
        return cachedCommonUnlockedSkillStep;
    }

    private bool IsAutoUnlockedStartingSkillNode(HWJ_SkillNodeDataSO skillNode)
    {
        return useSkillNodeProgress
            && autoUnlockFreeStartingSkillNodes
            && skillNode != null
            && skillNode.IsFreeStartingNode
            && GetCurrentLevel() >= skillNode.RequiredLevel;
    }

    private bool IsPreviousSkillNodeRequirementMet(HWJ_SkillNodeDataSO skillNode)
    {
        if (skillNode == null)
        {
            return false;
        }

        if (!skillNode.HasPrerequisite)
        {
            return true;
        }

        return IsSkillNodeUnlocked(skillNode.PrerequisiteNodeId);
    }

    private bool TryGetSkillNodeList(out HWJ_SkillNodeDataSO[] skillNodeList)
    {
        skillNodeList = null;

        if (!ResolveGameplayDatabase() || gameplayDatabase.SkillNodes == null)
        {
            return false;
        }

        skillNodeList = gameplayDatabase.SkillNodes;
        return skillNodeList.Length > 0;
    }

    private bool ResolveGameplayDatabase()
    {
        if (gameplayDatabase != null)
        {
            return true;
        }

        gameplayDatabase = HWJ_GameAccess.Database;
        return gameplayDatabase != null;
    }

    private bool IsAutoUnlockEligible(HWJ_SkillEntryData definitionEntry)
    {
        if (definitionEntry == null || string.IsNullOrEmpty(definitionEntry.skillId))
        {
            return false;
        }

        if (IsSkillUnlocked(definitionEntry.skillId))
        {
            return false;
        }

        if (definitionEntry.startsUnlocked)
        {
            return true;
        }

        if (!autoUnlockLevelSkills)
        {
            return false;
        }

        if (autoUnlockOnlyFreeSkills && definitionEntry.requiredSkillPoint > 0)
        {
            return false;
        }

        return definitionEntry.unlockLevel > 0 && IsLevelRequirementMet(definitionEntry);
    }

    private bool IsAutoUnlockEligible(HWJ_SkillNodeDataSO skillNode)
    {
        if (skillNode == null || string.IsNullOrEmpty(skillNode.NodeId))
        {
            return false;
        }

        if (unlockedSkillNodeIds.Contains(skillNode.NodeId))
        {
            return false;
        }

        return IsAutoUnlockedStartingSkillNode(skillNode);
    }

    private bool IsLevelRequirementMet(HWJ_SkillEntryData definitionEntry)
    {
        return definitionEntry == null
            || definitionEntry.unlockLevel <= 0
            || GetCurrentLevel() >= definitionEntry.unlockLevel;
    }

    private bool TryFindSkillEntry(string skillId, out HWJ_SkillEntryData definitionEntry)
    {
        definitionEntry = null;

        if (string.IsNullOrEmpty(skillId) || !TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData))
        {
            return false;
        }

        if (playerTypeData.SkillSet == null || playerTypeData.SkillSet.skills == null)
        {
            return false;
        }

        for (int i = 0; i < playerTypeData.SkillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData definedSkill = playerTypeData.SkillSet.skills[i];

            if (definedSkill != null && definedSkill.skillId == skillId)
            {
                definitionEntry = definedSkill;
                return true;
            }
        }

        return false;
    }

    private bool TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData)
    {
        ResolveReferences();

        if (ownerDataResolver != null && ownerDataResolver.TryGetTypeData(out playerTypeData))
        {
            return playerTypeData != null;
        }

        playerTypeData = null;
        return false;
    }

    private int GetCurrentLevel()
    {
        return playerLevelProgress != null ? playerLevelProgress.CurrentLevel : 1;
    }

    private int GetRemainingSkillPoint()
    {
        return playerLevelProgress != null ? playerLevelProgress.SkillPoint : 0;
    }

    private void OnPlayerLevelChanged(HWJ_PlayerLevelChangedEvent levelChangedEvent)
    {
        ResolveReferences();

        if (playerLevelProgress == null || levelChangedEvent.LevelSystem != playerLevelProgress)
        {
            return;
        }

        RefreshStartingAndLevelUnlockedSkills();
    }

    private void ResolveReferences()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (playerLevelProgress == null)
        {
            playerLevelProgress = GetComponent<HWJ_LevelUpSystem>();
        }

        ResolveGameplayDatabase();
    }
}
