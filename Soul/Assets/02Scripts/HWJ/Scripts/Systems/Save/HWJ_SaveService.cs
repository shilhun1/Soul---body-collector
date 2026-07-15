using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 실제 저장 파일 생성, 읽기, 런타임 적용을 담당하는 서비스입니다.
/// GameManager를 더 키우지 않기 위해 저장 책임만 별도 컴포넌트로 분리합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_SaveService : MonoBehaviour
{
    private static HWJ_SaveService activeService;

    [Header("Save Target")]
    [SerializeField] private string saveSlotId = "slot_01";
    [SerializeField] private string saveFolderName = "HWJ";
    [SerializeField] private bool prettyPrintJson = true;

    [Space(8f)]
    [Header("Runtime Sources")]
    [SerializeField] private HWJ_RuntimeObjectContext playerRuntimeContext;
    [SerializeField] private HWJ_PlayerInputSystem playerInputSystem;
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;
    [SerializeField] private HWJ_GameplayDatabaseSO gameplayDatabase;

    [Space(8f)]
    [Header("Tracked Progression")]
    [SerializeField] private HWJ_SaveProgressionData trackedProgression = new HWJ_SaveProgressionData();
    [SerializeField] private HWJ_GameSaveData lastLoadedSaveData;
    [SerializeField] private string lastOperationMessage;

    private HWJ_SaveOperationResult lastOperationResult;

    public string SaveSlotId => saveSlotId;
    public HWJ_SaveProgressionData TrackedProgression => trackedProgression;
    public HWJ_GameSaveData LastLoadedSaveData => lastLoadedSaveData;
    public string LastOperationMessage => lastOperationMessage;
    public HWJ_SaveOperationResult LastOperationResult => lastOperationResult;
    public static HWJ_SaveService ActiveService => activeService;

    public static bool TryGetActiveService(out HWJ_SaveService saveService)
    {
        saveService = activeService;
        return saveService != null;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureTrackedProgression();
    }

    private void OnEnable()
    {
        activeService = this;
        HWJ_GameplayEvents.EnemyDefeated += OnEnemyDefeated;
        HWJ_GameplayEvents.BossDefeated += OnBossDefeated;
        HWJ_GameplayEvents.StageCleared += OnStageCleared;
        HWJ_GameplayEvents.RegionUnlocked += OnRegionUnlocked;
    }

    private void OnDisable()
    {
        if (activeService == this)
        {
            activeService = null;
        }

        HWJ_GameplayEvents.EnemyDefeated -= OnEnemyDefeated;
        HWJ_GameplayEvents.BossDefeated -= OnBossDefeated;
        HWJ_GameplayEvents.StageCleared -= OnStageCleared;
        HWJ_GameplayEvents.RegionUnlocked -= OnRegionUnlocked;
    }

    public void SetRuntimeSources(
        HWJ_RuntimeObjectContext runtimeContext,
        HWJ_StageProgressionSystem stageSystem,
        HWJ_GameplayDatabaseSO database)
    {
        playerRuntimeContext = runtimeContext;
        playerInputSystem = runtimeContext != null
            ? runtimeContext.GetComponent<HWJ_PlayerInputSystem>()
            : null;

        if (playerInputSystem == null)
        {
            playerInputSystem = HWJ_GameAccess.PlayerInput;
        }

        stageProgressionSystem = stageSystem;
        gameplayDatabase = database;
    }

    /// <summary>
    /// 현재 플레이어/스테이지 런타임 상태를 캡처해서 저장 파일로 씁니다.
    /// </summary>
    public HWJ_SaveOperationResult SaveCurrentGame(string overrideSlotId = null)
    {
        HWJ_GameSaveData currentSaveData;
        HWJ_SaveOperationResult captureResult = TryCreateCurrentSaveData(out currentSaveData, overrideSlotId);

        if (!captureResult.Succeeded)
        {
            return StoreAndRaiseSaveResult(captureResult);
        }

        return SaveData(currentSaveData, overrideSlotId);
    }

    /// <summary>
    /// 파일로 쓰기 전, 현재 런타임 값을 저장 DTO로만 변환합니다.
    /// 테스트나 UI 미리보기에서는 이 단계만 호출할 수 있습니다.
    /// </summary>
    public HWJ_SaveOperationResult TryCreateCurrentSaveData(out HWJ_GameSaveData saveData, string overrideSlotId = null)
    {
        saveData = null;
        ResolveReferences();

        if (!TryResolveSavePath(overrideSlotId, out string normalizedSlotId, out string filePath, out HWJ_SaveOperationResult pathFailure))
        {
            return pathFailure;
        }

        if (playerRuntimeContext == null)
        {
            return HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.MissingRuntimeContext,
                normalizedSlotId,
                filePath,
                null,
                "Save capture failed: missing player runtime context.");
        }

        HWJ_RuntimeObjectSnapshot playerSnapshot = playerRuntimeContext.CreateSnapshot();
        HWJ_RuntimeStageFlowSnapshot stageSnapshot = stageProgressionSystem != null
            ? stageProgressionSystem.CreateSnapshot()
            : default;
        EnsureTrackedProgression();
        saveData = HWJ_SaveDataFactory.CreateFromSnapshots(
            normalizedSlotId,
            playerSnapshot,
            stageSnapshot,
            trackedProgression);
        CaptureInputSettings(saveData);

        return HWJ_SaveOperationResult.Success(
            normalizedSlotId,
            filePath,
            saveData,
            "Save data captured from runtime.");
    }

    public HWJ_SaveOperationResult SaveData(HWJ_GameSaveData saveData, string overrideSlotId = null)
    {
        if (!TryResolveSavePath(overrideSlotId, out string normalizedSlotId, out string filePath, out HWJ_SaveOperationResult pathFailure))
        {
            return StoreAndRaiseSaveResult(pathFailure);
        }

        if (saveData == null)
        {
            return StoreAndRaiseSaveResult(HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.InvalidSaveData,
                normalizedSlotId,
                filePath,
                null,
                "Save failed: save data is null."));
        }

        saveData.saveId = string.IsNullOrEmpty(saveData.saveId) ? normalizedSlotId : saveData.saveId;
        HWJ_SaveOperationResult migrationResult = TryMigrateSaveDataToCurrent(
            ref saveData,
            normalizedSlotId,
            filePath);

        if (!migrationResult.Succeeded)
        {
            return StoreAndRaiseSaveResult(migrationResult);
        }

        HWJ_SaveDataFactory.RefreshSavedTime(saveData);

        HWJ_SaveOperationResult validationResult = ValidateSaveData(saveData, normalizedSlotId, filePath);

        if (!validationResult.Succeeded)
        {
            return StoreAndRaiseSaveResult(validationResult);
        }

        try
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string json = JsonUtility.ToJson(saveData, prettyPrintJson);
            File.WriteAllText(filePath, json);

            return StoreAndRaiseSaveResult(HWJ_SaveOperationResult.Success(
                normalizedSlotId,
                filePath,
                saveData,
                "Save file written."));
        }
        catch (Exception exception)
        {
            return StoreAndRaiseSaveResult(HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.IoException,
                normalizedSlotId,
                filePath,
                saveData,
                $"Save failed: {exception.Message}"));
        }
    }

    public HWJ_SaveOperationResult LoadData(out HWJ_GameSaveData loadedSaveData, string overrideSlotId = null)
    {
        HWJ_SaveOperationResult loadResult = ReadSaveData(out loadedSaveData, overrideSlotId);
        return StoreAndRaiseLoadResult(loadResult);
    }

    /// <summary>
    /// 저장 파일을 읽고 현재 연결된 런타임 시스템에 적용합니다.
    /// </summary>
    public HWJ_SaveOperationResult LoadAndApply(string overrideSlotId = null)
    {
        HWJ_SaveOperationResult loadResult = ReadSaveData(out HWJ_GameSaveData loadedSaveData, overrideSlotId);

        if (!loadResult.Succeeded)
        {
            return StoreAndRaiseLoadResult(loadResult);
        }

        HWJ_SaveOperationResult applyResult = ApplySaveDataToRuntime(
            loadedSaveData,
            loadResult.SlotId,
            loadResult.FilePath);
        return StoreAndRaiseLoadResult(applyResult);
    }

    public HWJ_SaveOperationResult ApplySaveDataToRuntime(HWJ_GameSaveData saveData)
    {
        HWJ_SaveOperationResult applyResult = ApplySaveDataToRuntime(saveData, saveData != null ? saveData.saveId : saveSlotId, null);
        return StoreAndRaiseLoadResult(applyResult);
    }

    public bool HasSaveFile(string overrideSlotId = null)
    {
        return TryResolveSavePath(overrideSlotId, out _, out string filePath, out _)
            && File.Exists(filePath);
    }

    public bool IsRewardClaimed(string rewardClaimId)
    {
        EnsureTrackedProgression();
        return !string.IsNullOrEmpty(rewardClaimId)
            && trackedProgression.defeatedEnemyRewardIds.Contains(rewardClaimId);
    }

    /// <summary>
    /// 보상 지급 전에 저장 진행 데이터에 claim ID를 등록합니다.
    /// 이미 등록된 ID라면 false를 반환해 중복 보상을 막습니다.
    /// </summary>
    public bool TryClaimReward(string rewardClaimId, bool isBossReward, string bossId)
    {
        EnsureTrackedProgression();

        if (string.IsNullOrEmpty(rewardClaimId) || IsRewardClaimed(rewardClaimId))
        {
            return false;
        }

        bool claimedReward = trackedProgression.AddDefeatedEnemyRewardId(rewardClaimId);

        if (claimedReward && isBossReward)
        {
            trackedProgression.AddDefeatedBossId(bossId);
        }

        return claimedReward;
    }

    public string GetSaveFilePath(string overrideSlotId = null)
    {
        return TryResolveSavePath(overrideSlotId, out _, out string filePath, out _)
            ? filePath
            : null;
    }

    private HWJ_SaveOperationResult ReadSaveData(out HWJ_GameSaveData loadedSaveData, string overrideSlotId)
    {
        loadedSaveData = null;

        if (!TryResolveSavePath(overrideSlotId, out string normalizedSlotId, out string filePath, out HWJ_SaveOperationResult pathFailure))
        {
            return pathFailure;
        }

        if (!File.Exists(filePath))
        {
            return HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.FileNotFound,
                normalizedSlotId,
                filePath,
                null,
                "Load failed: save file does not exist.");
        }

        try
        {
            string json = File.ReadAllText(filePath);

            if (string.IsNullOrEmpty(json))
            {
                return HWJ_SaveOperationResult.Fail(
                    HWJ_SaveOperationFailureCode.EmptyJson,
                    normalizedSlotId,
                    filePath,
                    null,
                    "Load failed: save file is empty.");
            }

            loadedSaveData = JsonUtility.FromJson<HWJ_GameSaveData>(json);

            if (loadedSaveData == null)
            {
                return HWJ_SaveOperationResult.Fail(
                    HWJ_SaveOperationFailureCode.JsonParseFailed,
                    normalizedSlotId,
                    filePath,
                    null,
                    "Load failed: save json could not be parsed.");
            }

            HWJ_SaveOperationResult migrationResult = TryMigrateSaveDataToCurrent(
                ref loadedSaveData,
                normalizedSlotId,
                filePath);

            if (!migrationResult.Succeeded)
            {
                return migrationResult;
            }

            HWJ_SaveOperationResult validationResult = ValidateSaveData(loadedSaveData, normalizedSlotId, filePath);

            if (!validationResult.Succeeded)
            {
                return validationResult;
            }

            lastLoadedSaveData = loadedSaveData;
            return HWJ_SaveOperationResult.Success(
                normalizedSlotId,
                filePath,
                loadedSaveData,
                "Save file loaded.");
        }
        catch (Exception exception)
        {
            return HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.IoException,
                normalizedSlotId,
                filePath,
                loadedSaveData,
                $"Load failed: {exception.Message}");
        }
    }

    private HWJ_SaveOperationResult ApplySaveDataToRuntime(
        HWJ_GameSaveData saveData,
        string slotId,
        string filePath)
    {
        HWJ_SaveOperationResult migrationResult = TryMigrateSaveDataToCurrent(
            ref saveData,
            slotId,
            filePath);

        if (!migrationResult.Succeeded)
        {
            return migrationResult;
        }

        HWJ_SaveOperationResult validationResult = ValidateSaveData(saveData, slotId, filePath);

        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        ResolveReferences();

        if (playerRuntimeContext == null)
        {
            return HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.MissingRuntimeContext,
                slotId,
                filePath,
                saveData,
                "Load apply failed: missing player runtime context.");
        }

        HWJ_SaveOperationResult playerResult = ApplyPlayerRuntimeData(saveData, slotId, filePath);

        if (!playerResult.Succeeded)
        {
            return playerResult;
        }

        ApplySettingsData(saveData.settings);

        if (stageProgressionSystem != null && saveData.stage != null)
        {
            stageProgressionSystem.RestoreStageFlowSnapshot(saveData.stage.ToRuntimeSnapshot());
        }

        trackedProgression = saveData.progression != null
            ? saveData.progression.Clone()
            : new HWJ_SaveProgressionData();
        trackedProgression.EnsureLists();
        lastLoadedSaveData = saveData;

        return HWJ_SaveOperationResult.Success(
            slotId,
            filePath,
            saveData,
            "Save data applied to runtime.");
    }

    private HWJ_SaveOperationResult ApplyPlayerRuntimeData(
        HWJ_GameSaveData saveData,
        string slotId,
        string filePath)
    {
        HWJ_SavePlayerRuntimeData playerData = saveData.player;
        playerData.EnsureDefaults();

        HWJ_SaveBodyRuntimeData bodyData = playerData.body;
        HWJ_SaveRuntimeStatData statData = playerData.stats;
        HWJ_SaveGrowthRuntimeData growthData = playerData.growth;

        HWJ_PossessionSystem possessionState = playerRuntimeContext.PossessionSystem;
        HWJ_SoulSystem soulState = playerRuntimeContext.SoulSystem;
        HWJ_RuntimeStatusSystem statusState = playerRuntimeContext.RuntimeStatus;
        HWJ_BodyDecaySystem decayState = playerRuntimeContext.BodyDecaySystem;
        HWJ_PossessedBodySystem possessedBodyState = playerRuntimeContext.PossessedBodySystem;
        HWJ_LevelUpSystem levelProgressState = playerRuntimeContext.LevelProgressState;
        HWJ_SkillUnlockSystem skillUnlockState = playerRuntimeContext.SkillUnlockState;

        if (bodyData.hasActivePossessedBody)
        {
            if (possessionState == null)
            {
                return HWJ_SaveOperationResult.Fail(
                    HWJ_SaveOperationFailureCode.MissingRuntimeContext,
                    slotId,
                    filePath,
                    saveData,
                    "Load apply failed: missing possession system.");
            }

            if (!TryResolveRootObjectData(
                bodyData.possessedBodyDefinitionDataId,
                out HWJ_RootObjectDataSO possessedRootObjectData,
                out HWJ_SaveOperationFailureCode rootFailureCode,
                out string rootFailureMessage))
            {
                return HWJ_SaveOperationResult.Fail(
                    rootFailureCode,
                    slotId,
                    filePath,
                    saveData,
                    rootFailureMessage);
            }

            bool restoredBody = possessionState.RestorePossessedBody(
                possessedRootObjectData,
                null,
                Color.white,
                false,
                false,
                null,
                false,
                false,
                false,
                false);

            if (!restoredBody)
            {
                return HWJ_SaveOperationResult.Fail(
                    HWJ_SaveOperationFailureCode.InvalidSaveData,
                    slotId,
                    filePath,
                    saveData,
                    "Load apply failed: possession system rejected saved body.");
            }
        }
        else
        {
            possessionState?.ClearPossessedBody(false, false, false);
        }

        soulState?.RestoreSoulSnapshot(bodyData.ToRuntimeSnapshot());
        statusState?.RestoreHpSnapshot(statData.currentHp, statData.soulHp, statData.possessedBodyHp);
        decayState?.RestoreDecaySnapshot(bodyData.currentDecayValue);

        if (possessedBodyState != null && bodyData.hasPossessedBodyRuntimeState)
        {
            possessedBodyState.SetCurrentHp(bodyData.possessedBodyCurrentHp);
            possessedBodyState.SetCurrentDecayValue(bodyData.currentDecayValue);
        }

        levelProgressState?.RestoreProgress(
            growthData.currentLevel,
            growthData.currentExperience,
            growthData.skillPoint);
        skillUnlockState?.RestoreUnlockedSkills(growthData.unlockedSkillIds);
        return HWJ_SaveOperationResult.Success(
            slotId,
            filePath,
            saveData,
            "Player runtime data applied.");
    }

    private void CaptureInputSettings(HWJ_GameSaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        if (saveData.settings == null)
        {
            saveData.settings = new HWJ_SaveSettingsData();
        }

        saveData.settings.inputBindings = playerInputSystem != null
            ? playerInputSystem.CreateSaveInputBindingData()
            : new HWJ_SaveInputBindingData();
        saveData.settings.EnsureDefaults();
    }

    private void ApplySettingsData(HWJ_SaveSettingsData settingsData)
    {
        if (settingsData == null || playerInputSystem == null)
        {
            return;
        }

        settingsData.EnsureDefaults();
        playerInputSystem.ApplySaveInputBindingData(settingsData.inputBindings);
    }

    private HWJ_SaveOperationResult ValidateSaveData(
        HWJ_GameSaveData saveData,
        string slotId,
        string filePath)
    {
        HWJ_SaveDataValidationResult validationResult = HWJ_SaveDataRuntimeValidator.Validate(saveData);

        if (!validationResult.Succeeded)
        {
            return HWJ_SaveOperationResult.Fail(
                ResolveSaveDataValidationOperationFailureCode(validationResult.FailureCode),
                slotId,
                filePath,
                saveData,
                $"Save data validation failed [{validationResult.FailureCode}] Field:{validationResult.FieldName}. {validationResult.Message}");
        }

        return HWJ_SaveOperationResult.Success(
            slotId,
            filePath,
            saveData,
            "Save data is valid.");
    }

    private static HWJ_SaveOperationFailureCode ResolveSaveDataValidationOperationFailureCode(
        HWJ_SaveDataValidationFailureCode validationFailureCode)
    {
        switch (validationFailureCode)
        {
            case HWJ_SaveDataValidationFailureCode.UnsupportedLegacyVersion:
                return HWJ_SaveOperationFailureCode.MigrationRequired;
            case HWJ_SaveDataValidationFailureCode.UnsupportedFutureVersion:
                return HWJ_SaveOperationFailureCode.UnsupportedFutureVersion;
            default:
                return HWJ_SaveOperationFailureCode.InvalidSaveData;
        }
    }

    private static HWJ_SaveOperationResult TryMigrateSaveDataToCurrent(
        ref HWJ_GameSaveData saveData,
        string slotId,
        string filePath)
    {
        HWJ_SaveMigrationResult migrationResult = HWJ_SaveMigrationService.MigrateToCurrent(saveData);

        if (migrationResult.Succeeded)
        {
            saveData = migrationResult.SaveData;
            return HWJ_SaveOperationResult.Success(
                slotId,
                filePath,
                saveData,
                migrationResult.Message);
        }

        HWJ_SaveOperationFailureCode operationFailureCode = ResolveMigrationOperationFailureCode(migrationResult.FailureCode);
        return HWJ_SaveOperationResult.Fail(
            operationFailureCode,
            slotId,
            filePath,
            migrationResult.SaveData,
            migrationResult.Message);
    }

    private static HWJ_SaveOperationFailureCode ResolveMigrationOperationFailureCode(
        HWJ_SaveMigrationFailureCode migrationFailureCode)
    {
        switch (migrationFailureCode)
        {
            case HWJ_SaveMigrationFailureCode.MissingSaveData:
                return HWJ_SaveOperationFailureCode.InvalidSaveData;
            case HWJ_SaveMigrationFailureCode.UnsupportedFutureVersion:
                return HWJ_SaveOperationFailureCode.UnsupportedFutureVersion;
            case HWJ_SaveMigrationFailureCode.UnsupportedLegacyVersion:
                return HWJ_SaveOperationFailureCode.MigrationRequired;
            default:
                return HWJ_SaveOperationFailureCode.MigrationRequired;
        }
    }

    private bool TryResolveRootObjectData(
        string rootObjectId,
        out HWJ_RootObjectDataSO rootObjectData,
        out HWJ_SaveOperationFailureCode failureCode,
        out string failureMessage)
    {
        rootObjectData = null;
        failureCode = HWJ_SaveOperationFailureCode.None;
        failureMessage = null;

        if (string.IsNullOrEmpty(rootObjectId))
        {
            failureCode = HWJ_SaveOperationFailureCode.InvalidSaveData;
            failureMessage = "Load apply failed: saved possessed body id is empty.";
            return false;
        }

        ResolveReferences();

        if (gameplayDatabase == null)
        {
            failureCode = HWJ_SaveOperationFailureCode.MissingDatabase;
            failureMessage = "Load apply failed: missing gameplay database.";
            return false;
        }

        if (!gameplayDatabase.TryGetRootObject(rootObjectId, out rootObjectData) || rootObjectData == null)
        {
            failureCode = HWJ_SaveOperationFailureCode.MissingDatabaseEntry;
            failureMessage = $"Load apply failed: root object id '{rootObjectId}' was not found.";
            return false;
        }

        return true;
    }

    private bool TryResolveSavePath(
        string overrideSlotId,
        out string normalizedSlotId,
        out string filePath,
        out HWJ_SaveOperationResult failureResult)
    {
        string requestedSlotId = !string.IsNullOrEmpty(overrideSlotId)
            ? overrideSlotId
            : saveSlotId;
        normalizedSlotId = NormalizeSlotId(requestedSlotId);
        filePath = null;
        failureResult = default;

        if (string.IsNullOrEmpty(normalizedSlotId))
        {
            failureResult = HWJ_SaveOperationResult.Fail(
                HWJ_SaveOperationFailureCode.EmptySlotId,
                requestedSlotId,
                null,
                null,
                "Save path failed: slot id is empty.");
            return false;
        }

        string normalizedFolderName = NormalizeSlotId(saveFolderName);
        string folderName = !string.IsNullOrEmpty(normalizedFolderName) ? normalizedFolderName : "HWJ";
        filePath = Path.Combine(Application.persistentDataPath, folderName, normalizedSlotId + ".json");
        return true;
    }

    private static string NormalizeSlotId(string rawSlotId)
    {
        if (string.IsNullOrEmpty(rawSlotId))
        {
            return null;
        }

        string trimmedSlotId = rawSlotId.Trim();

        if (string.IsNullOrEmpty(trimmedSlotId))
        {
            return null;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        char[] normalizedChars = trimmedSlotId.ToCharArray();

        for (int i = 0; i < normalizedChars.Length; i++)
        {
            if (Array.IndexOf(invalidChars, normalizedChars[i]) >= 0)
            {
                normalizedChars[i] = '_';
            }
        }

        return new string(normalizedChars);
    }

    private void ResolveReferences()
    {
        if (gameplayDatabase == null)
        {
            gameplayDatabase = HWJ_GameAccess.Database;
        }

        if (playerRuntimeContext == null
            && HWJ_GameAccess.HasManager
            && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            playerRuntimeContext = HWJ_GameAccess.Manager.PlayerResolver.GetComponent<HWJ_RuntimeObjectContext>();
        }

        if (playerInputSystem == null && playerRuntimeContext != null)
        {
            playerInputSystem = playerRuntimeContext.GetComponent<HWJ_PlayerInputSystem>();
        }

        if (playerInputSystem == null)
        {
            playerInputSystem = HWJ_GameAccess.PlayerInput;
        }

        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = GetComponent<HWJ_StageProgressionSystem>();
        }
    }

    private void OnEnemyDefeated(HWJ_EnemyDefeatedEvent defeatedEvent)
    {
        EnsureTrackedProgression();

        if (defeatedEvent.DefeatedResolver == null)
        {
            return;
        }

        if (!HWJ_RuntimeSaveIdentity.TryGetRewardClaimId(defeatedEvent.DefeatedResolver, out string rewardClaimId))
        {
            return;
        }

        trackedProgression.AddDefeatedEnemyRewardId(rewardClaimId);

        if (defeatedEvent.IsBoss)
        {
            string defeatedObjectId = ResolveResolverObjectId(defeatedEvent.DefeatedResolver);
            trackedProgression.AddDefeatedBossId(defeatedObjectId);
        }
    }

    private void OnStageCleared(HWJ_StageProgressionEvent progressionEvent)
    {
        EnsureTrackedProgression();
        trackedProgression.AddClearedStageId(progressionEvent.StageId);
    }

    private void OnBossDefeated(HWJ_StageProgressionEvent progressionEvent)
    {
        EnsureTrackedProgression();

        if (!string.IsNullOrEmpty(progressionEvent.BossId))
        {
            trackedProgression.AddDefeatedBossId(progressionEvent.BossId);
        }
    }

    private void OnRegionUnlocked(HWJ_StageProgressionEvent progressionEvent)
    {
        EnsureTrackedProgression();
        string unlockedRegionId = !string.IsNullOrEmpty(progressionEvent.UnlockRegionId)
            ? progressionEvent.UnlockRegionId
            : progressionEvent.NextRegionId;
        trackedProgression.AddUnlockedRegionId(unlockedRegionId);
    }

    private void EnsureTrackedProgression()
    {
        if (trackedProgression == null)
        {
            trackedProgression = new HWJ_SaveProgressionData();
        }

        trackedProgression.EnsureLists();
    }

    private static string ResolveResolverObjectId(HWJ_RootObjectDataResolver resolver)
    {
        if (resolver == null || resolver.RootObjectData == null)
        {
            return null;
        }

        if (resolver.RootObjectData.Identity != null
            && !string.IsNullOrEmpty(resolver.RootObjectData.Identity.objectId))
        {
            return resolver.RootObjectData.Identity.objectId;
        }

        return resolver.RootObjectData.name;
    }

    private HWJ_SaveOperationResult StoreAndRaiseSaveResult(HWJ_SaveOperationResult operationResult)
    {
        lastOperationResult = operationResult;
        lastOperationMessage = operationResult.Message;
        HWJ_GameplayEvents.RaiseSaveCompleted(new HWJ_SaveCompletedEvent(operationResult));
        return operationResult;
    }

    private HWJ_SaveOperationResult StoreAndRaiseLoadResult(HWJ_SaveOperationResult operationResult)
    {
        lastOperationResult = operationResult;
        lastOperationMessage = operationResult.Message;
        HWJ_GameplayEvents.RaiseLoadCompleted(new HWJ_LoadCompletedEvent(operationResult));
        return operationResult;
    }
}
