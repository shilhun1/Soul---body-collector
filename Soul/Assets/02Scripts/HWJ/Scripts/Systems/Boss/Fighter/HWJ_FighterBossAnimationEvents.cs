using System;
using UnityEngine;

/// <summary>
/// Routes Animation Events from the shared boss Animator to the one active pattern system.
/// The Anim_* methods are the current clip contract. Legacy method names remain as aliases
/// so older clips cannot leave a hitbox or movement routine active.
/// </summary>
public class HWJ_FighterBossAnimationEvents : MonoBehaviour
{
    [SerializeField] private HWJ_FighterBossComboSystem comboSystem;
    [SerializeField] private HWJ_BossBrainSystem bossBrain;
    [SerializeField] private HWJ_FighterBossDeathSystem deathSystem;

    private void Awake()
    {
        if (comboSystem == null)
        {
            comboSystem = GetComponent<HWJ_FighterBossComboSystem>();
        }

        if (bossBrain == null)
        {
            bossBrain = GetComponent<HWJ_BossBrainSystem>();
        }

        if (deathSystem == null)
        {
            deathSystem = GetComponent<HWJ_FighterBossDeathSystem>();
        }
    }

    public void Anim_AttackStart()
    {
        Dispatch(receiver => receiver.OnAnimationAttackStart());
    }

    public void Anim_TelegraphStart()
    {
        Dispatch(receiver => receiver.OnAnimationTelegraphStart());
    }

    public void Anim_EnableHitbox(int strikeNumber)
    {
        Dispatch(receiver => receiver.OnAnimationEnableHitbox(strikeNumber));
    }

    public void Anim_DisableHitbox()
    {
        Dispatch(receiver => receiver.OnAnimationDisableHitbox());
    }

    public void Anim_RecoveryStart()
    {
        Dispatch(receiver => receiver.OnAnimationRecoveryStart());
    }

    public void Anim_AttackEnd()
    {
        Dispatch(receiver => receiver.OnAnimationAttackEnd());
    }

    public void Anim_ApplyMovement()
    {
        Dispatch(receiver => receiver.OnAnimationApplyMovement());
    }

    public void Anim_StopMovement()
    {
        Dispatch(receiver => receiver.OnAnimationStopMovement());
    }

    public void Anim_SpawnProjectile()
    {
        Dispatch(receiver => receiver.OnAnimationSpawnProjectile());
    }

    public void Anim_SpawnGroundHazard()
    {
        Dispatch(receiver => receiver.OnAnimationSpawnGroundHazard());
    }

    public void Anim_TeleportOut()
    {
        Dispatch(receiver => receiver.OnAnimationTeleportOut());
    }

    public void Anim_TeleportIn()
    {
        Dispatch(receiver => receiver.OnAnimationTeleportIn());
    }

    public void Anim_CameraShakeHook()
    {
        Dispatch(receiver => receiver.OnAnimationCameraShakeHook());
    }

    public void Anim_SFXHook()
    {
        Dispatch(receiver => receiver.OnAnimationSfxHook());
    }

    public void Anim_PhaseStep(int step)
    {
        bossBrain?.HandleAnimationPhaseStep(step);
    }

    public void Anim_DeathComplete()
    {
        deathSystem?.DeathEnd();
    }

    // Legacy aliases used by clips created before the Anim_* contract.
    public void AttackStart() => Anim_AttackStart();
    public void TelegraphStart() => Anim_TelegraphStart();
    public void EnableHitbox(int strikeNumber) => Anim_EnableHitbox(strikeNumber);
    public void DisableHitbox() => Anim_DisableHitbox();
    public void AttackRecoveryStart() => Anim_RecoveryStart();
    public void AttackEnd() => Anim_AttackEnd();
    public void ApplyMovement() => Anim_ApplyMovement();
    public void StopMovement() => Anim_StopMovement();
    public void SpawnProjectile() => Anim_SpawnProjectile();
    public void SpawnGroundHazard() => Anim_SpawnGroundHazard();
    public void TeleportOut() => Anim_TeleportOut();
    public void TeleportIn() => Anim_TeleportIn();
    public void CameraShakeHook() => Anim_CameraShakeHook();
    public void SFXHook() => Anim_SFXHook();

    private void Dispatch(Action<HWJ_IFighterBossAnimationEventReceiver> dispatch)
    {
        MonoBehaviour[] localBehaviours = GetComponents<MonoBehaviour>();

        for (int i = 0; i < localBehaviours.Length; i++)
        {
            if (localBehaviours[i] is HWJ_IFighterBossAnimationEventReceiver receiver
                && receiver.IsPatternRunning)
            {
                dispatch(receiver);
                return;
            }
        }

        // Keep the serialized combo fallback for older prefab revisions.
        if (comboSystem != null && comboSystem.IsPatternRunning)
        {
            dispatch(comboSystem);
        }
    }
}
