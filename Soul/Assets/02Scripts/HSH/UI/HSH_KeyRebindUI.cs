using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    /// 카테고리별 전용 컨테이너 매핑 클래스
    /// </summary>
    [Serializable]
    public class HSH_KeyCategoryGroup
    {
        [Tooltip("카테고리/그룹 명칭 (예: 'System', 'Movement', 'Combat')")]
        public string categoryName;

        [Tooltip("해당 카테고리 항목들이 생성되어 들어갈 UI 컨테이너 Transform")]
        public Transform container;
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

        [Tooltip("단일 UI 컨테이너 (기본값)")]
        [SerializeField] private Transform itemContainer;

        [Header("다중 / 카테고리별 분배 컨테이너 설정 (원하는 지정 방식 선택)")]
        [Tooltip("카테고리별(System, Movement, Combat 등) 전용 컨테이너를 지정할 경우 사용합니다.")]
        [SerializeField] private List<HSH_KeyCategoryGroup> categoryGroups = new List<HSH_KeyCategoryGroup>();

        [Tooltip("여러 개의 컨테이너를 등록하면 키 바인딩 항목들이 순차적으로 균등 분배되어 분할 생성됩니다.")]
        [SerializeField] private List<Transform> itemContainers = new List<Transform>();

        [Header("레이아웃 자동 조정 설정")]
        [Tooltip("키 세팅 항목들을 2열(Grid 2열)로 나열하여 세로 길이를 줄입니다.")]
        [SerializeField] private bool useTwoColumnGrid = false;

        [Tooltip("2열 배치 시 각 항목의 셀 크기 (너비, 높이)")]
        [SerializeField] private Vector2 itemCellSize = new Vector2(340f, 45f);

        [Tooltip("항목 간 간격 (가로 간격, 세로 간격)")]
        [SerializeField] private Vector2 itemSpacing = new Vector2(10f, 8f);

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
        /// 지정된 컨테이너(단일, 다중 균등, 또는 카테고리별)로 나뉘어 들어갑니다.
        /// </summary>
        public void BuildDynamicItems()
        {
            if (itemPrefab == null) return;

            List<Transform> allContainers = GetActiveContainers();
            if (allContainers.Count == 0) return;

            // 기존 생성된 항목 정리 및 레이아웃 설정
            foreach (var container in allContainers)
            {
                if (container == null) continue;
                EnsureLayoutComponents(container);
                foreach (Transform child in container)
                {
                    Destroy(child.gameObject);
                }
            }
            spawnedItems.Clear();

            HSH_KeyBindingDataSO dataSO = HSH_KeyBindingManager.Instance != null 
                ? HSH_KeyBindingManager.Instance.BindingDataSO 
                : null;

            if (dataSO == null || dataSO.BindingEntries == null) return;

            var entries = dataSO.BindingEntries;

            // 1. 카테고리별 전용 컨테이너 지정 방식 사용 시
            if (categoryGroups != null && categoryGroups.Count > 0)
            {
                foreach (var entry in entries)
                {
                    if (entry == null) continue;
                    Transform targetContainer = FindCategoryContainer(entry.categoryName);
                    if (targetContainer == null) targetContainer = allContainers[0];

                    CreateItemInstance(entry, targetContainer);
                }
            }
            // 2. 다중 컨테이너 균등 분배 방식 사용 시
            else if (itemContainers != null && itemContainers.Count > 0)
            {
                List<Transform> validContainers = new List<Transform>();
                foreach (var c in itemContainers)
                {
                    if (c != null) validContainers.Add(c);
                }

                if (validContainers.Count > 0)
                {
                    int totalEntries = entries.Count;
                    int itemsPerContainer = Mathf.CeilToInt((float)totalEntries / validContainers.Count);

                    for (int i = 0; i < totalEntries; i++)
                    {
                        var entry = entries[i];
                        if (entry == null) continue;

                        int targetIndex = Mathf.Clamp(i / itemsPerContainer, 0, validContainers.Count - 1);
                        Transform targetContainer = validContainers[targetIndex];

                        CreateItemInstance(entry, targetContainer);
                    }
                }
            }
            // 3. 단일 컨테이너 기본 방식 사용 시
            else if (itemContainer != null)
            {
                foreach (var entry in entries)
                {
                    if (entry == null) continue;
                    CreateItemInstance(entry, itemContainer);
                }
            }
        }

        private void CreateItemInstance(HSH_KeyBindingConfigEntry entry, Transform targetContainer)
        {
            HSH_KeyRebindItemUI itemInstance = Instantiate(itemPrefab, targetContainer);
            HSH_KeyBindingConfigEntry currentEntry = entry;
            itemInstance.Setup(currentEntry, (item) => StartRebindingEntry(item, currentEntry));
            spawnedItems.Add(itemInstance);
        }

        private Transform FindCategoryContainer(string categoryName)
        {
            if (categoryGroups == null) return null;
            foreach (var group in categoryGroups)
            {
                if (group != null && group.container != null && string.Equals(group.categoryName, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return group.container;
                }
            }
            return null;
        }

        private List<Transform> GetActiveContainers()
        {
            List<Transform> list = new List<Transform>();
            if (categoryGroups != null && categoryGroups.Count > 0)
            {
                foreach (var g in categoryGroups)
                {
                    if (g != null && g.container != null && !list.Contains(g.container))
                        list.Add(g.container);
                }
            }
            if (itemContainers != null && itemContainers.Count > 0)
            {
                foreach (var c in itemContainers)
                {
                    if (c != null && !list.Contains(c))
                        list.Add(c);
                }
            }
            if (itemContainer != null && !list.Contains(itemContainer))
            {
                list.Add(itemContainer);
            }
            return list;
        }

        /// <summary>
        /// 컨테이너에 필요한 LayoutGroup 및 ContentSizeFitter를 자동으로 보장/설정합니다.
        /// </summary>
        private void EnsureLayoutComponents(Transform container)
        {
            if (container == null) return;

            if (useTwoColumnGrid)
            {
                var oldVertical = container.GetComponent<VerticalLayoutGroup>();
                if (oldVertical != null) Destroy(oldVertical);

                var grid = container.GetComponent<GridLayoutGroup>();
                if (grid == null) grid = container.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = itemCellSize;
                grid.spacing = itemSpacing;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                grid.childAlignment = TextAnchor.UpperCenter;
            }
            else
            {
                var oldGrid = container.GetComponent<GridLayoutGroup>();
                if (oldGrid != null) Destroy(oldGrid);

                var vertical = container.GetComponent<VerticalLayoutGroup>();
                if (vertical == null) vertical = container.gameObject.AddComponent<VerticalLayoutGroup>();
                vertical.childControlWidth = true;
                vertical.childControlHeight = false;
                vertical.childForceExpandWidth = true;
                vertical.spacing = itemSpacing.y;
                vertical.childAlignment = TextAnchor.UpperCenter;
            }

            var fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

            if (entry.isHWJAction)
            {
                if (HSH_KeyBindingManager.Instance != null)
                {
                    return HSH_KeyBindingManager.Instance.GetHWJBindingDisplayText(entry.hwjActionId);
                }
                return entry.defaultKey != KeyCode.None ? entry.defaultKey.ToString() : $"Mouse {entry.defaultMouseButton}";
            }

            KeyCode key = HSH_KeyBindingManager.Instance != null 
                ? HSH_KeyBindingManager.Instance.GetKey(entry.keyAction) 
                : entry.defaultKey;
            return key.ToString();
        }

        public void StartRebindingEntry(HSH_KeyRebindItemUI itemUI, HSH_KeyBindingConfigEntry entry)
        {
            if (isRebinding || entry == null) return;

            currentItemUI = itemUI;
            currentRebindingEntry = entry;
            currentManualElement = null;
            isRebinding = true;

            if (itemUI != null) itemUI.SetKeyText("[ inputing... ]");
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

            UpdateManualElementText(element, "[ inputing... ]");
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
            // 리바인드 버튼을 마우스 클릭할 때의 클릭 이벤트가 바로 감지되어 닫히는 것을 방지
            yield return new WaitForSecondsRealtime(0.15f);

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                while (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed)
                {
                    yield return null;
                }
            }
#else
            while (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                yield return null;
            }
#endif

            while (isRebinding)
            {
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    foreach (Key k in Enum.GetValues(typeof(Key)))
                    {
                        if (k == Key.None) continue;
                        if (Keyboard.current[k].wasPressedThisFrame)
                        {
                            KeyCode legacyKey = HSH_KeyBindingManager.InputKeyToKeyCode(k);
                            if (legacyKey != KeyCode.None)
                            {
                                CompleteKeyboardRebinding(legacyKey);
                                yield break;
                            }
                        }
                    }
                }

                if (Mouse.current != null)
                {
                    if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        CompleteMouseRebinding(HWJ_InputMouseButton.Left);
                        yield break;
                    }
                    if (Mouse.current.rightButton.wasPressedThisFrame)
                    {
                        CompleteMouseRebinding(HWJ_InputMouseButton.Right);
                        yield break;
                    }
                    if (Mouse.current.middleButton.wasPressedThisFrame)
                    {
                        CompleteMouseRebinding(HWJ_InputMouseButton.Middle);
                        yield break;
                    }
                }
#endif

                // Legacy Input Manager (Fallback)
                for (int m = 0; m < 3; m++)
                {
                    if (Input.GetMouseButtonDown(m))
                    {
                        HWJ_InputMouseButton mouseBtn = (HWJ_InputMouseButton)(m + 1);
                        CompleteMouseRebinding(mouseBtn);
                        yield break;
                    }
                }

                if (Input.anyKeyDown)
                {
                    foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                    {
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
