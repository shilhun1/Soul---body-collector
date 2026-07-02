using UnityEngine;

public class HWJ_BodyDecaySystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private float currentDecayValue;

    public float CurrentDecayValue => currentDecayValue;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (dataResolver != null && dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            currentDecayValue = playerData.BodyDecay.maxDecayValue;
        }
    }

    public void ApplyHitDecayPenalty()
    {
        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return;
        }

        currentDecayValue -= playerData.BodyDecay.hitDecayPenalty;
    }
}
