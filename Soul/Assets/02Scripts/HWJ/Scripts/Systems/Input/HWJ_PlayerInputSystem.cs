using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public partial class HWJ_PlayerInputSystem : MonoBehaviour
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

}
