using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_MotionProfile", menuName = "HWJ/Data/System/Motion Profile")]
public class HWJ_MotionProfileSO : ScriptableObject
{
    [Header("모션 프로필")]
    [Tooltip("이 모션 프로필이 적용될 무기 타입입니다.")]
    [InspectorName("무기 타입")]
    [SerializeField] private HWJ_WeaponType weaponType;
    [Tooltip("해당 무기에 사용할 Animator Controller입니다.")]
    [InspectorName("애니메이터 컨트롤러")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [Tooltip("기본 공격에 사용할 모션 키입니다.")]
    [InspectorName("기본 공격 모션 키")]
    [SerializeField] private string defaultAttackMotionKey = "Attack";
    [Tooltip("모션 키별 애니메이션 실행 설정 목록입니다.")]
    [InspectorName("모션 목록")]
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
    [Header("모션 클립")]
    [Tooltip("스킬 또는 공격 데이터에서 호출할 모션 키입니다.")]
    [InspectorName("모션 키")]
    [SerializeField] private string motionKey;
    [Tooltip("Animator Trigger 이름입니다.")]
    [InspectorName("애니메이터 트리거 이름")]
    [SerializeField] private string animatorTriggerName;
    [Tooltip("CrossFade로 직접 이동할 Animator State 이름입니다.")]
    [InspectorName("애니메이터 상태 이름")]
    [SerializeField] private string animatorStateName;
    [Tooltip("Animator 레이어 인덱스입니다.")]
    [InspectorName("레이어 인덱스")]
    [SerializeField] private int layerIndex;
    [Tooltip("CrossFade 전환 시간입니다.")]
    [InspectorName("전환 시간")]
    [SerializeField] private float transitionSeconds = 0.05f;
    [Tooltip("켜면 Trigger 방식으로 애니메이션을 실행합니다.")]
    [InspectorName("트리거 사용")]
    [SerializeField] private bool useTrigger = true;
    [Tooltip("켜면 Animator State로 CrossFade합니다.")]
    [InspectorName("크로스페이드 사용")]
    [SerializeField] private bool useCrossFade;
    [Tooltip("켜면 Trigger를 넣기 전에 같은 Trigger를 초기화합니다.")]
    [InspectorName("트리거 사전 초기화")]
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
