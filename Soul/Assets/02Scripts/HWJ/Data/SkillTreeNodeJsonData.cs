using UnityEngine;
using System;
using System.Collections.Generic;


[Serializable]
public class SkillTreeNodeJsonData
{
    public string id; // 스킬 트리 노드 고유 Id
    public string name; // 노드 이름
    public int requiredLevel; // 노드를 해금하기 위해 필요한 최소 플레이어 레벨
    public List<string> requiredNodeIds = new List<string>(); // 선행 노드 ID 목록
    public StatData statBouns; // 해금 시 적용할 능력치 보너스
    public string unlockSkillId; // 해금 시 추가할 스킬 ID
}
