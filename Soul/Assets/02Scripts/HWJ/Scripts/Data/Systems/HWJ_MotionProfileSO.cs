using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_MotionProfile", menuName = "HWJ/Data/System/Motion Profile")]
public class HWJ_MotionProfileSO : ScriptableObject
{
    [SerializeField] private HWJ_WeaponType weaponType;
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private string defaultAttackMotionKey = "Attack";
    [SerializeField] private HWJ_MotionClipData[] motions;

    public HWJ_WeaponType WeaponType => weaponType;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public string DefaultAttackMotionKey => defaultAttackMotionKey;

    public bool TryGetMotion(string motionKey, out HWJ_MotionClipData motion)
    {
        motion = null;

        if (string.IsNullOrEmpty(motionKey) || motions == null)
        {
            return false;
        }

        for (int i = 0; i < motions.Length; i++)
        {
            HWJ_MotionClipData candidate = motions[i];

            if (candidate != null && candidate.MotionKey == motionKey)
            {
                motion = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryGetDefaultAttackMotion(out HWJ_MotionClipData motion)
    {
        return TryGetMotion(defaultAttackMotionKey, out motion);
    }
}

[Serializable]
public class HWJ_MotionClipData
{
    [SerializeField] private string motionKey;
    [SerializeField] private string animatorTriggerName;
    [SerializeField] private string animatorStateName;
    [SerializeField] private int layerIndex;
    [SerializeField] private float transitionSeconds = 0.05f;
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private bool useCrossFade;
    [SerializeField] private bool resetTriggerBeforeSet = true;

    public string MotionKey => motionKey;
    public string AnimatorTriggerName => animatorTriggerName;
    public string AnimatorStateName => animatorStateName;
    public int LayerIndex => layerIndex;
    public float TransitionSeconds => transitionSeconds;
    public bool UseTrigger => useTrigger;
    public bool UseCrossFade => useCrossFade;
    public bool ResetTriggerBeforeSet => resetTriggerBeforeSet;
}
