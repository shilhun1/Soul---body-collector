using UnityEngine;

/// <summary>
/// 각 살아 있는 몬스터가 개별적으로 보유하는 정신력 런타임 상태입니다.
/// 현재 정신력이 10 이하이면 생체 빙의를 시도할 수 없습니다.
/// 정신력이 0이 되면 영구 재빙의 불가 상태가 됩니다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Systems/Live Possession Mental State")]
public class HWJ_LivePossessionMentalState : MonoBehaviour
{
    [Header("데이터 참조")]
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;

    [Header("몬스터 정신력")]
    [SerializeField, Min(0f)] private float maxMentalValue = 100f;
    [SerializeField, Min(0f)] private float currentMentalValue = 100f;
    [SerializeField] private bool initializeToMaxOnAwake = true;

    [Tooltip("생체 빙의 성공 1회당 대상 정신력에서 차감하는 값입니다.")]
    [SerializeField, Min(0f)] private float possessionCostOnSuccess = 10f;

    [Tooltip("생체 빙의 유지 중 정신력을 감소시키는 간격입니다.")]
    [SerializeField, Min(0.01f)] private float mentalDrainInterval = 1f;

    [Tooltip("각 감소 간격마다 차감하는 대상 정신력입니다.")]
    [SerializeField, Min(0f)] private float mentalDrainAmount = 1f;

    [Space(8f)]
    [Header("빙의 상태")]
    [SerializeField] private bool permanentlyBlocked;
    [SerializeField] private bool becameCorpseAfterLivePossession;
    [SerializeField] private bool corpsePossessionConsumed;

    [Space(8f)]
    [Header("Mental Change Log")]
    [Tooltip("Logs possession cost, periodic drain, and depletion to the Unity Console.")]
    [SerializeField] private bool logMentalChanges = true;

    public float MaxMentalValue => Mathf.Max(0f, maxMentalValue);
    public float CurrentMentalValue => Mathf.Clamp(currentMentalValue, 0f, MaxMentalValue);
    public float CurrentMentalRatio => MaxMentalValue > 0f
        ? Mathf.Clamp01(CurrentMentalValue / MaxMentalValue)
        : 0f;
    public bool IsPermanentlyBlocked => permanentlyBlocked;
    public bool IsMentalDepleted => CurrentMentalValue <= 0f;
    public bool BecameCorpseAfterLivePossession => becameCorpseAfterLivePossession;
    public float PossessionCostOnSuccess => Mathf.Max(0f, possessionCostOnSuccess);
    public float MentalDrainInterval => Mathf.Max(0.01f, mentalDrainInterval);
    public float MentalDrainAmount => Mathf.Max(0f, mentalDrainAmount);

    private void Awake()
    {
        LoadConfigurationFromTypeData();
        maxMentalValue = Mathf.Max(0f, maxMentalValue);

        if (initializeToMaxOnAwake)
        {
            currentMentalValue = maxMentalValue;
        }
        else
        {
            currentMentalValue = Mathf.Clamp(currentMentalValue, 0f, maxMentalValue);
        }

        if (currentMentalValue <= 0f)
        {
            permanentlyBlocked = true;
        }
    }

    private void OnValidate()
    {
        maxMentalValue = Mathf.Max(0f, maxMentalValue);
        currentMentalValue = Mathf.Clamp(currentMentalValue, 0f, maxMentalValue);
        possessionCostOnSuccess = Mathf.Max(0f, possessionCostOnSuccess);
        mentalDrainInterval = Mathf.Max(0.01f, mentalDrainInterval);
        mentalDrainAmount = Mathf.Max(0f, mentalDrainAmount);
    }

    public bool CanAttemptLivePossession(float possessionCost)
    {
        float cost = Mathf.Max(0f, possessionCost);

        return !permanentlyBlocked
            && !becameCorpseAfterLivePossession
            && CurrentMentalValue >= cost;
    }

    public bool TrySpendPossessionCost(
        float possessionCost,
        out string resultMessage)
    {
        float cost = Mathf.Max(0f, possessionCost);
        float before = CurrentMentalValue;

        if (permanentlyBlocked || becameCorpseAfterLivePossession)
        {
            resultMessage = "이 몬스터는 다시 생체 빙의할 수 없습니다.";
            return false;
        }

        if (CurrentMentalValue < cost)
        {
            resultMessage =
                $"빙의 불가능: 대상 정신력이 {cost:0.##} 이상이어야 합니다. "
                + $"현재 정신력: {CurrentMentalValue:0.##}";
            return false;
        }

        currentMentalValue = Mathf.Clamp(
            CurrentMentalValue - cost,
            0f,
            MaxMentalValue);

        if (currentMentalValue <= 0f)
        {
            permanentlyBlocked = true;
        }

        resultMessage =
            $"빙의 성공: 대상 정신력 {cost:0.##} 감소. "
            + $"남은 정신력: {CurrentMentalValue:0.##}";
        LogMental("SUCCESS_COST", before, CurrentMentalValue, cost);
        return true;
    }

    /// <summary>
    /// 몬스터 TypeData SO의 생체 빙의 설정을 런타임 상태에 복사합니다.
    /// SO는 고정 설정만 보관하고, 현재 정신력과 영구 차단 여부는 이 컴포넌트가 개별 몬스터별로 관리합니다.
    /// </summary>
    public void LoadConfigurationFromTypeData()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (dataResolver == null
            || !dataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData)
            || enemyData.PossessionBody == null)
        {
            return;
        }

        HWJ_PossessionData possessionData = enemyData.PossessionBody;
        maxMentalValue = Mathf.Max(0f, possessionData.livePossessionMaxMental);
        // SO의 최대치가 런타임 중 변경되어도 현재 정신력이 새 범위를 벗어나지 않게 보정합니다.
        currentMentalValue = Mathf.Clamp(currentMentalValue, 0f, maxMentalValue);
        possessionCostOnSuccess = Mathf.Max(
            0f,
            possessionData.livePossessionMentalCostOnSuccess);
        mentalDrainInterval = Mathf.Max(
            0.01f,
            possessionData.livePossessionMentalDrainInterval);
        mentalDrainAmount = Mathf.Max(
            0f,
            possessionData.livePossessionMentalDrainAmount);
    }

    /// <summary>
    /// 생체 빙의 유지 중 정신력을 감소시킵니다.
    /// 0이 되면 true를 반환하고 영구 재빙의 불가 상태로 전환합니다.
    /// </summary>
    public bool ApplyMentalDrain(float amount)
    {
        if (permanentlyBlocked)
        {
            return IsMentalDepleted;
        }

        if (amount <= 0f)
        {
            return false;
        }

        float before = CurrentMentalValue;

        currentMentalValue = Mathf.Clamp(
            CurrentMentalValue - amount,
            0f,
            MaxMentalValue);

        LogMental("DRAIN", before, CurrentMentalValue, amount);

        if (currentMentalValue > 0f)
        {
            return false;
        }

        BlockPossessionPermanently();
        LogMentalWarning(
            "DEPLETED",
            before,
            CurrentMentalValue,
            amount,
            "Possessed body mental reached zero; possession will be released.");
        return true;
    }

    public void BlockPossessionPermanently()
    {
        currentMentalValue = 0f;
        permanentlyBlocked = true;
    }

    /// <summary>
    /// 생체 빙의체 HP가 0이 되어 시체가 되었음을 기록합니다.
    /// 이후 E 시체 빙의를 한 번 허용할 수 있습니다.
    /// </summary>
    public void MarkBecameCorpseAfterLivePossession()
    {
        currentMentalValue = 0f;
        permanentlyBlocked = true;
        becameCorpseAfterLivePossession = true;
        corpsePossessionConsumed = false;
    }

    public bool CanPossessAsCorpse()
    {
        return becameCorpseAfterLivePossession
            && !corpsePossessionConsumed;
    }

    public void MarkCorpsePossessionConsumed()
    {
        if (becameCorpseAfterLivePossession)
        {
            corpsePossessionConsumed = true;
        }
    }

    /// <summary>
    /// 저장 시스템에서 정신력 상태를 복원할 때 사용할 수 있는 진입점입니다.
    /// </summary>
    public void RestoreRuntimeState(
        float restoredMentalValue,
        bool restoredPermanentlyBlocked,
        bool restoredAsCorpse,
        bool restoredCorpseConsumed)
    {
        currentMentalValue = Mathf.Clamp(restoredMentalValue, 0f, MaxMentalValue);
        permanentlyBlocked = restoredPermanentlyBlocked || currentMentalValue <= 0f;
        becameCorpseAfterLivePossession = restoredAsCorpse;
        corpsePossessionConsumed = restoredCorpseConsumed;
    }

    private void LogMental(string stage, float before, float after, float amount)
    {
        if (logMentalChanges)
        {
            Debug.Log(
                $"[HWJ][PossessionMental][{stage}] target={name}, " +
                $"mental={before:0.##}->{after:0.##}/{MaxMentalValue:0.##}, amount={amount:0.##}",
                this);
        }
    }

    private void LogMentalWarning(
        string stage,
        float before,
        float after,
        float amount,
        string message)
    {
        if (logMentalChanges)
        {
            Debug.LogWarning(
                $"[HWJ][PossessionMental][{stage}] target={name}, " +
                $"mental={before:0.##}->{after:0.##}/{MaxMentalValue:0.##}, " +
                $"amount={amount:0.##}, message={message}",
                this);
        }
    }
}
