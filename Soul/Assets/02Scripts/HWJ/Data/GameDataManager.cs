using UnityEngine; // MonoBehaviour와 Resources를 사용하기 위해
using System; // Func를 사용하기 위해
using System.Collections.Generic; // Dictionary와 List를 사용하기 위해

public class GameDataManager : MonoBehaviour // JSON 데이터를 로드하고 검색하는 싱글톤 매니저
{
    public static GameDataManager Instance { get; private set;  } // 어디서든 접근 가능한 싱글톤

    private readonly Dictionary<string, MonsterJsonData> monsterMap = new Dictionary<string, MonsterJsonData>(); // 몬스터 Id 검색용 Dictionary
    private readonly Dictionary<string, PossessionBodyJsonData> possessionBodyMap = new Dictionary<string, PossessionBodyJsonData>(); // 빙의체 ID 검색용 Dictionary
    private readonly Dictionary<string, SkillJsonData> skillMap = new Dictionary<string, SkillJsonData>(); // 스킬 ID 검색용 Dictionary
    private readonly Dictionary<string, SkillTreeNodeJsonData> skillTreeNodeMap = new Dictionary<string, SkillTreeNodeJsonData>(); // 스킬 트리 노드 ID 검색용 Dictionary
    private readonly Dictionary<int, LevelUpJsonData> levelUpMap = new Dictionary<int, LevelUpJsonData>(); // 레벨 검색용 Dictionary
    private readonly Dictionary<string, JumpJsonData> jumpMap = new Dictionary<string, JumpJsonData>(); // 점프 ID 검색용 Dictionary


    private void Awake() // 오브젝트 생성 호출
    {
        if (Instance != null && Instance != this) // 이미 다른 GameDataManager가 있는지 확인
        {
            Destroy(gameObject); // 중복 매니저 제거
            return; // 함수 종료
        }

        Instance = this; // 현재 객체를 싱글톤 인스턴스로 지정
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않게 함
        LoadData(); // Json 데이터 로드
    }

    public void LoadData()  // Resources/Data/game_data.json을 읽음
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/game_data"); // Resources 폴더에서 JSON 파일을 불러옴

        if (jsonAsset == null) // JSON 파일이 없는지 확인
        {
            Debug.LogError("Resources/Data/game_data.json 파일을 찾을 수 없습니다."); // 오류 로그를 출력
            return; // 함수를 종료
        }

        GameData gameData = JsonUtility.FromJson<GameData>(jsonAsset.text); // JSON 문자열을 GameData 객체로 변환

        if (gameData == null) // 파싱 결과가 없는지 확인
        {
            Debug.LogError("game_data.json 파싱에 실패했습니다."); // 오류 로그를 출력
            return; // 함수를 종료
        }

        BuildMap(gameData.monsters, monsterMap, delegate (MonsterJsonData data) { return data.id; }); // 몬스터 Dictionary를 만듬
        BuildMap(gameData.possessionBodies, possessionBodyMap, delegate (PossessionBodyJsonData data) { return data.id; }); // 빙의체 Dictionary를 만듬
        BuildMap(gameData.skills, skillMap, delegate (SkillJsonData data) { return data.id; }); // 스킬 Dictionary를 만듬
        BuildMap(gameData.skillTreeNodes, skillTreeNodeMap, delegate (SkillTreeNodeJsonData data) { return data.id; }); // 스킬 트리 Dictionary를 만듬
        BuildMap(gameData.levelUps, levelUpMap, delegate (LevelUpJsonData data) { return data.level; }); // 레벨업 Dictionary를 만듭니다.
        BuildMap(gameData.jumps, jumpMap, delegate (JumpJsonData data) { return data.id; }); // 점프 Dictionary를 만듬
    }
    

    private void BuildMap<T, TKey>(List<T> list, Dictionary<TKey, T> map, Func<T, TKey> getKey) // 리스트를 Dictionary로 변환
    {
        map.Clear(); // 기존 Dictionary 내용 비우기

        if(list == null) // 리스트가 없는지 확인
        {
            return; //리스트가 없으면 종료
        }

        foreach (T item in list) // 리스트의 모든 데이터 순회
        {
            if(item == null) // 데이터 비어있는지 확인
            {
                Debug.LogError(typeof(T).Name + "안에null 데이터가 있습니다."); // 오류 로그 출력
                continue; // 다음 데이터로 넘김
            }

            TKey key = getKey(item); // 데이터에서 ID 또는 key 값을 가져옴

            if ( key == null || key.Equals(default(TKey))) // key가 비어 있는지 확인
            {
                Debug.LogError(typeof(T).Name + " 안에 비어 있는 ID가 있습니다."); // 오류 로그 출력
                continue; // 다음 데이터로 넘김
            }

            if (map.ContainsKey(key)) // 중복 key가 있는지 확인
            {
                Debug.LogError("중복 ID가 있습니다: " + key); // 중복 ID 오류 출력
                continue; // 다음 데이터로 넘김
            }

            map.Add(key, item); // Dictionary에 데이터 추가
        }
    }

    public MonsterJsonData GetMonster(string id) // 몬스터 데이터 가져옴
    {
        return GetData(monsterMap, id); // 몬스터 dictionary에서 검색
    }

    public PossessionBodyJsonData GetPossessionBody(string id) // 빙의체 데이터 가져옴
    {
        return GetData(possessionBodyMap, id); // 빙의체 Dictionary에서 검색
    }

    public SkillJsonData GetSkill(string id) // 스킬 데이터 가져옴
    {
        return GetData(skillMap, id); // 스킬 Dictionary에서 검색
    }

    public SkillTreeNodeJsonData GetSkillTreeNode(string id) // 스킬 트리 노드 데이터 가져옴
    {
        return GetData(skillTreeNodeMap, id); // 스킬 트리 Dictionary에서 검색
    }

    public LevelUpJsonData GetLevelUp(int level) // 레벨업 데이터 가져옴
    {
        return GetData(levelUpMap, level); // 레벨업 Dictionary에서 검색
    }

    public JumpJsonData GetJump(string id) // 점프 데이터 가져옴
    {
        return GetData(jumpMap, id); // 점프 Dictionary에서 검색
    }

    private TValue GetData<TKey, TValue>(Dictionary<TKey, TValue> map, TKey key) // Dictionary에서 데이터 안전하게 가져옴
    {
        if(key == null || key.Equals(default(TKey))) // key가 비어 있는지 확인
        {
            Debug.LogError("비어 있는 ID로 데이터를 요청했습니다."); // 오류 로그 출력
            return default(TValue); // 기본값을 반환
        }

        TValue value; // 검색 결과를 저장할 변수

        if (map.TryGetValue(key, out value)) // Dictionary에서 key를 검색
        {
            return value; // 찾은 데이터 반환
        }

        Debug.LogError("데이터를 찾을 수 없습니다: " + key); // 찾지 못했다는 로그 출력
        return default(TValue); // 기본값을 반환

    }
}
