using UnityEngine;

[DisallowMultipleComponent]
public class HWJ_RuntimeObjectContext : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_CombatSystem combatSystem;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_BodyDecaySystem bodyDecaySystem;
    [SerializeField] private HWJ_PossessedBodySystem possessedBodySystem;
    [SerializeField] private HWJ_SkillActionSystem skillActionSystem;
    [SerializeField] private HWJ_LevelUpSystem levelProgressState;
    [SerializeField] private HWJ_SkillUnlockSystem skillUnlockState;
    [SerializeField] private HWJ_StatOrbProgressSystem statOrbProgressState;

    public HWJ_RootObjectDataResolver DataResolver
    {
        get
        {
            ResolveReferences();
            return dataResolver;
        }
    }

    public HWJ_RuntimeStatusSystem RuntimeStatus
    {
        get
        {
            ResolveReferences();
            return runtimeStatus;
        }
    }

    public HWJ_CombatSystem CombatSystem
    {
        get
        {
            ResolveReferences();
            return combatSystem;
        }
    }

    public HWJ_SoulSystem SoulSystem
    {
        get
        {
            ResolveReferences();
            return soulSystem;
        }
    }

    public HWJ_PossessionSystem PossessionSystem
    {
        get
        {
            ResolveReferences();
            return possessionSystem;
        }
    }

    public HWJ_BodyDecaySystem BodyDecaySystem
    {
        get
        {
            ResolveReferences();
            return bodyDecaySystem;
        }
    }

    public HWJ_PossessedBodySystem PossessedBodySystem
    {
        get
        {
            ResolveReferences();
            return possessedBodySystem;
        }
    }

    public HWJ_SkillActionSystem SkillActionSystem
    {
        get
        {
            ResolveReferences();
            return skillActionSystem;
        }
    }

    public HWJ_LevelUpSystem LevelProgressState
    {
        get
        {
            ResolveReferences();
            return levelProgressState;
        }
    }

    public HWJ_SkillUnlockSystem SkillUnlockState
    {
        get
        {
            ResolveReferences();
            return skillUnlockState;
        }
    }

    public HWJ_StatOrbProgressSystem StatOrbProgressState
    {
        get
        {
            ResolveReferences();
            return statOrbProgressState;
        }
    }

    public HWJ_RootObjectDataSO RootObjectData => DataResolver != null ? DataResolver.RootObjectData : null;
    public HWJ_ObjectType ObjectType => DataResolver != null ? DataResolver.ObjectType : HWJ_ObjectType.Player;
    public HWJ_Faction Faction => DataResolver != null && DataResolver.Identity != null ? DataResolver.Identity.faction : HWJ_Faction.Neutral;
    public HWJ_WeaponType WeaponType => DataResolver != null ? DataResolver.WeaponType : HWJ_WeaponType.None;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public bool TryGetTypeData<T>(out T typeData) where T : HWJ_ObjectTypeDataSO
    {
        typeData = null;
        return DataResolver != null && DataResolver.TryGetTypeData(out typeData);
    }

    public HWJ_StatusData GetEffectiveStatusData()
    {
        ResolveReferences();

        if (possessionSystem != null && possessionSystem.TryGetPossessedStatus(out HWJ_StatusData possessedStatus))
        {
            return possessedStatus;
        }

        return dataResolver != null ? dataResolver.Status : null;
    }

    public HWJ_DamageData GetEffectiveDamageData()
    {
        ResolveReferences();

        if (possessionSystem != null && possessionSystem.TryGetPossessedDamage(out HWJ_DamageData possessedDamage))
        {
            return possessedDamage;
        }

        return dataResolver != null ? dataResolver.Damage : null;
    }

    public HWJ_ReceivedDamageData GetEffectiveReceivedDamageData()
    {
        ResolveReferences();

        if (possessionSystem != null
            && possessionSystem.TryGetPossessedReceivedDamage(out HWJ_ReceivedDamageData possessedReceivedDamage))
        {
            return possessedReceivedDamage;
        }

        return dataResolver != null ? dataResolver.ReceivedDamage : null;
    }

    public HWJ_RuntimeObjectSnapshot CreateSnapshot()
    {
        ResolveReferences();

        return new HWJ_RuntimeObjectSnapshot
        {
            rootObjectData = dataResolver != null ? dataResolver.RootObjectData : null,
            objectType = dataResolver != null ? dataResolver.ObjectType : HWJ_ObjectType.Player,
            faction = dataResolver != null && dataResolver.Identity != null ? dataResolver.Identity.faction : HWJ_Faction.Neutral,
            weaponType = dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None,
            sourceStatusData = GetEffectiveStatusData(),
            sourceDamageData = GetEffectiveDamageData(),
            sourceReceivedDamageData = GetEffectiveReceivedDamageData(),
            runtimeStats = HWJ_RuntimeStatSnapshot.FromStatus(runtimeStatus),
            runtimeBody = HWJ_RuntimeBodySnapshot.FromSystems(soulSystem, possessionSystem, bodyDecaySystem, possessedBodySystem),
            runtimeGrowth = HWJ_RuntimeGrowthSnapshot.FromSystems(levelProgressState, skillUnlockState, statOrbProgressState)
        };
    }

    public void ResolveReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (bodyDecaySystem == null)
        {
            bodyDecaySystem = GetComponent<HWJ_BodyDecaySystem>();
        }

        if (possessedBodySystem == null)
        {
            possessedBodySystem = GetComponent<HWJ_PossessedBodySystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (levelProgressState == null)
        {
            levelProgressState = GetComponent<HWJ_LevelUpSystem>();
        }

        if (skillUnlockState == null)
        {
            skillUnlockState = GetComponent<HWJ_SkillUnlockSystem>();
        }

        if (statOrbProgressState == null)
        {
            statOrbProgressState = GetComponent<HWJ_StatOrbProgressSystem>();
        }
    }
}
