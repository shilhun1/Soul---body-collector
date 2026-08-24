using UnityEngine;

// 이 HYS 보스방에 들어온 실제 플레이어의 네모 몸 콜라이더를 원형 몸 콜라이더로 교체합니다.
[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class hys_MidBoss2PlayerCircleColliderAdapter : MonoBehaviour
{
    [SerializeField] private Vector2 circleOffset = new Vector2(-0.08f, -0.88f);
    [SerializeField, Min(0.1f)] private float circleRadius = 0.9f;
    [SerializeField, Min(0.05f)] private float refreshSeconds = 0.25f;
    [SerializeField] private GameObject configuredPlayer;
    [SerializeField] private int configureCount;

    private float nextRefreshTime;

    public GameObject ConfiguredPlayer => configuredPlayer;
    public int ConfigureCount => configureCount;
    public float CircleRadius => circleRadius;

    private void Awake()
    {
        RefreshPlayerCollider();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshSeconds);
        RefreshPlayerCollider();
    }

    public bool RefreshPlayerCollider()
    {
        GameObject playerRoot = ResolvePlayerBodyRoot();
        if (playerRoot == null) return false;

        bool changed = configuredPlayer != playerRoot;
        BoxCollider2D[] boxes = playerRoot.GetComponents<BoxCollider2D>();
        for (int i = 0; i < boxes.Length; i++)
        {
            BoxCollider2D box = boxes[i];
            if (box == null || box.isTrigger) continue;
            box.enabled = false;
            Destroy(box);
            changed = true;
        }

        CircleCollider2D circle = playerRoot.GetComponent<CircleCollider2D>();
        if (circle == null)
        {
            circle = playerRoot.AddComponent<CircleCollider2D>();
            changed = true;
        }

        // 같은 콜라이더 값을 주기적으로 다시 쓰면 바닥 접촉 형상이 재생성되어 몸체가 튈 수 있습니다.
        // 실제 값이 다를 때만 변경해 타일 바닥 위의 물리 접촉을 안정적으로 유지합니다.
        if (circle.isTrigger)
        {
            circle.isTrigger = false;
            changed = true;
        }

        if ((circle.offset - circleOffset).sqrMagnitude > 0.000001f)
        {
            circle.offset = circleOffset;
            changed = true;
        }

        float targetRadius = Mathf.Max(0.1f, circleRadius);
        if (Mathf.Abs(circle.radius - targetRadius) > 0.0001f)
        {
            circle.radius = targetRadius;
            changed = true;
        }

        if (!circle.enabled)
        {
            circle.enabled = true;
            changed = true;
        }

        configuredPlayer = playerRoot;
        if (changed) configureCount++;
        return true;
    }

    private static GameObject ResolvePlayerBodyRoot()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
            return ResolveBodyObject(HWJ_GameAccess.Manager.PlayerResolver.transform);

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
                return ResolveBodyObject(resolvers[i].transform);
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        return taggedPlayer != null ? ResolveBodyObject(taggedPlayer.transform) : null;
    }

    private static GameObject ResolveBodyObject(Transform playerTransform)
    {
        if (playerTransform == null) return null;
        Rigidbody2D body = playerTransform.GetComponentInParent<Rigidbody2D>();
        return body != null ? body.gameObject : playerTransform.gameObject;
    }
}
