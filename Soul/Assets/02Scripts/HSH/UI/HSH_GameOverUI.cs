using UnityEngine;
using UnityEngine.SceneManagement; // 씬 다시 시작을 위해 필요

public class HSH_GameOverUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject gameOverPanel; // 게임오버 시 화면에 띄울 창 (패널)

    public static HSH_GameOverUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // 씬이 변경되어도 파괴되지 않도록 설정 (루트 오브젝트여야 작동합니다)
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 게임 시작 시 게임오버 창이 켜져있다면 숨기기
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 게임오버 창을 화면에 띄우고 게임을 일시정지합니다.
    /// 체력이 0이 되었을 때 호출됩니다.
    /// </summary>
    public void ShowGameOver()
    {
        
        if (gameOverPanel != null)
        {

            gameOverPanel.SetActive(true);
        }
        
        // 게임 내 시간을 멈춤 (캐릭터 이동, 애니메이션 등 정지)
        // Time.timeScale = 0f;
    }

    /// <summary>
    /// 다시 시작 버튼을 눌렀을 때 호출될 함수입니다.
    /// 버튼의 OnClick 이벤트에 연결해주세요.
    /// </summary>
    public void RestartGame()
    {
        // 멈췄던 시간을 다시 흐르게 되돌림
        Time.timeScale = 1f;
        
        // 현재 활성화된 씬을 처음부터 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 지정한 씬으로 이동하는 버튼 함수입니다.
    /// OnClick 이벤트에 연결 후, 빈칸(String)에 이동할 씬 이름을 직접 적어주세요.
    /// </summary>
    public void GoToTestScene(string targetSceneName)
    {
        // 멈췄던 시간을 다시 흐르게 되돌림
        Time.timeScale = 1f;
        
        // OnClick 칸에 직접 적은 씬 이름으로 이동
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 게임 종료 버튼을 눌렀을 때 호출될 함수입니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }
}
