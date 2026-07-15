using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(hys_Player_State))]
public class hys_Player_Attack : MonoBehaviour
{
    [Header("Attack")]
    // 공격이 맞았을 때 상대에게 전달할 데미지입니다.
    [SerializeField] private float attackDamage = 1f;

    // 공격을 다시 사용할 수 있을 때까지 기다리는 시간입니다.
    [SerializeField] private float attackCooldown = 0.3f;

    // 플레이어 상태를 Attack으로 유지하는 전체 시간입니다.
    [SerializeField] private float attackStateTime = 0.18f;

    // 공격 입력 후 실제 히트박스가 켜지는 타이밍입니다.
    [SerializeField] private float hitDelay = 0.12f;

    // 공격 판정이 감지할 레이어입니다.
    [SerializeField] private LayerMask targetLayers = ~0;

    // 검 끝이나 손 위치를 따로 기준점으로 쓰고 싶을 때 넣습니다.
    [SerializeField] private Transform attackPoint;

    [Header("Hit Box")]
    // 플레이어 기준 공격 박스 중심 위치입니다. x는 바라보는 방향에 따라 좌우 반전됩니다.
    [SerializeField] private Vector2 hitBoxOffset = new Vector2(0.95f, 0f);

    // 실제 데미지가 들어가는 가로 막대 판정 크기입니다.
    [SerializeField] private Vector2 hitBoxSize = new Vector2(1.6f, 0.45f);

    [Header("Debug")]
    // 공격 성공/타격 수를 Console에 출력할지 정합니다.
    [SerializeField] private bool useDebugLog = true;

    // Scene 뷰에서 공격 판정을 표시할지 정합니다.
    [SerializeField] private bool drawAttackRange = true;
    [SerializeField] private Color attackRangeColor = Color.red;

    // 공격이 실제로 발생한 순간 잠깐 채워지는 색입니다.
    [SerializeField] private Color attackRangeFillColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private float attackFillVisibleTime = 0.08f;

    // 같은 대상이 박스 안에 여러 콜라이더를 가져도 한 번만 맞게 하기 위한 임시 목록입니다.
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    private hys_Player_State playerState;
    private hys_Player_Movement playerMovement;

    // 공격 도중 플레이어가 움직여도 판정 위치가 흔들리지 않게 공격 시작 순간을 저장합니다.
    private Vector2 attackOriginSnapshot;
    private Vector2 attackDirectionSnapshot;
    private float nextAttackTime;
    private float attackFillEndTime;
    private Coroutine attackRoutine;

    private void Awake()
    {
        playerState = GetComponent<hys_Player_State>();
        playerMovement = GetComponent<hys_Player_Movement>();
    }

    private void Update()
    {
        // X 키를 공격 입력으로 사용합니다.
        if (IsAttackInputPressed())
        {
            TryAttack();
        }
    }

    // 공격 가능 여부를 확인하고 공격 루틴을 시작합니다.
    public bool TryAttack()
    {
        if (Time.time < nextAttackTime || playerState == null)
        {
            return false;
        }

        // 기본 상태에서 공격할 수 없더라도 대쉬 후딜 공격 허용 옵션은 별도로 확인합니다.
        if (!playerState.CanAttack &&
            (playerMovement == null || !playerMovement.TryConsumeDashRecoveryForAttack()))
        {
            return false;
        }

        StartFirstAttack();
        return true;
    }

    private void StartFirstAttack()
    {
        attackOriginSnapshot = GetCurrentAttackOrigin();
        attackDirectionSnapshot = GetCurrentAttackDirection();
        nextAttackTime = Time.time + attackCooldown;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    // 공격 상태 진입, 선딜레이, 판정 발생, 상태 복구를 순서대로 처리합니다.
    private IEnumerator AttackRoutine()
    {
        playerState.SetState(hys_PlayerState.Attack);

        yield return new WaitForSeconds(hitDelay);

        if (playerState.CurrentState != hys_PlayerState.Attack)
        {
            attackRoutine = null;
            yield break;
        }

        attackFillEndTime = Time.time + attackFillVisibleTime;
        HitTargets();

        float remainStateTime = Mathf.Max(0f, attackStateTime - hitDelay);
        yield return new WaitForSeconds(remainStateTime);

        if (playerState.CurrentState == hys_PlayerState.Attack)
        {
            playerState.SetState(GetReturnState());
        }

        attackRoutine = null;
    }

    // 저장된 공격 위치 기준으로 박스 판정을 검사하고 TakeDamage 메시지를 보냅니다.
    private void HitTargets()
    {
        hitTargets.Clear();
        Collider2D[] hits = Physics2D.OverlapBoxAll(GetHitBoxCenter(), hitBoxSize, 0f, targetLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && !hit.transform.IsChildOf(transform))
            {
                hitTargets.Add(hit);
            }
        }

        foreach (Collider2D hit in hitTargets)
        {
            hit.SendMessageUpwards("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }

        if (useDebugLog)
        {
            Debug.Log($"[Player Attack] X Attack / hits: {hitTargets.Count}", this);
        }
    }

    // 공격이 끝났을 때 돌아갈 상태를 정합니다.
    private hys_PlayerState GetReturnState()
    {
        if (playerMovement != null && playerMovement.Is_Dashing)
        {
            return hys_PlayerState.Dash;
        }

        return hys_PlayerState.Idle;
    }

    // 공격 입력 체크입니다.
    private bool IsAttackInputPressed()
    {
        return Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
    }

    // attackPoint가 있으면 그 위치를, 없으면 플레이어 위치를 공격 기준점으로 사용합니다.
    private Vector2 GetCurrentAttackOrigin()
    {
        if (attackPoint != null)
        {
            return attackPoint.position;
        }

        return transform.position;
    }

    // 마지막 이동 방향 또는 오브젝트 스케일을 기준으로 공격 방향을 정합니다.
    private Vector2 GetCurrentAttackDirection()
    {
        float direction = playerMovement != null ? playerMovement.LastMoveDirection : Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(direction, 0f))
        {
            direction = 1f;
        }

        return Vector2.right * direction;
    }

    // 실제 공격이 발생한 순간의 박스 중심입니다.
    private Vector2 GetHitBoxCenter()
    {
        float directionSign = Mathf.Sign(attackDirectionSnapshot.x);
        return attackOriginSnapshot + new Vector2(hitBoxOffset.x * directionSign, hitBoxOffset.y);
    }

    // Scene 뷰에서 미리 보여줄 현재 기준 박스 중심입니다.
    private Vector2 GetPreviewHitBoxCenter()
    {
        Vector2 direction = GetCurrentAttackDirection();
        float directionSign = Mathf.Sign(direction.x);
        return GetCurrentAttackOrigin() + new Vector2(hitBoxOffset.x * directionSign, hitBoxOffset.y);
    }

    // 선택된 상태에서 공격 판정을 시각적으로 확인하기 위한 기즈모입니다.
    private void OnDrawGizmosSelected()
    {
        if (!drawAttackRange)
        {
            return;
        }

        if (Application.isPlaying && Time.time < attackFillEndTime)
        {
            Gizmos.color = attackRangeFillColor;
            Gizmos.DrawCube(GetHitBoxCenter(), hitBoxSize);
            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireCube(GetHitBoxCenter(), hitBoxSize);
        }
    }
}
