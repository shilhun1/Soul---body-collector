using System.Collections.Generic;
using UnityEngine;

// 보스 연출 중 HYS와 HWJ 플레이어 조작을 함께 잠그고 종료 시 원래 상태로 복구합니다.
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class hys_PlayerCinematicControlLock : MonoBehaviour
{
    private readonly HashSet<int> lockOwners = new HashSet<int>();
    private readonly List<Behaviour> suspendedBehaviours = new List<Behaviour>();
    private readonly List<bool> suspendedEnabledStates = new List<bool>();
    private readonly List<Animator> frozenVisualAnimators = new List<Animator>();
    private readonly List<float> frozenVisualAnimatorSpeeds = new List<float>();

    private Rigidbody2D body;
    private HWJ_RuntimeStatusSystem runtimeStatus;
    private hys_Player_Animator playerAnimator;
    private bool bodyStateStored;
    private bool storedBodySimulated;

    public bool IsLocked => lockOwners.Count > 0;
    public int LockOwnerCount => lockOwners.Count;
    public bool IsPhysicsFrozen => bodyStateStored && body != null && !body.simulated;
    public bool AreVisualAnimatorsFrozen
    {
        get
        {
            if (!IsLocked || frozenVisualAnimators.Count == 0) return false;
            for (int i = 0; i < frozenVisualAnimators.Count; i++)
            {
                if (frozenVisualAnimators[i] != null
                    && !Mathf.Approximately(frozenVisualAnimators[i].speed, 0f)) return false;
            }
            return true;
        }
    }

    public static bool IsLockedFor(Transform target)
    {
        if (target == null) return false;
        hys_PlayerCinematicControlLock controlLock =
            target.GetComponentInParent<hys_PlayerCinematicControlLock>();
        return controlLock != null && controlLock.IsLocked;
    }

    public void Acquire(Object owner)
    {
        if (owner == null || !lockOwners.Add(owner.GetInstanceID())) return;
        CacheReferences();
        if (lockOwners.Count == 1)
        {
            SuspendPossessionInput();
            FreezePlayerPhysics();
            FreezeVisualAnimators();
            playerAnimator?.EnterCinematicIdleLock();
        }
        StopHorizontalMovement();
    }

    public void Release(Object owner)
    {
        if (owner == null || !lockOwners.Remove(owner.GetInstanceID())) return;
        if (lockOwners.Count == 0)
        {
            playerAnimator?.ExitCinematicIdleLock();
            RestoreVisualAnimators();
            RestorePlayerPhysics();
            RestorePossessionInput();
        }
    }

    public void ReleaseAll()
    {
        lockOwners.Clear();
        playerAnimator?.ExitCinematicIdleLock();
        RestoreVisualAnimators();
        RestorePlayerPhysics();
        RestorePossessionInput();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        if (!IsLocked) return;

        // HWJ 이동·공격·대시 잠금을 짧게 갱신해 연출 종료 직후 자동으로 풀리게 합니다.
        runtimeStatus?.LockControl(0.25f);
        StopHorizontalMovement();
    }

    private void FixedUpdate()
    {
        if (IsLocked) StopHorizontalMovement();
    }

    private void LateUpdate()
    {
        if (!IsLocked) return;

        // 다른 모션 브리지가 LateUpdate에서 다시 재생 속도를 바꾸더라도 연출 프레임을 고정합니다.
        for (int i = 0; i < frozenVisualAnimators.Count; i++)
        {
            if (frozenVisualAnimators[i] != null) frozenVisualAnimators[i].speed = 0f;
        }
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }

    private void CacheReferences()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (runtimeStatus == null) runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        if (playerAnimator == null) playerAnimator = GetComponentInChildren<hys_Player_Animator>();
    }

    private void StopHorizontalMovement()
    {
        if (body == null) return;
        body.linearVelocity = body.simulated
            ? new Vector2(0f, body.linearVelocity.y)
            : Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void FreezePlayerPhysics()
    {
        if (body == null || bodyStateStored) return;
        storedBodySimulated = body.simulated;
        bodyStateStored = true;

        // 착지 직후 바닥 타일에 아주 조금 겹친 상태라면 최소 거리만큼 먼저 밀어내
        // 연출 중 충돌 보정이 좌우로 반복되는 현상을 막습니다.
        ResolveStaticColliderOverlap();
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;

        // 충돌 보정과 중력을 함께 멈춰 연출 중 벽이나 바닥에서 플레이어가 떨리지 않게 합니다.
        body.simulated = false;
    }

    private void ResolveStaticColliderOverlap()
    {
        if (body == null) return;
        Collider2D[] ownColliders = body.GetComponentsInChildren<Collider2D>();
        if (ownColliders.Length == 0) return;

        for (int iteration = 0; iteration < 4; iteration++)
        {
            Physics2D.SyncTransforms();
            bool corrected = false;
            for (int i = 0; i < ownColliders.Length && !corrected; i++)
            {
                Collider2D own = ownColliders[i];
                if (own == null || !own.enabled || own.isTrigger) continue;

                Bounds bounds = own.bounds;
                Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                    bounds.center,
                    bounds.size + Vector3.one * 0.04f,
                    0f);
                for (int j = 0; j < overlaps.Length; j++)
                {
                    Collider2D other = overlaps[j];
                    if (other == null || other == own || other.isTrigger || !other.enabled) continue;
                    if (other.transform.IsChildOf(body.transform)) continue;
                    if (other.attachedRigidbody != null
                        && other.attachedRigidbody.bodyType != RigidbodyType2D.Static) continue;

                    ColliderDistance2D distance = own.Distance(other);
                    if (!distance.isValid || !distance.isOverlapped) continue;
                    // normal은 바닥/벽(B)에서 플레이어(A)를 향하므로 음수 겹침 깊이의 반대값만큼 A를 이동합니다.
                    Vector2 separation = distance.normal * -distance.distance;
                    if (!float.IsFinite(separation.x) || !float.IsFinite(separation.y)
                        || separation.sqrMagnitude < 0.0000001f) continue;

                    // 잘못 배치된 깊은 벽에서도 한 번에 멀리 순간이동하지 않도록 보정량을 제한합니다.
                    body.position += Vector2.ClampMagnitude(separation, 0.25f);
                    corrected = true;
                    break;
                }
            }

            if (!corrected) break;
        }
        Physics2D.SyncTransforms();
    }

    private void RestorePlayerPhysics()
    {
        if (!bodyStateStored) return;
        if (body != null)
        {
            body.simulated = storedBodySimulated;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        bodyStateStored = false;
    }

    private void FreezeVisualAnimators()
    {
        frozenVisualAnimators.Clear();
        frozenVisualAnimatorSpeeds.Clear();

        // 실제 중간보스2 플레이어는 hys_Player_Animator 없이 자식 Animator만 사용하므로 직접 멈춥니다.
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator target = animators[i];
            if (target == null) continue;
            frozenVisualAnimators.Add(target);
            frozenVisualAnimatorSpeeds.Add(target.speed);
            target.speed = 0f;
        }
    }

    private void RestoreVisualAnimators()
    {
        for (int i = 0; i < frozenVisualAnimators.Count; i++)
        {
            if (frozenVisualAnimators[i] != null)
                frozenVisualAnimators[i].speed = frozenVisualAnimatorSpeeds[i];
        }

        frozenVisualAnimators.Clear();
        frozenVisualAnimatorSpeeds.Clear();
    }

    private void SuspendPossessionInput()
    {
        suspendedBehaviours.Clear();
        suspendedEnabledStates.Clear();

        // HWJ 입력 버퍼가 인트로 종료 뒤 점프·공격으로 이어지지 않도록 실행 컴포넌트도 잠시 멈춥니다.
        Suspend(GetComponent<HWJ_PlayerMovementSystem>());
        Suspend(GetComponent<HWJ_PlayerAttackSystem>());
        // HWJ 모션 시스템의 방향·스프라이트 갱신도 멈춰 좌우 반전 왕복을 차단합니다.
        Suspend(GetComponent<HWJ_CharacterMotionSystem>());
        Suspend(GetComponent<HWJ_PossessionSystem>());
        Suspend(GetComponent<HWJ_PossessionInteractionController>());
        Suspend(GetComponent<hys_LivingMonsterPossessionTest>());
        Suspend(GetComponent<hys_Test_SoulStateHotkey>());
    }

    private void Suspend(Behaviour behaviour)
    {
        if (behaviour == null || behaviour == this) return;
        suspendedBehaviours.Add(behaviour);
        suspendedEnabledStates.Add(behaviour.enabled);
        behaviour.enabled = false;
    }

    private void RestorePossessionInput()
    {
        for (int i = 0; i < suspendedBehaviours.Count; i++)
        {
            if (suspendedBehaviours[i] != null)
                suspendedBehaviours[i].enabled = suspendedEnabledStates[i];
        }

        suspendedBehaviours.Clear();
        suspendedEnabledStates.Clear();
    }
}
