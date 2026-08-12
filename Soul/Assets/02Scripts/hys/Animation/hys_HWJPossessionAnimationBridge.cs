using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HWJ 빙의 성공 이벤트를 받아 유령의 Possession 모션을 끝까지 재생한 뒤 육체 Controller로 넘깁니다.
/// 빙의 해제 시에는 저장한 유령 Controller를 복구하므로 씬별 Animator 설정이 필요하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-900)]
public sealed class hys_HWJPossessionAnimationBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private HWJ_CharacterMotionSystem motionSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_CollapseSystem collapseSystem;
    [SerializeField] private hys_PossessionAnimationLibrary possessionAnimationLibrary;
    [SerializeField, Range(0.5f, 1f)] private float handoffNormalizedTime = 0.98f;
    [SerializeField, Min(0.1f)] private float fallbackHandoffSeconds = 1f;
    [Header("살아 있는 몸 해제 분리")]
    [SerializeField] private Vector2 livingReleaseSoulOffset = new Vector2(0.65f, 0.45f);

    private RuntimeAnimatorController ghostController;
    private AnimatorOverrideController activePossessionController;
    private bool possessionAnimationPlaying;
    private bool soulExitAnimationPlaying;
    private bool deathAnimationPlaying;
    private bool ghostAppearAnimationPlaying;
    private bool livingBodyReleaseVisualActive;
    private bool continueSoulExitAfterDeath;
    private bool suppressNextAutomaticGhostAppear;
    private float lastMentalNormalized = 1f;
    private bool bodyCollapsePending;
    private bool collapsedBodyWasAlive;
    private HWJ_RootObjectDataResolver delayedCorpseResolver;
    private SpriteRenderer[] delayedCorpseRenderers;
    private bool[] delayedCorpseRendererStates;
    private Collider2D[] delayedCorpseColliders;
    private bool[] delayedCorpseColliderStates;
    private Rigidbody2D[] delayedCorpseBodies;
    private bool[] delayedCorpseBodyStates;
    private bool hasDelayedCorpseAnchor;
    private Vector2 delayedCorpseAnchor;
    private bool restoreMotionSystemEnabled;
    private float possessionStartedAt;
    private float activeFallbackHandoffSeconds;
    private int activeStateShortNameHash;
    private HWJ_WeaponType lastPossessedWeaponType = HWJ_WeaponType.None;

    // 다른 hys 애니메이션 브리지가 빙의 모션을 덮어쓰지 않도록 현재 재생 여부를 공유합니다.
    public bool IsPossessionAnimationPlaying => possessionAnimationPlaying
        || soulExitAnimationPlaying
        || deathAnimationPlaying
        || ghostAppearAnimationPlaying;
    // 살아 있는 몸에서 나온 영혼의 외형을 다른 Animator 제어기가 덮어쓰지 않게 공유합니다.
    public bool IsLivingBodyReleaseVisualActive => livingBodyReleaseVisualActive;

    // 무기별 Soul Exit가 이미 영혼 출현을 보여준 경우 Ghost Animator의 자동 Appear를 한 번 막습니다.
    public bool ConsumeSoulExitAppearSuppression()
    {
        if (!suppressNextAutomaticGhostAppear)
        {
            return false;
        }

        suppressNextAutomaticGhostAppear = false;
        return true;
    }

    private void Awake()
    {
        CacheReferences();
        CacheGhostController();
    }

    private void OnEnable()
    {
        CacheReferences();
        CacheGhostController();

        HWJ_GameplayEvents.PossessionChanged += OnPossessionChanged;
        HWJ_GameplayEvents.BodyCollapseStarted += OnBodyCollapseStarted;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.PossessionChanged -= OnPossessionChanged;
        HWJ_GameplayEvents.BodyCollapseStarted -= OnBodyCollapseStarted;
        bodyCollapsePending = false;
        collapsedBodyWasAlive = false;
        RestoreDelayedCorpsePresentation();
        continueSoulExitAfterDeath = false;
        suppressNextAutomaticGhostAppear = false;
        livingBodyReleaseVisualActive = false;
        FinishPossessionAnimation();
    }

    private void Update()
    {
        CacheReferences();
        CacheGhostController();
        UpdateMentalAnimatorParameters();
        if (livingBodyReleaseVisualActive
            && soulSystem != null
            && soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            livingBodyReleaseVisualActive = false;
        }

        if (!IsPossessionAnimationPlaying || animator == null)
        {
            return;
        }

        bool timedOut = Time.unscaledTime - possessionStartedAt >= activeFallbackHandoffSeconds;
        bool finishedState = false;
        if (!animator.IsInTransition(0))
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            finishedState = activeStateShortNameHash != 0
                && state.shortNameHash == activeStateShortNameHash
                && state.normalizedTime >= handoffNormalizedTime;
        }

        if (finishedState || timedOut)
        {
            FinishPossessionAnimation();
        }
    }

    private void OnPossessionChanged(HWJ_PossessionEvent possessionEvent)
    {
        if (possessionEvent.PossessionSystem != possessionSystem)
        {
            return;
        }

        if (possessionEvent.Possessed)
        {
            RestoreDelayedCorpsePresentation();
            collapsedBodyWasAlive = false;
            suppressNextAutomaticGhostAppear = false;
            livingBodyReleaseVisualActive = false;
            HWJ_WeaponType weaponType = possessionEvent.BodyResolver != null
                ? possessionEvent.BodyResolver.WeaponType
                : possessionSystem.CurrentWeaponType;
            PlayPossessionAnimation(weaponType);
        }
        else
        {
            HWJ_WeaponType weaponType = possessionEvent.BodyResolver != null
                ? possessionEvent.BodyResolver.WeaponType
                : lastPossessedWeaponType;
            // HP 0 붕괴만 플레이어 쪽 육체 사망 연출을 유지합니다.
            // 살아 있는 몸은 실제 몬스터가 자기 Animator로 Die→Revive를 담당하고 플레이어는 즉시 영혼으로 분리합니다.
            bool shouldContinueToSoulExit = bodyCollapsePending;
            bodyCollapsePending = false;
            if (shouldContinueToSoulExit)
            {
                // 살아있는 몸은 HWJ가 같은 프레임에 실제 시체로 복원하므로 연출이 끝날 때까지 외형과 충돌을 숨깁니다.
                if (collapsedBodyWasAlive)
                {
                    DelayCorpsePresentation(possessionEvent.BodyResolver);
                }

                collapsedBodyWasAlive = false;
                PlayDeathAnimation(weaponType, true);
                return;
            }

            HWJ_PossessionBodyState releasedBodyState = possessionEvent.BodyResolver != null
                ? possessionEvent.BodyResolver.GetComponent<HWJ_PossessionBodyState>()
                : null;
            bool isLivingBodyRelease = releasedBodyState != null
                && releasedBodyState.WasAliveWhenPossessed
                && releasedBodyState.IsReleasedAfterPossession;
            if (!isLivingBodyRelease)
            {
                // 기존 시체 빙의 해제는 원본 흐름을 유지하고 살아 있는 몸에만 분리·부활 연출을 적용합니다.
                PlayDeathAnimation(weaponType, false);
                return;
            }

            livingBodyReleaseVisualActive = true;
            SeparateSoulFromReleasedBody(possessionEvent.BodyResolver);
            PlayGhostAppearAnimation();
        }
    }

    private void SeparateSoulFromReleasedBody(HWJ_RootObjectDataResolver releasedBody)
    {
        if (releasedBody == null)
        {
            return;
        }

        // HWJ가 원본 몬스터를 플레이어와 같은 좌표에 복원하므로 영혼만 옮겨 두 외형이 겹치지 않게 합니다.
        float horizontalDirection = spriteRenderer != null && spriteRenderer.flipX ? 1f : -1f;
        Vector2 offset = new Vector2(
            Mathf.Abs(livingReleaseSoulOffset.x) * horizontalDirection,
            livingReleaseSoulOffset.y);
        Vector2 separatedPosition = (Vector2)releasedBody.transform.position + offset;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
            playerBody.position = separatedPosition;
        }
        else
        {
            transform.position = new Vector3(separatedPosition.x, separatedPosition.y, transform.position.z);
        }
    }

    private void OnBodyCollapseStarted(HWJ_BodyCollapseEvent collapseEvent)
    {
        CacheReferences();
        if (collapseEvent.CollapseSystem != collapseSystem)
        {
            return;
        }

        // HWJ가 같은 프레임에 빙의 비주얼을 해제하므로 다음 PossessionChanged에서 사망 경로임을 식별해 둡니다.
        bodyCollapsePending = true;
        HWJ_PossessionBodyState collapsedBodyState = collapseEvent.BodyResolver != null
            ? collapseEvent.BodyResolver.GetComponent<HWJ_PossessionBodyState>()
            : null;
        // 살아있는 몬스터에게 빙의한 뒤 HP 0으로 죽은 경우 HWJ가 실제 빙의 가능 시체를 복원하므로 가짜 시체 외형을 만들지 않습니다.
        collapsedBodyWasAlive = collapsedBodyState != null
            && collapsedBodyState.WasAliveWhenPossessed;
        if (collapseEvent.BodyResolver != null)
        {
            lastPossessedWeaponType = collapseEvent.BodyResolver.WeaponType;
        }
    }

    private void PlayPossessionAnimation(HWJ_WeaponType weaponType)
    {
        CacheReferences();
        CacheGhostController();
        if (animator == null || ghostController == null)
        {
            return;
        }

        lastPossessedWeaponType = weaponType;
        AnimationClip possessionClip = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetPossessionClip(weaponType)
            : null;
        activePossessionController = CreatePossessionOverrideController(possessionClip);
        if (activePossessionController == null)
        {
            // 무기 전용 클립이 없을 때 Sword 기본 모션을 잘못 보여주지 않고 즉시 육신 모션으로 넘깁니다.
            Debug.LogWarning($"[hys Animator] {weaponType} 전용 빙의 클립을 찾지 못해 공용 Sword 모션을 재생하지 않습니다.", this);
            if (motionSystem != null && !motionSystem.enabled)
            {
                motionSystem.enabled = true;
            }
            ApplyPlayerController(weaponType);
            return;
        }

        restoreMotionSystemEnabled = motionSystem != null && motionSystem.enabled;
        if (motionSystem != null)
        {
            // HWJ가 같은 프레임에 육체 Controller로 교체하지 않도록 빙의 모션 동안만 애니메이션 갱신을 보류합니다.
            motionSystem.enabled = false;
        }

        if (animator.runtimeAnimatorController != activePossessionController)
        {
            animator.runtimeAnimatorController = activePossessionController;
            animator.Rebind();
            animator.Update(0f);
        }

        int stateHash = Animator.StringToHash("Base Layer.Possession");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);

            // Sword 빙의 클립처럼 1초보다 긴 모션도 중간에 잘리지 않도록 실제 상태 길이를 사용합니다.
            AnimatorStateInfo possessionState = animator.GetCurrentAnimatorStateInfo(0);
            activeFallbackHandoffSeconds = Mathf.Max(
                fallbackHandoffSeconds,
                possessionState.length * handoffNormalizedTime + 0.05f);
        }
        else
        {
            SetTriggerIfPresent("Possess");
            activeFallbackHandoffSeconds = fallbackHandoffSeconds;
        }

        possessionAnimationPlaying = true;
        soulExitAnimationPlaying = false;
        deathAnimationPlaying = false;
        ghostAppearAnimationPlaying = false;
        continueSoulExitAfterDeath = false;
        activeStateShortNameHash = Animator.StringToHash("Possession");
        possessionStartedAt = Time.unscaledTime;
        Debug.Log($"[hys Animator] Possession clip -> {possessionClip.name}, Weapon={weaponType}", this);
    }

    private void PlaySoulExitAnimation(HWJ_WeaponType weaponType)
    {
        CacheReferences();
        CacheGhostController();
        if (animator == null || ghostController == null)
        {
            return;
        }

        if (weaponType == HWJ_WeaponType.None)
        {
            weaponType = lastPossessedWeaponType;
        }

        AnimationClip soulExitClip = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetSoulExitClip(weaponType)
            : null;
        if (soulExitClip == null || soulExitClip.empty)
        {
            // Sword처럼 비어 있는 Soul 클립은 기다리지 않고 실제 Ghost 출현 모션으로 연결합니다.
            Debug.LogWarning($"[hys Animator] {weaponType} Soul Exit 클립이 비어 있어 Ghost Appear로 대체합니다.", this);
            CompleteDelayedCorpseRelease();
            PlayGhostAppearAnimation();
            return;
        }

        activePossessionController = CreatePossessionOverrideController(soulExitClip);
        if (activePossessionController == null)
        {
            Debug.LogWarning($"[hys Animator] {weaponType} 전용 빙의 해제 클립을 찾지 못했습니다.", this);
            CompleteDelayedCorpseRelease();
            RestoreGhostStanding();
            RestoreMotionSystem();
            return;
        }

        if (!IsPossessionAnimationPlaying)
        {
            restoreMotionSystemEnabled = motionSystem != null && motionSystem.enabled;
        }
        if (motionSystem != null)
        {
            motionSystem.enabled = false;
        }

        animator.runtimeAnimatorController = activePossessionController;
        animator.Rebind();
        animator.Update(0f);

        int stateHash = Animator.StringToHash("Base Layer.Possession");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
            AnimatorStateInfo exitState = animator.GetCurrentAnimatorStateInfo(0);
            activeFallbackHandoffSeconds = Mathf.Max(
                fallbackHandoffSeconds,
                exitState.length * handoffNormalizedTime + 0.05f);
        }
        else
        {
            activeFallbackHandoffSeconds = fallbackHandoffSeconds;
        }

        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = true;
        deathAnimationPlaying = false;
        ghostAppearAnimationPlaying = false;
        activeStateShortNameHash = Animator.StringToHash("Possession");
        possessionStartedAt = Time.unscaledTime;
        Debug.Log($"[hys Animator] Soul exit clip -> {soulExitClip.name}, Weapon={weaponType}", this);
    }

    private void PlayDeathAnimation(HWJ_WeaponType weaponType, bool continueToSoulExit)
    {
        CacheReferences();
        CacheGhostController();
        if (animator == null || ghostController == null)
        {
            return;
        }

        if (weaponType == HWJ_WeaponType.None)
        {
            weaponType = lastPossessedWeaponType;
        }

        RuntimeAnimatorController playerController = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetPlayerController(weaponType)
            : null;
        if (playerController == null)
        {
            Debug.LogWarning($"[hys Animator] {weaponType} 플레이어 Controller가 없어 빙의 해제 Die를 재생하지 못했습니다.", this);
            CompleteDelayedCorpseRelease();
            RestoreGhostStanding();
            RestoreMotionSystem();
            return;
        }

        lastPossessedWeaponType = weaponType;
        restoreMotionSystemEnabled = motionSystem != null && motionSystem.enabled;
        if (motionSystem != null)
        {
            motionSystem.enabled = false;
        }

        animator.runtimeAnimatorController = playerController;
        animator.Rebind();
        animator.Update(0f);

        string weaponName = ResolveWeaponName(weaponType);
        string stateName = $"hys_{weaponName}_Die";
        int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");
        if (!animator.HasState(0, fullPathHash))
        {
            Debug.LogWarning($"[hys Animator] 빙의 해제 Die 상태를 찾지 못했습니다: {stateName}", this);
            CompleteDelayedCorpseRelease();
            RestoreGhostStanding();
            RestoreMotionSystem();
            return;
        }

        animator.Play(fullPathHash, 0, 0f);
        animator.Update(0f);
        AnimatorStateInfo deathState = animator.GetCurrentAnimatorStateInfo(0);
        activeFallbackHandoffSeconds = Mathf.Max(
            fallbackHandoffSeconds,
            deathState.length * handoffNormalizedTime + 0.05f);

        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = false;
        deathAnimationPlaying = true;
        ghostAppearAnimationPlaying = false;
        continueSoulExitAfterDeath = continueToSoulExit;
        activeStateShortNameHash = Animator.StringToHash(stateName);
        possessionStartedAt = Time.unscaledTime;
        Debug.Log($"[hys Animator] Possession exit Die -> {stateName}, Weapon={weaponType}, ContinueSoul={continueToSoulExit}", this);
    }

    private void PlayGhostAppearAnimation()
    {
        CacheReferences();
        CacheGhostController();
        if (animator == null || ghostController == null)
        {
            RestoreMotionSystem();
            return;
        }

        animator.runtimeAnimatorController = ghostController;
        animator.Rebind();
        animator.Update(0f);

        // 직접 Appear를 재생하는 동안 예약된 Any State Appear가 끝난 뒤 다시 발동하지 않게 합니다.
        suppressNextAutomaticGhostAppear = true;
        ResetTriggerIfPresent("Appear");

        int fullPathHash = Animator.StringToHash("Base Layer.Appear");
        if (!animator.HasState(0, fullPathHash))
        {
            RestoreGhostStanding();
            RestoreMotionSystem();
            return;
        }

        animator.Play(fullPathHash, 0, 0f);
        animator.Update(0f);
        AnimatorStateInfo appearState = animator.GetCurrentAnimatorStateInfo(0);
        activeFallbackHandoffSeconds = Mathf.Max(
            fallbackHandoffSeconds,
            appearState.length * handoffNormalizedTime + 0.05f);

        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = false;
        deathAnimationPlaying = false;
        ghostAppearAnimationPlaying = true;
        continueSoulExitAfterDeath = false;
        activeStateShortNameHash = Animator.StringToHash("Appear");
        possessionStartedAt = Time.unscaledTime;
        Debug.Log("[hys Animator] Soul emerged from corpse -> Ghost Appear", this);
    }

    private void DelayCorpsePresentation(HWJ_RootObjectDataResolver corpseResolver)
    {
        RestoreDelayedCorpsePresentation();
        if (corpseResolver == null || !corpseResolver.gameObject.activeInHierarchy)
        {
            return;
        }

        delayedCorpseResolver = corpseResolver;
        delayedCorpseRenderers = corpseResolver.GetComponentsInChildren<SpriteRenderer>(true);
        delayedCorpseRendererStates = new bool[delayedCorpseRenderers.Length];
        for (int i = 0; i < delayedCorpseRenderers.Length; i++)
        {
            SpriteRenderer corpseRenderer = delayedCorpseRenderers[i];
            delayedCorpseRendererStates[i] = corpseRenderer != null && corpseRenderer.enabled;
            if (corpseRenderer != null)
            {
                corpseRenderer.enabled = false;
            }
        }

        delayedCorpseColliders = corpseResolver.GetComponentsInChildren<Collider2D>(true);
        delayedCorpseColliderStates = new bool[delayedCorpseColliders.Length];
        for (int i = 0; i < delayedCorpseColliders.Length; i++)
        {
            Collider2D corpseCollider = delayedCorpseColliders[i];
            delayedCorpseColliderStates[i] = corpseCollider != null && corpseCollider.enabled;
            if (corpseCollider != null)
            {
                corpseCollider.enabled = false;
            }
        }

        delayedCorpseBodies = corpseResolver.GetComponentsInChildren<Rigidbody2D>(true);
        delayedCorpseBodyStates = new bool[delayedCorpseBodies.Length];
        for (int i = 0; i < delayedCorpseBodies.Length; i++)
        {
            Rigidbody2D corpseBody = delayedCorpseBodies[i];
            delayedCorpseBodyStates[i] = corpseBody != null && corpseBody.simulated;
            if (corpseBody != null)
            {
                corpseBody.linearVelocity = Vector2.zero;
                corpseBody.angularVelocity = 0f;
                corpseBody.simulated = false;
            }
        }
    }

    private void CompleteDelayedCorpseRelease()
    {
        HWJ_RootObjectDataResolver releasedCorpse = delayedCorpseResolver;
        bool shouldAlignCorpse = hasDelayedCorpseAnchor;
        Vector2 corpseAnchor = delayedCorpseAnchor;
        RestoreDelayedCorpsePresentation();
        if (releasedCorpse == null || !releasedCorpse.gameObject.activeInHierarchy)
        {
            return;
        }

        if (shouldAlignCorpse)
        {
            AlignCorpseFeet(releasedCorpse, corpseAnchor);
        }

        // Soul Exit가 끝난 뒤 실제 빙의 가능 시체를 표시하고 영혼을 옆으로 분리합니다.
        livingBodyReleaseVisualActive = true;
        SeparateSoulFromReleasedBody(releasedCorpse);
    }

    private void RestoreDelayedCorpsePresentation()
    {
        RestoreEnabledStates(delayedCorpseRenderers, delayedCorpseRendererStates);
        RestoreEnabledStates(delayedCorpseColliders, delayedCorpseColliderStates);

        if (delayedCorpseBodies != null && delayedCorpseBodyStates != null)
        {
            int count = Mathf.Min(delayedCorpseBodies.Length, delayedCorpseBodyStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (delayedCorpseBodies[i] != null)
                {
                    delayedCorpseBodies[i].simulated = delayedCorpseBodyStates[i];
                }
            }
        }

        delayedCorpseResolver = null;
        delayedCorpseRenderers = null;
        delayedCorpseRendererStates = null;
        delayedCorpseColliders = null;
        delayedCorpseColliderStates = null;
        delayedCorpseBodies = null;
        delayedCorpseBodyStates = null;
        hasDelayedCorpseAnchor = false;
        delayedCorpseAnchor = Vector2.zero;
    }

    private void CaptureDelayedCorpseAnchor()
    {
        if (delayedCorpseResolver == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        // 플레이어 Die 마지막 프레임의 발 위치를 실제 시체가 나타날 기준점으로 보관합니다.
        Bounds bounds = spriteRenderer.bounds;
        delayedCorpseAnchor = new Vector2(bounds.center.x, bounds.min.y);
        hasDelayedCorpseAnchor = true;
    }

    private static void AlignCorpseFeet(HWJ_RootObjectDataResolver corpseResolver, Vector2 targetFeet)
    {
        SpriteRenderer[] corpseRenderers = corpseResolver.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer visibleRenderer = null;
        for (int i = 0; i < corpseRenderers.Length; i++)
        {
            if (corpseRenderers[i] != null
                && corpseRenderers[i].enabled
                && corpseRenderers[i].sprite != null)
            {
                visibleRenderer = corpseRenderers[i];
                break;
            }
        }

        if (visibleRenderer == null)
        {
            return;
        }

        Bounds corpseBounds = visibleRenderer.bounds;
        Vector2 currentFeet = new Vector2(corpseBounds.center.x, corpseBounds.min.y);
        Vector2 correction = targetFeet - currentFeet;
        corpseResolver.transform.position += new Vector3(correction.x, correction.y, 0f);
        Physics2D.SyncTransforms();
    }

    private static void RestoreEnabledStates(SpriteRenderer[] components, bool[] enabledStates)
    {
        if (components == null || enabledStates == null)
        {
            return;
        }

        int count = Mathf.Min(components.Length, enabledStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (components[i] != null)
            {
                components[i].enabled = enabledStates[i];
            }
        }
    }

    private static void RestoreEnabledStates(Collider2D[] components, bool[] enabledStates)
    {
        if (components == null || enabledStates == null)
        {
            return;
        }

        int count = Mathf.Min(components.Length, enabledStates.Length);
        for (int i = 0; i < count; i++)
        {
            if (components[i] != null)
            {
                components[i].enabled = enabledStates[i];
            }
        }
    }

    private AnimatorOverrideController CreatePossessionOverrideController(AnimationClip replacementClip)
    {
        if (replacementClip == null || ghostController == null)
        {
            return null;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(ghostController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip originalClip = overrides[i].Key;
            if (originalClip == null || originalClip.name != "hys_Ghost_possession")
            {
                continue;
            }

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, replacementClip);
            overrideController.ApplyOverrides(overrides);
            return overrideController;
        }

        return null;
    }

    private void FinishPossessionAnimation()
    {
        if (!IsPossessionAnimationPlaying)
        {
            return;
        }

        bool finishedSoulExit = soulExitAnimationPlaying;
        bool finishedDeath = deathAnimationPlaying;
        bool finishedGhostAppear = ghostAppearAnimationPlaying;
        bool shouldContinueToSoulExit = finishedDeath && continueSoulExitAfterDeath;

        if (shouldContinueToSoulExit)
        {
            // 사망 시에는 Die가 끝난 다음 같은 무기의 Soul Exit를 재생합니다.
            CaptureDelayedCorpseAnchor();
            PlaySoulExitAnimation(lastPossessedWeaponType);
            return;
        }

        if (finishedDeath)
        {
            // 안전망: 붕괴 표식 없이 Die가 끝난 경우에도 플레이어는 몬스터 Revive를 재생하지 않습니다.
            PlayGhostAppearAnimation();
            return;
        }

        if (finishedSoulExit)
        {
            // Soul Exit 자체가 영혼이 빠져나오는 연출이므로 Ghost Appear를 다시 재생하지 않습니다.
            CompleteDelayedCorpseRelease();
            FinishSoulExitToStanding();
            return;
        }

        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = false;
        deathAnimationPlaying = false;
        ghostAppearAnimationPlaying = false;
        continueSoulExitAfterDeath = false;
        collapsedBodyWasAlive = false;
        RestoreDelayedCorpsePresentation();
        activeStateShortNameHash = 0;
        activePossessionController = null;

        if (finishedDeath || finishedGhostAppear)
        {
            RestoreGhostStanding();
        }
        else
        {
            ApplyPlayerController(lastPossessedWeaponType);
        }

        RestoreMotionSystem();
    }

    private void FinishSoulExitToStanding()
    {
        // 무기별 영혼 이탈 모션의 마지막 모습에서 유령 대기 상태로 한 번만 전환합니다.
        suppressNextAutomaticGhostAppear = true;
        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = false;
        deathAnimationPlaying = false;
        ghostAppearAnimationPlaying = false;
        continueSoulExitAfterDeath = false;
        collapsedBodyWasAlive = false;
        activeStateShortNameHash = 0;
        activePossessionController = null;
        RestoreGhostStanding();
        RestoreMotionSystem();
    }

    private void RestoreMotionSystem()
    {
        if (motionSystem != null && restoreMotionSystemEnabled)
        {
            motionSystem.enabled = true;
        }
    }

    private void RestoreGhostStanding()
    {
        CacheReferences();
        if (animator == null || ghostController == null)
        {
            return;
        }

        animator.runtimeAnimatorController = ghostController;
        animator.Rebind();
        animator.Update(0f);
        int standingHash = Animator.StringToHash("Base Layer.Standing");
        if (animator.HasState(0, standingHash))
        {
            // 육체의 Soul 모션이 이미 해제 연출을 끝냈으므로 Ghost 등장 모션을 중복 재생하지 않습니다.
            animator.Play(standingHash, 0, 0f);
            animator.Update(0f);
        }
    }

    private void ApplyPlayerController(HWJ_WeaponType weaponType)
    {
        RuntimeAnimatorController playerController = possessionAnimationLibrary != null
            ? possessionAnimationLibrary.GetPlayerController(weaponType)
            : null;
        if (animator == null || playerController == null)
        {
            return;
        }

        animator.runtimeAnimatorController = playerController;
        animator.Rebind();
        animator.Update(0f);

        string weaponName = ResolveWeaponName(weaponType);
        int idleHash = Animator.StringToHash($"Base Layer.hys_{weaponName}_Idle");
        if (animator.HasState(0, idleHash))
        {
            animator.Play(idleHash, 0, 0f);
            animator.Update(0f);
        }
    }

    private static string ResolveWeaponName(HWJ_WeaponType weaponType)
    {
        switch (weaponType)
        {
            case HWJ_WeaponType.Axe:
                return "Axe";
            case HWJ_WeaponType.Bow:
                return "Bow";
            case HWJ_WeaponType.Lance:
                return "Lance";
            case HWJ_WeaponType.Shield:
                return "Shield";
            default:
                return "Sword";
        }
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (spriteRenderer == null)
        {
            if (animator != null)
            {
                spriteRenderer = animator.GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (playerBody == null)
        {
            playerBody = GetComponent<Rigidbody2D>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (collapseSystem == null)
        {
            collapseSystem = GetComponent<HWJ_CollapseSystem>();
        }

        if (possessionAnimationLibrary == null)
        {
            // Resources의 공용 표를 사용해 씬별 인스펙터 설정 없이 같은 무기별 빙의 모션을 사용합니다.
            possessionAnimationLibrary = Resources.Load<hys_PossessionAnimationLibrary>(
                "hys_PossessionAnimationLibrary");
        }
    }

    private void CacheGhostController()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        if (currentController is AnimatorOverrideController overrideController)
        {
            // 재생 중인 무기별 Override가 아니라 원본 Ghost Controller만 보관합니다.
            RuntimeAnimatorController baseController = overrideController.runtimeAnimatorController;
            if (baseController != null
                && baseController.name.StartsWith("hys_Ghost", System.StringComparison.Ordinal))
            {
                ghostController = baseController;
            }

            return;
        }

        if (currentController.name.StartsWith("hys_Ghost", System.StringComparison.Ordinal))
        {
            ghostController = currentController;
        }
    }

    private void SetTriggerIfPresent(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                animator.ResetTrigger(parameter.nameHash);
                animator.SetTrigger(parameter.nameHash);
                return;
            }
        }
    }

    private void ResetTriggerIfPresent(string parameterName)
    {
        if (animator == null)
        {
            return;
        }

        // 직접 상태를 재생하기 전에 동일한 Trigger 예약만 지워 중복 상태 진입을 막습니다.
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                animator.ResetTrigger(parameter.nameHash);
                return;
            }
        }
    }

    private void SetFloatIfPresent(string parameterName, float value)
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                animator.SetFloat(parameter.nameHash, value);
                return;
            }
        }
    }

    private void UpdateMentalAnimatorParameters()
    {
        if (animator == null || possessionSystem == null
            || !possessionSystem.TryGetActiveLiveMentalState(out HWJ_LivePossessionMentalState mentalState)
            || mentalState == null)
        {
            return;
        }

        lastMentalNormalized = mentalState.CurrentMentalRatio;
        SetFloatIfPresent("MentalNormalized", lastMentalNormalized);
    }
}
