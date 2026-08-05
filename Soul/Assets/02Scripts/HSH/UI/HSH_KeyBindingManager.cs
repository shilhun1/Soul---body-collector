using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HSH.UI
{
    /// <summary>
    /// 재설정 가능한 게임 액션 키 종류입니다.
    /// </summary>
    public enum HSH_KeyAction
    {
        Pause,       // 일시정지 (기본값: Escape)
        SkillTree,   // 스킬트리 (기본값: Tab)
        Interact,    // 상호작용 (기본값: F)
        MoveUp,      // 위/전진 (기본값: UpArrow / W)
        MoveDown,    // 아래/후진 (기본값: DownArrow / S)
        MoveLeft,    // 좌 (기본값: LeftArrow / A)
        MoveRight    // 우 (기본값: RightArrow / D)
    }

    /// <summary>
    /// 게임 전반의 단축키 바인딩 및 변경(Rebinding)을 관리하는 싱글톤 매니저 스크립트입니다.
    /// HSH_KeyBindingDataSO와 연동되며, HWJ_PlayerInputSystem 정보를 읽어오고/적용시킵니다.
    /// </summary>
    public class HSH_KeyBindingManager : MonoBehaviour
    {
        public static HSH_KeyBindingManager Instance { get; private set; }

        private const string PREFS_KEY_PREFIX = "HSH_KeyBinding_";

        [Header("ScriptableObject 키 바인딩 에셋 데이터")]
        [SerializeField] private HSH_KeyBindingDataSO bindingDataSO;

        private Dictionary<HSH_KeyAction, KeyCode> keyBindings = new Dictionary<HSH_KeyAction, KeyCode>();
        private Dictionary<HSH_KeyAction, KeyCode> defaultBindings = new Dictionary<HSH_KeyAction, KeyCode>();
        private Dictionary<HWJ_PlayerInputActionId, KeyCode> hwjKeyBindings = new Dictionary<HWJ_PlayerInputActionId, KeyCode>();
        private Dictionary<HWJ_PlayerInputActionId, HWJ_InputMouseButton> hwjMouseBindings = new Dictionary<HWJ_PlayerInputActionId, HWJ_InputMouseButton>();

        // 키 바인딩이 변경될 때 발생하는 이벤트 (ActionEnum, NewKeyCode)
        public event Action<HSH_KeyAction, KeyCode> OnKeyBindingChanged;
        public event Action OnAnyBindingChanged;

        public HSH_KeyBindingDataSO BindingDataSO => bindingDataSO;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            EnsureBindingDataSO();
            InitDefaultBindings();
            LoadKeyBindings();
        }

        private void EnsureBindingDataSO()
        {
            if (bindingDataSO == null)
            {
                bindingDataSO = ScriptableObject.CreateInstance<HSH_KeyBindingDataSO>();
                bindingDataSO.InitDefaultEntries();
            }
        }

        private void InitDefaultBindings()
        {
            defaultBindings.Clear();

            if (bindingDataSO != null && bindingDataSO.BindingEntries != null)
            {
                foreach (var entry in bindingDataSO.BindingEntries)
                {
                    if (!entry.isHWJAction)
                    {
                        defaultBindings[entry.keyAction] = entry.defaultKey;
                    }
                    else
                    {
                        // HWJ 액션 중 HSH_KeyAction과 매핑되는 항목 설정
                        switch (entry.hwjActionId)
                        {
                            case HWJ_PlayerInputActionId.Interact:
                                defaultBindings[HSH_KeyAction.Interact] = entry.defaultKey;
                                break;
                            case HWJ_PlayerInputActionId.MoveUp:
                                defaultBindings[HSH_KeyAction.MoveUp] = entry.defaultKey;
                                break;
                            case HWJ_PlayerInputActionId.MoveDown:
                                defaultBindings[HSH_KeyAction.MoveDown] = entry.defaultKey;
                                break;
                            case HWJ_PlayerInputActionId.MoveLeft:
                                defaultBindings[HSH_KeyAction.MoveLeft] = entry.defaultKey;
                                break;
                            case HWJ_PlayerInputActionId.MoveRight:
                                defaultBindings[HSH_KeyAction.MoveRight] = entry.defaultKey;
                                break;
                        }
                    }
                }
            }

            // 폴백 기본값 보장
            if (!defaultBindings.ContainsKey(HSH_KeyAction.Pause)) defaultBindings[HSH_KeyAction.Pause] = KeyCode.Escape;
            if (!defaultBindings.ContainsKey(HSH_KeyAction.SkillTree)) defaultBindings[HSH_KeyAction.SkillTree] = KeyCode.Tab;
            if (!defaultBindings.ContainsKey(HSH_KeyAction.Interact)) defaultBindings[HSH_KeyAction.Interact] = KeyCode.F;
            if (!defaultBindings.ContainsKey(HSH_KeyAction.MoveUp)) defaultBindings[HSH_KeyAction.MoveUp] = KeyCode.W;
            if (!defaultBindings.ContainsKey(HSH_KeyAction.MoveDown)) defaultBindings[HSH_KeyAction.MoveDown] = KeyCode.S;
            if (!defaultBindings.ContainsKey(HSH_KeyAction.MoveLeft)) defaultBindings[HSH_KeyAction.MoveLeft] = KeyCode.A;
            if (!defaultBindings.ContainsKey(HSH_KeyAction.MoveRight)) defaultBindings[HSH_KeyAction.MoveRight] = KeyCode.D;
        }

        /// <summary>
        /// PlayerPrefs에 저장된 키 바인딩을 불러옵니다.
        /// </summary>
        public void LoadKeyBindings()
        {
            keyBindings.Clear();
            hwjKeyBindings.Clear();
            hwjMouseBindings.Clear();

            foreach (HSH_KeyAction action in Enum.GetValues(typeof(HSH_KeyAction)))
            {
                string prefKey = PREFS_KEY_PREFIX + action.ToString();
                KeyCode defaultKey = defaultBindings.ContainsKey(action) ? defaultBindings[action] : KeyCode.None;

                if (PlayerPrefs.HasKey(prefKey))
                {
                    string savedKeyStr = PlayerPrefs.GetString(prefKey);
                    if (Enum.TryParse(savedKeyStr, out KeyCode parsedKey))
                    {
                        keyBindings[action] = parsedKey;
                    }
                    else
                    {
                        keyBindings[action] = defaultKey;
                    }
                }
                else
                {
                    keyBindings[action] = defaultKey;
                }
            }

            foreach (HWJ_PlayerInputActionId hwjAction in Enum.GetValues(typeof(HWJ_PlayerInputActionId)))
            {
                string prefKeyKey = PREFS_KEY_PREFIX + "HWJ_Key_" + hwjAction.ToString();
                string prefMouseKey = PREFS_KEY_PREFIX + "HWJ_Mouse_" + hwjAction.ToString();

                if (PlayerPrefs.HasKey(prefKeyKey))
                {
                    if (Enum.TryParse(PlayerPrefs.GetString(prefKeyKey), out KeyCode parsedKey))
                    {
                        hwjKeyBindings[hwjAction] = parsedKey;
                    }
                }

                if (PlayerPrefs.HasKey(prefMouseKey))
                {
                    if (Enum.TryParse(PlayerPrefs.GetString(prefMouseKey), out HWJ_InputMouseButton parsedMouse))
                    {
                        hwjMouseBindings[hwjAction] = parsedMouse;
                    }
                }
            }
        }

        /// <summary>
        /// 씬 내 HWJ_PlayerInputSystem을 찾습니다.
        /// </summary>
        public HWJ_PlayerInputSystem GetHWJPlayerInputSystem()
        {
            if (HWJ_GameAccess.HasManager && HWJ_GameAccess.PlayerInput != null)
            {
                return HWJ_GameAccess.PlayerInput;
            }
            return FindObjectOfType<HWJ_PlayerInputSystem>();
        }

        /// <summary>
        /// 특정 액션의 현재 KeyCode를 반환합니다 (HWJ 인풋 시스템 정보 연동).
        /// </summary>
        public KeyCode GetKey(HSH_KeyAction action)
        {
            // HWJ_PlayerInputSystem 연동 확인
            HWJ_PlayerInputSystem hwjInput = GetHWJPlayerInputSystem();
            if (hwjInput != null && bindingDataSO != null)
            {
                if (bindingDataSO.TryGetEntry(action, out var entry) && entry.isHWJAction)
                {
                    if (hwjInput.TryGetEffectiveBinding(entry.hwjActionId, out var binding))
                    {
                        if (binding.HasKeyboard && binding.KeyboardKey != KeyCode.None)
                        {
                            return binding.KeyboardKey;
                        }
                    }
                }
            }

            if (keyBindings.TryGetValue(action, out KeyCode key))
            {
                return key;
            }
            return defaultBindings.ContainsKey(action) ? defaultBindings[action] : KeyCode.None;
        }

        /// <summary>
        /// HWJ 액션의 현재 표시 텍스트를 반환합니다.
        /// </summary>
        public string GetHWJBindingDisplayText(HWJ_PlayerInputActionId hwjActionId)
        {
            HWJ_PlayerInputSystem hwjInput = GetHWJPlayerInputSystem();
            if (hwjInput != null && hwjInput.TryGetEffectiveBinding(hwjActionId, out var binding))
            {
                if (binding.HasMouse) return $"Mouse {binding.MouseButton}";
                if (binding.HasKeyboard && binding.KeyboardKey != KeyCode.None) return binding.KeyboardKey.ToString();
            }

            if (hwjMouseBindings.TryGetValue(hwjActionId, out var mouseBtn) && mouseBtn != HWJ_InputMouseButton.None)
            {
                return $"Mouse {mouseBtn}";
            }

            if (hwjKeyBindings.TryGetValue(hwjActionId, out var key) && key != KeyCode.None)
            {
                return key.ToString();
            }

            if (bindingDataSO != null && bindingDataSO.TryGetEntry(hwjActionId, out var entry))
            {
                if (entry.defaultMouseButton != HWJ_InputMouseButton.None) return $"Mouse {entry.defaultMouseButton}";
                if (entry.defaultKey != KeyCode.None) return entry.defaultKey.ToString();
            }

            return "None";
        }

        /// <summary>
        /// 특정 액션의 KeyCode를 변경하고 PlayerPrefs 및 HWJ 인풋 시스템에 적용합니다.
        /// </summary>
        public void SetKey(HSH_KeyAction action, KeyCode newKey)
        {
            keyBindings[action] = newKey;
            string prefKey = PREFS_KEY_PREFIX + action.ToString();
            PlayerPrefs.SetString(prefKey, newKey.ToString());
            PlayerPrefs.Save();

            // HWJ PlayerInputSystem에 적용
            HWJ_PlayerInputSystem hwjInput = GetHWJPlayerInputSystem();
            if (hwjInput != null && bindingDataSO != null)
            {
                if (bindingDataSO.TryGetEntry(action, out var entry) && entry.isHWJAction)
                {
                    hwjInput.TrySetKeyboardBinding(entry.hwjActionId, newKey);
                }
            }

            Debug.Log($"[HSH_KeyBindingManager] {action} 키가 {newKey}(으)로 변경되었습니다.");
            OnKeyBindingChanged?.Invoke(action, newKey);
            OnAnyBindingChanged?.Invoke();
        }

        /// <summary>
        /// HWJ 전용 인풋 액션 키를 바인딩하고 HWJ 시스템 및 PlayerPrefs에 적용합니다.
        /// </summary>
        public bool SetHWJKeyboardBinding(HWJ_PlayerInputActionId hwjActionId, KeyCode newKey)
        {
            hwjKeyBindings[hwjActionId] = newKey;
            string prefKey = PREFS_KEY_PREFIX + "HWJ_Key_" + hwjActionId.ToString();
            PlayerPrefs.SetString(prefKey, newKey.ToString());
            PlayerPrefs.Save();

            if (bindingDataSO != null && bindingDataSO.TryGetEntry(hwjActionId, out var entry))
            {
                if (entry.keyAction != default)
                {
                    keyBindings[entry.keyAction] = newKey;
                    PlayerPrefs.SetString(PREFS_KEY_PREFIX + entry.keyAction.ToString(), newKey.ToString());
                    PlayerPrefs.Save();
                    OnKeyBindingChanged?.Invoke(entry.keyAction, newKey);
                }
            }

            HWJ_PlayerInputSystem hwjInput = GetHWJPlayerInputSystem();
            if (hwjInput != null)
            {
                hwjInput.TrySetKeyboardBinding(hwjActionId, newKey);
            }

            Debug.Log($"[HSH_KeyBindingManager] HWJ {hwjActionId} 키가 {newKey}(으)로 변경되었습니다.");
            OnAnyBindingChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// HWJ 전용 마우스 입력 버튼을 바인딩하고 HWJ 시스템 및 PlayerPrefs에 적용합니다.
        /// </summary>
        public bool SetHWJMouseBinding(HWJ_PlayerInputActionId hwjActionId, HWJ_InputMouseButton mouseButton)
        {
            hwjMouseBindings[hwjActionId] = mouseButton;
            string prefKey = PREFS_KEY_PREFIX + "HWJ_Mouse_" + hwjActionId.ToString();
            PlayerPrefs.SetString(prefKey, mouseButton.ToString());
            PlayerPrefs.Save();

            HWJ_PlayerInputSystem hwjInput = GetHWJPlayerInputSystem();
            if (hwjInput != null)
            {
                hwjInput.TrySetMouseBinding(hwjActionId, mouseButton);
            }

            Debug.Log($"[HSH_KeyBindingManager] HWJ {hwjActionId} 마우스가 {mouseButton}(으)로 변경되었습니다.");
            OnAnyBindingChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 모든 키 바인딩을 기본값으로 복원합니다.
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (var kvp in defaultBindings)
            {
                SetKey(kvp.Key, kvp.Value);
            }

            HWJ_PlayerInputSystem hwjInput = GetHWJPlayerInputSystem();
            if (hwjInput != null)
            {
                hwjInput.ResetRuntimeBindings();
            }

            OnAnyBindingChanged?.Invoke();
        }

        /// <summary>
        /// KeyCode를 UnityEngine.InputSystem.Key로 변환합니다.
        /// </summary>
        public Key GetInputSystemKey(HSH_KeyAction action)
        {
            KeyCode code = GetKey(action);
            return KeyCodeToInputKey(code);
        }

        /// <summary>
        /// Legacy KeyCode를 InputSystem Key로 변환해주는 헬퍼
        /// </summary>
        public static Key KeyCodeToInputKey(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.Alpha8: return Key.Digit8;
                case KeyCode.Alpha9: return Key.Digit9;
                default: return Key.None;
            }
        }

        /// <summary>
        /// InputSystem Key를 Legacy KeyCode로 변환해주는 헬퍼
        /// </summary>
        public static KeyCode InputKeyToKeyCode(Key key)
        {
            switch (key)
            {
                case Key.A: return KeyCode.A;
                case Key.B: return KeyCode.B;
                case Key.C: return KeyCode.C;
                case Key.D: return KeyCode.D;
                case Key.E: return KeyCode.E;
                case Key.F: return KeyCode.F;
                case Key.G: return KeyCode.G;
                case Key.H: return KeyCode.H;
                case Key.I: return KeyCode.I;
                case Key.J: return KeyCode.J;
                case Key.K: return KeyCode.K;
                case Key.L: return KeyCode.L;
                case Key.M: return KeyCode.M;
                case Key.N: return KeyCode.N;
                case Key.O: return KeyCode.O;
                case Key.P: return KeyCode.P;
                case Key.Q: return KeyCode.Q;
                case Key.R: return KeyCode.R;
                case Key.S: return KeyCode.S;
                case Key.T: return KeyCode.T;
                case Key.U: return KeyCode.U;
                case Key.V: return KeyCode.V;
                case Key.W: return KeyCode.W;
                case Key.X: return KeyCode.X;
                case Key.Y: return KeyCode.Y;
                case Key.Z: return KeyCode.Z;
                case Key.Space: return KeyCode.Space;
                case Key.Tab: return KeyCode.Tab;
                case Key.Escape: return KeyCode.Escape;
                case Key.LeftShift: return KeyCode.LeftShift;
                case Key.RightShift: return KeyCode.RightShift;
                case Key.LeftCtrl: return KeyCode.LeftControl;
                case Key.RightCtrl: return KeyCode.RightControl;
                case Key.LeftAlt: return KeyCode.LeftAlt;
                case Key.RightAlt: return KeyCode.RightAlt;
                case Key.UpArrow: return KeyCode.UpArrow;
                case Key.DownArrow: return KeyCode.DownArrow;
                case Key.LeftArrow: return KeyCode.LeftArrow;
                case Key.RightArrow: return KeyCode.RightArrow;
                case Key.Digit0: return KeyCode.Alpha0;
                case Key.Digit1: return KeyCode.Alpha1;
                case Key.Digit2: return KeyCode.Alpha2;
                case Key.Digit3: return KeyCode.Alpha3;
                case Key.Digit4: return KeyCode.Alpha4;
                case Key.Digit5: return KeyCode.Alpha5;
                case Key.Digit6: return KeyCode.Alpha6;
                case Key.Digit7: return KeyCode.Alpha7;
                case Key.Digit8: return KeyCode.Alpha8;
                case Key.Digit9: return KeyCode.Alpha9;
                default: return KeyCode.None;
            }
        }
    }
}
