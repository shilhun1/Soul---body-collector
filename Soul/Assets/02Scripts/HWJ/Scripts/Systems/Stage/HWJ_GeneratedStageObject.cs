using UnityEngine;

/// <summary>
/// HWJ 에디터 생성기가 만든 씬 오브젝트를 식별하기 위한 표시 컴포넌트입니다.
/// 직접 게임 규칙을 실행하지 않고, 검증/정리 도구가 안전하게 생성물을 구분할 때 사용합니다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("HWJ/Stage/Generated Stage Object")]
public sealed class HWJ_GeneratedStageObject : MonoBehaviour
{
    [Header("생성 정보")]
    [SerializeField] private string generatedId;
    [SerializeField] private string stageSceneId;
    [SerializeField] private string generatorName = "HWJ_Stage1TilemapSceneBuilderBridge";
    [SerializeField] private string generatedCategory;

    [Header("정리 도구")]
    [SerializeField] private bool temporaryPlaceholder;
    [SerializeField] private bool protectFromGeneratedCleanup;

    [Header("확인용")]
    [SerializeField] private string generatedAtLocalTime;

    public string GeneratedId => generatedId;
    public string StageSceneId => stageSceneId;
    public string GeneratorName => generatorName;
    public string GeneratedCategory => generatedCategory;
    public bool TemporaryPlaceholder => temporaryPlaceholder;
    public bool ProtectFromGeneratedCleanup => protectFromGeneratedCleanup;

    public void Configure(
        string newGeneratedId,
        string newStageSceneId,
        string newGeneratedCategory,
        bool isTemporaryPlaceholder,
        bool isProtected)
    {
        generatedId = newGeneratedId;
        stageSceneId = newStageSceneId;
        generatedCategory = newGeneratedCategory;
        temporaryPlaceholder = isTemporaryPlaceholder;
        protectFromGeneratedCleanup = isProtected;
        generatedAtLocalTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
