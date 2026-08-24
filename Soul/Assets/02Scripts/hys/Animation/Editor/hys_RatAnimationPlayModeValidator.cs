#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// hys 씬의 쥐 Animator가 Idle부터 Die까지 실제 Play Mode에서 정상 전환되는지 검사합니다.
/// 검증 중 시스템 비활성화와 파라미터 변경은 런타임 인스턴스에만 적용됩니다.
/// </summary>
[InitializeOnLoad]
public static class hys_RatAnimationPlayModeValidator
{
    private const string RunningKey = "hys.RatAnimationValidation.Running.v1";
    private const string PreviousRunInBackgroundKey = "hys.RatAnimationValidation.PreviousRunInBackground.v1";
    private const string HysScenePath = "Assets/01Scenes/hys.unity";
    private const float TimeoutSeconds = 12f;

    private static Animator animator;
    private static SpriteRenderer spriteRenderer;
    private static Sprite phaseSprite;
    private static float phaseStartedAt;
    private static float validationStartedAt;
    private static int phase;

    static hys_RatAnimationPlayModeValidator()
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            EditorApplication.delayCall += ContinueValidation;
        }
    }

    [MenuItem("Tools/hys/Animation/쥐 Play Mode 검증")]
    public static void BeginFromMenu()
    {
        // 다른 프로그램이 포커스를 가진 상태에서도 검증 동안만 Play Mode가 진행되게 합니다.
        SessionState.SetBool(PreviousRunInBackgroundKey, Application.runInBackground);
        Application.runInBackground = true;
        SessionState.SetBool(RunningKey, true);
        if (Application.isBatchMode && SceneManager.GetActiveScene().path != HysScenePath)
        {
            EditorSceneManager.OpenScene(HysScenePath, OpenSceneMode.Single);
        }

        if (EditorApplication.isPlaying)
        {
            StartRuntimeValidation();
        }
        else
        {
            EditorApplication.EnterPlaymode();
        }
    }

    private static void ContinueValidation()
    {
        EditorApplication.delayCall -= ContinueValidation;
        if (EditorApplication.isPlaying && SessionState.GetBool(RunningKey, false))
        {
            StartRuntimeValidation();
        }
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
                if (!TryFindAndPrepareRat())
                {
                    return;
                }

                BeginIdle();
                return;
            }

            switch (phase)
            {
                case 1: ValidateIdle(); break;
                case 2: ValidateRunStart(); break;
                case 3: ValidateRunLoopEntered(); break;
                case 4: ValidateRunLoopFrames(); break;
                case 5: ValidateAttack(); break;
                case 6: ValidateAttackExit(); break;
                case 7: ValidateHurt(); break;
                case 8: ValidateHurtExit(); break;
                case 9: ValidateDie(); break;
                case 10: ValidateDieCompletion(); break;
            }
        }
        catch (Exception exception)
        {
            Fail(exception.ToString());
        }
    }

    private static bool TryFindAndPrepareRat()
    {
        Animator[] animators = Resources.FindObjectsOfTypeAll<Animator>();
        foreach (Animator candidate in animators)
        {
            if (candidate == null
                || !candidate.gameObject.scene.IsValid()
                || candidate.runtimeAnimatorController == null
                || candidate.runtimeAnimatorController.name != "hys_Monster_Rat")
            {
                continue;
            }

            animator = candidate;
            break;
        }

        if (animator == null)
        {
            return false;
        }

        spriteRenderer = animator.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Fail("쥐 Animator 오브젝트의 SpriteRenderer를 찾지 못했습니다.");
            return false;
        }

        // AI가 검증 파라미터를 덮어쓰지 않도록 Play Mode 동안만 정지합니다.
        HWJ_CharacterMotionSystem motion = animator.GetComponentInParent<HWJ_CharacterMotionSystem>();
        GameObject root = motion != null ? motion.gameObject : animator.transform.root.gameObject;
        DisableBehaviour<HWJ_MonsterAISystem>(root);
        DisableBehaviour<HWJ_EnemyNavigationSystem>(root);
        DisableBehaviour<HWJ_EnemyAttackSystem>(root);
        DisableBehaviour<HWJ_CharacterMotionSystem>(root);
        animator.enabled = true;
        animator.speed = 1f;
        return true;
    }

    private static void BeginIdle()
    {
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsDead", false);
        animator.ResetTrigger("AttackTrigger");
        animator.ResetTrigger("HitTrigger");
        animator.ResetTrigger("DeadTrigger");
        animator.Play("hys_Rat_Idle", 0, 0f);
        animator.Update(0f);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(1);
    }

    private static void ValidateIdle()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.25f);
        RequireState("hys_Rat_Idle");
        RequireSpriteChanged("Idle");
        RequireLegacyPartsHidden();
        animator.SetBool("IsMoving", true);
        animator.Update(0f);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(2);
    }

    private static void ValidateRunStart()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.25f);
        RequireState("hys_Rat_RunStart");
        RequireSpriteChanged("RUN1");
        SetPhase(3);
    }

    private static void ValidateRunLoopEntered()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.4f);
        RequireState("hys_Rat_RunLoop");
        phaseSprite = spriteRenderer.sprite;
        SetPhase(4);
    }

    private static void ValidateRunLoopFrames()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.25f);
        RequireState("hys_Rat_RunLoop");
        RequireSpriteChanged("RUN2");
        animator.SetBool("IsMoving", false);
        animator.SetTrigger("AttackTrigger");
        animator.Update(0f);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(5);
    }

    private static void ValidateAttack()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.25f);
        RequireState("hys_Rat_Attack");
        RequireSpriteChanged("Attack");
        SetPhase(6);
    }

    private static void ValidateAttackExit()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.65f);
        RequireState("hys_Rat_Idle");
        animator.SetTrigger("HitTrigger");
        animator.Update(0f);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(7);
    }

    private static void ValidateHurt()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.25f);
        RequireState("hys_Rat_Hurt");
        RequireSpriteChanged("Hurt");
        SetPhase(8);
    }

    private static void ValidateHurtExit()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.75f);
        RequireState("hys_Rat_Idle");
        animator.SetTrigger("DeadTrigger");
        animator.SetBool("IsDead", true);
        animator.Update(0f);
        phaseSprite = spriteRenderer.sprite;
        SetPhase(9);
    }

    private static void ValidateDie()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.25f);
        RequireState("hys_Rat_Die");
        RequireSpriteChanged("Die");
        SetPhase(10);
    }

    private static void ValidateDieCompletion()
    {
        if (Elapsed < 0.1f) return;
        animator.Update(0.65f);
        RequireState("hys_Rat_Die");
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
            throw new InvalidOperationException($"상태 전환 실패: 예상={expected}, hash={state.shortNameHash}");
        }
    }

    private static void RequireSpriteChanged(string motion)
    {
        if (spriteRenderer.sprite == null || spriteRenderer.sprite == phaseSprite)
        {
            throw new InvalidOperationException($"{motion} 애니메이션 프레임이 변경되지 않았습니다.");
        }
    }

    private static void RequireLegacyPartsHidden()
    {
        string[] names = { "LeftEar", "RightEar", "Tail" };
        foreach (string name in names)
        {
            Transform child = animator.transform.Find(name);
            SpriteRenderer renderer = child != null ? child.GetComponent<SpriteRenderer>() : null;
            if (renderer != null && renderer.enabled)
            {
                throw new InvalidOperationException($"기존 조립형 부품이 표시 중입니다: {name}");
            }
        }
    }

    private static void DisableBehaviour<T>(GameObject root) where T : Behaviour
    {
        T behaviour = root.GetComponent<T>();
        if (behaviour != null) behaviour.enabled = false;
    }

    private static void Pass()
    {
        Debug.Log("[hys Rat Validation] PASS - Idle/RUN1/RUN2/Attack/Hurt/Die 전환과 프레임 변경을 확인했습니다.");
        Finish(0);
    }

    private static void Fail(string reason)
    {
        Debug.LogError($"[hys Rat Validation] FAIL - {reason}");
        Finish(1);
    }

    private static void Finish(int exitCode)
    {
        EditorApplication.update -= ValidateUpdate;
        SessionState.SetBool(RunningKey, false);
        Application.runInBackground = SessionState.GetBool(PreviousRunInBackgroundKey, false);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
            return;
        }

        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
