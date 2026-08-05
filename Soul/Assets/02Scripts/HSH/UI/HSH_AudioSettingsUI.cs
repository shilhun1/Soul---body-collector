using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HSH.UI
{
    /// <summary>
    /// 설정창 패널 내부에서 마스터, BGM, SFX 슬라이더 및 텍스트 표시를 제어하는 스크립트입니다.
    /// 슬라이더 조정 시 HSH_AudioManager를 통해 실시간으로 볼륨을 반영하고 저장합니다.
    /// </summary>
    public class HSH_AudioSettingsUI : MonoBehaviour
    {
        [Header("마스터 음량 UI")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private TMP_Text masterValueTextTMP;
        [SerializeField] private Text masterValueTextLegacy;

        [Header("BGM / 노래 음량 UI")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmValueTextTMP;
        [SerializeField] private Text bgmValueTextLegacy;

        [Header("SFX / 효과음 음량 UI")]
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValueTextTMP;
        [SerializeField] private Text sfxValueTextLegacy;

        private bool isInitializing = false;

        private void OnEnable()
        {
            RefreshUI();
        }

        private void Start()
        {
            // 슬라이더 이벤트 바인딩
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
            }

            RefreshUI();
        }

        /// <summary>
        /// AudioManager의 저장된 볼륨 상태로 UI 슬라이더 및 텍스트를 초기화합니다.
        /// </summary>
        public void RefreshUI()
        {
            if (HSH_AudioManager.Instance == null) return;

            isInitializing = true;

            if (masterSlider != null)
            {
                masterSlider.value = HSH_AudioManager.Instance.MasterVolume;
            }
            UpdateText(masterValueTextTMP, masterValueTextLegacy, HSH_AudioManager.Instance.MasterVolume);

            if (bgmSlider != null)
            {
                bgmSlider.value = HSH_AudioManager.Instance.BGMVolume;
            }
            UpdateText(bgmValueTextTMP, bgmValueTextLegacy, HSH_AudioManager.Instance.BGMVolume);

            if (sfxSlider != null)
            {
                sfxSlider.value = HSH_AudioManager.Instance.SFXVolume;
            }
            UpdateText(sfxValueTextTMP, sfxValueTextLegacy, HSH_AudioManager.Instance.SFXVolume);

            isInitializing = false;
        }

        private void OnMasterSliderChanged(float val)
        {
            if (isInitializing) return;
            if (HSH_AudioManager.Instance != null)
            {
                HSH_AudioManager.Instance.SetMasterVolume(val);
            }
            UpdateText(masterValueTextTMP, masterValueTextLegacy, val);
        }

        private void OnBGMSliderChanged(float val)
        {
            if (isInitializing) return;
            if (HSH_AudioManager.Instance != null)
            {
                HSH_AudioManager.Instance.SetBGMVolume(val);
            }
            UpdateText(bgmValueTextTMP, bgmValueTextLegacy, val);
        }

        private void OnSFXSliderChanged(float val)
        {
            if (isInitializing) return;
            if (HSH_AudioManager.Instance != null)
            {
                HSH_AudioManager.Instance.SetSFXVolume(val);
            }
            UpdateText(sfxValueTextTMP, sfxValueTextLegacy, val);
        }

        private void UpdateText(TMP_Text tmp, Text legacy, float val)
        {
            int percentage = Mathf.RoundToInt(val * 100f);
            string textValue = percentage + "%";

            if (tmp != null)
            {
                tmp.text = textValue;
            }
            if (legacy != null)
            {
                legacy.text = textValue;
            }
        }
    }
}
