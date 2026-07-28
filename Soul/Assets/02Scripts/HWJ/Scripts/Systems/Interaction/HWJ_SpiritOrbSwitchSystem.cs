using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class HWJ_SpiritOrbSwitchSystem : MonoBehaviour
{
    [Header("스위치 식별")]
    [Tooltip("저장, 이벤트, 디버그에서 사용하는 고정 ID입니다.")]
    [SerializeField] private string switchId = "spirit_switch";

    [Tooltip("켜면 한 번만 작동합니다.")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("켜면 시작할 때 이미 작동한 상태가 됩니다.")]
    [SerializeField] private bool startActivated;

    [Header("작동 조건")]
    [Tooltip("켜면 영혼 상태의 플레이어만 스위치를 작동할 수 있습니다.")]
    [SerializeField] private bool requireSpiritState = true;

    [Tooltip("켜면 범위 안에서 상호작용 입력을 눌러야 작동합니다. 끄면 닿는 순간 작동합니다.")]
    [SerializeField] private bool requireInteractInput = true;

    [Tooltip("작동할 때 소모할 영혼 정신력입니다. 0이면 비용이 없습니다.")]
    [SerializeField] private float spiritMentalCost;

    [Tooltip("켜면 Player 태그를 가진 오브젝트만 작동 대상으로 인정합니다.")]
    [SerializeField] private bool requirePlayerTag = true;

    [Header("무기/스킬 작동 조건")]
    [Tooltip("켜면 활 화살, 창 돌진, 검 근접 공격 같은 무기 스킬 히트로도 스위치를 작동할 수 있습니다.")]
    [InspectorName("무기 스킬 히트 허용")]
    [SerializeField] private bool allowWeaponSkillHitActivation;

    [Tooltip("켜면 무기 스킬 히트도 플레이어가 빙의한 몸에서 나온 공격일 때만 인정합니다.")]
    [InspectorName("빙의 플레이어 공격만 허용")]
    [SerializeField] private bool requirePossessedPlayerHit = true;

    [Tooltip("작동에 필요한 빙의 무기입니다. None이면 무기 제한이 없습니다.")]
    [InspectorName("필요 히트 무기")]
    [SerializeField] private HWJ_WeaponType requiredHitWeaponType = HWJ_WeaponType.None;

    [Tooltip("작동에 필요한 스킬 행동 타입입니다. None이면 일반 공격도 포함해서 타입 제한 없이 인정합니다.")]
    [InspectorName("필요 히트 행동 타입")]
    [SerializeField] private HWJ_SkillActionType requiredHitSkillActionType = HWJ_SkillActionType.None;

    [Tooltip("특정 스킬 ID로만 작동해야 할 때 입력합니다. 비워두면 스킬 ID를 제한하지 않습니다.")]
    [InspectorName("필요 스킬 ID")]
    [SerializeField] private string requiredHitSkillActionId;

    [Tooltip("투사체로 스위치를 맞췄을 때 투사체를 사라지게 할지 결정합니다.")]
    [InspectorName("투사체 소모")]
    [SerializeField] private bool consumeProjectileOnWeaponHit = true;

    [Tooltip("스위치가 실제로 켜지기 위해 필요한 타격 횟수입니다. 검 전용 연속 타격 장치에 사용합니다.")]
    [InspectorName("필요 타격 횟수")]
    [SerializeField] private int requiredHitCount = 1;

    [Tooltip("연속 타격이 유지되는 시간입니다. 이 시간이 지나면 누적 타격 수가 초기화됩니다.")]
    [InspectorName("연속 타격 제한 시간")]
    [SerializeField] private float hitComboWindowSeconds = 1.5f;

    [Tooltip("켜면 연속 타격 제한 시간이 지나면 누적 타격 수를 0으로 되돌립니다.")]
    [InspectorName("시간 초과 시 타격 초기화")]
    [SerializeField] private bool resetHitCountWhenWindowExpires = true;

    [Header("작동 대상")]
    [Tooltip("스위치가 켜질 때 활성화할 오브젝트 목록입니다.")]
    [SerializeField] private GameObject[] activateTargets;

    [Tooltip("스위치가 켜질 때 비활성화할 오브젝트 목록입니다.")]
    [SerializeField] private GameObject[] deactivateTargets;

    [Tooltip("스위치가 켜질 때 enabled를 켤 컴포넌트 목록입니다.")]
    [SerializeField] private Behaviour[] enableBehaviours;

    [Tooltip("스위치가 켜질 때 enabled를 끌 컴포넌트 목록입니다.")]
    [SerializeField] private Behaviour[] disableBehaviours;

    [Tooltip("스위치가 켜질 때 enabled를 켤 콜라이더 목록입니다.")]
    [SerializeField] private Collider2D[] enableColliders;

    [Tooltip("스위치가 켜질 때 enabled를 끌 콜라이더 목록입니다.")]
    [SerializeField] private Collider2D[] disableColliders;

    [Header("애니메이션 선택")]
    [Tooltip("스위치 작동 애니메이션을 제어할 Animator입니다.")]
    [SerializeField] private Animator animator;

    [Tooltip("작동 상태를 넘길 Bool 파라미터 이름입니다.")]
    [SerializeField] private string activatedBoolParameter = "IsActivated";

    [Tooltip("작동 순간 실행할 Trigger 파라미터 이름입니다.")]
    [SerializeField] private string activateTriggerParameter = "Activate";

    [Header("런타임 확인")]
    [SerializeField] private bool isActivated;
    [SerializeField] private bool playerInside;
    [SerializeField] private int currentWeaponHitCount;
    [SerializeField] private string lastSwitchResult;

    private GameObject currentPlayerObject;
    private HWJ_PlayerInputSystem currentPlayerInput;
    private float lastWeaponHitTime;

    public string SwitchId => switchId;
    public bool IsActivated => isActivated;
    public bool PlayerInside => playerInside;
    public string LastSwitchResult => lastSwitchResult;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();

        if (startActivated)
        {
            SetActivated(true, null, "초기 활성화");
        }
    }

    private void Update()
    {
        if (!playerInside || !requireInteractInput)
        {
            return;
        }

        ResolveCurrentPlayerInput();

        if (currentPlayerInput != null && currentPlayerInput.InteractPressedThisFrame)
        {
            TryActivate(currentPlayerObject);
        }
    }

    public bool TryActivate(GameObject playerObject)
    {
        if (oneShot && isActivated)
        {
            lastSwitchResult = "영혼 구슬 스위치 작동 실패: 이미 작동했습니다.";
            return false;
        }

        if (!CanActivate(playerObject, out string failureMessage))
        {
            lastSwitchResult = failureMessage;
            return false;
        }

        if (!TrySpendSpiritMental(playerObject, out string mentalFailureMessage))
        {
            lastSwitchResult = mentalFailureMessage;
            return false;
        }

        SetActivated(true, playerObject, "영혼 구슬 스위치 작동");
        return true;
    }

    public bool TryActivateFromSkillHit(
        Transform sourceTransform,
        HWJ_SkillActionDataSO skillAction,
        HWJ_GimmickHitSource hitSource,
        out bool consumeHit)
    {
        consumeHit = false;

        if (!allowWeaponSkillHitActivation)
        {
            return false;
        }

        if (oneShot && isActivated)
        {
            lastSwitchResult = "무기 스위치 작동 실패: 이미 작동했습니다.";
            return false;
        }

        if (!CanWeaponHitActivate(sourceTransform, skillAction, out string failureMessage))
        {
            lastSwitchResult = failureMessage;
            return false;
        }

        consumeHit = consumeProjectileOnWeaponHit && hitSource == HWJ_GimmickHitSource.Projectile;

        if (resetHitCountWhenWindowExpires
            && currentWeaponHitCount > 0
            && Time.time - lastWeaponHitTime > Mathf.Max(0.01f, hitComboWindowSeconds))
        {
            currentWeaponHitCount = 0;
        }

        currentWeaponHitCount++;
        lastWeaponHitTime = Time.time;

        int requiredCount = Mathf.Max(1, requiredHitCount);

        if (currentWeaponHitCount < requiredCount)
        {
            lastSwitchResult = $"무기 스위치 타격 누적: {currentWeaponHitCount}/{requiredCount}.";
            return true;
        }

        currentWeaponHitCount = 0;
        GameObject sourceObject = HWJ_WeaponGimmickActivatorUtility.ResolveSourceGameObject(sourceTransform);
        SetActivated(true, sourceObject, "무기 스킬 히트로 스위치 작동");
        return true;
    }

    private bool CanActivate(GameObject playerObject, out string failureMessage)
    {
        failureMessage = null;

        if (playerObject == null)
        {
            failureMessage = "영혼 구슬 스위치 작동 실패: 플레이어가 범위 안에 없습니다.";
            return false;
        }

        if (requirePlayerTag && !HasPlayerTag(playerObject))
        {
            failureMessage = "영혼 구슬 스위치 작동 실패: Player 태그가 아닙니다.";
            return false;
        }

        if (!requireSpiritState)
        {
            return true;
        }

        HWJ_SoulSystem soulSystem = playerObject.GetComponentInParent<HWJ_SoulSystem>();

        if (soulSystem == null || soulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Spirit)
        {
            failureMessage = "영혼 구슬 스위치 작동 실패: 영혼 상태에서만 작동할 수 있습니다.";
            return false;
        }

        return true;
    }

    private bool CanWeaponHitActivate(
        Transform sourceTransform,
        HWJ_SkillActionDataSO skillAction,
        out string failureMessage)
    {
        failureMessage = null;

        if (requirePossessedPlayerHit && !HWJ_WeaponGimmickActivatorUtility.IsPossessedPlayerSource(sourceTransform))
        {
            failureMessage = "무기 스위치 작동 실패: 플레이어가 빙의한 몸의 공격이 아닙니다.";
            return false;
        }

        HWJ_WeaponType sourceWeapon = HWJ_WeaponGimmickActivatorUtility.ResolveSourceWeapon(sourceTransform);

        if (requiredHitWeaponType != HWJ_WeaponType.None && sourceWeapon != requiredHitWeaponType)
        {
            failureMessage = $"무기 스위치 작동 실패: 필요 무기 {requiredHitWeaponType}, 현재 무기 {sourceWeapon}.";
            return false;
        }

        if (requiredHitSkillActionType != HWJ_SkillActionType.None)
        {
            HWJ_SkillActionType sourceActionType = skillAction != null
                ? skillAction.ActionType
                : HWJ_SkillActionType.None;

            if (sourceActionType != requiredHitSkillActionType)
            {
                failureMessage = $"무기 스위치 작동 실패: 필요 행동 {requiredHitSkillActionType}, 현재 행동 {sourceActionType}.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(requiredHitSkillActionId))
        {
            string sourceSkillId = skillAction != null ? skillAction.SkillActionId : null;

            if (!string.Equals(sourceSkillId, requiredHitSkillActionId, System.StringComparison.Ordinal))
            {
                failureMessage = $"무기 스위치 작동 실패: 필요 스킬 ID {requiredHitSkillActionId}.";
                return false;
            }
        }

        return true;
    }

    private bool TrySpendSpiritMental(GameObject playerObject, out string failureMessage)
    {
        failureMessage = null;

        if (spiritMentalCost <= 0f)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem status = playerObject != null
            ? playerObject.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;

        if (status == null)
        {
            failureMessage = "영혼 구슬 스위치 작동 실패: 정신력 상태 시스템이 없습니다.";
            return false;
        }

        if (status.CurrentSpiritMentalValue - spiritMentalCost <= 0f)
        {
            failureMessage = $"영혼 구슬 스위치 작동 실패: 정신력이 부족합니다. 필요 {spiritMentalCost:0.##}.";
            return false;
        }

        if (!status.TryApplySpiritMentalCost(spiritMentalCost, "spirit_orb_switch"))
        {
            failureMessage = "영혼 구슬 스위치 작동 실패: 정신력 비용을 지불할 수 없습니다.";
            return false;
        }

        return true;
    }

    private void SetActivated(bool activated, GameObject playerObject, string message)
    {
        isActivated = activated;
        ApplyTargets(activated);
        ApplyAnimator(activated);
        lastSwitchResult = message;

        HWJ_GameplayEvents.RaiseSpiritOrbSwitchChanged(
            new HWJ_SpiritOrbSwitchEvent(this, switchId, playerObject, isActivated, lastSwitchResult));
    }

    private void ApplyTargets(bool activated)
    {
        SetGameObjectsActive(activateTargets, activated);
        SetGameObjectsActive(deactivateTargets, !activated);
        SetBehavioursEnabled(enableBehaviours, activated);
        SetBehavioursEnabled(disableBehaviours, !activated);
        SetCollidersEnabled(enableColliders, activated);
        SetCollidersEnabled(disableColliders, !activated);
    }

    private void ApplyAnimator(bool activated)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(activatedBoolParameter))
        {
            animator.SetBool(activatedBoolParameter, activated);
        }

        if (activated && !string.IsNullOrWhiteSpace(activateTriggerParameter))
        {
            animator.SetTrigger(activateTriggerParameter);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject playerObject = ResolvePlayerObject(other);

        if (playerObject == null)
        {
            return;
        }

        playerInside = true;
        currentPlayerObject = playerObject;
        currentPlayerInput = null;
        lastSwitchResult = "영혼 구슬 스위치 범위 진입";

        if (!requireInteractInput)
        {
            TryActivate(currentPlayerObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject playerObject = ResolvePlayerObject(other);

        if (currentPlayerObject == null || playerObject != currentPlayerObject)
        {
            return;
        }

        playerInside = false;
        currentPlayerObject = null;
        currentPlayerInput = null;
        lastSwitchResult = "영혼 구슬 스위치 범위 이탈";
    }

    private GameObject ResolvePlayerObject(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        if (requirePlayerTag && !HasPlayerTag(other.gameObject))
        {
            return null;
        }

        HWJ_SoulSystem soulSystem = other.GetComponentInParent<HWJ_SoulSystem>();
        return soulSystem != null ? soulSystem.gameObject : other.gameObject;
    }

    private static bool HasPlayerTag(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (candidate.CompareTag("Player"))
        {
            return true;
        }

        Transform parent = candidate.transform.parent;

        while (parent != null)
        {
            if (parent.CompareTag("Player"))
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    private static void SetGameObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(active);
            }
        }
    }

    private static void SetBehavioursEnabled(Behaviour[] targets, bool enabled)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].enabled = enabled;
            }
        }
    }

    private static void SetCollidersEnabled(Collider2D[] targets, bool enabled)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].enabled = enabled;
            }
        }
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

    private void EnsureTriggerCollider()
    {
        Collider2D switchCollider = GetComponent<Collider2D>();

        if (switchCollider != null)
        {
            switchCollider.isTrigger = true;
        }
    }
}
