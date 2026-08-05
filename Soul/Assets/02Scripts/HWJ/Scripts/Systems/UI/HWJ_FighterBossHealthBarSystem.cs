using UnityEngine;

/// <summary>
/// 같은 보스 체력 바를 Phase1과 Phase2에서 다시 사용합니다.
/// Transition 중에는 숨기고, Phase2 진입 시 완전히 회복된 RuntimeStatus HP를 다시 표시합니다.
/// </summary>
public class HWJ_FighterBossHealthBarSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private LineRenderer backgroundRenderer;
    [SerializeField] private LineRenderer fillRenderer;
    [SerializeField] private TextMesh bossNameText;
    [SerializeField] private TextMesh phaseText;
    [SerializeField] private float barWidth = 4f;
    [SerializeField] private Color phaseOneColor = new Color(0.75f, 0.08f, 0.08f, 1f);
    [SerializeField] private Color phaseTwoColor = new Color(0.55f, 0.16f, 0.85f, 1f);
    [SerializeField] private string phaseTwoBossName = "흑뢰의 마투사";

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

        HWJ_FighterBossPhase phase = bossBrain != null
            ? bossBrain.FighterPhase
            : HWJ_FighterBossPhase.Phase1;
        bool visible = phase != HWJ_FighterBossPhase.Transition
            && phase != HWJ_FighterBossPhase.Dead;

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
            DisplayedPhaseLabel = phase == HWJ_FighterBossPhase.Transition ? "Transition" : "Dead";
            return;
        }

        DisplayedRatio = runtimeStatus != null && runtimeStatus.MaxHp > 0f
            ? Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp)
            : 0f;
        DisplayedPhaseLabel = phase == HWJ_FighterBossPhase.Phase2 ? "Phase II" : "Phase I";

        if (fillRenderer != null)
        {
            float halfWidth = Mathf.Max(0.1f, barWidth) * 0.5f;
            float fillEnd = Mathf.Lerp(-halfWidth, halfWidth, DisplayedRatio);
            fillRenderer.positionCount = 2;
            fillRenderer.SetPosition(0, new Vector3(-halfWidth, 0f, 0f));
            fillRenderer.SetPosition(1, new Vector3(fillEnd, 0f, 0f));
            Color color = phase == HWJ_FighterBossPhase.Phase2 ? phaseTwoColor : phaseOneColor;
            fillRenderer.startColor = color;
            fillRenderer.endColor = color;
        }

        if (phaseText != null)
        {
            phaseText.text = DisplayedPhaseLabel;
        }

        if (bossNameText != null)
        {
            bossNameText.text = phase == HWJ_FighterBossPhase.Phase2
                ? phaseTwoBossName
                : ResolvePhaseOneBossName();
        }
    }

    private string ResolvePhaseOneBossName()
    {
        if (dataResolver != null
            && dataResolver.Identity != null
            && !string.IsNullOrEmpty(dataResolver.Identity.displayName))
        {
            return dataResolver.Identity.displayName;
        }

        return "Fighter Boss";
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
