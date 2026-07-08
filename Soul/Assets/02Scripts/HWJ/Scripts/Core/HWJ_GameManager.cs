using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// HWJ 시스템들의 중심 접근점입니다.
/// 데이터베이스, 오브젝트 풀, 스포너, 플레이어 상태처럼 여러 시스템이 공통으로 참조하는 요소를 한 곳에서 관리합니다.
/// </summary>
public class HWJ_GameManager : MonoBehaviour
{
    public static HWJ_GameManager Instance { get; private set; }

    [Header("Core")]
    [SerializeField] private HWJ_GameplayDatabaseSO database;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private HWJ_SpawnerSystem spawner;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Player Runtime")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem playerStatus;
    [SerializeField] private HWJ_LevelUpSystem playerLevel;
    [SerializeField] private HWJ_SoulSystem playerSoul;
    [SerializeField] private HWJ_BodyDecaySystem playerBodyDecay;

    [Header("Player Runtime Persistence")]
    [SerializeField] private bool preservePlayerRuntimeAcrossScenes = true;
    [SerializeField] private bool hasPlayerRuntimeSnapshot;
    [SerializeField] private float savedCurrentHp;
    [SerializeField] private float savedSoulHp;
    [SerializeField] private float savedPossessedBodyHp;
    [SerializeField] private bool hasBodyDecaySnapshot;
    [SerializeField] private float savedBodyDecayValue;

    [Header("Pause")]
    [SerializeField] private bool togglePauseWithEscape = true;
    [SerializeField] private bool isPaused;
    [SerializeField] private float timeScaleBeforePause = 1f;

    public HWJ_GameplayDatabaseSO Database => database;
    public HWJ_ObjectPoolSystem ObjectPool => objectPool;
    public HWJ_SpawnerSystem Spawner => spawner;
    public HWJ_RootObjectDataResolver PlayerResolver => playerResolver;
    public HWJ_RuntimeStatusSystem PlayerStatus => playerStatus;
    public HWJ_LevelUpSystem PlayerLevel => playerLevel;
    public HWJ_SoulSystem PlayerSoul => playerSoul;
    public HWJ_BodyDecaySystem PlayerBodyDecay => playerBodyDecay;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveSceneReferences();
        ApplyDatabaseLinks();

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (objectPool != null)
        {
            objectPool.Prewarm();
        }
    }

    private void Update()
    {
        if (togglePauseWithEscape && WasPausePressedThisFrame())
        {
            SetPaused(!isPaused);
        }
    }

    private void LateUpdate()
    {
        SavePlayerRuntimeSnapshot();
    }

    public void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;

        if (isPaused)
        {
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            return;
        }

        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
    }

    private static bool WasPausePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void SavePlayerRuntimeSnapshot()
    {
        if (!preservePlayerRuntimeAcrossScenes)
        {
            return;
        }

        CapturePlayerRuntimeSnapshot();
    }

    /// <summary>
    /// GameplayDatabase에 모아둔 시스템 데이터를 런타임 시스템에 전달합니다.
    /// 현재는 ObjectPoolData를 ObjectPoolSystem에 연결해서 인스펙터 중복 연결을 줄입니다.
    /// </summary>
    public void ApplyDatabaseLinks()
    {
        if (database == null || objectPool == null)
        {
            return;
        }

        if (objectPool.PoolData == null && database.ObjectPoolData != null)
        {
            objectPool.SetPoolData(database.ObjectPoolData);
        }
    }

    /// <summary>
    /// GameManager 하위에 배치된 핵심 시스템을 자동으로 연결합니다.
    /// Inspector에 직접 넣는 방식을 우선하고, 비어 있을 때만 하위 컴포넌트를 찾습니다.
    /// </summary>
    public void ResolveSceneReferences()
    {
        if (objectPool == null)
        {
            objectPool = GetComponentInChildren<HWJ_ObjectPoolSystem>();
        }

        if (spawner == null)
        {
            spawner = GetComponentInChildren<HWJ_SpawnerSystem>();
        }
    }

    /// <summary>
    /// 런타임에 생성되거나 교체된 플레이어를 GameManager에 등록합니다.
    /// UI, 카메라, 레벨업, 소울 시스템이 같은 플레이어 참조를 공유할 때 사용합니다.
    /// </summary>
    public void RegisterPlayer(HWJ_RootObjectDataResolver resolver)
    {
        if (preservePlayerRuntimeAcrossScenes)
        {
            SavePlayerRuntimeSnapshot();
        }

        playerResolver = resolver;

        if (resolver == null)
        {
            playerStatus = null;
            playerLevel = null;
            playerSoul = null;
            playerBodyDecay = null;
            return;
        }

        playerStatus = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
        playerLevel = resolver.GetComponent<HWJ_LevelUpSystem>();
        playerSoul = resolver.GetComponent<HWJ_SoulSystem>();
        playerBodyDecay = resolver.GetComponent<HWJ_BodyDecaySystem>();

        if (preservePlayerRuntimeAcrossScenes)
        {
            ApplyPlayerRuntimeSnapshot();
        }
    }

    private void CapturePlayerRuntimeSnapshot()
    {
        if (playerStatus == null)
        {
            return;
        }

        playerStatus.CacheCurrentHpForActiveState();
        savedCurrentHp = playerStatus.CurrentHp;
        savedSoulHp = playerStatus.SoulHp;
        savedPossessedBodyHp = playerStatus.PossessedBodyHp;
        hasPlayerRuntimeSnapshot = true;

        if (playerBodyDecay == null && playerResolver != null)
        {
            playerBodyDecay = playerResolver.GetComponent<HWJ_BodyDecaySystem>();
        }

        if (playerBodyDecay != null)
        {
            savedBodyDecayValue = playerBodyDecay.CurrentDecayValue;
            hasBodyDecaySnapshot = true;
        }
    }

    private void ApplyPlayerRuntimeSnapshot()
    {
        if (playerStatus != null && hasPlayerRuntimeSnapshot)
        {
            playerStatus.RestoreHpSnapshot(savedCurrentHp, savedSoulHp, savedPossessedBodyHp);
        }

        if (playerBodyDecay != null && hasBodyDecaySnapshot)
        {
            playerBodyDecay.RestoreDecaySnapshot(savedBodyDecayValue);
        }
    }

    /// <summary>
    /// 풀을 우선 사용해 오브젝트를 생성합니다.
    /// ObjectPoolSystem이 없을 때만 중앙 fallback으로 Instantiate를 사용해서 생성 지점을 한 곳으로 모읍니다.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        if (objectPool != null)
        {
            return objectPool.Spawn(prefab, position, rotation, parent);
        }

        return Instantiate(prefab, position, rotation, parent);
    }

    /// <summary>
    /// 풀링 오브젝트는 풀로 반환하고, 풀링 정보가 없는 오브젝트는 중앙 fallback으로 비활성화합니다.
    /// 각 시스템이 Destroy를 직접 호출하지 않도록 이 메서드를 사용합니다.
    /// </summary>
    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (objectPool != null)
        {
            objectPool.Despawn(instance);
            return;
        }

        instance.SetActive(false);
    }

    /// <summary>
    /// 외부 시스템이 데이터베이스를 통해 RootObjectData를 찾을 때 사용하는 편의 메서드입니다.
    /// </summary>
    public bool TryGetRootObject(string objectId, out HWJ_RootObjectDataSO rootObjectData)
    {
        rootObjectData = null;
        return database != null && database.TryGetRootObject(objectId, out rootObjectData);
    }

    /// <summary>
    /// 능력치 구슬 ID로 데이터베이스에서 구슬 데이터를 찾습니다.
    /// 외부 상호작용 시스템이 데이터베이스를 직접 들고 있지 않아도 됩니다.
    /// </summary>
    public bool TryGetStatOrb(string orbId, out HWJ_StatOrbDataSO statOrbData)
    {
        statOrbData = null;
        return database != null && database.TryGetStatOrb(orbId, out statOrbData);
    }

    /// <summary>
    /// 스킬 행동 ID로 데이터베이스에서 스킬 행동 데이터를 찾습니다.
    /// 플레이어, 적, 보스 스킬 시스템이 같은 조회 경로를 사용할 수 있습니다.
    /// </summary>
    public bool TryGetSkillAction(string skillActionId, out HWJ_SkillActionDataSO skillActionData)
    {
        skillActionData = null;
        return database != null && database.TryGetSkillAction(skillActionId, out skillActionData);
    }
}
