using UnityEngine;
using UnityEngine.SceneManagement;

public class HSH_SceneMove : MonoBehaviour
{
    [Header("이동할 씬 이름")]
    public string targetSceneName;

    [Header("이동 후 스폰될 포인트의 ID (선택)")]
    public string targetSpawnID;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                // 타겟 스폰 ID를 저장한 뒤 씬 이동
                HSH_SceneTransfer.TargetSpawnID = targetSpawnID;
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning("이동할 씬의 이름이 설정되지 않았습니다!");
            }
        }
    }
}
