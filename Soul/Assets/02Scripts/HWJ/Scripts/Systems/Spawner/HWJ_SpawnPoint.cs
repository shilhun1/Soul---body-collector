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
    [SerializeField] private float gizmoRadius = 0.35f;

    public string PointId => pointId;
    public HWJ_SpawnPointType SpawnPointType => spawnPointType;
    public Transform SpawnParent => spawnParent;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    private void OnDrawGizmos()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * gizmoRadius);
    }

    private Color GetGizmoColor()
    {
        switch (spawnPointType)
        {
            case HWJ_SpawnPointType.PlayerStart:
                return Color.green;
            case HWJ_SpawnPointType.Enemy:
                return Color.red;
            case HWJ_SpawnPointType.NPC:
                return Color.cyan;
            case HWJ_SpawnPointType.Boss:
                return Color.magenta;
            case HWJ_SpawnPointType.StatOrb:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
}
