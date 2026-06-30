using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]

public class PossessionBodyJsonData
{
    public string id; // 빙의체 고유 ID
    public string name; // 빙의체 이름
    public int maxHp; // 빙의 시 추가 또는 적용할 최대 체력
    public int physicalAttack; // 빙의 시 적용할 물리 공격력 보너스
    public int magicAttack; // 빙의 시 적용할 마법 공격력 보너스
    public int defense; // 빙의 시 적용할 방어력 보너스
    public float moveSpeed; // 빙의 시 적용할 이동 속도 보너스
    public float attackSpeed; // 빙의 시 적용할 공격 속도 보너스
    public float duration; // 빙의 지속 시간
    public string possessionBehaviourId; // 실제 빙의 행동 클래스와 연결할 ID
    public string jumpDataId; // 빙의 중 사용할 점프 데이터 ID
    public List<string> skillIds = new List<string>(); // 빙의 중 사용할 스킬 ID 목록
}
