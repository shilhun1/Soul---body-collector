using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class hys_BossPortalEntrance : MonoBehaviour
{
    // 보스방으로 이동할 씬과 도착 지점을 지정하는 전용 포탈입니다.
    [Header("이동 설정")]
    [SerializeField] private string bossSceneName = "hys";
    [SerializeField] private string destinationSpawnId = "BossEntrance";
    [SerializeField] private bool enterImmediatelyOnContact = true;
    [SerializeField] private Key interactionKey = Key.F;
    [SerializeField] private float loadDelaySeconds;

    [Header("선택 연출")]
    [SerializeField] private GameObject interactionGuide;

    private Transform playerInRange;
    private bool isLoading;

    public static string PendingSpawnId { get; private set; }

    private void Awake()
    {
        SetGuideVisible(false);
    }

    private void Update()
    {
        if (enterImmediatelyOnContact || playerInRange == null || isLoading || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[interactionKey].wasPressedThisFrame)
        {
            StartCoroutine(LoadBossSceneRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryResolvePlayer(other, out Transform player))
        {
            return;
        }

        playerInRange = player;
        SetGuideVisible(!enterImmediatelyOnContact);

        if (enterImmediatelyOnContact && !isLoading)
        {
            StartCoroutine(LoadBossSceneRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInRange == null || other.transform.root != playerInRange.root)
        {
            return;
        }

        playerInRange = null;
        SetGuideVisible(false);
    }

    private IEnumerator LoadBossSceneRoutine()
    {
        if (isLoading || string.IsNullOrWhiteSpace(bossSceneName))
        {
            yield break;
        }

        isLoading = true;
        SetGuideVisible(false);
        PendingSpawnId = destinationSpawnId;

        if (loadDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(loadDelaySeconds);
        }

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(bossSceneName);

        if (loadOperation == null)
        {
            isLoading = false;
            Debug.LogError($"보스 씬을 불러올 수 없습니다: {bossSceneName}", this);
        }
    }

    public static bool ConsumeSpawnId(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId) || PendingSpawnId != spawnId)
        {
            return false;
        }

        PendingSpawnId = null;
        return true;
    }

    private static bool TryResolvePlayer(Collider2D other, out Transform player)
    {
        player = null;

        if (other == null)
        {
            return false;
        }

        HWJ_RootObjectDataResolver resolver = other.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (resolver != null && resolver.ObjectType == HWJ_ObjectType.Player)
        {
            player = resolver.transform;
            return true;
        }

        if (other.CompareTag("Player"))
        {
            player = other.transform.root;
            return true;
        }

        return false;
    }

    private void SetGuideVisible(bool visible)
    {
        if (interactionGuide != null)
        {
            interactionGuide.SetActive(visible);
        }
    }
}
