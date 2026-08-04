using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HSH.UI
{
    /// <summary>
    /// 단일 키 변경 항목 수동 매핑 구조체/클래스 (기존 인펙터 할당 방식 하위 호환용)
    /// </summary>
    [Serializable]
    public class HSH_KeyRebindElement
    {
        [Tooltip("변경할 대상 HSH 액션")]
        public HSH_KeyAction keyAction;

        [Tooltip("키 변경 시작 버튼")]
        public Button rebindButton;

        [Tooltip("현재 키 이름을 표시할 TextMeshPro 텍스트")]
        public TMP_Text keyTextTMP;

        [Tooltip("현재 키 이름을 표시할 Legacy Text")]
        public Text keyTextLegacy;
    }

    /// <summary>
    /// HWJ 인풋 바인딩 SO 및 HSH 단축키 데이터와 연동되는 키 변경 UI 컨트롤러입니다.
    /// 컨테이너에 UI 항목 개체들이 동적으로 인스턴스화("개체가 나와 키바꿀수있는 ui")되거나 수동 항목들을 제어합니다.
    /// </summary>
    public class HSH_KeyRebindUI : MonoBehaviour
    {
        [Header("동적 개체 생성 설정 (추천)")]
        [Tooltip("키 바인딩 항목 UI 프리팹 (HSH_KeyRebindItemUI 스크립트 첨부)")]
        [SerializeField] private HSH_KeyRebindItemUI itemPrefab;

        [Tooltip("UI 항목 개체들이 동적으로 생성되어 들어갈 컨테이너 (Scroll Content 등)")]
        [SerializeField] private Transform itemContainer;

        [Header("수동 지정 바인딩 항목 목록 (선택 사항)")]
        [SerializeField] private List<HSH_KeyRebindElement> rebindElements = new List<HSH_KeyRebindElement>();

        [Header("키 입력 대기 연출 UI")]
        [Tooltip("키 변경 중일 때 활성화되는 오버레이 패널")]
        [SerializeField] private GameObject waitingOverlayPanel;

        [Tooltip("키 입력 안내 메시지 텍스트 (예: '원하는 키를 누르세요...')")]
        [SerializeField] private TMP_Text waitingMessageTMP;
        [SerializeField] private Text waitingMessageLegacy;

        [Header("초기화 버튼")]
        [Tooltip("모든 단축키를 기본값으로 복원하는 버튼")]
        [SerializeField] private Button resetDefaultsButton;

        private List<HSH_KeyRebindItemUI> spawnedItems = new List<HSH_KeyRebindItemUI>();
        private HSH_KeyBindingConfigEntry currentRebindingEntry = null;
        private HSH_KeyRebindItemUI currentItemUI = null;
        private HSH_KeyRebindElement currentManualElement = null;
        private bool isRebinding = false;

        private void OnEnable()
        {
            if (HSH_KeyBindingManager.Instance != null)
            {
                HSH_KeyBindingManager.Instance.OnAnyBindingChanged += RefreshAllKeyTexts;
                HSH_KeyBindingManager.Instance.OnKeyBindingChanged += OnKeyBindingChanged;
            }
            RefreshAllKeyTexts();
        }

        private void OnDisable()
        {
            if (HSH_KeyBindingManager.Instance != null)
            {
                HSH_KeyBindingManager.Instance.OnAnyBindingChanged -= RefreshAllKeyTexts;
                HSH_KeyBindingManager.Instance.OnKeyBindingChanged -= OnKeyBindingChanged;
            }
            CancelRebinding();
        }

        private void Start()
        {
            BuildDynamicItems();
            SetupManualElements();

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.RemoveAllListeners();
                resetDefaultsButton.onClick.AddListener(ResetAllToDefaults);
            }

            if (waitingOverlayPanel != null)
            {
                waitingOverlayPanel.SetActive(false);
            }

            RefreshAllKeyTexts();
        }

        /// <summary>
        /// ScriptableObject 데이터 기반으로 UI 항목 개체들을 동적으로 자동 생성합니다.
        /// </summary>
        public void BuildDynamicItems()
        {
            if (itemPrefab == null || itemContainer == null) return;

            // 기존 생성된 항목 정리
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedItems.Clear();

            HSH_KeyBindingDataSO dataSO = HSH_KeyBindingManager.Instance != null 
                ? HSH_KeyBindingManager.Instance.BindingDataSO 
                : null;

            if (dataSO == null || dataSO.BindingEntries == null) return;

            foreach (var entry in dataSO.BindingEntries)
            {
                if (entry == null) continue;

                HSH_KeyRebindItemUI itemInstance = Instantiate(itemPrefab, itemContainer);
                HSH_KeyBindingConfigEntry currentEntry = entry;

                itemInstance.Setup(currentEntry, (item) => StartRebindingEntry(item, currentEntry));
                spawnedItems.Add(itemInstance);
            }
        }

        private void SetupManualElements()
        {
            foreach (var element in rebindElements)
            {
                if (element != null && element.rebindButton != null)
                {
                    HSH_KeyRebindElement el = element;
                    element.rebindButton.onClick.RemoveAllListeners();
                    element.rebindButton.onClick.AddListener(() => StartRebindingManualElement(el));
                }
            }
        }

        /// <summary>
        /// 모든 바인딩 항목의 UI 텍스트를 HWJ 및 HSH의 최신 Key 값으로 갱신합니다.
        /// </summary>
        public void RefreshAllKeyTexts()
        {
            if (HSH_KeyBindingManager.Instance == null) return;

            HWJ_PlayerInputSystem hwjInput = HSH_KeyBindingManager.Instance.GetHWJPlayerInputSystem();

            // 1. 동적 생성된 개체들 텍스트 갱신
            foreach (var item in spawnedItems)
            {
                if (item == null || item.ConfigEntry == null) continue;

                string displayString = GetBindingDisplayText(item.ConfigEntry, hwjInput);
                item.SetKeyText(displayString);
            }

            // 2. 수동 지정 항목들 텍스트 갱신
            foreach (var element in rebindElements)
            {
                if (element == null) continue;
                KeyCode currentKey = HSH_KeyBindingManager.Instance.GetKey(element.keyAction);
                UpdateManualElementText(element, currentKey.ToString());
            }
        }

        private string GetBindingDisplayText(HSH_KeyBindingConfigEntry entry, HWJ_PlayerInputSystem hwjInput)
        {
            if (entry == null) return "None";

            if (entry.isHWJAction && hwjInput != null)
            {
                if (hwjInput.TryGetEffectiveBinding(entry.hwjActionId, out var binding))
                {
                    if (binding.HasMouse)
                    {
                        return $"Mouse {binding.MouseButton}";
                    }
                    if (binding.HasKeyboard)
                    {
                        return binding.KeyboardKey.ToString();
                    }
                }
            }

            if (!entry.isHWJAction)
            {
                KeyCode key = HSH_KeyBindingManager.Instance.GetKey(entry.keyAction);
                return key.ToString();
            }

            return entry.defaultKey != KeyCode.None ? entry.defaultKey.ToString() : $"Mouse {entry.defaultMouseButton}";
        }

        public void StartRebindingEntry(HSH_KeyRebindItemUI itemUI, HSH_KeyBindingConfigEntry entry)
        {
            if (isRebinding || entry == null) return;

            currentItemUI = itemUI;
            currentRebindingEntry = entry;
            currentManualElement = null;
            isRebinding = true;

            if (itemUI != null) itemUI.SetKeyText("[ 입력 대기... ]");
            ShowWaitingOverlay(entry.displayName);

            StartCoroutine(WaitForKeyInputCoroutine());
        }

        public void StartRebindingManualElement(HSH_KeyRebindElement element)
        {
            if (isRebinding || element == null) return;

            currentManualElement = element;
            currentRebindingEntry = null;
            currentItemUI = null;
            isRebinding = true;

            UpdateManualElementText(element, "[ 입력 대기... ]");
            ShowWaitingOverlay(element.keyAction.ToString());

            StartCoroutine(WaitForKeyInputCoroutine());
        }

        private void ShowWaitingOverlay(string actionTitle)
        {
            if (waitingOverlayPanel != null)
            {
                waitingOverlayPanel.SetActive(true);
            }

            string msg = $"[{actionTitle}] 키 변경 중...\n변경할 키보드 버튼 또는 마우스를 눌러주세요.";
            if (waitingMessageTMP != null) waitingMessageTMP.text = msg;
            if (waitingMessageLegacy != null) waitingMessageLegacy.text = msg;
        }

        private IEnumerator WaitForKeyInputCoroutine()
        {
            // 클릭 프레임의 입력 오작동 방지
            yield return null;

            while (isRebinding)
            {
                // 마우스 클릭 확인 (HWJ 마우스 액션 수용)
                for (int m = 0; m < 3; m++)
                {
                    if (Input.GetMouseButtonDown(m))
                    {
                        HWJ_InputMouseButton mouseBtn = (HWJ_InputMouseButton)(m + 1);
                        CompleteMouseRebinding(mouseBtn);
                        yield break;
                    }
                }

                // 키보드 입력 확인
                if (Input.anyKeyDown)
                {
                    foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                    {
                        // 마우스 버튼 키코드 제외
                        if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
                        {
                            continue;
                        }

                        if (Input.GetKeyDown(keyCode))
                        {
                            CompleteKeyboardRebinding(keyCode);
                            yield break;
                        }
                    }
                }
                yield return null;
            }
        }

        private void CompleteKeyboardRebinding(KeyCode newKey)
        {
            if (HSH_KeyBindingManager.Instance != null)
            {
                if (currentRebindingEntry != null)
                {
                    if (currentRebindingEntry.isHWJAction)
                    {
                        HSH_KeyBindingManager.Instance.SetHWJKeyboardBinding(currentRebindingEntry.hwjActionId, newKey);
                    }
                    else
                    {
                        HSH_KeyBindingManager.Instance.SetKey(currentRebindingEntry.keyAction, newKey);
                    }
                }
                else if (currentManualElement != null)
                {
                    HSH_KeyBindingManager.Instance.SetKey(currentManualElement.keyAction, newKey);
                }
            }

            FinishRebindingUI();
        }

        private void CompleteMouseRebinding(HWJ_InputMouseButton mouseButton)
        {
            if (HSH_KeyBindingManager.Instance != null && currentRebindingEntry != null && currentRebindingEntry.isHWJAction)
            {
                HSH_KeyBindingManager.Instance.SetHWJMouseBinding(currentRebindingEntry.hwjActionId, mouseButton);
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
            currentRebindingEntry = null;
            currentItemUI = null;
            currentManualElement = null;

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

        private void UpdateManualElementText(HSH_KeyRebindElement element, string textValue)
        {
            if (element.keyTextTMP != null) element.keyTextTMP.text = textValue;
            if (element.keyTextLegacy != null) element.keyTextLegacy.text = textValue;
        }
    }
}
