#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// hys 씬의 슬라임 Animator 연결과 Idle·Walk·Hit·Die 전환을 실제 Play Mode에서 검증합니다.
/// 검증 중 변경은 런타임 인스턴스에만 적용되며 씬과 HWJ 프리팹에는 저장하지 않습니다.
/// </summary>
[InitializeOnLoad]
public static class hys_SlimeAnimationPlayModeValidator
{
    private const string ScenePath = "Assets/01Scenes/hys.unity";
    private const string RunningKey = "hys.SlimeAnimationValidation.Running.v1";
    private const string CompletedKey = "hys.SlimeAnimationValidation.Completed.v1";
    private const string BatchExitPendingKey = "hys.SlimeAnimationValidation.BatchExitPending.v1";
    private const string BatchExitCodeKey = "hys.SlimeAnimationValidation.BatchExitCode.v1";
    private const float TimeoutSeconds = 8f;

    private static Animator animator;
    private static SpriteRenderer spriteRenderer;
    private static Sprite phaseSprite;
    private static float phaseStartedAt;
    private static float validationStartedAt;
    private static int phase;

    static hys_SlimeAnimationPlayModeValidator()
    {
        // 메뉴에서 시작한 검증의 Play Mode 도메인 리로드만 이어서 처리합니다.
        if (Application.isBatchMode && SessionState.GetBool(BatchExitPendingKey, false))
        {
            EditorApplication.delayCall += ExitBatchValidation;
        }
        else if (SessionState.GetBool(RunningKey, false))
        {
            EditorApplication.delayCall += BeginAutomatically;
        }
    }

    [MenuItem("Tools/hys/Animation/슬라임 Play Mode 검증")]
    public static void BeginFromMenu()
    {
        SessionState.SetBool(CompletedKey, false);
        SessionState.SetBool(RunningKey, false);

        // 독립 검증 실행에서는 저장된 hys 씬을 직접 열어 동일한 조건으로 테스트합니다.
        if (Application.isBatchMode && SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        BeginAutomatically();
    }

    private static void BeginAutomatically()
    {
        EditorApplication.delayCall -= BeginAutomatically;

        if (SessionState.GetBool(CompletedKey, false))
        {
            return;
        }

        if (EditorApplication.isPlaying)
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                StartRuntimeValidation();
            }

            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BeginAutomatically;
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError($"[hys Slime Validation] hys 씬이 활성 상태가 아닙니다: {activeScene.path}");
            SessionState.SetBool(CompletedKey, true);
            return;
        }

        SessionState.SetBool(RunningKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void StartRuntimeValidation()
    {
        animator = null;
        spriteRenderer = null;
        phaseSprite = null;
        phase = 0;
        validationStartedAt = Time.realtimeSinceStartup;
        phaseStartedAt = validationStartedAt;
        EditorApplication.update -= ValidateUpdate;
        EditorApplication.update += ValidateUpdate;
    }

    private static void ValidateUpdate()
    {
        try
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= ValidateUpdate;
                return;
            }

            if (Time.realtimeSinceStartup - validationStartedAt > TimeoutSeconds)
            {
                Fail("검증 제한 시간을 초과했습니다.");
                return;
            }

            if (animator == null)
            {
                if (!TryFindAndPrepareSlime())
                {
                    return;
                }

                BeginIdlePhase();
                return;
            }

            switch (phase)
            {
                case 1:
                    ValidateIdle();
                    break;
                case 2:
                    ValidateWalk();
                    break;
                case 3:
                    ValidateHit();
                    break;
                case 4:
                    ValidateHitExit();
                    break;
                case 5:
                    ValidateDie();
                    break;
                case 6:
                    ValidateDieCompletion();
                    break;
            }
        }
        catch (Exception exception)
        {
            Fail(exception.ToString());
        }
    }

    private static bool TryFindAndPrepareSlime()
    {
        GameObject slime = GameObject.Find("HWJ_Runtime_Enemy_General_Slime");
        if (slime == null)
        {
            return false;
        }

        animator = slime.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Fail("슬라임 하위 Animator를 찾지 못했습니다.");
            return false;
        }

        spriteRenderer = animator.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Fail("Animator 오브젝트의 SpriteRenderer를 찾지 못했습니다.");
            return false;
        }

        if (animator.runtimeAnimatorController == null
            || animator.runtimeAnimatorController.name != "hys_Monster_Slime")
        {
            string actual = animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : "NULL";
            Fail($"슬라임 Controller 연결이 다릅니다: {actual}");
            return false;
        }

        // AI와 모션 동기화가 테스트 파라미터를 덮어쓰지 않도록 Play Mode 동안만 비활성화합니다.
        DisableBehaviour<HWJ_MonsterAISystem>(slime);
        DisableBehaviour<HWJ_EnemyNavigationSystem>(slime);
        DisableBehaviour<HWJ_EnemyAttackSystem>(slime);
        DisableBehaviour<HWJ_CharacterMotionSystem>(slime);
        animator.enabled = true;
        animator.speed = 1f;
        return true;
    }

    private static void BeginIdlePhase()
    {
        animator.SetBool("IsDead", false);
        animator.SetBool("IsMoving", false);
        animator.ResetTrigger("HitTrigger");
        animator.ResetTrigger("DeadTrigger");
        animator.Play("hys_Slime_Idle", 0, 0f);
        animator.Update(0f);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(1);
    }

    private static void ValidateIdle()
    {
        if (Elapsed < 0.25f)
        {
            return;
        }

        RequireState("hys_Slime_Idle");
        RequireSpriteChanged("Idle");
        animator.SetBool("IsMoving", true);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(2);
    }

    private static void ValidateWalk()
    {
        if (Elapsed < 0.22f)
        {
            return;
        }

        RequireState("hys_Slime_Walk");
        RequireSpriteChanged("Walk");
        animator.SetBool("IsMoving", false);
        animator.SetTrigger("HitTrigger");
        phaseSprite = spriteRenderer.sprite;
        SetPhase(3);
    }

    private static void ValidateHit()
    {
        if (Elapsed < 0.3f)
        {
            return;
        }

        RequireState("hys_Slime_Hit");
        RequireSpriteChanged("Hit");
        SetPhase(4);
    }

    private static void ValidateHitExit()
    {
        if (Elapsed < 0.45f)
        {
            return;
        }

        RequireState("hys_Slime_Idle");
        animator.SetTrigger("DeadTrigger");
        animator.SetBool("IsDead", true);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(5);
    }

    private static void ValidateDie()
    {
        if (Elapsed < 0.3f)
        {
            return;
        }

        RequireState("hys_Slime_Die");
        RequireSpriteChanged("Die");
        SetPhase(6);
    }

    private static void ValidateDieCompletion()
    {
        if (Elapsed < 1.4f)
        {
            return;
        }

        RequireState("hys_Slime_Die");
        Pass();
    }

    private static float Elapsed => Time.realtimeSinceStartup - phaseStartedAt;

    private static void SetPhase(int value)
    {
        phase = value;
        phaseStartedAt = Time.realtimeSinceStartup;
    }

    private static void RequireState(string expected)
    {
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(expected))
        {
            throw new InvalidOperationException(
                $"상태 전환 실패: 예상={expected}, shortNameHash={state.shortNameHash}");
        }
    }

    private static void RequireSpriteChanged(string motion)
    {
        if (spriteRenderer.sprite == null || spriteRenderer.sprite == phaseSprite)
        {
            throw new InvalidOperationException($"{motion} 애니메이션 프레임이 변경되지 않았습니다.");
        }
    }

    private static void DisableBehaviour<T>(GameObject root) where T : Behaviour
    {
        T behaviour = root.GetComponent<T>();
        if (behaviour != null)
        {
            behaviour.enabled = false;
        }
    }

    private static void Pass()
    {
        Debug.Log("[hys Slime Validation] PASS - Controller 연결, Idle/Walk/Hit/Die 상태 전환, Sprite 프레임 변경을 확인했습니다.");
        Finish(0);
    }

    private static void Fail(string reason)
    {
        Debug.LogError($"[hys Slime Validation] FAIL - {reason}");
        Finish(1);
    }

    private static void Finish(int exitCode)
    {
        EditorApplication.update -= ValidateUpdate;
        SessionState.SetBool(RunningKey, false);
        SessionState.SetBool(CompletedKey, true);
        if (Application.isBatchMode)
        {
            SessionState.SetInt(BatchExitCodeKey, exitCode);
            SessionState.SetBool(BatchExitPendingKey, true);
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
        else if (Application.isBatchMode)
        {
            ExitBatchValidation();
        }
    }

    private static void ExitBatchValidation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ExitBatchValidation;
            return;
        }

        int exitCode = SessionState.GetInt(BatchExitCodeKey, 1);
        SessionState.SetBool(BatchExitPendingKey, false);
        EditorApplication.Exit(exitCode);
    }
}
#endif
