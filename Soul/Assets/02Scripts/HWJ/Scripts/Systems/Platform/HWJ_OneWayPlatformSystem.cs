using UnityEngine;

public class HWJ_OneWayPlatformSystem : MonoBehaviour
{
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private PlatformEffector2D platformEffector;
    [SerializeField] private bool configurePlatformEffector = true;
    [SerializeField] private bool allowJumpThroughFromBelow;
    [SerializeField] private float surfaceArc = 160f;

    private void Awake()
    {
        CacheReferences();
        ApplyEffectorSettings();
    }

    private void OnValidate()
    {
        CacheReferences();
        ApplyEffectorSettings();
    }

    private void CacheReferences()
    {
        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        if (platformEffector == null)
        {
            platformEffector = GetComponent<PlatformEffector2D>();
        }
    }

    private void ApplyEffectorSettings()
    {
        if (!configurePlatformEffector)
        {
            return;
        }

        if (!allowJumpThroughFromBelow)
        {
            if (platformCollider != null)
            {
                platformCollider.usedByEffector = false;
            }

            if (platformEffector != null)
            {
                platformEffector.useOneWay = false;
            }

            return;
        }

        if (platformCollider == null)
        {
            platformCollider = GetComponent<Collider2D>();
        }

        if (platformCollider != null)
        {
            platformCollider.usedByEffector = true;
        }

        if (platformEffector == null)
        {
            platformEffector = GetComponent<PlatformEffector2D>();
        }

        if (platformEffector == null)
        {
            platformEffector = gameObject.AddComponent<PlatformEffector2D>();
        }

        platformEffector.useOneWay = true;
        platformEffector.surfaceArc = Mathf.Clamp(surfaceArc, 1f, 360f);
    }
}
