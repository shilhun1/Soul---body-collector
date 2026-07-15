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
    NotEnoughSkillPoint
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
    public readonly int SkillPointCost;
    public readonly int RemainingSkillPoint;
    public readonly string Message;

    private HWJ_SkillUnlockResult(
        bool succeeded,
        HWJ_SkillUnlockFailureCode failureCode,
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        int skillPointCost,
        int remainingSkillPoint,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        SkillId = skillId;
        DefinitionEntry = definitionEntry;
        SkillPointCost = skillPointCost;
        RemainingSkillPoint = remainingSkillPoint;
        Message = message;
    }

    public static HWJ_SkillUnlockResult Success(
        string skillId,
        HWJ_SkillEntryData definitionEntry,
        int skillPointCost,
        int remainingSkillPoint,
        string message)
    {
        return new HWJ_SkillUnlockResult(
            true,
            HWJ_SkillUnlockFailureCode.None,
            skillId,
            definitionEntry,
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
        string message)
    {
        return new HWJ_SkillUnlockResult(
            false,
            failureCode,
            skillId,
            definitionEntry,
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
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_LevelUpSystem playerLevelProgress;
    [SerializeField] private bool initializeStartingSkillsOnAwake = true;
    [SerializeField] private bool autoUnlockLevelSkills = true;
    [SerializeField] private bool autoUnlockOnlyFreeSkills = true;
    [SerializeField] private List<string> unlockedSkillIds = new List<string>();
    [SerializeField] private string lastUnlockMessage;

    public string LastUnlockMessage => lastUnlockMessage;
    public int UnlockedSkillCount => unlockedSkillIds.Count;

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
        return !string.IsNullOrEmpty(skillId) && unlockedSkillIds.Contains(skillId);
    }

    public bool HasSkillDefinition(string skillId)
    {
        return TryFindSkillEntry(skillId, out _);
    }

    /// <summary>
    /// 저장 시스템이 Unity Object 참조 없이 문자열 ID 목록만 가져갈 때 사용합니다.
    /// </summary>
    public string[] GetUnlockedSkillIds()
    {
        return unlockedSkillIds.ToArray();
    }

    /// <summary>
    /// 저장 데이터에서 복원한 스킬 ID 목록을 런타임 상태에 다시 넣습니다.
    /// </summary>
    public void RestoreUnlockedSkills(IEnumerable<string> skillIds)
    {
        unlockedSkillIds.Clear();

        if (skillIds == null)
        {
            lastUnlockMessage = "Restored empty unlocked skill list.";
            return;
        }

        foreach (string skillId in skillIds)
        {
            AddUnlockedSkillId(skillId);
        }

        lastUnlockMessage = $"Restored {unlockedSkillIds.Count} unlocked skills.";
    }

    /// <summary>
    /// 시작 해금 스킬과 현재 레벨 조건을 만족하는 무료 스킬을 런타임 해금 목록에 반영합니다.
    /// </summary>
    public int RefreshStartingAndLevelUnlockedSkills()
    {
        ResolveReferences();

        int unlockedCount = 0;

        if (!TryGetPlayerData(out HWJ_PlayerTypeDataSO playerTypeData)
            || playerTypeData.SkillSet == null
            || playerTypeData.SkillSet.skills == null)
        {
            lastUnlockMessage = "Skill refresh skipped: missing player skill set.";
            return 0;
        }

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
        string message)
    {
        HWJ_SkillUnlockResult unlockResult = HWJ_SkillUnlockResult.Success(
            skillId,
            definitionEntry,
            skillPointCost,
            GetRemainingSkillPoint(),
            message);
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
    }
}
