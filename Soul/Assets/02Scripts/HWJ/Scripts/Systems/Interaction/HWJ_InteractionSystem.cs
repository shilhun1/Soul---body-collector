using UnityEngine;

/// <summary>
/// 플레이어가 현재 접촉 중인 상호작용 대상을 실행하는 기본 시스템입니다.
/// NPC, 능력치 구슬, 빙의 대상처럼 실제 동작은 대상 컴포넌트에 위임해서 결합도를 낮춥니다.
/// </summary>
public class HWJ_InteractionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RootObjectDataResolver dataResolver;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;
    [SerializeField] private HWJ_PlayerInputSystem playerInput;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private LayerMask interactionTargetLayer;
    [SerializeField] private string lastInteractionResult;

    private GameObject currentTarget;
    private readonly Collider2D[] interactionHits = new Collider2D[16];

    private void Awake()
    {
        if (dataResolver == null)
        {
            dataResolver = GetComponent<HWJ_RootObjectDataResolver>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<HWJ_PlayerInputSystem>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }
    }

    private void Update()
    {
        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            return;
        }

        if (playerInput != null && playerInput.InteractPressedThisFrame)
        {
            TryInteract();
        }
    }

    /// <summary>
    /// 현재 대상과 상호작용합니다.
    /// 우선순위는 능력치 구슬, NPC, 빙의 대상 순서입니다.
    /// </summary>
    public bool TryInteract()
    {
        if (soulSystem != null && soulSystem.IsControlLocked)
        {
            lastInteractionResult = "Interaction failed: control is locked.";
            return false;
        }

        if (TryInteractWith(currentTarget))
        {
            return true;
        }

        if (TryInteractNearby())
        {
            return true;
        }

        lastInteractionResult = "Interaction failed: no valid target in range.";
        return false;
    }

    private bool TryInteractWith(GameObject target)
    {
        GameObject targetObject = ResolveInteractionTarget(target);

        if (targetObject == null)
        {
            return false;
        }

        HWJ_StatOrbPickupSystem statOrb = targetObject.GetComponent<HWJ_StatOrbPickupSystem>();

        if (statOrb != null && statOrb.TryCollect(runtimeStatus))
        {
            lastInteractionResult = $"Collected {targetObject.name}.";
            return true;
        }

        HWJ_NPCInteractionSystem npc = targetObject.GetComponent<HWJ_NPCInteractionSystem>();

        if (npc != null && npc.TryInteract(gameObject))
        {
            lastInteractionResult = $"Interacted with {targetObject.name}.";
            return true;
        }

        HWJ_RootObjectDataResolver targetResolver = targetObject.GetComponent<HWJ_RootObjectDataResolver>();

        if (possessionSystem != null && targetResolver != null && possessionSystem.TryPossess(targetResolver))
        {
            lastInteractionResult = $"Possessed {targetObject.name}.";
            return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        currentTarget = other.gameObject;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentTarget == other.gameObject)
        {
            currentTarget = null;
        }
    }

    private GameObject FindBestInteractionTarget()
    {
        float range = GetInteractionRange();
        int layerMask = interactionTargetLayer.value != 0 ? interactionTargetLayer.value : Physics2D.AllLayers;
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, range, interactionHits, layerMask);
        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = interactionHits[i];

            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            GameObject target = ResolveInteractionTarget(hit.gameObject);

            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance < bestDistance)
            {
                bestTarget = target;
                bestDistance = distance;
            }
        }

        return bestTarget;
    }

    private bool TryInteractNearby()
    {
        float range = GetInteractionRange();
        int layerMask = interactionTargetLayer.value != 0 ? interactionTargetLayer.value : Physics2D.AllLayers;
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, range, interactionHits, layerMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = interactionHits[i];

            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (TryInteractWith(hit.gameObject))
            {
                currentTarget = ResolveInteractionTarget(hit.gameObject);
                return true;
            }
        }

        return false;
    }

    private GameObject ResolveInteractionTarget(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        HWJ_RootObjectDataResolver resolver = target.GetComponentInParent<HWJ_RootObjectDataResolver>();

        if (resolver != null)
        {
            return resolver.gameObject;
        }

        return target;
    }

    private float GetInteractionRange()
    {
        float range = dataResolver != null && dataResolver.Interaction != null
            ? dataResolver.Interaction.interactionRange
            : 0f;

        if (dataResolver != null
            && dataResolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData)
            && playerData.Possession != null
            && playerData.Possession.possessionRange > range)
        {
            range = playerData.Possession.possessionRange;
        }

        return Mathf.Max(1.5f, range);
    }
}
