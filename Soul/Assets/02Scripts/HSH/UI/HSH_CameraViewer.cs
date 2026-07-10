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

    [Header("UI 대상 캔버스")]
    [Tooltip("화면을 띄울 대상 캔버스 (비워두면 씬에 있는 캔버스를 자동으로 찾습니다)")]
    public Canvas targetCanvas;

    private RenderTexture renderTexture;
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

        // 2. 화면에 띄울 UI Canvas 가져오기
        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[HSH_CameraViewer] 씬에 Canvas가 없어서 화면을 띄울 수 없습니다. 캔버스를 만들어주세요!");
            return;
        }

        // 3. RawImage 생성하여 오른쪽 위에 배치
        rawImageObj = new GameObject("PiPRawImage");
        rawImageObj.transform.SetParent(targetCanvas.transform, false);

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
        // 스크립트가 파괴될 때 메모리와 생성했던 이미지를 정리합니다.
        if (captureCamera != null && captureCamera.targetTexture == renderTexture)
        {
            captureCamera.targetTexture = null;
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        if (rawImageObj != null)
        {
            Destroy(rawImageObj); // 캔버스는 남겨두고 내가 만든 RawImage만 파괴
        }
    }
}
