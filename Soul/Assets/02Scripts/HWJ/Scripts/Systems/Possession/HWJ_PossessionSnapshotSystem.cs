using UnityEngine;

/// <summary>
/// 저장 데이터에서 빙의체와 외형 스냅샷을 복원합니다.
/// 현재 저장 포맷에는 생체/시체 구분값이 없으므로 기본적으로 시체 빙의로 복원합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PossessionSnapshotSystem : MonoBehaviour
{
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessionTargetValidator targetValidator;
    [SerializeField] private HWJ_PossessionVisualController visualController;
    [SerializeField] private HWJ_RootObjectDataResolver runtimePossessedBodyResolver;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public bool RestorePossessedBody(
        HWJ_RootObjectDataSO rootObjectData,
        Sprite visualSprite,
        Color visualColor,
        bool visualFlipX,
        bool visualFlipY,
        RuntimeAnimatorController animatorController,
        bool hasVisualSnapshot,
        bool refreshStatus,
        bool refillToMax,
        bool saveSnapshot)
    {
        ResolveReferences();
        visualController?.CacheOwnerVisual();

        if (possessionSystem == null || rootObjectData == null)
        {
            possessionSystem?.StoreResultMessage(
                "Restore possession failed: missing root object data.");
            return false;
        }

        HWJ_RootObjectDataResolver restoredResolver =
            GetOrCreateRuntimePossessedBodyResolver();

        if (restoredResolver == null)
        {
            possessionSystem.StoreResultMessage(
                "Restore possession failed: missing runtime resolver.");
            return false;
        }

        restoredResolver.SetRootObjectData(rootObjectData);

        if (targetValidator == null
            || !targetValidator.TryGetPossessionBodyData(
                restoredResolver,
                out HWJ_PossessionData possessionData))
        {
            possessionSystem.ClearRegisteredPossessionState();
            possessionSystem.StoreResultMessage(
                $"Restore possession failed: {rootObjectData.name} has no body data.");
            return false;
        }

        possessionSystem.RegisterPossessionState(
            restoredResolver,
            possessionData,
            HWJ_PossessionKind.Corpse,
            null);
        possessionSystem.CreateRuntimeBodyState(
            restoredResolver,
            refillToMax,
            true);
        possessionSystem.SoulSystem?.EnterBodyState();

        if (hasVisualSnapshot)
        {
            visualController?.ApplySnapshot(
                visualSprite,
                visualColor,
                visualFlipX,
                visualFlipY,
                animatorController);
        }
        else
        {
            visualController?.ApplyModelData(rootObjectData);
        }

        if (refreshStatus
            && possessionData.loadsBodyStatsToPlayer
            && possessionSystem.RuntimeStatus != null)
        {
            possessionSystem.RuntimeStatus.RefreshCurrentHpFromData(refillToMax);
        }

        string resultMessage =
            $"Restored possessed body from {rootObjectData.name}.";
        possessionSystem.StoreResultMessage(resultMessage);

        HWJ_GameplayEvents.RaisePossessionChanged(
            new HWJ_PossessionEvent(
                possessionSystem,
                restoredResolver,
                true,
                resultMessage));

        if (saveSnapshot)
        {
            possessionSystem.SaveRuntimeSnapshot();
        }

        return true;
    }

    private HWJ_RootObjectDataResolver GetOrCreateRuntimePossessedBodyResolver()
    {
        if (runtimePossessedBodyResolver != null)
        {
            return runtimePossessedBodyResolver;
        }

        Transform runtimeBodyTransform =
            transform.Find("HWJ_RuntimePossessedBodyData");

        if (runtimeBodyTransform != null)
        {
            runtimePossessedBodyResolver =
                runtimeBodyTransform.GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimePossessedBodyResolver == null)
        {
            GameObject runtimeBody =
                new GameObject("HWJ_RuntimePossessedBodyData");
            runtimeBody.transform.SetParent(transform, false);
            runtimePossessedBodyResolver =
                runtimeBody.AddComponent<HWJ_RootObjectDataResolver>();
            runtimeBody.SetActive(false);
        }

        return runtimePossessedBodyResolver;
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

        if (visualController == null)
        {
            visualController = GetComponent<HWJ_PossessionVisualController>();
        }
    }
}
