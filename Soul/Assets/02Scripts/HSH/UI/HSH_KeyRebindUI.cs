using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HSH.UI
{
    /// <summary>
    /// 단일 키 변경 항목 데이터 및 UI 매핑 구조체/클래스
    /// </summary>
    [Serializable]
    public class HSH_KeyRebindElement
    {
        [Tooltip("변경할 대상 액션")]
        public HSH_KeyAction keyAction;

        [Tooltip("키 변경 시작 버튼")]
        public Button rebindButton;

        [Tooltip("현재 키 이름을 표시할 TextMeshPro 텍스트")]
        public TMP_Text keyTextTMP;

        [Tooltip("현재 키 이름을 표시할 Legacy Text")]
        public Text keyTextLegacy;
    }

    /// <summary>
    /// 설정창 패널 내부에서 단축키 바인딩 목록을 표시하고, 키 입력 대기 및 변경(Rebind)을 처리하는 UI 컨트롤러 스크립트입니다.
    /// </summary>
    public class HSH_KeyRebindUI : MonoBehaviour
    {
        [Header("키 바인딩 항목 목록")]
        [SerializeField] private List<HSH_KeyRebindElement> rebindElements = new List<HSH_KeyRebindElement>();

        [Header("키 입력 대기 연출 UI (선택 사항)")]
        [Tooltip("키 변경 중일 때 활성화되는 오버레이 패널")]
        [SerializeField] private GameObject waitingOverlayPanel;

        [Tooltip("키 입력 안내 메시지 텍스트 (예: '원하는 키를 누르세요...')")]
        [SerializeField] private TMP_Text waitingMessageTMP;
        [SerializeField] private Text waitingMessageLegacy;

        [Header("초기화 버튼 (선택 사항)")]
        [Tooltip("모든 단축키를 기본값으로 복원하는 버튼")]
        [SerializeField] private Button resetDefaultsButton;

        private HSH_KeyRebindElement currentRebindingElement = null;
        private bool isRebinding = false;

        private void OnEnable()
        {
            RefreshAllKeyTexts();
            if (HSH_KeyBindingManager.Instance != null)
            {
                HSH_KeyBindingManager.Instance.OnKeyBindingChanged += OnKeyBindingChanged;
            }
        }

        private void OnDisable()
        {
            if (HSH_KeyBindingManager.Instance != null)
            {
                HSH_KeyBindingManager.Instance.OnKeyBindingChanged -= OnKeyBindingChanged;
            }
            CancelRebinding();
        }

        private void Start()
        {
            // 각 버튼에 클릭 이벤트 바인딩
            foreach (var element in rebindElements)
            {
                if (element != null && element.rebindButton != null)
                {
                    HSH_KeyRebindElement el = element;
                    element.rebindButton.onClick.AddListener(() => StartRebinding(el));
                }
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.AddListener(ResetAllToDefaults);
            }

            if (waitingOverlayPanel != null)
            {
                waitingOverlayPanel.SetActive(false);
            }

            RefreshAllKeyTexts();
        }

        /// <summary>
        /// 모든 바인딩 항목의 UI 텍스트를 최신 KeyCode로 갱신합니다.
        /// </summary>
        public void RefreshAllKeyTexts()
        {
            if (HSH_KeyBindingManager.Instance == null) return;

            foreach (var element in rebindElements)
            {
                if (element == null) continue;
                KeyCode currentKey = HSH_KeyBindingManager.Instance.GetKey(element.keyAction);
                UpdateElementText(element, currentKey.ToString());
            }
        }

        /// <summary>
        /// 특정 키 변경 버튼 클릭 시 키 입력 대기 모드를 시작합니다.
        /// </summary>
        public void StartRebinding(HSH_KeyRebindElement element)
        {
            if (isRebinding || element == null) return;

            currentRebindingElement = element;
            isRebinding = true;

            UpdateElementText(element, "[ 입력 대기... ]");

            if (waitingOverlayPanel != null)
            {
                waitingOverlayPanel.SetActive(true);
            }

            string msg = $"{element.keyAction} 키 변경 중...\n변경할 키를 눌러주세요.";
            if (waitingMessageTMP != null) waitingMessageTMP.text = msg;
            if (waitingMessageLegacy != null) waitingMessageLegacy.text = msg;

            StartCoroutine(WaitForKeyInputCoroutine());
        }

        private IEnumerator WaitForKeyInputCoroutine()
        {
            // 클릭 순간의 프레임 입력 오작동 방지
            yield return null;

            while (isRebinding)
            {
                if (Input.anyKeyDown)
                {
                    foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                    {
                        // 마우스 버튼 클릭 제외 (필요 시 Mouse0 ~ Mouse6 제외)
                        if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
                        {
                            continue;
                        }

                        if (Input.GetKeyDown(keyCode))
                        {
                            CompleteRebinding(keyCode);
                            yield break;
                        }
                    }
                }
                yield return null;
            }
        }

        private void CompleteRebinding(KeyCode newKey)
        {
            if (currentRebindingElement != null && HSH_KeyBindingManager.Instance != null)
            {
                HSH_KeyBindingManager.Instance.SetKey(currentRebindingElement.keyAction, newKey);
            }

            FinishRebindingUI();
        }

        public void CancelRebinding()
        {
            if (!isRebinding) return;
            FinishRebindingUI();
            RefreshAllKeyTexts();
        }

        private void FinishRebindingUI()
        {
            isRebinding = false;
            currentRebindingElement = null;

            if (waitingOverlayPanel != null)
            {
                waitingOverlayPanel.SetActive(false);
            }

            RefreshAllKeyTexts();
        }

        private void ResetAllToDefaults()
        {
            if (HSH_KeyBindingManager.Instance != null)
            {
                HSH_KeyBindingManager.Instance.ResetToDefaults();
                RefreshAllKeyTexts();
            }
        }

        private void OnKeyBindingChanged(HSH_KeyAction action, KeyCode newKey)
        {
            RefreshAllKeyTexts();
        }

        private void UpdateElementText(HSH_KeyRebindElement element, string textValue)
        {
            if (element.keyTextTMP != null)
            {
                element.keyTextTMP.text = textValue;
            }
            if (element.keyTextLegacy != null)
            {
                element.keyTextLegacy.text = textValue;
            }
        }
    }
}
