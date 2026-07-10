using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 생성 시 하이라키의 Canvas를 찾아 체력바 UI를 생성하고, 적 발 밑을 따라다니게 하는 스크립트입니다.
/// 적(Enemy) 프리팹 최상단에 붙여서 사용하세요.
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

    private Camera mainCam;

    private void Start()
    {
        statusSystem = GetComponent<HWJ_RuntimeStatusSystem>();
        mainCam = Camera.main;

        // 1. 하이라키에서 캔버스를 찾아서 UI 생성
        if (hpUIPrefab != null)
        {
            Canvas mainCanvas = FindAnyObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                // 캔버스의 자식으로 UI 생성 (캔버스 중앙 등에 임시로 생성됨)
                spawnedUI = Instantiate(hpUIPrefab, mainCanvas.transform);
            }
            else
            {
                spawnedUI = Instantiate(hpUIPrefab);
            }

            // 2. 생성된 UI 안에서 Slider 컴포넌트 찾기
            hpSlider = spawnedUI.GetComponentInChildren<Slider>();
        }
    }

    private void Update()
    {
        if (statusSystem != null && hpSlider != null && spawnedUI != null)
        {
            float maxHp = statusSystem.MaxHp;
            float currentHp = statusSystem.CurrentHp;

            // 슬라이더 값 갱신 (0 ~ 1)
            if (maxHp > 0)
            {
                hpSlider.value = currentHp / maxHp;
            }

            // 체력에 따른 숨김 처리 로직
            if (currentHp <= 0)
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
        // 3. UI가 적을 계속 따라다니도록 위치 업데이트 (Screen Space 캔버스용)
        if (spawnedUI != null && mainCam != null)
        {
            // 적의 실제 월드(3D/2D) 위치를 모니터 화면(UI 스크린) 좌표로 변환합니다.
            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + uiOffset);
            
            // 변환된 스크린 좌표를 UI의 위치로 지정합니다.
            spawnedUI.transform.position = screenPos;
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
