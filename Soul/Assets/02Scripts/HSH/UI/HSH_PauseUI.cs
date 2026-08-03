using UnityEngine;
using UnityEngine.UI;

namespace HSH.UI
{
    /// <summary>
    /// ESC 키를 눌러 일시정지(Pause) UI 창을 띄우는 컨트롤러 스크립트입니다.
    /// - 계속하기 (Resume): 일시정지를 해제하고 게임을 재개합니다.
    /// - 설정 (Settings): 일시정지 창을 닫고/숨기고 설정 창(Settings Panel)을 표시합니다.
    /// - 게임 종료 (Quit): 게임을 종료합니다.
    /// </summary>
    public class HSH_PauseUI : MonoBehaviour
    {
        public static HSH_PauseUI Instance { get; private set; }

        [Header("단축키 설정")]
        [Tooltip("일시정지 창을 토글할 단축키 (기본값: ESC)")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        [Header("UI 패널 연결")]
        [Tooltip("일시정지 UI 패널 (계속하기, 설정, 게임 종료 버튼을 포함하는 패널)")]
        [SerializeField] private GameObject pausePanel;

        [Tooltip("설정 UI 패널 (설정 창)")]
        [SerializeField] private GameObject settingsPanel;

        [Header("일시정지 창 버튼 (선택 사항 - 자동으로 Listener 등록)")]
        [Tooltip("계속하기 버튼")]
        [SerializeField] private Button resumeButton;

        [Tooltip("설정 버튼")]
        [SerializeField] private Button settingsButton;

        [Tooltip("게임 종료 버튼")]
        [SerializeField] private Button quitButton;

        [Header("설정 창 버튼 (선택 사항)")]
        [Tooltip("설정 창에서 일시정지 메뉴로 돌아가는 뒤로가기/닫기 버튼")]
        [SerializeField] private Button settingsBackButton;

        [Header("시간 및 조작 설정")]
        [Tooltip("일시정지 중 게임 시간(Time.timeScale)을 0으로 멈출지 여부")]
        [SerializeField] private bool pauseTimeWhenOpen = true;

        [Tooltip("일시정지 시 마우스 커서를 자유롭게 해제할지 여부")]
        [SerializeField] private bool unlockCursorWhenOpen = true;

        private float savedTimeScale = 1f;
        private CursorLockMode savedCursorLockState = CursorLockMode.None;
        private bool savedCursorVisibility = true;
        private bool isPaused = false;

        public KeyCode PauseKey
        {
            get
            {
                if (HSH_KeyBindingManager.Instance != null)
                {
                    return HSH_KeyBindingManager.Instance.GetKey(HSH_KeyAction.Pause);
                }
                return pauseKey;
            }
            set => pauseKey = value;
        }

        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 버튼 클릭 이벤트 바인딩 (인스펙터에 연결된 경우)
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.AddListener(BackToPauseMenu);
            }
        }

        private void Start()
        {
            // 게임 시작 시 패널 초기화 (숨김)
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void Update()
        {
            KeyCode currentPauseKey = PauseKey;
            if (Input.GetKeyDown(currentPauseKey))
            {
                // 설정 창이 켜져 있는 상태에서 일시정지 키를 누르면 일시정지 메뉴로 복귀
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    BackToPauseMenu();
                }
                // 일시정지 창이 켜져 있는 상태에서 일시정지 키를 누르면 게임 재개
                else if (isPaused)
                {
                    ResumeGame();
                }
                // 게임 진행 중 일시정지 키를 누르면 일시정지
                else
                {
                    PauseGame();
                }
            }
        }

        /// <summary>
        /// 일시정지 상태를 토글합니다.
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /// <summary>
        /// 게임을 일시정지하고 일시정지 UI 창을 엽니다.
        /// </summary>
        public void PauseGame()
        {
            isPaused = true;

            // UI 활성화
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            // 게임 시간 정지
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
        }

        /// <summary>
        /// 일시정지를 해제하고 게임을 재개(계속하기)합니다.
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;

            // UI 비활성화
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

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

        /// <summary>
        /// 설정 버튼 클릭 시: 일시정지 창을 숨기고 설정 창을 표시하며 오디오 및 키바인딩 UI를 갱신합니다.
        /// </summary>
        public void OpenSettings()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);

                var audioUI = settingsPanel.GetComponentInChildren<HSH_AudioSettingsUI>();
                if (audioUI != null) audioUI.RefreshUI();

                var keyUI = settingsPanel.GetComponentInChildren<HSH_KeyRebindUI>();
                if (keyUI != null) keyUI.RefreshAllKeyTexts();
            }
        }

        /// <summary>
        /// 설정 창에서 닫기/뒤로가기 클릭 시: 설정 창을 숨기고 일시정지 창으로 돌아옵니다.
        /// </summary>
        public void BackToPauseMenu()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
        }

        /// <summary>
        /// 게임 종료 버튼 클릭 시 호출됩니다.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[HSH_PauseUI] 게임을 종료합니다.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDisable()
        {
            // 오브젝트 비활성화 또는 씬 전환 시 시간에 정지 상태로 남지 않도록 보장
            if (isPaused && pauseTimeWhenOpen)
            {
                Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
                isPaused = false;
            }
        }
    }
}
