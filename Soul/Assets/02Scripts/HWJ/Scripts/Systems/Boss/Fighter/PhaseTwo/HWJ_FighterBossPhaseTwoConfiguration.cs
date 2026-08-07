using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds pattern profiles, animator states, materials, counters, and component references.
/// This partial belongs to the single HWJ_FighterBossPhaseTwoPatternSystem component.
/// </summary>
public sealed partial class HWJ_FighterBossPhaseTwoPatternSystem
{
    private HWJ_FighterBossPhaseTwoPatternProfile FindProfile(string patternId)
    {
        string normalized = NormalizePatternId(patternId);

        if (profiles == null)
        {
            return null;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            HWJ_FighterBossPhaseTwoPatternProfile profile = profiles[i];

            if (profile != null
                && string.Equals(
                    NormalizePatternId(profile.PatternId),
                    normalized,
                    StringComparison.Ordinal))
            {
                return profile;
            }
        }

        return null;
    }

    private bool PlayLegacyAnimatorState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        string[] candidates =
        {
            $"Base Layer.Phase2Attacks.{stateName}",
            $"Base Layer.{stateName}"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            int hash = Animator.StringToHash(candidates[i]);

            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
                return true;
            }
        }

        return false;
    }

    private Material ResolveTelegraphMaterial()
    {
        if (profiles == null)
        {
            return null;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i]?.TelegraphRenderer != null
                && profiles[i].TelegraphRenderer.sharedMaterial != null)
            {
                return profiles[i].TelegraphRenderer.sharedMaterial;
            }
        }

        return null;
    }

    private int CountRuntimeObjects(bool hazards)
    {
        int count = 0;

        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            if (runtimeObjectPool[i].InUse && runtimeObjectPool[i].IsHazard == hazards)
            {
                count++;
            }
        }

        return count;
    }

    private RuntimeAttackObject FindFirstActiveHazard()
    {
        for (int i = 0; i < runtimeObjectPool.Count; i++)
        {
            if (runtimeObjectPool[i].InUse && runtimeObjectPool[i].IsHazard)
            {
                return runtimeObjectPool[i];
            }
        }

        return null;
    }

    private static int GetAttackId(HWJ_FighterBossPhaseTwoPatternKind kind)
    {
        return 11 + (int)kind;
    }

    private static string GetCastStateName(HWJ_FighterBossPhaseTwoPatternKind kind)
    {
        return kind switch
        {
            HWJ_FighterBossPhaseTwoPatternKind.EnhancedCombo => "P2_Cast_EnhancedCombo",
            HWJ_FighterBossPhaseTwoPatternKind.DoubleCharge => "P2_Cast_DoubleCharge",
            HWJ_FighterBossPhaseTwoPatternKind.ThunderUppercut => "P2_Cast_ThunderUppercut",
            HWJ_FighterBossPhaseTwoPatternKind.DarkGroundSlam => "P2_Cast_DarkGroundSlam",
            HWJ_FighterBossPhaseTwoPatternKind.ShadowCombo => "P2_Cast_ShadowCombo",
            HWJ_FighterBossPhaseTwoPatternKind.LightningCast => "P2_Cast_LightningCast",
            HWJ_FighterBossPhaseTwoPatternKind.DarkWave => "P2_Cast_DarkWave",
            HWJ_FighterBossPhaseTwoPatternKind.SoulBind => "P2_Cast_SoulBind",
            HWJ_FighterBossPhaseTwoPatternKind.Ultimate => "P2_Cast_Ultimate",
            _ => string.Empty
        };
    }

    private static int GetCount(Dictionary<string, int> source, string key)
    {
        return !string.IsNullOrEmpty(key) && source.TryGetValue(key, out int count)
            ? count
            : 0;
    }

    private static void Record(Dictionary<string, int> destination, string patternId)
    {
        string key = NormalizePatternId(patternId);

        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        destination[key] = GetCount(destination, key) + 1;
    }

    private static string NormalizePatternId(string patternId)
    {
        switch (patternId)
        {
            case "P2_Enhanced_Combo":
                return "P2_EnhancedCombo";
            case "P2_Enhanced_Charge":
                return "P2_DoubleCharge";
            case "P2_Enhanced_Uppercut":
                return "P2_ThunderUppercut";
            case "P2_Enhanced_GroundSlam":
                return "P2_DarkGroundSlam";
            case "P2_Attack_Shockwave":
                return "P2_ShadowCombo";
            case "P2_Attack_AerialDive":
                return "P2_LightningCast";
            case "P2_Attack_CrossSlash":
                return "P2_DarkWave";
            case "P2_Attack_PhantomRush":
                return "P2_SoulBind";
            case "P2_Attack_Execution":
                return "P2_Ultimate";
            default:
                return patternId;
        }
    }

    private void CacheReferences()
    {
        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (combatSystem == null)
        {
            combatSystem = GetComponent<HWJ_CombatSystem>();
        }

        if (animatorSystem == null)
        {
            animatorSystem = GetComponent<HWJ_FighterBossAnimatorSystem>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (facingRenderer == null)
        {
            facingRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }
    }
}
