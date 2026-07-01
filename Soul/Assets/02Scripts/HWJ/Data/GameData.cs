using System; // Serializable을 사용하기 위해 필요합니다.
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.

[Serializable] // Unity JsonUtility가 JSON을 이 클래스로 읽을 수 있게 합니다.
public class GameData // game_data.json 전체 구조와 연결되는 최상위 데이터입니다.
{
    public List<MonsterJsonData> monsters = new List<MonsterJsonData>(); // 몬스터 데이터 목록입니다.
    public List<PossessionBodyJsonData> possessionBodies = new List<PossessionBodyJsonData>(); // 빙의체 데이터 목록입니다.
    public List<SkillJsonData> skills = new List<SkillJsonData>(); // 스킬 데이터 목록입니다.
    public List<SkillTreeNodeJsonData> skillTreeNodes = new List<SkillTreeNodeJsonData>(); // 스킬 트리 노드 데이터 목록입니다.
    public List<LevelUpJsonData> levelUps = new List<LevelUpJsonData>(); // 레벨업 데이터 목록입니다.
    public List<JumpJsonData> jumps = new List<JumpJsonData>(); // 점프 데이터 목록입니다.
    public List<StatOrbJsonData> statOrbs = new List<StatOrbJsonData>(); // 능력치 구슬 데이터 목록입니다.
}
