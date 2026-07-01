using UnityEngine; // MonoBehaviour와 Instantiate를 사용하기 위해 필요합니다.

public class StatOrbManager : MonoBehaviour // 강화체 처치 후 능력치 구슬 생성을 관리합니다.
{
    public static StatOrbManager Instance { get; private set; } // 어디서든 접근할 수 있는 싱글톤 인스턴스입니다.

    public StatOrbObject statOrbPrefab; // 생성할 능력치 구슬 프리팹입니다.

    private void Awake() // 오브젝트가 생성될 때 실행됩니다.
    {
        if (Instance != null && Instance != this) // 이미 다른 매니저가 있으면
        {
            Destroy(gameObject); // 중복 매니저를 제거합니다.
            return; // 종료합니다.
        }

        Instance = this; // 현재 매니저를 싱글톤으로 저장합니다.
    }

    public void TrySpawnStatOrb(MonsterJsonData monsterData, Vector3 spawnPosition) // 몬스터 데이터에 따라 구슬 생성을 시도합니다.
    {
        if (monsterData == null) return; // 몬스터 데이터가 없으면 종료합니다.
        if (!monsterData.isEnhanced) return; // 강화체가 아니면 생성하지 않습니다.
        if (!monsterData.canDropStatOrb) return; // 구슬 드롭이 꺼져 있으면 생성하지 않습니다.
        if (string.IsNullOrEmpty(monsterData.statOrbId)) return; // 구슬 ID가 없으면 생성하지 않습니다.
        if (statOrbPrefab == null) return; // 프리팹이 없으면 생성하지 않습니다.

        StatOrbJsonData orbData = GameDataManager.Instance.GetStatOrb(monsterData.statOrbId); // 구슬 데이터를 가져옵니다.

        if (orbData == null) return; // 구슬 데이터가 없으면 종료합니다.

        StatOrbObject orb = Instantiate(statOrbPrefab, spawnPosition, Quaternion.identity); // 구슬 오브젝트를 생성합니다.
        orb.Init(orbData); // 생성된 구슬에 데이터를 넣습니다.
    }
}
