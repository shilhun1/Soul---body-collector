using UnityEngine; // MonoBehaviour, Rigidbody2D, Physics2D, Input, LayerMask를 사용하기 위해 필요합니다.

public class PlayerJumpController : MonoBehaviour // 플레이어의 점프 입력과 점프 행동 교체를 관리합니다.
{
    public string defaultJumpDataId = "player_default_jump"; // 기본으로 사용할 점프 데이터 ID입니다.
    public Transform groundCheckPoint; // 바닥을 검사할 위치입니다.
    public float groundCheckRadius = 0.1f; // 바닥 검사 원의 반지름입니다.
    public LayerMask groundLayer; // 바닥으로 인정할 레이어입니다.

    private Rigidbody2D rb; // 플레이어의 Rigidbody2D입니다.
    private JumpBehaviourBase jumpBehaviour; // 현재 사용 중인 점프 행동 컴포넌트입니다.
    private bool jumpHeld; // 점프 버튼을 누르고 있는지 저장합니다.

    private void Awake() // 오브젝트가 생성될 때 호출됩니다.
    {
        rb = GetComponent<Rigidbody2D>(); // 같은 오브젝트에서 Rigidbody2D를 가져옵니다.
    }

    private void Start() // 첫 프레임 전에 호출됩니다.
    {
        SetDefaultJump(); // 기본 점프 데이터를 적용합니다.
    }

    private void Update() // 매 프레임 입력을 감지합니다.
    {
        if (jumpBehaviour == null) // 점프 행동이 없으면 입력을 처리할 수 없습니다.
        {
            return; // 안전하게 종료합니다.
        }

        jumpHeld = Input.GetButton("Jump"); // 점프 버튼을 누르고 있는지 저장합니다.
        jumpBehaviour.SetJumpHeld(jumpHeld); // 점프 행동에 버튼 유지 상태를 전달합니다.

        if (Input.GetButtonDown("Jump")) // 점프 버튼을 누른 바로 그 프레임인지 확인합니다.
        {
            jumpBehaviour.PressJump(); // 점프 입력 시간을 저장해 버퍼 점프가 가능하게 합니다.
        }
    }

    private void FixedUpdate() // 물리 프레임마다 점프 판정과 물리 적용을 처리합니다.
    {
        if (jumpBehaviour == null) // 점프 행동이 없으면 물리를 처리할 수 없습니다.
        {
            return; // 안전하게 종료합니다.
        }

        bool grounded = IsGrounded(); // 현재 바닥에 닿아 있는지 확인합니다.
        jumpBehaviour.SetGrounded(grounded); // 점프 행동에 바닥 상태를 전달합니다.
        jumpBehaviour.SetJumpHeld(jumpHeld); // 물리 처리 직전에도 최신 버튼 유지 상태를 전달합니다.
        jumpBehaviour.Tick(); // 점프 가능 여부, 실제 점프, 낙하 중력을 처리합니다.
    }

    public bool IsGrounded() // 플레이어가 바닥에 닿았는지 검사합니다.
    {
        if (groundCheckPoint == null) // 바닥 체크 위치가 없으면 검사할 수 없습니다.
        {
            return false; // 바닥이 아니라고 반환합니다.
        }

        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer); // 원형 충돌 검사로 바닥 접촉을 확인합니다.
    }

    public void SetDefaultJump() // 기본 점프 방식으로 되돌립니다.
    {
        SetJump(defaultJumpDataId); // 기본 점프 데이터 ID를 적용합니다.
    }

    public void SetJump(string jumpDataId) // 특정 점프 데이터 ID로 점프 방식을 바꿉니다.
    {
        JumpJsonData data = GameDataManager.Instance.GetJump(jumpDataId); // GameDataManager에서 점프 데이터를 가져옵니다.

        if (data == null) // 점프 데이터가 없으면 바꿀 수 없습니다.
        {
            return; // 안전하게 종료합니다.
        }

        if (jumpBehaviour != null) // 기존 점프 행동이 이미 붙어 있는지 확인합니다.
        {
            Destroy(jumpBehaviour); // 기존 점프 행동 컴포넌트를 제거합니다.
        }

        jumpBehaviour = JumpBehaviourFactory.Create(data, gameObject); // 데이터의 behaviourId에 맞는 점프 행동을 생성합니다.

        if (jumpBehaviour != null) // 새 점프 행동이 정상적으로 생성되었는지 확인합니다.
        {
            jumpBehaviour.Init(rb, data); // 새 점프 행동에 Rigidbody2D와 점프 데이터를 전달합니다.
        }
    }
}
