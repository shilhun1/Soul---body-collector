using UnityEngine;

// HWJ 직업별 적 패턴 시작 정보를 해당 몬스터 Animator Trigger로 전달합니다.
[DisallowMultipleComponent]
public class hys_HWJEnemyPatternAnimatorBridge : MonoBehaviour
{
    private const string PreparingPrefix = "Preparing skill ";

    [Header("HWJ 참조")]
    [SerializeField] private HWJ_EnemyAttackSystem enemyAttackSystem;
    [SerializeField] private HWJ_RuntimeStatusSystem runtimeStatus;
    [SerializeField] private Animator animator;

    private readonly string[] skillIds = new string[3];
    private readonly int[] triggerHashes = new int[3];
    private readonly bool[] hasTriggerParameters = new bool[3];

    private string configuredControllerName;
    private string lastObservedAttackResult;

    private void Awake()
    {
        CacheReferences();
        ConfigurePatterns();
        lastObservedAttackResult = enemyAttackSystem != null
            ? enemyAttackSystem.LastAttackResult
            : string.Empty;
    }

    private void Update()
    {
        if (enemyAttackSystem == null || animator == null)
        {
            CacheReferences();
            ConfigurePatterns();
        }

        if (enemyAttackSystem == null || animator == null || string.IsNullOrEmpty(configuredControllerName))
        {
            return;
        }

        // 사망한 몬스터는 남아 있던 준비 문자열로 패턴 Trigger를 다시 실행하지 않습니다.
        if (runtimeStatus != null && runtimeStatus.IsDead)
        {
            lastObservedAttackResult = enemyAttackSystem.LastAttackResult;
            return;
        }

        string currentResult = enemyAttackSystem.LastAttackResult;
        if (string.IsNullOrEmpty(currentResult) || currentResult == lastObservedAttackResult)
        {
            return;
        }

        lastObservedAttackResult = currentResult;

        // HWJ가 패턴 예고를 시작한 순간 한 번만 대응하는 애니메이션 상태로 진입합니다.
        if (!currentResult.StartsWith(PreparingPrefix))
        {
            return;
        }

        string preparedSkillId = currentResult.Substring(PreparingPrefix.Length).TrimEnd('.');
        for (int i = 0; i < skillIds.Length; i++)
        {
            if (preparedSkillId == skillIds[i])
            {
                PlayPattern(i);
                return;
            }
        }
    }

    // 자동 설치 도구가 찾은 HWJ 적과 Animator를 즉시 연결할 때 사용합니다.
    public void Initialize(HWJ_EnemyAttackSystem attackSystem, Animator targetAnimator)
    {
        enemyAttackSystem = attackSystem;
        runtimeStatus = attackSystem != null
            ? attackSystem.GetComponent<HWJ_RuntimeStatusSystem>()
            : null;
        animator = targetAnimator;
        ConfigurePatterns(true);
        lastObservedAttackResult = enemyAttackSystem != null
            ? enemyAttackSystem.LastAttackResult
            : string.Empty;
    }

    private void PlayPattern(int patternIndex)
    {
        if ((runtimeStatus != null && runtimeStatus.IsDead)
            || patternIndex < 0
            || patternIndex >= triggerHashes.Length
            || !hasTriggerParameters[patternIndex])
        {
            return;
        }

        for (int i = 0; i < triggerHashes.Length; i++)
        {
            animator.ResetTrigger(triggerHashes[i]);
        }

        animator.SetTrigger(triggerHashes[patternIndex]);
    }

    private void ConfigurePatterns(bool force = false)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            configuredControllerName = null;
            return;
        }

        string controllerName = animator.runtimeAnimatorController.name;
        if (!force && controllerName == configuredControllerName)
        {
            return;
        }

        if (!TryGetPatternSet(controllerName, out string token, out string skill1, out string skill2, out string skill3))
        {
            configuredControllerName = null;
            return;
        }

        configuredControllerName = controllerName;
        skillIds[0] = skill1;
        skillIds[1] = skill2;
        skillIds[2] = skill3;

        for (int i = 0; i < triggerHashes.Length; i++)
        {
            triggerHashes[i] = Animator.StringToHash($"M_{token}_{i + 1}");
            hasTriggerParameters[i] = HasParameter(
                triggerHashes[i],
                AnimatorControllerParameterType.Trigger);
        }
    }

    internal static bool TryGetPatternSet(
        string controllerName,
        out string token,
        out string skill1,
        out string skill2,
        out string skill3)
    {
        token = null;
        skill1 = null;
        skill2 = null;
        skill3 = null;

        switch (controllerName)
        {
            case "hys_Monster_Sword":
                token = "sword";
                skill1 = "sword_diagonal_slash";
                skill2 = "sword_up_diagonal_slash";
                skill3 = "sword_wave";
                return true;
            case "hys_Monster_Axe":
                token = "axe";
                skill1 = "axe_swing";
                skill2 = "axe_body_charge";
                skill3 = "axe_spin_charge";
                return true;
            case "hys_Monster_Bow":
                token = "bow";
                skill1 = "bow_air_arrow_shot";
                skill2 = "bow_rapid_shot";
                skill3 = "bow_low_charge_shot";
                return true;
            case "hys_Monster_Lance":
                token = "lance";
                skill1 = "lance_charge_thrust";
                skill2 = "lance_thrust_combo";
                skill3 = "lance_finisher_thrust";
                return true;
            case "hys_Monster_Shield":
                token = "shield";
                skill1 = "shield_charge";
                skill2 = "shield_slam";
                skill3 = "shield_diagonal_knockback";
                return true;
            default:
                return false;
        }
    }

    private bool HasParameter(int parameterHash, AnimatorControllerParameterType expectedType)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash && parameters[i].type == expectedType)
            {
                return true;
            }
        }

        return false;
    }

    private void CacheReferences()
    {
        if (enemyAttackSystem == null)
        {
            enemyAttackSystem = GetComponent<HWJ_EnemyAttackSystem>();
        }

        if (runtimeStatus == null)
        {
            runtimeStatus = GetComponent<HWJ_RuntimeStatusSystem>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }
}

// HWJ 프리팹을 수정하지 않고 5개 직업 몬스터에 공통 패턴 브리지를 자동으로 연결합니다.
internal sealed class hys_HWJEnemyPatternAnimatorBridgeInstaller : MonoBehaviour
{
    private const float ScanIntervalSeconds = 0.5f;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstaller()
    {
        if (FindAnyObjectByType<hys_HWJEnemyPatternAnimatorBridgeInstaller>() != null)
        {
            return;
        }

        GameObject installerObject = new GameObject("hys_HWJEnemyPatternAnimatorBridgeInstaller");
        installerObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(installerObject);
        installerObject.AddComponent<hys_HWJEnemyPatternAnimatorBridgeInstaller>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + ScanIntervalSeconds;
        AttachMissingBridges();
    }

    private static void AttachMissingBridges()
    {
        HWJ_EnemyAttackSystem[] attackSystems = FindObjectsByType<HWJ_EnemyAttackSystem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < attackSystems.Length; i++)
        {
            HWJ_EnemyAttackSystem attackSystem = attackSystems[i];
            if (attackSystem == null
                || attackSystem.GetComponent<hys_HWJEnemyPatternAnimatorBridge>() != null)
            {
                continue;
            }

            Animator targetAnimator = attackSystem.GetComponentInChildren<Animator>(true);
            if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
            {
                continue;
            }

            if (!hys_HWJEnemyPatternAnimatorBridge.TryGetPatternSet(
                    targetAnimator.runtimeAnimatorController.name,
                    out _, out _, out _, out _))
            {
                continue;
            }

            hys_HWJEnemyPatternAnimatorBridge bridge =
                attackSystem.gameObject.AddComponent<hys_HWJEnemyPatternAnimatorBridge>();
            bridge.Initialize(attackSystem, targetAnimator);
        }
    }
}
