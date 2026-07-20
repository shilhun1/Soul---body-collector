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

    [Header("Core Services")]
    [SerializeField] private HWJ_GameplayDatabaseSO database;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private HWJ_SpawnerSystem spawner;

    [Space(8f)]
    [Header("Lifetime")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Space(8f)]
    [Header("Player Runtime References")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_RuntimeStatusSystem playerStatus;
    [SerializeField] private HWJ_LevelUpSystem playerLevel;
    [SerializeField] private HWJ_SoulSystem playerSoul;
    [SerializeField] private HWJ_BodyDecaySystem playerBodyDecay;
    [SerializeField] private HWJ_PossessionSystem playerPossession;

    [Space(8f)]
    [Header("Player Runtime Auto Wiring")]
    [SerializeField] private bool autoFindPlayerResolverInScene = true;
    [SerializeField] private bool autoAddMissingPlayerCoreSystems = true;

    [Space(8f)]
    [Header("Player Runtime Persistence")]
    [SerializeField] private bool preservePlayerRuntimeAcrossScenes = true;
    [SerializeField] private bool hasPlayerRuntimeSnapshot;
    [SerializeField] private float savedCurrentHp;
    [SerializeField] private float savedSoulHp;
    [SerializeField] private float savedPossessedBodyHp;
    [SerializeField] private bool hasBodyDecaySnapshot;
    [SerializeField] private float savedBodyDecayValue;
    [SerializeField] private bool hasPlayerGrowthSnapshot;
    [SerializeField] private int savedLevel;
    [SerializeField] private int savedExperience;
    [SerializeField] private int savedSkillPoint;
    [SerializeField] private bool hasPlayerPossessionSnapshot;
    [SerializeField] private bool savedHasActivePossessedBody;
    [SerializeField] private string savedPossessedRootObjectId;
    [SerializeField] private HWJ_RootObjectDataSO savedPossessedRootObjectData;
    [SerializeField] private bool savedHasPossessedVisualSnapshot;
    [SerializeField] private Sprite savedPossessedVisualSprite;
    [SerializeField] private Color savedPossessedVisualColor = Color.white;
    [SerializeField] private bool savedPossessedVisualFlipX;
    [SerializeField] private bool savedPossessedVisualFlipY;
    [SerializeField] private RuntimeAnimatorController savedPossessedAnimatorController;

    [Space(8f)]
    [Header("Pause")]
    [SerializeField] private bool togglePauseWithEscape = true;
    [SerializeField] private bool isPaused;
    [SerializeField] private float timeScaleBeforePause = 1f;

    public HWJ_GameplayDatabaseSO Database => database;
    public HWJ_ObjectPoolSystem ObjectPool => objectPool;
    public HWJ_SpawnerSystem Spawner => spawner;
    public HWJ_RootObjectDataResolver PlayerResolver => playerResolver;
    public HWJ_PlayerInputSystem PlayerInput => playerInput;
    public HWJ_RuntimeStatusSystem PlayerStatus => playerStatus;
    public HWJ_LevelUpSystem PlayerLevel => playerLevel;
    public HWJ_SoulSystem PlayerSoul => playerSoul;
    public HWJ_BodyDecaySystem PlayerBodyDecay => playerBodyDecay;
    public HWJ_PossessionSystem PlayerPossession => playerPossession;
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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

        if (playerInput == null)
        {
            playerInput = GetComponentInChildren<HWJ_PlayerInputSystem>();
        }

        if (playerResolver == null && autoFindPlayerResolverInScene)
        {
            playerResolver = FindPlayerResolverInScene();
        }

        if (playerResolver != null)
        {
            EnsurePlayerCoreSystems(playerResolver);
            CachePlayerRuntimeReferencesFromResolver(playerResolver);
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
        EnsurePlayerCoreSystems(playerResolver);

        if (resolver == null)
        {
            ClearRegisteredPlayerRuntimeReferences(false);
            return;
        }

        CachePlayerRuntimeReferencesFromResolver(resolver);

        if (preservePlayerRuntimeAcrossScenes)
        {
            ApplyPlayerRuntimeSnapshot();
        }
    }

    public void RegisterPlayerInput(HWJ_PlayerInputSystem inputSystem)
    {
        playerInput = inputSystem;
    }

    private void CachePlayerRuntimeReferencesFromResolver(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null)
        {
            ClearRegisteredPlayerRuntimeReferences(false);
            return;
        }

        HWJ_PlayerInputSystem resolverInput = resolver.GetComponent<HWJ_PlayerInputSystem>();

        if (resolverInput != null)
        {
            playerInput = resolverInput;
        }
        else if (playerInput == null)
        {
            playerInput = GetComponentInChildren<HWJ_PlayerInputSystem>();
        }

        playerStatus = resolver.GetComponent<HWJ_RuntimeStatusSystem>();
        playerLevel = resolver.GetComponent<HWJ_LevelUpSystem>();
        playerSoul = resolver.GetComponent<HWJ_SoulSystem>();
        playerBodyDecay = resolver.GetComponent<HWJ_BodyDecaySystem>();
        playerPossession = resolver.GetComponent<HWJ_PossessionSystem>();
    }

    private void EnsurePlayerCoreSystems(HWJ_RootObjectDataResolver resolver)
    {
        if (!autoAddMissingPlayerCoreSystems || resolver == null)
        {
            return;
        }

        GameObject playerObject = resolver.gameObject;

        // These components are required for the current vertical slice: spirit, possession, decay, collapse, and rediscovery.
        EnsureComponent<HWJ_RuntimeStatusSystem>(playerObject);
        EnsureComponent<HWJ_SoulSystem>(playerObject);
        EnsureComponent<HWJ_PossessedBodySystem>(playerObject);
        EnsureComponent<HWJ_PossessionSystem>(playerObject);
        EnsureComponent<HWJ_BodyDecaySystem>(playerObject);
        EnsureComponent<HWJ_CollapseSystem>(playerObject);
        EnsureComponent<HWJ_BodyDiscoverySystem>(playerObject);
    }

    private static T EnsureComponent<T>(GameObject owner) where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : owner.AddComponent<T>();
    }

    private static HWJ_RootObjectDataResolver FindPlayerResolverInScene()
    {
        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            HWJ_RootObjectDataResolver resolver = resolvers[i];

            if (resolver != null
                && resolver.RootObjectData != null
                && resolver.ObjectType == HWJ_ObjectType.Player)
            {
                return resolver;
            }
        }

        return null;
    }

    private void ClearRegisteredPlayerRuntimeReferences(bool clearInput)
    {
        if (clearInput)
        {
            playerInput = null;
        }

        playerStatus = null;
        playerLevel = null;
        playerSoul = null;
        playerBodyDecay = null;
        playerPossession = null;
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

        if (playerPossession == null && playerResolver != null)
        {
            playerPossession = playerResolver.GetComponent<HWJ_PossessionSystem>();
        }

        CapturePlayerPossessionSnapshot();

        if (playerLevel == null && playerResolver != null)
        {
            playerLevel = playerResolver.GetComponent<HWJ_LevelUpSystem>();
        }

        if (playerLevel != null)
        {
            savedLevel = playerLevel.CurrentLevel;
            savedExperience = playerLevel.CurrentExperience;
            savedSkillPoint = playerLevel.SkillPoint;
            hasPlayerGrowthSnapshot = true;
        }
    }

    private void ApplyPlayerRuntimeSnapshot()
    {
        ApplyPlayerPossessionSnapshot();

        if (playerStatus != null && hasPlayerRuntimeSnapshot)
        {
            playerStatus.RestoreHpSnapshot(savedCurrentHp, savedSoulHp, savedPossessedBodyHp);
        }

        if (playerBodyDecay != null && hasBodyDecaySnapshot)
        {
            playerBodyDecay.RestoreDecaySnapshot(savedBodyDecayValue);
        }

        if (playerLevel != null && hasPlayerGrowthSnapshot)
        {
            playerLevel.RestoreProgress(savedLevel, savedExperience, savedSkillPoint);
        }
    }

    private void CapturePlayerPossessionSnapshot()
    {
        if (playerPossession == null)
        {
            return;
        }

        hasPlayerPossessionSnapshot = true;
        savedHasActivePossessedBody = playerPossession.TryGetPossessedRootObjectId(out savedPossessedRootObjectId);

        if (!savedHasActivePossessedBody)
        {
            savedPossessedRootObjectId = null;
            savedPossessedRootObjectData = null;
            savedHasPossessedVisualSnapshot = false;
            savedPossessedVisualSprite = null;
            savedPossessedAnimatorController = null;
            return;
        }

        savedPossessedRootObjectData = playerPossession.PossessedBodyResolver != null
            ? playerPossession.PossessedBodyResolver.RootObjectData
            : null;

        savedHasPossessedVisualSnapshot = playerPossession.TryGetPossessedVisualSnapshot(
            out savedPossessedVisualSprite,
            out savedPossessedVisualColor,
            out savedPossessedVisualFlipX,
            out savedPossessedVisualFlipY,
            out savedPossessedAnimatorController);
    }

    private void ApplyPlayerPossessionSnapshot()
    {
        if (!hasPlayerPossessionSnapshot || playerPossession == null)
        {
            return;
        }

        if (!savedHasActivePossessedBody)
        {
            playerPossession.ClearPossessedBody(false, false, false);
            return;
        }

        HWJ_RootObjectDataSO rootObjectData = savedPossessedRootObjectData;

        if (rootObjectData == null
            && !string.IsNullOrEmpty(savedPossessedRootObjectId))
        {
            TryGetRootObject(savedPossessedRootObjectId, out rootObjectData);
        }

        if (rootObjectData == null)
        {
            return;
        }

        playerPossession.RestorePossessedBody(
            rootObjectData,
            savedPossessedVisualSprite,
            savedPossessedVisualColor,
            savedPossessedVisualFlipX,
            savedPossessedVisualFlipY,
            savedPossessedAnimatorController,
            savedHasPossessedVisualSnapshot,
            false,
            false,
            false);
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

    public bool TryGetLevelTable(string tableId, out HWJ_LevelUpDataSO levelTable)
    {
        levelTable = null;
        return database != null && database.TryGetLevelTable(tableId, out levelTable);
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

    public bool TryGetSkillNode(string nodeId, out HWJ_SkillNodeDataSO skillNodeData)
    {
        skillNodeData = null;
        return database != null && database.TryGetSkillNode(nodeId, out skillNodeData);
    }

    public bool TryGetSkillNodeBySkillAction(string skillActionId, out HWJ_SkillNodeDataSO skillNodeData)
    {
        skillNodeData = null;
        return database != null && database.TryGetSkillNodeBySkillAction(skillActionId, out skillNodeData);
    }

    public bool TryGetGameplayRule(string ruleId, out HWJ_GameplayRuleSO gameplayRule)
    {
        gameplayRule = null;
        return database != null && database.TryGetGameplayRule(ruleId, out gameplayRule);
    }

    public bool TryGetRuleExecutionCore(string executionCoreId, out HWJ_RuleExecutionCoreSO executionCore)
    {
        executionCore = null;
        return database != null && database.TryGetRuleExecutionCore(executionCoreId, out executionCore);
    }

    public bool TryValidateGameplayDatabase(out HWJ_GameDataRegistryReport report)
    {
        report = null;

        if (database == null)
        {
            return false;
        }

        report = database.ValidateRegistryIds();
        return report != null;
    }
}
