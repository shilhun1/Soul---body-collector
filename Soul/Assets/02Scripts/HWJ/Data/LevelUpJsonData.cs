using UnityEngine;
using System;

[Serializable]
public class LevelUpJsonData
{
    public int level; // 적용될 레벨
    public int requiredExp; // 레벨업에 필요한 경험치
    public StatData statBouns; // 해당 레벨에서 추가로 얻는 능력치
}
