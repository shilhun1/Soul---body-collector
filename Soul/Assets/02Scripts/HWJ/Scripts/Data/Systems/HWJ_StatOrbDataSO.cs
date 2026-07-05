using UnityEngine;

/// <summary>
/// 능력치 구슬 하나의 보상 값을 정의하는 ScriptableObject입니다.
/// StatOrbSystem과 Level/Reward 시스템이 이 데이터를 읽어 플레이어의 런타임 능력치에 보너스를 적용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_StatOrbData", menuName = "HWJ/Data/System/Stat Orb")]
public class HWJ_StatOrbDataSO : ScriptableObject
{
    [SerializeField] private string orbId;
    [SerializeField] private string displayName;
    [SerializeField] private HWJ_StatOrbType orbType;
    [SerializeField] private float amount;
    [SerializeField] private bool isPermanent = true;
    [SerializeField] private float durationSeconds;
    [SerializeField] private GameObject orbPrefab;
    [SerializeField] private GameObject collectEffectPrefab;

    public string OrbId => orbId;
    public string DisplayName => displayName;
    public HWJ_StatOrbType OrbType => orbType;
    public float Amount => amount;
    public bool IsPermanent => isPermanent;
    public float DurationSeconds => durationSeconds;
    public GameObject OrbPrefab => orbPrefab;
    public GameObject CollectEffectPrefab => collectEffectPrefab;
}
