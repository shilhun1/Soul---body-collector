using UnityEngine;

/// <summary>
/// 씬에 배치하는 시작 지점/스폰 위치 컴포넌트입니다.
/// SpawnerSystem이 이 위치와 타입을 보고 플레이어, 적, NPC, 보스, 구슬을 생성합니다.
/// </summary>
public class HWJ_SpawnPoint : MonoBehaviour
{
    [SerializeField] private string pointId;
    [SerializeField] private HWJ_SpawnPointType spawnPointType;
    [SerializeField] private Transform spawnParent;

    public string PointId => pointId;
    public HWJ_SpawnPointType SpawnPointType => spawnPointType;
    public Transform SpawnParent => spawnParent;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
}
