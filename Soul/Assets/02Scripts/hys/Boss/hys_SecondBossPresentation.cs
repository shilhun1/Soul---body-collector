using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private hys_SecondBossCinematicOverlay cinematicOverlay;

    [Header("보스 표시")]
    [SerializeField] private string bossDisplayName = "붉은 기사단장 바르칸";
    [SerializeField, Range(1f, 1.8f)] private float bossVisualScaleMultiplier = 1.28f;
    [SerializeField] private bool autoPlayIntro = true;
    [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoHpRatio = 0.5f;
    [SerializeField, Min(0.1f)] private float fallbackIntroSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float fallbackPhaseSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float fallbackDeathSeconds = 3f;

    [Header("애니메이터 없는 인트로")]
    [SerializeField] private bool useInteractiveIntro = true;
    [SerializeField, Range(1, 13)] private int cinematicIntroLineCount = 6;
    [SerializeField, Min(0.3f)] private float introLineAutoAdvanceSeconds = 2.15f;
    [SerializeField, Min(0.05f)] private float minimumLineInputSeconds = 0.16f;
    [SerializeField, Min(0.2f)] private float introSkipHoldSeconds = 0.7f;
    [SerializeField, Min(0.1f)] private float introTitleSeconds = 1.15f;
    [SerializeField, Min(0.08f)] private float releaseFlashSeconds = 0.32f;
    [SerializeField] private string bossEpithet = "왕명을 집행하는 마지막 칼";
    [SerializeField] private Color royalSealColor = new Color(1f, 0.58f, 0.12f, 0.95f);

    [Header("보스 낙하 인트로")]
    [SerializeField] private bool useBossDropIntro = true;
    [SerializeField, Min(1f)] private float introDropHeight = 7f;
    [SerializeField, Min(0.1f)] private float introDropSeconds = 0.72f;
    [SerializeField, Min(0f)] private float introDropSettleSeconds = 0.15f;
    [SerializeField, Min(0f)] private float introLandingShakeForce = 1.15f;
    [SerializeField, Min(0.05f)] private float introLandingRecoilSeconds = 0.24f;
    [SerializeField] private Color introDropSilhouetteColor = new Color(0.08f, 0.04f, 0.03f, 1f);

    [Header("공통 대사 조작 및 전투 연결")]
    [SerializeField] private bool useInteractivePhaseDialogue = true;
    [SerializeField] private bool useInteractiveDeathDialogue = true;
    [SerializeField, Range(1, 13)] private int phaseDialogueLineCount = 13;
    [SerializeField, Range(1, 13)] private int deathDialogueLineCount = 13;
    [SerializeField, Min(0.1f)] private float combatStartSafetySeconds = 0.7f;

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

    [Header("임시 테스트 강화")]
    [SerializeField] private bool enableTemporaryPlayerTestBoost = true;
    [SerializeField, Min(0f)] private float temporaryPlayerHpBonus = 1000000f;
    [SerializeField, Min(0f)] private float temporaryPlayerAttackBonus = 1000000f;

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
    [SerializeField] private bool interactiveIntroPlaying;
    [SerializeField] private bool playerControlLocked;
    [SerializeField] private int currentInteractiveLineIndex = -1;
    [SerializeField] private int dialogueAdvanceCount;
    [SerializeField] private int introSkipCount;
    [SerializeField] private int dialogueSkipCount;
    [SerializeField] private bool interactiveDialoguePlaying;
    [SerializeField] private HWJ_BossDialogueSequenceType activeInteractiveSequence;
    [SerializeField] private bool combatStartGraceActive;
    [SerializeField] private bool introSuppressedByReentry;
    [SerializeField] private int playerDeathAbortCount;
    [SerializeField] private bool introDropCompleted;
    [SerializeField] private int introLandingCount;
    [SerializeField] private int introLandingSparkCount;

    private Coroutine presentationRoutine;
    private Coroutine hitFlashRoutine;
    private Color normalColor = Color.white;
    private float previousHp = float.NaN;
    private bool patternEventSubscribed;
    private hys_PlayerCinematicControlLock playerControlLock;
    private float skipHoldProgress;
    private bool introAdvanceRequested;
    private bool introSkipRequested;
    private Coroutine phaseEffectsRoutine;
    private bool phaseFinalBurstPlayed;
    private hys_MidBoss2TemporaryPlayerBoost temporaryPlayerBoost;
    private bool bossDropInProgress;
    private Vector3 bossDropLandingPosition;
    private bool bossDropBodyWasSimulated;
    private bool bossVisualScaleApplied;

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
    public bool InteractiveIntroPlaying => interactiveIntroPlaying;
    public bool IsPlayerControlLocked => playerControlLocked;
    public int CurrentInteractiveLineIndex => currentInteractiveLineIndex;
    public int DialogueAdvanceCount => dialogueAdvanceCount;
    public int IntroSkipCount => introSkipCount;
    public int DialogueSkipCount => dialogueSkipCount;
    public bool InteractiveDialoguePlaying => interactiveDialoguePlaying;
    public HWJ_BossDialogueSequenceType ActiveInteractiveSequence => activeInteractiveSequence;
    public bool IsCombatStartGraceActive => combatStartGraceActive;
    public bool IntroSuppressedByReentry => introSuppressedByReentry;
    public int PlayerDeathAbortCount => playerDeathAbortCount;
    public bool IntroDropCompleted => introDropCompleted;
    public int IntroLandingCount => introLandingCount;
    public int IntroLandingSparkCount => introLandingSparkCount;
    public bool HasTemporaryPlayerTestBoost => temporaryPlayerBoost != null
        && temporaryPlayerBoost.IsApplied;
    public bool HasCinematicOverlay => cinematicOverlay != null;
    public bool IsBossVisualScaleApplied => bossVisualScaleApplied;
    public float BossVisualScaleMultiplier => bossVisualScaleMultiplier;

    public void RequestDialogueAdvance()
    {
        if (interactiveDialoguePlaying) introAdvanceRequested = true;
    }

    public void RequestIntroSkip()
    {
        RequestDialogueSkip();
    }

    public void RequestDialogueSkip()
    {
        if (!interactiveDialoguePlaying || introSkipRequested) return;
        introSkipRequested = true;
        dialogueSkipCount++;
        if (activeInteractiveSequence == HWJ_BossDialogueSequenceType.Intro) introSkipCount++;
    }

    private void Awake()
    {
        CacheReferences();
        ApplyBossVisualScale();
        if (bossRenderer != null) normalColor = bossRenderer.color;
        if (autoPlayIntro) SetCombatBlocked(true);
    }

    private void OnEnable()
    {
        CacheReferences();
        ApplyBossVisualScale();
        SubscribePatternImpact();
        if (introPlayed && !deathPresentationStarted && runtimeStatus != null && !runtimeStatus.IsDead)
        {
            presentationBlocking = false;
            if (bossLogic != null)
            {
                bossLogic.enabled = true;
                bossLogic.StartEncounter();
            }
        }
    }

    private IEnumerator Start()
    {
        CacheReferences();
        previousHp = runtimeStatus != null ? runtimeStatus.CurrentHp : float.NaN;
        yield return null;
        EnsureTemporaryPlayerTestBoost();

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
            if (hys_MidBoss2EncounterSession.HasSeenIntro(this))
            {
                // 같은 실행 중 재도전에서는 긴 인트로를 반복하지 않고 바로 전투 준비 상태로 복구합니다.
                introSuppressedByReentry = true;
                introPlayed = true;
                SetCombatBlocked(false);
            }
            else
            {
                hys_MidBoss2EncounterSession.MarkIntroSeen(this);
                presentationRoutine = StartCoroutine(PlayIntroRoutine());
            }
        }
    }

    private void Update()
    {
        CacheReferences();
        EnsureTemporaryPlayerTestBoost();
        SubscribePatternImpact();
        UpdateBossName();
        UpdateHitPresentation();
        UpdatePhaseTransition();
        UpdateDeathPresentation();
        UpdatePlayerDeathDuringPresentation();
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
        if (phaseEffectsRoutine != null) StopCoroutine(phaseEffectsRoutine);
        phaseEffectsRoutine = null;
        phaseTransitionVisual?.CompleteNow();
        cameraFocusSystem?.StopFocus();
        dialogueSystem?.CancelDialogueSequence(true);
        cinematicOverlay?.EndIntro();
        CompleteBossDropImmediately();
        interactiveIntroPlaying = false;
        interactiveDialoguePlaying = false;
        activeInteractiveSequence = default;
        introAdvanceRequested = false;
        introSkipRequested = false;
        skipHoldProgress = 0f;
        combatStartGraceActive = false;
        ReleasePlayerControl();
        RestoreBossColor();
        presentationBlocking = false;
        if (bossLogic != null && (runtimeStatus == null || !runtimeStatus.IsDead))
            bossLogic.enabled = true;
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
        Transform player = ResolvePlayerTransform();
        AcquirePlayerControl(player);

        if (useBossDropIntro)
            yield return PlayBossDropIntro(player);

        if (useInteractiveIntro && dialogueSystem != null)
        {
            yield return PlayInteractiveIntroRoutine(player);
        }
        else
        {
            float duration = ResolveDialogueDuration(
                HWJ_BossDialogueSequenceType.Intro,
                fallbackIntroSeconds);
            dialogueSystem?.ShowIntroDialogue();
            cameraFocusSystem?.FocusOnBoss(transform, player, duration);
            GenerateImpulse(phaseShakeForce * 0.45f);
            yield return new WaitForSeconds(duration);
        }

        // 연출이 끝난 뒤 플레이어를 먼저 풀고, 보스 AI는 짧게 늦춰 불공정한 선공을 막습니다.
        ReleasePlayerControl();
        combatStartGraceActive = true;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, combatStartSafetySeconds));
        combatStartGraceActive = false;
        SetCombatBlocked(false);
        presentationRoutine = null;
    }

    private IEnumerator PlayBossDropIntro(Transform player)
    {
        bossDropLandingPosition = transform.position;
        StopPlayerAtCurrentPosition(player);
        cameraFocusSystem?.StopFocus();

        bossDropBodyWasSimulated = body != null && body.simulated;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        bossDropInProgress = true;
        Vector3 dropStart = bossDropLandingPosition + Vector3.up * Mathf.Max(1f, introDropHeight);
        transform.position = dropStart;
        if (bossRenderer != null)
        {
            Color silhouette = introDropSilhouetteColor;
            silhouette.a = normalColor.a;
            bossRenderer.color = silhouette;
        }

        // 착지 위치를 먼저 보여줘 플레이어와 보스의 위치 관계를 놓치지 않게 합니다.
        hys_SecondBossMagicVisual.SpawnShockwave(
            bossDropLandingPosition + Vector3.down * 0.1f,
            0.25f,
            2.8f,
            Mathf.Max(0.25f, introDropSeconds),
            new Color(0.25f, 0.12f, 0.05f, 0.55f));
        if (introDropSettleSeconds > 0f)
            yield return new WaitForSecondsRealtime(introDropSettleSeconds);

        float duration = Mathf.Max(0.1f, introDropSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float accelerated = progress * progress;
            transform.position = Vector3.LerpUnclamped(dropStart, bossDropLandingPosition, accelerated);
            if (bossRenderer != null)
            {
                Color silhouette = introDropSilhouetteColor;
                silhouette.a = normalColor.a;
                bossRenderer.color = Color.Lerp(silhouette, normalColor, Mathf.InverseLerp(0.55f, 1f, progress));
            }
            yield return null;
        }

        CompleteBossDropImmediately();
        introDropCompleted = true;
        introLandingCount++;
        hys_SecondBossMagicVisual.SpawnShockwave(
            bossDropLandingPosition + Vector3.down * 0.1f,
            0.65f,
            7.5f,
            1.15f,
            royalSealColor);
        hys_SecondBossMagicVisual.SpawnShockwave(
            bossDropLandingPosition + Vector3.down * 0.05f,
            0.35f,
            4.6f,
            0.7f,
            Color.white);
        SpawnLandingSparks();
        GenerateImpulse(introLandingShakeForce);
        cinematicOverlay?.PlayReleaseFlash(releaseFlashSeconds * 0.55f);
        yield return PlayLandingRecoil(player);
    }

    private IEnumerator PlayLandingRecoil(Transform player)
    {
        if (bossRenderer == null) yield break;
        // 낙하 인트로에서도 전투 로직과 동일하게 기존 이미지 방향의 반대를 사용합니다.
        if (player != null) bossRenderer.flipX = player.position.x < transform.position.x;

        Transform visual = bossRenderer.transform;
        Vector3 originalScale = visual.localScale;
        Vector3 impactScale = new Vector3(
            originalScale.x * 1.12f,
            originalScale.y * 0.82f,
            originalScale.z);
        bossRenderer.color = Color.white;
        visual.localScale = impactScale;
        yield return new WaitForSecondsRealtime(0.07f);

        float duration = Mathf.Max(0.05f, introLandingRecoilSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float bounce = 1f - Mathf.Pow(1f - progress, 3f);
            visual.localScale = Vector3.LerpUnclamped(impactScale, originalScale, bounce);
            bossRenderer.color = Color.Lerp(Color.white, normalColor, progress);
            yield return null;
        }
        visual.localScale = originalScale;
        bossRenderer.color = normalColor;
    }

    private void SpawnLandingSparks()
    {
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Lerp(38f, 142f, i / 5f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            hys_SecondBossMagicVisual.SpawnSword(
                bossDropLandingPosition + Vector3.up * 0.25f,
                direction,
                0.34f,
                0.75f,
                0.07f,
                4.5f,
                Color.Lerp(Color.white, royalSealColor, 0.55f));
            introLandingSparkCount++;
        }
    }

    private static void StopPlayerAtCurrentPosition(Transform player)
    {
        if (player == null) return;
        Rigidbody2D playerBody = player.GetComponentInParent<Rigidbody2D>();
        if (playerBody == null) return;

        // 좌표는 건드리지 않고 진입 당시의 물리 속도만 제거해 벽·타일 겹침을 만들지 않습니다.
        playerBody.linearVelocity = Vector2.zero;
        playerBody.angularVelocity = 0f;
    }

    private void CompleteBossDropImmediately()
    {
        if (!bossDropInProgress) return;
        transform.position = bossDropLandingPosition;
        if (body != null)
        {
            body.position = bossDropLandingPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = bossDropBodyWasSimulated;
        }
        bossDropInProgress = false;
    }

    private IEnumerator PlayInteractiveIntroRoutine(Transform player)
    {
        int availableLineCount = dialogueSystem.GetSequenceLineCount(HWJ_BossDialogueSequenceType.Intro);
        int lineCount = Mathf.Clamp(cinematicIntroLineCount, 1, Mathf.Max(1, availableLineCount));
        float maximumFocusSeconds = lineCount * Mathf.Max(0.3f, introLineAutoAdvanceSeconds)
            + introTitleSeconds + releaseFlashSeconds + 1f;

        cinematicOverlay?.BeginIntro(player, royalSealColor);
        cameraFocusSystem?.FocusOnBoss(transform, player, maximumFocusSeconds);
        GenerateImpulse(phaseShakeForce * 0.45f);
        yield return PlayInteractiveDialogueLines(
            HWJ_BossDialogueSequenceType.Intro,
            lineCount,
            introLineAutoAdvanceSeconds);
        cinematicOverlay?.ShowBossTitle(bossDisplayName, bossEpithet);

        // 별도 캐릭터 모션 없이 금색 충격파와 카메라 충격으로 처형식의 마지막 박자를 만듭니다.
        hys_SecondBossMagicVisual.SpawnShockwave(
            transform.position + Vector3.down * 0.1f,
            0.7f,
            8.5f,
            1.5f,
            royalSealColor);
        GenerateImpulse(phaseShakeForce * 0.72f);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, introTitleSeconds));

        cinematicOverlay?.PlayReleaseFlash(releaseFlashSeconds);
        yield return new WaitForSecondsRealtime(releaseFlashSeconds * 0.45f);
        cameraFocusSystem?.StopFocus();
        yield return new WaitForSecondsRealtime(releaseFlashSeconds * 0.55f);
        cinematicOverlay?.EndIntro();
        dialogueSystem.CancelDialogueSequence(true);
    }

    private IEnumerator PlayInteractiveDialogueLines(
        HWJ_BossDialogueSequenceType sequenceType,
        int requestedLineCount,
        float autoAdvanceSeconds)
    {
        interactiveDialoguePlaying = true;
        interactiveIntroPlaying = sequenceType == HWJ_BossDialogueSequenceType.Intro;
        activeInteractiveSequence = sequenceType;
        currentInteractiveLineIndex = -1;
        skipHoldProgress = 0f;
        introAdvanceRequested = false;
        introSkipRequested = false;

        int availableLineCount = dialogueSystem != null
            ? dialogueSystem.GetSequenceLineCount(sequenceType)
            : 0;
        int lineCount = Mathf.Clamp(requestedLineCount, 1, Mathf.Max(1, availableLineCount));
        for (int i = 0; i < lineCount; i++)
        {
            string line = dialogueSystem?.GetSequenceLine(sequenceType, i);
            if (string.IsNullOrWhiteSpace(line)) continue;

            currentInteractiveLineIndex = i;
            dialogueSystem.ShowLine(line, 60f);
            float lineStartTime = Time.unscaledTime;
            while (Time.unscaledTime < lineStartTime + Mathf.Max(0.3f, autoAdvanceSeconds))
            {
                if (IsSkipHeld())
                {
                    skipHoldProgress += Time.unscaledDeltaTime;
                    cinematicOverlay?.SetSkipProgress(
                        skipHoldProgress / Mathf.Max(0.01f, introSkipHoldSeconds));
                    if (skipHoldProgress >= introSkipHoldSeconds) RequestDialogueSkip();
                }
                else
                {
                    skipHoldProgress = 0f;
                    cinematicOverlay?.SetSkipProgress(0f);
                }

                if (WasAdvancePressed()) RequestDialogueAdvance();
                if (introSkipRequested) break;
                if (Time.unscaledTime >= lineStartTime + minimumLineInputSeconds
                    && introAdvanceRequested)
                {
                    dialogueAdvanceCount++;
                    introAdvanceRequested = false;
                    break;
                }
                yield return null;
            }

            dialogueSystem.CancelDialogueSequence(true);
            if (introSkipRequested) break;
        }

        currentInteractiveLineIndex = -1;
        skipHoldProgress = 0f;
        cinematicOverlay?.SetSkipProgress(0f);
        dialogueSystem?.CancelDialogueSequence(true);
        interactiveIntroPlaying = false;
        interactiveDialoguePlaying = false;
        activeInteractiveSequence = default;
        introAdvanceRequested = false;
        introSkipRequested = false;
    }

    private IEnumerator PlayPhaseTwoRoutine()
    {
        phaseTwoTriggered = true;
        phaseTransitionPlayCount++;
        SetCombatBlocked(true);
        Transform player = ResolvePlayerTransform();
        AcquirePlayerControl(player);
        int availableLineCount = dialogueSystem != null
            ? dialogueSystem.GetSequenceLineCount(HWJ_BossDialogueSequenceType.PhaseTransition)
            : 0;
        int lineCount = Mathf.Clamp(phaseDialogueLineCount, 1, Mathf.Max(1, availableLineCount));
        float duration = useInteractivePhaseDialogue && dialogueSystem != null
            ? lineCount * Mathf.Max(0.3f, introLineAutoAdvanceSeconds) + 0.5f
            : ResolveDialogueDuration(HWJ_BossDialogueSequenceType.PhaseTransition, fallbackPhaseSeconds);
        if (useInteractivePhaseDialogue && dialogueSystem != null)
            cinematicOverlay?.BeginDialogueControls("2페이즈 각성", phasePulseColor);
        else
            dialogueSystem?.ShowPhaseTwoDialogue();
        cameraFocusSystem?.FocusOnBoss(transform, player, duration);
        GenerateImpulse(phaseShakeForce);

        // 금지된 흑마법이 깨어나는 순간을 마력진, 검의 포위와 연속 충격파로 단계적으로 보여줍니다.
        int phaseBurstCountAtStart = phaseVisualBurstCount;
        SpawnPhaseTransitionOpening(duration);
        phaseTransitionVisual?.Play(duration, phasePulseColor);
        phaseFinalBurstPlayed = false;
        phaseEffectsRoutine = StartCoroutine(PlayPhaseTransitionEffectsRoutine(duration));
        if (useInteractivePhaseDialogue && dialogueSystem != null)
        {
            yield return PlayInteractiveDialogueLines(
                HWJ_BossDialogueSequenceType.PhaseTransition,
                lineCount,
                introLineAutoAdvanceSeconds);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        if (phaseEffectsRoutine != null) StopCoroutine(phaseEffectsRoutine);
        phaseEffectsRoutine = null;
        phaseTransitionVisual?.CompleteNow();
        CompleteRemainingPhaseBursts(phaseBurstCountAtStart);

        RestoreBossColor();
        cameraFocusSystem?.StopFocus();
        cinematicOverlay?.EndIntro();
        dialogueSystem?.CancelDialogueSequence(true);
        ReleasePlayerControl();
        SetCombatBlocked(false);
        presentationRoutine = null;
    }

    private IEnumerator PlayPhaseTransitionEffectsRoutine(float duration)
    {
        bool firstBurstPlayed = false;
        bool secondBurstPlayed = false;
        bool thirdBurstPlayed = false;
        float endTime = Time.time + Mathf.Max(0.1f, duration);
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
            if (!phaseFinalBurstPlayed && progress >= 0.86f)
            {
                phaseFinalBurstPlayed = true;
                SpawnPhaseTransitionBurst(1.35f, 11.2f, 3.1f);
            }
            yield return null;
        }
        phaseEffectsRoutine = null;
    }

    private void CompleteRemainingPhaseBursts(int countBeforeTransition)
    {
        // 스킵하더라도 5단계 의식의 핵심 타격은 압축 재생해 전환이 허전해지지 않게 합니다.
        int playedStages = phaseVisualBurstCount - countBeforeTransition;
        if (playedStages < 2) SpawnPhaseTransitionBurst(0.55f, 4.8f, 1.35f);
        if (playedStages < 3) SpawnPhaseTransitionBurst(0.8f, 6.8f, 1.9f);
        if (playedStages < 4) SpawnPhaseTransitionBurst(1.05f, 8.8f, 2.45f);
        if (playedStages < 5)
        {
            phaseFinalBurstPlayed = true;
            SpawnPhaseTransitionBurst(1.35f, 11.2f, 3.1f);
        }
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
        hys_MidBoss2EncounterSession.MarkDefeated(this);
        SetCombatBlocked(true);
        Transform player = ResolvePlayerTransform();
        AcquirePlayerControl(player);
        int availableLineCount = dialogueSystem != null
            ? dialogueSystem.GetSequenceLineCount(HWJ_BossDialogueSequenceType.Death)
            : 0;
        int lineCount = Mathf.Clamp(deathDialogueLineCount, 1, Mathf.Max(1, availableLineCount));
        float duration = useInteractiveDeathDialogue && dialogueSystem != null
            ? lineCount * Mathf.Max(0.3f, introLineAutoAdvanceSeconds) + 0.5f
            : ResolveDialogueDuration(HWJ_BossDialogueSequenceType.Death, fallbackDeathSeconds);
        if (useInteractiveDeathDialogue && dialogueSystem != null)
            cinematicOverlay?.BeginDialogueControls("최후의 유언", royalSealColor);
        else
            dialogueSystem?.ShowDeathDialogue();
        cameraFocusSystem?.FocusOnBoss(transform, player, duration);
        GenerateImpulse(deathShakeForce);
        if (useInteractiveDeathDialogue && dialogueSystem != null)
        {
            yield return PlayInteractiveDialogueLines(
                HWJ_BossDialogueSequenceType.Death,
                lineCount,
                introLineAutoAdvanceSeconds);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
        RestoreBossColor();
        cameraFocusSystem?.StopFocus();
        cinematicOverlay?.EndIntro();
        dialogueSystem?.CancelDialogueSequence(true);
        ReleasePlayerControl();
        presentationRoutine = null;
    }

    private void AcquirePlayerControl(Transform player)
    {
        if (player == null) return;
        Rigidbody2D playerBody = player.GetComponentInParent<Rigidbody2D>();
        GameObject playerRoot = playerBody != null ? playerBody.gameObject : player.gameObject;
        playerControlLock = playerRoot.GetComponent<hys_PlayerCinematicControlLock>();
        if (playerControlLock == null)
            playerControlLock = playerRoot.AddComponent<hys_PlayerCinematicControlLock>();
        playerControlLock.Acquire(this);
        playerControlLocked = playerControlLock.IsLocked;
    }

    private void ReleasePlayerControl()
    {
        if (playerControlLock != null) playerControlLock.Release(this);
        playerControlLocked = false;
        playerControlLock = null;
    }

    private static bool WasAdvancePressed()
    {
        bool keyboardPressed = Keyboard.current != null
            && (Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        return keyboardPressed || mousePressed || gamepadPressed;
    }

    private static bool IsSkipHeld()
    {
        bool keyboardHeld = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
        bool gamepadHeld = Gamepad.current != null && Gamepad.current.startButton.isPressed;
        return keyboardHeld || gamepadHeld;
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

    private void UpdatePlayerDeathDuringPresentation()
    {
        if (!presentationBlocking || runtimeStatus != null && runtimeStatus.IsDead) return;
        Transform player = ResolvePlayerTransform();
        HWJ_RuntimeStatusSystem playerStatus = player != null
            ? player.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;
        if (playerStatus == null || !playerStatus.IsDead) return;

        playerDeathAbortCount++;
        if (presentationRoutine != null) StopCoroutine(presentationRoutine);
        presentationRoutine = null;
        if (phaseEffectsRoutine != null) StopCoroutine(phaseEffectsRoutine);
        phaseEffectsRoutine = null;
        phaseTransitionVisual?.CompleteNow();
        dialogueSystem?.CancelDialogueSequence(true);
        cameraFocusSystem?.StopFocus();
        cinematicOverlay?.EndIntro();
        ResetInteractiveDialogueState();
        combatStartGraceActive = false;
        ReleasePlayerControl();
        RestoreBossColor();
        presentationBlocking = false;
        patternSystem?.CancelActivePattern();
        if (bossLogic != null)
        {
            bossLogic.enabled = true;
            bossLogic.StopEncounter();
        }
    }

    private void ResetInteractiveDialogueState()
    {
        interactiveIntroPlaying = false;
        interactiveDialoguePlaying = false;
        activeInteractiveSequence = default;
        currentInteractiveLineIndex = -1;
        introAdvanceRequested = false;
        introSkipRequested = false;
        skipHoldProgress = 0f;
        cinematicOverlay?.SetSkipProgress(0f);
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

    private void ApplyBossVisualScale()
    {
        if (bossVisualScaleApplied || bossRenderer == null || bossRenderer.transform == transform) return;

        // 공격 판정과 Rigidbody 크기는 유지하고 스프라이트 자식만 키워 보스의 체격을 강조합니다.
        float multiplier = Mathf.Clamp(bossVisualScaleMultiplier, 1f, 1.8f);
        Vector3 scale = bossRenderer.transform.localScale;
        bossRenderer.transform.localScale = new Vector3(
            scale.x * multiplier,
            scale.y * multiplier,
            scale.z);
        bossVisualScaleApplied = true;
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

    private void EnsureTemporaryPlayerTestBoost()
    {
        if (!enableTemporaryPlayerTestBoost)
        {
            if (temporaryPlayerBoost != null) Destroy(temporaryPlayerBoost);
            temporaryPlayerBoost = null;
            return;
        }
        if (temporaryPlayerBoost != null && temporaryPlayerBoost.IsApplied) return;

        Transform player = ResolvePlayerTransform();
        HWJ_RuntimeStatusSystem playerStatus = player != null
            ? player.GetComponentInParent<HWJ_RuntimeStatusSystem>()
            : null;
        if (playerStatus == null) return;

        Rigidbody2D playerBody = player.GetComponentInParent<Rigidbody2D>();
        GameObject playerRoot = playerBody != null ? playerBody.gameObject : playerStatus.gameObject;
        temporaryPlayerBoost = playerRoot.GetComponent<hys_MidBoss2TemporaryPlayerBoost>();
        if (temporaryPlayerBoost == null)
            temporaryPlayerBoost = playerRoot.AddComponent<hys_MidBoss2TemporaryPlayerBoost>();
        temporaryPlayerBoost.Initialize(
            playerStatus,
            temporaryPlayerHpBonus,
            temporaryPlayerAttackBonus,
            hys_MidBoss2EncounterSession.GetSceneKey(this));
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
        if (cinematicOverlay == null)
        {
            cinematicOverlay = GetComponent<hys_SecondBossCinematicOverlay>();
            if (cinematicOverlay == null)
                cinematicOverlay = gameObject.AddComponent<hys_SecondBossCinematicOverlay>();
        }
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
