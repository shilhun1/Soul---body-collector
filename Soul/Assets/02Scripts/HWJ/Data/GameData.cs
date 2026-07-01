using UnityEngine;
using System.Collections.Generic;
using System; // List 사용하기 위함

[Serializable]
public class GameData
{
    public List<MonsterJsonData> monsters = new List<MonsterJsonData>(); // 몬스터 데이터 목록
    public List<PossessionBodyJsonData> possessionBodies = new List<PossessionBodyJsonData>(); // 빙의체 데이터 목록
    public List<SkillJsonData> skills = new List<SkillJsonData>(); // 스킬 데이터 목록
    public List<SkillTreeNodeJsonData> skillTreeNodes = new List<SkillTreeNodeJsonData>(); // 스킬 트리 데이터 목록
    public List<LevelUpJsonData> levelUps = new List<LevelUpJsonData>(); // 레벨업 데이터 목록
    public List<JumpJsonData> jumps = new List<JumpJsonData>(); // 점프 데이터 목록
    public List<StatOrbJsonData> statOrbs = new List<StatOrbJsonData>(); // 능력치 구슬 데이터 목록
    //public List<BossJsonData> bosses = new List<BossJsonData>(); // 보스 데이터 목록입니다.
    //public List<BossPatternJsonData> bossPatterns = new List<BossPatternJsonData>(); // 보스 패턴 데이터 목록입니다.
}
