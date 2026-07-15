using System.Collections.Generic;
using UnityEngine;

public class HWJ_BodyDiscoverySystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private LayerMask discoveryLayer;
    [SerializeField] private float fallbackRange = 1.5f;
    [SerializeField] private int maxOverlapHits = 32;
    [SerializeField] private bool useSceneFallbackSearch = true;
    [SerializeField] private HWJ_RootObjectDataResolver currentTarget;
    [SerializeField] private string lastDiscoveryMessage;

    private Collider2D[] overlapHits;
    private HWJ_BodyDiscoveryResult lastDiscoveryResult;

    public HWJ_RootObjectDataResolver CurrentTarget => currentTarget;
    public HWJ_BodyDiscoveryResult LastDiscoveryResult => lastDiscoveryResult;
    public string LastDiscoveryMessage => lastDiscoveryMessage;

    private void Awake()
    {
        CacheReferences();
        EnsureOverlapBuffer();
    }

    public bool RefreshDiscovery()
    {
        lastDiscoveryResult = Discover();
        UpdateCurrentTarget(lastDiscoveryResult.Target, lastDiscoveryResult);
        lastDiscoveryMessage = lastDiscoveryResult.Message;
        return lastDiscoveryResult.HasTarget;
    }

    public bool TryGetBestTarget(out HWJ_RootObjectDataResolver target)
    {
        return TryGetBestTarget(out target, out _);
    }

    public bool TryGetBestTarget(
        out HWJ_RootObjectDataResolver target,
        out HWJ_BodyDiscoveryResult discoveryResult)
    {
        RefreshDiscovery();
        discoveryResult = lastDiscoveryResult;
        target = lastDiscoveryResult.Target;
        return lastDiscoveryResult.HasTarget;
    }

    public HWJ_BodyDiscoveryResult Discover()
    {
        CacheReferences();
        EnsureOverlapBuffer();

        if (possessionSystem == null)
        {
            return HWJ_BodyDiscoveryResult.Empty("Body discovery failed: missing possession system.");
        }

        if (soulSystem != null && soulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Spirit)
        {
            return HWJ_BodyDiscoveryResult.Empty("Body discovery skipped: player is not in Spirit state.");
        }

        float range = GetDiscoveryRange();
        List<HWJ_BodyDiscoveryCandidate> candidates = new List<HWJ_BodyDiscoveryCandidate>();
        HashSet<int> seenResolvers = new HashSet<int>();
        int evaluatedCount = 0;
        int rejectedCount = 0;

        CollectCandidatesFromOverlap(range, candidates, seenResolvers, ref evaluatedCount, ref rejectedCount);

        if (useSceneFallbackSearch)
        {
            CollectCandidatesFromScene(range, candidates, seenResolvers, ref evaluatedCount, ref rejectedCount);
        }

        if (candidates.Count == 0)
        {
            return HWJ_BodyDiscoveryResult.Empty(
                "Body discovery found no valid possessable body.",
                evaluatedCount,
                rejectedCount);
        }

        candidates.Sort(CompareCandidates);
        HWJ_RootObjectDataResolver target = candidates[0].Resolver;

        return new HWJ_BodyDiscoveryResult(
            true,
            target,
            candidates.ToArray(),
            evaluatedCount,
            rejectedCount,
            $"Body discovery selected {target.name}.");
    }

    public void ClearCurrentTarget()
    {
        UpdateCurrentTarget(null, HWJ_BodyDiscoveryResult.Empty("Body discovery target cleared."));
    }

    private void CollectCandidatesFromOverlap(
        float range,
        List<HWJ_BodyDiscoveryCandidate> candidates,
        HashSet<int> seenResolvers,
        ref int evaluatedCount,
        ref int rejectedCount)
    {
        int layerMask = discoveryLayer.value != 0 ? discoveryLayer.value : Physics2D.AllLayers;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useTriggers = Physics2D.queriesHitTriggers;

        int hitCount = Physics2D.OverlapCircle(transform.position, range, filter, overlapHits);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = overlapHits[i];

            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            HWJ_RootObjectDataResolver resolver = hit.GetComponentInParent<HWJ_RootObjectDataResolver>();
            TryAddCandidate(resolver, range, candidates, seenResolvers, ref evaluatedCount, ref rejectedCount);
        }
    }

    private void CollectCandidatesFromScene(
        float range,
        List<HWJ_BodyDiscoveryCandidate> candidates,
        HashSet<int> seenResolvers,
        ref int evaluatedCount,
        ref int rejectedCount)
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            TryAddCandidate(resolvers[i], range, candidates, seenResolvers, ref evaluatedCount, ref rejectedCount);
        }
    }

    private void TryAddCandidate(
        HWJ_RootObjectDataResolver resolver,
        float range,
        List<HWJ_BodyDiscoveryCandidate> candidates,
        HashSet<int> seenResolvers,
        ref int evaluatedCount,
        ref int rejectedCount)
    {
        if (resolver == null || resolver == ownerDataResolver)
        {
            return;
        }

        int resolverId = resolver.GetInstanceID();

        if (seenResolvers != null && !seenResolvers.Add(resolverId))
        {
            return;
        }

        if (!resolver.gameObject.activeInHierarchy
            || (resolver.ObjectType != HWJ_ObjectType.Enemy && resolver.ObjectType != HWJ_ObjectType.Boss))
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, resolver.transform.position);

        if (distance > range)
        {
            return;
        }

        evaluatedCount++;
        HWJ_PossessionResult possessionResult = possessionSystem.EvaluatePossession(resolver);

        if (!possessionResult.Succeeded)
        {
            rejectedCount++;
            return;
        }

        float score = distance;
        candidates.Add(new HWJ_BodyDiscoveryCandidate(resolver, distance, score, possessionResult));
    }

    private void UpdateCurrentTarget(
        HWJ_RootObjectDataResolver nextTarget,
        HWJ_BodyDiscoveryResult discoveryResult)
    {
        if (currentTarget == nextTarget)
        {
            return;
        }

        HWJ_RootObjectDataResolver previousTarget = currentTarget;
        currentTarget = nextTarget;
        HWJ_GameplayEvents.RaisePossessionTargetChanged(
            new HWJ_PossessionTargetChangedEvent(this, previousTarget, currentTarget, discoveryResult));
    }

    private int CompareCandidates(HWJ_BodyDiscoveryCandidate left, HWJ_BodyDiscoveryCandidate right)
    {
        int scoreCompare = left.Score.CompareTo(right.Score);

        if (scoreCompare != 0)
        {
            return scoreCompare;
        }

        string leftName = left.Resolver != null ? left.Resolver.name : string.Empty;
        string rightName = right.Resolver != null ? right.Resolver.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private float GetDiscoveryRange()
    {
        float range = fallbackRange;

        if (ownerDataResolver != null && ownerDataResolver.Interaction != null)
        {
            range = Mathf.Max(range, ownerDataResolver.Interaction.interactionRange);
        }

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Possession != null)
        {
            range = Mathf.Max(range, playerData.Possession.possessionRange);
        }

        return Mathf.Max(0f, range);
    }

    private void CacheReferences()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }
    }

    private void EnsureOverlapBuffer()
    {
        int size = Mathf.Max(1, maxOverlapHits);

        if (overlapHits == null || overlapHits.Length != size)
        {
            overlapHits = new Collider2D[size];
        }
    }
}
