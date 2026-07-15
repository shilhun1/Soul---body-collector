using System.Text;
using UnityEngine;

public enum HWJ_SaveIdentityValidationFailureCode
{
    None,
    MissingResolver,
    MissingIdentityComponent,
    MissingStableInstanceId,
    MissingRootObjectId
}

public readonly struct HWJ_SaveIdentityValidationResult
{
    public readonly bool Succeeded;
    public readonly HWJ_SaveIdentityValidationFailureCode FailureCode;
    public readonly HWJ_RuntimeSaveIdentity IdentityComponent;
    public readonly HWJ_RootObjectDataResolver ResolverComponent;
    public readonly string StableInstanceId;
    public readonly string RootObjectId;
    public readonly string RewardClaimId;
    public readonly string Message;

    private HWJ_SaveIdentityValidationResult(
        bool succeeded,
        HWJ_SaveIdentityValidationFailureCode failureCode,
        HWJ_RuntimeSaveIdentity identityComponent,
        HWJ_RootObjectDataResolver resolverComponent,
        string stableInstanceId,
        string rootObjectId,
        string rewardClaimId,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        IdentityComponent = identityComponent;
        ResolverComponent = resolverComponent;
        StableInstanceId = stableInstanceId;
        RootObjectId = rootObjectId;
        RewardClaimId = rewardClaimId;
        Message = message;
    }

    public static HWJ_SaveIdentityValidationResult Success(
        HWJ_RuntimeSaveIdentity identityComponent,
        HWJ_RootObjectDataResolver resolverComponent,
        string stableInstanceId,
        string rootObjectId,
        string rewardClaimId)
    {
        return new HWJ_SaveIdentityValidationResult(
            true,
            HWJ_SaveIdentityValidationFailureCode.None,
            identityComponent,
            resolverComponent,
            stableInstanceId,
            rootObjectId,
            rewardClaimId,
            "Save identity is valid.");
    }

    public static HWJ_SaveIdentityValidationResult Fail(
        HWJ_SaveIdentityValidationFailureCode failureCode,
        string message,
        HWJ_RuntimeSaveIdentity identityComponent = null,
        HWJ_RootObjectDataResolver resolverComponent = null,
        string stableInstanceId = null,
        string rootObjectId = null)
    {
        return new HWJ_SaveIdentityValidationResult(
            false,
            failureCode,
            identityComponent,
            resolverComponent,
            stableInstanceId,
            rootObjectId,
            null,
            message);
    }
}

/// <summary>
/// 런타임 오브젝트를 저장 데이터에서 다시 식별하기 위한 안정 ID 컴포넌트입니다.
/// 스폰 시스템은 SpawnId와 순번으로 자동 할당하고, 씬에 직접 배치한 오브젝트는 Inspector에서 수동 ID를 지정합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_RuntimeSaveIdentity : MonoBehaviour
{
    [SerializeField] private string stableInstanceId;
    [SerializeField] private string sourceSpawnId;
    [SerializeField] private string sourceSpawnPointId;
    [SerializeField] private int sourceSpawnIndex = -1;
    [SerializeField] private string rootObjectId;
    [SerializeField] private bool assignedBySpawner;

    public string StableInstanceId => stableInstanceId;
    public string SourceSpawnId => sourceSpawnId;
    public string SourceSpawnPointId => sourceSpawnPointId;
    public int SourceSpawnIndex => sourceSpawnIndex;
    public string RootObjectId => rootObjectId;
    public bool AssignedBySpawner => assignedBySpawner;
    public bool HasStableInstanceId => !string.IsNullOrEmpty(stableInstanceId);

    /// <summary>
    /// 스포너가 생성한 오브젝트에 저장 가능한 런타임 ID를 주입합니다.
    /// 같은 스폰 테이블과 같은 순번이면 세션이 달라도 같은 ID가 만들어져야 합니다.
    /// </summary>
    public void AssignSpawnIdentity(
        string spawnId,
        string spawnPointId,
        string spawnedRootObjectId,
        int spawnIndex)
    {
        sourceSpawnId = NormalizeIdPart(spawnId);
        sourceSpawnPointId = NormalizeIdPart(spawnPointId);
        rootObjectId = NormalizeIdPart(spawnedRootObjectId);
        sourceSpawnIndex = Mathf.Max(0, spawnIndex);
        stableInstanceId = BuildSpawnStableId(sourceSpawnId, sourceSpawnPointId, rootObjectId, sourceSpawnIndex);
        assignedBySpawner = true;
    }

    /// <summary>
    /// 씬에 직접 배치한 오브젝트의 저장 ID를 코드나 에디터 도구에서 지정할 때 사용합니다.
    /// </summary>
    public void SetManualIdentity(string manualStableInstanceId, string manualRootObjectId = null)
    {
        stableInstanceId = NormalizeIdPart(manualStableInstanceId);
        rootObjectId = NormalizeIdPart(manualRootObjectId);
        sourceSpawnId = null;
        sourceSpawnPointId = null;
        sourceSpawnIndex = -1;
        assignedBySpawner = false;
    }

    public string GetRewardClaimId(HWJ_RootObjectDataResolver fallbackResolver = null)
    {
        return BuildRewardClaimId(this, fallbackResolver);
    }

    public HWJ_SaveIdentityValidationResult ValidateForRewardClaim(HWJ_RootObjectDataResolver fallbackResolver = null)
    {
        HWJ_RootObjectDataResolver resolver = fallbackResolver != null
            ? fallbackResolver
            : GetComponent<HWJ_RootObjectDataResolver>();
        return ValidateRewardClaimIdentity(this, resolver);
    }

    public static string BuildRewardClaimId(HWJ_RuntimeSaveIdentity saveIdentity, HWJ_RootObjectDataResolver resolver)
    {
        string objectId = ResolveRootObjectId(saveIdentity, resolver);
        string instanceId = saveIdentity != null ? NormalizeIdPart(saveIdentity.StableInstanceId) : null;

        if (string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(instanceId))
        {
            return null;
        }

        return objectId + ":" + instanceId;
    }

    /// <summary>
    /// 저장 진행 데이터에 들어갈 보상 claim ID를 만들 수 있는지 실패 사유까지 포함해 검사합니다.
    /// 씬 수동 배치 오브젝트 검증과 저장 서비스가 켜진 보상 지급 경로에서 사용합니다.
    /// </summary>
    public static HWJ_SaveIdentityValidationResult ValidateRewardClaimIdentity(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null)
        {
            return HWJ_SaveIdentityValidationResult.Fail(
                HWJ_SaveIdentityValidationFailureCode.MissingResolver,
                "Save identity validation failed: resolver is missing.");
        }

        return ValidateRewardClaimIdentity(resolver.GetComponent<HWJ_RuntimeSaveIdentity>(), resolver);
    }

    public static HWJ_SaveIdentityValidationResult ValidateRewardClaimIdentity(
        HWJ_RuntimeSaveIdentity saveIdentity,
        HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null)
        {
            return HWJ_SaveIdentityValidationResult.Fail(
                HWJ_SaveIdentityValidationFailureCode.MissingResolver,
                "Save identity validation failed: resolver is missing.",
                saveIdentity);
        }

        if (saveIdentity == null)
        {
            return HWJ_SaveIdentityValidationResult.Fail(
                HWJ_SaveIdentityValidationFailureCode.MissingIdentityComponent,
                "Save identity validation failed: HWJ_RuntimeSaveIdentity is missing.",
                null,
                resolver);
        }

        string instanceId = NormalizeIdPart(saveIdentity.StableInstanceId);
        string objectId = ResolveRootObjectId(saveIdentity, resolver);

        if (string.IsNullOrEmpty(instanceId))
        {
            return HWJ_SaveIdentityValidationResult.Fail(
                HWJ_SaveIdentityValidationFailureCode.MissingStableInstanceId,
                "Save identity validation failed: stable instance id is empty.",
                saveIdentity,
                resolver,
                instanceId,
                objectId);
        }

        if (string.IsNullOrEmpty(objectId))
        {
            return HWJ_SaveIdentityValidationResult.Fail(
                HWJ_SaveIdentityValidationFailureCode.MissingRootObjectId,
                "Save identity validation failed: root object id is empty.",
                saveIdentity,
                resolver,
                instanceId,
                objectId);
        }

        return HWJ_SaveIdentityValidationResult.Success(
            saveIdentity,
            resolver,
            instanceId,
            objectId,
            objectId + ":" + instanceId);
    }

    public static bool TryGetRewardClaimId(HWJ_RootObjectDataResolver resolver, out string rewardClaimId)
    {
        HWJ_SaveIdentityValidationResult validationResult = ValidateRewardClaimIdentity(resolver);
        rewardClaimId = validationResult.RewardClaimId;
        return validationResult.Succeeded;
    }

    public static bool TryGetRewardClaimIdDetailed(
        HWJ_RootObjectDataResolver resolver,
        out string rewardClaimId,
        out HWJ_SaveIdentityValidationResult validationResult)
    {
        validationResult = ValidateRewardClaimIdentity(resolver);
        rewardClaimId = validationResult.RewardClaimId;
        return validationResult.Succeeded;
    }

    public static string ResolveRootObjectId(HWJ_RuntimeSaveIdentity saveIdentity, HWJ_RootObjectDataResolver resolver)
    {
        if (saveIdentity != null && !string.IsNullOrEmpty(saveIdentity.RootObjectId))
        {
            return NormalizeIdPart(saveIdentity.RootObjectId);
        }

        if (resolver != null
            && resolver.RootObjectData != null
            && resolver.RootObjectData.Identity != null
            && !string.IsNullOrEmpty(resolver.RootObjectData.Identity.objectId))
        {
            return NormalizeIdPart(resolver.RootObjectData.Identity.objectId);
        }

        return resolver != null && resolver.RootObjectData != null
            ? NormalizeIdPart(resolver.RootObjectData.name)
            : null;
    }

    public static string CreateSpawnStableId(
        string spawnId,
        string spawnPointId,
        string spawnedRootObjectId,
        int spawnIndex)
    {
        return BuildSpawnStableId(
            NormalizeIdPart(spawnId),
            NormalizeIdPart(spawnPointId),
            NormalizeIdPart(spawnedRootObjectId),
            Mathf.Max(0, spawnIndex));
    }

    private static string BuildSpawnStableId(
        string spawnId,
        string spawnPointId,
        string spawnedRootObjectId,
        int spawnIndex)
    {
        StringBuilder builder = new StringBuilder("spawn");
        AppendIdPart(builder, spawnId);
        AppendIdPart(builder, spawnPointId);
        AppendIdPart(builder, spawnedRootObjectId);
        builder.Append('.');
        builder.Append(spawnIndex.ToString("D3"));
        return builder.ToString();
    }

    private static void AppendIdPart(StringBuilder builder, string idPart)
    {
        if (builder == null || string.IsNullOrEmpty(idPart))
        {
            return;
        }

        builder.Append('.');
        builder.Append(idPart);
    }

    public static string NormalizeIdPart(string rawId)
    {
        if (string.IsNullOrEmpty(rawId))
        {
            return null;
        }

        string trimmedId = rawId.Trim();

        if (string.IsNullOrEmpty(trimmedId))
        {
            return null;
        }

        char[] normalizedChars = trimmedId.ToCharArray();

        for (int i = 0; i < normalizedChars.Length; i++)
        {
            if (!IsSafeIdChar(normalizedChars[i]))
            {
                normalizedChars[i] = '_';
            }
        }

        return new string(normalizedChars);
    }

    private static bool IsSafeIdChar(char character)
    {
        return char.IsLetterOrDigit(character)
            || character == '_'
            || character == '-'
            || character == '.';
    }
}
