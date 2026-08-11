using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates runtime key changes, detects conflicts, and converts bindings to save data.
/// This partial belongs to the single HWJ_PlayerInputSystem component.
/// </summary>
public partial class HWJ_PlayerInputSystem
{
    public void SetInputBindingData(HWJ_PlayerInputBindingDataSO bindingData)
    {
        inputBindingData = bindingData;
    }

    public bool SetKeyboardBinding(HWJ_PlayerInputActionId actionId, KeyCode keyboardKey)
    {
        return TrySetKeyboardBinding(actionId, keyboardKey).Succeeded;
    }

    public bool SetMouseBinding(HWJ_PlayerInputActionId actionId, HWJ_InputMouseButton mouseButton)
    {
        return TrySetMouseBinding(actionId, mouseButton).Succeeded;
    }

    public bool SetRuntimeBinding(HWJ_PlayerInputRuntimeBinding binding)
    {
        return TrySetRuntimeBinding(binding).Succeeded;
    }

    public bool ClearRuntimeBinding(HWJ_PlayerInputActionId actionId)
    {
        return TryClearRuntimeBinding(actionId).Succeeded;
    }

    public HWJ_PlayerInputRebindResult TrySetKeyboardBinding(HWJ_PlayerInputActionId actionId, KeyCode keyboardKey)
    {
        if (!IsActionIdDefined(actionId))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.InvalidActionId,
                "Input action ID is not defined.");
        }

        if (keyboardKey == KeyCode.None)
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.EmptyKeyboardKey,
                "Keyboard binding cannot use KeyCode.None.");
        }

        if (!System.Enum.IsDefined(typeof(KeyCode), keyboardKey))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.InvalidKeyboardKey,
                "Keyboard binding uses an undefined KeyCode value.");
        }

        return TrySetRuntimeBinding(HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, keyboardKey));
    }

    public HWJ_PlayerInputRebindResult TrySetMouseBinding(HWJ_PlayerInputActionId actionId, HWJ_InputMouseButton mouseButton)
    {
        if (!IsActionIdDefined(actionId))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.InvalidActionId,
                "Input action ID is not defined.");
        }

        if (mouseButton == HWJ_InputMouseButton.None)
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.EmptyMouseButton,
                "Mouse binding cannot use None.");
        }

        if (!System.Enum.IsDefined(typeof(HWJ_InputMouseButton), mouseButton))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.InvalidMouseButton,
                "Mouse binding uses an undefined button value.");
        }

        return TrySetRuntimeBinding(HWJ_PlayerInputRuntimeBinding.FromMouse(actionId, mouseButton));
    }

    public HWJ_PlayerInputRebindResult TrySetRuntimeBinding(HWJ_PlayerInputRuntimeBinding binding)
    {
        if (!IsActionIdDefined(binding.ActionId))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                binding.ActionId,
                HWJ_PlayerInputRebindFailureCode.InvalidActionId,
                "Input action ID is not defined.");
        }

        if (!HasAnyBinding(binding))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                binding.ActionId,
                HWJ_PlayerInputRebindFailureCode.EmptyBinding,
                "Runtime binding has neither keyboard nor mouse input.");
        }

        HWJ_PlayerInputRebindResult validationResult = ValidateRuntimeBinding(binding);

        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        runtimeBindingOverrides[binding.ActionId] = binding;
        return HWJ_PlayerInputRebindResult.Success(binding.ActionId);
    }

    public HWJ_PlayerInputRebindResult TryClearRuntimeBinding(HWJ_PlayerInputActionId actionId)
    {
        if (!IsActionIdDefined(actionId))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.InvalidActionId,
                "Input action ID is not defined.");
        }

        if (!runtimeBindingOverrides.Remove(actionId))
        {
            return HWJ_PlayerInputRebindResult.Failure(
                actionId,
                HWJ_PlayerInputRebindFailureCode.RuntimeOverrideNotFound,
                "Runtime binding override does not exist.");
        }

        return HWJ_PlayerInputRebindResult.Success(actionId);
    }

    public void ResetRuntimeBindings()
    {
        runtimeBindingOverrides.Clear();
    }

    public void SetAllowDuplicateRuntimeBindings(bool allowDuplicates)
    {
        allowDuplicateRuntimeBindings = allowDuplicates;
    }

    public bool TryGetEffectiveBinding(
        HWJ_PlayerInputActionId actionId,
        out HWJ_PlayerInputRuntimeBinding binding)
    {
        if (runtimeBindingOverrides.TryGetValue(actionId, out binding))
        {
            return HasAnyBinding(binding);
        }

        if (inputBindingData != null
            && inputBindingData.TryGetBinding(actionId, out HWJ_PlayerInputBindingEntry entry))
        {
            binding = HWJ_PlayerInputRuntimeBinding.FromEntry(entry);
            return HasAnyBinding(binding);
        }

        return TryGetDefaultBinding(actionId, out binding);
    }

    public bool WasSkillSlotPressedThisFrame(int slotIndex)
    {
        return TryGetSkillSlotActionId(slotIndex, out HWJ_PlayerInputActionId actionId)
            && WasActionPressedThisFrame(actionId);
    }

    public HWJ_SaveInputBindingData CreateSaveInputBindingData()
    {
        HWJ_SaveInputBindingData saveInputBindingData = new HWJ_SaveInputBindingData();

        foreach (KeyValuePair<HWJ_PlayerInputActionId, HWJ_PlayerInputRuntimeBinding> pair in runtimeBindingOverrides)
        {
            HWJ_PlayerInputRuntimeBinding binding = pair.Value;

            if (!HasAnyBinding(binding))
            {
                continue;
            }

            saveInputBindingData.AddOrReplace(new HWJ_SaveInputBindingOverrideData
            {
                actionId = binding.ActionId,
                hasKeyboard = binding.HasKeyboard,
                keyboardKeyCode = binding.HasKeyboard ? (int)binding.KeyboardKey : 0,
                hasMouse = binding.HasMouse,
                mouseButton = binding.HasMouse ? binding.MouseButton : HWJ_InputMouseButton.None
            });
        }

        return saveInputBindingData;
    }

    public void ApplySaveInputBindingData(HWJ_SaveInputBindingData saveInputBindingData)
    {
        ResetRuntimeBindings();

        if (saveInputBindingData == null)
        {
            return;
        }

        saveInputBindingData.EnsureLists();

        for (int i = 0; i < saveInputBindingData.bindingOverrides.Count; i++)
        {
            HWJ_SaveInputBindingOverrideData bindingOverride = saveInputBindingData.bindingOverrides[i];

            if (bindingOverride == null)
            {
                continue;
            }

            if (bindingOverride.hasKeyboard
                && !System.Enum.IsDefined(typeof(KeyCode), bindingOverride.keyboardKeyCode))
            {
                continue;
            }

            if (bindingOverride.hasMouse
                && !System.Enum.IsDefined(typeof(HWJ_InputMouseButton), bindingOverride.mouseButton))
            {
                continue;
            }

            KeyCode keyboardKey = bindingOverride.hasKeyboard
                ? (KeyCode)bindingOverride.keyboardKeyCode
                : KeyCode.None;
            HWJ_InputMouseButton mouseButton = bindingOverride.hasMouse
                ? bindingOverride.mouseButton
                : HWJ_InputMouseButton.None;

            SetRuntimeBinding(new HWJ_PlayerInputRuntimeBinding(
                bindingOverride.actionId,
                keyboardKey,
                mouseButton,
                bindingOverride.hasKeyboard && keyboardKey != KeyCode.None,
                bindingOverride.hasMouse && mouseButton != HWJ_InputMouseButton.None));
        }
    }

    private Vector2 ReadMoveInputFromBindings()
    {
        Vector2 move = Vector2.zero;

        if (IsActionPressed(HWJ_PlayerInputActionId.MoveLeft))
        {
            move.x -= 1f;
        }

        if (IsActionPressed(HWJ_PlayerInputActionId.MoveRight))
        {
            move.x += 1f;
        }

        if (IsActionPressed(HWJ_PlayerInputActionId.MoveDown))
        {
            move.y -= 1f;
        }

        if (IsActionPressed(HWJ_PlayerInputActionId.MoveUp))
        {
            move.y += 1f;
        }

        return move;
    }

    private bool WasActionPressedThisFrame(HWJ_PlayerInputActionId actionId)
    {
        return TryGetEffectiveBinding(actionId, out HWJ_PlayerInputRuntimeBinding binding)
            && (WasKeyboardPressedThisFrame(binding) || WasMousePressedThisFrame(binding));
    }

    private bool WasActionReleasedThisFrame(HWJ_PlayerInputActionId actionId)
    {
        return TryGetEffectiveBinding(actionId, out HWJ_PlayerInputRuntimeBinding binding)
            && (WasKeyboardReleasedThisFrame(binding) || WasMouseReleasedThisFrame(binding));
    }

    private bool IsActionPressed(HWJ_PlayerInputActionId actionId)
    {
        return TryGetEffectiveBinding(actionId, out HWJ_PlayerInputRuntimeBinding binding)
            && (IsKeyboardPressed(binding) || IsMousePressed(binding));
    }

    private static bool HasAnyBinding(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard || binding.HasMouse;
    }

    private HWJ_PlayerInputRebindResult ValidateRuntimeBinding(HWJ_PlayerInputRuntimeBinding binding)
    {
        if (binding.HasKeyboard)
        {
            if (binding.KeyboardKey == KeyCode.None)
            {
                return HWJ_PlayerInputRebindResult.Failure(
                    binding.ActionId,
                    HWJ_PlayerInputRebindFailureCode.EmptyKeyboardKey,
                    "Keyboard binding cannot use KeyCode.None.");
            }

            if (!System.Enum.IsDefined(typeof(KeyCode), binding.KeyboardKey))
            {
                return HWJ_PlayerInputRebindResult.Failure(
                    binding.ActionId,
                    HWJ_PlayerInputRebindFailureCode.InvalidKeyboardKey,
                    "Keyboard binding uses an undefined KeyCode value.");
            }
        }

        if (binding.HasMouse)
        {
            if (binding.MouseButton == HWJ_InputMouseButton.None)
            {
                return HWJ_PlayerInputRebindResult.Failure(
                    binding.ActionId,
                    HWJ_PlayerInputRebindFailureCode.EmptyMouseButton,
                    "Mouse binding cannot use None.");
            }

            if (!System.Enum.IsDefined(typeof(HWJ_InputMouseButton), binding.MouseButton))
            {
                return HWJ_PlayerInputRebindResult.Failure(
                    binding.ActionId,
                    HWJ_PlayerInputRebindFailureCode.InvalidMouseButton,
                    "Mouse binding uses an undefined button value.");
            }
        }

        if (!allowDuplicateRuntimeBindings
            && TryFindRuntimeBindingConflict(binding, out HWJ_PlayerInputActionId conflictActionId))
        {
            return HWJ_PlayerInputRebindResult.Conflict(binding.ActionId, conflictActionId);
        }

        return HWJ_PlayerInputRebindResult.Success(binding.ActionId);
    }

    private bool TryFindRuntimeBindingConflict(
        HWJ_PlayerInputRuntimeBinding candidateBinding,
        out HWJ_PlayerInputActionId conflictActionId)
    {
        System.Array actionValues = System.Enum.GetValues(typeof(HWJ_PlayerInputActionId));

        for (int i = 0; i < actionValues.Length; i++)
        {
            HWJ_PlayerInputActionId otherActionId = (HWJ_PlayerInputActionId)actionValues.GetValue(i);

            if (otherActionId == candidateBinding.ActionId)
            {
                continue;
            }

            if (!TryGetEffectiveBinding(otherActionId, out HWJ_PlayerInputRuntimeBinding existingBinding))
            {
                continue;
            }

            if (BindingsConflict(candidateBinding, existingBinding))
            {
                conflictActionId = otherActionId;
                return true;
            }
        }

        conflictActionId = default(HWJ_PlayerInputActionId);
        return false;
    }

    private static bool BindingsConflict(
        HWJ_PlayerInputRuntimeBinding candidateBinding,
        HWJ_PlayerInputRuntimeBinding existingBinding)
    {
        bool keyboardConflict = candidateBinding.HasKeyboard
            && existingBinding.HasKeyboard
            && candidateBinding.KeyboardKey != KeyCode.None
            && candidateBinding.KeyboardKey == existingBinding.KeyboardKey;
        bool mouseConflict = candidateBinding.HasMouse
            && existingBinding.HasMouse
            && candidateBinding.MouseButton != HWJ_InputMouseButton.None
            && candidateBinding.MouseButton == existingBinding.MouseButton;

        return keyboardConflict || mouseConflict;
    }

    private static bool TryGetDefaultBinding(
        HWJ_PlayerInputActionId actionId,
        out HWJ_PlayerInputRuntimeBinding binding)
    {
        switch (actionId)
        {
            case HWJ_PlayerInputActionId.MoveLeft:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.LeftArrow);
                return true;
            case HWJ_PlayerInputActionId.MoveRight:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.RightArrow);
                return true;
            case HWJ_PlayerInputActionId.MoveUp:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.UpArrow);
                return true;
            case HWJ_PlayerInputActionId.MoveDown:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.DownArrow);
                return true;
            case HWJ_PlayerInputActionId.Jump:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.Space);
                return true;
            case HWJ_PlayerInputActionId.Dash:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.LeftShift);
                return true;
            case HWJ_PlayerInputActionId.Attack:
                binding = HWJ_PlayerInputRuntimeBinding.FromMouse(actionId, HWJ_InputMouseButton.Left);
                return true;
            case HWJ_PlayerInputActionId.Interact:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.E);
                return true;
            case HWJ_PlayerInputActionId.ExitPossession:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.Q);
                return true;
            case HWJ_PlayerInputActionId.SkillSlot1:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.Alpha1);
                return true;
            case HWJ_PlayerInputActionId.SkillSlot2:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.Alpha2);
                return true;
            case HWJ_PlayerInputActionId.SkillSlot3:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.Alpha3);
                return true;
            case HWJ_PlayerInputActionId.SkillSlot4:
                binding = HWJ_PlayerInputRuntimeBinding.FromKeyboard(actionId, KeyCode.Alpha4);
                return true;
            default:
                binding = default(HWJ_PlayerInputRuntimeBinding);
                return false;
        }
    }

    private static bool TryGetSkillSlotActionId(int slotIndex, out HWJ_PlayerInputActionId actionId)
    {
        switch (slotIndex)
        {
            case 0:
                actionId = HWJ_PlayerInputActionId.SkillSlot1;
                return true;
            case 1:
                actionId = HWJ_PlayerInputActionId.SkillSlot2;
                return true;
            case 2:
                actionId = HWJ_PlayerInputActionId.SkillSlot3;
                return true;
            case 3:
                actionId = HWJ_PlayerInputActionId.SkillSlot4;
                return true;
            default:
                actionId = default(HWJ_PlayerInputActionId);
                return false;
        }
    }

    private static bool IsActionIdDefined(HWJ_PlayerInputActionId actionId)
    {
        return actionId >= HWJ_PlayerInputActionId.MoveLeft
            && actionId <= HWJ_PlayerInputActionId.SkillSlot4;
    }

}
