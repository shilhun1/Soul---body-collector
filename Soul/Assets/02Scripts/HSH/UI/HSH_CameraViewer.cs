using UnityEngine;
using UnityEngine.UI;

public class HSH_CameraViewer : MonoBehaviour
{
    [Header("오른쪽 위 화면(PiP) 설정")]
    [Tooltip("화면을 찍고 있는 서브 카메라. 비워두면 이 스크립트가 붙은 카메라를 사용합니다.")]
    public Camera captureCamera;

    [Tooltip("화면에 띄울 UI 크기")]
    public Vector2 viewSize = new Vector2(320, 180);

    [Tooltip("화면 오른쪽 위 모서리로부터의 여백 (x: 왼쪽으로, y: 아래쪽으로)")]
    public Vector2 padding = new Vector2(-20, -20);

    private RenderTexture renderTexture;
    private GameObject canvasObj;
    private GameObject rawImageObj;

    private void Start()
    {
        if (captureCamera == null)
        {
            captureCamera = GetComponent<Camera>();
        }

        if (captureCamera == null)
        {
            Debug.LogError("[HSH_CameraViewer] 카메라를 찾을 수 없습니다! 찍고 있는 카메라를 연결해주세요.");
            return;
        }

        // 1. RenderTexture 생성 (이 텍스처에 카메라 화면을 그림)
        renderTexture = new RenderTexture(Mathf.RoundToInt(viewSize.x), Mathf.RoundToInt(viewSize.y), 16);
        captureCamera.targetTexture = renderTexture;

        // 2. 화면에 띄울 UI Canvas 자동 생성
        canvasObj = new GameObject("HSH_PiPCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // UI가 다른 것들보다 가장 위에 보이도록 설정
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 씬이 넘어가도 유지하고 싶다면 아래 주석을 해제하세요.
        // DontDestroyOnLoad(canvasObj);

        // 3. RawImage 생성하여 오른쪽 위에 배치
        rawImageObj = new GameObject("PiPRawImage");
        rawImageObj.transform.SetParent(canvasObj.transform, false);

        RawImage rawImage = rawImageObj.AddComponent<RawImage>();
        rawImage.texture = renderTexture; // 카메라가 찍고 있는 텍스처를 UI에 연결

        RectTransform rect = rawImageObj.GetComponent<RectTransform>();
        
        // 앵커를 우상단(Top-Right)으로 설정
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        
        // 크기와 여백 설정
        rect.sizeDelta = viewSize;
        rect.anchoredPosition = new Vector2(padding.x, padding.y);
    }

    private void OnDestroy()
    {
        // 스크립트가 파괴될 때 생성했던 오브젝트들과 메모리를 정리합니다.
        if (captureCamera != null && captureCamera.targetTexture == renderTexture)
        {
            captureCamera.targetTexture = null;
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        if (canvasObj != null)
        {
            Destroy(canvasObj);
        }
    }
}
