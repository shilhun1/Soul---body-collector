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
    /// KeyCode 기반 및 UnityEngine.InputSystem.Key 호환 변환을 지원하며 PlayerPrefs에 자동 저장됩니다.
    /// </summary>
    public class HSH_KeyBindingManager : MonoBehaviour
    {
        public static HSH_KeyBindingManager Instance { get; private set; }

        private const string PREFS_KEY_PREFIX = "HSH_KeyBinding_";

        private Dictionary<HSH_KeyAction, KeyCode> keyBindings = new Dictionary<HSH_KeyAction, KeyCode>();
        private Dictionary<HSH_KeyAction, KeyCode> defaultBindings = new Dictionary<HSH_KeyAction, KeyCode>();

        // 키 바인딩이 변경될 때 발생하는 이벤트 (ActionEnum, NewKeyCode)
        public event Action<HSH_KeyAction, KeyCode> OnKeyBindingChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitDefaultBindings();
            LoadKeyBindings();
        }

        private void InitDefaultBindings()
        {
            defaultBindings[HSH_KeyAction.Pause] = KeyCode.Escape;
            defaultBindings[HSH_KeyAction.SkillTree] = KeyCode.Tab;
            defaultBindings[HSH_KeyAction.Interact] = KeyCode.F;
            defaultBindings[HSH_KeyAction.MoveUp] = KeyCode.W;
            defaultBindings[HSH_KeyAction.MoveDown] = KeyCode.S;
            defaultBindings[HSH_KeyAction.MoveLeft] = KeyCode.A;
            defaultBindings[HSH_KeyAction.MoveRight] = KeyCode.D;
        }

        /// <summary>
        /// PlayerPrefs에 저장된 키 바인딩을 불러옵니다.
        /// </summary>
        public void LoadKeyBindings()
        {
            keyBindings.Clear();

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
        }

        /// <summary>
        /// 특정 액션의 현재 KeyCode를 반환합니다.
        /// </summary>
        public KeyCode GetKey(HSH_KeyAction action)
        {
            if (keyBindings.TryGetValue(action, out KeyCode key))
            {
                return key;
            }
            return defaultBindings.ContainsKey(action) ? defaultBindings[action] : KeyCode.None;
        }

        /// <summary>
        /// 특정 액션의 KeyCode를 변경하고 PlayerPrefs에 저장합니다.
        /// </summary>
        public void SetKey(HSH_KeyAction action, KeyCode newKey)
        {
            keyBindings[action] = newKey;
            string prefKey = PREFS_KEY_PREFIX + action.ToString();
            PlayerPrefs.SetString(prefKey, newKey.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[HSH_KeyBindingManager] {action} 키가 {newKey}(으)로 변경되었습니다.");
            OnKeyBindingChanged?.Invoke(action, newKey);
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
    }
}
