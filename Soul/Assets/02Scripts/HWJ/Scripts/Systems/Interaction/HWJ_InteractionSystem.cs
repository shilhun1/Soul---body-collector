using UnityEngine;

/// <summary>
/// 플레이어가 현재 접촉 중인 상호작용 대상을 실행하는 기본 시스템입니다.
/// NPC, 능력치 구슬, 빙의 대상처럼 실제 동작은 대상 컴포넌트에 위임해서 결합도를 낮춥니다.
/// </summary>
public class HWJ_InteractionSystem : MonoBehaviour
{
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_PossessionSystem possessionSystem;

    private GameObject currentTarget;

    private void Awake()
    {
        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (possessionSystem == null)
        {
            possessionSystem = GetComponent<HWJ_PossessionSystem>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
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
        if (currentTarget == null)
        {
            return false;
        }

        HWJ_StatOrbPickupSystem statOrb = currentTarget.GetComponent<HWJ_StatOrbPickupSystem>();

        if (statOrb != null && statOrb.TryCollect(runtimeStatus))
        {
            return true;
        }

        HWJ_NPCInteractionSystem npc = currentTarget.GetComponent<HWJ_NPCInteractionSystem>();

        if (npc != null && npc.TryInteract(gameObject))
        {
            return true;
        }

        HWJ_RootObjectDataResolver targetResolver = currentTarget.GetComponent<HWJ_RootObjectDataResolver>();
        return possessionSystem != null && targetResolver != null && possessionSystem.TryPossess(targetResolver);
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
}
