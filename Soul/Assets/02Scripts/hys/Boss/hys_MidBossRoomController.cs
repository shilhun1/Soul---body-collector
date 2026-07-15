using UnityEngine;

public class hys_MidBossRoomController : MonoBehaviour
{
    // 플레이어가 보스방에 들어오면 입구를 닫고, 처치 후 출구와 보상을 활성화합니다.
    [Header("보스 참조")]
    [SerializeField] private hys_Stage1MidBossLogic bossLogic;
    [SerializeField] private HWJ_RuntimeStatusSystem bossStatus;

    [Header("방 진행 오브젝트")]
    [SerializeField] private GameObject[] entranceBlockers;
    [SerializeField] private GameObject[] clearObjects;
    [SerializeField] private GameObject clearRewardPrefab;
    [SerializeField] private Transform clearRewardSpawnPoint;

    [Header("설정")]
    [SerializeField] private bool startEncounterOnPlayerEnter = true;
    [SerializeField] private bool openRoomWhenDisabled = true;

    private bool encounterStarted;
    private bool clearHandled;

    private void Awake()
    {
        CacheReferences();
        SetEntranceClosed(false);
        SetClearObjectsVisible(false);
    }

    private void Update()
    {
        CacheReferences();

        if (!clearHandled && bossStatus != null && bossStatus.IsDead)
        {
            HandleBossClear();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!startEncounterOnPlayerEnter || encounterStarted || !IsPlayer(other))
        {
            return;
        }

        StartEncounter();
    }

    public void StartEncounter()
    {
        if (encounterStarted || clearHandled || bossLogic == null)
        {
            return;
        }

        encounterStarted = true;
        SetEntranceClosed(true);
        bossLogic.StartEncounter();
    }

    public void HandleBossClear()
    {
        if (clearHandled)
        {
            return;
        }

        clearHandled = true;
        encounterStarted = false;
        SetEntranceClosed(false);
        SetClearObjectsVisible(true);

        if (clearRewardPrefab != null)
        {
            Vector3 spawnPosition = clearRewardSpawnPoint != null
                ? clearRewardSpawnPoint.position
                : transform.position;
            Instantiate(clearRewardPrefab, spawnPosition, Quaternion.identity);
        }
    }

    private void CacheReferences()
    {
        if (bossLogic == null)
        {
            bossLogic = FindFirstObjectByType<hys_Stage1MidBossLogic>();
        }

        if (bossStatus == null && bossLogic != null)
        {
            bossStatus = bossLogic.GetComponent<HWJ_RuntimeStatusSystem>();
        }
    }

    private void SetEntranceClosed(bool closed)
    {
        SetObjectsActive(entranceBlockers, closed);
    }

    private void SetClearObjectsVisible(bool visible)
    {
        SetObjectsActive(clearObjects, visible);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        HWJ_RootObjectDataResolver resolver = other.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return (resolver != null && resolver.ObjectType == HWJ_ObjectType.Player)
            || other.CompareTag("Player");
    }

    private void OnDisable()
    {
        if (openRoomWhenDisabled)
        {
            SetEntranceClosed(false);
        }
    }
}
