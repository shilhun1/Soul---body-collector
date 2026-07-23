using UnityEngine;

// 몬스터 중심점 대신 실제로 바닥에 닿아야 하는 발 위치를 명시합니다.
[DisallowMultipleComponent]
public class hys_PossessionFeetAnchor : MonoBehaviour
{
    [SerializeField] private Vector2 localFeetOffset;
    [SerializeField, Min(0.01f)] private float gizmoRadius = 0.15f;

    public Vector2 WorldPosition => transform.TransformPoint(localFeetOffset);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(WorldPosition, gizmoRadius);
    }
}
