using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// SoulSystem 또는 RuntimeStatusSystem의 사망 상태를 감지해서 게임오버 창을 표시하는 기본 시스템입니다.
/// 실제 버튼 동작과 씬 전환은 UI 프리팹 쪽 버튼 이벤트에서 GameOverDataSO.RestartSceneName을 사용해 연결합니다.
/// </summary>
public class HWJ_GameOverWindowSystem : MonoBehaviour
{
    [SerializeField] private HWJ_GameOverDataSO gameOverData;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private GameObject windowRoot;

    private bool isShown;

    private void Awake()
    {
        ResolveReferences();

        if (windowRoot != null)
        {
            windowRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (isShown)
        {
            return;
        }

        ResolveReferences();

        bool soulDead = soulSystem != null
            && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead
            && IsSpiritMentalDepleted();
        bool legacyStatusDead = soulSystem == null && runtimeStatus != null && runtimeStatus.IsDead;

        if (soulDead || legacyStatusDead)
        {
            Show();
        }
    }

    private bool IsSpiritMentalDepleted()
    {
        ResolveReferences();
        return runtimeStatus != null && runtimeStatus.CurrentSpiritMentalValue <= 0f;
    }

    private void ResolveReferences()
    {
        if (runtimeStatus == null && soulSystem != null)
        {
            runtimeStatus = soulSystem.GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (soulSystem == null && runtimeStatus != null)
        {
            soulSystem = runtimeStatus.GetComponent<HWJ_SoulSystem>();
        }

        if (runtimeStatus == null && HWJ_GameAccess.HasManager)
        {
            runtimeStatus = HWJ_GameAccess.Manager.PlayerStatus;
        }

        if (soulSystem == null && HWJ_GameAccess.HasManager)
        {
            soulSystem = HWJ_GameAccess.Manager.PlayerSoul;
        }
    }

    /// <summary>
    /// 게임오버 창을 표시합니다.
    /// windowRoot가 직접 지정되어 있으면 활성화하고, 없으면 GameOverDataSO의 프리팹을 생성합니다.
    /// </summary>
    public void Show()
    {
        isShown = true;

        if (windowRoot != null)
        {
            windowRoot.SetActive(true);
            return;
        }

        if (gameOverData != null && gameOverData.WindowPrefab != null)
        {
            windowRoot = objectPool != null
                ? objectPool.Spawn(gameOverData.WindowPrefab, Vector3.zero, Quaternion.identity)
                : HWJ_GameAccess.Spawn(gameOverData.WindowPrefab, Vector3.zero, Quaternion.identity);

            if (windowRoot == null)
            {
                windowRoot = Instantiate(gameOverData.WindowPrefab, Vector3.zero, Quaternion.identity);
            }
        }
    }
}

/// <summary>
/// GameOver UI 프리팹 안에서 버튼 입력을 처리합니다.
/// GameOverWindowSystem은 죽음 감지와 창 표시만 담당하고, 재시작 같은 UI 입력은 이 컴포넌트가 담당합니다.
/// </summary>
public class HWJ_GameOverUiInputSystem : MonoBehaviour
{
    [SerializeField] private HWJ_GameOverDataSO gameOverData;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        CacheButtons();
    }

    private void OnEnable()
    {
        CacheButtons();
        AddButtonListeners();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
    }

    public void SetGameOverData(HWJ_GameOverDataSO data)
    {
        gameOverData = data;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        string sceneName = gameOverData != null ? gameOverData.RestartSceneName : null;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            sceneName = SceneManager.GetActiveScene().name;
        }

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private void AddButtonListeners()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(Restart);
            restartButton.onClick.AddListener(Restart);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void RemoveButtonListeners()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(Restart);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    private void CacheButtons()
    {
        if (restartButton == null)
        {
            Transform restartTransform = transform.Find("RetryButton");

            if (restartTransform != null)
            {
                restartButton = restartTransform.GetComponent<Button>();
            }
        }

        if (quitButton == null)
        {
            Transform quitTransform = transform.Find("QuitButton");

            if (quitTransform != null)
            {
                quitButton = quitTransform.GetComponent<Button>();
            }
        }
    }
}

/// <summary>
/// 시작 타이틀 UI 프리팹에서 시작/종료 버튼을 처리합니다.
/// 첫 게임플레이 씬 이름이 비어 있으면 씬 전환 없이 타이틀 화면만 닫아 테스트 씬에서도 사용할 수 있습니다.
/// </summary>
public class HWJ_TitleScreenWindowSystem : MonoBehaviour
{
    [SerializeField] private HWJ_TitleScreenDataSO titleScreenData;
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private bool showOnEnable = true;

    private float previousTimeScale = 1f;
    private bool isShown;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        AddButtonListeners();

        if (showOnEnable)
        {
            Show();
        }
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
        RestoreTimeScale();
    }

    public void SetTitleScreenData(HWJ_TitleScreenDataSO data)
    {
        titleScreenData = data;
    }

    public void Show()
    {
        if (isShown)
        {
            return;
        }

        isShown = true;

        if (windowRoot != null)
        {
            windowRoot.SetActive(true);
        }

        if (titleScreenData != null && titleScreenData.PauseGameWhileShown)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void StartGame()
    {
        Hide();
        string sceneName = titleScreenData != null ? titleScreenData.FirstGameplaySceneName : null;

        if (!string.IsNullOrWhiteSpace(sceneName) && SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void QuitGame()
    {
        RestoreTimeScale();
        Application.Quit();
    }

    public void Hide()
    {
        if (!isShown)
        {
            return;
        }

        isShown = false;

        if (windowRoot != null)
        {
            windowRoot.SetActive(false);
        }

        RestoreTimeScale();
    }

    private void AddButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void RemoveButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    private void RestoreTimeScale()
    {
        if (titleScreenData != null && titleScreenData.PauseGameWhileShown)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }
    }

    private void CacheReferences()
    {
        if (windowRoot == null)
        {
            windowRoot = gameObject;
        }

        if (startButton == null)
        {
            Transform startTransform = transform.Find("StartButton");

            if (startTransform != null)
            {
                startButton = startTransform.GetComponent<Button>();
            }
        }

        if (quitButton == null)
        {
            Transform quitTransform = transform.Find("QuitButton");

            if (quitTransform != null)
            {
                quitButton = quitTransform.GetComponent<Button>();
            }
        }
    }
}
