using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)]
public class hys_PossessionSceneKeeper : MonoBehaviour
{
    private static HWJ_RootObjectDataSO savedPossessedRootData;
    private static float savedCurrentHp;
    private static float savedSoulHp;
    private static float savedPossessedBodyHp;
    private static float savedBodyDecayValue;
    private static Sprite savedSprite;
    private static Color savedColor;
    private static bool savedFlipX;
    private static bool savedFlipY;
    private static RuntimeAnimatorController savedAnimatorController;
    private static bool hasSavedPossession;

    [Header("References")]
    [SerializeField] private HWJ_RootObjectDataResolver playerResolver;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;

    [Header("Options")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoRestoreOnSceneLoaded = true;
    [SerializeField] private float restoreDelaySeconds = 0.1f;

    private static hys_PossessionSceneKeeper instance;
    private HWJ_RootObjectDataResolver virtualPossessedResolver;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        CacheReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LateUpdate()
    {
        CacheReferences();
        SavePossessionSnapshot();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoRestoreOnSceneLoaded && hasSavedPossession)
        {
            StartCoroutine(RestoreAfterSceneLoad());
        }
    }

    private IEnumerator RestoreAfterSceneLoad()
    {
        if (restoreDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(restoreDelaySeconds);
        }

        CacheReferences(true);
        RestorePossessionSnapshot();
    }

    private void CacheReferences(bool forceFindPlayer = false)
    {
        if (forceFindPlayer)
        {
            playerResolver = null;
            possessionSystem = null;
            soulSystem = null;
            runtimeStatus = null;
            bodyDecaySystem = null;
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

        if (possessionSystem == null)
        {
            possessionSystem = playerResolver.GetComponent<HWJ_PossessionSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = playerResolver.GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = playerResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = playerResolver.GetComponent<HWJ_BodyDecaySystem>();
        }
    }

    private void SavePossessionSnapshot()
    {
        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            return;
        }

        HWJ_RootObjectDataResolver possessedResolver = possessionSystem.PossessedBodyResolver;
        if (possessedResolver == null || possessedResolver.RootObjectData == null)
        {
            return;
        }

        savedPossessedRootData = possessedResolver.RootObjectData;
        savedCurrentHp = runtimeStatus != null ? runtimeStatus.CurrentHp : 0f;
        savedSoulHp = runtimeStatus != null ? runtimeStatus.SoulHp : 0f;
        savedPossessedBodyHp = runtimeStatus != null ? runtimeStatus.PossessedBodyHp : savedCurrentHp;
        savedBodyDecayValue = bodyDecaySystem != null ? bodyDecaySystem.CurrentDecayValue : 0f;
        CacheCurrentVisual();
        hasSavedPossession = true;
    }

    private void RestorePossessionSnapshot()
    {
        if (!hasSavedPossession
            || savedPossessedRootData == null
            || playerResolver == null
            || possessionSystem == null
            || soulSystem == null)
        {
            return;
        }

        HWJ_PossessionData possessionData = GetPossessionData(savedPossessedRootData);
        if (possessionData == null)
        {
            return;
        }

        virtualPossessedResolver = CreateVirtualPossessedResolver(savedPossessedRootData);
        SetPrivateField(possessionSystem, "possessedBodyResolver", virtualPossessedResolver);
        SetPrivateField(possessionSystem, "activePossessionBodyData", possessionData);

        soulSystem.EnterBodyState();

        if (runtimeStatus != null)
        {
            runtimeStatus.RestoreHpSnapshot(savedCurrentHp, savedSoulHp, savedPossessedBodyHp);
        }

        if (bodyDecaySystem != null)
        {
            bodyDecaySystem.RestoreDecaySnapshot(savedBodyDecayValue);
        }

        ApplySavedVisual();
    }

    private HWJ_RootObjectDataResolver CreateVirtualPossessedResolver(HWJ_RootObjectDataSO rootData)
    {
        if (virtualPossessedResolver != null)
        {
            Destroy(virtualPossessedResolver.gameObject);
        }

        GameObject virtualObject = new GameObject("hys_VirtualPossessedBody");
        virtualObject.transform.SetParent(transform);
        virtualObject.SetActive(false);

        HWJ_RootObjectDataResolver resolver = virtualObject.AddComponent<HWJ_RootObjectDataResolver>();
        resolver.SetRootObjectData(rootData);
        return resolver;
    }

    private static HWJ_PossessionData GetPossessionData(HWJ_RootObjectDataSO rootData)
    {
        if (rootData == null)
        {
            return null;
        }

        if (rootData.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return enemyData.PossessionBody;
        }

        if (rootData.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            return bossData.PossessionBody;
        }

        return null;
    }

    private void CacheCurrentVisual()
    {
        if (playerResolver == null)
        {
            return;
        }

        SpriteRenderer renderer = playerResolver.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            savedSprite = renderer.sprite;
            savedColor = renderer.color;
            savedFlipX = renderer.flipX;
            savedFlipY = renderer.flipY;
        }

        Animator animator = playerResolver.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            savedAnimatorController = animator.runtimeAnimatorController;
        }
    }

    private void ApplySavedVisual()
    {
        if (playerResolver == null)
        {
            return;
        }

        SpriteRenderer renderer = playerResolver.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null && savedSprite != null)
        {
            renderer.sprite = savedSprite;
            renderer.color = savedColor;
            renderer.flipX = savedFlipX;
            renderer.flipY = savedFlipY;
        }

        Animator animator = playerResolver.GetComponentInChildren<Animator>();
        if (animator != null && savedAnimatorController != null)
        {
            animator.runtimeAnimatorController = savedAnimatorController;
        }

        HWJ_CharacterMotionSystem motionSystem = playerResolver.GetComponent<HWJ_CharacterMotionSystem>();
        motionSystem?.RefreshFacingBaseline();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
