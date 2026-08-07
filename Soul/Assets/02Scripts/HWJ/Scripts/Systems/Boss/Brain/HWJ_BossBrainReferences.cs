using UnityEngine;

/// <summary>
/// Resolves boss components, target references, boss data, and debug gizmos.
/// This partial belongs to the single HWJ_BossBrainSystem component.
/// </summary>
public partial class HWJ_BossBrainSystem
{
    private bool TryGetBossData(out HWJ_BossTypeDataSO bossData)
    {
        bossData = null;
        return dataResolver != null && dataResolver.TryGetTypeData(out bossData);
    }

    private void CacheReferences()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (patternSystem == null)
        {
            patternSystem = GetComponent<HWJ_BossPatternSystem>();
        }

        if (stageOnePatternSystem == null)
        {
            stageOnePatternSystem = GetComponent<HWJ_Stage1BossPatternSystem>();
        }

        if (skillActionSystem == null)
        {
            skillActionSystem = GetComponent<HWJ_SkillActionSystem>();
        }

        if (motionSystem == null)
        {
            motionSystem = GetComponent<HWJ_CharacterMotionSystem>();
        }

        if (cameraFocusSystem == null)
        {
            cameraFocusSystem = GetComponent<HWJ_BossCameraFocusSystem>();
        }

        if (dialogueBubbleSystem == null)
        {
            dialogueBubbleSystem = GetComponent<HWJ_BossDialogueBubbleSystem>();
        }

        if (fighterComboSystem == null)
        {
            fighterComboSystem = GetComponent<HWJ_FighterBossComboSystem>();
        }

        if (fighterChargeSystem == null)
        {
            fighterChargeSystem = GetComponent<HWJ_FighterBossChargeSystem>();
        }

        if (fighterUppercutSystem == null)
        {
            fighterUppercutSystem = GetComponent<HWJ_FighterBossUppercutSystem>();
        }

        if (fighterGroundSlamSystem == null)
        {
            fighterGroundSlamSystem = GetComponent<HWJ_FighterBossGroundSlamSystem>();
        }

        if (fighterPhaseTwoPatternSystem == null)
        {
            fighterPhaseTwoPatternSystem = GetComponent<HWJ_FighterBossPhaseTwoPatternSystem>();
        }

        if (fighterDeathSystem == null)
        {
            fighterDeathSystem = GetComponent<HWJ_FighterBossDeathSystem>();
        }

        if (fighterAnimatorSystem == null)
        {
            fighterAnimatorSystem = GetComponent<HWJ_FighterBossAnimatorSystem>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
    }

    private Transform FindPlayerTarget()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
        {
            return HWJ_GameAccess.Manager.PlayerResolver.transform;
        }

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
            {
                return resolvers[i].transform;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        CacheReferences();

        if (!TryGetBossData(out HWJ_BossTypeDataSO bossData) || !bossData.FSM.useBossRoomBounds)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Vector3 center = transform.position + (Vector3)bossData.FSM.bossRoomOffset;
        Gizmos.DrawWireCube(center, bossData.FSM.bossRoomSize);
    }
}
