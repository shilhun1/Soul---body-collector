using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public class hys_BodyDecaySlider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;

    [Header("Options")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool hideWhenNotPossessed;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        CacheReferences();
        UpdateSlider();
    }

    private void CacheReferences()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        if (!autoFindPlayer)
        {
            return;
        }

        if (playerResolver == null)
        {
            if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
            {
                playerResolver = HWJ_GameAccess.Manager.PlayerResolver;
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerResolver = player.GetComponent<HWJ_RootObjectDataResolver>();
                }
            }
        }

        if (playerResolver == null)
        {
            return;
        }

        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = playerResolver.GetComponent<HWJ_BodyDecaySystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = playerResolver.GetComponent<HWJ_SoulSystem>();
        }
    }

    private void UpdateSlider()
    {
        if (slider == null)
        {
            return;
        }

        float maxDecay = 0f;
        bool showDecay = soulSystem != null
            && soulSystem.CurrentState == HWJ_SoulRuntimeState.Body
            && bodyDecaySystem != null
            && bodyDecaySystem.IsDecaying
            && TryGetMaxDecay(out maxDecay)
            && maxDecay > 0f;

        if (hideWhenNotPossessed)
        {
            slider.gameObject.SetActive(showDecay);
        }

        if (!showDecay)
        {
            return;
        }

        slider.maxValue = maxDecay;
        slider.value = Mathf.Clamp(bodyDecaySystem.CurrentDecayValue, 0f, maxDecay);
    }

    private bool TryGetMaxDecay(out float maxDecay)
    {
        maxDecay = 0f;

        if (playerResolver == null || !playerResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
        {
            return false;
        }

        if (playerData.BodyDecay == null)
        {
            return false;
        }

        maxDecay = playerData.BodyDecay.maxDecayValue;
        return true;
    }
}
