using UnityEngine;

/// <summary>
/// HWJ 피해 이벤트를 받아 최종보스 본체 피해를 보호막 체력으로 대신 처리합니다.
/// </summary>
public class hys_FinalBossShield : MonoBehaviour
{
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private SpriteRenderer bossRenderer;
    [SerializeField] private Color shieldColor = new Color(0.45f, 0.08f, 0.75f, 0.7f);
    [SerializeField, Min(0.01f)] private float maxHpRatio = 0.05f;
    [SerializeField] private bool isActive;
    [SerializeField] private float currentShieldHp;
    [SerializeField] private float maxShieldHp;

    private GameObject visual;
    private bool handlingDamage;
    private float previousObservedHp;

    public bool IsActive => isActive;
    public float CurrentShieldHp => currentShieldHp;
    public float MaxShieldHp => maxShieldHp;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        previousObservedHp = runtimeStatus != null ? runtimeStatus.CurrentHp : 0f;
    }

    private void OnDisable()
    {
        RemoveVisual();
    }

    private void Update()
    {
        ObserveDamage();
    }

    public bool ActivateShield()
    {
        return ActivateShield(maxHpRatio);
    }

    public bool ActivateShield(float shieldHpRatio)
    {
        CacheReferences();
        if (runtimeStatus == null || runtimeStatus.IsDead) return false;

        // 페이즈별 기획 수치에 따라 최대 체력 비례 보호막 양을 바꿉니다.
        maxShieldHp = Mathf.Max(1f, runtimeStatus.MaxHp * Mathf.Max(0.01f, shieldHpRatio));
        currentShieldHp = maxShieldHp;
        isActive = true;
        RemoveVisual();
        visual = hys_FinalBossMagicVisual.SpawnRing(
            transform,
            2.8f,
            shieldColor,
            -1f,
            "hys_FinalBossShieldVisual");
        return true;
    }

    public void BreakShield()
    {
        currentShieldHp = 0f;
        isActive = false;
        RemoveVisual();
    }

    private void ObserveDamage()
    {
        if (runtimeStatus == null) return;

        float currentHp = runtimeStatus.CurrentHp;
        float receivedDamage = previousObservedHp - currentHp;
        previousObservedHp = currentHp;
        if (!isActive || handlingDamage || receivedDamage <= 0f) return;

        handlingDamage = true;
        // HWJ 이벤트가 없는 이전 브랜치에서도 작동하도록 실제 HP 감소량을 보호막으로 흡수합니다.
        float absorbed = Mathf.Min(currentShieldHp, receivedDamage);
        float restoredHp = Mathf.Min(runtimeStatus.MaxHp, runtimeStatus.CurrentHp + absorbed);
        runtimeStatus.RestoreHpSnapshot(restoredHp, restoredHp, restoredHp);
        if (restoredHp > 0f && runtimeStatus.IsDead) runtimeStatus.SetState(HWJ_RuntimeState.Idle);

        currentShieldHp = Mathf.Max(0f, currentShieldHp - absorbed);
        if (currentShieldHp <= 0f) BreakShield();
        previousObservedHp = runtimeStatus.CurrentHp;
        handlingDamage = false;
    }

#if HYS_USE_HWJ_GAMEPLAY_EVENTS
    private void HandleDamageApplied(HWJ_DamageEvent damageEvent)
    {
        if (!isActive || handlingDamage || runtimeStatus == null
            || damageEvent.TargetStatus != runtimeStatus || damageEvent.Damage <= 0f)
        {
            return;
        }

        handlingDamage = true;
        float absorbed = Mathf.Min(currentShieldHp, damageEvent.Damage);
        float restoredHp = Mathf.Min(runtimeStatus.MaxHp, runtimeStatus.CurrentHp + absorbed);

        // 피해가 먼저 적용되는 HWJ 구조이므로 같은 프레임에 흡수량만큼 본체 체력을 복구합니다.
        runtimeStatus.RestoreHpSnapshot(restoredHp, restoredHp, restoredHp);
        if (restoredHp > 0f && runtimeStatus.IsDead)
            runtimeStatus.SetState(HWJ_RuntimeState.Idle);

        currentShieldHp = Mathf.Max(0f, currentShieldHp - absorbed);
        if (currentShieldHp <= 0f) BreakShield();
        handlingDamage = false;
    }

#endif

    private void RemoveVisual()
    {
        if (visual != null) Destroy(visual);
        visual = null;
    }

    private void CacheReferences()
    {
        if (runtimeStatus == null) runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        if (bossRenderer == null) bossRenderer = GetComponentInChildren<SpriteRenderer>();
    }
}
