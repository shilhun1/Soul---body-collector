using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_PlayerInputBindingData", menuName = "HWJ/Data/System/Player Input Bindings")]
public class HWJ_PlayerInputBindingDataSO : ScriptableObject
{
    // Definition data only. User changes should be stored as runtime overrides or save data.
    [SerializeField] private HWJ_PlayerInputBindingEntry[] bindings =
    {
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.MoveLeft, KeyCode.LeftArrow),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.MoveRight, KeyCode.RightArrow),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.MoveUp, KeyCode.UpArrow),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.MoveDown, KeyCode.DownArrow),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.Jump, KeyCode.Space),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.Dash, KeyCode.LeftShift),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.Attack, HWJ_InputMouseButton.Left),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.Interact, KeyCode.E),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.ExitPossession, KeyCode.Q),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.SkillSlot1, KeyCode.Alpha1),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.SkillSlot2, KeyCode.Alpha2),
        new HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId.SkillSlot3, KeyCode.Alpha3)
    };

    public int BindingCount => bindings != null ? bindings.Length : 0;

    public bool TryGetBindingAt(int index, out HWJ_PlayerInputBindingEntry binding)
    {
        binding = null;

        if (bindings == null || index < 0 || index >= bindings.Length)
        {
            return false;
        }

        binding = bindings[index];
        return binding != null;
    }

    public bool TryGetBinding(HWJ_PlayerInputActionId actionId, out HWJ_PlayerInputBindingEntry binding)
    {
        binding = null;

        if (bindings == null)
        {
            return false;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            HWJ_PlayerInputBindingEntry candidate = bindings[i];

            if (candidate != null && candidate.ActionId == actionId)
            {
                binding = candidate;
                return true;
            }
        }

        return false;
    }

    public KeyCode GetKeyboardKey(HWJ_PlayerInputActionId actionId, KeyCode fallbackKey)
    {
        return TryGetBinding(actionId, out HWJ_PlayerInputBindingEntry binding)
            ? binding.KeyboardKey
            : fallbackKey;
    }

    public HWJ_InputMouseButton GetMouseButton(HWJ_PlayerInputActionId actionId, HWJ_InputMouseButton fallbackButton)
    {
        return TryGetBinding(actionId, out HWJ_PlayerInputBindingEntry binding)
            ? binding.MouseButton
            : fallbackButton;
    }
}

[Serializable]
public class HWJ_PlayerInputBindingEntry
{
    [SerializeField] private HWJ_PlayerInputActionId actionId;
    [SerializeField] private KeyCode keyboardKey;
    [SerializeField] private HWJ_InputMouseButton mouseButton;

    public HWJ_PlayerInputBindingEntry()
    {
    }

    public HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId actionId, KeyCode keyboardKey)
    {
        this.actionId = actionId;
        this.keyboardKey = keyboardKey;
        mouseButton = HWJ_InputMouseButton.None;
    }

    public HWJ_PlayerInputBindingEntry(HWJ_PlayerInputActionId actionId, HWJ_InputMouseButton mouseButton)
    {
        this.actionId = actionId;
        keyboardKey = KeyCode.None;
        this.mouseButton = mouseButton;
    }

    public HWJ_PlayerInputActionId ActionId => actionId;
    public KeyCode KeyboardKey => keyboardKey;
    public HWJ_InputMouseButton MouseButton => mouseButton;
}

public readonly struct HWJ_PlayerInputRuntimeBinding
{
    public HWJ_PlayerInputRuntimeBinding(
        HWJ_PlayerInputActionId actionId,
        KeyCode keyboardKey,
        HWJ_InputMouseButton mouseButton,
        bool hasKeyboard,
        bool hasMouse)
    {
        ActionId = actionId;
        KeyboardKey = keyboardKey;
        MouseButton = mouseButton;
        HasKeyboard = hasKeyboard;
        HasMouse = hasMouse;
    }

    public HWJ_PlayerInputActionId ActionId { get; }
    public KeyCode KeyboardKey { get; }
    public HWJ_InputMouseButton MouseButton { get; }
    public bool HasKeyboard { get; }
    public bool HasMouse { get; }

    public static HWJ_PlayerInputRuntimeBinding FromKeyboard(HWJ_PlayerInputActionId actionId, KeyCode keyboardKey)
    {
        return new HWJ_PlayerInputRuntimeBinding(
            actionId,
            keyboardKey,
            HWJ_InputMouseButton.None,
            keyboardKey != KeyCode.None,
            false);
    }

    public static HWJ_PlayerInputRuntimeBinding FromMouse(HWJ_PlayerInputActionId actionId, HWJ_InputMouseButton mouseButton)
    {
        return new HWJ_PlayerInputRuntimeBinding(
            actionId,
            KeyCode.None,
            mouseButton,
            false,
            mouseButton != HWJ_InputMouseButton.None);
    }

    public static HWJ_PlayerInputRuntimeBinding FromEntry(HWJ_PlayerInputBindingEntry entry)
    {
        if (entry == null)
        {
            return default(HWJ_PlayerInputRuntimeBinding);
        }

        return new HWJ_PlayerInputRuntimeBinding(
            entry.ActionId,
            entry.KeyboardKey,
            entry.MouseButton,
            entry.KeyboardKey != KeyCode.None,
            entry.MouseButton != HWJ_InputMouseButton.None);
    }
}

public readonly struct HWJ_PlayerInputRebindResult
{
    public HWJ_PlayerInputRebindResult(
        bool succeeded,
        HWJ_PlayerInputActionId actionId,
        HWJ_PlayerInputRebindFailureCode failureCode,
        HWJ_PlayerInputActionId conflictActionId,
        string message)
    {
        Succeeded = succeeded;
        ActionId = actionId;
        FailureCode = failureCode;
        ConflictActionId = conflictActionId;
        Message = message;
    }

    public bool Succeeded { get; }
    public HWJ_PlayerInputActionId ActionId { get; }
    public HWJ_PlayerInputRebindFailureCode FailureCode { get; }
    public HWJ_PlayerInputActionId ConflictActionId { get; }
    public string Message { get; }
    public bool HasConflict => FailureCode == HWJ_PlayerInputRebindFailureCode.BindingAlreadyUsed;

    public static HWJ_PlayerInputRebindResult Success(HWJ_PlayerInputActionId actionId)
    {
        return new HWJ_PlayerInputRebindResult(
            true,
            actionId,
            HWJ_PlayerInputRebindFailureCode.None,
            default(HWJ_PlayerInputActionId),
            "Input binding updated.");
    }

    public static HWJ_PlayerInputRebindResult Failure(
        HWJ_PlayerInputActionId actionId,
        HWJ_PlayerInputRebindFailureCode failureCode,
        string message)
    {
        return new HWJ_PlayerInputRebindResult(
            false,
            actionId,
            failureCode,
            default(HWJ_PlayerInputActionId),
            message);
    }

    public static HWJ_PlayerInputRebindResult Conflict(
        HWJ_PlayerInputActionId actionId,
        HWJ_PlayerInputActionId conflictActionId)
    {
        return new HWJ_PlayerInputRebindResult(
            false,
            actionId,
            HWJ_PlayerInputRebindFailureCode.BindingAlreadyUsed,
            conflictActionId,
            $"Input binding is already used by {conflictActionId}.");
    }
}
