using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HSH.UI
{
    /// <summary>
    /// 동적으로 인스턴스화되거나 인스펙터에서 바인딩되는 개별 키 변경 UI 항목(개체) 스크립트입니다.
    /// </summary>
    public class HSH_KeyRebindItemUI : MonoBehaviour
    {
        [Header("UI 구성 요소")]
        [Tooltip("액션 표시 이름 (TMP)")]
        [SerializeField] private TMP_Text actionNameTMP;
        [Tooltip("액션 표시 이름 (Legacy Text)")]
        [SerializeField] private Text actionNameLegacy;

        [Tooltip("현재 키 텍스트 (TMP)")]
        [SerializeField] private TMP_Text keyTextTMP;
        [Tooltip("현재 키 텍스트 (Legacy Text)")]
        [SerializeField] private Text keyTextLegacy;

        [Tooltip("키 바인딩 변경 시작 버튼")]
        [SerializeField] private Button rebindButton;

        private HSH_KeyBindingConfigEntry configEntry;
        private Action<HSH_KeyRebindItemUI> onRebindRequested;

        public HSH_KeyBindingConfigEntry ConfigEntry => configEntry;
        public Button RebindButton => rebindButton;

        public void Setup(HSH_KeyBindingConfigEntry entry, Action<HSH_KeyRebindItemUI> rebindCallback)
        {
            configEntry = entry;
            onRebindRequested = rebindCallback;

            string displayName = entry != null ? entry.displayName : "알 수 없음";
            SetActionName(displayName);

            if (rebindButton != null)
            {
                rebindButton.onClick.RemoveAllListeners();
                rebindButton.onClick.AddListener(() => onRebindRequested?.Invoke(this));
            }
        }

        public void SetActionName(string title)
        {
            if (actionNameTMP != null) actionNameTMP.text = title;
            if (actionNameLegacy != null) actionNameLegacy.text = title;
        }

        public void SetKeyText(string keyDisplay)
        {
            if (keyTextTMP != null) keyTextTMP.text = keyDisplay;
            if (keyTextLegacy != null) keyTextLegacy.text = keyDisplay;
        }
    }
}
