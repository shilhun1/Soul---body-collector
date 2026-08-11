using UnityEngine;

public enum HWJ_GimmickHitSource
{
    Unknown,
    AreaAttack,
    Projectile,
    DashContact
}

public enum HWJ_BodyObstacleRequirementMode
{
    SpiritOnly,
    PossessedBody,
    WeaponType,
    AbilityTag,
    BodyObjectId,
    BodyObjectType
}

[DisallowMultipleComponent]
public class HWJ_BodyExclusiveObstacleSystem : MonoBehaviour
{
    [Header("장애물 식별")]
    [Tooltip("이벤트와 디버그에서 사용하는 고정 ID입니다.")]
    [SerializeField] private string obstacleId = "body_obstacle";

    [Header("통과 조건")]
    [Tooltip("어떤 기준으로 통과 가능 여부를 판단할지 선택합니다.")]
    [SerializeField] private HWJ_BodyObstacleRequirementMode requirementMode = HWJ_BodyObstacleRequirementMode.WeaponType;

    [Tooltip("WeaponType 조건에서 허용할 빙의체 무기 타입 목록입니다. 비워두면 None이 아닌 모든 무기를 허용합니다.")]
    [SerializeField] private HWJ_WeaponType[] allowedWeaponTypes;

    [Tooltip("AbilityTag condition list. These tags live on RootObjectDataSO.Identity and describe map gimmick access.")]
    [SerializeField] private HWJ_AbilityTag[] allowedAbilityTags;

    [Tooltip("BodyObjectId 조건에서 허용할 RootObjectData Identity Object Id 목록입니다.")]
    [SerializeField] private string[] allowedBodyObjectIds;

    [Tooltip("BodyObjectType 조건에서 허용할 빙의체 오브젝트 타입입니다.")]
    [SerializeField] private HWJ_ObjectType requiredBodyObjectType = HWJ_ObjectType.Enemy;

    [Tooltip("켜면 조건을 만족할 때 장애물이 열립니다. 끄면 조건을 만족할 때 닫힙니다.")]
    [SerializeField] private bool openWhenRequirementMet = true;

    [Tooltip("켜면 플레이어가 근처에 없어도 매 프레임 현재 상태를 다시 검사합니다.")]
    [SerializeField] private bool updateContinuously = true;

    [Tooltip("켜면 Player 태그를 가진 오브젝트만 검사합니다.")]
    [SerializeField] private bool requirePlayerTag = true;

    [Header("무기 공격 작동")]
    [Tooltip("켜면 요구 무기/스킬로 공격했을 때 장애물이 열립니다. 도끼 파괴 벽, 창 돌진 장치에 사용합니다.")]
    [SerializeField] private bool openByWeaponSkillHit;

    [Tooltip("When enabled, the gate stays closed until the required weapon skill hit activates it.")]
    [SerializeField] private bool requireWeaponSkillHitToOpen;

    [Tooltip("무기 공격으로 열린 뒤 계속 열린 상태를 유지합니다.")]
    [SerializeField] private bool stayOpenAfterWeaponSkillHit = true;

    [Tooltip("무기 공격으로 열린 뒤 투사체를 소모할지 결정합니다.")]
    [SerializeField] private bool consumeProjectileOnWeaponHit = true;

    [Tooltip("공격으로 열 때 요구할 빙의 무기입니다. None이면 현재 빙의 무기를 제한하지 않습니다.")]
    [SerializeField] private HWJ_WeaponType requiredHitWeaponType = HWJ_WeaponType.None;

    [Tooltip("공격으로 열 때 요구할 스킬 행동 타입입니다. None이면 스킬 타입을 제한하지 않습니다.")]
    [SerializeField] private HWJ_SkillActionType requiredHitSkillActionType = HWJ_SkillActionType.None;

    [Tooltip("특정 스킬 ID로만 열어야 할 때 입력합니다. 비워두면 스킬 ID를 제한하지 않습니다.")]
    [SerializeField] private string requiredHitSkillActionId;

    [Tooltip("켜면 플레이어가 빙의한 몸으로 공격했을 때만 무기 공격 작동이 가능합니다.")]
    [SerializeField] private bool requirePossessedPlayerHit = true;

    [Header("위험 구간 피해")]
    [Tooltip("조건을 만족하지 못한 플레이어가 범위 안에 있을 때 주기적으로 HP 피해를 줍니다. 방패 전용 화살 통로에 사용합니다.")]
    [SerializeField] private bool damagePlayerWhenBlocked;

    [Tooltip("피해 한 번에 적용할 HP 피해량입니다.")]
    [SerializeField] private float blockedDamageAmount = 10f;

    [Tooltip("피해 적용 간격입니다.")]
    [SerializeField] private float blockedDamageIntervalSeconds = 0.5f;

    [Tooltip("켜면 빙의 상태의 플레이어에게만 피해를 줍니다. 영혼 정찰 구간과 충돌하지 않도록 기본값을 켜둡니다.")]
    [SerializeField] private bool damageOnlyPossessedBody = true;

    [Header("장애물 대상")]
    [Tooltip("통과 가능할 때 꺼지고, 막힐 때 켜지는 콜라이더 목록입니다. 비워두면 자식 non-trigger 콜라이더를 자동 사용합니다.")]
    [SerializeField] private Collider2D[] obstacleColliders;

    [Tooltip("통과 가능할 때 숨길 오브젝트입니다.")]
    [SerializeField] private GameObject[] hideWhenOpen;

    [Tooltip("통과 가능할 때 보여줄 오브젝트입니다.")]
    [SerializeField] private GameObject[] showWhenOpen;

    [Header("런타임 확인")]
    [SerializeField] private bool isOpen;
    [SerializeField] private bool playerInside;
    [SerializeField] private bool openedByWeaponHit;
    [SerializeField] private string lastObstacleResult;

    private GameObject currentPlayerObject;
    private float nextBlockedDamageTime;

    public string ObstacleId => obstacleId;
    public bool IsOpen => isOpen;
    public bool OpenedByWeaponHit => openedByWeaponHit;
    public string LastObstacleResult => lastObstacleResult;

    private void Reset()
    {
        CacheObstacleColliders();
    }

    private void Awake()
    {
        CacheObstacleColliders();
        ApplyObstacleState(false, null, "장애물 초기화");
    }

    private void Update()
    {
        if (!updateContinuously && !playerInside)
        {
            return;
        }

        EvaluateAndApply(ResolveActivePlayerObject());
    }

    public bool EvaluateAndApply(GameObject playerObject)
    {
        if (openedByWeaponHit && stayOpenAfterWeaponSkillHit)
        {
            ApplyObstacleState(true, playerObject, "무기 기믹으로 열린 상태를 유지합니다.");
            return true;
        }

        if (requireWeaponSkillHitToOpen)
        {
            bool canUseRequiredBody = CanPlayerPass(playerObject, out string bodyReason);
            string waitReason = canUseRequiredBody
                ? "Weapon skill gate is waiting for the required skill hit."
                : bodyReason;
            ApplyObstacleState(false, playerObject, waitReason);
            TryApplyBlockedDamage(playerObject, false);
            return false;
        }

        bool canPass = CanPlayerPass(playerObject, out string reason);
        bool shouldOpen = openWhenRequirementMet ? canPass : !canPass;
        ApplyObstacleState(shouldOpen, playerObject, reason);
        TryApplyBlockedDamage(playerObject, canPass);
        return canPass;
    }

    public bool TryActivateFromSkillHit(
        Transform sourceTransform,
        HWJ_SkillActionDataSO skillAction,
        HWJ_GimmickHitSource hitSource,
        out bool consumeHit)
    {
        consumeHit = false;

        if (!openByWeaponSkillHit)
        {
            return false;
        }

        if (openedByWeaponHit && stayOpenAfterWeaponSkillHit)
        {
            consumeHit = consumeProjectileOnWeaponHit && hitSource == HWJ_GimmickHitSource.Projectile;
            lastObstacleResult = "무기 기믹 작동 실패: 이미 열린 장애물입니다.";
            return false;
        }

        if (!CanWeaponHitActivate(sourceTransform, skillAction, out string failureReason))
        {
            lastObstacleResult = failureReason;
            return false;
        }

        openedByWeaponHit = true;
        consumeHit = consumeProjectileOnWeaponHit && hitSource == HWJ_GimmickHitSource.Projectile;
        GameObject sourceObject = HWJ_WeaponGimmickActivatorUtility.ResolveSourceGameObject(sourceTransform);
        ApplyObstacleState(true, sourceObject, "무기 기믹 작동: 장애물이 열렸습니다.");
        return true;
    }

    public bool CanPlayerPass(GameObject playerObject, out string reason)
    {
        reason = null;

        if (playerObject == null)
        {
            reason = "육신 전용 장애물 대기: 플레이어를 찾지 못했습니다.";
            return false;
        }

        if (requirePlayerTag && !HasPlayerTag(playerObject))
        {
            reason = "육신 전용 장애물 차단: Player 태그가 아닙니다.";
            return false;
        }

        HWJ_SoulSystem soulSystem = playerObject.GetComponentInParent<HWJ_SoulSystem>();
        HWJ_PossessionSystem possessionSystem = playerObject.GetComponentInParent<HWJ_PossessionSystem>();

        switch (requirementMode)
        {
            case HWJ_BodyObstacleRequirementMode.SpiritOnly:
                return CheckSpiritOnly(soulSystem, out reason);
            case HWJ_BodyObstacleRequirementMode.PossessedBody:
                return CheckPossessedBody(possessionSystem, out reason);
            case HWJ_BodyObstacleRequirementMode.WeaponType:
                return CheckWeaponType(possessionSystem, out reason);
            case HWJ_BodyObstacleRequirementMode.AbilityTag:
                return CheckAbilityTag(possessionSystem, out reason);
            case HWJ_BodyObstacleRequirementMode.BodyObjectId:
                return CheckBodyObjectId(possessionSystem, out reason);
            case HWJ_BodyObstacleRequirementMode.BodyObjectType:
                return CheckBodyObjectType(possessionSystem, out reason);
            default:
                reason = "육신 전용 장애물 차단: 알 수 없는 조건입니다.";
                return false;
        }
    }

    private static bool CheckSpiritOnly(HWJ_SoulSystem soulSystem, out string reason)
    {
        bool passed = soulSystem != null
            && soulSystem.CurrentExistenceState == HWJ_PlayerExistenceState.Spirit;
        reason = passed
            ? "영혼 상태 조건을 만족했습니다."
            : "영혼 상태에서만 통과할 수 있습니다.";
        return passed;
    }

    private static bool CheckPossessedBody(HWJ_PossessionSystem possessionSystem, out string reason)
    {
        bool passed = possessionSystem != null && possessionSystem.HasActivePossessedBody;
        reason = passed
            ? "빙의 상태 조건을 만족했습니다."
            : "빙의 상태에서만 통과할 수 있습니다.";
        return passed;
    }

    private bool CheckWeaponType(HWJ_PossessionSystem possessionSystem, out string reason)
    {
        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            reason = "빙의체 무기 조건 차단: 현재 빙의체가 없습니다.";
            return false;
        }

        HWJ_WeaponType currentWeapon = possessionSystem.CurrentWeaponType;
        bool passed = ContainsWeaponType(currentWeapon);
        reason = passed
            ? $"빙의체 무기 조건을 만족했습니다: {currentWeapon}."
            : $"이 무기 타입으로는 통과할 수 없습니다: {currentWeapon}.";
        return passed;
    }

    private bool CheckAbilityTag(HWJ_PossessionSystem possessionSystem, out string reason)
    {
        HWJ_RootObjectDataResolver bodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : null;

        if (bodyResolver == null || !possessionSystem.HasActivePossessedBody)
        {
            reason = "AbilityTag gate blocked: there is no active possessed body.";
            return false;
        }

        bool passed = ContainsAbilityTag(bodyResolver);
        reason = passed
            ? "AbilityTag gate passed by the possessed body."
            : "AbilityTag gate blocked: the possessed body does not have the required tag.";
        return passed;
    }

    private bool CheckBodyObjectId(HWJ_PossessionSystem possessionSystem, out string reason)
    {
        if (possessionSystem == null || !possessionSystem.TryGetPossessedRootObjectId(out string bodyObjectId))
        {
            reason = "빙의체 ID 조건 차단: 현재 빙의체 ID가 없습니다.";
            return false;
        }

        bool passed = ContainsBodyObjectId(bodyObjectId);
        reason = passed
            ? $"빙의체 ID 조건을 만족했습니다: {bodyObjectId}."
            : $"이 빙의체로는 통과할 수 없습니다: {bodyObjectId}.";
        return passed;
    }

    private bool CheckBodyObjectType(HWJ_PossessionSystem possessionSystem, out string reason)
    {
        HWJ_RootObjectDataResolver bodyResolver = possessionSystem != null
            ? possessionSystem.PossessedBodyResolver
            : null;

        if (bodyResolver == null)
        {
            reason = "빙의체 타입 조건 차단: 현재 빙의체가 없습니다.";
            return false;
        }

        bool passed = bodyResolver.ObjectType == requiredBodyObjectType;
        reason = passed
            ? $"빙의체 타입 조건을 만족했습니다: {requiredBodyObjectType}."
            : $"이 빙의체 타입으로는 통과할 수 없습니다: {bodyResolver.ObjectType}.";
        return passed;
    }

    private bool ContainsWeaponType(HWJ_WeaponType weaponType)
    {
        if (allowedWeaponTypes == null || allowedWeaponTypes.Length == 0)
        {
            return weaponType != HWJ_WeaponType.None;
        }

        for (int i = 0; i < allowedWeaponTypes.Length; i++)
        {
            if (allowedWeaponTypes[i] == weaponType)
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsAbilityTag(HWJ_RootObjectDataResolver bodyResolver)
    {
        if (bodyResolver == null || allowedAbilityTags == null || allowedAbilityTags.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < allowedAbilityTags.Length; i++)
        {
            if (bodyResolver.HasAbilityTag(allowedAbilityTags[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsBodyObjectId(string bodyObjectId)
    {
        if (string.IsNullOrWhiteSpace(bodyObjectId)
            || allowedBodyObjectIds == null
            || allowedBodyObjectIds.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < allowedBodyObjectIds.Length; i++)
        {
            if (allowedBodyObjectIds[i] == bodyObjectId)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyObstacleState(bool open, GameObject playerObject, string reason)
    {
        if (isOpen == open && lastObstacleResult == reason)
        {
            return;
        }

        isOpen = open;
        lastObstacleResult = reason;
        SetCollidersEnabled(obstacleColliders, !isOpen);
        SetGameObjectsActive(hideWhenOpen, !isOpen);
        SetGameObjectsActive(showWhenOpen, isOpen);

        HWJ_GameplayEvents.RaiseBodyObstacleGateChanged(
            new HWJ_BodyObstacleGateEvent(this, obstacleId, playerObject, isOpen, lastObstacleResult));
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
        EvaluateAndApply(currentPlayerObject);
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

        if (!updateContinuously)
        {
            ApplyObstacleState(false, null, "플레이어가 장애물 범위에서 벗어났습니다.");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damagePlayerWhenBlocked)
        {
            return;
        }

        GameObject playerObject = ResolvePlayerObject(other);

        if (playerObject == null)
        {
            return;
        }

        bool canPass = CanPlayerPass(playerObject, out _);
        TryApplyBlockedDamage(playerObject, canPass);
    }

    private bool CanWeaponHitActivate(
        Transform sourceTransform,
        HWJ_SkillActionDataSO skillAction,
        out string failureReason)
    {
        failureReason = null;

        if (requirePossessedPlayerHit && !HWJ_WeaponGimmickActivatorUtility.IsPossessedPlayerSource(sourceTransform))
        {
            failureReason = "무기 기믹 작동 실패: 플레이어가 빙의한 몸의 공격이 아닙니다.";
            return false;
        }

        HWJ_WeaponType sourceWeapon = HWJ_WeaponGimmickActivatorUtility.ResolveSourceWeapon(sourceTransform);

        if (requiredHitWeaponType != HWJ_WeaponType.None && sourceWeapon != requiredHitWeaponType)
        {
            failureReason = $"무기 기믹 작동 실패: 요구 무기 {requiredHitWeaponType}, 현재 무기 {sourceWeapon}.";
            return false;
        }

        if (requiredHitSkillActionType != HWJ_SkillActionType.None)
        {
            HWJ_SkillActionType sourceActionType = skillAction != null
                ? skillAction.ActionType
                : HWJ_SkillActionType.None;

            if (sourceActionType != requiredHitSkillActionType)
            {
                failureReason = $"무기 기믹 작동 실패: 요구 스킬 타입 {requiredHitSkillActionType}, 현재 타입 {sourceActionType}.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(requiredHitSkillActionId))
        {
            string sourceSkillId = skillAction != null ? skillAction.SkillActionId : null;

            if (!string.Equals(sourceSkillId, requiredHitSkillActionId, System.StringComparison.Ordinal))
            {
                failureReason = $"무기 기믹 작동 실패: 요구 스킬 ID {requiredHitSkillActionId}.";
                return false;
            }
        }

        if (allowedAbilityTags != null
            && allowedAbilityTags.Length > 0
            && !HWJ_WeaponGimmickActivatorUtility.SourceHasAnyAbilityTag(sourceTransform, allowedAbilityTags))
        {
            failureReason = "Weapon skill gate activation failed: the possessed body does not have the required AbilityTag.";
            return false;
        }

        return true;
    }

    private void TryApplyBlockedDamage(GameObject playerObject, bool canPass)
    {
        if (!damagePlayerWhenBlocked
            || canPass
            || playerObject == null
            || Time.time < nextBlockedDamageTime)
        {
            return;
        }

        HWJ_SoulSystem playerSoul = playerObject.GetComponentInParent<HWJ_SoulSystem>();

        if (damageOnlyPossessedBody
            && (playerSoul == null || playerSoul.CurrentState != HWJ_SoulRuntimeState.Body))
        {
            return;
        }

        HWJ_RuntimeStatusSystem playerStatus = playerObject.GetComponentInParent<HWJ_RuntimeStatusSystem>();

        if (playerStatus == null)
        {
            return;
        }

        nextBlockedDamageTime = Time.time + Mathf.Max(0.05f, blockedDamageIntervalSeconds);
        playerStatus.ApplyDamage(Mathf.Max(0f, blockedDamageAmount));
        lastObstacleResult = $"위험 구간 피해 적용: {blockedDamageAmount:0.##}.";
    }

    private GameObject ResolveActivePlayerObject()
    {
        if (currentPlayerObject != null)
        {
            return currentPlayerObject;
        }

        return HWJ_GameAccess.PlayerResolver != null
            ? HWJ_GameAccess.PlayerResolver.gameObject
            : null;
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

    private void CacheObstacleColliders()
    {
        if (obstacleColliders != null && obstacleColliders.Length > 0)
        {
            return;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        int solidCount = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                solidCount++;
            }
        }

        obstacleColliders = new Collider2D[solidCount];
        int index = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                obstacleColliders[index++] = colliders[i];
            }
        }
    }

    private static void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
            }
        }
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
}

/// <summary>
/// 스킬 공격이 스위치와 장애물 같은 기믹을 때렸는지 공통으로 판단하는 유틸리티입니다.
/// 투사체, 근접 판정, 돌진 판정이 같은 규칙을 쓰도록 한 곳에서 소스 무기와 빙의 상태를 해석합니다.
/// </summary>
public static class HWJ_WeaponGimmickActivatorUtility
{
    public static bool TryActivateFromHit(
        Collider2D hitCollider,
        Transform sourceTransform,
        HWJ_SkillActionDataSO skillAction,
        HWJ_GimmickHitSource hitSource,
        out bool consumeHit,
        out string resultMessage)
    {
        consumeHit = false;
        resultMessage = null;

        if (hitCollider == null)
        {
            return false;
        }

        HWJ_SpiritOrbSwitchSystem switchSystem = hitCollider.GetComponentInParent<HWJ_SpiritOrbSwitchSystem>();

        if (switchSystem != null
            && switchSystem.TryActivateFromSkillHit(sourceTransform, skillAction, hitSource, out consumeHit))
        {
            resultMessage = switchSystem.LastSwitchResult;
            return true;
        }

        HWJ_BodyExclusiveObstacleSystem obstacleSystem = hitCollider.GetComponentInParent<HWJ_BodyExclusiveObstacleSystem>();

        if (obstacleSystem != null
            && obstacleSystem.TryActivateFromSkillHit(sourceTransform, skillAction, hitSource, out consumeHit))
        {
            resultMessage = obstacleSystem.LastObstacleResult;
            return true;
        }

        HSH.Gimmick.HSH_SpearDashBreakable spearBreakable = hitCollider.GetComponentInParent<HSH.Gimmick.HSH_SpearDashBreakable>();

        if (spearBreakable != null
            && spearBreakable.TryActivateFromSkillHit(sourceTransform, skillAction, out consumeHit))
        {
            resultMessage = $"Spear Dash Breakable activated by skill {skillAction?.SkillActionId}.";
            return true;
        }

        return false;
    }

    public static GameObject ResolveSourceGameObject(Transform sourceTransform)
    {
        return sourceTransform != null ? sourceTransform.gameObject : null;
    }

    public static HWJ_WeaponType ResolveSourceWeapon(Transform sourceTransform)
    {
        if (sourceTransform == null)
        {
            return HWJ_WeaponType.None;
        }

        HWJ_PossessionSystem possessionSystem = sourceTransform.GetComponentInParent<HWJ_PossessionSystem>();

        if (possessionSystem != null && possessionSystem.HasActivePossessedBody)
        {
            return possessionSystem.CurrentWeaponType;
        }

        HWJ_RootObjectDataResolver dataResolver = sourceTransform.GetComponentInParent<HWJ_RootObjectDataResolver>();
        return dataResolver != null ? dataResolver.WeaponType : HWJ_WeaponType.None;
    }

    public static bool SourceHasAnyAbilityTag(Transform sourceTransform, HWJ_AbilityTag[] abilityTags)
    {
        if (sourceTransform == null || abilityTags == null || abilityTags.Length == 0)
        {
            return false;
        }

        HWJ_PossessionSystem possessionSystem = sourceTransform.GetComponentInParent<HWJ_PossessionSystem>();
        HWJ_RootObjectDataResolver resolver = possessionSystem != null && possessionSystem.HasActivePossessedBody
            ? possessionSystem.PossessedBodyResolver
            : sourceTransform.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (resolver == null)
        {
            return false;
        }

        for (int i = 0; i < abilityTags.Length; i++)
        {
            if (resolver.HasAbilityTag(abilityTags[i]))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsPossessedPlayerSource(Transform sourceTransform)
    {
        if (sourceTransform == null)
        {
            return false;
        }

        HWJ_PossessionSystem possessionSystem = sourceTransform.GetComponentInParent<HWJ_PossessionSystem>();
        HWJ_SoulSystem soulSystem = sourceTransform.GetComponentInParent<HWJ_SoulSystem>();

        if (possessionSystem == null || !possessionSystem.HasActivePossessedBody)
        {
            return false;
        }

        if (soulSystem != null && soulSystem.CurrentExistenceState != HWJ_PlayerExistenceState.Possessed)
        {
            return false;
        }

        HWJ_RootObjectDataResolver dataResolver = sourceTransform.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (dataResolver != null)
        {
            return dataResolver.ObjectType == HWJ_ObjectType.Player;
        }

        return HasPlayerTag(sourceTransform.gameObject);
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
}
