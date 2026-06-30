using UnityEngine;
using System;


[Serializable]
public class StatOrbJsonData
{
    public string id; // 구슬 고유 ID
    public string name; // 구슬 이름
    public StatData statBouns; // 획득 시 플레이어에게적용할 능력치 보너스
}
