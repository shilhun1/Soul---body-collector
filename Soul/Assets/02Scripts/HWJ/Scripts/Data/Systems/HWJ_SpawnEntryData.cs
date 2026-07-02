using System;
using UnityEngine;

/// <summary>
/// 하나의 스폰 항목을 정의합니다.
/// SpawnerSystem이 어떤 RootObjectData 또는 Prefab을 몇 개, 어떤 조건으로 생성할지 읽습니다.
/// </summary>
[Serializable]
public class HWJ_SpawnEntryData
{
    public string spawnId;
    public HWJ_SpawnPointType spawnPointType;
    public HWJ_RootObjectDataSO rootObjectData;
    public GameObject prefabOverride;
    public int spawnCount = 1;
    public float spawnDelaySeconds;
    public bool spawnOnStart = true;
}
