using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_PossessedBodySystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PossessedBodyRuntimeState currentBodyState;
    [SerializeField] private string runtimeStateMessage;

    public HWJ_PossessedBodyRuntimeState CurrentBodyState => currentBodyState;
    public string RuntimeStateMessage => runtimeStateMessage;
    public bool HasCurrentBody => currentBodyState != null
        && currentBodyState.IsPossessed
        && !currentBodyState.IsCollapsed;
    public bool IsCurrentPossessionMentalDepleted => currentBodyState != null
        && currentBodyState.IsPossessionMentalDepleted;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public bool TryGetCurrentBodyState(out HWJ_PossessedBodyRuntimeState state)
    {
        state = currentBodyState;
        return state != null;
    }

    public bool TryGetActiveBodyState(out HWJ_PossessedBodyRuntimeState state)
    {
        state = currentBodyState;
        return HasCurrentBody;
    }

    public bool CreateCurrentBodyState(
        HWJ_RootObjectDataResolver bodyResolver,
        HWJ_BodyDecayData decayData,
        bool refillHpToMax = true,
        bool resetDecayToInitial = true)
    {
        ResolveReferences();

        if (bodyResolver == null || bodyResolver.RootObjectData == null)
        {
            runtimeStateMessage = "Create possessed body runtime state failed: missing body data.";
            currentBodyState = null;
            RaiseRuntimeStateChanged();
            return false;
        }

        float maxHp = ResolveMaxHp(bodyResolver);
        float initialHp = refillHpToMax || currentBodyState == null
            ? maxHp
            : currentBodyState.CurrentHp;
        float maxDecayValue = ResolveMaxDecayValue(decayData);
        float initialDecayValue = resetDecayToInitial || currentBodyState == null
            ? ResolveInitialDecayValue(decayData)
            : currentBodyState.CurrentDecayValue;

        currentBodyState = HWJ_PossessedBodyRuntimeState.Create(
            ResolveDefinitionDataId(bodyResolver),
            bodyResolver.GetInstanceID(),
            maxHp,
            initialHp,
            maxDecayValue,
            initialDecayValue);

        runtimeStateMessage = $"Created possessed body runtime state: {currentBodyState.DefinitionDataId}.";
        RaiseRuntimeStateChanged();
        return true;
    }

    public void ClearCurrentBodyState(bool markCollapsed = false)
    {
        if (currentBodyState == null)
        {
            return;
        }

        if (markCollapsed)
        {
            currentBodyState.MarkCollapsed();
        }
        else
        {
            currentBodyState.SetPossessed(false);
        }

        currentBodyState.ClearTransientState();
        runtimeStateMessage = markCollapsed
            ? "Possessed body runtime state collapsed."
            : "Possessed body runtime state cleared.";
        currentBodyState = null;
        RaiseRuntimeStateChanged();
    }

    public void MarkCurrentBodyCollapsed()
    {
        if (currentBodyState == null)
        {
            return;
        }

        currentBodyState.MarkCollapsed();
        runtimeStateMessage = "Possessed body runtime state marked collapsed.";
        RaiseRuntimeStateChanged();
    }

    public bool SetCurrentHp(float value)
    {
        if (currentBodyState == null)
        {
            runtimeStateMessage = "Set body HP failed: no runtime body state.";
            return false;
        }

        currentBodyState.SetCurrentHp(value);
        runtimeStateMessage = "Possessed body runtime HP updated.";
        RaiseRuntimeStateChanged();
        return true;
    }

    public bool SetCurrentDecayValue(float value)
    {
        if (currentBodyState == null)
        {
            runtimeStateMessage = "Set body decay failed: no runtime body state.";
            return false;
        }

        currentBodyState.SetCurrentDecayValue(value);
        runtimeStateMessage = "Possessed body runtime possession mental updated.";
        RaiseRuntimeStateChanged();
        return true;
    }

    private void ResolveReferences()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    private static float ResolveMaxHp(HWJ_RootObjectDataResolver bodyResolver)
    {
        return bodyResolver != null && bodyResolver.Status != null
            ? Mathf.Max(0f, bodyResolver.Status.maxHp)
            : 0f;
    }

    private static float ResolveMaxDecayValue(HWJ_BodyDecayData decayData)
    {
        return decayData != null ? Mathf.Max(0f, decayData.maxDecayValue) : 0f;
    }

    private static float ResolveInitialDecayValue(HWJ_BodyDecayData decayData)
    {
        if (decayData == null)
        {
            return 0f;
        }

        return Mathf.Clamp(decayData.initialDecayValue, 0f, Mathf.Max(0f, decayData.maxDecayValue));
    }

    private static string ResolveDefinitionDataId(HWJ_RootObjectDataResolver bodyResolver)
    {
        if (bodyResolver == null || bodyResolver.RootObjectData == null)
        {
            return string.Empty;
        }

        HWJ_IdentityData identity = bodyResolver.RootObjectData.Identity;

        if (identity != null && !string.IsNullOrEmpty(identity.objectId))
        {
            return identity.objectId;
        }

        return bodyResolver.RootObjectData.name;
    }

    private void RaiseRuntimeStateChanged()
    {
        HWJ_GameplayEvents.RaisePossessedBodyRuntimeStateChanged(
            new HWJ_PossessedBodyRuntimeStateChangedEvent(this, currentBodyState, runtimeStateMessage));
    }
}
