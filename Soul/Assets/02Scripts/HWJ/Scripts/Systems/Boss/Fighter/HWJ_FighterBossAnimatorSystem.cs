using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralizes the fighter boss Animator contract.
/// Attack systems request a logical state name here, while this component owns
/// controller parameters and the concrete sub-state-machine path.
/// </summary>
[DisallowMultipleComponent]
public sealed class HWJ_FighterBossAnimatorSystem : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int PhaseHash = Animator.StringToHash("Phase");
    private static readonly int AttackIdHash = Animator.StringToHash("AttackId");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int PhaseTransitionHash = Animator.StringToHash("PhaseTransition");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private float moveThreshold = 0.05f;

    private readonly Dictionary<string, int> resolvedStateHashes = new Dictionary<string, int>();
    private RuntimeAnimatorController cachedController;
    private int currentAttackId;
    private int currentPhase = 1;
    private bool isAttacking;
    private bool transitionRunning;
    private bool deathRunning;
    private HWJ_RuntimeState previousRuntimeState;

    public Animator Animator => animator;
    public int CurrentAttackId => currentAttackId;
    public int CurrentPhase => currentPhase;
    public bool IsAttacking => isAttacking;
    public string CurrentStateName { get; private set; }

    private void Awake()
    {
        CacheReferences();
        RefreshControllerCache();
        SyncPhaseFromBrain();
        previousRuntimeState = runtimeStatus != null
            ? runtimeStatus.CurrentState
            : HWJ_RuntimeState.None;
        WriteAllParameters();
    }

    private void Update()
    {
        CacheReferences();
        RefreshControllerCache();
        SyncPhaseFromBrain();
        UpdateHurtState();

        float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
        SetFloat(SpeedHash, speed);
        SetInteger(PhaseHash, currentPhase);

        if (isAttacking || transitionRunning || deathRunning)
        {
            return;
        }

        string locomotionState = speed > moveThreshold ? "P1_Move" : "P1_Idle";

        if (!IsCurrentState(locomotionState))
        {
            PlayState(locomotionState, 0f);
        }
    }

    /// <summary>
    /// Starts one attack and writes the public Animator parameters used by tools and transitions.
    /// </summary>
    public bool BeginAttack(int attackId, string stateName)
    {
        if (deathRunning || transitionRunning || attackId <= 0)
        {
            return false;
        }

        currentAttackId = attackId;
        isAttacking = true;
        SetInteger(AttackIdHash, currentAttackId);
        SetBool(IsAttackingHash, true);
        return PlayState(stateName, 0f);
    }

    /// <summary>
    /// Clears the attack contract even when an Animation Event is missing.
    /// </summary>
    public void EndAttack(bool returnToIdle = true)
    {
        currentAttackId = 0;
        isAttacking = false;
        SetInteger(AttackIdHash, 0);
        SetBool(IsAttackingHash, false);

        if (returnToIdle && !transitionRunning && !deathRunning)
        {
            PlayState("P1_Idle", 0.04f);
        }
    }

    public void TriggerHurt()
    {
        if (isAttacking || transitionRunning || deathRunning)
        {
            return;
        }

        SetTrigger(HurtHash);
        PlayState("Hurt", 0.02f);
    }

    public void BeginPhaseTransition()
    {
        EndAttack(false);
        transitionRunning = true;
        SetTrigger(PhaseTransitionHash);
    }

    public void CompletePhaseTransition()
    {
        transitionRunning = false;
        SetPhase(2);
    }

    public void BeginDeath()
    {
        EndAttack(false);
        transitionRunning = false;
        deathRunning = true;
        SetTrigger(DeadHash);
        PlayState("P2_Death", 0f);
    }

    public void SetPhase(int phase)
    {
        currentPhase = Mathf.Clamp(phase, 1, 2);
        SetInteger(PhaseHash, currentPhase);
    }

    public bool HasState(string stateName)
    {
        return TryResolveStateHash(stateName, out _);
    }

    public bool PlayState(string stateName, float crossFadeSeconds = 0f)
    {
        if (!TryResolveStateHash(stateName, out int stateHash))
        {
            return false;
        }

        if (crossFadeSeconds > 0f)
        {
            animator.CrossFadeInFixedTime(stateHash, crossFadeSeconds, 0);
        }
        else
        {
            animator.Play(stateHash, 0, 0f);
        }

        CurrentStateName = stateName;
        return true;
    }

    /// <summary>
    /// Restores a reusable prefab instance to a clean phase-one state.
    /// </summary>
    public void ResetAnimatorState()
    {
        currentAttackId = 0;
        currentPhase = 1;
        isAttacking = false;
        transitionRunning = false;
        deathRunning = false;
        previousRuntimeState = runtimeStatus != null
            ? runtimeStatus.CurrentState
            : HWJ_RuntimeState.None;
        WriteAllParameters();
        PlayState("P1_Idle", 0f);
    }

    private bool IsCurrentState(string stateName)
    {
        if (!TryResolveStateHash(stateName, out int stateHash))
        {
            return false;
        }

        return animator.GetCurrentAnimatorStateInfo(0).fullPathHash == stateHash;
    }

    private bool TryResolveStateHash(string stateName, out int stateHash)
    {
        stateHash = 0;

        if (animator == null
            || animator.runtimeAnimatorController == null
            || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        RefreshControllerCache();

        if (resolvedStateHashes.TryGetValue(stateName, out stateHash))
        {
            return stateHash != 0;
        }

        string[] candidates = BuildStatePathCandidates(stateName);

        for (int i = 0; i < candidates.Length; i++)
        {
            int candidateHash = Animator.StringToHash(candidates[i]);

            if (animator.HasState(0, candidateHash))
            {
                stateHash = candidateHash;
                resolvedStateHashes[stateName] = stateHash;
                return true;
            }
        }

        resolvedStateHashes[stateName] = 0;
        return false;
    }

    private static string[] BuildStatePathCandidates(string stateName)
    {
        if (stateName == "P1_Idle" || stateName == "P1_Move" || stateName == "Hurt")
        {
            return new[]
            {
                $"Base Layer.Base.{stateName}",
                $"Base Layer.{stateName}",
                stateName
            };
        }

        if (stateName.StartsWith("P1_Attack_", System.StringComparison.Ordinal))
        {
            return new[]
            {
                $"Base Layer.Phase1Attacks.{stateName}",
                $"Base Layer.{stateName}",
                stateName
            };
        }

        if (stateName.StartsWith("PhaseBreak_", System.StringComparison.Ordinal)
            || stateName == "Phase2_Start")
        {
            return new[]
            {
                $"Base Layer.Transition.{stateName}",
                $"Base Layer.{stateName}",
                stateName
            };
        }

        if (stateName == "P2_Death")
        {
            return new[]
            {
                "Base Layer.Death.P2_Death",
                "Base Layer.P2_Death",
                "P2_Death"
            };
        }

        if (stateName.StartsWith("P2_", System.StringComparison.Ordinal))
        {
            return new[]
            {
                $"Base Layer.Phase2Attacks.{stateName}",
                $"Base Layer.{stateName}",
                stateName
            };
        }

        return new[]
        {
            $"Base Layer.{stateName}",
            stateName
        };
    }

    private void RefreshControllerCache()
    {
        RuntimeAnimatorController controller = animator != null
            ? animator.runtimeAnimatorController
            : null;

        if (cachedController == controller)
        {
            return;
        }

        cachedController = controller;
        resolvedStateHashes.Clear();
    }

    private void SyncPhaseFromBrain()
    {
        if (bossBrain == null)
        {
            return;
        }

        currentPhase = bossBrain.FighterPhase == HWJ_FighterBossPhase.Phase2
            || bossBrain.FighterPhase == HWJ_FighterBossPhase.Dead
            ? 2
            : 1;
    }

    /// <summary>
    /// Converts the shared runtime Hit state into the fighter-specific Hurt animation once.
    /// Damage remains owned by HWJ_RuntimeStatusSystem; this class only presents it.
    /// </summary>
    private void UpdateHurtState()
    {
        if (runtimeStatus == null)
        {
            return;
        }

        HWJ_RuntimeState currentRuntimeState = runtimeStatus.CurrentState;

        if (currentRuntimeState == HWJ_RuntimeState.Hit
            && previousRuntimeState != HWJ_RuntimeState.Hit)
        {
            TriggerHurt();
        }

        previousRuntimeState = currentRuntimeState;
    }

    private void WriteAllParameters()
    {
        SetFloat(SpeedHash, 0f);
        SetInteger(PhaseHash, currentPhase);
        SetInteger(AttackIdHash, currentAttackId);
        SetBool(IsAttackingHash, isAttacking);
    }

    private void SetFloat(int hash, float value)
    {
        if (HasParameter(hash, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(hash, value);
        }
    }

    private void SetInteger(int hash, int value)
    {
        if (HasParameter(hash, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(hash, value);
        }
    }

    private void SetBool(int hash, bool value)
    {
        if (HasParameter(hash, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(hash, value);
        }
    }

    private void SetTrigger(int hash)
    {
        if (HasParameter(hash, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(hash);
            animator.SetTrigger(hash);
        }
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hash && parameters[i].type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }
}
