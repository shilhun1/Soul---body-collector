using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class HWJ_ScenePortalSystem : MonoBehaviour
{
    [Header("포탈 이동 대상")]
    [Tooltip("이동할 씬 이름입니다. 해당 씬은 반드시 Build Settings에 등록되어 있어야 합니다.")]
    [SerializeField] private string targetSceneName;
    [Tooltip("다음 씬에서 이동할 HWJ_SpawnPoint의 Point Id입니다.")]
    [SerializeField] private string targetSpawnPointId;

    [Space(8f)]
    [Header("작동 방식")]
    [Tooltip("켜져 있으면 포탈 범위 안에서 상호작용 입력을 눌러야 이동합니다.")]
    [SerializeField] private bool requireInteractInput = true;
    [Tooltip("켜져 있으면 플레이어가 포탈에 닿는 순간 바로 이동합니다.")]
    [SerializeField] private bool loadImmediatelyOnEnter;
    [Tooltip("포탈이 연속으로 여러 번 발동하지 않도록 막는 시간입니다.")]
    [SerializeField] private float reuseCooldownSeconds = 0.5f;
    [Tooltip("Player 태그를 가진 오브젝트만 포탈을 사용할 수 있게 제한합니다.")]
    [SerializeField] private bool requirePlayerTag = true;

    [Space(8f)]
    [Header("스테이지 진행 조건")]
    [Tooltip("켜면 스테이지 목표가 완료된 뒤에만 포탈을 사용할 수 있습니다.")]
    [SerializeField] private bool requireStageObjectiveComplete;
    [Tooltip("목표 완료 상태를 확인할 스테이지 진행 시스템입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private HWJ_StageProgressionSystem stageProgressionSystem;
    [Tooltip("포탈 사용 직전에 남은 적 수를 다시 검사할 적 카운트 시스템입니다.")]
    [SerializeField] private HWJ_StageEnemyCountSystem stageEnemyCountSystem;
    [Tooltip("켜면 포탈을 사용하기 직전에 적 카운트를 강제로 다시 검사합니다.")]
    [SerializeField] private bool scanEnemyCountBeforeUse = true;

    [Space(8f)]
    [Header("참조")]
    [Tooltip("비워두면 자동으로 찾거나 생성합니다.")]
    [SerializeField] private HWJ_SceneTransitionSystem sceneTransitionSystem;

    [Space(8f)]
    [Header("확인용 결과")]
    [SerializeField] private bool playerInside;
    [SerializeField] private string lastPortalResult;

    private GameObject currentPlayerObject;
    private HWJ_PlayerInputSystem currentPlayerInput;
    private float nextUsableTime;

    public string TargetSceneName => targetSceneName;
    public string TargetSpawnPointId => targetSpawnPointId;
    public bool PlayerInside => playerInside;
    public bool RequireStageObjectiveComplete => requireStageObjectiveComplete;
    public string LastPortalResult => lastPortalResult;

    private void Reset()
    {
        Collider2D portalCollider = GetComponent<Collider2D>();

        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        EnsureTriggerCollider();
    }

    private void Update()
    {
        if (!playerInside || loadImmediatelyOnEnter || !requireInteractInput)
        {
            return;
        }

        ResolveCurrentPlayerInput();

        if (currentPlayerInput != null && currentPlayerInput.InteractPressedThisFrame)
        {
            TryUsePortal();
        }
    }

    public bool TryUsePortal()
    {
        if (Time.time < nextUsableTime)
        {
            lastPortalResult = "포탈 사용 실패: 재사용 대기시간 중입니다.";
            return false;
        }

        if (currentPlayerObject == null)
        {
            lastPortalResult = "포탈 사용 실패: 플레이어가 포탈 범위 안에 없습니다.";
            return false;
        }

        if (!CanPassStageProgressionGate())
        {
            return false;
        }

        if (sceneTransitionSystem == null)
        {
            sceneTransitionSystem = HWJ_SceneTransitionSystem.GetOrCreate();
        }

        HWJ_SceneTransitionResult result = sceneTransitionSystem.TryStartTransitionResult(
            targetSceneName,
            targetSpawnPointId,
            currentPlayerObject,
            this);

        lastPortalResult = result.Message;

        if (result.Succeeded)
        {
            nextUsableTime = Time.time + Mathf.Max(0f, reuseCooldownSeconds);
        }

        return result.Succeeded;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = true;
        currentPlayerObject = other.gameObject;
        currentPlayerInput = null;
        lastPortalResult = "포탈 범위 진입";

        if (loadImmediatelyOnEnter || !requireInteractInput)
        {
            TryUsePortal();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentPlayerObject == null || other.gameObject != currentPlayerObject)
        {
            return;
        }

        playerInside = false;
        currentPlayerObject = null;
        currentPlayerInput = null;
        lastPortalResult = "포탈 범위 이탈";
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (requirePlayerTag && !other.CompareTag("Player"))
        {
            return false;
        }

        HWJ_RootObjectDataResolver resolver = other.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return resolver == null || resolver.ObjectType == HWJ_ObjectType.Player;
    }

    private void ResolveCurrentPlayerInput()
    {
        if (currentPlayerInput != null)
        {
            return;
        }

        if (currentPlayerObject != null)
        {
            currentPlayerInput = currentPlayerObject.GetComponentInParent<HWJ_PlayerInputSystem>();
        }

        if (currentPlayerInput == null)
        {
            currentPlayerInput = HWJ_GameAccess.PlayerInput;
        }
    }

    private bool CanPassStageProgressionGate()
    {
        if (!requireStageObjectiveComplete)
        {
            return true;
        }

        ResolveStageProgressionReferences();

        if (scanEnemyCountBeforeUse && stageEnemyCountSystem != null)
        {
            stageEnemyCountSystem.ForceScan("portal_use");
        }

        if (stageProgressionSystem == null)
        {
            lastPortalResult = "포탈 사용 실패: 스테이지 진행 시스템을 찾지 못했습니다.";
            return false;
        }

        if (!stageProgressionSystem.ObjectiveComplete)
        {
            lastPortalResult = stageEnemyCountSystem != null
                ? $"포탈 잠김: 남은 적 {stageEnemyCountSystem.RemainingAliveEnemyCount}마리를 모두 처치해야 합니다."
                : "포탈 잠김: 스테이지 목표를 먼저 완료해야 합니다.";
            return false;
        }

        return true;
    }

    private void ResolveStageProgressionReferences()
    {
        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = GetComponentInParent<HWJ_StageProgressionSystem>();
        }

        if (stageProgressionSystem == null)
        {
            stageProgressionSystem = FindFirstObjectByType<HWJ_StageProgressionSystem>(FindObjectsInactive.Include);
        }

        if (stageEnemyCountSystem == null)
        {
            stageEnemyCountSystem = GetComponentInParent<HWJ_StageEnemyCountSystem>();
        }

        if (stageEnemyCountSystem == null)
        {
            stageEnemyCountSystem = FindFirstObjectByType<HWJ_StageEnemyCountSystem>(FindObjectsInactive.Include);
        }
    }

    private void EnsureTriggerCollider()
    {
        Collider2D portalCollider = GetComponent<Collider2D>();

        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.25f, 1f, 0.85f);
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}
