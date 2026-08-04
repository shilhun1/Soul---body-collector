using System;
using System.Collections.Generic;
using UnityEngine;

namespace HSH.UI
{
    [Serializable]
    public class HSH_KeyBindingConfigEntry
    {
        [Tooltip("HSH 고유 액션 종류 (HSH 전용인 경우 지정)")]
        public HSH_KeyAction keyAction;

        [Tooltip("HWJ 인풋 액션과의 연동 여부")]
        public bool isHWJAction;

        [Tooltip("연동할 HWJ 액션 ID")]
        public HWJ_PlayerInputActionId hwjActionId;

        [Tooltip("UI 화면에 표시될 명칭 (예: '상호작용', '점프', '일시정지')")]
        public string displayName;

        [Tooltip("기본 키보드 KeyCode")]
        public KeyCode defaultKey = KeyCode.None;

        [Tooltip("기본 마우스 버튼 (HWJ 마우스 액션 전용)")]
        public HWJ_InputMouseButton defaultMouseButton = HWJ_InputMouseButton.None;
    }

    /// <summary>
    /// HSH 키 바인딩 설정 및 HWJ 인풋 시스템 연동 항목 정보를 들고 있는 ScriptableObject 에셋 스크립트입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "HSH_KeyBindingData", menuName = "HSH/UI/Key Binding Data")]
    public class HSH_KeyBindingDataSO : ScriptableObject
    {
        [Header("키 바인딩 기본 설정 항목들")]
        [SerializeField]
        private List<HSH_KeyBindingConfigEntry> bindingEntries = new List<HSH_KeyBindingConfigEntry>();

        public IReadOnlyList<HSH_KeyBindingConfigEntry> BindingEntries => bindingEntries;

        private void Reset()
        {
            InitDefaultEntries();
        }

        public void InitDefaultEntries()
        {
            bindingEntries = new List<HSH_KeyBindingConfigEntry>
            {
                // HSH 고유 액션
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.Pause, isHWJAction = false, displayName = "일시정지", defaultKey = KeyCode.Escape },
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.SkillTree, isHWJAction = false, displayName = "스킬트리", defaultKey = KeyCode.Tab },

                // HSH & HWJ 공통 및 HWJ 확장 액션
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.Interact, isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.Interact, displayName = "상호작용", defaultKey = KeyCode.F },
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.MoveUp, isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.MoveUp, displayName = "위로 이동 (W)", defaultKey = KeyCode.W },
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.MoveDown, isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.MoveDown, displayName = "아래로 이동 (S)", defaultKey = KeyCode.S },
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.MoveLeft, isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.MoveLeft, displayName = "왼쪽 이동 (A)", defaultKey = KeyCode.A },
                new HSH_KeyBindingConfigEntry { keyAction = HSH_KeyAction.MoveRight, isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.MoveRight, displayName = "오른쪽 이동 (D)", defaultKey = KeyCode.D },

                // HWJ 추가 플레이어 조작 액션
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.Jump, displayName = "점프 (Space)", defaultKey = KeyCode.Space },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.Dash, displayName = "대시 (Shift)", defaultKey = KeyCode.LeftShift },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.Attack, displayName = "공격 (마우스 좌클릭)", defaultKey = KeyCode.None, defaultMouseButton = HWJ_InputMouseButton.Left },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.ExitPossession, displayName = "빙의 해제 (Q)", defaultKey = KeyCode.Q },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.SkillSlot1, displayName = "스킬 슬롯 1", defaultKey = KeyCode.Alpha1 },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.SkillSlot2, displayName = "스킬 슬롯 2", defaultKey = KeyCode.Alpha2 },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.SkillSlot3, displayName = "스킬 슬롯 3", defaultKey = KeyCode.Alpha3 },
                new HSH_KeyBindingConfigEntry { isHWJAction = true, hwjActionId = HWJ_PlayerInputActionId.SkillSlot4, displayName = "스킬 슬롯 4", defaultKey = KeyCode.Alpha4 }
            };
        }

        public bool TryGetEntry(HSH_KeyAction action, out HSH_KeyBindingConfigEntry entry)
        {
            foreach (var item in bindingEntries)
            {
                if (!item.isHWJAction && item.keyAction == action)
                {
                    entry = item;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        public bool TryGetEntry(HWJ_PlayerInputActionId hwjActionId, out HSH_KeyBindingConfigEntry entry)
        {
            foreach (var item in bindingEntries)
            {
                if (item.isHWJAction && item.hwjActionId == hwjActionId)
                {
                    entry = item;
                    return true;
                }
            }
            entry = null;
            return false;
        }
    }
}
