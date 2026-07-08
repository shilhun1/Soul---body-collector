using UnityEngine;

/// <summary>
/// 런타임 HP, 육신 부패, 영혼 타이머를 하나의 바 표시로 갱신하는 기본 UI 시스템입니다.
/// UI Image 대신 Transform 스케일과 SpriteRenderer 색상만 사용해서 UI 방식이 바뀌어도 쉽게 교체할 수 있습니다.
/// </summary>
public class HWJ_HealthBarSystem : MonoBehaviour
{
    [SerializeField] private HWJ_HealthBarDataSO healthBarData;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private Transform fillRoot;
    [SerializeField] private SpriteRenderer fillRenderer;

    private Vector3 initialFillScale = Vector3.one;

    private void Awake()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponentInParent<HWJ_RuntimeStatusSystem>();
        }

        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = GetComponentInParent<HWJ_BodyDecaySystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponentInParent<HWJ_SoulSystem>();
        }

        if (dataResolver == null)
        {
            dataResolver = GetComponentInParent<HWJ_RootObjectDataResolver>();
        }

        if (fillRoot != null)
        {
            initialFillScale = fillRoot.localScale;
        }
    }

    private void Update()
    {
        float ratio = GetCurrentRatio();
        ApplyFill(ratio);
        ApplyColor();

        if (healthBarData != null && healthBarData.HideWhenDead && runtimeStatus != null && runtimeStatus.IsDead)
        {
            gameObject.SetActive(false);
        }
    }

    private float GetCurrentRatio()
    {
        if (soulSystem != null
            && soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
            && healthBarData != null
            && healthBarData.ShowSoulTimer)
        {
            return GetSoulTimerRatio();
        }

        if (soulSystem != null
            && soulSystem.CurrentState == HWJ_SoulRuntimeState.Body
            && bodyDecaySystem != null
            && bodyDecaySystem.IsDecaying
            && dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            if (playerData.BodyDecay == null)
            {
                return 0f;
            }

            float maxDecay = playerData.BodyDecay.maxDecayValue;
            return maxDecay > 0f ? Mathf.Clamp01(bodyDecaySystem.CurrentDecayValue / maxDecay) : 0f;
        }

        if (runtimeStatus != null && runtimeStatus.MaxHp > 0f)
        {
            return Mathf.Clamp01(runtimeStatus.CurrentHp / runtimeStatus.MaxHp);
        }

        return 0f;
    }

    private float GetSoulTimerRatio()
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return 0f;
        }

        float maxTime = playerData.SoulState.possessionDeadlineSeconds;
        return maxTime > 0f ? Mathf.Clamp01(soulSystem.SoulDeadlineTimer / maxTime) : 0f;
    }

    private void ApplyFill(float ratio)
    {
        if (fillRoot == null)
        {
            return;
        }

        fillRoot.localScale = new Vector3(initialFillScale.x * ratio, initialFillScale.y, initialFillScale.z);
    }

    private void ApplyColor()
    {
        if (healthBarData == null || fillRenderer == null)
        {
            return;
        }

        if (soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            fillRenderer.color = healthBarData.SoulStateColor;
        }
        else if (runtimeStatus != null && runtimeStatus.CurrentState == HWJ_RuntimeState.Possessed)
        {
            fillRenderer.color = healthBarData.PossessedStateColor;
        }
        else
        {
            fillRenderer.color = healthBarData.BodyStateColor;
        }
    }
}
