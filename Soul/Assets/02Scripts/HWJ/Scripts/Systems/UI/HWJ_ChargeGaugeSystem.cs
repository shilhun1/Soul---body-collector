using UnityEngine;

/// <summary>
/// 플레이어 기본 공격 차지 시간을 Transform 스케일로 표시하는 선택형 UI 시스템입니다.
/// 실제 UI 프리팹은 fillRoot와 visibleRoot만 연결하면 되고, 공격 로직은 직접 수정하지 않습니다.
/// </summary>
public class HWJ_ChargeGaugeSystem : MonoBehaviour
{
    [SerializeField] private HWJ_PlayerAttackSystem playerAttackSystem;
    [SerializeField] private Transform visibleRoot;
    [SerializeField] private Transform fillRoot;
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private bool hideWhenNotCharging = true;
    [SerializeField] private Color chargingColor = new Color(0.55f, 0.25f, 1f, 1f);
    [SerializeField] private Color fullyChargedColor = new Color(1f, 0.95f, 0.35f, 1f);

    private Vector3 initialFillScale = Vector3.one;

    public float CurrentRatio => playerAttackSystem != null ? playerAttackSystem.CurrentBasicAttackChargeRatio : 0f;

    private void Awake()
    {
        if (playerAttackSystem == null)
        {
            playerAttackSystem = GetComponentInParent<HWJ_PlayerAttackSystem>();
        }

        if (fillRoot != null)
        {
            initialFillScale = fillRoot.localScale;
        }
    }

    private void OnEnable()
    {
        HWJ_GameplayEvents.BasicAttackChargeChanged += OnBasicAttackChargeChanged;
        RefreshGauge();
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.BasicAttackChargeChanged -= OnBasicAttackChargeChanged;
    }

    private void Update()
    {
        RefreshGauge();
    }

    private void OnBasicAttackChargeChanged(HWJ_BasicAttackChargeEvent chargeEvent)
    {
        if (playerAttackSystem != null && chargeEvent.AttackSystem != playerAttackSystem)
        {
            return;
        }

        RefreshGauge();
    }

    private void RefreshGauge()
    {
        bool isCharging = playerAttackSystem != null && playerAttackSystem.IsChargingBasicAttack;
        float ratio = playerAttackSystem != null ? playerAttackSystem.CurrentBasicAttackChargeRatio : 0f;

        if (visibleRoot != null && hideWhenNotCharging)
        {
            visibleRoot.gameObject.SetActive(isCharging);
        }

        if (fillRoot != null)
        {
            fillRoot.localScale = new Vector3(initialFillScale.x * ratio, initialFillScale.y, initialFillScale.z);
        }

        if (fillRenderer != null)
        {
            fillRenderer.color = ratio >= 1f ? fullyChargedColor : chargingColor;
        }
    }
}
