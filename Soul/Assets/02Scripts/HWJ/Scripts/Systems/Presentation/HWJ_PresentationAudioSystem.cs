using UnityEngine;

/// <summary>
/// 중간발표 데모용 최소 오디오 시스템입니다.
/// 프로젝트 안에 AudioClip이 없을 때도 BGM과 핵심 SFX가 NullReference 없이 들리도록 런타임 톤을 생성합니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PresentationAudioSystem : MonoBehaviour
{
    [Header("발표 오디오 사용")]
    [Tooltip("켜져 있으면 발표용 BGM과 SFX를 재생합니다.")]
    [SerializeField] private bool enablePresentationAudio = true;
    [Tooltip("에디터 또는 개발 빌드가 아니어도 발표 오디오를 사용할지 결정합니다.")]
    [SerializeField] private bool allowInReleaseBuild;

    [Header("볼륨")]
    [Range(0f, 1f)]
    [Tooltip("배경 음악 볼륨입니다.")]
    [SerializeField] private float bgmVolume = 0.18f;
    [Range(0f, 1f)]
    [Tooltip("효과음 볼륨입니다.")]
    [SerializeField] private float sfxVolume = 0.52f;

    [Header("재생 제한")]
    [Tooltip("같은 효과음이 너무 촘촘히 겹치지 않도록 막는 최소 간격입니다.")]
    [SerializeField] private float sameSfxCooldownSeconds = 0.045f;
    [Tooltip("입력 기반 점프/대쉬 소리를 읽을 플레이어 입력입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private HWJ_PlayerInputSystem playerInput;

    [Header("확인용 상태")]
    [SerializeField] private bool bgmPlaying;
    [SerializeField] private string lastPlayedSfx;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioClip bgmClip;
    private AudioClip attackClip;
    private AudioClip bowShotClip;
    private AudioClip hitClip;
    private AudioClip deathClip;
    private AudioClip jumpClip;
    private AudioClip dashClip;
    private AudioClip soulClip;
    private AudioClip possessionClip;
    private AudioClip switchClip;
    private AudioClip doorClip;
    private AudioClip portalClip;
    private float nextSfxTime;

    private bool IsAllowed => enablePresentationAudio
        && (Application.isEditor || Debug.isDebugBuild || allowInReleaseBuild);

    private void Awake()
    {
        EnsureSources();
        EnsureGeneratedClips();
    }

    private void OnEnable()
    {
        HWJ_GameplayEvents.DamageApplied += OnDamageApplied;
        HWJ_GameplayEvents.ActorDied += OnActorDied;
        HWJ_GameplayEvents.SoulStateChanged += OnSoulStateChanged;
        HWJ_GameplayEvents.PossessionChanged += OnPossessionChanged;
        HWJ_GameplayEvents.AbilityUsed += OnAbilityUsed;
        HWJ_GameplayEvents.BodyObstacleGateChanged += OnBodyObstacleGateChanged;
        HWJ_GameplayEvents.SpiritOrbSwitchChanged += OnSpiritOrbSwitchChanged;
        HWJ_GameplayEvents.StageObjectiveChanged += OnStageObjectiveChanged;
        HWJ_GameplayEvents.StageCleared += OnStageCleared;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.DamageApplied -= OnDamageApplied;
        HWJ_GameplayEvents.ActorDied -= OnActorDied;
        HWJ_GameplayEvents.SoulStateChanged -= OnSoulStateChanged;
        HWJ_GameplayEvents.PossessionChanged -= OnPossessionChanged;
        HWJ_GameplayEvents.AbilityUsed -= OnAbilityUsed;
        HWJ_GameplayEvents.BodyObstacleGateChanged -= OnBodyObstacleGateChanged;
        HWJ_GameplayEvents.SpiritOrbSwitchChanged -= OnSpiritOrbSwitchChanged;
        HWJ_GameplayEvents.StageObjectiveChanged -= OnStageObjectiveChanged;
        HWJ_GameplayEvents.StageCleared -= OnStageCleared;
    }

    private void Start()
    {
        PlayBgmIfNeeded();
    }

    private void Update()
    {
        if (!IsAllowed)
        {
            StopBgmIfNeeded();
            return;
        }

        PlayBgmIfNeeded();
        ResolvePlayerInput();
        PollPlayerInputSfx();
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }
    }

    private void EnsureGeneratedClips()
    {
        bgmClip = bgmClip != null ? bgmClip : CreateBgmClip();
        attackClip = attackClip != null ? attackClip : CreateToneClip("HWJ_SFX_Attack", 0.11f, 420f, 0.72f, 0.18f);
        bowShotClip = bowShotClip != null ? bowShotClip : CreateToneClip("HWJ_SFX_BowShot", 0.14f, 660f, 0.82f, 0.14f);
        hitClip = hitClip != null ? hitClip : CreateToneClip("HWJ_SFX_Hit", 0.09f, 160f, 0.9f, 0.22f);
        deathClip = deathClip != null ? deathClip : CreateToneClip("HWJ_SFX_Death", 0.22f, 90f, 0.85f, 0.35f);
        jumpClip = jumpClip != null ? jumpClip : CreateToneClip("HWJ_SFX_Jump", 0.1f, 520f, 0.62f, 0.12f);
        dashClip = dashClip != null ? dashClip : CreateToneClip("HWJ_SFX_Dash", 0.08f, 760f, 0.72f, 0.12f);
        soulClip = soulClip != null ? soulClip : CreateToneClip("HWJ_SFX_Soul", 0.24f, 300f, 0.78f, 0.18f);
        possessionClip = possessionClip != null ? possessionClip : CreateToneClip("HWJ_SFX_Possession", 0.2f, 240f, 0.72f, 0.12f);
        switchClip = switchClip != null ? switchClip : CreateToneClip("HWJ_SFX_Switch", 0.16f, 880f, 0.72f, 0.1f);
        doorClip = doorClip != null ? doorClip : CreateToneClip("HWJ_SFX_Door", 0.25f, 130f, 0.75f, 0.2f);
        portalClip = portalClip != null ? portalClip : CreateToneClip("HWJ_SFX_Portal", 0.3f, 520f, 0.64f, 0.1f);
    }

    private void PlayBgmIfNeeded()
    {
        if (bgmSource == null || bgmClip == null || bgmSource.isPlaying)
        {
            bgmPlaying = bgmSource != null && bgmSource.isPlaying;
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
        bgmPlaying = true;
    }

    private void StopBgmIfNeeded()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        bgmPlaying = false;
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null)
        {
            return;
        }

        playerInput = HWJ_GameAccess.PlayerInput;

        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<HWJ_PlayerInputSystem>(FindObjectsInactive.Exclude);
        }
    }

    private void PollPlayerInputSfx()
    {
        if (playerInput == null)
        {
            return;
        }

        if (playerInput.JumpPressedThisFrame)
        {
            PlaySfx(jumpClip, "점프");
        }

        if (playerInput.DashPressedThisFrame)
        {
            PlaySfx(dashClip, "대쉬");
        }
    }

    private void OnDamageApplied(HWJ_DamageEvent damageEvent)
    {
        if (damageEvent.TargetDied)
        {
            return;
        }

        PlaySfx(hitClip, "피격");
    }

    private void OnActorDied(HWJ_DamageEvent damageEvent)
    {
        PlaySfx(deathClip, "사망");
    }

    private void OnSoulStateChanged(HWJ_SoulStateChangedEvent stateEvent)
    {
        if (stateEvent.CurrentState == HWJ_SoulRuntimeState.Soul)
        {
            PlaySfx(soulClip, "영혼 전환");
        }
    }

    private void OnPossessionChanged(HWJ_PossessionEvent possessionEvent)
    {
        if (possessionEvent.Possessed)
        {
            PlaySfx(possessionClip, "빙의");
        }
    }

    private void OnAbilityUsed(HWJ_AbilityUsedEvent abilityEvent)
    {
        if (IsBowAbility(abilityEvent))
        {
            PlaySfx(bowShotClip, "활 발사");
            return;
        }

        PlaySfx(attackClip, abilityEvent.IsBasicAttack ? "기본 공격" : "스킬");
    }

    private void OnBodyObstacleGateChanged(HWJ_BodyObstacleGateEvent gateEvent)
    {
        if (gateEvent.IsOpen)
        {
            PlaySfx(doorClip, "문 열림");
        }
    }

    private void OnSpiritOrbSwitchChanged(HWJ_SpiritOrbSwitchEvent switchEvent)
    {
        if (switchEvent.Activated)
        {
            PlaySfx(switchClip, "스위치");
        }
    }

    private void OnStageObjectiveChanged(HWJ_StageProgressionEvent progressionEvent)
    {
        PlaySfx(portalClip, "포탈 활성 조건");
    }

    private void OnStageCleared(HWJ_StageProgressionEvent progressionEvent)
    {
        PlaySfx(portalClip, "스테이지 클리어");
    }

    private bool IsBowAbility(HWJ_AbilityUsedEvent abilityEvent)
    {
        if (!string.IsNullOrWhiteSpace(abilityEvent.AbilityId)
            && abilityEvent.AbilityId.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return abilityEvent.UserResolver != null
            && abilityEvent.UserResolver.RootObjectData != null
            && abilityEvent.UserResolver.RootObjectData.WeaponType == HWJ_WeaponType.Bow;
    }

    private void PlaySfx(AudioClip clip, string label)
    {
        if (!IsAllowed || sfxSource == null || clip == null || Time.unscaledTime < nextSfxTime)
        {
            return;
        }

        sfxSource.volume = sfxVolume;
        sfxSource.PlayOneShot(clip);
        nextSfxTime = Time.unscaledTime + Mathf.Max(0f, sameSfxCooldownSeconds);
        lastPlayedSfx = label;
    }

    private static AudioClip CreateBgmClip()
    {
        const int sampleRate = 22050;
        const float durationSeconds = 4f;
        int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float chordRoot = Mathf.Sin(2f * Mathf.PI * 110f * time);
            float chordFifth = Mathf.Sin(2f * Mathf.PI * 165f * time);
            float pulse = Mathf.Sin(2f * Mathf.PI * 2f * time) * 0.5f + 0.5f;
            samples[i] = (chordRoot * 0.12f + chordFifth * 0.07f) * (0.65f + pulse * 0.35f);
        }

        AudioClip clip = AudioClip.Create("HWJ_Presentation_BGM_Generated", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateToneClip(string clipName, float durationSeconds, float frequency, float volume, float noiseAmount)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * Mathf.Max(0.01f, durationSeconds)));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float normalized = i / (float)(sampleCount - 1);
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(normalized));
            float noise = Mathf.PerlinNoise(t * 60f, frequency * 0.01f) * 2f - 1f;
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
            samples[i] = (tone * (1f - noiseAmount) + noise * noiseAmount) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
