using UnityEngine;

/// <summary>
/// 플레이어가 적 또는 보스의 육신에 빙의할 수 있는지 판단하는 컴포넌트입니다.
/// 플레이어 오브젝트에 붙이고, 대상 오브젝트의 HWJ_RootObjectDataResolver를 받아 빙의 가능 데이터를 확인합니다.
/// </summary>
public class HWJ_PossessionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver ownerDataResolver;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_RootObjectDataResolver possessedBodyResolver;
    [SerializeField] private bool moveOwnerToPossessedBody = true;
    [SerializeField] private bool copyPossessedBodyVisual = true;
    [SerializeField] private bool consumePossessedCorpse = true;
    [SerializeField] private string lastPossessionResult;

    private HWJ_PossessionData activePossessionBodyData;
    private SpriteRenderer ownerSpriteRenderer;
    private Sprite ownerOriginalSprite;
    private Color ownerOriginalColor;
    private bool ownerOriginalFlipX;
    private bool ownerOriginalFlipY;
    private bool hasOwnerSpriteCache;
    private Animator ownerAnimator;
    private RuntimeAnimatorController ownerOriginalAnimatorController;
    private bool hasOwnerAnimatorCache;

    public bool HasActivePossessedBody => possessedBodyResolver != null
        && (soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Body);

    public HWJ_RootObjectDataResolver PossessedBodyResolver => possessedBodyResolver;
    public HWJ_WeaponType CurrentWeaponType => HasActivePossessedBody
        ? possessedBodyResolver.WeaponType
        : HWJ_WeaponType.None;

    private void Awake()
    {
        if (ownerDataResolver == null)
        {
            ownerDataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        CacheOwnerVisual();
    }

    /// <summary>
    /// 현재 플레이어 상태와 대상의 PossessionData를 기준으로 빙의 가능 여부를 반환합니다.
    /// 대상은 EnemyTypeDataSO 또는 BossTypeDataSO를 가진 RootObjectData여야 합니다.
    /// </summary>
    public bool CanPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            lastPossessionResult = "Possession failed: missing target.";
            return false;
        }

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Soul)
        {
            lastPossessionResult = "Possession failed: player is not in Soul state.";
            return false;
        }

        if (ownerDataResolver != null
            && ownerDataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Possession != null
            && !playerData.Possession.canPossess)
        {
            lastPossessionResult = "Possession failed: player possession is disabled.";
            return false;
        }

        if (!TryGetPossessionBodyData(targetDataResolver, out HWJ_PossessionData possessionBody))
        {
            lastPossessionResult = "Possession failed: target has no possessable body data.";
            return false;
        }

        if (!possessionBody.canBePossessed)
        {
            lastPossessionResult = "Possession failed: target cannot be possessed.";
            return false;
        }

        if (!IsDefeatedIfRequired(targetDataResolver, possessionBody))
        {
            lastPossessionResult = "Possession failed: target is not defeated.";
            return false;
        }

        lastPossessionResult = "Possession target is valid.";
        return true;
    }

    /// <summary>
    /// 빙의 가능하면 대상 시체를 현재 육신으로 등록하고 플레이어를 육신 상태로 전환합니다.
    /// 등록된 육신의 무기, 스탯, 스킬은 RuntimeStatus/Combat/SkillActionSystem에서 읽어 사용합니다.
    /// </summary>
    public bool TryPossess(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (!CanPossess(targetDataResolver))
        {
            return false;
        }

        TryGetPossessionBodyData(targetDataResolver, out activePossessionBodyData);
        possessedBodyResolver = targetDataResolver;

        if (activePossessionBodyData == null || activePossessionBodyData.transfersControlToBody)
        {
            TransferOwnerToPossessedBody(targetDataResolver);
        }

        soulSystem?.EnterBodyState();

        if (activePossessionBodyData != null
            && activePossessionBodyData.loadsBodyStatsToPlayer
            && runtimeStatus != null)
        {
            runtimeStatus.RefreshCurrentHpFromData(true);
        }

        lastPossessionResult = $"Possessed {targetDataResolver.name}.";
        return true;
    }

    /// <summary>
    /// 현재 빙의 중인 육신을 해제합니다.
    /// 다시 유령 상태로 돌아가거나 육신이 소멸될 때 호출합니다.
    /// </summary>
    public void ClearPossessedBody(bool refreshStatus = true, bool refillToMax = false)
    {
        possessedBodyResolver = null;
        activePossessionBodyData = null;
        RestoreOwnerVisual();

        if (refreshStatus)
        {
            runtimeStatus?.RefreshCurrentHpFromData(refillToMax);
        }
    }

    /// <summary>
    /// 빙의한 육신의 공통 스탯을 가져옵니다.
    /// 플레이어 자체 SO를 바꾸지 않고 런타임 계산에서만 육신 스탯을 사용하기 위한 통로입니다.
    /// </summary>
    public bool TryGetPossessedStatus(out HWJ_StatusData status)
    {
        status = null;

        if (!CanLoadBodyStats() || possessedBodyResolver.Status == null)
        {
            return false;
        }

        status = possessedBodyResolver.Status;
        return true;
    }

    /// <summary>
    /// 빙의한 육신의 공격 데이터를 가져옵니다.
    /// 무기별 기본 피해나 속성은 시체의 RootObjectData를 기준으로 계산합니다.
    /// </summary>
    public bool TryGetPossessedDamage(out HWJ_DamageData damage)
    {
        damage = null;

        if (!CanLoadBodyStats() || possessedBodyResolver.Damage == null)
        {
            return false;
        }

        damage = possessedBodyResolver.Damage;
        return true;
    }

    /// <summary>
    /// 빙의한 육신의 피격 데이터를 가져옵니다.
    /// 방어 보정, 무적 여부, 피해 배율도 시체 데이터 기준으로 바꿀 수 있습니다.
    /// </summary>
    public bool TryGetPossessedReceivedDamage(out HWJ_ReceivedDamageData receivedDamage)
    {
        receivedDamage = null;

        if (!CanLoadBodyStats() || possessedBodyResolver.ReceivedDamage == null)
        {
            return false;
        }

        receivedDamage = possessedBodyResolver.ReceivedDamage;
        return true;
    }

    /// <summary>
    /// 빙의한 적/보스가 가진 스킬 세트를 가져옵니다.
    /// 플레이어가 시체의 무기 스킬을 사용할 때 이 데이터를 우선합니다.
    /// </summary>
    public bool TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet)
    {
        skillSet = null;

        if (!HasActivePossessedBody)
        {
            return false;
        }

        if (possessedBodyResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            skillSet = enemyData.SkillCycle;
            return skillSet != null;
        }

        if (possessedBodyResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            skillSet = bossData.SkillCycle;
            return skillSet != null;
        }

        return false;
    }

    /// <summary>
    /// 현재 빙의한 무기 타입으로 사용할 수 있는 첫 번째 스킬 ID를 가져옵니다.
    /// 기본 공격 입력이 들어왔을 때 시체 무기 스킬을 우선 실행하기 위해 사용합니다.
    /// </summary>
    public bool TryGetPrimaryPossessedSkillId(out string skillId)
    {
        skillId = null;

        if (!TryGetPossessedSkillSet(out HWJ_SkillSetData skillSet) || skillSet.skills == null)
        {
            return false;
        }

        HWJ_WeaponType weaponType = CurrentWeaponType;
        string firstMatchedSkillId = null;

        for (int i = 0; i < skillSet.skills.Length; i++)
        {
            HWJ_SkillEntryData skill = skillSet.skills[i];

            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                continue;
            }

            bool weaponMatched = skill.requiredWeaponType == HWJ_WeaponType.None
                || skill.requiredWeaponType == weaponType;

            if (!weaponMatched)
            {
                continue;
            }

            if (string.IsNullOrEmpty(firstMatchedSkillId))
            {
                firstMatchedSkillId = skill.skillId;
            }

            if (skill.startsUnlocked)
            {
                skillId = skill.skillId;
                return true;
            }
        }

        skillId = firstMatchedSkillId;
        return !string.IsNullOrEmpty(skillId);
    }

    /// <summary>
    /// 대상 오브젝트가 적 또는 보스일 때 빙의당하는 몸 데이터를 꺼냅니다.
    /// 적 역할 데이터에서 빙의 불가 몸으로 설정되어 있으면 false를 반환합니다.
    /// </summary>
    private bool TryGetPossessionBodyData(
        HWJ_RootObjectDataResolver targetDataResolver,
        out HWJ_PossessionData possessionBody)
    {
        possessionBody = null;

        if (targetDataResolver.TryGetTypeData(out HWJ_EnemyTypeDataSO enemyData))
        {
            if (enemyData.Role != null
                && (!enemyData.Role.leavesCorpseOnDeath || !enemyData.Role.isPossessableBody))
            {
                return false;
            }

            possessionBody = enemyData.PossessionBody;
            return possessionBody != null;
        }

        if (targetDataResolver.TryGetTypeData(out HWJ_BossTypeDataSO bossData))
        {
            possessionBody = bossData.PossessionBody;
            return possessionBody != null;
        }

        return false;
    }

    private bool CanLoadBodyStats()
    {
        return HasActivePossessedBody
            && activePossessionBodyData != null
            && activePossessionBodyData.loadsBodyStatsToPlayer;
    }

    private bool IsDefeatedIfRequired(
        HWJ_RootObjectDataResolver targetDataResolver,
        HWJ_PossessionData possessionBody)
    {
        if (possessionBody == null || !possessionBody.requiresDefeatedState)
        {
            return true;
        }

        HWJ_RuntimeStatusSystem targetStatus = targetDataResolver.GetComponent<HWJ_RuntimeStatusSystem>();
        return targetStatus != null && targetStatus.IsDead;
    }

    private void TransferOwnerToPossessedBody(HWJ_RootObjectDataResolver targetDataResolver)
    {
        if (targetDataResolver == null)
        {
            return;
        }

        if (moveOwnerToPossessedBody)
        {
            Vector3 targetPosition = targetDataResolver.transform.position;
            targetPosition.z = transform.position.z;
            transform.SetPositionAndRotation(targetPosition, targetDataResolver.transform.rotation);

            if (TryGetComponent(out Rigidbody2D ownerBody))
            {
                ownerBody.linearVelocity = Vector2.zero;
                ownerBody.angularVelocity = 0f;
            }
        }

        if (copyPossessedBodyVisual)
        {
            ApplyPossessedBodyVisual(targetDataResolver.gameObject);
        }

        if (consumePossessedCorpse)
        {
            DisablePossessedCorpseObject(targetDataResolver.gameObject);
        }
    }

    private void CacheOwnerVisual()
    {
        if (ownerSpriteRenderer == null)
        {
            ownerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (ownerSpriteRenderer != null && !hasOwnerSpriteCache)
        {
            ownerOriginalSprite = ownerSpriteRenderer.sprite;
            ownerOriginalColor = ownerSpriteRenderer.color;
            ownerOriginalFlipX = ownerSpriteRenderer.flipX;
            ownerOriginalFlipY = ownerSpriteRenderer.flipY;
            hasOwnerSpriteCache = true;
        }

        if (ownerAnimator == null)
        {
            ownerAnimator = GetComponentInChildren<Animator>();
        }

        if (ownerAnimator != null && !hasOwnerAnimatorCache)
        {
            ownerOriginalAnimatorController = ownerAnimator.runtimeAnimatorController;
            hasOwnerAnimatorCache = true;
        }
    }

    private void ApplyPossessedBodyVisual(GameObject possessedBody)
    {
        CacheOwnerVisual();

        if (possessedBody == null)
        {
            return;
        }

        SpriteRenderer possessedRenderer = possessedBody.GetComponentInChildren<SpriteRenderer>();

        if (ownerSpriteRenderer != null && possessedRenderer != null)
        {
            ownerSpriteRenderer.sprite = possessedRenderer.sprite;
            ownerSpriteRenderer.color = possessedRenderer.color;
            ownerSpriteRenderer.flipX = possessedRenderer.flipX;
            ownerSpriteRenderer.flipY = possessedRenderer.flipY;
        }

        Animator possessedAnimator = possessedBody.GetComponentInChildren<Animator>();

        if (ownerAnimator != null && possessedAnimator != null)
        {
            ownerAnimator.runtimeAnimatorController = possessedAnimator.runtimeAnimatorController;
        }
    }

    private void RestoreOwnerVisual()
    {
        if (ownerSpriteRenderer != null && hasOwnerSpriteCache)
        {
            ownerSpriteRenderer.sprite = ownerOriginalSprite;
            ownerSpriteRenderer.color = ownerOriginalColor;
            ownerSpriteRenderer.flipX = ownerOriginalFlipX;
            ownerSpriteRenderer.flipY = ownerOriginalFlipY;
        }

        if (ownerAnimator != null && hasOwnerAnimatorCache)
        {
            ownerAnimator.runtimeAnimatorController = ownerOriginalAnimatorController;
        }
    }

    private void DisablePossessedCorpseObject(GameObject possessedBody)
    {
        if (possessedBody == null)
        {
            return;
        }

        HWJ_EnemyNavigationSystem navigation = possessedBody.GetComponent<HWJ_EnemyNavigationSystem>();

        if (navigation != null)
        {
            navigation.enabled = false;
        }

        HWJ_MonsterAISystem monsterAI = possessedBody.GetComponent<HWJ_MonsterAISystem>();

        if (monsterAI != null)
        {
            monsterAI.enabled = false;
        }

        Collider2D[] colliders = possessedBody.GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        SpriteRenderer[] renderers = possessedBody.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        Animator animator = possessedBody.GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        Rigidbody2D body = possessedBody.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
    }
}
