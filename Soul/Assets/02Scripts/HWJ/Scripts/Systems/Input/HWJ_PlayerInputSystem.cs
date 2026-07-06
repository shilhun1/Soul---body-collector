using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HWJ_PlayerInputSystem : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference exitPossessionAction;
#endif

    public Vector2 MoveInput
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 input = ReadVector2(moveAction, "Move");
            return input != Vector2.zero ? input : ReadKeyboardArrowMove();
#else
            return ReadLegacyArrowMove();
#endif
        }
    }

    public bool JumpPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(jumpAction, "Jump")
                || WasKeyboardPressedThisFrame(Key.Space);
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }

    public bool DashPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(dashAction, "Dash")
                || WasKeyboardPressedThisFrame(Key.LeftShift);
#else
            return Input.GetKeyDown(KeyCode.LeftShift);
#endif
        }
    }

    public bool AttackPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(attackAction, "Attack")
                || WasMouseLeftPressedThisFrame();
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }

    public bool InteractPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(interactAction, "Interact")
                || WasKeyboardPressedThisFrame(Key.E);
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }
    }

    public bool ExitPossessionPressedThisFrame
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return WasPressedThisFrame(exitPossessionAction, "ExitPossession")
                || WasKeyboardPressedThisFrame(Key.Q);
#else
            return Input.GetKeyDown(KeyCode.Q);
#endif
        }
    }

#if !ENABLE_INPUT_SYSTEM
    private static Vector2 ReadLegacyArrowMove()
    {
        Vector2 move = Vector2.zero;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            move.x -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            move.x += 1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            move.y -= 1f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            move.y += 1f;
        }

        return move;
    }
#endif

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(jumpAction);
        EnableAction(dashAction);
        EnableAction(attackAction);
        EnableAction(interactAction);
        EnableAction(exitPossessionAction);
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(jumpAction);
        DisableAction(dashAction);
        DisableAction(attackAction);
        DisableAction(interactAction);
        DisableAction(exitPossessionAction);
    }

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

    private static bool IsExpectedAction(InputActionReference actionReference, string expectedActionName)
    {
        return actionReference != null
            && actionReference.action != null
            && actionReference.action.name == expectedActionName;
    }

    private static Vector2 ReadKeyboardArrowMove()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 move = Vector2.zero;

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            move.x -= 1f;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            move.x += 1f;
        }

        if (Keyboard.current.downArrowKey.isPressed)
        {
            move.y -= 1f;
        }

        if (Keyboard.current.upArrowKey.isPressed)
        {
            move.y += 1f;
        }

        return move;
    }

    private static bool WasKeyboardPressedThisFrame(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        switch (key)
        {
            case Key.Space:
                return Keyboard.current.spaceKey.wasPressedThisFrame;
            case Key.LeftShift:
                return Keyboard.current.leftShiftKey.wasPressedThisFrame;
            case Key.E:
                return Keyboard.current.eKey.wasPressedThisFrame;
            case Key.Q:
                return Keyboard.current.qKey.wasPressedThisFrame;
            default:
                return false;
        }
    }

    private static bool WasMouseLeftPressedThisFrame()
    {
        return Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame;
    }
#endif
}
