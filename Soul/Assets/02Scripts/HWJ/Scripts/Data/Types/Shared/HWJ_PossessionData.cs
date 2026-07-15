using System;
using UnityEngine;

/// <summary>
/// 빙의하는 쪽과 빙의당하는 몸이 공통으로 사용하는 데이터입니다.
/// PlayerTypeDataSO에서는 canPossess를, Enemy/Boss TypeData에서는 canBePossessed와 심장 이펙트 정보를 주로 사용합니다.
/// </summary>
[Serializable]
public class HWJ_PossessionData
{
    [Header("빙의 가능 여부")]
    [InspectorName("빙의 시도 가능")]
    [Tooltip("플레이어처럼 다른 몸에 들어갈 수 있는 쪽이면 켭니다.")]
    public bool canPossess;
    [InspectorName("빙의 대상 가능")]
    [Tooltip("적 시체처럼 플레이어가 들어갈 수 있는 몸이면 켭니다.")]
    public bool canBePossessed;
    [InspectorName("빙의 가능 심장 이펙트")]
    [Tooltip("빙의 가능한 대상의 심장 위치에 표시할 이펙트 프리팹입니다.")]
    public GameObject possessableHeartEffectPrefab;
    [InspectorName("심장 소켓 이름")]
    [Tooltip("심장 이펙트를 붙일 소켓 이름입니다.")]
    public string heartSocketName;
    [InspectorName("빙의 거리")]
    [Tooltip("이 거리 안에서만 빙의를 시도할 수 있습니다.")]
    public float possessionRange;
    [InspectorName("처치된 상태 필요")]
    [Tooltip("켜면 살아있는 대상에게는 빙의할 수 없습니다.")]
    public bool requiresDefeatedState = true;
    [InspectorName("조작권 이전")]
    [Tooltip("켜면 빙의 성공 시 조작권이 해당 몸으로 넘어갑니다.")]
    public bool transfersControlToBody = true;
    [InspectorName("몸 능력치 로드")]
    [Tooltip("켜면 빙의한 몸의 스탯과 무기 데이터를 플레이어 런타임에 적용합니다.")]
    public bool loadsBodyStatsToPlayer = true;
}
