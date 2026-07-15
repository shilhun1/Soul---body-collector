using System;

[Serializable]
public readonly struct HWJ_BodyDiscoveryCandidate
{
    public readonly HWJ_RootObjectDataResolver Resolver;
    public readonly float Distance;
    public readonly float Score;
    public readonly HWJ_PossessionResult PossessionResult;

    public HWJ_BodyDiscoveryCandidate(
        HWJ_RootObjectDataResolver resolver,
        float distance,
        float score,
        HWJ_PossessionResult possessionResult)
    {
        Resolver = resolver;
        Distance = distance;
        Score = score;
        PossessionResult = possessionResult;
    }
}

[Serializable]
public readonly struct HWJ_BodyDiscoveryResult
{
    public readonly bool HasTarget;
    public readonly HWJ_RootObjectDataResolver Target;
    public readonly HWJ_BodyDiscoveryCandidate[] Candidates;
    public readonly int EvaluatedCount;
    public readonly int RejectedCount;
    public readonly string Message;

    public HWJ_BodyDiscoveryResult(
        bool hasTarget,
        HWJ_RootObjectDataResolver target,
        HWJ_BodyDiscoveryCandidate[] candidates,
        int evaluatedCount,
        int rejectedCount,
        string message)
    {
        HasTarget = hasTarget;
        Target = target;
        Candidates = candidates ?? Array.Empty<HWJ_BodyDiscoveryCandidate>();
        EvaluatedCount = evaluatedCount;
        RejectedCount = rejectedCount;
        Message = message;
    }

    public static HWJ_BodyDiscoveryResult Empty(string message, int evaluatedCount = 0, int rejectedCount = 0)
    {
        return new HWJ_BodyDiscoveryResult(
            false,
            null,
            Array.Empty<HWJ_BodyDiscoveryCandidate>(),
            evaluatedCount,
            rejectedCount,
            message);
    }
}
