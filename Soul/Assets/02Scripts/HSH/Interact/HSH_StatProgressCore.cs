using UnityEngine;
using UnityEngine.InputSystem;

public class HSH_StatProgressCore : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("생성할 HSH_StatProgressUi 프리팹")]
    public HSH_StatProgressUi progressUiPrefab;
    
    [Tooltip("코어 기준으로 UI가 생성될 위치 (오프셋)")]
    public Vector3 uiOffset = new Vector3(0, 0.5f, 0);

    private HSH_StatProgressUi spawnedUi;

    [Header("Core Settings")]
    [Tooltip("상호작용할 키 (기본값: F)")]
    public Key interactionKey = Key.F;

    [Tooltip("상승할 수치의 최소값")]
    public int minStatIncrease = 1;
    [Tooltip("상승할 수치의 최대값")]
    public int maxStatIncrease = 5;

    private bool isPlayerInRange = false;

    // 랜덤으로 상승시킬 스탯의 종류들 목록
    private string[] statTypes = { "최대 체력(MaxHp)", "이동 속도(MoveSpeed)", "물리 공격력(PhysicalAttack)", "방어력(Defense)", "공격 속도(AttackSpeed)" };

    void Start()
    {
        // UI 생성은 플레이어가 접근했을 때 처리합니다.
    }

    void Update()
    {
        // 플레이어가 범위 안에 있고, 상호작용 키를 눌렀을 때 작동
        if (isPlayerInRange && Keyboard.current != null && Keyboard.current[interactionKey].wasPressedThisFrame)
        {
            TriggerRandomStat();
        }

        // UI가 켜져 있다면, 캔버스 모드에 맞춰 위치를 계속 업데이트 (카메라 이동 대응)
        if (spawnedUi != null && spawnedUi.gameObject.activeInHierarchy)
        {
            UpdateUIPosition();
        }
    }

    private void UpdateUIPosition()
    {
        if (spawnedUi != null)
        {
            Canvas canvas = spawnedUi.GetComponentInParent<Canvas>();
            // 오버레이 캔버스 혹은 카메라 캔버스인 경우 월드 좌표를 스크린 좌표로 변환
            if (canvas != null && (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera))
            {
                if (Camera.main != null)
                {
                    spawnedUi.transform.position = Camera.main.WorldToScreenPoint(transform.position + uiOffset);
                }
            }
            else
            {
                // 월드 스페이스 캔버스일 경우 일반 월드 좌표 사용
                spawnedUi.transform.position = transform.position + uiOffset;
            }
        }
    }

    private void TriggerRandomStat()
    {
        // 1. 랜덤으로 스탯 종류 결정
        int randomTypeIndex = Random.Range(0, statTypes.Length);
        string selectedStat = statTypes[randomTypeIndex];

        // 2. 랜덤으로 상승 수치 결정 (min 이상, max 이하)
        int randomValue = Random.Range(minStatIncrease, maxStatIncrease + 1);

        // 3. 실제 능력치를 올리지 않고 시뮬레이션 메시지만 출력 (요구사항)
        Debug.Log($"<color=green>[능력치 코어 흡수]</color> 플레이어의 <b>'{selectedStat}'</b> 능력이 <b>{randomValue}</b> 만큼 증가했습니다!");

        // 생성된 UI 오브젝트도 깔끔하게 같이 삭제
        if (spawnedUi != null)
        {
            Destroy(spawnedUi.gameObject);
        }

        // 4. 코어 오브젝트 삭제
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 접근했을 때 올바른 키 이름과 함께 UI 표시
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            
            // UI가 없다면 씬 내의 Canvas를 찾아 자식으로 생성
            if (spawnedUi == null && progressUiPrefab != null)
            {
                Canvas mainCanvas = FindObjectOfType<Canvas>();
                if (mainCanvas != null)
                {
                    spawnedUi = Instantiate(progressUiPrefab, transform.position + uiOffset, Quaternion.identity, mainCanvas.transform);
                }
                else
                {
                    // 캔버스가 없다면 부모 없이 생성
                    spawnedUi = Instantiate(progressUiPrefab, transform.position + uiOffset, Quaternion.identity);
                }
            }

            if (spawnedUi != null)
            {
                UpdateUIPosition();
                spawnedUi.ShowUI(interactionKey.ToString());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어가 멀어졌을 때 UI 숨김
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (spawnedUi != null)
            {
                spawnedUi.HideUI();
            }
        }
    }
}
