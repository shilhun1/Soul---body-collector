using System;
using UnityEngine;

[Serializable]
public class HWJ_StageChoiceRewardOptionData
{
    [Header("선택지 식별")]
    [Tooltip("저장과 중복 지급 방지에 사용하는 고정 ID입니다.")]
    [InspectorName("선택지 ID")]
    public string optionId;

    [Tooltip("UI에 표시할 선택지 이름입니다.")]
    [InspectorName("표시 이름")]
    public string displayName;

    [TextArea]
    [Tooltip("UI에 표시할 선택지 설명입니다.")]
    [InspectorName("설명")]
    public string description;

    [Header("지급 보상")]
    [Tooltip("경험치, 스킬 포인트, 스탯 구슬 ID를 담는 기존 보상 데이터입니다.")]
    [InspectorName("보상 데이터")]
    public HWJ_RewardData reward = new HWJ_RewardData();

    [Tooltip("직접 지급할 스탯 구슬 SO입니다. 비워두면 보상 데이터의 StatOrb ID를 사용합니다.")]
    [InspectorName("직접 스탯 구슬")]
    public HWJ_StatOrbDataSO directStatOrb;
}

[CreateAssetMenu(fileName = "HWJ_StageChoiceRewardData", menuName = "HWJ/Data/System/Stage Choice Reward")]
public class HWJ_StageChoiceRewardDataSO : ScriptableObject
{
    [Header("보상 묶음 식별")]
    [Tooltip("저장과 중복 지급 방지에 사용하는 고정 ID입니다.")]
    [InspectorName("보상 묶음 ID")]
    [SerializeField] private string rewardSetId = "stage_choice_reward";

    [Tooltip("이 보상을 적용할 스테이지 ID입니다. 비워두면 연결된 스테이지 진행 시스템의 현재 스테이지에 적용합니다.")]
    [InspectorName("스테이지 ID")]
    [SerializeField] private string stageId;

    [Tooltip("UI에 표시할 제목입니다.")]
    [InspectorName("표시 제목")]
    [SerializeField] private string displayTitle = "스테이지 보상 선택";

    [Header("선택지")]
    [Tooltip("플레이어가 선택할 수 있는 보상 후보 목록입니다.")]
    [InspectorName("보상 선택지")]
    [SerializeField] private HWJ_StageChoiceRewardOptionData[] options;

    public string RewardSetId => rewardSetId;
    public string StageId => stageId;
    public string DisplayTitle => displayTitle;
    public HWJ_StageChoiceRewardOptionData[] Options => options;

    public bool TryGetOption(string optionId, out HWJ_StageChoiceRewardOptionData option)
    {
        option = null;

        if (string.IsNullOrWhiteSpace(optionId) || options == null)
        {
            return false;
        }

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].optionId == optionId)
            {
                option = options[i];
                return true;
            }
        }

        return false;
    }

    public bool TryGetOptionAt(int index, out HWJ_StageChoiceRewardOptionData option)
    {
        option = null;

        if (options == null || index < 0 || index >= options.Length)
        {
            return false;
        }

        option = options[index];
        return option != null;
    }
}
