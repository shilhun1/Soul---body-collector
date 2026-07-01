using System.Collections.Generic; // Queue를 사용하기 위해 필요합니다.
using UnityEngine; // MonoBehaviour와 Instantiate를 사용하기 위해 필요합니다.

public class PossessionBodyManager : MonoBehaviour // 맵에 존재하는 빙의체 수와 생성을 관리합니다.
{
    public static PossessionBodyManager Instance { get; private set; } // 어디서든 접근할 수 있는 싱글톤 인스턴스입니다.

    public PossessionBodyObject possessionBodyPrefab; // 생성할 빙의체 프리팹입니다.
    public int maxActiveBodyCount = 5; // 맵에 유지할 최대 빙의체 개수입니다.

    private readonly Queue<PossessionBodyObject> activeBodies = new Queue<PossessionBodyObject>(); // 생성된 빙의체를 순서대로 저장합니다.

    private void Awake() // 오브젝트가 생성될 때 실행됩니다.
    {
        if (Instance != null && Instance != this) // 이미 다른 매니저가 있으면
        {
            Destroy(gameObject); // 중복 매니저를 제거합니다.
            return; // 종료합니다.
        }

        Instance = this; // 현재 매니저를 싱글톤으로 저장합니다.
    }

    public void TrySpawnBody(MonsterJsonData monsterData, Vector3 spawnPosition) // 몬스터 데이터에 따라 빙의체 생성을 시도합니다.
    {
        if (monsterData == null) return; // 몬스터 데이터가 없으면 종료합니다.
        if (!monsterData.canSpawnPossessionBody) return; // 빙의체 생성 몬스터가 아니면 종료합니다.
        if (string.IsNullOrEmpty(monsterData.possessionBodyId)) return; // 빙의체 ID가 없으면 종료합니다.

        PossessionBodyJsonData bodyData = GameDataManager.Instance.GetPossessionBody(monsterData.possessionBodyId); // 빙의체 데이터를 가져옵니다.

        if (bodyData == null || possessionBodyPrefab == null) return; // 데이터나 프리팹이 없으면 종료합니다.

        while (activeBodies.Count >= maxActiveBodyCount) // 최대 개수를 넘으면
        {
            PossessionBodyObject oldest = activeBodies.Dequeue(); // 가장 오래된 빙의체를 꺼냅니다.

            if (oldest != null) Destroy(oldest.gameObject); // 오브젝트가 남아 있으면 제거합니다.
        }

        PossessionBodyObject body = Instantiate(possessionBodyPrefab, spawnPosition, Quaternion.identity); // 새 빙의체를 생성합니다.
        body.Init(bodyData); // 생성된 빙의체에 데이터를 넣습니다.
        activeBodies.Enqueue(body); // 활성 목록에 추가합니다.
    }
}
