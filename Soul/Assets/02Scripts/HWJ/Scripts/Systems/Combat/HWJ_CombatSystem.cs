using UnityEngine;

public class HWJ_CombatSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }
    }

    public float GetOutgoingDamage()
    {
        if (dataResolver == null || dataResolver.Status == null || dataResolver.Damage == null)
        {
            return 0f;
        }

        return dataResolver.Status.attackPower + dataResolver.Damage.baseDamage;
    }
}
