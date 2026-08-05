using UnityEngine;
using UnityEngine.InputSystem;

public class HSH_StealBody : MonoBehaviour
{
    [Header("Steal Body UI Settings")]
    [Tooltip("생성할 HSH_StealBodyUi 프리팹")]
    public HSH_StealBodyUi stealBodyUiPrefab;
    [Tooltip("오브젝트 기준으로 UI가 생성될 위치 (오프셋)")]
    public Vector3 uiOffset = new Vector3(0, 0.5f, 0);

    private HSH_StealBodyUi spawnedUi;

    [Header("Steal Body Settings")]
    [Tooltip("빙의에 사용할 상호작용 키 (기본값: F)")]
    public Key interactionKey = Key.F;

    private bool isPlayerInRange = false;

    void Update()
    {
        Key effectiveKey = (HSH.UI.HSH_KeyBindingManager.Instance != null)
            ? HSH.UI.HSH_KeyBindingManager.Instance.GetInputSystemKey(HSH.UI.HSH_KeyAction.Interact)
            : interactionKey;

        // 플레이어가 범위 안에 있고, 상호작용 키를 눌렀을 때
        if (isPlayerInRange && Keyboard.current != null && Keyboard.current[effectiveKey].wasPressedThisFrame)
        {
            PossessBody();
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
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                Camera targetCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                if (targetCamera != null)
                {
                    Vector3 screenPos = targetCamera.WorldToScreenPoint(transform.position + uiOffset);
                    spawnedUi.transform.position = screenPos;
                }
            }
            else
            {
                spawnedUi.transform.position = transform.position + uiOffset;
            }
        }
    }

    private void PossessBody()
    {
        // 1. 빙의 성공 메시지 출력
        Debug.Log("<color=magenta>[빙의 성공]</color> 적의 시체에 빙의되었습니다!");

        // 생성된 UI 삭제
        if (spawnedUi != null)
        {
            Destroy(spawnedUi.gameObject);
        }

        // 2. 빙의 후 시체 오브젝트 삭제 (또는 필요한 처리)
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어가 다가오면 상호작용 가능 상태로 변경
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;

            // UI 팝업 생성 및 활성화
            if (spawnedUi == null && stealBodyUiPrefab != null)
            {
                // 씬에 Canvas가 있는지 확인
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    // 캔버스의 자식으로 UI를 생성하여 UI 렌더링 레이어 보장
                    spawnedUi = Instantiate(stealBodyUiPrefab, canvas.transform);
                }
                else
                {
                    // 캔버스가 없다면 부모 없이 생성
                    spawnedUi = Instantiate(stealBodyUiPrefab, transform.position + uiOffset, Quaternion.identity);
                }
            }

            if (spawnedUi != null)
            {
                Key effectiveKey = (HSH.UI.HSH_KeyBindingManager.Instance != null)
                    ? HSH.UI.HSH_KeyBindingManager.Instance.GetInputSystemKey(HSH.UI.HSH_KeyAction.Interact)
                    : interactionKey;

                UpdateUIPosition();
                spawnedUi.ShowUI(effectiveKey.ToString());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어가 멀어지면 상호작용 불가 상태로 변경
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
