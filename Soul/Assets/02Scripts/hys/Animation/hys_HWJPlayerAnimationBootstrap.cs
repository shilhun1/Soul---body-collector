using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬별 수동 설정 없이 HWJ 플레이어 루트에 hys 애니메이션 보조 컴포넌트를 연결합니다.
/// 이동 시스템과 영혼 시스템을 함께 가진 실제 플레이어 루트만 처리합니다.
/// </summary>
[DefaultExecutionOrder(-1200)]
public sealed class hys_HWJPlayerAnimationBootstrap : MonoBehaviour
{
    private static hys_HWJPlayerAnimationBootstrap instance;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject("hys_HWJPlayerAnimationBootstrap");
        host.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        DontDestroyOnLoad(host);
        instance = host.AddComponent<hys_HWJPlayerAnimationBootstrap>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ScanPlayers();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + 0.5f;
        ScanPlayers();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanPlayers();
    }

    private static void ScanPlayers()
    {
        HWJ_PlayerMovementSystem[] movementSystems = FindObjectsByType<HWJ_PlayerMovementSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (HWJ_PlayerMovementSystem movementSystem in movementSystems)
        {
            if (movementSystem == null)
            {
                continue;
            }

            GameObject root = movementSystem.gameObject;
            if (root.GetComponent<HWJ_CharacterMotionSystem>() == null)
            {
                continue;
            }

            // 단발 모션 종료 후 무기별 Entry 상태로 복귀시킵니다.
            if (root.GetComponent<hys_AnimatorReturnToEntry>() == null)
            {
                root.AddComponent<hys_AnimatorReturnToEntry>();
            }

            // HWJ 플레이어에서는 기존 통합 Animator 대신 기능별 HWJ 브리지만 사용합니다.
            hys_Player_Animator legacyAnimator = root.GetComponent<hys_Player_Animator>();
            if (legacyAnimator != null && legacyAnimator.enabled)
            {
                legacyAnimator.enabled = false;
            }

            // 빙의 시작과 해제는 모든 HWJ 플레이어에서 공용 브리지가 담당합니다.
            if (root.GetComponent<HWJ_SoulSystem>() != null
                && root.GetComponent<hys_HWJPossessionAnimationBridge>() == null)
            {
                root.AddComponent<hys_HWJPossessionAnimationBridge>();
            }

            // 성공한 HWJ 기본 공격을 현재 hys 무기 Animator의 1타/2타 상태로 전달합니다.
            if (root.GetComponent<hys_HWJBasicAttackAnimationBridge>() == null)
            {
                root.AddComponent<hys_HWJBasicAttackAnimationBridge>();
            }
        }
    }
}
