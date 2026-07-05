using UnityEngine;

/// <summary>
/// SoulSystem 또는 RuntimeStatusSystem의 사망 상태를 감지해서 게임오버 창을 표시하는 기본 시스템입니다.
/// 실제 버튼 동작과 씬 전환은 UI 프리팹 쪽 버튼 이벤트에서 GameOverDataSO.RestartSceneName을 사용해 연결합니다.
/// </summary>
public class HWJ_GameOverWindowSystem : MonoBehaviour
{
    [SerializeField] private HWJ_GameOverDataSO gameOverData;
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private HWJ_ObjectPoolSystem objectPool;
    [SerializeField] private GameObject windowRoot;

    private bool isShown;

    private void Awake()
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (isShown)
        {
            return;
        }

        bool soulDead = soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead;
        bool statusDead = runtimeStatus != null && runtimeStatus.IsDead;

        if (soulDead || statusDead)
        {
            Show();
        }
    }

    /// <summary>
    /// 게임오버 창을 표시합니다.
    /// windowRoot가 직접 지정되어 있으면 활성화하고, 없으면 GameOverDataSO의 프리팹을 생성합니다.
    /// </summary>
    public void Show()
    {
        isShown = true;

        if (windowRoot != null)
        {
            windowRoot.SetActive(true);
            return;
        }

        if (gameOverData != null && gameOverData.WindowPrefab != null)
        {
            windowRoot = objectPool != null
                ? objectPool.Spawn(gameOverData.WindowPrefab, Vector3.zero, Quaternion.identity)
                : HWJ_GameAccess.Spawn(gameOverData.WindowPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}
