using UnityEngine; // MonoBehaviour 기능과 Object 검색을 사용하기 위해 필요합니다.

public class EnemyController : CharacterBase // 몬스터의 초기화, 체력, 사망 처리를 담당합니다.
{
    private MonsterJsonData monsterData; // 현재 몬스터의 JSON 데이터입니다.

    public void InitFromMonsterId(string monsterId) // 몬스터 ID로 데이터를 찾아 초기화합니다.
    {
        monsterData = GameDataManager.Instance.GetMonster(monsterId); // GameDataManager에서 몬스터 데이터를 가져옵니다.

        if (monsterData == null) return; // 데이터가 없으면 종료합니다.

        Init(new StatData // CharacterBase의 능력치를 초기화합니다.
        {
            maxHp = monsterData.maxHp, // 최대 체력입니다.
            physicalAttack = monsterData.physicalAttack, // 물리 공격력입니다.
            magicAttack = monsterData.magicAttack, // 마법 공격력입니다.
            defense = monsterData.defense, // 방어력입니다.
            moveSpeed = monsterData.moveSpeed, // 이동 속도입니다.
            attackSpeed = monsterData.attackSpeed // 공격 속도입니다.
        });
    }

    public override void Die() // 몬스터가 죽었을 때 실행됩니다.
    {
        if (monsterData == null) // 몬스터 데이터가 없으면
        {
            Destroy(gameObject); // 오브젝트만 제거합니다.
            return; // 종료합니다.
        }

        PlayerStatController playerStat = Object.FindFirstObjectByType<PlayerStatController>(); // 씬에서 플레이어 능력치 컨트롤러를 찾습니다.

        if (playerStat != null) playerStat.AddExp(monsterData.expReward); // 경험치 보상을 지급합니다.

        if (PossessionBodyManager.Instance != null) // 빙의체 매니저가 있으면
        {
            PossessionBodyManager.Instance.TrySpawnBody(monsterData, transform.position); // 조건에 맞게 빙의체 생성을 시도합니다.
        }

        if (StatOrbManager.Instance != null) // 능력치 구슬 매니저가 있으면
        {
            StatOrbManager.Instance.TrySpawnStatOrb(monsterData, transform.position); // 강화체라면 능력치 구슬 생성을 시도합니다.
        }

        Destroy(gameObject); // 몬스터 오브젝트를 제거합니다.
    }
}
