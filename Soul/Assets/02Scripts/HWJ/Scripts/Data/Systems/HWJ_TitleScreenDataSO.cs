using UnityEngine;

/// <summary>
/// 시작 타이틀 화면이 사용할 UI 프리팹과 시작 대상 씬을 관리하는 ScriptableObject입니다.
/// 파일명과 클래스명을 일치시켜 Unity가 독립 SO 타입으로 안정적으로 직렬화할 수 있게 합니다.
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
