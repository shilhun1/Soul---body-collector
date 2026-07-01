using System; // Func를 사용하기 위해 필요합니다.
using System.Collections.Generic; // Dictionary와 List를 사용하기 위해 필요합니다.
using UnityEngine; // MonoBehaviour, Resources, Debug를 사용하기 위해 필요합니다.

public class GameDataManager : MonoBehaviour // JSON 데이터를 로드하고 ID로 검색해주는 매니저입니다.
{
    private static GameDataManager instance; // 싱글톤 인스턴스를 저장합니다.

    public static GameDataManager Instance // 어디서든 GameDataManager에 접근하기 위한 프로퍼티입니다.
    {
        get
        {
            if (instance == null) // 인스턴스가 아직 없으면
            {
                instance = UnityEngine.Object.FindFirstObjectByType<GameDataManager>(); // 씬에서 먼저 찾습니다.

                if (instance == null) // 씬에도 없으면
                {
                    GameObject obj = new GameObject("GameDataManager"); // 새 오브젝트를 만듭니다.
                    instance = obj.AddComponent<GameDataManager>(); // GameDataManager 컴포넌트를 붙입니다.
                }
            }

            return instance; // 준비된 인스턴스를 반환합니다.
        }
    }

    private readonly Dictionary<string, MonsterJsonData> monsterMap = new Dictionary<string, MonsterJsonData>(); // 몬스터 ID 검색용입니다.
    private readonly Dictionary<string, PossessionBodyJsonData> possessionBodyMap = new Dictionary<string, PossessionBodyJsonData>(); // 빙의체 ID 검색용입니다.
    private readonly Dictionary<string, SkillJsonData> skillMap = new Dictionary<string, SkillJsonData>(); // 스킬 ID 검색용입니다.
    private readonly Dictionary<string, SkillTreeNodeJsonData> skillTreeNodeMap = new Dictionary<string, SkillTreeNodeJsonData>(); // 스킬 트리 노드 ID 검색용입니다.
    private readonly Dictionary<int, LevelUpJsonData> levelUpMap = new Dictionary<int, LevelUpJsonData>(); // 레벨 검색용입니다.
    private readonly Dictionary<string, JumpJsonData> jumpMap = new Dictionary<string, JumpJsonData>(); // 점프 ID 검색용입니다.
    private readonly Dictionary<string, StatOrbJsonData> statOrbMap = new Dictionary<string, StatOrbJsonData>(); // 능력치 구슬 ID 검색용입니다.

    private void Awake() // 오브젝트가 생성될 때 호출됩니다.
    {
        if (instance != null && instance != this) // 이미 다른 인스턴스가 있으면
        {
            Destroy(gameObject); // 중복 오브젝트를 제거합니다.
            return; // 더 진행하지 않습니다.
        }

        instance = this; // 현재 오브젝트를 싱글톤으로 등록합니다.
        DontDestroyOnLoad(gameObject); // 씬 이동 시 파괴되지 않게 합니다.
        LoadData(); // 게임 데이터를 로드합니다.
    }

    public void LoadData() // Resources/Data/game_data.json을 읽고 Dictionary로 정리합니다.
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/game_data"); // HWJ/Resources/Data/game_data.json을 TextAsset으로 로드합니다.

        if (jsonAsset == null) // 파일을 찾지 못하면
        {
            Debug.LogError("Resources/Data/game_data.json 파일을 찾을 수 없습니다."); // 오류를 출력합니다.
            return; // 로드를 중단합니다.
        }

        GameData gameData = JsonUtility.FromJson<GameData>(jsonAsset.text); // JSON 문자열을 GameData로 변환합니다.

        if (gameData == null) // 변환에 실패하면
        {
            Debug.LogError("game_data.json 파싱에 실패했습니다."); // 오류를 출력합니다.
            return; // 로드를 중단합니다.
        }

        BuildMap(gameData.monsters, monsterMap, delegate (MonsterJsonData data) { return data.id; }); // 몬스터 맵을 만듭니다.
        BuildMap(gameData.possessionBodies, possessionBodyMap, delegate (PossessionBodyJsonData data) { return data.id; }); // 빙의체 맵을 만듭니다.
        BuildMap(gameData.skills, skillMap, delegate (SkillJsonData data) { return data.id; }); // 스킬 맵을 만듭니다.
        BuildMap(gameData.skillTreeNodes, skillTreeNodeMap, delegate (SkillTreeNodeJsonData data) { return data.id; }); // 스킬 트리 노드 맵을 만듭니다.
        BuildMap(gameData.levelUps, levelUpMap, delegate (LevelUpJsonData data) { return data.level; }); // 레벨업 맵을 만듭니다.
        BuildMap(gameData.jumps, jumpMap, delegate (JumpJsonData data) { return data.id; }); // 점프 맵을 만듭니다.
        BuildMap(gameData.statOrbs, statOrbMap, delegate (StatOrbJsonData data) { return data.id; }); // 능력치 구슬 맵을 만듭니다.
    }

    private void BuildMap<T, TKey>(List<T> list, Dictionary<TKey, T> map, Func<T, TKey> getKey) // List 데이터를 Dictionary로 변환합니다.
    {
        map.Clear(); // 기존 데이터를 비웁니다.

        if (list == null) // 리스트가 없으면
        {
            return; // 변환할 데이터가 없으므로 종료합니다.
        }

        foreach (T item in list) // 모든 데이터를 하나씩 확인합니다.
        {
            if (item == null) // 비어 있는 데이터가 있으면
            {
                Debug.LogError(typeof(T).Name + " 안에 null 데이터가 있습니다."); // 오류를 출력합니다.
                continue; // 다음 데이터로 넘어갑니다.
            }

            TKey key = getKey(item); // 데이터에서 ID 또는 레벨 값을 가져옵니다.

            if (IsEmptyKey(key)) // key가 비어 있으면
            {
                Debug.LogError(typeof(T).Name + " 안에 비어 있는 ID가 있습니다."); // 오류를 출력합니다.
                continue; // 다음 데이터로 넘어갑니다.
            }

            if (map.ContainsKey(key)) // 중복 key가 있으면
            {
                Debug.LogError("중복 ID가 있습니다: " + key); // 오류를 출력합니다.
                continue; // 다음 데이터로 넘어갑니다.
            }

            map.Add(key, item); // Dictionary에 추가합니다.
        }
    }

    private bool IsEmptyKey<TKey>(TKey key) // Dictionary key가 비어 있는지 확인합니다.
    {
        if (key == null) return true; // null이면 비어 있습니다.
        object keyObject = key; // 제네릭 key를 안전하게 object로 변환합니다.
        string stringKey = keyObject as string; // 문자열 key인지 확인합니다.
        if (stringKey != null) return string.IsNullOrEmpty(stringKey); // string이면 빈 문자열도 비어 있다고 봅니다.
        return key.Equals(default(TKey)); // int 같은 값 타입은 기본값인지 확인합니다.
    }

    public MonsterJsonData GetMonster(string id) { return GetData(monsterMap, id); } // 몬스터 데이터를 가져옵니다.
    public PossessionBodyJsonData GetPossessionBody(string id) { return GetData(possessionBodyMap, id); } // 빙의체 데이터를 가져옵니다.
    public SkillJsonData GetSkill(string id) { return GetData(skillMap, id); } // 스킬 데이터를 가져옵니다.
    public SkillTreeNodeJsonData GetSkillTreeNode(string id) { return GetData(skillTreeNodeMap, id); } // 스킬 트리 노드 데이터를 가져옵니다.
    public LevelUpJsonData GetLevelUp(int level) { return GetData(levelUpMap, level); } // 레벨업 데이터를 가져옵니다.
    public JumpJsonData GetJump(string id) { return GetData(jumpMap, id); } // 점프 데이터를 가져옵니다.
    public StatOrbJsonData GetStatOrb(string id) { return GetData(statOrbMap, id); } // 능력치 구슬 데이터를 가져옵니다.

    private TValue GetData<TKey, TValue>(Dictionary<TKey, TValue> map, TKey key) // Dictionary에서 데이터를 안전하게 가져옵니다.
    {
        if (IsEmptyKey(key)) // key가 비어 있으면
        {
            Debug.LogError("비어 있는 ID로 데이터를 요청했습니다."); // 오류를 출력합니다.
            return default(TValue); // 기본값을 반환합니다.
        }

        TValue value; // 찾은 값을 담을 변수입니다.

        if (map.TryGetValue(key, out value)) // Dictionary에서 값을 찾으면
        {
            return value; // 찾은 값을 반환합니다.
        }

        Debug.LogError("데이터를 찾을 수 없습니다: " + key); // 못 찾았다는 오류를 출력합니다.
        return default(TValue); // 기본값을 반환합니다.
    }
}
