using UnityEngine;

/// <summary>
/// Stage1-1 발표용 클리어 화면입니다.
/// UI 담당자가 만든 최종 UI가 들어오기 전까지, StageCleared 이벤트가 발생했음을 Game View에서 확실히 보여줍니다.
/// </summary>
[DisallowMultipleComponent]
public class HWJ_PresentationClearOverlaySystem : MonoBehaviour
{
    [Header("클리어 화면 사용")]
    [Tooltip("켜져 있으면 스테이지 클리어 시 발표용 클리어 화면을 표시합니다.")]
    [SerializeField] private bool enableClearOverlay = true;
    [Tooltip("포탈 강제 활성화나 디버그 클리어에서도 화면을 표시합니다.")]
    [SerializeField] private bool showWhenStageStateIsClear = true;

    [Header("표시 문구")]
    [SerializeField] private string titleText = "STAGE CLEAR";
    [SerializeField] private string subText = "1스테이지 핵심 루프 시연 완료";
    [SerializeField] private string guideText = "포탈 진입 또는 다음 시연으로 진행";

    [Header("확인용 상태")]
    [SerializeField] private bool clearShown;
    [SerializeField] private string lastClearMessage;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;

    private void OnEnable()
    {
        HWJ_GameplayEvents.StageCleared += OnStageCleared;
    }

    private void OnDisable()
    {
        HWJ_GameplayEvents.StageCleared -= OnStageCleared;
    }

    private void Update()
    {
        if (!enableClearOverlay || clearShown || !showWhenStageStateIsClear)
        {
            return;
        }

        HWJ_StageProgressionSystem progression = FindFirstObjectByType<HWJ_StageProgressionSystem>(FindObjectsInactive.Exclude);

        if (progression != null && progression.CurrentState == HWJ_StageFlowState.StageClear)
        {
            clearShown = true;
            lastClearMessage = progression.LastTransitionMessage;
        }
    }

    private void OnGUI()
    {
        if (!enableClearOverlay || !clearShown)
        {
            return;
        }

        EnsureStyles();

        float width = Mathf.Min(560f, Screen.width - 80f);
        float height = 190f;
        Rect area = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.22f, width, height);

        GUILayout.BeginArea(area, panelStyle);
        GUILayout.Label(titleText, titleStyle);
        GUILayout.Space(8f);
        GUILayout.Label(subText, bodyStyle);
        GUILayout.Label(guideText, bodyStyle);

        if (!string.IsNullOrWhiteSpace(lastClearMessage))
        {
            GUILayout.Space(8f);
            GUILayout.Label(lastClearMessage, bodyStyle);
        }

        GUILayout.EndArea();
    }

    private void OnStageCleared(HWJ_StageProgressionEvent progressionEvent)
    {
        clearShown = true;
        lastClearMessage = progressionEvent.Message;
    }

    private void EnsureStyles()
    {
        if (panelStyle == null)
        {
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.padding = new RectOffset(20, 20, 18, 18);
            panelStyle.normal.background = Texture2D.grayTexture;
        }

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 40;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(1f, 0.86f, 0.36f, 1f);
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.alignment = TextAnchor.MiddleCenter;
            bodyStyle.fontSize = 20;
            bodyStyle.normal.textColor = Color.white;
            bodyStyle.wordWrap = true;
        }
    }
}
