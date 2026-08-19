using UnityEngine;

/// <summary>
/// 죽은 몬스터에게 E 입력으로 즉시 빙의하는 흐름을 담당합니다.
/// 시체 빙의는 미니게임과 몬스터 정신력 차감을 사용하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_CorpsePossessionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessionTargetValidator targetValidator;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public bool TryPossessCorpse(HWJ_RootObjectDataResolver targetDataResolver)
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
                out HWJ_PossessionData possessionBody))
        {
            possessionSystem.StoreFailure(
                HWJ_PossessionFailureCode.InvalidTarget,
                "시체 빙의 실패: 대상 빙의 데이터가 없습니다.");
            return false;
        }

        if (targetValidator.IsLiveTarget(targetDataResolver, possessionBody))
        {
            possessionSystem.StoreFailure(
                HWJ_PossessionFailureCode.PossessionBlocked,
                "살아 있는 몬스터는 R 미니게임 성공 후 빙의해야 합니다.");
            return false;
        }

        return possessionSystem.BeginPossession(
            targetDataResolver,
            possessionBody,
            HWJ_PossessionKind.Corpse,
            null);
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
