using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_StageEnemyCountSystem : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;
    [SerializeField] private bool autoCompleteObjectiveWhenAllEnemiesDefeated = true;
    [SerializeField] private bool enterCombatWhenEnemiesDetected = true;
    [SerializeField] private bool requireAtLeastOneEnemyDetectedBeforeComplete = true;

    [Header("Detection Scope")]
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Collider2D detectionArea2D;
    [SerializeField] private bool includeInactiveEnemies;
    [SerializeField] private bool requireRuntimeStatus = true;

    [Header("Target Filter")]
    [SerializeField] private bool countEnemyObjects = true;
    [SerializeField] private bool countBossObjects;
    [SerializeField] private bool requireFaction;
    [SerializeField] private HWJ_Faction requiredFaction = HWJ_Faction.Monster;

    [Header("Scan")]
    [SerializeField] private bool scanOnStart = true;
    [SerializeField] private bool rescanOnGameplayEvents = true;
    [SerializeField] private bool periodicRescan = true;
    [SerializeField] private float rescanIntervalSeconds = 0.25f;

    [Header("Runtime Read Only")]
    [SerializeField] private int trackedEnemyCount;
    [SerializeField] private int remainingAliveEnemyCount;
    [SerializeField] private int defeatedEnemyCount;
    [SerializeField] private bool hasDetectedAnyEnemy;
    [SerializeField] private bool objectiveCompletedByThisSystem;
    [SerializeField] private string lastDetectionMessage;

    private float nextScanTime;

    public int TrackedEnemyCount => trackedEnemyCount;
    public int RemainingAliveEnemyCount => remainingAliveEnemyCount;
    public int DefeatedEnemyCount => defeatedEnemyCount;
    public bool HasDetectedAnyEnemy => hasDetectedAnyEnemy;
    public bool ObjectiveCompletedByThisSystem => objectiveCompletedByThisSystem;
    public string LastDetectionMessage => lastDetectionMessage;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (scanOnStart)
        {
            ForceScan("start");
        }
    }

    private void OnEnable()
    {
        if (!rescanOnGameplayEvents)
        {
            return;
        }

        HWJ_GameplayEvents.EnemyDefeated += OnEnemyDefeated;
        HWJ_GameplayEvents.ActorDied += OnActorDied;
        HWJ_GameplayEvents.RuntimeStateChanged += OnRuntimeStateChanged;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.EnemyDefeated -= OnEnemyDefeated;
        HWJ_GameplayEvents.ActorDied -= OnActorDied;
        HWJ_GameplayEvents.RuntimeStateChanged -= OnRuntimeStateChanged;
    }

    private void Update()
    {
        if (!periodicRescan || Time.time < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.time + Mathf.Max(0.05f, rescanIntervalSeconds);
        ForceScan("periodic");
    }

    public void ForceScan()
    {
        ForceScan("manual");
    }

    public void ForceScan(string reason)
    {
        ResolveReferences();

        int newTrackedCount = 0;
        int newAliveCount = 0;
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            includeInactiveEnemies ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (!IsTrackableEnemy(resolver))
            {
                continue;
            }

            newTrackedCount++;

            if (IsEnemyAlive(resolver))
            {
                newAliveCount++;
            }
        }

        trackedEnemyCount = newTrackedCount;
        remainingAliveEnemyCount = newAliveCount;
        defeatedEnemyCount = Mathf.Max(0, trackedEnemyCount - remainingAliveEnemyCount);
        hasDetectedAnyEnemy = hasDetectedAnyEnemy || trackedEnemyCount > 0;
        lastDetectionMessage = $"Stage enemy scan ({reason}) tracked:{trackedEnemyCount}, alive:{remainingAliveEnemyCount}, defeated:{defeatedEnemyCount}.";

        TryEnterCombatWhenEnemiesExist();
        TryCompleteObjectiveWhenNoEnemiesRemain(reason);
    }

    private void OnEnemyDefeated(HWJ_EnemyDefeatedEvent defeatedEvent)
    {
        if (defeatedEvent.IsBoss && !countBossObjects)
        {
            return;
        }

        ForceScan("enemy_defeated_event");
    }

    private void OnActorDied(HWJ_DamageEvent damageEvent)
    {
        if (damageEvent.TargetStatus == null)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = damageEvent.TargetStatus.GetComponent<HWJ_RootObjectDataResolver>();

        if (IsTrackableEnemy(resolver))
        {
            ForceScan("actor_died_event");
        }
    }

    private void OnRuntimeStateChanged(HWJ_RuntimeStateChangedEvent stateEvent)
    {
        if (stateEvent.Status == null || stateEvent.CurrentState != HWJ_RuntimeState.Dead)
        {
            return;
        }

        HWJ_RootObjectDataResolver resolver = stateEvent.Status.GetComponent<HWJ_RootObjectDataResolver>();

        if (IsTrackableEnemy(resolver))
        {
            ForceScan("runtime_dead_state_event");
        }
    }

    private void TryEnterCombatWhenEnemiesExist()
    {
        if (!enterCombatWhenEnemiesDetected
            || stageProgressionSystem == null
            || remainingAliveEnemyCount <= 0)
        {
            return;
        }

        if (stageProgressionSystem.CurrentState == HWJ_StageFlowState.Entering
            || stageProgressionSystem.CurrentState == HWJ_StageFlowState.Exploring)
        {
            stageProgressionSystem.TryEnterCombat("Stage enemy count detected active enemies.");
        }
    }

    private void TryCompleteObjectiveWhenNoEnemiesRemain(string reason)
    {
        if (!autoCompleteObjectiveWhenAllEnemiesDefeated
            || objectiveCompletedByThisSystem
            || stageProgressionSystem == null
            || stageProgressionSystem.ObjectiveComplete
            || remainingAliveEnemyCount > 0)
        {
            return;
        }

        if (requireAtLeastOneEnemyDetectedBeforeComplete && !hasDetectedAnyEnemy)
        {
            lastDetectionMessage = "Stage enemy objective held: no enemy has been detected yet.";
            return;
        }

        if (stageProgressionSystem.CurrentState == HWJ_StageFlowState.Entering)
        {
            stageProgressionSystem.TryEnterExploring("Stage enemy objective found no remaining enemies.");
        }

        HWJ_StageFlowTransitionResult result = stageProgressionSystem.TryMarkObjectiveComplete(
            $"All stage enemies defeated by enemy count system. Reason: {reason}.");
        objectiveCompletedByThisSystem = result.Succeeded;
        lastDetectionMessage = result.Succeeded
            ? "Stage enemy objective completed: all tracked enemies are defeated."
            : result.Message;
    }

    private bool IsTrackableEnemy(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.RootObjectData == null)
        {
            return false;
        }

        if (!includeInactiveEnemies && !resolver.gameObject.activeInHierarchy)
        {
            return false;
        }

        bool isTargetType = (countEnemyObjects && resolver.ObjectType == HWJ_ObjectType.Enemy)
            || (countBossObjects && resolver.ObjectType == HWJ_ObjectType.Boss);

        if (!isTargetType)
        {
            return false;
        }

        if (requireFaction && resolver.Identity != null && resolver.Identity.faction != requiredFaction)
        {
            return false;
        }

        if (requireRuntimeStatus && resolver.GetComponent<HWJ_RuntimeStatusSystem>() == null)
        {
            return false;
        }

        if (stageRoot != null && !resolver.transform.IsChildOf(stageRoot))
        {
            return false;
        }

        return detectionArea2D == null || detectionArea2D.OverlapPoint(resolver.transform.position);
    }

    private bool IsEnemyAlive(HWJ_RootObjectDataResolver resolver)
    {
        HWJ_RuntimeStatusSystem status = resolver != null
            ? resolver.GetComponent<HWJ_RuntimeStatusSystem>()
            : null;

        if (status == null)
        {
            return !requireRuntimeStatus;
        }

        return !status.IsDead;
    }

    private void ResolveReferences()
    {
        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = GetComponent<HWJ_StageProgressionSystem>();
        }
    }
}
