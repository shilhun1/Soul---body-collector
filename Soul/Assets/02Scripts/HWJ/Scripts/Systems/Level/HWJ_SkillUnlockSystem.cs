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
public partial class HWJ_SkillUnlockSystem : MonoBehaviour
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

}
