using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Professor demo HUD that reads HWJ runtime systems and displays the current playable loop.
/// The UI never changes gameplay data. It only observes player, possession, growth, and stage systems.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_DemoHudSystem : MonoBehaviour
{
    [Header("플레이어 참조")]
    [Tooltip("비워두면 HWJ_GameManager 또는 현재 씬에서 플레이어를 자동으로 찾습니다.")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [Tooltip("플레이어의 현재 HP와 정신력 값을 읽는 시스템입니다.")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [Tooltip("영혼/빙의/전환/사망 상태를 읽는 시스템입니다.")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [Tooltip("빙의한 몸의 정신력 소모량을 읽는 시스템입니다.")]
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;
    [Tooltip("현재 빙의한 몸과 사용 가능한 스킬을 읽는 시스템입니다.")]
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [Tooltip("레벨, 경험치, 스킬 포인트를 읽는 시스템입니다.")]
    [SerializeField] private HWJ_LevelUpSystem levelSystem;

    [Header("스테이지 참조")]
    [Tooltip("스테이지 흐름과 포탈 활성 조건을 읽는 시스템입니다.")]
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;
    [Tooltip("현재 맵의 남은 몬스터 수를 읽는 시스템입니다.")]
    [SerializeField] private HWJ_StageEnemyCountSystem stageEnemyCountSystem;

    [Header("막대 이미지")]
    [Tooltip("체력 또는 영혼 정신력 막대입니다.")]
    [SerializeField] private Image hpFillImage;
    [Tooltip("빙의한 몸의 남은 빙의 정신력 막대입니다.")]
    [SerializeField] private Image possessionFillImage;
    [Tooltip("현재 레벨의 경험치 진행도 막대입니다.")]
    [SerializeField] private Image experienceFillImage;

    [Header("텍스트")]
    [Tooltip("현재 플레이어 상태를 표시합니다.")]
    [SerializeField] private Text stateText;
    [Tooltip("HP, 영혼 정신력, 빙의 정신력 수치를 표시합니다.")]
    [SerializeField] private Text resourceText;
    [Tooltip("레벨, 경험치, 스킬 포인트를 표시합니다.")]
    [SerializeField] private Text growthText;
    [Tooltip("현재 스테이지 목표와 포탈 조건을 표시합니다.")]
    [SerializeField] private Text objectiveText;
    [Tooltip("현재 상태에서 가능한 행동을 표시합니다.")]
    [SerializeField] private Text actionText;
    [Tooltip("빙의 상태에서 사용 가능한 스킬 슬롯을 표시합니다.")]
    [SerializeField] private Text skillSlotText;

    [Header("색상")]
    [SerializeField] private Color soulColor = new Color(0.45f, 0.8f, 1f, 1f);
    [SerializeField] private Color possessedColor = new Color(0.75f, 0.25f, 1f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.78f, 0.28f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.28f, 0.22f, 1f);
    [SerializeField] private Color experienceColor = new Color(0.28f, 0.95f, 0.5f, 1f);

    [Header("갱신")]
    [Tooltip("참조가 비었거나 씬이 바뀐 경우 자동으로 다시 찾습니다.")]
    [SerializeField] private bool autoResolveReferences = true;
    [Tooltip("UI를 갱신하는 간격입니다. 너무 낮추면 매 프레임 문자열 생성이 늘어납니다.")]
    [SerializeField] private float refreshIntervalSeconds = 0.08f;

    private readonly StringBuilder textBuilder = new StringBuilder(256);
    private float nextRefreshTime;

    private void OnEnable()
    {
        ResolveReferences();
        RefreshHud();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.02f, refreshIntervalSeconds);

        if (autoResolveReferences)
        {
            ResolveReferences();
        }

        RefreshHud();
    }

    private void ResolveReferences()
    {
        if (playerResolver == null)
        {
            playerResolver = ResolvePlayerResolver();
        }

        if (playerResolver != null)
        {
            if (runtimeStatus == null)
            {
                runtimeStatus = playerResolver.GetComponent<HWJ_RuntimeStatusSystem>();
            }

            if (soulSystem == null)
            {
                soulSystem = playerResolver.GetComponent<HWJ_SoulSystem>();
            }

            if (bodyDecaySystem == null)
            {
                bodyDecaySystem = playerResolver.GetComponent<HWJ_BodyDecaySystem>();
            }

            if (possessionSystem == null)
            {
                possessionSystem = playerResolver.GetComponent<HWJ_PossessionSystem>();
            }

            if (levelSystem == null)
            {
                levelSystem = playerResolver.GetComponent<HWJ_LevelUpSystem>();
            }
        }

        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = FindFirstObjectByType<HWJ_StageProgressionSystem>();
        }

        if (stageEnemyCountSystem == null)
        {
            stageEnemyCountSystem = FindFirstObjectByType<HWJ_StageEnemyCountSystem>();
        }
    }

    private static HWJ_RootObjectDataResolver ResolvePlayerResolver()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver;
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

    private void RefreshHud()
    {
        RefreshStateAndResources();
        RefreshGrowth();
        RefreshObjective();
        RefreshActions();
        RefreshSkillSlots();
    }

    private void RefreshStateAndResources()
    {
        HWJ_SoulRuntimeState soulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
        float hpRatio = ResolveHpOrSpiritRatio(soulState);
        float possessionRatio = bodyDecaySystem != null ? bodyDecaySystem.RemainingPossessionMentalRatio : 0f;

        SetFill(hpFillImage, hpRatio, ResolvePrimaryBarColor(soulState, hpRatio));
        SetFill(possessionFillImage, possessionRatio, ResolvePossessionBarColor(possessionRatio));

        if (stateText != null)
        {
            stateText.text = ResolveStateLabel(soulState);
        }

        if (resourceText == null)
        {
            return;
        }

        textBuilder.Clear();

        if (runtimeStatus != null)
        {
            if (soulState == HWJ_SoulRuntimeState.Soul)
            {
                textBuilder.Append("영혼 정신력 ");
                AppendNumberPair(textBuilder, runtimeStatus.CurrentSpiritMentalValue, runtimeStatus.MaxSpiritMentalValue);
            }
            else
            {
                textBuilder.Append("육신 HP ");
                AppendNumberPair(textBuilder, runtimeStatus.CurrentHp, runtimeStatus.MaxHp);
            }
        }
        else
        {
            textBuilder.Append("플레이어 상태 대기 중");
        }

        if (bodyDecaySystem != null && bodyDecaySystem.MaxPossessionMentalValue > 0f)
        {
            textBuilder.Append("  |  빙의 정신력 ");
            AppendNumberPair(textBuilder, bodyDecaySystem.RemainingPossessionMentalValue, bodyDecaySystem.MaxPossessionMentalValue);
        }

        resourceText.text = textBuilder.ToString();
    }

    private float ResolveHpOrSpiritRatio(HWJ_SoulRuntimeState soulState)
    {
        if (runtimeStatus == null)
        {
            return 0f;
        }

        if (soulState == HWJ_SoulRuntimeState.Soul)
        {
            return runtimeStatus.SpiritMentalRatio;
        }

        return runtimeStatus.MaxHp > 0f
            ? Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp)
            : 0f;
    }

    private Color ResolvePrimaryBarColor(HWJ_SoulRuntimeState soulState, float ratio)
    {
        if (ratio <= 0.25f)
        {
            return dangerColor;
        }

        if (ratio <= 0.45f)
        {
            return warningColor;
        }

        return soulState == HWJ_SoulRuntimeState.Soul ? soulColor : possessedColor;
    }

    private Color ResolvePossessionBarColor(float ratio)
    {
        if (ratio <= 0.25f)
        {
            return dangerColor;
        }

        if (ratio <= 0.45f)
        {
            return warningColor;
        }

        return possessedColor;
    }

    private static string ResolveStateLabel(HWJ_SoulRuntimeState soulState)
    {
        switch (soulState)
        {
            case HWJ_SoulRuntimeState.Soul:
                return "영혼 상태";
            case HWJ_SoulRuntimeState.Body:
                return "빙의 상태";
            case HWJ_SoulRuntimeState.BodyToSoul:
                return "영혼 전환 중";
            case HWJ_SoulRuntimeState.Dead:
                return "사망";
            default:
                return "상태 확인 중";
        }
    }

    private void RefreshGrowth()
    {
        if (growthText == null)
        {
            return;
        }

        if (levelSystem == null)
        {
            growthText.text = "Lv.-  XP -  SP -";
            SetFill(experienceFillImage, 0f, experienceColor);
            return;
        }

        int requiredExperience = 0;
        bool hasRequiredExperience = levelSystem.TryGetRequiredExperienceForCurrentLevel(out requiredExperience);
        SetFill(experienceFillImage, levelSystem.CurrentExperienceRatio, experienceColor);
        growthText.text = hasRequiredExperience
            ? $"Lv.{levelSystem.CurrentLevel}  XP {levelSystem.CurrentExperience}/{requiredExperience}  SP {levelSystem.SkillPoint}"
            : $"Lv.{levelSystem.CurrentLevel}  XP {levelSystem.CurrentExperience}  SP {levelSystem.SkillPoint}";
    }

    private void RefreshObjective()
    {
        if (objectiveText == null)
        {
            return;
        }

        textBuilder.Clear();

        if (stageEnemyCountSystem != null)
        {
            if (stageEnemyCountSystem.TrackedEnemyCount > 0)
            {
                textBuilder.Append("목표: 몬스터 ");
                textBuilder.Append(stageEnemyCountSystem.DefeatedEnemyCount);
                textBuilder.Append("/");
                textBuilder.Append(stageEnemyCountSystem.TrackedEnemyCount);
                textBuilder.Append(" 처치");
            }
            else
            {
                textBuilder.Append("목표: 보스전 준비");
            }
        }
        else
        {
            textBuilder.Append("목표: 스테이지 진행");
        }

        if (stageProgressionSystem != null)
        {
            textBuilder.Append("  |  진행 ");
            textBuilder.Append(stageProgressionSystem.CurrentState);

            if (stageProgressionSystem.ObjectiveComplete)
            {
                textBuilder.Append("  |  포탈 사용 가능");
            }
        }

        objectiveText.text = textBuilder.ToString();
    }

    private void RefreshActions()
    {
        if (actionText == null)
        {
            return;
        }

        HWJ_SoulRuntimeState soulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;

        switch (soulState)
        {
            case HWJ_SoulRuntimeState.Soul:
                actionText.text = "가능 행동: 비행 / 벽 통과 / 영혼 통로 / E 빙의 / 영혼 구슬 작동";
                break;
            case HWJ_SoulRuntimeState.Body:
                actionText.text = "가능 행동: 이동 / 점프 / 더블 점프 / 대쉬 / 기본 공격 / 빙의체 스킬 / 직접 이탈";
                break;
            case HWJ_SoulRuntimeState.BodyToSoul:
                actionText.text = "가능 행동: 전환 연출 중 조작 잠금";
                break;
            case HWJ_SoulRuntimeState.Dead:
                actionText.text = "가능 행동: 없음";
                break;
            default:
                actionText.text = "가능 행동: 확인 중";
                break;
        }
    }

    private void RefreshSkillSlots()
    {
        if (skillSlotText == null)
        {
            return;
        }

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            skillSlotText.text = "스킬: 영혼 상태에서는 공격/스킬 사용 불가";
            return;
        }

        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            skillSlotText.text = "스킬: 빙의한 육신 없음";
            return;
        }

        textBuilder.Clear();
        textBuilder.Append("스킬: ");
        bool hasAnySkill = false;

        for (int i = 0; i < 4; i++)
        {
            if (i > 0)
            {
                textBuilder.Append("  /  ");
            }

            if (possessionSystem.TryGetCurrentPossessedSkillNodeAt(i, out HWJ_SkillNodeDataSO skillNode))
            {
                textBuilder.Append(i + 1);
                textBuilder.Append(".");
                textBuilder.Append(!string.IsNullOrWhiteSpace(skillNode.SkillDisplayName)
                    ? skillNode.SkillDisplayName
                    : skillNode.NodeId);
                hasAnySkill = true;
                continue;
            }

            if (possessionSystem.TryGetPossessedSkillIdAt(i, out string skillId))
            {
                textBuilder.Append(i + 1);
                textBuilder.Append(".");
                textBuilder.Append(skillId);
                hasAnySkill = true;
                continue;
            }

            textBuilder.Append(i + 1);
            textBuilder.Append(".잠김");
        }

        skillSlotText.text = hasAnySkill ? textBuilder.ToString() : "스킬: 해금된 빙의 스킬 없음";
    }

    private static void SetFill(Image fillImage, float ratio, Color color)
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(ratio);
        fillImage.color = color;
    }

    private static void AppendNumberPair(StringBuilder builder, float current, float max)
    {
        builder.Append(Mathf.CeilToInt(Mathf.Max(0f, current)));
        builder.Append("/");
        builder.Append(Mathf.CeilToInt(Mathf.Max(0f, max)));
    }
}
