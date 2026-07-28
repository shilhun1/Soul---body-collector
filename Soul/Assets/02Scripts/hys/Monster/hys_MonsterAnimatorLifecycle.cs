using UnityEngine;

public class hys_MonsterAnimatorLifecycle : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Rigidbody2D body;

    [SerializeField] private int baseLayerIndex = 0;
    [SerializeField] private float patternReturnNormalizedTime = 0.98f;
    [SerializeField] private float patternReturnTransitionSeconds = 0.05f;
    [SerializeField] private float deathFreezeNormalizedTime = 0.995f;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    public void Initialize(Animator animatorRef, HWJ_RuntimeStatusSystem statusRef)
    {
        animator = animatorRef;
        runtimeStatus = statusRef;
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
            if (runtimeStatus == null)
            {
                runtimeStatus = GetComponentInParent<HWJ_RuntimeStatusSystem>();
            }
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = GetComponentInParent<Rigidbody2D>();
            }
        }
    }
}
