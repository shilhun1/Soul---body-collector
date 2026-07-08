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
    [SerializeField] private bool useDebugLog = true;

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
        if (!WasPossessionKeyPressed())
        {
            return;
        }

        Log("F key pressed. Checking possession animation conditions.");

        if (isPlayingPossessionAnimation)
        {
            Log("Ignored: possession animation is already playing.");
            return;
        }

        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Soul)
        {
            Log($"Ignored: current soul state is {soulSystem.CurrentState}, not Soul.");
            return;
        }

        Log("Starting possession animation routine.");
        StartCoroutine(PlayPossessionAnimationRoutine());
    }

    private IEnumerator PlayPossessionAnimationRoutine()
    {
        isPlayingPossessionAnimation = true;

        if (soulAnimation != null)
        {
            soulAnimation.enabled = false;
            Log("Disabled hys_Soul_anim while possession animation plays.");
        }

        if (animator != null)
        {
            animator.SetTrigger(possessHash);
            Log($"Animator trigger sent: {possessTrigger}");
        }
        else
        {
            Log("Animator is missing, so trigger could not be sent.");
        }

        yield return new WaitForSeconds(Mathf.Max(0f, possessionAnimationSeconds));
        Log($"Possession animation wait finished: {possessionAnimationSeconds:0.###} seconds.");

        if (interactAfterAnimation && interactionSystem != null)
        {
            Log("Calling HWJ_InteractionSystem.TryInteract().");
            interactionSystem.TryInteract();
        }
        else if (interactAfterAnimation)
        {
            Log("InteractionSystem is missing, so TryInteract could not be called.");
        }

        if (soulAnimation != null && (soulSystem == null || soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul))
        {
            soulAnimation.enabled = true;
            Log("Re-enabled hys_Soul_anim.");
        }

        Log("Possession animation routine ended.");
        isPlayingPossessionAnimation = false;
    }

    private void Log(string message)
    {
        if (useDebugLog)
        {
            Debug.Log($"[hys_Soul_PossessionAnim] {message}", this);
        }
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
