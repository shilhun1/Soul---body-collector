using System;
using UnityEngine;

// 빙의할 육체마다 플레이어에게 적용할 충돌 크기와 발바닥 여백을 저장합니다.
[Serializable]
public struct hys_BodyCollisionSettings
{
    public Vector2 size;
    public Vector2 offset;
    public float groundClearance;

    public static hys_BodyCollisionSettings FromCollider(BoxCollider2D collider, float clearance)
    {
        return new hys_BodyCollisionSettings
        {
            size = collider != null ? collider.size : new Vector2(1f, 2f),
            offset = collider != null ? collider.offset : new Vector2(0f, 1f),
            groundClearance = Mathf.Max(0f, clearance)
        };
    }
}

[DisallowMultipleComponent]
public class hys_BodyCollisionProfile : MonoBehaviour
{
    [Header("육체 충돌 프로필")]
    [SerializeField] private Vector2 colliderSize = new Vector2(1f, 2f);
    [SerializeField] private Vector2 colliderOffset = new Vector2(0f, 1f);
    [SerializeField, Min(0f)] private float groundClearance = 0.05f;

    public hys_BodyCollisionSettings GetSettings()
    {
        return new hys_BodyCollisionSettings
        {
            size = new Vector2(Mathf.Max(0.05f, colliderSize.x), Mathf.Max(0.05f, colliderSize.y)),
            offset = colliderOffset,
            groundClearance = groundClearance
        };
    }
}
