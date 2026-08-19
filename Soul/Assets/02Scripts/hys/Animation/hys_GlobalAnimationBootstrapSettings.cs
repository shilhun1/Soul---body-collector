using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 모든 게임 씬이 같은 RuntimeReady 플레이어와 hys 애니메이션 구성을 사용하도록 연결합니다.
public sealed class hys_GlobalAnimationBootstrapSettings : ScriptableObject
{
    [SerializeField] private GameObject playerPrefab;

    public GameObject PlayerPrefab => playerPrefab;
}

// 씬을 직접 실행하거나 씬 전환으로 들어와도 구형 플레이어를 애니메이션 적용 플레이어로 교체합니다.
[DefaultExecutionOrder(-10000)]
internal sealed class hys_GlobalAnimationBootstrap : MonoBehaviour
{
    private const string SettingsResourcePath = "hys/hys_GlobalAnimationBootstrapSettings";
    private static hys_GlobalAnimationBootstrap instance;
    private hys_GlobalAnimationBootstrapSettings settings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstaller()
    {
        if (instance != null)
        {
            return;
        }

        GameObject installerObject = new GameObject("hys_GlobalAnimationBootstrap");
        installerObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(installerObject);
        instance = installerObject.AddComponent<hys_GlobalAnimationBootstrap>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        settings = Resources.Load<hys_GlobalAnimationBootstrapSettings>(SettingsResourcePath);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAnimatedPlayer(scene);
    }

    private void EnsureAnimatedPlayer(Scene loadedScene)
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        HWJ_RootObjectDataResolver animatedPlayer = ResolveRegisteredRuntimePlayer();
        List<HWJ_RootObjectDataResolver> scenePlayers =
            new List<HWJ_RootObjectDataResolver>();

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];
            if (resolver == null || resolver.ObjectType != HWJ_ObjectType.Player)
            {
                continue;
            }

            if (resolver.gameObject.scene != loadedScene)
            {
                continue;
            }

            scenePlayers.Add(resolver);

            if (animatedPlayer == null && IsRuntimeReadyPlayer(resolver))
            {
                // 씬에 이미 Animator와 HWJ 런타임 구성이 있으면 정상 플레이어이므로 교체하지 않습니다.
                animatedPlayer = resolver;
            }
        }

        // 타이틀처럼 플레이어가 없는 씬에는 플레이어를 강제로 만들지 않습니다.
        if (animatedPlayer == null && scenePlayers.Count == 0)
        {
            return;
        }

        if (animatedPlayer == null)
        {
            animatedPlayer = CreateAnimatedPlayer(scenePlayers[0]);
            if (animatedPlayer == null)
            {
                return;
            }
        }

        EnsureFacingCoordinator(animatedPlayer);

        // 씬에 남아 있는 정적 스프라이트 플레이어는 즉시 비활성화해 중복 입력과 중복 렌더링을 막습니다.
        for (int i = 0; i < scenePlayers.Count; i++)
        {
            HWJ_RootObjectDataResolver fallback = scenePlayers[i];
            if (fallback == null || fallback == animatedPlayer)
            {
                continue;
            }

            fallback.gameObject.SetActive(false);
            Destroy(fallback.gameObject);
        }

        if (HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Manager.RegisterPlayer(animatedPlayer);
        }
    }

    private static HWJ_RootObjectDataResolver ResolveRegisteredRuntimePlayer()
    {
        if (!HWJ_GameAccess.HasManager)
        {
            return null;
        }

        HWJ_RootObjectDataResolver registeredPlayer = HWJ_GameAccess.Manager.PlayerResolver;
        return IsRuntimeReadyPlayer(registeredPlayer) ? registeredPlayer : null;
    }

    private static bool IsRuntimeReadyPlayer(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.ObjectType != HWJ_ObjectType.Player)
        {
            return false;
        }

        // hys 전용 Animator가 없어도 HWJ 모션과 영혼 시스템 및 Animator가 있으면 완성된 플레이어입니다.
        // 기존에는 hys_Player_Animator 하나만 검사해 정상 유령 플레이어를 Sword 테스트 프리팹으로 바꿨습니다.
        return resolver.GetComponent<hys_Player_Animator>() != null
            || resolver.GetComponent<HWJ_CharacterMotionSystem>() != null
                && resolver.GetComponent<HWJ_SoulSystem>() != null
                && resolver.GetComponentInChildren<Animator>(true) != null;
    }

    private static void EnsureFacingCoordinator(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.GetComponent<hys_PlayerFacingCoordinator>() != null)
        {
            return;
        }

        resolver.gameObject.AddComponent<hys_PlayerFacingCoordinator>();
    }

    private HWJ_RootObjectDataResolver CreateAnimatedPlayer(HWJ_RootObjectDataResolver fallback)
    {
        if (settings == null)
        {
            settings = Resources.Load<hys_GlobalAnimationBootstrapSettings>(SettingsResourcePath);
        }

        if (settings == null || settings.PlayerPrefab == null)
        {
            Debug.LogError("[hys Animation] 전역 애니메이션 플레이어 설정 또는 프리팹을 찾지 못했습니다.");
            return null;
        }

        Vector3 position = fallback != null ? fallback.transform.position : Vector3.zero;
        Quaternion rotation = fallback != null ? fallback.transform.rotation : Quaternion.identity;
        GameObject playerObject = Instantiate(settings.PlayerPrefab, position, rotation);
        playerObject.name = settings.PlayerPrefab.name;

        HWJ_RootObjectDataResolver resolver = playerObject.GetComponent<HWJ_RootObjectDataResolver>();
        if (resolver != null)
        {
            return resolver;
        }

        Debug.LogError("[hys Animation] RuntimeReady 플레이어에 HWJ_RootObjectDataResolver가 없습니다.", playerObject);
        Destroy(playerObject);
        return null;
    }
}

// Sword/Axe/Bow/Lance/Shield의 원본 방향과 현재 바라보는 방향을 분리해 빙의 후 좌우 반전을 막습니다.
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
internal sealed class hys_PlayerFacingCoordinator : MonoBehaviour
{
    private HWJ_RootObjectDataResolver dataResolver;
    private HWJ_CharacterMotionSystem motionSystem;
    private HWJ_PossessionSystem possessionSystem;
    private HWJ_SoulSystem soulSystem;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private HWJ_WeaponType lastWeaponType = HWJ_WeaponType.None;
    private HWJ_SoulRuntimeState lastSoulState = HWJ_SoulRuntimeState.Body;
    private RuntimeAnimatorController lastAnimatorController;
    private bool hasObservedState;
    private bool hasPendingFacing;
    private bool pendingFaceLeft;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        HWJ_GameplayEvents.PossessionChanged += HandlePossessionChanged;
        QueueCurrentFacingNormalization();
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.PossessionChanged -= HandlePossessionChanged;
    }

    private void LateUpdate()
    {
        CacheReferences();

        HWJ_WeaponType weaponType = ResolveWeaponType();
        HWJ_SoulRuntimeState soulState = soulSystem != null
            ? soulSystem.CurrentState
            : HWJ_SoulRuntimeState.Body;
        RuntimeAnimatorController currentController = animator != null
            ? animator.runtimeAnimatorController
            : null;

        bool enteredBody = hasObservedState
            && lastSoulState != HWJ_SoulRuntimeState.Body
            && soulState == HWJ_SoulRuntimeState.Body;
        bool bodyVisualChanged = soulState == HWJ_SoulRuntimeState.Body
            && (weaponType != lastWeaponType || currentController != lastAnimatorController);

        if (!hasPendingFacing && (enteredBody || bodyVisualChanged))
        {
            QueueCurrentFacingNormalization();
        }

        if (hasPendingFacing && soulState == HWJ_SoulRuntimeState.Body)
        {
            ApplyCanonicalFacing(weaponType, pendingFaceLeft);
            hasPendingFacing = false;
            currentController = animator != null ? animator.runtimeAnimatorController : null;
        }

        lastWeaponType = weaponType;
        lastSoulState = soulState;
        lastAnimatorController = currentController;
        hasObservedState = true;
    }

    private void HandlePossessionChanged(HWJ_PossessionEvent possessionEvent)
    {
        if (!possessionEvent.Possessed || possessionEvent.PossessionSystem != possessionSystem)
        {
            return;
        }

        HWJ_WeaponType weaponType = possessionEvent.BodyResolver != null
            ? possessionEvent.BodyResolver.WeaponType
            : ResolveWeaponType();
        SpriteRenderer targetRenderer = possessionEvent.BodyResolver != null
            ? possessionEvent.BodyResolver.GetComponentInChildren<SpriteRenderer>(true)
            : null;

        // 몬스터가 현재 왼쪽을 보던 상태는 유지하되 그 값을 오른쪽 원본 기준으로 저장하지 않습니다.
        pendingFaceLeft = targetRenderer != null
            ? targetRenderer.flipX != GetRightFacingFlipX(weaponType)
            : ResolveCurrentFaceLeft();
        hasPendingFacing = true;
    }

    private void QueueCurrentFacingNormalization()
    {
        CacheReferences();
        pendingFaceLeft = ResolveCurrentFaceLeft();
        hasPendingFacing = true;
    }

    private void ApplyCanonicalFacing(HWJ_WeaponType weaponType, bool faceLeft)
    {
        if (spriteRenderer == null || motionSystem == null)
        {
            return;
        }

        // 다섯 무기 원본은 flipX=false일 때 오른쪽을 바라보도록 제작되어 있습니다.
        spriteRenderer.flipX = GetRightFacingFlipX(weaponType);
        motionSystem.RefreshFacingBaseline();
        motionSystem.FaceDirection(faceLeft ? -1f : 1f);
    }

    private bool ResolveCurrentFaceLeft()
    {
        if (motionSystem != null)
        {
            return motionSystem.ResolveCurrentFacingDirection() < 0f;
        }

        return spriteRenderer != null && spriteRenderer.flipX;
    }

    private HWJ_WeaponType ResolveWeaponType()
    {
        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            return possessionSystem.CurrentWeaponType;
        }

        return dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
    }

    private static bool GetRightFacingFlipX(HWJ_WeaponType weaponType)
    {
        switch (weaponType)
        {
            case HWJ_WeaponType.Sword:
            case HWJ_WeaponType.Axe:
            case HWJ_WeaponType.Bow:
            case HWJ_WeaponType.Lance:
            case HWJ_WeaponType.Shield:
            default:
                return false;
        }
    }

    private void CacheReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }
}
