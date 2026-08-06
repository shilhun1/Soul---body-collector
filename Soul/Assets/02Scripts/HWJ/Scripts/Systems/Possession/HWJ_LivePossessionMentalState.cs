// HWJ_LivePossessionMentalState.cs

using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_LivePossessionMentalState : MonoBehaviour
{
    [Header("몬스터 정신력")]
    [SerializeField, Min(0f)] private float maxMentalValue = 100f;
    [SerializeField, Min(0f)] private float currentMentalValue = 100f;
    [SerializeField] private bool initializeToMaxOnAwake = true;

    [Header("빙의 상태")]
    [SerializeField] private bool permanentlyBlocked;
    [SerializeField] private bool becameCorpseAfterLivePossession;
    [SerializeField] private bool corpsePossessionConsumed;

    public float MaxMentalValue => Mathf.Max(0f, maxMentalValue);

    public float CurrentMentalValue =>
        Mathf.Clamp(currentMentalValue, 0f, MaxMentalValue);

    public bool IsPermanentlyBlocked => permanentlyBlocked;

    public bool IsMentalDepleted => CurrentMentalValue <= 0f;

    public bool CanAttemptLivePossession(float possessionCost)
    {
        float cost = Mathf.Max(0f, possessionCost);

        return !permanentlyBlocked
            && CurrentMentalValue > cost;
    }

    public bool TrySpendPossessionCost(
        float possessionCost,
        out string resultMessage)
    {
        float cost = Mathf.Max(0f, possessionCost);

        if (permanentlyBlocked)
        {
            resultMessage = "이 몬스터는 다시 빙의할 수 없습니다.";
            return false;
        }

        // 정신력이 10 이하라면 빙의 불가능
        if (CurrentMentalValue <= cost)
        {
            resultMessage =
                $"빙의 불가능: 대상 정신력이 {cost:0.##}을 초과해야 합니다. " +
                $"현재 정신력: {CurrentMentalValue:0.##}";

            return false;
        }

        currentMentalValue =
            Mathf.Clamp(CurrentMentalValue - cost, 0f, MaxMentalValue);

        resultMessage =
            $"빙의 성공: 대상 정신력 {cost:0.##} 감소. " +
            $"남은 정신력: {CurrentMentalValue:0.##}";

        return true;
    }

    public bool ApplyMentalDrain(float amount)
    {
        if (permanentlyBlocked)
        {
            return true;
        }

        if (amount <= 0f)
        {
            return false;
        }

        currentMentalValue =
            Mathf.Clamp(CurrentMentalValue - amount, 0f, MaxMentalValue);

        if (currentMentalValue > 0f)
        {
            return false;
        }

        BlockPossessionPermanently();
        return true;
    }

    public void BlockPossessionPermanently()
    {
        currentMentalValue = 0f;
        permanentlyBlocked = true;
    }

    public void MarkBecameCorpseAfterLivePossession()
    {
        becameCorpseAfterLivePossession = true;
        corpsePossessionConsumed = false;
        permanentlyBlocked = true;
    }

    public bool CanPossessAsCorpse()
    {
        return becameCorpseAfterLivePossession
            && !corpsePossessionConsumed;
    }

    public void MarkCorpsePossessionConsumed()
    {
        if (!becameCorpseAfterLivePossession)
        {
            return;
        }

        corpsePossessionConsumed = true;
    }

    private void Awake()
    {
        maxMentalValue = Mathf.Max(0f, maxMentalValue);

        if (initializeToMaxOnAwake)
        {
            currentMentalValue = maxMentalValue;
        }
        else
        {
            currentMentalValue =
                Mathf.Clamp(currentMentalValue, 0f, maxMentalValue);
        }

        if (currentMentalValue <= 0f)
        {
            permanentlyBlocked = true;
        }
    }

    private void OnValidate()
    {
        maxMentalValue = Mathf.Max(0f, maxMentalValue);

        currentMentalValue =
            Mathf.Clamp(currentMentalValue, 0f, maxMentalValue);
    }
}