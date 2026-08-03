using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace HSH.UI
{
    /// <summary>
    /// 오디오 음량(마스터, BGM, SFX)을 총괄 관리하는 싱글톤 매니저 스크립트입니다.
    /// AudioMixer 연동을 기본 지원하며, AudioMixer가 없는 경우 AudioListener 및 개별 AudioSource 조절을 자동 지원합니다.
    /// 설정된 볼륨 값은 PlayerPrefs에 자동으로 저장/로드됩니다.
    /// </summary>
    public class HSH_AudioManager : MonoBehaviour
    {
        public static HSH_AudioManager Instance { get; private set; }

        private const string PREFS_MASTER_KEY = "HSH_MasterVolume";
        private const string PREFS_BGM_KEY = "HSH_BGMVolume";
        private const string PREFS_SFX_KEY = "HSH_SFXVolume";

        [Header("Audio Mixer 설정 (선택 사항)")]
        [Tooltip("사용 중인 AudioMixer. 지정하지 않으면 AudioListener 및 AudioSource 직렬 제어로 동작합니다.")]
        [SerializeField] private AudioMixer audioMixer;

        [Tooltip("AudioMixer에서 노출(Expose)된 Master 볼륨 파라미터 이름")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";

        [Tooltip("AudioMixer에서 노출(Expose)된 BGM 볼륨 파라미터 이름")]
        [SerializeField] private string bgmVolumeParam = "BGMVolume";

        [Tooltip("AudioMixer에서 노출(Expose)된 SFX 볼륨 파라미터 이름")]
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Header("개별 AudioSource 설정 (AudioMixer 미사용 시 Fallback)")]
        [Tooltip("BGM 재생용 AudioSource (선택 사항)")]
        [SerializeField] private AudioSource bgmAudioSource;

        [Tooltip("SFX 효과음 재생용 AudioSource 목록 (선택 사항)")]
        [SerializeField] private List<AudioSource> sfxAudioSources = new List<AudioSource>();

        // 0.0f ~ 1.0f 범위 값
        public float MasterVolume { get; private set; } = 1.0f;
        public float BGMVolume { get; private set; } = 1.0f;
        public float SFXVolume { get; private set; } = 1.0f;

        // 볼륨 변경 시 발행되는 이벤트
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnBGMVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumeSettings();
        }

        private void Start()
        {
            ApplyAllVolumes();
        }

        /// <summary>
        /// PlayerPrefs에 저장된 음량 설정값을 읽어옵니다.
        /// </summary>
        public void LoadVolumeSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(PREFS_MASTER_KEY, 1.0f);
            BGMVolume = PlayerPrefs.GetFloat(PREFS_BGM_KEY, 1.0f);
            SFXVolume = PlayerPrefs.GetFloat(PREFS_SFX_KEY, 1.0f);
        }

        /// <summary>
        /// 마스터 볼륨을 설정합니다 (0.0 ~ 1.0)
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_MASTER_KEY, MasterVolume);
            PlayerPrefs.Save();

            ApplyMasterVolume();
            OnMasterVolumeChanged?.Invoke(MasterVolume);
        }

        /// <summary>
        /// BGM 볼륨을 설정합니다 (0.0 ~ 1.0)
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_BGM_KEY, BGMVolume);
            PlayerPrefs.Save();

            ApplyBGMVolume();
            OnBGMVolumeChanged?.Invoke(BGMVolume);
        }

        /// <summary>
        /// SFX 볼륨을 설정합니다 (0.0 ~ 1.0)
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_SFX_KEY, SFXVolume);
            PlayerPrefs.Save();

            ApplySFXVolume();
            OnSFXVolumeChanged?.Invoke(SFXVolume);
        }

        /// <summary>
        /// 모든 볼륨을 AudioMixer 또는 AudioListener/AudioSource에 적용합니다.
        /// </summary>
        public void ApplyAllVolumes()
        {
            ApplyMasterVolume();
            ApplyBGMVolume();
            ApplySFXVolume();
        }

        private void ApplyMasterVolume()
        {
            if (audioMixer != null && !string.IsNullOrEmpty(masterVolumeParam))
            {
                audioMixer.SetFloat(masterVolumeParam, LinearToDecibel(MasterVolume));
            }
            else
            {
                AudioListener.volume = MasterVolume;
            }
        }

        private void ApplyBGMVolume()
        {
            if (audioMixer != null && !string.IsNullOrEmpty(bgmVolumeParam))
            {
                audioMixer.SetFloat(bgmVolumeParam, LinearToDecibel(BGMVolume));
            }

            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = BGMVolume;
            }
        }

        private void ApplySFXVolume()
        {
            if (audioMixer != null && !string.IsNullOrEmpty(sfxVolumeParam))
            {
                audioMixer.SetFloat(sfxVolumeParam, LinearToDecibel(SFXVolume));
            }

            if (sfxAudioSources != null)
            {
                foreach (var source in sfxAudioSources)
                {
                    if (source != null)
                    {
                        source.volume = SFXVolume;
                    }
                }
            }
        }

        /// <summary>
        /// 0.0~1.0 리니어 볼륨 값을 AudioMixer 데시벨(-80dB ~ 0dB)로 변환
        /// </summary>
        private float LinearToDecibel(float linear)
        {
            float clamped = Mathf.Clamp(linear, 0.0001f, 1.0f);
            return Mathf.Log10(clamped) * 20f;
        }

        /// <summary>
        /// SFX 재생용으로 새 AudioSource 등록
        /// </summary>
        public void RegisterSFXSource(AudioSource source)
        {
            if (source != null && !sfxAudioSources.Contains(source))
            {
                sfxAudioSources.Add(source);
                source.volume = SFXVolume;
            }
        }

        /// <summary>
        /// 간단한 원샷 SFX 재생 헬퍼 함수
        /// </summary>
        public void PlayOneShotSFX(AudioClip clip, Vector3 position = default)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position != default ? position : Camera.main != null ? Camera.main.transform.position : Vector3.zero, SFXVolume);
        }
    }
}
