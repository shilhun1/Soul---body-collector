using UnityEngine;

public class hys_HWJEnemyPatternAnimatorBridge : MonoBehaviour
{
    [SerializeField] private Component enemyAttackSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    public void Initialize(Component attackSystem, Animator animatorRef)
    {
        enemyAttackSystem = attackSystem;
        animator = animatorRef;
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
    }
}
