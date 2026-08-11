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
    [SerializeField] private bool preserveCorpseVisualAfterDeath = true;
    [SerializeField, Min(0f)] private float corpseVisualLifetimeSeconds;
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
    private float lastMentalNormalized = 1f;
    private bool bodyCollapsePending;
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
        continueSoulExitAfterDeath = false;
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
            PlayGhostAppearAnimation();
            return;
        }

        activePossessionController = CreatePossessionOverrideController(soulExitClip);
        if (activePossessionController == null)
        {
            Debug.LogWarning($"[hys Animator] {weaponType} 전용 빙의 해제 클립을 찾지 못했습니다.", this);
            RestoreGhostStanding();
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
            RestoreGhostStanding();
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

    private void CreateCorpseVisualSnapshot()
    {
        CacheReferences();
        if (!preserveCorpseVisualAfterDeath || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        // Die의 마지막 프레임을 별도 렌더러로 남겨 영혼이 나온 뒤에도 시체가 사라지지 않게 합니다.
        GameObject corpseObject = new GameObject($"hys_Runtime_CorpseVisual_{ResolveWeaponName(lastPossessedWeaponType)}");
        corpseObject.transform.SetPositionAndRotation(
            spriteRenderer.transform.position,
            spriteRenderer.transform.rotation);
        corpseObject.transform.localScale = spriteRenderer.transform.lossyScale;

        SpriteRenderer corpseRenderer = corpseObject.AddComponent<SpriteRenderer>();
        corpseRenderer.sprite = spriteRenderer.sprite;
        corpseRenderer.color = spriteRenderer.color;
        corpseRenderer.flipX = spriteRenderer.flipX;
        corpseRenderer.flipY = spriteRenderer.flipY;
        corpseRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        corpseRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        // 영혼 렌더러가 시체 앞에서 확실히 보이도록 시체를 한 단계 뒤에 둡니다.
        corpseRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        corpseRenderer.maskInteraction = spriteRenderer.maskInteraction;
        corpseRenderer.spriteSortPoint = spriteRenderer.spriteSortPoint;

        if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(corpseObject, gameObject.scene);
        }

        if (corpseVisualLifetimeSeconds > 0f)
        {
            Destroy(corpseObject, corpseVisualLifetimeSeconds);
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
            CreateCorpseVisualSnapshot();
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
            // 몸의 Soul Exit가 끝난 위치에서 실제 영혼이 나타나는 모션을 이어 재생합니다.
            PlayGhostAppearAnimation();
            return;
        }

        possessionAnimationPlaying = false;
        soulExitAnimationPlaying = false;
        deathAnimationPlaying = false;
        ghostAppearAnimationPlaying = false;
        continueSoulExitAfterDeath = false;
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
