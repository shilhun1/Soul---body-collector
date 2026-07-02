using UnityEngine;

public enum HWJ_SoulRuntimeState
{
    Body,
    Soul,
    Dead
}

public class HWJ_SoulSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_SoulRuntimeState currentState = HWJ_SoulRuntimeState.Body;

    public HWJ_SoulRuntimeState CurrentState => currentState;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }
    }

    public void EnterSoulState()
    {
        currentState = HWJ_SoulRuntimeState.Soul;
    }

    public void EnterBodyState()
    {
        currentState = HWJ_SoulRuntimeState.Body;
    }

    public void EnterDeadState()
    {
        currentState = HWJ_SoulRuntimeState.Dead;
    }
}
