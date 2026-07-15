using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class HWJ_PlayerInputSystem : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions - Input System")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference exitPossessionAction;
#endif

    [Space(8f)]
    [Header("Input Binding Data")]
    [SerializeField] private HWJ_PlayerInputBindingDataSO inputBindingData;

    [Space(8f)]
    [Header("Runtime Rebinding Policy")]
    [SerializeField] private bool allowDuplicateRuntimeBindings;

    // Runtime overrides are changed by settings UI or save loading without mutating the source SO.
    private readonly Dictionary<HWJ_PlayerInputActionId, HWJ_PlayerInputRuntimeBinding> runtimeBindingOverrides =
        new Dictionary<HWJ_PlayerInputActionId, HWJ_PlayerInputRuntimeBinding>();

    public HWJ_PlayerInputBindingDataSO InputBindingData => inputBindingData;
    public int RuntimeBindingOverrideCount => runtimeBindingOverrides.Count;
    public bool AllowDuplicateRuntimeBindings => allowDuplicateRuntimeBindings;

    public Vector2 MoveInput
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 input = ReadVector2(moveAction, "Move");

            if (input != Vector2.zero)
            {
                return input;
            }
#endif

            return ReadMoveInputFromBindings();
        }
    }

    public bool JumpPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(jumpAction, "Jump")
                || WasActionPressedThisFrame(HWJ_PlayerInputActionId.Jump);
#else
            return WasActionPressedThisFrame(HWJ_PlayerInputActionId.Jump);
#endif
        }
    }

    public bool JumpHeld
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return IsPressed(jumpAction, "Jump")
                || IsActionPressed(HWJ_PlayerInputActionId.Jump);
#else
            return IsActionPressed(HWJ_PlayerInputActionId.Jump);
#endif
        }
    }

    public bool DownHeld => IsActionPressed(HWJ_PlayerInputActionId.MoveDown);

    public bool DashPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(dashAction, "Dash")
                || WasActionPressedThisFrame(HWJ_PlayerInputActionId.Dash);
#else
            return WasActionPressedThisFrame(HWJ_PlayerInputActionId.Dash);
#endif
        }
    }

    public bool AttackPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(attackAction, "Attack")
                || WasActionPressedThisFrame(HWJ_PlayerInputActionId.Attack);
#else
            return WasActionPressedThisFrame(HWJ_PlayerInputActionId.Attack);
#endif
        }
    }

    public bool AttackReleasedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasReleasedThisFrame(attackAction, "Attack")
                || WasActionReleasedThisFrame(HWJ_PlayerInputActionId.Attack);
#else
            return WasActionReleasedThisFrame(HWJ_PlayerInputActionId.Attack);
#endif
        }
    }

    public bool AttackHeld
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return IsPressed(attackAction, "Attack")
                || IsActionPressed(HWJ_PlayerInputActionId.Attack);
#else
            return IsActionPressed(HWJ_PlayerInputActionId.Attack);
#endif
        }
    }

    public bool InteractPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(interactAction, "Interact")
                || WasActionPressedThisFrame(HWJ_PlayerInputActionId.Interact);
#else
            return WasActionPressedThisFrame(HWJ_PlayerInputActionId.Interact);
#endif
        }
    }

    public bool ExitPossessionPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(exitPossessionAction, "ExitPossession")
                || WasActionPressedThisFrame(HWJ_PlayerInputActionId.ExitPossession);
#else
            return WasActionPressedThisFrame(HWJ_PlayerInputActionId.ExitPossession);
#endif
        }
    }

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
            default:
                actionId = default(HWJ_PlayerInputActionId);
                return false;
        }
    }

    private static bool IsActionIdDefined(HWJ_PlayerInputActionId actionId)
    {
        return actionId >= HWJ_PlayerInputActionId.MoveLeft
            && actionId <= HWJ_PlayerInputActionId.SkillSlot3;
    }

    private void OnEnable()
    {
        RegisterWithGameManager();

#if ENABLE_INPUT_SYSTEM
        EnableAction(moveAction);
        EnableAction(jumpAction);
        EnableAction(dashAction);
        EnableAction(attackAction);
        EnableAction(interactAction);
        EnableAction(exitPossessionAction);
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        DisableAction(moveAction);
        DisableAction(jumpAction);
        DisableAction(dashAction);
        DisableAction(attackAction);
        DisableAction(interactAction);
        DisableAction(exitPossessionAction);
#endif

        UnregisterFromGameManager();
    }

    private void RegisterWithGameManager()
    {
        if (HWJ_GameAccess.HasManager)
        {
            HWJ_GameAccess.Manager.RegisterPlayerInput(this);
        }
    }

    private void UnregisterFromGameManager()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.PlayerInput == this)
        {
            HWJ_GameAccess.Manager.RegisterPlayerInput(null);
        }
    }

#if ENABLE_INPUT_SYSTEM
    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    private static Vector2 ReadVector2(InputActionReference actionReference, string expectedActionName)
    {
        if (!IsExpectedAction(actionReference, expectedActionName))
        {
            return Vector2.zero;
        }

        return actionReference.action.ReadValue<Vector2>();
    }

    private static bool WasPressedThisFrame(InputActionReference actionReference, string expectedActionName)
    {
        return IsExpectedAction(actionReference, expectedActionName)
            && actionReference.action.WasPressedThisFrame();
    }

    private static bool WasReleasedThisFrame(InputActionReference actionReference, string expectedActionName)
    {
        return IsExpectedAction(actionReference, expectedActionName)
            && actionReference.action.WasReleasedThisFrame();
    }

    private static bool IsPressed(InputActionReference actionReference, string expectedActionName)
    {
        return IsExpectedAction(actionReference, expectedActionName)
            && actionReference.action.IsPressed();
    }

    private static bool IsExpectedAction(InputActionReference actionReference, string expectedActionName)
    {
        return actionReference != null
            && actionReference.action != null
            && actionReference.action.name == expectedActionName;
    }

    private static bool WasKeyboardPressedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard
            && TryGetKeyboardControl(binding.KeyboardKey, out KeyControl control)
            && control.wasPressedThisFrame;
    }

    private static bool WasKeyboardReleasedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard
            && TryGetKeyboardControl(binding.KeyboardKey, out KeyControl control)
            && control.wasReleasedThisFrame;
    }

    private static bool IsKeyboardPressed(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard
            && TryGetKeyboardControl(binding.KeyboardKey, out KeyControl control)
            && control.isPressed;
    }

    private static bool TryGetKeyboardControl(KeyCode keyCode, out KeyControl control)
    {
        control = null;

        if (Keyboard.current == null || !TryConvertKeyCode(keyCode, out Key key))
        {
            return false;
        }

        control = Keyboard.current[key];
        return control != null;
    }

    private static bool TryConvertKeyCode(KeyCode keyCode, out Key key)
    {
        switch (keyCode)
        {
            case KeyCode.Space:
                key = Key.Space;
                return true;
            case KeyCode.LeftArrow:
                key = Key.LeftArrow;
                return true;
            case KeyCode.RightArrow:
                key = Key.RightArrow;
                return true;
            case KeyCode.UpArrow:
                key = Key.UpArrow;
                return true;
            case KeyCode.DownArrow:
                key = Key.DownArrow;
                return true;
            case KeyCode.LeftShift:
                key = Key.LeftShift;
                return true;
            case KeyCode.RightShift:
                key = Key.RightShift;
                return true;
            case KeyCode.Escape:
                key = Key.Escape;
                return true;
            case KeyCode.Alpha0:
                key = Key.Digit0;
                return true;
            case KeyCode.Alpha1:
                key = Key.Digit1;
                return true;
            case KeyCode.Alpha2:
                key = Key.Digit2;
                return true;
            case KeyCode.Alpha3:
                key = Key.Digit3;
                return true;
            case KeyCode.Alpha4:
                key = Key.Digit4;
                return true;
            case KeyCode.Alpha5:
                key = Key.Digit5;
                return true;
            case KeyCode.Alpha6:
                key = Key.Digit6;
                return true;
            case KeyCode.Alpha7:
                key = Key.Digit7;
                return true;
            case KeyCode.Alpha8:
                key = Key.Digit8;
                return true;
            case KeyCode.Alpha9:
                key = Key.Digit9;
                return true;
            case KeyCode.Keypad0:
                key = Key.Numpad0;
                return true;
            case KeyCode.Keypad1:
                key = Key.Numpad1;
                return true;
            case KeyCode.Keypad2:
                key = Key.Numpad2;
                return true;
            case KeyCode.Keypad3:
                key = Key.Numpad3;
                return true;
            case KeyCode.Keypad4:
                key = Key.Numpad4;
                return true;
            case KeyCode.Keypad5:
                key = Key.Numpad5;
                return true;
            case KeyCode.Keypad6:
                key = Key.Numpad6;
                return true;
            case KeyCode.Keypad7:
                key = Key.Numpad7;
                return true;
            case KeyCode.Keypad8:
                key = Key.Numpad8;
                return true;
            case KeyCode.Keypad9:
                key = Key.Numpad9;
                return true;
            case KeyCode.A:
                key = Key.A;
                return true;
            case KeyCode.B:
                key = Key.B;
                return true;
            case KeyCode.C:
                key = Key.C;
                return true;
            case KeyCode.D:
                key = Key.D;
                return true;
            case KeyCode.E:
                key = Key.E;
                return true;
            case KeyCode.F:
                key = Key.F;
                return true;
            case KeyCode.G:
                key = Key.G;
                return true;
            case KeyCode.H:
                key = Key.H;
                return true;
            case KeyCode.I:
                key = Key.I;
                return true;
            case KeyCode.J:
                key = Key.J;
                return true;
            case KeyCode.K:
                key = Key.K;
                return true;
            case KeyCode.L:
                key = Key.L;
                return true;
            case KeyCode.M:
                key = Key.M;
                return true;
            case KeyCode.N:
                key = Key.N;
                return true;
            case KeyCode.O:
                key = Key.O;
                return true;
            case KeyCode.P:
                key = Key.P;
                return true;
            case KeyCode.Q:
                key = Key.Q;
                return true;
            case KeyCode.R:
                key = Key.R;
                return true;
            case KeyCode.S:
                key = Key.S;
                return true;
            case KeyCode.T:
                key = Key.T;
                return true;
            case KeyCode.U:
                key = Key.U;
                return true;
            case KeyCode.V:
                key = Key.V;
                return true;
            case KeyCode.W:
                key = Key.W;
                return true;
            case KeyCode.X:
                key = Key.X;
                return true;
            case KeyCode.Y:
                key = Key.Y;
                return true;
            case KeyCode.Z:
                key = Key.Z;
                return true;
            default:
                key = Key.None;
                return false;
        }
    }

    private static bool WasMousePressedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasMouse
            && TryGetMouseButtonControl(binding.MouseButton, out ButtonControl control)
            && control.wasPressedThisFrame;
    }

    private static bool WasMouseReleasedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasMouse
            && TryGetMouseButtonControl(binding.MouseButton, out ButtonControl control)
            && control.wasReleasedThisFrame;
    }

    private static bool IsMousePressed(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasMouse
            && TryGetMouseButtonControl(binding.MouseButton, out ButtonControl control)
            && control.isPressed;
    }

    private static bool TryGetMouseButtonControl(HWJ_InputMouseButton button, out ButtonControl control)
    {
        control = null;

        if (Mouse.current == null)
        {
            return false;
        }

        switch (button)
        {
            case HWJ_InputMouseButton.Left:
                control = Mouse.current.leftButton;
                return true;
            case HWJ_InputMouseButton.Right:
                control = Mouse.current.rightButton;
                return true;
            case HWJ_InputMouseButton.Middle:
                control = Mouse.current.middleButton;
                return true;
            default:
                return false;
        }
    }
#else
    private static bool WasKeyboardPressedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard
            && Input.GetKeyDown(binding.KeyboardKey);
    }

    private static bool WasKeyboardReleasedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard
            && Input.GetKeyUp(binding.KeyboardKey);
    }

    private static bool IsKeyboardPressed(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasKeyboard
            && Input.GetKey(binding.KeyboardKey);
    }

    private static bool WasMousePressedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasMouse
            && TryGetLegacyMouseButtonIndex(binding.MouseButton, out int buttonIndex)
            && Input.GetMouseButtonDown(buttonIndex);
    }

    private static bool WasMouseReleasedThisFrame(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasMouse
            && TryGetLegacyMouseButtonIndex(binding.MouseButton, out int buttonIndex)
            && Input.GetMouseButtonUp(buttonIndex);
    }

    private static bool IsMousePressed(HWJ_PlayerInputRuntimeBinding binding)
    {
        return binding.HasMouse
            && TryGetLegacyMouseButtonIndex(binding.MouseButton, out int buttonIndex)
            && Input.GetMouseButton(buttonIndex);
    }

    private static bool TryGetLegacyMouseButtonIndex(HWJ_InputMouseButton button, out int buttonIndex)
    {
        switch (button)
        {
            case HWJ_InputMouseButton.Left:
                buttonIndex = 0;
                return true;
            case HWJ_InputMouseButton.Right:
                buttonIndex = 1;
                return true;
            case HWJ_InputMouseButton.Middle:
                buttonIndex = 2;
                return true;
            default:
                buttonIndex = -1;
                return false;
        }
    }
#endif
}
