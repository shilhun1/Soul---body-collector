using System;
using System.Collections.Generic;
using UnityEngine;

public enum HWJ_SaveDataValidationFailureCode
{
    None,
    MissingSaveData,
    UnsupportedLegacyVersion,
    UnsupportedFutureVersion,
    EmptySaveId,
    MissingPlayerData,
    MissingStatData,
    MissingBodyData,
    MissingGrowthData,
    MissingStageData,
    MissingProgressionData,
    InvalidEnumValue,
    InvalidHpRange,
    InvalidSoulHpRange,
    InvalidPossessedBodyHpRange,
    InvalidDecayRange,
    NegativeRuntimeStat,
    PossessedBodyMissingDefinitionId,
    PossessedBodyStateConflict,
    InvalidGrowthLevel,
    NegativeExperience,
    NegativeSkillPoint,
    InvalidStatOrbStackCount,
    EmptyStableId,
    DuplicateStableId,
    InvalidInputBinding,
    DuplicateInputBinding
}

public readonly struct HWJ_SaveDataValidationResult
{
    public readonly bool Succeeded;
    public readonly HWJ_SaveDataValidationFailureCode FailureCode;
    public readonly string FieldName;
    public readonly string Message;

    private HWJ_SaveDataValidationResult(
        bool succeeded,
        HWJ_SaveDataValidationFailureCode failureCode,
        string fieldName,
        string message)
    {
        Succeeded = succeeded;
        FailureCode = failureCode;
        FieldName = fieldName;
        Message = message;
    }

    public static HWJ_SaveDataValidationResult Success()
    {
        return new HWJ_SaveDataValidationResult(
            true,
            HWJ_SaveDataValidationFailureCode.None,
            null,
            "Save data is valid.");
    }

    public static HWJ_SaveDataValidationResult Fail(
        HWJ_SaveDataValidationFailureCode failureCode,
        string fieldName,
        string message)
    {
        return new HWJ_SaveDataValidationResult(
            false,
            failureCode,
            fieldName,
            message);
    }
}

public static class HWJ_SaveDataRuntimeValidator
{
    public static HWJ_SaveDataValidationResult Validate(HWJ_GameSaveData saveData)
    {
        if (saveData == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingSaveData, "saveData", "Save data is null.");
        }

        if (saveData.schemaVersion < HWJ_SaveSchema.CurrentVersion)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.UnsupportedLegacyVersion, "schemaVersion", $"Schema {saveData.schemaVersion} is older than current schema {HWJ_SaveSchema.CurrentVersion}.");
        }

        if (saveData.schemaVersion > HWJ_SaveSchema.CurrentVersion)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.UnsupportedFutureVersion, "schemaVersion", $"Schema {saveData.schemaVersion} is newer than current schema {HWJ_SaveSchema.CurrentVersion}.");
        }

        if (string.IsNullOrWhiteSpace(saveData.saveId))
        {
            return Fail(HWJ_SaveDataValidationFailureCode.EmptySaveId, "saveId", "Save id is empty.");
        }

        if (saveData.player == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingPlayerData, "player", "Player save data is missing.");
        }

        if (saveData.stage == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingStageData, "stage", "Stage save data is missing.");
        }

        if (saveData.progression == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingProgressionData, "progression", "Progression save data is missing.");
        }

        HWJ_SaveDataValidationResult playerResult = ValidatePlayerData(saveData.player);

        if (!playerResult.Succeeded)
        {
            return playerResult;
        }

        HWJ_SaveDataValidationResult stageResult = ValidateStageData(saveData.stage);

        if (!stageResult.Succeeded)
        {
            return stageResult;
        }

        HWJ_SaveDataValidationResult progressionResult = ValidateProgressionData(saveData.progression);

        if (!progressionResult.Succeeded)
        {
            return progressionResult;
        }

        return ValidateSettingsData(saveData.settings);
    }

    private static HWJ_SaveDataValidationResult ValidatePlayerData(HWJ_SavePlayerRuntimeData playerData)
    {
        if (playerData.stats == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingStatData, "player.stats", "Player stat save data is missing.");
        }

        if (playerData.body == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingBodyData, "player.body", "Player body save data is missing.");
        }

        if (playerData.growth == null)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.MissingGrowthData, "player.growth", "Player growth save data is missing.");
        }

        HWJ_SaveDataValidationResult enumResult = ValidateDefinedEnum(playerData.objectType, "player.objectType");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        enumResult = ValidateDefinedEnum(playerData.faction, "player.faction");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        enumResult = ValidateDefinedEnum(playerData.weaponType, "player.weaponType");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        HWJ_SaveDataValidationResult statResult = ValidateStatData(playerData.stats);

        if (!statResult.Succeeded)
        {
            return statResult;
        }

        HWJ_SaveDataValidationResult bodyResult = ValidateBodyData(playerData.body);

        if (!bodyResult.Succeeded)
        {
            return bodyResult;
        }

        return ValidateGrowthData(playerData.growth);
    }

    private static HWJ_SaveDataValidationResult ValidateStatData(HWJ_SaveRuntimeStatData statData)
    {
        HWJ_SaveDataValidationResult enumResult = ValidateDefinedEnum(statData.runtimeState, "player.stats.runtimeState");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        HWJ_SaveDataValidationResult hpResult = ValidateValueInsideMaximum(
            statData.currentHp,
            statData.maxHp,
            "player.stats.currentHp",
            "player.stats.maxHp",
            HWJ_SaveDataValidationFailureCode.InvalidHpRange);

        if (!hpResult.Succeeded)
        {
            return hpResult;
        }

        hpResult = ValidateValueInsideMaximum(
            statData.soulHp,
            statData.soulMaxHp,
            "player.stats.soulHp",
            "player.stats.soulMaxHp",
            HWJ_SaveDataValidationFailureCode.InvalidSoulHpRange);

        if (!hpResult.Succeeded)
        {
            return hpResult;
        }

        if (statData.possessedBodyHp < 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidPossessedBodyHpRange, "player.stats.possessedBodyHp", "Possessed body HP cannot be negative.");
        }

        if (statData.moveSpeed < 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.NegativeRuntimeStat, "player.stats.moveSpeed", "Move speed cannot be negative.");
        }

        if (statData.attackSpeed < 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.NegativeRuntimeStat, "player.stats.attackSpeed", "Attack speed cannot be negative.");
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateBodyData(HWJ_SaveBodyRuntimeData bodyData)
    {
        HWJ_SaveDataValidationResult enumResult = ValidateDefinedEnum(bodyData.soulState, "player.body.soulState");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        enumResult = ValidateDefinedEnum(bodyData.playerExistenceState, "player.body.playerExistenceState");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        enumResult = ValidateDefinedEnum(bodyData.possessedWeaponType, "player.body.possessedWeaponType");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        enumResult = ValidateDefinedEnum(bodyData.decayDangerLevel, "player.body.decayDangerLevel");

        if (!enumResult.Succeeded)
        {
            return enumResult;
        }

        if (bodyData.hasActivePossessedBody && string.IsNullOrWhiteSpace(bodyData.possessedBodyDefinitionDataId))
        {
            return Fail(HWJ_SaveDataValidationFailureCode.PossessedBodyMissingDefinitionId, "player.body.possessedBodyDefinitionDataId", "Active possessed body has no definition data id.");
        }

        if (bodyData.playerExistenceState == HWJ_PlayerExistenceState.Possessed && !bodyData.hasActivePossessedBody)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.PossessedBodyStateConflict, "player.body.hasActivePossessedBody", "Player existence state is Possessed but no active body is saved.");
        }

        if (bodyData.playerExistenceState == HWJ_PlayerExistenceState.Spirit && bodyData.hasActivePossessedBody)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.PossessedBodyStateConflict, "player.body.playerExistenceState", "Spirit state cannot keep an active possessed body.");
        }

        bool requiresBodyValues = bodyData.hasActivePossessedBody
            || bodyData.hasPossessedBodyRuntimeState
            || bodyData.isDecaying;

        if (requiresBodyValues)
        {
            HWJ_SaveDataValidationResult possessedHpResult = ValidateStrictValueInsideMaximum(
                bodyData.possessedBodyCurrentHp,
                bodyData.possessedBodyMaxHp,
                "player.body.possessedBodyCurrentHp",
                "player.body.possessedBodyMaxHp",
                HWJ_SaveDataValidationFailureCode.InvalidPossessedBodyHpRange);

            if (!possessedHpResult.Succeeded)
            {
                return possessedHpResult;
            }
        }

        return ValidateDecayValues(bodyData, requiresBodyValues);
    }

    private static HWJ_SaveDataValidationResult ValidateDecayValues(
        HWJ_SaveBodyRuntimeData bodyData,
        bool requiresBodyValues)
    {
        if (bodyData.maxDecayValue < 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, "player.body.maxDecayValue", "Max possession mental value cannot be negative.");
        }

        if (requiresBodyValues && bodyData.maxDecayValue <= 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, "player.body.maxDecayValue", "Active possession mental requires maxDecayValue greater than 0.");
        }

        if (bodyData.currentDecayValue < 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, "player.body.currentDecayValue", "Consumed possession mental value cannot be negative.");
        }

        if (bodyData.remainingDecayValue < 0f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, "player.body.remainingDecayValue", "Remaining possession mental value cannot be negative.");
        }

        if (bodyData.maxDecayValue > 0f)
        {
            if (bodyData.currentDecayValue > bodyData.maxDecayValue)
            {
                return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, "player.body.currentDecayValue", "Consumed possession mental value cannot exceed maxDecayValue.");
            }

            if (bodyData.remainingDecayValue > bodyData.maxDecayValue)
            {
                return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, "player.body.remainingDecayValue", "Remaining possession mental value cannot exceed maxDecayValue.");
            }
        }

        HWJ_SaveDataValidationResult ratioResult = ValidateRatio(bodyData.currentDecayRatio, "player.body.currentDecayRatio");

        if (!ratioResult.Succeeded)
        {
            return ratioResult;
        }

        return ValidateRatio(bodyData.remainingDecayRatio, "player.body.remainingDecayRatio");
    }

    private static HWJ_SaveDataValidationResult ValidateGrowthData(HWJ_SaveGrowthRuntimeData growthData)
    {
        if (growthData.currentLevel <= 0)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidGrowthLevel, "player.growth.currentLevel", "Current level must be greater than 0.");
        }

        if (growthData.currentExperience < 0)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.NegativeExperience, "player.growth.currentExperience", "Current experience cannot be negative.");
        }

        if (growthData.skillPoint < 0)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.NegativeSkillPoint, "player.growth.skillPoint", "Skill point cannot be negative.");
        }

        HWJ_SaveDataValidationResult unlockedSkillResult = ValidateStableIdList(growthData.unlockedSkillIds, "player.growth.unlockedSkillIds");

        if (!unlockedSkillResult.Succeeded)
        {
            return unlockedSkillResult;
        }

        HWJ_SaveDataValidationResult unlockedSkillNodeResult = ValidateStableIdList(growthData.unlockedSkillNodeIds, "player.growth.unlockedSkillNodeIds");

        if (!unlockedSkillNodeResult.Succeeded)
        {
            return unlockedSkillNodeResult;
        }

        return ValidateStatOrbStackList(growthData.statOrbStacks, "player.growth.statOrbStacks");
    }

    private static HWJ_SaveDataValidationResult ValidateStageData(HWJ_SaveStageRuntimeData stageData)
    {
        return ValidateDefinedEnum(stageData.currentState, "stage.currentState");
    }

    private static HWJ_SaveDataValidationResult ValidateProgressionData(HWJ_SaveProgressionData progressionData)
    {
        HWJ_SaveDataValidationResult listResult = ValidateStableIdList(progressionData.defeatedEnemyRewardIds, "progression.defeatedEnemyRewardIds");

        if (!listResult.Succeeded)
        {
            return listResult;
        }

        listResult = ValidateStableIdList(progressionData.defeatedBossIds, "progression.defeatedBossIds");

        if (!listResult.Succeeded)
        {
            return listResult;
        }

        listResult = ValidateStableIdList(progressionData.clearedStageIds, "progression.clearedStageIds");

        if (!listResult.Succeeded)
        {
            return listResult;
        }

        return ValidateStableIdList(progressionData.unlockedRegionIds, "progression.unlockedRegionIds");
    }

    private static HWJ_SaveDataValidationResult ValidateSettingsData(HWJ_SaveSettingsData settingsData)
    {
        if (settingsData == null || settingsData.inputBindings == null)
        {
            return HWJ_SaveDataValidationResult.Success();
        }

        return ValidateInputBindingData(settingsData.inputBindings);
    }

    private static HWJ_SaveDataValidationResult ValidateInputBindingData(HWJ_SaveInputBindingData inputBindingData)
    {
        if (inputBindingData.bindingOverrides == null)
        {
            return HWJ_SaveDataValidationResult.Success();
        }

        HashSet<HWJ_PlayerInputActionId> seenActionIds = new HashSet<HWJ_PlayerInputActionId>();
        Dictionary<KeyCode, HWJ_PlayerInputActionId> firstActionByKeyboardKey = new Dictionary<KeyCode, HWJ_PlayerInputActionId>();
        Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId> firstActionByMouseButton = new Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId>();

        for (int i = 0; i < inputBindingData.bindingOverrides.Count; i++)
        {
            HWJ_SaveInputBindingOverrideData bindingOverride = inputBindingData.bindingOverrides[i];
            string fieldPrefix = $"settings.inputBindings.bindingOverrides[{i}]";

            if (bindingOverride == null)
            {
                return Fail(HWJ_SaveDataValidationFailureCode.InvalidInputBinding, fieldPrefix, "Input binding override is null.");
            }

            HWJ_SaveDataValidationResult enumResult = ValidateDefinedEnum(bindingOverride.actionId, fieldPrefix + ".actionId");

            if (!enumResult.Succeeded)
            {
                return enumResult;
            }

            enumResult = ValidateDefinedEnum(bindingOverride.mouseButton, fieldPrefix + ".mouseButton");

            if (!enumResult.Succeeded)
            {
                return enumResult;
            }

            if (!seenActionIds.Add(bindingOverride.actionId))
            {
                return Fail(
                    HWJ_SaveDataValidationFailureCode.DuplicateInputBinding,
                    fieldPrefix + ".actionId",
                    $"Input binding action '{bindingOverride.actionId}' is duplicated.");
            }

            if (!bindingOverride.hasKeyboard && !bindingOverride.hasMouse)
            {
                return Fail(
                    HWJ_SaveDataValidationFailureCode.InvalidInputBinding,
                    fieldPrefix,
                    "Input binding override must contain at least one keyboard key or mouse button.");
            }

            if (bindingOverride.hasKeyboard
                && !Enum.IsDefined(typeof(KeyCode), bindingOverride.keyboardKeyCode))
            {
                return Fail(
                    HWJ_SaveDataValidationFailureCode.InvalidInputBinding,
                    fieldPrefix + ".keyboardKeyCode",
                    $"Keyboard key code '{bindingOverride.keyboardKeyCode}' is not a valid KeyCode.");
            }

            if (bindingOverride.hasKeyboard && (KeyCode)bindingOverride.keyboardKeyCode == KeyCode.None)
            {
                return Fail(
                    HWJ_SaveDataValidationFailureCode.InvalidInputBinding,
                    fieldPrefix + ".keyboardKeyCode",
                    "Keyboard binding override cannot use KeyCode.None when hasKeyboard is true.");
            }

            if (bindingOverride.hasMouse && bindingOverride.mouseButton == HWJ_InputMouseButton.None)
            {
                return Fail(
                    HWJ_SaveDataValidationFailureCode.InvalidInputBinding,
                    fieldPrefix + ".mouseButton",
                    "Mouse binding override cannot use None when hasMouse is true.");
            }

            HWJ_SaveDataValidationResult conflictResult = ValidateInputBindingConflict(
                bindingOverride,
                fieldPrefix,
                firstActionByKeyboardKey,
                firstActionByMouseButton);

            if (!conflictResult.Succeeded)
            {
                return conflictResult;
            }
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateInputBindingConflict(
        HWJ_SaveInputBindingOverrideData bindingOverride,
        string fieldPrefix,
        Dictionary<KeyCode, HWJ_PlayerInputActionId> firstActionByKeyboardKey,
        Dictionary<HWJ_InputMouseButton, HWJ_PlayerInputActionId> firstActionByMouseButton)
    {
        if (bindingOverride.hasKeyboard)
        {
            KeyCode keyboardKey = (KeyCode)bindingOverride.keyboardKeyCode;

            if (keyboardKey != KeyCode.None)
            {
                if (firstActionByKeyboardKey.TryGetValue(keyboardKey, out HWJ_PlayerInputActionId existingActionId))
                {
                    return Fail(
                        HWJ_SaveDataValidationFailureCode.DuplicateInputBinding,
                        fieldPrefix + ".keyboardKeyCode",
                        $"Keyboard key '{keyboardKey}' is already used by input action '{existingActionId}'.");
                }

                firstActionByKeyboardKey.Add(keyboardKey, bindingOverride.actionId);
            }
        }

        if (bindingOverride.hasMouse && bindingOverride.mouseButton != HWJ_InputMouseButton.None)
        {
            if (firstActionByMouseButton.TryGetValue(bindingOverride.mouseButton, out HWJ_PlayerInputActionId existingActionId))
            {
                return Fail(
                    HWJ_SaveDataValidationFailureCode.DuplicateInputBinding,
                    fieldPrefix + ".mouseButton",
                    $"Mouse button '{bindingOverride.mouseButton}' is already used by input action '{existingActionId}'.");
            }

            firstActionByMouseButton.Add(bindingOverride.mouseButton, bindingOverride.actionId);
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateStableIdList(
        List<string> ids,
        string fieldPrefix)
    {
        if (ids == null)
        {
            return HWJ_SaveDataValidationResult.Success();
        }

        HashSet<string> seenIds = new HashSet<string>();

        for (int i = 0; i < ids.Count; i++)
        {
            string fieldName = $"{fieldPrefix}[{i}]";
            string id = ids[i];

            if (string.IsNullOrWhiteSpace(id))
            {
                return Fail(HWJ_SaveDataValidationFailureCode.EmptyStableId, fieldName, "Stable id entry is empty.");
            }

            if (!seenIds.Add(id))
            {
                return Fail(HWJ_SaveDataValidationFailureCode.DuplicateStableId, fieldName, $"Stable id '{id}' is duplicated.");
            }
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateStatOrbStackList(
        List<HWJ_SaveStatOrbStackData> statOrbStacks,
        string fieldPrefix)
    {
        if (statOrbStacks == null)
        {
            return HWJ_SaveDataValidationResult.Success();
        }

        HashSet<string> seenIds = new HashSet<string>();

        for (int i = 0; i < statOrbStacks.Count; i++)
        {
            HWJ_SaveStatOrbStackData stackData = statOrbStacks[i];
            string entryPrefix = $"{fieldPrefix}[{i}]";

            if (stackData == null)
            {
                return Fail(HWJ_SaveDataValidationFailureCode.EmptyStableId, entryPrefix, "Stat orb stack entry is null.");
            }

            if (string.IsNullOrWhiteSpace(stackData.statOrbId))
            {
                return Fail(HWJ_SaveDataValidationFailureCode.EmptyStableId, entryPrefix + ".statOrbId", "Stat orb id is empty.");
            }

            if (!seenIds.Add(stackData.statOrbId))
            {
                return Fail(HWJ_SaveDataValidationFailureCode.DuplicateStableId, entryPrefix + ".statOrbId", $"Stat orb id '{stackData.statOrbId}' is duplicated.");
            }

            if (stackData.stackCount < 0)
            {
                return Fail(HWJ_SaveDataValidationFailureCode.InvalidStatOrbStackCount, entryPrefix + ".stackCount", "Stat orb stack count cannot be negative.");
            }
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateValueInsideMaximum(
        float currentValue,
        float maxValue,
        string currentFieldName,
        string maxFieldName,
        HWJ_SaveDataValidationFailureCode failureCode)
    {
        if (maxValue < 0f)
        {
            return Fail(failureCode, maxFieldName, "Max value cannot be negative.");
        }

        if (currentValue < 0f)
        {
            return Fail(failureCode, currentFieldName, "Current value cannot be negative.");
        }

        if (currentValue > maxValue)
        {
            return Fail(failureCode, currentFieldName, "Current value cannot exceed max value.");
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateStrictValueInsideMaximum(
        float currentValue,
        float maxValue,
        string currentFieldName,
        string maxFieldName,
        HWJ_SaveDataValidationFailureCode failureCode)
    {
        if (maxValue <= 0f)
        {
            return Fail(failureCode, maxFieldName, "Max value must be greater than 0.");
        }

        return ValidateValueInsideMaximum(currentValue, maxValue, currentFieldName, maxFieldName, failureCode);
    }

    private static HWJ_SaveDataValidationResult ValidateRatio(float ratio, string fieldName)
    {
        if (ratio < 0f || ratio > 1f)
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidDecayRange, fieldName, "Ratio must be between 0 and 1.");
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult ValidateDefinedEnum<TEnum>(
        TEnum enumValue,
        string fieldName) where TEnum : struct
    {
        if (!Enum.IsDefined(typeof(TEnum), enumValue))
        {
            return Fail(HWJ_SaveDataValidationFailureCode.InvalidEnumValue, fieldName, $"Enum value '{enumValue}' is not defined.");
        }

        return HWJ_SaveDataValidationResult.Success();
    }

    private static HWJ_SaveDataValidationResult Fail(
        HWJ_SaveDataValidationFailureCode failureCode,
        string fieldName,
        string message)
    {
        return HWJ_SaveDataValidationResult.Fail(failureCode, fieldName, message);
    }
}
