using UnityEngine;

/// <summary>
/// 범용 보스 체력바 표시 시스템입니다.
/// 특정 보스 타입에 묶이지 않고 RuntimeStatus와 BossBrain의 현재 페이즈만 읽어서 표시합니다.
/// </summary>
public class HWJ_BossHealthBarSystem : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("보스의 현재 HP와 최대 HP를 읽는 런타임 상태 시스템입니다.")]
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [Tooltip("보스의 현재 FSM 상태와 페이즈 번호를 읽는 보스 상태 시스템입니다.")]
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [Tooltip("보스 이름을 RootObjectData에서 가져오기 위한 데이터 해석기입니다.")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [Tooltip("체력바 배경 선입니다.")]
    [SerializeField] private LineRenderer backgroundRenderer;
    [Tooltip("현재 체력 비율을 표시하는 선입니다.")]
    [SerializeField] private LineRenderer fillRenderer;
    [Tooltip("보스 이름을 표시하는 TextMesh입니다.")]
    [SerializeField] private TextMesh bossNameText;
    [Tooltip("현재 페이즈를 표시하는 TextMesh입니다.")]
    [SerializeField] private TextMesh phaseText;

    [Header("표시")]
    [Tooltip("체력바의 전체 가로 길이입니다.")]
    [SerializeField] private float barWidth = 4f;
    [Tooltip("1페이즈에서 사용할 체력바 색상입니다.")]
    [SerializeField] private Color phaseOneColor = new Color(0.75f, 0.08f, 0.08f, 1f);
    [Tooltip("2페이즈 이상에서 사용할 체력바 색상입니다.")]
    [SerializeField] private Color phaseTwoColor = new Color(0.55f, 0.16f, 0.85f, 1f);
    [Tooltip("2페이즈 이후 이름을 다르게 보여주고 싶을 때만 입력합니다. 비우면 RootObjectData 이름을 그대로 씁니다.")]
    [SerializeField] private string phaseTwoDisplayNameOverride;
    [Tooltip("페이즈 전환 연출 중 체력바를 숨길지 정합니다.")]
    [SerializeField] private bool hideDuringPhaseTransition = true;
    [Tooltip("보스 사망 후 체력바를 숨길지 정합니다.")]
    [SerializeField] private bool hideWhenDead = true;

    public float DisplayedRatio { get; private set; }
    public string DisplayedPhaseLabel { get; private set; }
    public bool IsBarVisible => fillRenderer != null && fillRenderer.enabled;

    private void Awake()
    {
        CacheReferences();
        RefreshDisplay();
    }

    private void Update()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        CacheReferences();

        bool visible = ShouldShowBar();
        SetRendererVisible(backgroundRenderer, visible);
        SetRendererVisible(fillRenderer, visible);

        if (bossNameText != null)
        {
            bossNameText.gameObject.SetActive(visible);
        }

        if (phaseText != null)
        {
            phaseText.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            DisplayedRatio = 0f;
            DisplayedPhaseLabel = ResolveHiddenLabel();
            return;
        }

        DisplayedRatio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
            ? Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp)
            : 0f;
        DisplayedPhaseLabel = ResolvePhaseLabel();

        ApplyFillRenderer();

        if (phaseText != null)
        {
            phaseText.text = DisplayedPhaseLabel;
        }

        if (bossNameText != null)
        {
            bossNameText.text = ResolveBossName();
        }
    }

    private bool ShouldShowBar()
    {
        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            return !hideWhenDead;
        }

        if (bossBrain == null)
        {
            return true;
        }

        if (hideWhenDead && bossBrain.CurrentState == HWJ_BossFSMState.Dead)
        {
            return false;
        }

        return !hideDuringPhaseTransition || bossBrain.CurrentState != HWJ_BossFSMState.PhaseTransition;
    }

    private string ResolveHiddenLabel()
    {
        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            return "Dead";
        }

        return bossBrain != null && bossBrain.CurrentState == HWJ_BossFSMState.PhaseTransition
            ? "Transition"
            : "Hidden";
    }

    private string ResolvePhaseLabel()
    {
        int phaseNumber = bossBrain != null ? Mathf.Max(1, bossBrain.CurrentPhaseNumber) : 1;
        return phaseNumber >= 2 ? $"Phase {phaseNumber}" : "Phase 1";
    }

    private string ResolveBossName()
    {
        int phaseNumber = bossBrain != null ? Mathf.Max(1, bossBrain.CurrentPhaseNumber) : 1;

        if (phaseNumber >= 2 && !string.IsNullOrEmpty(phaseTwoDisplayNameOverride))
        {
            return phaseTwoDisplayNameOverride;
        }

        if (dataResolver != null
            && dataResolver.Identity != null
            && !string.IsNullOrEmpty(dataResolver.Identity.displayName))
        {
            return dataResolver.Identity.displayName;
        }

        return "Boss";
    }

    private void ApplyFillRenderer()
    {
        if (fillRenderer == null)
        {
            return;
        }

        float halfWidth = Mathf.Max(0.1f, barWidth) * 0.5f;
        float fillEnd = Mathf.Lerp(-halfWidth, halfWidth, DisplayedRatio);
        fillRenderer.positionCount = 2;
        fillRenderer.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
        fillRenderer.SetPosition(1, new Vector3(fillEnd, 0f, 0f));

        Color color = bossBrain != null && bossBrain.CurrentPhaseNumber >= 2
            ? phaseTwoColor
            : phaseOneColor;
        fillRenderer.startColor = color;
        fillRenderer.endColor = color;
    }

    private static void SetRendererVisible(Renderer renderer, bool visible)
    {
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void CacheReferences()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponentInParent<HWJ_RuntimeStatusSystem>();
        }

        if (bossBrain == null)
        {
            bossBrain = GetComponentInParent<HWJ_BossBrainSystem>();
        }

        if (dataResolver == null)
        {
            dataResolver = GetComponentInParent<HWJ_RootObjectDataResolver>();
        }
    }
}
