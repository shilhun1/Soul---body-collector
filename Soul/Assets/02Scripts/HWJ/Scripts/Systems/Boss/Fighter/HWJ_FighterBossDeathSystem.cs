using System.Collections;
using System;
using UnityEngine;

/// <summary>
/// Owns the fighter boss final-death presentation and terminal physics cleanup.
/// The death clip calls the public Death* methods through Animation Events.
/// </summary>
public class HWJ_FighterBossDeathSystem : MonoBehaviour
{
    private const string DeathAnimatorState = "P2_Death";

    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_FighterBossAnimatorSystem animatorSystem;
    [SerializeField] private HWJ_FighterBossPhaseTwoPatternSystem phaseTwoPatternSystem;
    [SerializeField] private HWJ_BossDialogueBubbleSystem dialogueBubbleSystem;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private float watchdogSeconds = 1.65f;

    private Coroutine deathRoutine;
    private bool deathRunning;
    private bool deathComplete;
    private int beginDeathCount;
    private int completedDeathCount;
    private int cameraShakeHookCount;
    private int sfxHookCount;
    private int completionEventCount;

    public event Action<HWJ_FighterBossDeathSystem> FinalDeathCompleted;

    public bool IsDeathRunning => deathRunning;
    public bool IsDeathComplete => deathComplete;
    public int BeginDeathCount => beginDeathCount;
    public int CompletedDeathCount => completedDeathCount;
    public int CameraShakeHookCount => cameraShakeHookCount;
    public int SfxHookCount => sfxHookCount;
    public int CompletionEventCount => completionEventCount;
    public bool BodyColliderEnabled => bodyCollider != null && bodyCollider.enabled;
    public bool BodySimulated => body != null && body.simulated;

    private void Awake()
    {
        CacheReferences();
    }

    /// <summary>
    /// Called once by BossBrain when phase-two HP reaches zero.
    /// </summary>
    public bool BeginFinalDeath()
    {
        CacheReferences();

        if (deathRunning || deathComplete)
        {
            return false;
        }

        deathRunning = true;
        beginDeathCount++;
        phaseTwoPatternSystem?.CancelActivePattern();
        DisableAllAttackHitboxes();
        StopBodyMotion();

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        bool usesAnimator = animatorSystem != null
            ? animatorSystem.HasState(DeathAnimatorState)
            : HasAnimatorState(DeathAnimatorState);

        if (dialogueBubbleSystem != null && !dialogueBubbleSystem.IsDeathSequencePlaying)
        {
            dialogueBubbleSystem.ShowDeathDialogue();
        }

        deathRoutine = StartCoroutine(DeathDialogueThenAnimationRoutine(usesAnimator));

        return true;
    }

    // Animation Event: confirms the first death frame has started.
    public void DeathStart()
    {
        DisableAllAttackHitboxes();
        StopBodyMotion();
    }

    // Animation Event hook for a future final impact camera effect.
    public void DeathCameraShakeHook()
    {
        cameraShakeHookCount++;
    }

    // Animation Event hook for a future final death sound.
    public void DeathSFXHook()
    {
        sfxHookCount++;
    }

    // Animation Event: freezes the terminal body on the final sprite frame.
    public void DeathEnd()
    {
        CompleteDeath();
    }

    private IEnumerator DeathDialogueThenAnimationRoutine(bool usesAnimator)
    {
        // 기획 대사를 모두 보여준 뒤 마지막 쓰러짐 애니메이션을 시작합니다.
        while (deathRunning
            && dialogueBubbleSystem != null
            && dialogueBubbleSystem.IsDeathSequencePlaying)
        {
            yield return null;
        }

        if (!deathRunning)
        {
            yield break;
        }

        if (!usesAnimator)
        {
            CompleteDeath();
            yield break;
        }

        if (animatorSystem != null)
        {
            animatorSystem.BeginDeath();
        }
        else
        {
            animator.Play(GetFullPathHash(DeathAnimatorState), 0, 0f);
        }

        float elapsed = 0f;

        while (deathRunning && elapsed < Mathf.Max(0.1f, watchdogSeconds))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (deathRunning)
        {
            CompleteDeath();
        }
    }

    private void CompleteDeath()
    {
        if (!deathRunning)
        {
            return;
        }

        deathRunning = false;
        deathComplete = true;
        completedDeathCount++;
        DisableAllAttackHitboxes();
        StopBodyMotion();

        if (body != null)
        {
            body.simulated = false;
        }

        completionEventCount++;
        FinalDeathCompleted?.Invoke(this);
        deathRoutine = null;
    }

    /// <summary>
    /// Restores this component when a pooled boss or the debug window resets the encounter.
    /// </summary>
    public void ResetDeathState()
    {
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        deathRunning = false;
        deathComplete = false;
        dialogueBubbleSystem?.CancelDialogueSequence(true);
        DisableAllAttackHitboxes();

        if (body != null)
        {
            body.simulated = true;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }
    }

    private void DisableAllAttackHitboxes()
    {
        HWJ_FighterBossHitboxSystem[] hitboxes =
            GetComponentsInChildren<HWJ_FighterBossHitboxSystem>(true);

        for (int i = 0; i < hitboxes.Length; i++)
        {
            hitboxes[i]?.Disarm();
        }
    }

    private void StopBodyMotion()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private bool HasAnimatorState(string stateName)
    {
        return animator != null
            && animator.runtimeAnimatorController != null
            && animator.HasState(0, GetFullPathHash(stateName));
    }

    private static int GetFullPathHash(string stateName)
    {
        return Animator.StringToHash($"Base Layer.{stateName}");
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animatorSystem == null)
        {
            animatorSystem = GetComponent<HWJ_FighterBossAnimatorSystem>();
        }

        if (phaseTwoPatternSystem == null)
        {
            phaseTwoPatternSystem = GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        }

        if (dialogueBubbleSystem == null)
        {
            dialogueBubbleSystem = GetComponent<HWJ_BossDialogueBubbleSystem>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }
    }
}
