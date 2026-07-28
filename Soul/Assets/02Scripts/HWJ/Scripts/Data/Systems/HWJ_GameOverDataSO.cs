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

/// <summary>
/// 시작 타이틀 화면이 사용할 UI 프리팹과 시작 대상 씬을 관리하는 ScriptableObject입니다.
/// 씬에 놓는 타이틀 UI 프리팹은 이 데이터를 통해 어떤 화면을 표시하고, 시작 버튼이 어디로 이동할지 읽습니다.
/// </summary>
[CreateAssetMenu(fileName = "HWJ_TitleScreenData", menuName = "HWJ/Data/System/Title Screen")]
public class HWJ_TitleScreenDataSO : ScriptableObject
{
    [Header("타이틀 화면")]
    [Tooltip("타이틀 화면 설정을 구분하는 고정 ID입니다.")]
    [InspectorName("타이틀 화면 ID")]
    [SerializeField] private string titleScreenId;
    [Tooltip("처음 시작 화면에 사용할 UI 프리팹입니다.")]
    [InspectorName("화면 프리팹")]
    [SerializeField] private GameObject windowPrefab;
    [Tooltip("시작 버튼을 눌렀을 때 이동할 첫 게임플레이 씬 이름입니다. 비워두면 현재 씬에서 UI만 닫습니다.")]
    [InspectorName("첫 게임플레이 씬 이름")]
    [SerializeField] private string firstGameplaySceneName;
    [Tooltip("타이틀 화면이 열려 있는 동안 게임 시간을 멈출지 결정합니다.")]
    [InspectorName("표시 중 시간 정지")]
    [SerializeField] private bool pauseGameWhileShown = true;

    public string TitleScreenId => titleScreenId;
    public GameObject WindowPrefab => windowPrefab;
    public string FirstGameplaySceneName => firstGameplaySceneName;
    public bool PauseGameWhileShown => pauseGameWhileShown;
}
