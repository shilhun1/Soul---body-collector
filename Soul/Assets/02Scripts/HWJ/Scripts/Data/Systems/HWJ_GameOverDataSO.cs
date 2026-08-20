using UnityEngine;

/// <summary>
/// 게임오버 창을 띄울 때 필요한 UI 프리팹과 메시지를 관리하는 ScriptableObject입니다.
/// GameOverWindowSystem이 Dead 상태를 감지했을 때 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_GameOverData", menuName = "HWJ/Data/System/Game Over")]
public class HWJ_GameOverDataSO : ScriptableObject
{
    [Header("게임오버")]
    [Tooltip("게임오버 창 설정을 구분하는 고정 ID입니다.")]
    [InspectorName("게임오버 ID")]
    [SerializeField] private string gameOverId;
    [Tooltip("게임오버 UI 프리팹입니다.")]
    [InspectorName("창 프리팹")]
    [SerializeField] private GameObject windowPrefab;
    [Tooltip("게임오버 창에 표시할 제목입니다.")]
    [InspectorName("제목 텍스트")]
    [SerializeField] private string titleText = "Game Over";
    [Tooltip("재시작 시 이동할 씬 이름입니다.")]
    [InspectorName("재시작 씬 이름")]
    [SerializeField] private string restartSceneName;

    public string GameOverId => gameOverId;
    public GameObject WindowPrefab => windowPrefab;
    public string TitleText => titleText;
    public string RestartSceneName => restartSceneName;
}
