using UnityEngine;

public enum HWJ_StageChoiceRewardFailureCode
{
    None,
    MissingRewardData,
    MissingOption,
    StageNotClear,
    StageIdMismatch,
    AlreadySelected,
    AlreadyClaimed,
    MissingPlayerLevel,
    MissingPlayerStatus,
    StatOrbUnavailable
}

public readonly struct HWJ_StageChoiceRewardResult
{
    public readonly bool Succeeded;
    public readonly HWJ_StageChoiceRewardFailureCode FailureCode;
    public readonly HWJ_StageChoiceRewardSystem RewardSystem;
    public readonly HWJ_StageChoiceRewardDataSO RewardData;
    public readonly HWJ_StageChoiceRewardOptionData SelectedOption;
    public readonly int ExperienceGranted;
    public readonly int SkillPointGranted;
    public readonly bool StatOrbGranted;
    public readonly string RewardClaimId;
    public readonly string Message;

    private HWJ_StageChoiceRewardResult(
        bool succeeded,
        HWJ_StageChoiceRewardFailureCode failureCode,
        HWJ_StageChoiceRewardSystem rewardSystem,
        HWJ_StageChoiceRewardDataSO rewardData,
        HWJ_StageChoiceRewardOptionData selectedOption,
        int experienceGranted,
        int skillPointGranted,
        bool statOrbGranted,
        string rewardClaimId,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        RewardSystem = rewardSystem;
        RewardData = rewardData;
        SelectedOption = selectedOption;
        ExperienceGranted = experienceGranted;
        SkillPointGranted = skillPointGranted;
        StatOrbGranted = statOrbGranted;
        RewardClaimId = rewardClaimId;
        Message = message;
    }

    public static HWJ_StageChoiceRewardResult Success(
        HWJ_StageChoiceRewardSystem rewardSystem,
        HWJ_StageChoiceRewardDataSO rewardData,
        HWJ_StageChoiceRewardOptionData selectedOption,
        int experienceGranted,
        int skillPointGranted,
        bool statOrbGranted,
        string rewardClaimId,
        string message)
    {
        return new HWJ_StageChoiceRewardResult(
            true,
            HWJ_StageChoiceRewardFailureCode.None,
            rewardSystem,
            rewardData,
            selectedOption,
            experienceGranted,
            skillPointGranted,
            statOrbGranted,
            rewardClaimId,
            message);
    }

    public static HWJ_StageChoiceRewardResult Fail(
        HWJ_StageChoiceRewardFailureCode failureCode,
        HWJ_StageChoiceRewardSystem rewardSystem,
        HWJ_StageChoiceRewardDataSO rewardData,
        string rewardClaimId,
        string message)
    {
        return new HWJ_StageChoiceRewardResult(
            false,
            failureCode,
            rewardSystem,
            rewardData,
            null,
            0,
            0,
            false,
            rewardClaimId,
            message);
    }
}

[DisallowMultipleComponent]
public class HWJ_StageChoiceRewardSystem : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("스테이지 클리어 상태를 확인할 시스템입니다.")]
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;

    [Tooltip("경험치와 스킬 포인트 보상을 받을 플레이어 레벨 시스템입니다.")]
    [SerializeField] private HWJ_LevelUpSystem playerLevelSystem;

    [Tooltip("스탯 구슬 보상을 받을 플레이어 상태 시스템입니다.")]
    [SerializeField] private HWJ_RuntimeStatusSystem playerStatusSystem;

    [Header("보상 데이터")]
    [Tooltip("스테이지 클리어 후 제시할 보상 선택지 SO입니다.")]
    [SerializeField] private HWJ_StageChoiceRewardDataSO rewardData;

    [Tooltip("켜면 스테이지 클리어 이벤트가 발생했을 때 자동으로 보상 후보를 엽니다.")]
    [SerializeField] private bool offerOnStageCleared = true;

    [Tooltip("켜면 스테이지 클리어 상태에서만 보상을 선택할 수 있습니다.")]
    [SerializeField] private bool requireStageClear = true;

    [Tooltip("켜면 한 번 선택한 보상 묶음은 다시 받을 수 없습니다.")]
    [SerializeField] private bool preventDuplicateSelection = true;

    [Tooltip("켜면 SaveService에 claim ID를 기록해서 저장 데이터 기준 중복 지급도 막습니다.")]
    [SerializeField] private bool useSaveServiceClaim = true;

    [Header("런타임 확인")]
    [SerializeField] private bool isOffered;
    [SerializeField] private bool hasSelectedReward;
    [SerializeField] private string selectedOptionId;
    [SerializeField] private string lastRewardMessage;

    public HWJ_StageChoiceRewardDataSO RewardData => rewardData;
    public bool IsOffered => isOffered;
    public bool HasSelectedReward => hasSelectedReward;
    public string SelectedOptionId => selectedOptionId;
    public string LastRewardMessage => lastRewardMessage;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        HWJ_GameplayEvents.StageCleared += OnStageCleared;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.StageCleared -= OnStageCleared;
    }

    public bool TryOfferRewards()
    {
        ResolveReferences();

        if (rewardData == null)
        {
            lastRewardMessage = "스테이지 선택 보상 열기 실패: 보상 데이터가 없습니다.";
            return false;
        }

        if (!IsStageMatched())
        {
            lastRewardMessage = "스테이지 선택 보상 열기 실패: 스테이지 ID가 맞지 않습니다.";
            return false;
        }

        if (requireStageClear && stageProgressionSystem != null && stageProgressionSystem.CurrentState != HWJ_StageFlowState.StageClear)
        {
            lastRewardMessage = "스테이지 선택 보상 열기 실패: 아직 스테이지 클리어 상태가 아닙니다.";
            return false;
        }

        isOffered = true;
        lastRewardMessage = "스테이지 선택 보상이 열렸습니다.";
        HWJ_GameplayEvents.RaiseStageChoiceRewardOffered(
            new HWJ_StageChoiceRewardOfferedEvent(this, rewardData, lastRewardMessage));
        return true;
    }

    public HWJ_StageChoiceRewardOptionData[] GetOptions()
    {
        return rewardData != null && rewardData.Options != null
            ? rewardData.Options
            : new HWJ_StageChoiceRewardOptionData[0];
    }

    public HWJ_StageChoiceRewardResult TrySelectRewardAt(int optionIndex)
    {
        if (rewardData == null || !rewardData.TryGetOptionAt(optionIndex, out HWJ_StageChoiceRewardOptionData option))
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.MissingOption,
                this,
                rewardData,
                null,
                "스테이지 선택 보상 실패: 선택지 인덱스가 올바르지 않습니다."));
        }

        return TrySelectReward(option.optionId);
    }

    public HWJ_StageChoiceRewardResult TrySelectReward(string optionId)
    {
        ResolveReferences();

        if (rewardData == null)
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.MissingRewardData,
                this,
                rewardData,
                null,
                "스테이지 선택 보상 실패: 보상 데이터가 없습니다."));
        }

        string rewardClaimId = BuildRewardClaimId(optionId);

        if (preventDuplicateSelection && hasSelectedReward)
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.AlreadySelected,
                this,
                rewardData,
                rewardClaimId,
                "스테이지 선택 보상 실패: 이미 보상을 선택했습니다."));
        }

        if (!IsStageMatched())
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.StageIdMismatch,
                this,
                rewardData,
                rewardClaimId,
                "스테이지 선택 보상 실패: 현재 스테이지와 보상 데이터의 스테이지 ID가 다릅니다."));
        }

        if (requireStageClear && stageProgressionSystem != null && stageProgressionSystem.CurrentState != HWJ_StageFlowState.StageClear)
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.StageNotClear,
                this,
                rewardData,
                rewardClaimId,
                "스테이지 선택 보상 실패: 스테이지 클리어 후에 선택할 수 있습니다."));
        }

        if (!rewardData.TryGetOption(optionId, out HWJ_StageChoiceRewardOptionData option))
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.MissingOption,
                this,
                rewardData,
                rewardClaimId,
                "스테이지 선택 보상 실패: 선택지 ID를 찾을 수 없습니다."));
        }

        if (!ValidateGrantRequirements(option, out HWJ_StageChoiceRewardFailureCode validationFailure, out string validationMessage))
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                validationFailure,
                this,
                rewardData,
                rewardClaimId,
                validationMessage));
        }

        if (useSaveServiceClaim
            && HWJ_SaveService.TryGetActiveService(out HWJ_SaveService saveService)
            && !saveService.TryClaimReward(rewardClaimId, false, null))
        {
            return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Fail(
                HWJ_StageChoiceRewardFailureCode.AlreadyClaimed,
                this,
                rewardData,
                rewardClaimId,
                "스테이지 선택 보상 실패: 저장 데이터에서 이미 받은 보상입니다."));
        }

        return GrantSelectedReward(option, rewardClaimId);
    }

    private bool ValidateGrantRequirements(
        HWJ_StageChoiceRewardOptionData option,
        out HWJ_StageChoiceRewardFailureCode failureCode,
        out string message)
    {
        failureCode = HWJ_StageChoiceRewardFailureCode.None;
        message = null;

        if (option == null)
        {
            failureCode = HWJ_StageChoiceRewardFailureCode.MissingOption;
            message = "스테이지 선택 보상 실패: 선택지 데이터가 없습니다.";
            return false;
        }

        int experienceGranted = Mathf.Max(0, option.reward != null ? option.reward.experienceReward : 0);
        int skillPointGranted = Mathf.Max(0, option.reward != null ? option.reward.skillPointReward : 0);

        if ((experienceGranted > 0 || skillPointGranted > 0) && playerLevelSystem == null)
        {
            failureCode = HWJ_StageChoiceRewardFailureCode.MissingPlayerLevel;
            message = "스테이지 선택 보상 실패: 플레이어 레벨 시스템이 없습니다.";
            return false;
        }

        HWJ_StatOrbDataSO statOrb = ResolveStatOrb(option);

        if (statOrb == null)
        {
            return true;
        }

        if (playerStatusSystem == null)
        {
            failureCode = HWJ_StageChoiceRewardFailureCode.MissingPlayerStatus;
            message = "스테이지 선택 보상 실패: 스탯 구슬을 받을 플레이어 상태 시스템이 없습니다.";
            return false;
        }

        if (!playerStatusSystem.CanApplyStatOrb(statOrb))
        {
            failureCode = HWJ_StageChoiceRewardFailureCode.StatOrbUnavailable;
            message = "스테이지 선택 보상 실패: 해당 스탯 구슬은 더 이상 적용할 수 없습니다.";
            return false;
        }

        return true;
    }

    private HWJ_StageChoiceRewardResult GrantSelectedReward(
        HWJ_StageChoiceRewardOptionData option,
        string rewardClaimId)
    {
        int experienceGranted = Mathf.Max(0, option.reward != null ? option.reward.experienceReward : 0);
        int skillPointGranted = Mathf.Max(0, option.reward != null ? option.reward.skillPointReward : 0);

        if (experienceGranted > 0)
        {
            playerLevelSystem.AddExperience(experienceGranted);
        }

        if (skillPointGranted > 0)
        {
            playerLevelSystem.AddSkillPoint(skillPointGranted);
        }

        bool statOrbGranted = TryGrantStatOrb(option);
        hasSelectedReward = true;
        selectedOptionId = option.optionId;
        isOffered = false;

        return StoreAndRaiseResult(HWJ_StageChoiceRewardResult.Success(
            this,
            rewardData,
            option,
            experienceGranted,
            skillPointGranted,
            statOrbGranted,
            rewardClaimId,
            $"스테이지 선택 보상 지급 완료: {option.optionId}."));
    }

    private bool TryGrantStatOrb(HWJ_StageChoiceRewardOptionData option)
    {
        HWJ_StatOrbDataSO statOrb = ResolveStatOrb(option);

        if (statOrb == null)
        {
            return false;
        }

        return playerStatusSystem != null && playerStatusSystem.TryApplyStatOrb(statOrb);
    }

    private static HWJ_StatOrbDataSO ResolveDirectStatOrb(HWJ_StageChoiceRewardOptionData option)
    {
        return option != null ? option.directStatOrb : null;
    }

    private HWJ_StatOrbDataSO ResolveStatOrb(HWJ_StageChoiceRewardOptionData option)
    {
        HWJ_StatOrbDataSO statOrb = ResolveDirectStatOrb(option);

        if (statOrb == null
            && option != null
            && option.reward != null
            && !string.IsNullOrWhiteSpace(option.reward.statOrbId))
        {
            HWJ_GameAccess.TryGetStatOrb(option.reward.statOrbId, out statOrb);
        }

        return statOrb;
    }

    private HWJ_StageChoiceRewardResult StoreAndRaiseResult(HWJ_StageChoiceRewardResult result)
    {
        lastRewardMessage = result.Message;

        if (result.Succeeded)
        {
            HWJ_GameplayEvents.RaiseStageChoiceRewardSelected(
                new HWJ_StageChoiceRewardSelectedEvent(result));
        }

        return result;
    }

    private bool IsStageMatched()
    {
        if (rewardData == null || string.IsNullOrWhiteSpace(rewardData.StageId) || stageProgressionSystem == null)
        {
            return true;
        }

        return rewardData.StageId == stageProgressionSystem.StageId;
    }

    private string BuildRewardClaimId(string optionId)
    {
        string setId = rewardData != null && !string.IsNullOrWhiteSpace(rewardData.RewardSetId)
            ? rewardData.RewardSetId
            : "stage_choice_reward";
        string stageId = stageProgressionSystem != null && !string.IsNullOrWhiteSpace(stageProgressionSystem.StageId)
            ? stageProgressionSystem.StageId
            : rewardData != null ? rewardData.StageId : "unknown_stage";
        string normalizedOptionId = !string.IsNullOrWhiteSpace(optionId) ? optionId : "unknown_option";
        return $"stage_choice:{stageId}:{setId}:{normalizedOptionId}";
    }

    private void OnStageCleared(HWJ_StageProgressionEvent progressionEvent)
    {
        if (!offerOnStageCleared)
        {
            return;
        }

        if (stageProgressionSystem != null
            && progressionEvent.StageSystem != null
            && progressionEvent.StageSystem != stageProgressionSystem)
        {
            return;
        }

        TryOfferRewards();
    }

    private void ResolveReferences()
    {
        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = GetComponent<HWJ_StageProgressionSystem>();
        }

        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = FindFirstObjectByType<HWJ_StageProgressionSystem>(FindObjectsInactive.Include);
        }

        if (playerLevelSystem == null && HWJ_GameAccess.HasManager)
        {
            playerLevelSystem = HWJ_GameAccess.Manager.PlayerLevel;
        }

        if (playerStatusSystem == null && HWJ_GameAccess.HasManager)
        {
            playerStatusSystem = HWJ_GameAccess.Manager.PlayerStatus;
        }

        if (playerLevelSystem == null)
        {
            playerLevelSystem = FindFirstObjectByType<HWJ_LevelUpSystem>(FindObjectsInactive.Include);
        }

        if (playerStatusSystem == null)
        {
            playerStatusSystem = FindFirstObjectByType<HWJ_RuntimeStatusSystem>(FindObjectsInactive.Include);
        }
    }
}
