using UnityEngine;
using UnityEngine.SceneManagement;

public class HSH_SceneLoader : MonoBehaviour
{
    /// <summary>
    /// 버튼의 OnClick 이벤트에 이 함수를 연결하고, 매개변수로 이동할 씬의 이름을 문자열로 적어주면 됩니다.
    /// </summary>
    /// <param name="sceneName">이동할 씬의 이름 (Build Settings에 등록되어 있어야 함)</param>
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 씬의 인덱스 번호를 사용하여 이동할 경우 사용하는 함수입니다.
    /// </summary>
    /// <param name="sceneIndex">이동할 씬의 인덱스 (Build Settings에 등록되어 있어야 함)</param>
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
