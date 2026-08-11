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

        HWJ_RootObjectDataResolver animatedPlayer = null;
        List<HWJ_RootObjectDataResolver> sceneFallbackPlayers =
            new List<HWJ_RootObjectDataResolver>();

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];
            if (resolver == null || resolver.ObjectType != HWJ_ObjectType.Player)
            {
                continue;
            }

            if (resolver.GetComponent<hys_Player_Animator>() != null)
            {
                animatedPlayer = resolver;
                continue;
            }

            if (resolver.gameObject.scene == loadedScene)
            {
                sceneFallbackPlayers.Add(resolver);
            }
        }

        // 타이틀처럼 플레이어가 없는 씬에는 플레이어를 강제로 만들지 않습니다.
        if (animatedPlayer == null && sceneFallbackPlayers.Count == 0)
        {
            return;
        }

        if (animatedPlayer == null)
        {
            animatedPlayer = CreateAnimatedPlayer(sceneFallbackPlayers[0]);
            if (animatedPlayer == null)
            {
                return;
            }
        }

        // 씬에 남아 있는 정적 스프라이트 플레이어는 즉시 비활성화해 중복 입력과 중복 렌더링을 막습니다.
        for (int i = 0; i < sceneFallbackPlayers.Count; i++)
        {
            HWJ_RootObjectDataResolver fallback = sceneFallbackPlayers[i];
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
