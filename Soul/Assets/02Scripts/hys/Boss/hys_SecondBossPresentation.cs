using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

// 중간보스2의 인트로, 대사, 피격, 페이즈 전환과 Cinemachine 연출을 한곳에서 관리합니다.
[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
public class hys_SecondBossPresentation : MonoBehaviour, HWJ_IDamageAbsorber
{
    [Header("참조")]
    [SerializeField] private hys_SecondBossLogic bossLogic;
    [SerializeField] private hys_SecondBossPattern patternSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private SpriteRenderer bossRenderer;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private HWJ_BossDialogueBubbleSystem dialogueSystem;
    [SerializeField] private HWJ_BossCameraFocusSystem cameraFocusSystem;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private TextMesh bossNameText;
    [SerializeField] private hys_SecondBossPhaseTransitionVisual phaseTransitionVisual;

    [Header("보스 표시")]
    [SerializeField] private string bossDisplayName = "붉은 기사단장 바르칸";
    [SerializeField] private bool autoPlayIntro = true;
    [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoHpRatio = 0.5f;
    [SerializeField, Min(0.1f)] private float fallbackIntroSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float fallbackPhaseSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float fallbackDeathSeconds = 3f;

    [Header("피격 및 카메라")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color phasePulseColor = new Color(0.65f, 0.18f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float hitFlashSeconds = 0.12f;
    [SerializeField, Min(0f)] private float hitShakeForce = 0.22f;
    [SerializeField, Min(0f)] private float phaseShakeForce = 0.7f;
    [SerializeField, Min(0f)] private float deathShakeForce = 0.45f;

    [Header("단발 대사")]
    [SerializeField] private string playerDefeatedDialogue = "그 정도 각오로 내 앞에 선 것이냐.";
    [SerializeField, Min(0.1f)] private float playerDefeatedDialogueSeconds = 3f;

    [Header("상태 확인")]
    [SerializeField] private bool introPlayed;
    [SerializeField] private bool phaseTwoTriggered;
    [SerializeField] private bool deathPresentationStarted;
    [SerializeField] private bool playerDefeatLineShown;
    [SerializeField] private bool presentationBlocking;
    [SerializeField] private bool waitingForPlayerBody;
    [SerializeField] private int introPlayCount;
    [SerializeField] private int phaseTransitionPlayCount;
    [SerializeField] private int deathPlayCount;
    [SerializeField] private int hitFlashCount;
    [SerializeField] private int impulseCount;
    [SerializeField] private int phaseVisualBurstCount;

    private Coroutine presentationRoutine;
    private Coroutine hitFlashRoutine;
    private Color normalColor = Color.white;
    private float previousHp = float.NaN;
    private bool patternEventSubscribed;

    public bool IntroPlayed => introPlayed;
    public bool PhaseTwoTriggered => phaseTwoTriggered;
    public bool DeathPresentationStarted => deathPresentationStarted;
    public bool IsPresentationBlocking => presentationBlocking;
    public string BossDisplayName => bossDisplayName;
    public int IntroPlayCount => introPlayCount;
    public int PhaseTransitionPlayCount => phaseTransitionPlayCount;
    public int DeathPlayCount => deathPlayCount;
    public int HitFlashCount => hitFlashCount;
    public int ImpulseCount => impulseCount;
    public bool PlayerDefeatLineShown => playerDefeatLineShown;
    public bool WaitingForPlayerBody => waitingForPlayerBody;
    public int PhaseVisualBurstCount => phaseVisualBurstCount;

    private void Awake()
    {
        CacheReferences();
        if (bossRenderer != null) normalColor = bossRenderer.color;
        if (autoPlayIntro) SetCombatBlocked(true);
    }

    private void OnEnable()
    {
        CacheReferences();
        SubscribePatternImpact();
    }

    private IEnumerator Start()
    {
        CacheReferences();
        previousHp = runtimeStatus != null ? runtimeStatus.CurrentHp : float.NaN;
        yield return null;

        // 유령 또는 육체 전환 중에는 카메라와 대사를 띄우지 않고 육체 복귀를 기다립니다.
        while (autoPlayIntro && !introPlayed && !IsPlayerInBodyState())
        {
            waitingForPlayerBody = true;
            cameraFocusSystem?.StopFocus();
            dialogueSystem?.CancelDialogueSequence(true);
            yield return null;
        }

        waitingForPlayerBody = false;
        if (autoPlayIntro && !introPlayed)
        {
            presentationRoutine = StartCoroutine(PlayIntroRoutine());
        }
    }

    private void Update()
    {
        CacheReferences();
        SubscribePatternImpact();
        UpdateBossName();
        UpdateHitPresentation();
        UpdatePhaseTransition();
        UpdateDeathPresentation();
        UpdatePlayerDefeatedDialogue();
    }

    private void LateUpdate()
    {
        // 기존 체력바가 이름을 갱신한 뒤 HYS 보스 이름을 최종 적용합니다.
        UpdateBossName();
    }

    private void OnDisable()
    {
        UnsubscribePatternImpact();
        cameraFocusSystem?.StopFocus();
        RestoreBossColor();
    }

    public void Initialize(
        hys_SecondBossLogic logic,
        hys_SecondBossPattern patterns,
        HWJ_RuntimeStatusSystem status,
        SpriteRenderer renderer,
        Rigidbody2D targetBody,
        HWJ_BossDialogueBubbleSystem dialogue,
        HWJ_BossCameraFocusSystem cameraFocus,
        CinemachineImpulseSource impulse,
        TextMesh nameText)
    {
        UnsubscribePatternImpact();
        bossLogic = logic;
        patternSystem = patterns;
        runtimeStatus = status;
        bossRenderer = renderer;
        body = targetBody;
        dialogueSystem = dialogue;
        cameraFocusSystem = cameraFocus;
        impulseSource = impulse;
        bossNameText = nameText;
        if (bossRenderer != null) normalColor = bossRenderer.color;
        SubscribePatternImpact();
        UpdateBossName();
    }

    public bool TryAbsorbDamage(
        float incomingDamage,
        Component source,
        HWJ_DamageData sourceDamage,
        out float remainingDamage)
    {
        if (!presentationBlocking)
        {
            remainingDamage = incomingDamage;
            return false;
        }

        // 인트로와 페이즈 전환 중에는 연출이 끊기지 않도록 피해를 흡수합니다.
        remainingDamage = 0f;
        return true;
    }

    private IEnumerator PlayIntroRoutine()
    {
        waitingForPlayerBody = false;
        introPlayed = true;
        introPlayCount++;
        SetCombatBlocked(true);
        float duration = ResolveDialogueDuration(
            HWJ_BossDialogueSequenceType.Intro,
            fallbackIntroSeconds);
        dialogueSystem?.ShowIntroDialogue();
        cameraFocusSystem?.FocusOnBoss(transform, ResolvePlayerTransform(), duration);
        GenerateImpulse(phaseShakeForce * 0.45f);
        yield return new WaitForSeconds(duration);
        SetCombatBlocked(false);
        presentationRoutine = null;
    }

    private IEnumerator PlayPhaseTwoRoutine()
    {
        phaseTwoTriggered = true;
        phaseTransitionPlayCount++;
        SetCombatBlocked(true);
        float duration = ResolveDialogueDuration(
            HWJ_BossDialogueSequenceType.PhaseTransition,
            fallbackPhaseSeconds);
        dialogueSystem?.ShowPhaseTwoDialogue();
        cameraFocusSystem?.FocusOnBoss(transform, ResolvePlayerTransform(), duration);
        GenerateImpulse(phaseShakeForce);

        // 금지된 흑마법이 깨어나는 순간을 마력진, 검의 포위와 연속 충격파로 단계적으로 보여줍니다.
        SpawnPhaseTransitionOpening(duration);
        phaseTransitionVisual?.Play(duration, phasePulseColor);

        bool firstBurstPlayed = false;
        bool secondBurstPlayed = false;
        bool thirdBurstPlayed = false;
        bool finalBurstPlayed = false;
        float endTime = Time.time + duration;
        float startTime = Time.time;
        while (Time.time < endTime)
        {
            float progress = Mathf.InverseLerp(startTime, endTime, Time.time);
            if (bossRenderer != null)
            {
                float pulse = Mathf.PingPong(Time.time * Mathf.Lerp(4f, 11f, progress), 1f);
                Color awakenedColor = progress > 0.72f ? Color.white : phasePulseColor;
                bossRenderer.color = Color.Lerp(normalColor, awakenedColor, pulse);
            }

            if (!firstBurstPlayed && progress >= 0.18f)
            {
                firstBurstPlayed = true;
                SpawnPhaseTransitionBurst(0.55f, 4.8f, 1.35f);
            }

            if (!secondBurstPlayed && progress >= 0.42f)
            {
                secondBurstPlayed = true;
                SpawnPhaseTransitionBurst(0.8f, 6.8f, 1.9f);
            }

            if (!thirdBurstPlayed && progress >= 0.66f)
            {
                thirdBurstPlayed = true;
                SpawnPhaseTransitionBurst(1.05f, 8.8f, 2.45f);
            }

            if (!finalBurstPlayed && progress >= 0.86f)
            {
                finalBurstPlayed = true;
                SpawnPhaseTransitionBurst(1.35f, 11.2f, 3.1f);
            }
            yield return null;
        }

        RestoreBossColor();
        SetCombatBlocked(false);
        presentationRoutine = null;
    }

    private void SpawnPhaseTransitionOpening(float duration)
    {
        phaseVisualBurstCount++;
        Color ringColor = new Color(0.58f, 0.12f, 1f, 0.92f);
        hys_SecondBossMagicVisual.SpawnTrackingMark(
            transform,
            Mathf.Max(1f, duration),
            2.35f,
            ringColor);
        hys_SkillWarningIndicator.ShowCircle(
            transform.position,
            2.6f,
            Mathf.Max(0.8f, duration * 0.42f),
            ringColor,
            0.11f);

        const int swordCount = 10;
        for (int i = 0; i < swordCount; i++)
        {
            float angle = i / (float)swordCount * Mathf.PI * 2f;
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector3 spawnPosition = transform.position + (Vector3)(radial * 3.7f);
            hys_SecondBossMagicVisual.SpawnSword(
                spawnPosition,
                -radial,
                Mathf.Max(1f, duration * 0.88f),
                1.55f,
                0.13f,
                0.34f,
                ringColor);
        }
    }

    private void SpawnPhaseTransitionBurst(float force, float width, float height)
    {
        phaseVisualBurstCount++;
        Color burstColor = new Color(0.82f, 0.28f, 1f, 0.95f);
        hys_SecondBossMagicVisual.SpawnShockwave(
            transform.position + Vector3.down * 0.12f,
            0.7f,
            width,
            height,
            burstColor);
        hys_SkillWarningIndicator.ShowCircle(
            transform.position,
            width * 0.42f,
            0.65f,
            burstColor,
            0.14f);
        GenerateImpulse(Mathf.Max(phaseShakeForce, force));
    }

    private IEnumerator PlayDeathRoutine()
    {
        deathPresentationStarted = true;
        deathPlayCount++;
        SetCombatBlocked(true);
        float duration = ResolveDialogueDuration(
            HWJ_BossDialogueSequenceType.Death,
            fallbackDeathSeconds);
        dialogueSystem?.ShowDeathDialogue();
        cameraFocusSystem?.FocusOnBoss(transform, ResolvePlayerTransform(), duration);
        GenerateImpulse(deathShakeForce);
        yield return new WaitForSeconds(duration);
        RestoreBossColor();
        cameraFocusSystem?.StopFocus();
        presentationRoutine = null;
    }

    private void UpdateHitPresentation()
    {
        if (runtimeStatus == null) return;
        float currentHp = runtimeStatus.CurrentHp;
        if (float.IsNaN(previousHp))
        {
            previousHp = currentHp;
            return;
        }

        if (!presentationBlocking && currentHp < previousHp - 0.001f)
        {
            hitFlashCount++;
            if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = StartCoroutine(FlashHitRoutine());
            GenerateImpulse(hitShakeForce);
        }
        previousHp = currentHp;
    }

    private IEnumerator FlashHitRoutine()
    {
        if (bossRenderer != null) bossRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(Mathf.Max(0.01f, hitFlashSeconds));
        RestoreBossColor();
        hitFlashRoutine = null;
    }

    private void UpdatePhaseTransition()
    {
        if (!introPlayed || presentationBlocking || phaseTwoTriggered || runtimeStatus == null) return;
        if (runtimeStatus.IsDead || runtimeStatus.MaxHp <= 0f) return;
        if (runtimeStatus.CurrentHp / runtimeStatus.MaxHp > phaseTwoHpRatio) return;

        if (presentationRoutine != null) StopCoroutine(presentationRoutine);
        presentationRoutine = StartCoroutine(PlayPhaseTwoRoutine());
    }

    private void UpdateDeathPresentation()
    {
        if (deathPresentationStarted || runtimeStatus == null || !runtimeStatus.IsDead) return;
        if (presentationRoutine != null) StopCoroutine(presentationRoutine);
        presentationRoutine = StartCoroutine(PlayDeathRoutine());
    }

    private void UpdatePlayerDefeatedDialogue()
    {
        if (playerDefeatLineShown || presentationBlocking || dialogueSystem == null) return;
        Transform player = ResolvePlayerTransform();
        HWJ_RuntimeStatusSystem playerStatus = player != null
            ? player.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;
        if (playerStatus == null || !playerStatus.IsDead) return;

        playerDefeatLineShown = true;
        dialogueSystem.ShowLine(playerDefeatedDialogue, playerDefeatedDialogueSeconds);
    }

    private void SetCombatBlocked(bool blocked)
    {
        presentationBlocking = blocked;
        if (blocked)
        {
            patternSystem?.CancelActivePattern();
            if (bossLogic != null)
            {
                bossLogic.StopEncounter();
                bossLogic.enabled = false;
            }
            if (body != null) body.linearVelocity = Vector2.zero;
            return;
        }

        if (runtimeStatus != null && runtimeStatus.IsDead) return;
        if (bossLogic != null)
        {
            bossLogic.enabled = true;
            bossLogic.StartEncounter();
        }
    }

    private void HandlePatternImpact(float force)
    {
        if (!presentationBlocking) GenerateImpulse(force);
    }

    private void GenerateImpulse(float force)
    {
        if (impulseSource != null && force > 0f)
        {
            impulseCount++;
            impulseSource.GenerateImpulseWithForce(force);
        }
    }

    private float ResolveDialogueDuration(
        HWJ_BossDialogueSequenceType sequenceType,
        float fallbackDuration)
    {
        float duration = dialogueSystem != null
            ? dialogueSystem.GetSequenceDuration(sequenceType)
            : 0f;
        return duration > 0f ? duration : Mathf.Max(0.1f, fallbackDuration);
    }

    private void UpdateBossName()
    {
        if (bossNameText != null && bossNameText.text != bossDisplayName)
            bossNameText.text = bossDisplayName;
    }

    private void RestoreBossColor()
    {
        if (bossRenderer != null) bossRenderer.color = normalColor;
    }

    private void SubscribePatternImpact()
    {
        if (patternEventSubscribed || patternSystem == null) return;
        patternSystem.ImpactRequested += HandlePatternImpact;
        patternEventSubscribed = true;
    }

    private void UnsubscribePatternImpact()
    {
        if (!patternEventSubscribed || patternSystem == null) return;
        patternSystem.ImpactRequested -= HandlePatternImpact;
        patternEventSubscribed = false;
    }

    private Transform ResolvePlayerTransform()
    {
        if (HWJ_GameAccess.HasManager && HWJ_GameAccess.Manager.PlayerResolver != null)
            return HWJ_GameAccess.Manager.PlayerResolver.transform;

        HWJ_RootObjectDataResolver[] resolvers = FindObjectsByType<HWJ_RootObjectDataResolver>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] != null && resolvers[i].ObjectType == HWJ_ObjectType.Player)
                return resolvers[i].transform;
        }
        return null;
    }

    private bool IsPlayerInBodyState()
    {
        Transform player = ResolvePlayerTransform();
        if (player == null) return false;

        HWJ_SoulSystem soulSystem = player.GetComponentInParent<HWJ_SoulSystem>();
        return soulSystem == null
            || (soulSystem.CurrentState == HWJ_SoulRuntimeState.Body
                && soulSystem.IsPossessedExistence);
    }

    private void CacheReferences()
    {
        if (bossLogic == null) bossLogic = GetComponent<hys_SecondBossLogic>();
        if (patternSystem == null) patternSystem = GetComponent<hys_SecondBossPattern>();
        if (runtimeStatus == null) runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        if (bossRenderer == null)
        {
            Transform visual = transform.Find("hys_MidBoss2_Visual");
            bossRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        }
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (dialogueSystem == null) dialogueSystem = GetComponent<HWJ_BossDialogueBubbleSystem>();
        if (cameraFocusSystem == null) cameraFocusSystem = GetComponent<HWJ_BossCameraFocusSystem>();
        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
        if (phaseTransitionVisual == null)
            phaseTransitionVisual = GetComponent<hys_SecondBossPhaseTransitionVisual>();
        if (bossNameText == null)
        {
            TextMesh[] texts = GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "BossName")
                {
                    bossNameText = texts[i];
                    break;
                }
            }
        }
    }
}
