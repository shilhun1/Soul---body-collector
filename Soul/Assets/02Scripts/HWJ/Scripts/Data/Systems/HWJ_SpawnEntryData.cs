using System;
using UnityEngine;

/// <summary>
/// 하나의 스폰 항목을 정의합니다.
/// SpawnerSystem이 어떤 RootObjectData 또는 Prefab을 몇 개, 어떤 조건으로 생성할지 읽습니다.
/// </summary>
[Serializable]
public class HWJ_SpawnEntryData
{
    [Header("스폰 ID")]
    [InspectorName("스폰 ID")]
    [Tooltip("이 스폰 항목을 구분하는 고정 ID입니다.")]
    public string spawnId;
    [InspectorName("스폰 지점 ID")]
    [Tooltip("씬에 배치된 스폰 포인트의 ID입니다.")]
    public string spawnPointId;
    [InspectorName("스폰 지점 타입")]
    [Tooltip("플레이어 시작점, 몬스터 스폰 등 스폰 포인트 종류입니다.")]
    public HWJ_SpawnPointType spawnPointType;
    [Header("생성 대상")]
    [InspectorName("RootObjectData")]
    [Tooltip("생성할 대상의 RootObjectDataSO입니다.")]
    public HWJ_RootObjectDataSO rootObjectData;
    [InspectorName("프리팹 직접 지정")]
    [Tooltip("RootObjectData의 모델 대신 직접 생성할 프리팹입니다.")]
    public GameObject prefabOverride;
    [InspectorName("스폰 수")]
    [Tooltip("이 항목에서 생성할 개수입니다.")]
    public int spawnCount = 1;
    [InspectorName("첫 스폰 지연 시간")]
    [Tooltip("스폰 시작 전 기다릴 시간입니다.")]
    public float spawnDelaySeconds;
    [Header("다중 스폰 규칙")]
    [InspectorName("여러 마리 순차 스폰")]
    [Tooltip("켜면 여러 마리를 한 번에 만들지 않고 순서대로 생성합니다.")]
    public bool useSequentialSpawnWhenMultiple = true;
    [InspectorName("현재 몬스터 전멸 대기")]
    [Tooltip("켜면 현재 소환된 몬스터를 모두 잡은 뒤 다음 몬스터를 소환합니다.")]
    public bool waitUntilCurrentSpawnedMonstersDefeated = true;
    [InspectorName("다음 스폰 최대 대기 시간")]
    [Tooltip("전멸하지 않아도 이 시간이 지나면 다음 몬스터를 소환할 수 있습니다.")]
    public float nextSpawnMaxWaitSeconds = 5f;
    [InspectorName("보상 수령 시 스폰 생략")]
    [Tooltip("켜면 이미 보상을 받은 적/보스는 다시 스폰하지 않습니다.")]
    public bool skipSpawnWhenRewardClaimed = true;
    [Header("위치")]
    [InspectorName("스폰 위치 오프셋")]
    [Tooltip("스폰 포인트 기준으로 더할 위치 보정입니다.")]
    public Vector2 spawnOffset;
    [InspectorName("스폰 지점 랜덤 선택")]
    [Tooltip("켜면 조건에 맞는 스폰 지점 중 하나를 랜덤으로 선택합니다.")]
    public bool randomizePoint;
    [InspectorName("시작 시 자동 스폰")]
    [Tooltip("켜면 스테이지 시작 시 자동으로 스폰합니다.")]
    public bool spawnOnStart = true;
}
