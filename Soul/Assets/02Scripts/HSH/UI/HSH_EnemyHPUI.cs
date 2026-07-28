using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 생성 시 하이라키의 메인 HUD Canvas를 찾아 체력바 UI를 생성하고, 적 발 밑을 따라다니게 하는 스크립트입니다.
/// 씬 이동 및 다중 캔버스 환경에서도 안전하게 메인 캔버스를 찾고 체력바를 재할당합니다.
/// </summary>
public class HSH_EnemyHPUI : MonoBehaviour
{
    [Header("UI 프리팹 설정")]
    [Tooltip("생성할 적 체력바(Slider 포함) 프리팹을 넣어주세요.")]
    public GameObject hpUIPrefab; 

    [Header("위치 설정")]
    [Tooltip("적을 기준으로 UI를 띄울 위치 오프셋 (발 밑이면 y를 마이너스로 설정)")]
    public Vector3 uiOffset = new Vector3(0, -1f, 0); 

    [Header("표시 설정")]
    [Tooltip("체력이 꽉 차있을 때는 체력바를 숨길지 여부")]
    public bool hideWhenFull = true;

    // 내부 상태
    private HWJ_RuntimeStatusSystem statusSystem;
    private GameObject spawnedUI;
    private Slider hpSlider;
    private Canvas targetCanvas;
    private Camera mainCam;

    private void Start()
    {
        statusSystem = GetComponent<HWJ_RuntimeStatusSystem>();
        EnsureUI();
    }

    private void Update()
    {
        // 씬 이동 등으로 UI나 캔버스가 파괴되었을 경우 자동으로 탐색 및 재할당
        if (spawnedUI == null)
        {
            targetCanvas = null;
            EnsureUI();
        }

        if (statusSystem != null && hpSlider != null && spawnedUI != null)
        {
            float maxHp = statusSystem.MaxHp;
            float currentHp = statusSystem.CurrentHp;

            // 슬라이더 값 갱신 (0 ~ 1)
            if (maxHp > 0)
            {
                hpSlider.value = currentHp / maxHp;
            }

            // 체력 및 빙의 상태에 따른 숨김 처리 로직
            if (statusSystem.CurrentState == HWJ_RuntimeState.Possessed)
            {
                spawnedUI.SetActive(false); // 빙의 중이면 체력바 숨김
            }
            else if (currentHp <= 0)
            {
                spawnedUI.SetActive(false); // 죽으면 숨김
            }
            else if (hideWhenFull && currentHp >= maxHp)
            {
                spawnedUI.SetActive(false); // 꽉 차있으면 숨김
            }
            else
            {
                spawnedUI.SetActive(true);  // 그 외 깎였을 땐 표시
            }
        }
    }
    
    private void LateUpdate()
    {
        // 씬 전환 등으로 카메라가 파괴/변경되었을 때 메인 카메라 다시 갱신
        if (mainCam == null)
        {
            mainCam = Camera.main;
        }

        // UI가 적을 계속 따라다니도록 위치 업데이트 (Screen Space 캔버스용)
        if (spawnedUI != null && mainCam != null)
        {
            // 적의 실제 월드(3D/2D) 위치를 모니터 화면(UI 스크린) 좌표로 변환합니다.
            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + uiOffset);
            
            // 변환된 스크린 좌표를 UI의 위치로 지정합니다.
            spawnedUI.transform.position = screenPos;
        }
    }

    /// <summary>
    /// 씬 내의 적절한 메인 HUD 캔버스를 찾아 UI를 생성합니다.
    /// </summary>
    private void EnsureUI()
    {
        if (hpUIPrefab == null) return;

        if (targetCanvas == null)
        {
            targetCanvas = FindMainHUDCanvas();
        }

        if (targetCanvas != null)
        {
            spawnedUI = Instantiate(hpUIPrefab, targetCanvas.transform);
            hpSlider = spawnedUI.GetComponentInChildren<Slider>();
        }
        else
        {
            spawnedUI = Instantiate(hpUIPrefab);
            hpSlider = spawnedUI.GetComponentInChildren<Slider>();
        }
    }

    /// <summary>
    /// 무작위 Canvas 탐색 대신, 활성화된 ScreenSpace 메인 HUD Canvas를 우회 탐색합니다.
    /// </summary>
    private Canvas FindMainHUDCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        // 1순위: 활성화된 ScreenSpace 캔버스 중 이름에 HUD, Main, UI, Game 등이 포함된 캔버스
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.enabled)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    string name = c.gameObject.name.ToLower();
                    if (name.Contains("hud") || name.Contains("main") || name.Contains("ui") || name.Contains("game"))
                    {
                        return c;
                    }
                }
            }
        }

        // 2순위: 이름 조건이 맞지 않더라도 활성화된 ScreenSpace 캔버스 중 첫 번째
        foreach (var c in canvases)
        {
            if (c != null && c.gameObject.activeInHierarchy && c.enabled)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    return c;
                }
            }
        }

        // 3순위: Fallback
        return FindFirstObjectByType<Canvas>();
    }

    private void OnDisable()
    {
        // 적 오브젝트가 비활성화되면(예: 빙의되어 몸이 숨겨짐) UI도 숨김
        if (spawnedUI != null)
        {
            spawnedUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 적 오브젝트가 파괴될 때, 허공에 남지 않도록 생성된 UI도 같이 파괴
        if (spawnedUI != null)
        {
            Destroy(spawnedUI);
        }
    }
}
