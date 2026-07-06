using UnityEngine;

/// <summary>
/// 게임오버 창을 띄울 때 필요한 UI 프리팹과 메시지를 관리하는 ScriptableObject입니다.
/// GameOverWindowSystem이 Dead 상태를 감지했을 때 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_GameOverData", menuName = "HWJ/Data/System/Game Over")]
public class HWJ_GameOverDataSO : ScriptableObject
{
    [SerializeField] private string gameOverId;
    [SerializeField] private GameObject windowPrefab;
    [SerializeField] private string titleText = "Game Over";
    [SerializeField] private string restartSceneName;

    public string GameOverId => gameOverId;
    public GameObject WindowPrefab => windowPrefab;
    public string TitleText => titleText;
    public string RestartSceneName => restartSceneName;
}
