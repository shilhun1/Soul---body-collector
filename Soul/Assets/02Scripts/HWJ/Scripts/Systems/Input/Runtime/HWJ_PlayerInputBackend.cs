using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Reads Unity Input System controls or the legacy keyboard/mouse fallback.
/// It also registers the player input component with HWJ_GameManager.
/// </summary>
public partial class HWJ_PlayerInputSystem
{
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
