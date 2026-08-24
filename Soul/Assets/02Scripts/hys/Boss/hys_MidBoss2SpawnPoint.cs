using UnityEngine;

// 플레이어 시작 지점처럼 씬에서 중간보스2가 생성될 위치와 방향을 표시합니다.
[RequireComponent(typeof(HWJ_SpawnPoint))]
[DisallowMultipleComponent]
public class hys_MidBoss2SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "hys_midboss2_spawn";
    [SerializeField, Min(0.2f)] private float gizmoRadius = 0.9f;

    public string SpawnId => spawnId;
    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
    public HWJ_SpawnPoint SharedSpawnPoint => GetComponent<HWJ_SpawnPoint>();

    public void Initialize(string id)
    {
        if (!string.IsNullOrWhiteSpace(id)) spawnId = id;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.95f, 0.2f, 1f, 0.95f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.right * gizmoRadius * 1.6f);

        // 보스용 마커임을 알아보기 쉽도록 왕관 형태의 선을 함께 표시합니다.
        Vector3 crownBase = transform.position + Vector3.up * gizmoRadius;
        Vector3 left = crownBase + Vector3.left * gizmoRadius * 0.65f;
        Vector3 right = crownBase + Vector3.right * gizmoRadius * 0.65f;
        Vector3 leftPeak = crownBase + new Vector3(-0.35f, 0.7f, 0f) * gizmoRadius;
        Vector3 centerPeak = crownBase + Vector3.up * gizmoRadius;
        Vector3 rightPeak = crownBase + new Vector3(0.35f, 0.7f, 0f) * gizmoRadius;
        Gizmos.DrawLine(left, leftPeak);
        Gizmos.DrawLine(leftPeak, centerPeak);
        Gizmos.DrawLine(centerPeak, rightPeak);
        Gizmos.DrawLine(rightPeak, right);
        Gizmos.DrawLine(right, left);
    }
}
