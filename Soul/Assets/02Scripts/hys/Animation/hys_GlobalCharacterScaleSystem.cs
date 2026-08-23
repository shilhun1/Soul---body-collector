using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 모든 씬의 플레이어, 일반 몬스터, 보스를 기존 크기의 60%로 한 번만 축소합니다.
[DefaultExecutionOrder(-9000)]
public sealed class hys_GlobalCharacterScaleSystem : MonoBehaviour
{
    public const float CharacterScaleMultiplier = 0.6f;

    private const float SpawnScanIntervalSeconds = 0.25f;
    private static hys_GlobalCharacterScaleSystem instance;
    private Coroutine spawnScanRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstaller()
    {
        if (instance != null)
        {
            return;
        }

        GameObject installerObject = new GameObject("hys_GlobalCharacterScaleSystem");
        installerObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(installerObject);
        instance = installerObject.AddComponent<hys_GlobalCharacterScaleSystem>();
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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        spawnScanRoutine = StartCoroutine(ScanSpawnedCharactersRoutine());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (spawnScanRoutine != null)
        {
            StopCoroutine(spawnScanRoutine);
            spawnScanRoutine = null;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScaleToLoadedCharacters();
    }

    private static IEnumerator ScanSpawnedCharactersRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(SpawnScanIntervalSeconds);

        while (true)
        {
            ApplyScaleToLoadedCharacters();
            yield return wait;
        }
    }

    private static void ApplyScaleToLoadedCharacters()
    {
        HWJ_RootObjectDataResolver[] resolvers =
            FindObjectsByType<HWJ_RootObjectDataResolver>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver == null || !ShouldScale(resolver.ObjectType))
            {
                continue;
            }

            hys_GlobalCharacterScaleMarker marker =
                resolver.GetComponent<hys_GlobalCharacterScaleMarker>();

            if (marker == null)
            {
                marker = resolver.gameObject.AddComponent<hys_GlobalCharacterScaleMarker>();
            }

            marker.ApplyOnce(CharacterScaleMultiplier);
        }
    }

    private static bool ShouldScale(HWJ_ObjectType objectType)
    {
        return objectType == HWJ_ObjectType.Player
            || objectType == HWJ_ObjectType.Enemy
            || objectType == HWJ_ObjectType.Boss;
    }
}

// 풀링과 씬 재검색 중 같은 캐릭터가 반복 축소되지 않도록 최초 배율을 보관합니다.
[DisallowMultipleComponent]
internal sealed class hys_GlobalCharacterScaleMarker : MonoBehaviour
{
    [SerializeField, HideInInspector] private bool scaleApplied;
    [SerializeField, HideInInspector] private Vector3 originalLocalScale;

    public void ApplyOnce(float multiplier)
    {
        if (scaleApplied)
        {
            return;
        }

        originalLocalScale = transform.localScale;
        transform.localScale = new Vector3(
            originalLocalScale.x * multiplier,
            originalLocalScale.y * multiplier,
            originalLocalScale.z);
        scaleApplied = true;
    }
}
