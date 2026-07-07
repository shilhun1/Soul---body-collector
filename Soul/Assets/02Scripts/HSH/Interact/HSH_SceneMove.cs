using UnityEngine;
using UnityEngine.SceneManagement;

public class HSH_SceneMove : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    public string targetSceneName;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning("이동할 씬의 이름이 설정되지 않았습니다!");
            }
        }
    }
}
