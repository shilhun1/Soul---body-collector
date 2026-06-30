using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class MonsterJsonData
{
    public string id; // 몬스터 고유 ID
    public string name; // 몬스터 이름

    public int maxHp; // 최대 체력
    public int physicalAttack; // 물리 공격력
    public int magicAttack; // 마법 공격력
    public int defense; // 방어력
    public float moveSpeed; // 이동 속도
    public float attackSpeed; // 공격 속도

    public int soulReward; // 처치 시 지급할 영혼 수
    public int expReward; // 처치 시 지급할 경험치

    public float corpseDuration; // 시체 빙의 유지 시간
    public string possessionBehaviourId; // 빙의 행동 클래스 ID
    public string jumpDataId; // 빙의 시 적용할 점프 데이터 ID
    public List<string> skillIds; // 빙의 시 사용할 스킬 ID 목록
}
