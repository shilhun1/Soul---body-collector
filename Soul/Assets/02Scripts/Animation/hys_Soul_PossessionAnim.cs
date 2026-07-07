using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class hys_Soul_PossessionAnim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private hys_Soul_anim soulAnimation;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_InteractionSystem interactionSystem;

    [Header("Animation")]
    [SerializeField] private string possessTrigger = "Possess";
    [SerializeField] private float possessionAnimationSeconds = 0.5f;
    [SerializeField] private bool interactAfterAnimation = true;

    private bool isPlayingPossessionAnimation;
    private int possessHash;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (soulAnimation == null)
        {
            soulAnimation = GetComponent<hys_Soul_anim>();
        }

        if (soulSystem == null)
        {
            soulSystem = GetComponent<HWJ_SoulSystem>();
        }

        if (interactionSystem == null)
        {
            interactionSystem = GetComponent<HWJ_InteractionSystem>();
        }

        possessHash = Animator.StringToHash(possessTrigger);
    }

    private void Update()
    {
        if (!WasPossessionKeyPressed() || isPlayingPossessionAnimation)
        {
            return;
        }

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Soul)
        {
            return;
        }

        StartCoroutine(PlayPossessionAnimationRoutine());
    }

    private IEnumerator PlayPossessionAnimationRoutine()
    {
        isPlayingPossessionAnimation = true;

        if (soulAnimation != null)
        {
            soulAnimation.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger(possessHash);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, possessionAnimationSeconds));

        if (interactAfterAnimation && interactionSystem != null)
        {
            interactionSystem.TryInteract();
        }

        if (soulAnimation != null && (soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul))
        {
            soulAnimation.enabled = true;
        }

        isPlayingPossessionAnimation = false;
    }

    private static bool WasPossessionKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
