using UnityEngine;

/// <summary>
/// 능력치 구슬 하나의 보상 값을 정의하는 ScriptableObject입니다.
/// StatOrbSystem과 Level/Reward 시스템이 이 데이터를 읽어 플레이어의 런타임 능력치에 보너스를 적용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_StatOrbData", menuName = "HWJ/Data/System/Stat Orb")]
public class HWJ_StatOrbDataSO : ScriptableObject
{
    [Header("스탯 구슬 기본 정보")]
    [Tooltip("보상 데이터에서 참조할 고정 ID입니다.")]
    [InspectorName("스탯 구슬 ID")]
    [SerializeField] private string orbId;
    [Tooltip("사람이 읽는 스탯 구슬 이름입니다.")]
    [InspectorName("표시 이름")]
    [SerializeField] private string displayName;
    [Tooltip("어떤 능력치를 올릴지 정합니다.")]
    [InspectorName("증가 능력치 종류")]
    [SerializeField] private HWJ_StatOrbType orbType;
    [Tooltip("증가시킬 수치입니다.")]
    [InspectorName("증가량")]
    [SerializeField] private float amount;
    [Tooltip("같은 구슬을 영구 성장으로 몇 번까지 중첩 적용할 수 있는지 정합니다.")]
    [InspectorName("최대 중첩 횟수")]
    [SerializeField] private int maxStackCount = 1;
    [Tooltip("켜면 스테이지/씬이 바뀌어도 유지되는 영구 성장으로 사용합니다.")]
    [InspectorName("영구 적용")]
    [SerializeField] private bool isPermanent = true;
    [Tooltip("영구 적용이 아닐 때 유지되는 시간입니다.")]
    [InspectorName("지속 시간")]
    [SerializeField] private float durationSeconds;
    [Header("프리팹")]
    [Tooltip("필드에 떨어질 스탯 구슬 프리팹입니다.")]
    [InspectorName("구슬 프리팹")]
    [SerializeField] private GameObject orbPrefab;
    [Tooltip("획득 시 재생할 이펙트 프리팹입니다.")]
    [InspectorName("획득 이펙트 프리팹")]
    [SerializeField] private GameObject collectEffectPrefab;

    public string OrbId => orbId;
    public string DisplayName => displayName;
    public HWJ_StatOrbType OrbType => orbType;
    public float Amount => amount;
    public int ConfiguredMaxStackCount => maxStackCount;
    public int MaxStackCount => Mathf.Max(1, maxStackCount);
    public bool IsPermanent => isPermanent;
    public float DurationSeconds => durationSeconds;
    public GameObject OrbPrefab => orbPrefab;
    public GameObject CollectEffectPrefab => collectEffectPrefab;
}
