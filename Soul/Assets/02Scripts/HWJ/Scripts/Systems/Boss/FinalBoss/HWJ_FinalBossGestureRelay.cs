using UnityEngine;

/// <summary>
/// 최종보스 손짓 Animation Clip의 이벤트를 패턴 시스템으로 전달합니다.
/// 실제 공격 프레임에 HWJ_FinalBoss_CastMoment 이벤트를 배치합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HWJ_FinalBossGestureRelay : MonoBehaviour
{
    [SerializeField] private HWJ_FinalBossPatternSystem patternSystem;

    private void Awake()
    {
        ResolveReference();
    }

    public void HWJ_FinalBoss_CastMoment()
    {
        ResolveReference();
        patternSystem?.NotifyAnimationCastMoment();
    }

    private void ResolveReference()
    {
        if (patternSystem == null)
        {
            patternSystem = GetComponentInParent<HWJ_FinalBossPatternSystem>();
        }
    }
}
