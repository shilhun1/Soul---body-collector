using UnityEngine;

/// <summary>
/// 프로젝트별 레이어 구성 차이로 지면 판정 마스크가 비는 문제를 방지합니다.
/// Ground 레이어가 없으면 현재 맵 지형이 사용하는 Default 레이어를 사용합니다.
/// </summary>
public static class HWJ_PhysicsLayerUtility
{
    public static int ResolveGroundMask(LayerMask configuredMask)
    {
        if (configuredMask.value != 0)
        {
            return configuredMask.value;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");

        if (groundLayer >= 0)
        {
            return 1 << groundLayer;
        }

        int defaultLayer = LayerMask.NameToLayer("Default");
        return defaultLayer >= 0 ? 1 << defaultLayer : Physics2D.DefaultRaycastLayers;
    }

    public static LayerMask CreateGroundMaskForCurrentProject()
    {
        return ResolveGroundMask(default);
    }
}
