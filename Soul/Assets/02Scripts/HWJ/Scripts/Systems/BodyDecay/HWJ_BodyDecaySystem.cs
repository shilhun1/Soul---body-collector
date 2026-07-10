using UnityEngine;

[RequireComponent(typeof(HWJ_RootObjectDataResolver))]
[RequireComponent(typeof(HWJ_SoulSystem))]
public class HWJ_BodyDecaySystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private float currentDecayValue;
    [SerializeField] private bool resetDecayWhenEnterBody = true;
    [SerializeField] private bool isDecaying;
    [SerializeField] private string runtimeStateMessage;

    private float decayTimer;
    private bool hasInitializedDecay;
    private HWJ_SoulRuntimeState previousSoulState;
    private HWJ_RootObjectDataResolver previousPossessedBodyResolver;

    public float CurrentDecayValue => currentDecayValue;
    public bool IsDecaying => isDecaying;
    public string RuntimeStateMessage => runtimeStateMessage;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();

        if (TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            SetDecayToMax(bodyDecay);
        }

        previousSoulState = soulSystem != null ? soulSystem.CurrentState : HWJ_SoulRuntimeState.Body;
    }

    private void Update()
    {
        ResolveReferences();

        if (soulSystem == null)
        {
            StopDecay("Missing HWJ_SoulSystem.");
            return;
        }

        HWJ_SoulRuntimeState currentSoulState = soulSystem.CurrentState;
        HWJ_RootObjectDataResolver currentPossessedBodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : null;

        if (currentSoulState == HWJ_SoulRuntimeState.Body
            && (previousSoulState != HWJ_SoulRuntimeState.Body
                || previousPossessedBodyResolver != currentPossessedBodyResolver)
            && currentPossessedBodyResolver != null
            && resetDecayWhenEnterBody)
        {
            ResetDecay();
        }

        previousSoulState = currentSoulState;
        previousPossessedBodyResolver = currentPossessedBodyResolver;

        if (currentSoulState != HWJ_SoulRuntimeState.Body)
        {
            StopDecay($"Soul state is {currentSoulState}.");
            return;
        }

        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            StopDecay("Body decay waits for an active possessed corpse.");
            return;
        }

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            StopDecay("Missing player BodyDecay data.");
            return;
        }

        if (!bodyDecay.startDecayOnEnterBody)
        {
            StopDecay("Body decay is disabled on the PlayerTypeData asset.");
            return;
        }

        if (!hasInitializedDecay)
        {
            SetDecayToMax(bodyDecay);
        }

        isDecaying = true;
        runtimeStateMessage = "Body decay is running.";

        if (currentDecayValue <= 0f)
        {
            TryEnterSoulStateWhenDecayEmpty(bodyDecay);
            return;
        }

        float tickSeconds = Mathf.Max(0.01f, bodyDecay.decayTickSeconds);
        decayTimer += Time.deltaTime;

        if (decayTimer < tickSeconds)
        {
            return;
        }

        decayTimer = 0f;
        currentDecayValue -= bodyDecay.decayAmountPerTick;
        TryEnterSoulStateWhenDecayEmpty(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    public void ResetDecay()
    {
        ResolveReferences();

        if (TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            SetDecayToMax(bodyDecay);
        }
    }

    public void RestoreDecaySnapshot(float restoredDecayValue)
    {
        ResolveReferences();

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        currentDecayValue = Mathf.Clamp(restoredDecayValue, 0f, Mathf.Max(0f, bodyDecay.maxDecayValue));
        decayTimer = 0f;
        hasInitializedDecay = true;
        previousSoulState = soulSystem != null ? soulSystem.CurrentState : previousSoulState;
        previousPossessedBodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : previousPossessedBodyResolver;
    }

    public void ApplyHitDecayPenalty()
    {
        ApplyHitDecayPenalty(0f);
    }

    public void ApplyHitDecayPenalty(float incomingDamage)
    {
        ResolveReferences();

        if (soulSystem == null
            || soulSystem.CurrentState != HWJ_SoulRuntimeState.Body
            || possessionSystem == null
            || !possessionSystem.HasActivePossessedBody)
        {
            return;
        }

        if (!TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay))
        {
            return;
        }

        if (!hasInitializedDecay)
        {
            SetDecayToMax(bodyDecay);
        }

        float decayPenalty = Mathf.Max(bodyDecay.hitDecayPenalty, incomingDamage);
        currentDecayValue -= decayPenalty;
        TryEnterSoulStateWhenDecayEmpty(bodyDecay);
        SavePlayerRuntimeSnapshotIfOwner();
    }

    private void ResolveReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    private bool TryGetBodyDecayData(out HWJ_BodyDecayData bodyDecay)
    {
        bodyDecay = null;

        if (dataResolver == null || !dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        bodyDecay = playerData.BodyDecay;
        return bodyDecay != null;
    }

    private void SetDecayToMax(HWJ_BodyDecayData bodyDecay)
    {
        currentDecayValue = Mathf.Max(0f, bodyDecay.maxDecayValue);
        decayTimer = 0f;
        hasInitializedDecay = true;
    }

    private void StopDecay(string message)
    {
        isDecaying = false;
        decayTimer = 0f;
        runtimeStateMessage = message;
    }

    private void TryEnterSoulStateWhenDecayEmpty(HWJ_BodyDecayData bodyDecay)
    {
        if (currentDecayValue > 0f)
        {
            return;
        }

        currentDecayValue = 0f;
        isDecaying = false;

        if (!bodyDecay.enterSoulStateWhenEmpty)
        {
            runtimeStateMessage = "Body decay reached zero, but soul transition is disabled.";
            return;
        }

        if (soulSystem == null)
        {
            runtimeStateMessage = "Body decay reached zero, but HWJ_SoulSystem is missing.";
            return;
        }

        runtimeStateMessage = "Body decay reached zero. Entering soul state.";
        soulSystem.EnterSoulState();
    }

    private void SavePlayerRuntimeSnapshotIfOwner()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerBodyDecay == this)
        {
            HWJ_GameAccess.Manager.SavePlayerRuntimeSnapshot();
        }
    }
}
