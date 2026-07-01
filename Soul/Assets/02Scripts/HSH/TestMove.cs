using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TestMove : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("바닥 체크 설정")]
    [SerializeField] private LayerMask groundLayer; // 일반 바닥 레이어
    [SerializeField] private LayerMask platformLayer; // 통과하는 발판 레이어
    [SerializeField] private float groundCheckDistance = 1.1f; // 캐릭터 중심점(Center)에서 바닥까지의 거리

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasStickDownLastFrame;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            // 캐릭터가 물리 충돌로 인해 좌우로 회전하거나 넘어지지 않도록 Z축 회전 고정
            rb.freezeRotation = true;
        }
        else
        {
            Debug.LogError("Rigidbody2D 컴포넌트가 존재하지 않습니다! GameObject에 Rigidbody2D 컴포넌트를 추가해주세요.");
        }
    }

    void Update()
    {
        // 일반 바닥(groundLayer)과 통과하는 발판(platformLayer)을 모두 감지하도록 레이어 병합
        int combinedLayerMask = groundLayer | platformLayer;

        // 2D 바닥에 닿아있는지 확인 (Physics2D.Raycast 사용)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, combinedLayerMask);
        isGrounded = hit.collider != null;

        // 씬 뷰(Scene View)에서 바닥 체크 레이가 잘 작동하는지 시각적으로 표시
        Debug.DrawRay(transform.position, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        // 게임패드 스틱 입력 감지
        bool isStickDown = false;
        if (Gamepad.current != null)
        {
            var stick = Gamepad.current.leftStick.ReadValue();
            isStickDown = stick.y < -0.5f;
        }

        if (isGrounded)
        {
            // 아래 방향키/S키 입력 감지 (통과하는 발판 아래로 내려가기)
            bool downPressed = false;
            if (Keyboard.current != null && (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame))
            {
                downPressed = true;
            }
            else if (Gamepad.current != null && (Gamepad.current.dpad.down.wasPressedThisFrame || (isStickDown && !wasStickDownLastFrame)))
            {
                downPressed = true;
            }

            // 💡 LayerMask(platformLayer)와 오브젝트의 Layer 번호를 비교하려면 비트 연산을 사용해야 합니다.
            bool isPlatformLayer = hit.collider != null && ((1 << hit.collider.gameObject.layer) & platformLayer) != 0;

            if (downPressed && isPlatformLayer)
            {
                Debug.Log($"[TestMove] 아래 방향키 입력 감지됨! 현재 바닥(hit.collider): {(hit.collider != null ? hit.collider.gameObject.name : "null")}");

                if (hit.collider != null)
                {
                    PlatformerExtension platform = hit.collider.GetComponent<PlatformerExtension>();
                    
                    // 만약 자식 콜라이더를 맞췄을 경우를 대비해 부모에서도 찾아봅니다.
                    if (platform == null)
                    {
                        platform = hit.collider.GetComponentInParent<PlatformerExtension>();
                    }

                    if (platform != null)
                    {
                        Debug.Log($"[TestMove] 발판 스크립트를 찾았습니다! OnDownWay() 호출.");
                        platform.OnDownWay();
                    }
                    else
                    {
                        Debug.LogWarning($"[TestMove] {hit.collider.gameObject.name} 오브젝트에 PlatformerExtension 컴포넌트가 없습니다!");
                    }
                }
            }

            // 신규 인풋 시스템(Input System) Direct Polling 방식으로 점프 입력 처리
            bool jumpPressed = false;
            
            // 키보드 스페이스바 입력 감지
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
            // 게임패드 남쪽 버튼(A/X 버튼 등) 입력 감지
            else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                jumpPressed = true;
            }

            if (jumpPressed)
            {
                Jump();
            }
        }

        wasStickDownLastFrame = isStickDown;
    }

    void FixedUpdate()
    {
        // 물리적인 이동 처리는 FixedUpdate에서 수행
        Move();
    }

    private void Move()
    {
        if (rb == null) return;

        float horizontal = 0f;

        // 키보드 입력 처리 (A/D, 좌우 방향키)
        if (Keyboard.current != null)
        {
            var keyboard = Keyboard.current;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        }

        // 게임패드 입력 처리 (선택사항)
        if (Gamepad.current != null)
        {
            var stick = Gamepad.current.leftStick.ReadValue();
            horizontal += stick.x;
        }

        // 입력 값 범위 제한 (-1 ~ 1)
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);

        // 2D 플랫폼어의 X축 좌우 이동 속도 설정 (Y축은 기존 물리/중력 속도 유지)
        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (rb == null) return;

        // 연속 점프 시 일관된 점프력을 위해 현재 Y축 속도를 초기화하고 힘을 가함
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}
    