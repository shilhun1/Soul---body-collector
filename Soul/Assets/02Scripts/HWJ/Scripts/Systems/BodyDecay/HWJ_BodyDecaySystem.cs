using UnityEngine;

/// <summary>
/// 플레이어가 육신 상태일 때 부패 수치를 관리하는 컴포넌트입니다.
/// 플레이어 오브젝트에 붙이고 PlayerTypeDataSO.BodyDecay 값을 사용해 일정 시간마다 부패를 감소시킵니다.
/// </summary>
public class HWJ_BodyDecaySystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private float currentDecayValue;

    private float decayTimer;

    public float CurrentDecayValue => currentDecayValue;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            currentDecayValue = bodyDecay.maxDecayValue;
        }
    }

    /// <summary>
    /// 육신 상태일 때 부패 틱을 진행합니다.
    /// 부패 수치가 0 이하가 되면 SoulSystem을 통해 영혼 상태로 전환합니다.
    /// </summary>
    private void Update()
    {
        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            return;
        }

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay) || !bodyDecay.startDecayOnEnterBody)
        {
            return;
        }

        decayTimer += Time.deltaTime;

        if (decayTimer < bodyDecay.decayTickSeconds)
        {
            return;
        }

        decayTimer = 0f;
        currentDecayValue -= bodyDecay.decayAmountPerTick;
        TryEnterSoulStateWhenDecayEmpty(bodyDecay);
    }

    /// <summary>
    /// 현재 부패 수치를 데이터에 설정된 최대값으로 되돌립니다.
    /// 새로운 육신에 빙의했을 때 호출하는 용도로 사용합니다.
    /// </summary>
    public void ResetDecay()
    {
        if (TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            currentDecayValue = bodyDecay.maxDecayValue;
            decayTimer = 0f;
        }
    }

    /// <summary>
    /// 피격 시 부패 수치를 즉시 감소시킵니다.
    /// CombatSystem에서 계산된 피해 결과와 연결하면 피격 패널티로 사용할 수 있습니다.
    /// </summary>
    public void ApplyHitDecayPenalty()
    {
        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        currentDecayValue -= bodyDecay.hitDecayPenalty;
        TryEnterSoulStateWhenDecayEmpty(bodyDecay);
    }

    /// <summary>
    /// Resolver에 연결된 PlayerTypeDataSO에서 부패 데이터를 가져옵니다.
    /// 플레이어 데이터가 아니거나 부패 데이터가 없으면 false를 반환합니다.
    /// </summary>
    private bool TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay)
    {
        bodyDecay = null;

        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        bodyDecay = playerData.BodyDecay;
        return bodyDecay != null;
    }

    /// <summary>
    /// 부패 수치가 0 이하일 때 영혼 상태 전환 옵션이 켜져 있으면 SoulSystem.EnterSoulState를 호출합니다.
    /// </summary>
    private void TryEnterSoulStateWhenDecayEmpty(HWJ_BodyDecayData bodyDecay)
    {
        if (currentDecayValue > 0f || !bodyDecay.enterSoulStateWhenEmpty)
        {
            return;
        }

        currentDecayValue = 0f;
        soulSystem?.EnterSoulState();
    }
}
