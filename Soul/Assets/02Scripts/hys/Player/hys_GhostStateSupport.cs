using UnityEngine;

public class hys_GhostStateSupport : MonoBehaviour
{
    // HWJ 유령 상태 위에 필요한 보조 기능만 얹는 스크립트입니다.
    // 자유 비행, 10초 타이머, Dead 전환은 HWJ_SoulSystem/HWJ_PlayerMovementSystem이 담당합니다.

    [Header("References")]
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D rb;

    [Header("Invincible")]
    // 유령 상태 동안 짧은 무적 시간을 계속 갱신해서 적 공격과 함정 데미지를 무시하게 합니다.
    [SerializeField] private bool grantInvincibleWhileGhost = true;
    [SerializeField] private float invincibleRefreshSeconds = 0.25f;

    [Header("Layer")]
    // Ghost 레이어가 프로젝트에 있으면 유령 상태 동안 레이어를 바꿀 수 있습니다.
    [SerializeField] private bool changeLayerWhileGhost = true;
    [SerializeField] private string ghostLayerName = "Ghost";
    [SerializeField] private bool includeChildren = true;

    [Header("Map Bounds")]
    // 맵 외곽 경계가 필요할 때 인스펙터에서 켜고 min/max 값을 지정합니다.
    [SerializeField] private bool clampToMapBounds;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -20f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 20f);

    [Header("Debug")]
    [SerializeField] private bool isGhostActive;
    [SerializeField] private string lastGhostSupportState;

    private Transform[] cachedTransforms;
    private int[] originalLayers;
    private bool hasLayerBackup;

    private void Awake()
    {
        CacheReferences();
        CacheLayerBackup();
    }

    private void OnDisable()
    {
        RestoreOriginalLayers();
    }

    private void OnDestroy()
    {
        RestoreOriginalLayers();
    }

    private void Update()
    {
        CacheReferences();

        bool shouldBeGhost = IsGhostState();
        if (isGhostActive != shouldBeGhost)
        {
            SetGhostSupportMode(shouldBeGhost);
        }

        if (!shouldBeGhost)
        {
            return;
        }

        RefreshGhostInvincible();
    }

    private void LateUpdate()
    {
        if (isGhostActive && clampToMapBounds)
        {
            ClampPositionToMapBounds();
        }
    }

    private void CacheReferences()
    {
        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
    }

    private void SetGhostSupportMode(bool active)
    {
        isGhostActive = active;

        if (active)
        {
            ApplyGhostLayer();
            lastGhostSupportState = "유령 상태 보조 기능 활성화";
            return;
        }

        RestoreOriginalLayers();
        lastGhostSupportState = "유령 상태 보조 기능 해제";
    }

    private bool IsGhostState()
    {
        return soulSystem != null
            && (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
                || soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul);
    }

    private void RefreshGhostInvincible()
    {
        if (grantInvincibleWhileGhost && runtimeStatus != null)
        {
            runtimeStatus.GrantInvincibility(invincibleRefreshSeconds);
        }
    }

    private void ClampPositionToMapBounds()
    {
        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);

        if (rb != null)
        {
            rb.position = position;
            return;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void CacheLayerBackup()
    {
        cachedTransforms = includeChildren
            ? GetComponentsInChildren<Transform>(true)
            : new[] { transform };

        originalLayers = new int[cachedTransforms.Length];

        for (int i = 0; i < cachedTransforms.Length; i++)
        {
            originalLayers[i] = cachedTransforms[i] != null ? cachedTransforms[i].gameObject.layer : gameObject.layer;
        }

        hasLayerBackup = true;
    }

    private void ApplyGhostLayer()
    {
        if (!changeLayerWhileGhost)
        {
            return;
        }

        int ghostLayer = LayerMask.NameToLayer(ghostLayerName);
        if (ghostLayer < 0)
        {
            lastGhostSupportState = "Ghost 레이어가 없어 레이어 변경을 건너뜀";
            return;
        }

        if (!hasLayerBackup)
        {
            CacheLayerBackup();
        }

        for (int i = 0; i < cachedTransforms.Length; i++)
        {
            if (cachedTransforms[i] != null)
            {
                cachedTransforms[i].gameObject.layer = ghostLayer;
            }
        }
    }

    private void RestoreOriginalLayers()
    {
        if (!hasLayerBackup || cachedTransforms == null || originalLayers == null)
        {
            return;
        }

        for (int i = 0; i < cachedTransforms.Length && i < originalLayers.Length; i++)
        {
            if (cachedTransforms[i] != null)
            {
                cachedTransforms[i].gameObject.layer = originalLayers[i];
            }
        }
    }
}
