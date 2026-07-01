using UnityEngine; // MonoBehaviour와 Time을 사용하기 위해 필요합니다.

public class PossessionController : MonoBehaviour // 플레이어의 빙의 상태를 관리합니다.
{
    private PossessionBase currentPossession; // 현재 적용 중인 빙의 행동 컴포넌트입니다.

    [SerializeField] private SkillController skillController; // 빙의 시 스킬 목록을 바꿀 스킬 컨트롤러입니다.
    [SerializeField] private PlayerJumpController jumpController; // 빙의 시 점프 방식을 바꿀 점프 컨트롤러입니다.
    [SerializeField] private PlayerStatController statController; // 빙의 시 능력치를 바꿀 능력치 컨트롤러입니다.

    private void Awake() // 오브젝트가 생성될 때 실행됩니다.
    {
        if (skillController == null) skillController = GetComponent<SkillController>(); // 같은 오브젝트에서 SkillController를 찾습니다.
        if (jumpController == null) jumpController = GetComponent<PlayerJumpController>(); // 같은 오브젝트에서 PlayerJumpController를 찾습니다.
        if (statController == null) statController = GetComponent<PlayerStatController>(); // 같은 오브젝트에서 PlayerStatController를 찾습니다.
    }

    private void Update() // 매 프레임 실행됩니다.
    {
        if (currentPossession == null) return; // 빙의 중이 아니면 종료합니다.

        currentPossession.TickDecay(Time.deltaTime); // 빙의 남은 시간을 감소시킵니다.

        if (currentPossession.IsExpired()) // 빙의 시간이 끝났으면
        {
            ReleasePossession(); // 빙의를 해제합니다.
        }
    }

    public void Possess(string possessionBodyId) // 특정 빙의체 ID로 빙의합니다.
    {
        PossessionBodyJsonData data = GameDataManager.Instance.GetPossessionBody(possessionBodyId); // 빙의체 데이터를 가져옵니다.

        if (data == null) return; // 데이터가 없으면 종료합니다.

        ReleasePossession(); // 기존 빙의가 있으면 먼저 해제합니다.

        currentPossession = PossessionFactory.Create(data.possessionBehaviourId, gameObject); // 빙의 행동 컴포넌트를 생성합니다.

        if (currentPossession == null) return; // 생성 실패 시 종료합니다.

        currentPossession.OnPossess(data); // 빙의 시작 처리를 실행합니다.

        if (skillController != null) skillController.SetSkills(data.skillIds); // 빙의체 스킬 목록을 적용합니다.
        if (jumpController != null) jumpController.SetJump(data.jumpDataId); // 빙의체 점프 방식을 적용합니다.
        if (statController != null) statController.SetPossessionBonus(CreateStatFromBody(data)); // 빙의체 능력치를 적용합니다.
    }

    public void ReleasePossession() // 현재 빙의를 해제합니다.
    {
        if (currentPossession != null) // 현재 빙의 중이면
        {
            currentPossession.OnRelease(); // 빙의 해제 처리를 실행합니다.
            Destroy(currentPossession); // 빙의 컴포넌트를 제거합니다.
            currentPossession = null; // 현재 빙의 참조를 비웁니다.
        }

        if (skillController != null) skillController.SetDefaultSkills(); // 기본 스킬로 되돌립니다.
        if (jumpController != null) jumpController.SetDefaultJump(); // 기본 점프로 되돌립니다.
        if (statController != null) statController.SetPossessionBonus(null); // 빙의 능력치 보너스를 제거합니다.
    }

    private StatData CreateStatFromBody(PossessionBodyJsonData data) // 빙의체 데이터로 능력치 보너스를 만듭니다.
    {
        return new StatData // 새 능력치 데이터를 반환합니다.
        {
            maxHp = data.maxHp, // 최대 체력 보너스입니다.
            physicalAttack = data.physicalAttack, // 물리 공격력 보너스입니다.
            magicAttack = data.magicAttack, // 마법 공격력 보너스입니다.
            defense = data.defense, // 방어력 보너스입니다.
            moveSpeed = data.moveSpeed, // 이동 속도 보너스입니다.
            attackSpeed = data.attackSpeed // 공격 속도 보너스입니다.
        };
    }
}
