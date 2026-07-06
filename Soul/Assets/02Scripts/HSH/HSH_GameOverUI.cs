using UnityEngine;
using UnityEngine.SceneManagement; // 씬 다시 시작을 위해 필요

public class HSH_GameOverUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject gameOverPanel; // 게임오버 시 화면에 띄울 창 (패널)

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
        Debug.Log("22222222");
        if (gameOverPanel != null)
        {
            Debug.Log("33333333");
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
    /// 게임 종료 버튼을 눌렀을 때 호출될 함수입니다.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }
}
