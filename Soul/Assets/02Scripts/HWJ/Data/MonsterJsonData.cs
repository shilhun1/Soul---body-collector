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

    public int soulReward; // 처치 시 지급할 영혼 보상
    public int expReward; // 처치 시 지급할 경험치

    public bool canSpawnPossessionBody; // 죽었을 때 빙의체를 생성할 수 있는지 여부
    public string possessionBodyID; //생성할 빙의체 ID

    public bool isEnhanced; // 강화체 몬스터인지 여부
    public bool canDropStatOrb; // 처치 시 능력치 구슬을 드롭할 수 있는지 여부
    public string statOrbId; // 드롭할 능력치 구슬 ID
}
