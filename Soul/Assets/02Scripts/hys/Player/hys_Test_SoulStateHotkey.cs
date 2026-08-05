using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class hys_Test_SoulStateHotkey : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HWJ_SoulSystem soulSystem;

    [Header("Test Key")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key enterSoulKey = Key.T;
    [SerializeField] private Key enterBodyKey = Key.Y;
#else
    [SerializeField] private KeyCode enterSoulKey = KeyCode.T;
    [SerializeField] private KeyCode enterBodyKey = KeyCode.Y;
#endif
    [SerializeField] private bool refillSoulHp;
    [SerializeField] private bool ignoreWhenDead = true;
    [SerializeField] private bool useDebugLog = true;

    private void Awake()
    {
        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }
    }

    private void Update()
    {
        if (WasEnterSoulKeyPressed())
        {
            TryEnterSoulState();
        }

        if (WasEnterBodyKeyPressed())
        {
            TryEnterBodyState();
        }
    }

    private void TryEnterSoulState()
    {
        if (soulSystem == null)
        {
            Log("Cannot enter Soul state: HWJ_SoulSystem is missing.");
            return;
        }

        if (ignoreWhenDead && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead)
        {
            Log("Ignored: current state is Dead.");
            return;
        }

        Log($"Test key pressed. EnterSoulState called from {soulSystem.CurrentState}.");
        soulSystem.EnterSoulState(refillSoulHp);
    }

    private void TryEnterBodyState()
    {
        if (soulSystem == null)
        {
            Log("Cannot enter Body state: HWJ_SoulSystem is missing.");
            return;
        }

        if (ignoreWhenDead && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead)
        {
            Log("Ignored: current state is Dead.");
            return;
        }

        Log($"Test key pressed. EnterBodyState called from {soulSystem.CurrentState}.");
        soulSystem.EnterBodyState();
    }

    private bool WasEnterSoulKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[enterSoulKey].wasPressedThisFrame;
#else
        return Input.GetKeyDown(enterSoulKey);
#endif
    }

    private bool WasEnterBodyKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[enterBodyKey].wasPressedThisFrame;
#else
        return Input.GetKeyDown(enterBodyKey);
#endif
    }

    private void Log(string message)
    {
        if (useDebugLog)
        {
            Debug.Log($"[hys_Test_SoulStateHotkey] {message}", this);
        }
    }
}
