using System.Collections;
using UnityEngine;

/// <summary>
/// One-shot attack effects use this to reset their Animator when spawned and return to the pool after playback.
/// This keeps slash effects reusable through HWJ_ObjectPoolSystem instead of leaving spawned objects in the scene.
/// </summary>
public class HWJ_EffectAutoReturnSystem : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private HWJ_PoolableObject poolableObject;
    [SerializeField] private float lifetimeSeconds = 0.65f;

    private Coroutine lifetimeRoutine;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
        }

        lifetimeRoutine = StartCoroutine(ReturnAfterLifetime());
    }

    private void OnDisable()
    {
        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }
    }

    public void SetLifetime(float lifetimeSeconds)
    {
        this.lifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
    }

    private IEnumerator ReturnAfterLifetime()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, lifetimeSeconds));

        if (poolableObject != null)
        {
            poolableObject.ReturnToPool();
            yield break;
        }

        gameObject.SetActive(false);
    }

    private void CacheReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (poolableObject == null)
        {
            poolableObject = GetComponent<HWJ_PoolableObject>();
        }
    }
}
