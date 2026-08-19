using UnityEngine;
using SmilingEclipse.STMImporter;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HSH.UI
{
    /// <summary>
    /// 단축키(기본값: Tab)를 입력받아 스킬 트리 UI를 토글하며, 
    /// 스킬 트리가 열려있는 동안 게임 시간을 일시 정지(Time.timeScale = 0)시키는 컴포넌트입니다.
    /// </summary>
    public class HSH_SkillTreeToggleUI : MonoBehaviour
    {
        [Header("단축키 설정")]
        [Tooltip("스킬 트리를 켜고 끌 단축키 (인스펙터에서 변경 가능)")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        [Header("UI 대상")]
        [Tooltip("토글할 스킬 트리 Canvas 패널 오브젝트")]
        [SerializeField] private GameObject skillTreePanel;

        [Header("시간 및 조작 설정")]
        [Tooltip("스킬 트리가 열려있는 동안 게임 시간(Time.timeScale)을 정지할지 여부")]
        [SerializeField] private bool pauseTimeWhenOpen = true;

        [Tooltip("스킬 트리가 열릴 때 마우스 커서를 표시하고 해제할지 여부")]
        [SerializeField] private bool unlockCursorWhenOpen = true;

        [Header("스킬 트리 컨트롤러 (선택 사항)")]
        [Tooltip("열릴 때 정보 갱신을 위해 씬 내 SkillTreeController를 참조합니다.")]
        [SerializeField] private SkillTreeController skillTreeController;

        private float savedTimeScale = 1f;
        private CursorLockMode savedCursorLockState = CursorLockMode.None;
        private bool savedCursorVisibility = true;
        private bool isCurrentlyOpen = false;

        public KeyCode ToggleKey
        {
            get
            {
                if (HSH_KeyBindingManager.Instance != null)
                {
                    return HSH_KeyBindingManager.Instance.GetKey(HSH_KeyAction.SkillTree);
                }
                return toggleKey;
            }
            set => toggleKey = value;
        }

        public bool IsOpen => isCurrentlyOpen;

        private void Awake()
        {
            // 시작 시 스킬 트리 패널의 상태를 확인하여 초기화
            if (skillTreePanel != null)
            {
                isCurrentlyOpen = skillTreePanel.activeSelf;
            }

            if (skillTreeController == null)
            {
                skillTreeController = FindFirstObjectByType<SkillTreeController>();
            }
            CloseSkillTree();
        }

        private void Update()
        {
            KeyCode currentToggleKey = ToggleKey;
            if (WasKeyPressedThisFrame(currentToggleKey))
            {
                ToggleSkillTree();
            }
        }

        private bool WasKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key inputKey = HSH_KeyBindingManager.KeyCodeToInputKey(keyCode);
                if (inputKey != Key.None && Keyboard.current[inputKey].wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif
            return Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// 스킬 트리 UI 상태를 토글합니다.
        /// </summary>
        public void ToggleSkillTree()
        {
            if (isCurrentlyOpen)
            {
                CloseSkillTree();
            }
            else
            {
                OpenSkillTree();
            }
        }

        /// <summary>
        /// 스킬 트리를 엽니다.
        /// </summary>
        public void OpenSkillTree()
        {
            if (skillTreePanel == null)
            {
                Debug.LogWarning("[HSH_SkillTreeToggleUI] 연결된 skillTreePanel이 없습니다!");
                return;
            }

            if (isCurrentlyOpen) return;

            skillTreePanel.SetActive(true);
            isCurrentlyOpen = true;

            // 게임 시간 일시 정지
            if (pauseTimeWhenOpen)
            {
                savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }

            // 마우스 커서 해제
            if (unlockCursorWhenOpen)
            {
                savedCursorLockState = Cursor.lockState;
                savedCursorVisibility = Cursor.visible;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 스킬 트리 컨트롤러 UI 및 포인트 최신화
            var saveManager = HSH_SkillSaveManager.Instance ?? FindFirstObjectByType<HSH_SkillSaveManager>();
            if (saveManager != null)
            {
                saveManager.LoadSkillData();
            }
            else if (skillTreeController != null)
            {
                var levelUpSystem = FindFirstObjectByType<HWJ_LevelUpSystem>();
                if (levelUpSystem != null && skillTreeController.skillPoints != null)
                {
                    skillTreeController.skillPoints.Points = levelUpSystem.SkillPoint;
                }
                skillTreeController.StartCoroutine(skillTreeController.Load());
            }
        }

        /// <summary>
        /// 스킬 트리를 닫습니다.
        /// </summary>
        public void CloseSkillTree()
        {
            if (skillTreePanel == null || !isCurrentlyOpen) return;

            skillTreePanel.SetActive(false);
            isCurrentlyOpen = false;

            // 게임 시간 복원
            if (pauseTimeWhenOpen)
            {
                Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            }

            // 마우스 커서 복원
            if (unlockCursorWhenOpen)
            {
                Cursor.lockState = savedCursorLockState;
                Cursor.visible = savedCursorVisibility;
            }
        }

        private void OnDisable()
        {
            // 오브젝트가 비활성화되거나 씬 전환 시 시간 멈춤 현상 방지 안전장치
            if (isCurrentlyOpen && pauseTimeWhenOpen)
            {
                Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
                isCurrentlyOpen = false;
            }
        }
    }
}
