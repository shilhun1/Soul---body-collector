using UnityEngine;

/// <summary>
/// 살아 있는 몬스터 빙의를 담당합니다.
/// 미니게임 진입 조건, 성공 시 정신력 10 차감, 시간 감소, 정신력 0 해제를 처리합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_LivePossessionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessionTargetValidator targetValidator;

    [Space(8f)]
    [Header("생체 빙의 정신력")]
    [Tooltip("생체 빙의 성공 시 대상 몬스터 정신력에서 차감하는 값입니다.")]
    [SerializeField, Min(0f)] private float mentalCostOnSuccess = 10f;

    [Tooltip("생체 빙의 중 정신력이 감소하는 간격입니다.")]
    [SerializeField, Min(0.01f)] private float mentalDrainInterval = 1f;

    [Tooltip("각 감소 간격마다 소모되는 대상 몬스터 정신력입니다.")]
    [SerializeField, Min(0f)] private float mentalDrainAmount = 1f;

    [Space(8f)]
    [Header("디버그")]
    [SerializeField] private float drainTimer;
    [SerializeField] private string runtimeMessage;

    public float MentalCostOnSuccess => Mathf.Max(0f, mentalCostOnSuccess);
    public string RuntimeMessage => runtimeMessage;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (possessionSystem == null
            || !possessionSystem.TryGetActiveLiveMentalState(
                out HWJ_LivePossessionMentalState mentalState))
        {
            drainTimer = 0f;
            return;
        }

        float interval = mentalState != null
            ? mentalState.MentalDrainInterval
            : Mathf.Max(0.01f, mentalDrainInterval);
        drainTimer += Time.deltaTime;

        while (drainTimer >= interval)
        {
            drainTimer -= interval;

            bool depleted = mentalState.ApplyMentalDrain(
                mentalState != null
                    ? mentalState.MentalDrainAmount
                    : Mathf.Max(0f, mentalDrainAmount));

            runtimeMessage =
                $"생체 빙의 정신력: {mentalState.CurrentMentalValue:0.##}/"
                + $"{mentalState.MaxMentalValue:0.##}";

            possessionSystem.SaveRuntimeSnapshot();

            if (!depleted)
            {
                continue;
            }

            mentalState.BlockPossessionPermanently();
            runtimeMessage = "대상 정신력이 0이 되어 강제 빙의 해제를 요청했습니다.";
            possessionSystem.ReleasePossessedBodyByMentalDepletion();
            drainTimer = 0f;
            return;
        }
    }

    public bool CanAttemptTarget(
        HWJ_RootObjectDataResolver targetDataResolver,
        out string message)
    {
        message = null;

        if (targetDataResolver == null)
        {
            message = "빙의 대상이 없습니다.";
            return false;
        }

        HWJ_LivePossessionMentalState mentalState =
            targetDataResolver.GetComponent<HWJ_LivePossessionMentalState>();

        if (mentalState == null)
        {
            message = "대상에게 HWJ_LivePossessionMentalState가 없습니다.";
            return false;
        }

        if (mentalState.IsPermanentlyBlocked)
        {
            message = "이 몬스터는 다시 빙의할 수 없습니다.";
            return false;
        }

        mentalState.LoadConfigurationFromTypeData();
        float possessionCost = mentalState.PossessionCostOnSuccess;

        if (!mentalState.CanAttemptLivePossession(possessionCost))
        {
            message =
                $"빙의 불가능: 대상 정신력이 {possessionCost:0.##} 이상이어야 합니다. "
                + $"현재 정신력: {mentalState.CurrentMentalValue:0.##}";
            return false;
        }

        message = "생체 빙의 가능";
        return true;
    }

    public bool CanStartMinigame(HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (possessionSystem == null || targetValidator == null)
        {
            return false;
        }

        HWJ_PossessionResult result = possessionSystem.EvaluatePossession(targetDataResolver);

        if (!result.Succeeded)
        {
            return false;
        }

        if (!targetValidator.TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody)
            || !targetValidator.IsLiveTarget(targetDataResolver, possessionBody))
        {
            possessionSystem.StoreFailure(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "대상이 살아 있는 빙의 가능 몬스터가 아닙니다.");
            return false;
        }

        runtimeMessage = "생체 빙의 미니게임을 시작할 수 있습니다.";
        possessionSystem.RecordLiveChallengeDebug(0f, 1f);
        return true;
    }

    public bool CompleteFromMinigame(HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (possessionSystem == null || targetValidator == null)
        {
            return false;
        }

        // 미니게임 동안 대상이 죽거나 상태가 바뀔 수 있으므로 다시 검증합니다.
        HWJ_PossessionResult result = possessionSystem.EvaluatePossession(targetDataResolver);

        if (!result.Succeeded)
        {
            return false;
        }

        if (!targetValidator.TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody)
            || !targetValidator.IsLiveTarget(targetDataResolver, possessionBody))
        {
            possessionSystem.StoreFailure(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "대상이 살아 있는 빙의 가능 몬스터가 아닙니다.");
            return false;
        }

        HWJ_LivePossessionMentalState mentalState =
            targetDataResolver.GetComponent<HWJ_LivePossessionMentalState>();

        if (mentalState == null)
        {
            possessionSystem.StoreFailure(
                HWJ_PossessionFailureCode.TargetUnavailable,
                "대상에게 HWJ_LivePossessionMentalState가 없습니다.");
            return false;
        }

        mentalState.LoadConfigurationFromTypeData();

        if (!mentalState.TrySpendPossessionCost(
                mentalState.PossessionCostOnSuccess,
                out string spendMessage))
        {
            possessionSystem.StoreFailure(
                HWJ_PossessionFailureCode.TargetUnavailable,
                spendMessage);
            return false;
        }

        runtimeMessage = spendMessage;
        possessionSystem.RecordLiveChallengeDebug(1f, 1f);

        bool possessed = possessionSystem.BeginPossession(
            targetDataResolver,
            possessionBody,
            HWJ_PossessionKind.Live,
            mentalState);

        // 성공 비용으로 정신력이 정확히 0이 된 경우에도 기획대로 즉시 해제하고 영구 차단합니다.
        if (possessed && mentalState.IsMentalDepleted)
        {
            possessionSystem.ReleasePossessedBodyByMentalDepletion();
        }

        return possessed;
    }

    public void ApplyMinigameFailure(HWJ_RootObjectDataResolver targetDataResolver)
    {
        ResolveReferences();

        if (possessionSystem == null
            || targetValidator == null
            || targetDataResolver == null
            || !targetValidator.TryGetPossessionBodyData(
                targetDataResolver,
                out HWJ_PossessionData possessionBody)
            || !targetValidator.IsLiveTarget(targetDataResolver, possessionBody))
        {
            return;
        }

        HWJ_RuntimeStatusSystem runtimeStatus = possessionSystem.RuntimeStatus;

        if (runtimeStatus != null)
        {
            runtimeStatus.TryApplySpiritMentalCost(
                Mathf.Max(0f, possessionBody.livePossessionFailureSpiritMentalCost),
                "live_possession_failed");

            runtimeStatus.LockControl(
                Mathf.Max(0f, possessionBody.livePossessionFailureControlLockSeconds));
        }

        ApplyFailureKnockback(targetDataResolver, possessionBody);

        runtimeMessage = string.IsNullOrWhiteSpace(possessionBody.livePossessionFailureMessage)
            ? "생체 빙의 미니게임에 실패했습니다."
            : possessionBody.livePossessionFailureMessage;

        possessionSystem.StoreFailure(
            HWJ_PossessionFailureCode.PossessionResisted,
            runtimeMessage);
        possessionSystem.RecordLiveChallengeDebug(0f, 1f);
    }

    private void ApplyFailureKnockback(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        float knockbackPower = Mathf.Max(
            0f,
            possessionBody.livePossessionFailureKnockbackPower);

        if (knockbackPower <= 0f)
        {
            return;
        }

        HWJ_KnockbackSystem knockbackSystem =
            GetComponent<HWJ_KnockbackSystem>();

        if (knockbackSystem == null)
        {
            knockbackSystem = gameObject.AddComponent<HWJ_KnockbackSystem>();
        }

        Vector3 rawDirection = transform.position - targetDataResolver.transform.position;
        Vector2 direction = rawDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        knockbackSystem.PlayKnockback(
            direction,
            knockbackPower,
            Mathf.Max(0f, possessionBody.livePossessionFailureKnockbackSeconds));
    }

    private void ResolveReferences()
    {
        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (targetValidator == null)
        {
            targetValidator = GetComponent<HWJ_PossessionTargetValidator>();
        }
    }
}
