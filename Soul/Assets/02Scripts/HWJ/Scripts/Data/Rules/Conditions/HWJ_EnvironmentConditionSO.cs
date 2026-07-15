using UnityEngine;

[CreateAssetMenu(fileName = "HWJ_EnvironmentCondition", menuName = "HWJ/Data/Rules/Conditions/Environment")]
public class HWJ_EnvironmentConditionSO : HWJ_GameplayConditionSO
{
    [Header("환경 조건")]
    [Tooltip("Source 또는 Target 중 어느 쪽 위치를 기준으로 검사할지 정합니다.")]
    [InspectorName("검사 대상")]
    [SerializeField] private HWJ_GameplayActorSlot actor = HWJ_GameplayActorSlot.Source;
    [Tooltip("바닥, 벽, 시야 등 어떤 환경 조건을 검사할지 정합니다.")]
    [InspectorName("검사 조건")]
    [SerializeField] private HWJ_EnvironmentRequirement requirement = HWJ_EnvironmentRequirement.ActorGrounded;
    [Tooltip("검사할 레이어입니다.")]
    [InspectorName("검사 레이어")]
    [SerializeField] private LayerMask layerMask;
    [Tooltip("레이캐스트 또는 박스 검사 방향입니다.")]
    [InspectorName("검사 방향")]
    [SerializeField] private Vector2 direction = Vector2.down;
    [Tooltip("검사 거리입니다.")]
    [InspectorName("검사 거리")]
    [SerializeField] private float distance = 1f;
    [Tooltip("박스 검사에 사용할 크기입니다.")]
    [InspectorName("박스 크기")]
    [SerializeField] private Vector2 boxSize = Vector2.one;

    protected override bool Evaluate(HWJ_GameplayContext context)
    {
        Transform actorTransform = context.GetTransform(actor);

        if (actorTransform == null)
        {
            return false;
        }

        switch (requirement)
        {
            case HWJ_EnvironmentRequirement.ActorGrounded:
                return IsActorGrounded(context);
            case HWJ_EnvironmentRequirement.GroundBelow:
                return Raycast(actorTransform.position, Vector2.down, GetCheckDistance(context), GetLayerMask(context));
            case HWJ_EnvironmentRequirement.WallInDirection:
                return Raycast(actorTransform.position, GetCheckDirection(context), GetCheckDistance(context), GetLayerMask(context));
            case HWJ_EnvironmentRequirement.LineOfSightToTarget:
                return HasLineOfSight(context, actorTransform);
            case HWJ_EnvironmentRequirement.NoLineOfSightToTarget:
                return !HasLineOfSight(context, actorTransform);
            case HWJ_EnvironmentRequirement.PointOverlapsLayer:
                return PointOverlapsLayer(context, actorTransform);
            default:
                return false;
        }
    }

    private bool IsActorGrounded(HWJ_GameplayContext context)
    {
        HWJ_PlayerMovementSystem movement = context.GetMovement(actor);
        return movement != null && movement.IsGrounded;
    }

    private bool HasLineOfSight(HWJ_GameplayContext context, Transform actorTransform)
    {
        HWJ_GameplayActorSlot otherActor = actor == HWJ_GameplayActorSlot.Source
            ? HWJ_GameplayActorSlot.Target
            : HWJ_GameplayActorSlot.Source;
        Transform targetTransform = context.GetTransform(otherActor);

        if (targetTransform == null)
        {
            return false;
        }

        Vector2 start = actorTransform.position;
        Vector2 end = targetTransform.position;
        Vector2 rayDirection = end - start;
        float rayDistance = rayDirection.magnitude;

        if (rayDistance <= 0.001f)
        {
            return true;
        }

        return !Physics2D.Raycast(start, rayDirection.normalized, rayDistance, GetLayerMask(context));
    }

    private bool PointOverlapsLayer(HWJ_GameplayContext context, Transform actorTransform)
    {
        Vector2 center = context.HasWorldPoint
            ? context.WorldPoint
            : (Vector2)actorTransform.position;
        return Physics2D.OverlapBox(center, boxSize, 0f, GetLayerMask(context)) != null;
    }

    private bool Raycast(Vector2 origin, Vector2 rayDirection, float rayDistance, int rayLayerMask)
    {
        if (rayDirection.sqrMagnitude <= 0.0001f || rayDistance <= 0f)
        {
            return false;
        }

        return Physics2D.Raycast(origin, rayDirection.normalized, rayDistance, rayLayerMask);
    }

    private Vector2 GetCheckDirection(HWJ_GameplayContext context)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction.normalized;
        }

        return context.CheckDirection.sqrMagnitude > 0.0001f
            ? context.CheckDirection.normalized
            : Vector2.down;
    }

    private float GetCheckDistance(HWJ_GameplayContext context)
    {
        return distance > 0f ? distance : Mathf.Max(0f, context.CheckDistance);
    }

    private int GetLayerMask(HWJ_GameplayContext context)
    {
        if (layerMask.value != 0)
        {
            return layerMask.value;
        }

        if (context.EnvironmentLayerMask.value != 0)
        {
            return context.EnvironmentLayerMask.value;
        }

        return Physics2D.AllLayers;
    }
}
