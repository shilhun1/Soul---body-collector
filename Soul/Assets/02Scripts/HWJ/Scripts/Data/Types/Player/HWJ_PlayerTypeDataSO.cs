using UnityEngine;

/// <summary>
/// 플레이어 유형 전용 데이터 에셋입니다.
/// RootObjectDataSO의 Selected Type에 넣으면 조작, 스킬, 성장, 소울, 빙의, 부패 데이터가 함께 적용됩니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_PlayerTypeData", menuName = "HWJ/Data/Type Data/Player")]
public class HWJ_PlayerTypeDataSO : HWJ_ObjectTypeDataSO
{
    [Header("조작/이동")]
    [Tooltip("가속, 감속, 점프, 대쉬, 낙하 같은 플레이어 이동감 설정입니다.")]
    [InspectorName("조작 데이터")]
    [SerializeField] private HWJ_ControlData control = new HWJ_ControlData();

    [Header("공격과 상태")]
    [Tooltip("기본 공격 사거리, 판정 타이밍, 이동 잠금, 캔슬 가능 시간을 설정합니다.")]
    [InspectorName("공격 데이터")]
    [SerializeField] private HWJ_PlayerAttackData attack = new HWJ_PlayerAttackData();
    [Tooltip("몸 상태/영혼 상태에서 이동과 공격을 허용할지 설정합니다.")]
    [InspectorName("상태 데이터")]
    [SerializeField] private HWJ_PlayerStateData state = new HWJ_PlayerStateData();

    [Header("카메라")]
    [Tooltip("플레이어 추적 카메라의 이동 속도와 오프셋을 설정합니다.")]
    [InspectorName("카메라 데이터")]
    [SerializeField] private HWJ_PlayerCameraData camera = new HWJ_PlayerCameraData();

    [Header("스킬과 성장")]
    [Tooltip("플레이어 기본 스킬 목록입니다. 빙의 전용 스킬은 빙의한 몸의 EnemyTypeData를 우선 봅니다.")]
    [InspectorName("스킬 목록")]
    [SerializeField] private HWJ_SkillSetData skillSet = new HWJ_SkillSetData();
    [Tooltip("레벨, 스킬 포인트, 경험치 테이블 같은 성장 설정입니다.")]
    [InspectorName("성장 데이터")]
    [SerializeField] private HWJ_GrowthData growth = new HWJ_GrowthData();

    [Header("영혼과 육신")]
    [Tooltip("영혼 상태 시작 여부, 제한 시간, 비행/벽 통과 같은 영혼 규칙입니다.")]
    [InspectorName("영혼 상태 데이터")]
    [SerializeField] private HWJ_SoulStateData soulState = new HWJ_SoulStateData();
    [Tooltip("플레이어가 빙의를 시도할 수 있는지와 빙의 범위를 설정합니다.")]
    [InspectorName("빙의 데이터")]
    [SerializeField] private HWJ_PossessionData possession = new HWJ_PossessionData();
    [Tooltip("빙의한 몸의 부패 시간, 행동별 부패 증가량, 붕괴 조건을 설정합니다.")]
    [InspectorName("육신 부패 데이터")]
    [SerializeField] private HWJ_BodyDecayData bodyDecay = new HWJ_BodyDecayData();

    public HWJ_ControlData Control => control;
    public HWJ_PlayerAttackData Attack => attack;
    public HWJ_PlayerStateData State => state;
    public HWJ_PlayerCameraData Camera => camera;
    public HWJ_SkillSetData SkillSet => skillSet;
    public HWJ_GrowthData Growth => growth;
    public HWJ_SoulStateData SoulState => soulState;
    public HWJ_PossessionData Possession => possession;
    public HWJ_BodyDecayData BodyDecay => bodyDecay;
}
