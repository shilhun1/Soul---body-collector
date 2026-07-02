using UnityEngine;

public class HWJ_PossessionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;

    private void Awake()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }
    }

    public bool CanPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return false;
        }

        if (!targetDataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            return false;
        }

        return enemyData.PossessionBody != null && enemyData.PossessionBody.canBePossessed;
    }

    public bool TryPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!CanPossess(targetDataResolver))
        {
            return false;
        }

        soulSystem?.EnterBodyState();
        return true;
    }
}
