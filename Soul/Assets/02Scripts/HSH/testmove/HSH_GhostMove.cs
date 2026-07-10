using UnityEngine;

public class HSH_GhostMove : MonoBehaviour
{
    [Header("Ghost State")]
    public bool isGhost = false;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private HWJ_RuntimeStatusSystem statusSystem;

    private HSH_GhostStat ghostStat;
    
    private float originalGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();
        statusSystem = GetComponent<HWJ_RuntimeStatusSystem>();

        ghostStat = GetComponent<HSH_GhostStat>();

        if (rb != null)
            originalGravity = rb.gravityScale;
    }

    void Update()
    {
        if (!isGhost) return;

        Move();
    }

    public void EnterGhostState()
    {
        if (isGhost) return;
        isGhost = true;
        
        if (ghostStat != null)
        {
            ghostStat.StartTimer();
        }

        // 벽 통과 및 중력 무시
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        foreach (var col in colliders)
        {
            col.isTrigger = true;
        }
        
        Debug.Log("영혼 상태 진입");
    }

    public void ExitGhostState()
    {
        if (!isGhost) return;
        isGhost = false;
        
        if (ghostStat != null)
        {
            ghostStat.StopTimer();
        }

        // 원래 상태 복구
        if (rb != null)
        {
            rb.gravityScale = originalGravity;
        }

        foreach (var col in colliders)
        {
            col.isTrigger = false;
        }
        
        Debug.Log("영혼 상태 해제");
    }

    private void Move()
    {
        // 상하좌우 방향키 입력만 받음 (New Input System)
        float h = 0f;
        float v = 0f;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) h = 1f;
            else if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) h = -1f;

            if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) v = 1f;
            else if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) v = -1f;
        }

        Vector2 moveDir = new Vector2(h, v).normalized;

        // 속도는 HSH_GhostStat의 데이터를 최우선으로 사용합니다.
        float speed = 5f;
        if (ghostStat != null && ghostStat.CurrentStats != null)
        {
            speed = ghostStat.CurrentStats.moveSpeed;
        }
        else if (statusSystem != null)
        {
            speed = statusSystem.MoveSpeed; // HSH_GhostStat이 없으면 기존 StatusSystem 스탯 사용
        }

        // 이동속도가 0이거나 너무 낮으면 임의로 5f 지정
        if (speed <= 0f) speed = 5f;

        if (rb != null)
        {
            rb.linearVelocity = moveDir * speed;
        }
        else
        {
            transform.Translate(moveDir * speed * Time.deltaTime);
        }
    }
}
