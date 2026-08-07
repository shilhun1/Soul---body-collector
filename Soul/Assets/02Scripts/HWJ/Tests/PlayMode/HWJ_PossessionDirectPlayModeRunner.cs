#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 저장하지 않은 작업 씬을 건드리지 않고 빙의 PlayMode 테스트 5개를 실행합니다.
/// 일반 Play 모드의 씬 스냅샷 안에서 빈 런타임 씬으로 이동하므로 종료 후 사용자 씬은 그대로 복원됩니다.
/// </summary>
public sealed class HWJ_PossessionDirectPlayModeRunner : MonoBehaviour
{
    private const string RuntimeFlagPath =
        @"C:\Docs\Generated\HWJ_PossessionDirectPlayModeRuntime.flag";
    private const string SummaryPath =
        @"C:\Docs\Generated\HWJ_PossessionDirectPlayModeSummary.md";

    private static readonly string[] TestMethodNames =
    {
        nameof(HWJ_PossessionModesPlayModeTests.RMinigame_SuccessCostsTenEachTime_AndPreservesLiveBodyHp),
        nameof(HWJ_PossessionModesPlayModeTests.RMinigame_ProgressesWithUnscaledTimeWhileWorldIsPaused),
        nameof(HWJ_PossessionModesPlayModeTests.LiveMentalZero_ReleasesToSpirit_RestoresHostileWithRemainingHp_AndBlocksForever),
        nameof(HWJ_PossessionModesPlayModeTests.ECorpsePossession_IsImmediate_UsesDecay_AndGhostDoesNotDecay),
        nameof(HWJ_PossessionModesPlayModeTests.LiveBodyHpZero_EjectsPlayerToSpiritAndLeavesDeadMonster),
        nameof(HWJ_PossessionModesPlayModeTests.SpiritMentalZero_EntersGameOverDeadState),
        nameof(HWJ_PossessionModesPlayModeTests.PossessionTransition_RestoresBodyPhysics_AndAllowsJump),
        nameof(HWJ_PossessionModesPlayModeTests.PossessionTransition_ReleasesMashGate_AndAcceptsNextSpaceJump)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartWhenRequested()
    {
        if (!File.Exists(RuntimeFlagPath))
        {
            return;
        }

        GameObject runnerObject = new GameObject("HWJ_PossessionDirectPlayModeRunner");
        runnerObject.AddComponent<HWJ_PossessionDirectPlayModeRunner>();
    }

    private IEnumerator Start()
    {
        DateTime startedAt = DateTime.Now;
        List<string> failures = new List<string>();

        yield return IsolateValidationScene();

        for (int i = 0; i < TestMethodNames.Length; i++)
        {
            yield return RunTest(TestMethodNames[i], failures);
        }

        WriteSummary(startedAt, failures);

        if (File.Exists(RuntimeFlagPath))
        {
            File.Delete(RuntimeFlagPath);
        }

        // 일반 Play 모드를 끝내면 Unity가 저장하지 않은 사용자 씬 스냅샷을 원래 상태로 복원합니다.
        EditorApplication.ExitPlaymode();
    }

    private IEnumerator IsolateValidationScene()
    {
        Scene validationScene = SceneManager.CreateScene("HWJ_Possession_RuntimeValidation");
        SceneManager.MoveGameObjectToScene(gameObject, validationScene);
        SceneManager.SetActiveScene(validationScene);

        List<Scene> scenesToUnload = new List<Scene>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.IsValid() && scene != validationScene)
            {
                scenesToUnload.Add(scene);
            }
        }

        for (int i = 0; i < scenesToUnload.Count; i++)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scenesToUnload[i]);

            while (unload != null && !unload.isDone)
            {
                yield return null;
            }
        }

        yield return null;
    }

    private static IEnumerator RunTest(string methodName, List<string> failures)
    {
        HWJ_PossessionModesPlayModeTests test = new HWJ_PossessionModesPlayModeTests();
        Exception failure = null;

        yield return ExecuteEnumerator(test.SetUp(), exception => failure = exception);

        if (failure == null)
        {
            System.Reflection.MethodInfo method = typeof(HWJ_PossessionModesPlayModeTests)
                .GetMethod(methodName);

            if (method == null)
            {
                failure = new MissingMethodException(
                    typeof(HWJ_PossessionModesPlayModeTests).Name,
                    methodName);
            }
            else
            {
                IEnumerator testRoutine = method.Invoke(test, null) as IEnumerator;
                yield return ExecuteEnumerator(testRoutine, exception => failure = exception);
            }
        }

        Exception tearDownFailure = null;
        yield return ExecuteEnumerator(test.TearDown(), exception => tearDownFailure = exception);

        if (failure != null || tearDownFailure != null)
        {
            Exception result = failure ?? tearDownFailure;
            failures.Add($"{methodName}: {Unwrap(result)}");
        }
    }

    /// <summary>
    /// UnityTest의 중첩 IEnumerator와 WaitForSeconds를 그대로 처리하면서 Assertion 예외를 결과 파일에 남깁니다.
    /// </summary>
    private static IEnumerator ExecuteEnumerator(
        IEnumerator root,
        Action<Exception> onFailure)
    {
        if (root == null)
        {
            onFailure?.Invoke(new InvalidOperationException("Test coroutine was null."));
            yield break;
        }

        Stack<IEnumerator> stack = new Stack<IEnumerator>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            IEnumerator current = stack.Peek();
            bool movedNext;
            object yielded = null;

            try
            {
                movedNext = current.MoveNext();

                if (movedNext)
                {
                    yielded = current.Current;
                }
            }
            catch (Exception exception)
            {
                onFailure?.Invoke(exception);
                yield break;
            }

            if (!movedNext)
            {
                stack.Pop();
                continue;
            }

            if (yielded is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }

            yield return yielded;
        }
    }

    private static string Unwrap(Exception exception)
    {
        Exception current = exception;

        while (current is System.Reflection.TargetInvocationException
            && current.InnerException != null)
        {
            current = current.InnerException;
        }

        string stackTrace = string.IsNullOrWhiteSpace(current.StackTrace)
            ? string.Empty
            : $" | {current.StackTrace.Replace(Environment.NewLine, " <- ")}";
        return $"{current.GetType().Name}: {current.Message}{stackTrace}";
    }

    private static void WriteSummary(DateTime startedAt, List<string> failures)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SummaryPath));
        int passed = TestMethodNames.Length - failures.Count;
        StringBuilder report = new StringBuilder();
        report.AppendLine("# HWJ Possession Direct PlayMode Validation");
        report.AppendLine();
        report.AppendLine($"- Status: {(failures.Count == 0 ? "Passed" : "Failed")}");
        report.AppendLine("- Scene handling: isolated runtime scene; user scene not saved or modified");
        report.AppendLine($"- Started: {startedAt:O}");
        report.AppendLine($"- Finished: {DateTime.Now:O}");
        report.AppendLine($"- Total: {TestMethodNames.Length}");
        report.AppendLine($"- Passed: {passed}");
        report.AppendLine($"- Failed: {failures.Count}");

        for (int i = 0; i < failures.Count; i++)
        {
            report.AppendLine($"- Failure: {failures[i]}");
        }

        File.WriteAllText(SummaryPath, report.ToString(), Encoding.UTF8);
    }
}
#endif
