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

    [Header("영혼 정신력 비용")]
    [InspectorName("빙의 성공 정신력 비용")]
    [Tooltip("이 몸에 빙의가 성공했을 때 영혼 정신력에서 차감할 값입니다. 0이면 비용 없이 빙의합니다.")]
    public float spiritMentalCostOnPossession;

    [Header("살아있는 대상 빙의 저항")]
    [InspectorName("살아있는 대상 빙의 성공 확률")]
    [Tooltip("처치된 상태 필요를 끈 살아있는 대상에게 빙의할 때 성공할 확률입니다. 1은 항상 성공, 0은 항상 실패입니다.")]
    [Range(0f, 1f)]
    public float livePossessionSuccessChance = 1f;
    [InspectorName("실패 시 정신력 비용")]
    [Tooltip("살아있는 대상 빙의에 실패했을 때 영혼 정신력에서 추가로 차감할 값입니다.")]
    public float livePossessionFailureSpiritMentalCost = 10f;
    [InspectorName("실패 시 조작 잠금 시간")]
    [Tooltip("살아있는 대상 빙의에 실패했을 때 플레이어 조작을 잠글 시간입니다.")]
    public float livePossessionFailureControlLockSeconds = 0.5f;
    [InspectorName("실패 시 넉백 힘")]
    [Tooltip("살아있는 대상 빙의에 실패했을 때 플레이어를 대상 반대 방향으로 밀어내는 힘입니다.")]
    public float livePossessionFailureKnockbackPower = 3f;
    [InspectorName("실패 시 넉백 시간")]
    [Tooltip("살아있는 대상 빙의 실패 넉백이 유지되는 시간입니다.")]
    public float livePossessionFailureKnockbackSeconds = 0.2f;
    [InspectorName("실패 메시지")]
    [Tooltip("살아있는 대상 빙의 저항 판정에 실패했을 때 로그/UI에서 사용할 문장입니다.")]
    public string livePossessionFailureMessage = "대상의 정신력에 밀려 빙의에 실패했다.";

    [InspectorName("처치된 상태 필요")]
    [Tooltip("켜면 살아있는 대상에게는 빙의할 수 없습니다.")]
    public bool requiresDefeatedState = true;
    [InspectorName("조작권 이전")]
    [Tooltip("켜면 빙의 성공 시 조작권이 해당 몸으로 넘어갑니다.")]
    public bool transfersControlToBody = true;
    [InspectorName("몸 능력치 로드")]
    [Tooltip("켜면 빙의한 몸의 스탯과 무기 데이터를 플레이어 런타임에 적용합니다.")]
    public bool loadsBodyStatsToPlayer = true;
    [Header("빙의 육신 부패 오버라이드")]
    [InspectorName("빙의 부패값 덮어쓰기")]
    [Tooltip("켜면 플레이어 기본 부패값 대신 이 몸 전용 부패값을 사용합니다.")]
    public bool overrideBodyDecayOnPossession;
    [InspectorName("빙의 전용 부패값")]
    [Tooltip("이 몸에 빙의하고 있는 동안만 사용하는 부패값입니다. 중간보스처럼 더 오래 쓸 수 있는 몸에 사용합니다.")]
    public HWJ_BodyDecayData possessedBodyDecayOverride = new HWJ_BodyDecayData();
}
