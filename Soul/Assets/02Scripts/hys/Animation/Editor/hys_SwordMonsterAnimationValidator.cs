using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Sword 몬스터 전용 모션, 보호 클립, Animator 연결을 자동 검증하고 보고서를 남깁니다.
[InitializeOnLoad]
public static class hys_SwordMonsterAnimationValidator
{
    private const string SessionKey = "hys.SwordMonsterAnimationValidator.20260727.v4";
    private const string SwordRoot = "Assets/05Anims/hys_Enemy_Anims/Sword";
    private const string ControllerPath = SwordRoot + "/hys_Monster_Sword.controller";
    private const string MissingClipRoot = SwordRoot + "/Clips/MissingMotions";
    private const string RedQueenRoot = "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen";
    private const string ReportPath = "Reports/hys_SwordMonsterAnimationValidation_20260727.txt";

    private static readonly Dictionary<string, string> ProtectedHashes = new Dictionary<string, string>
    {
        // 현재 저장소가 추적하는 정상 Death 클립의 SHA-256으로 기준을 맞춥니다.
        { "hys_Enemy_Sword_Death.anim", "FF4F62AFA546A1A6FC3EFF173B0D7C3ABEDE16971E19D39991C2AA865C2288C0" },
        { "hys_Enemy_Sword_Hit.anim", "042F58E368ACA4CCC8DB6FD5F31438D529D2AAB2D4B53A9BE5F41A3B545929F9" },
        // 실제 R_Queen 검 프레임으로 갱신한 Sword 1·2번 클립을 보호합니다.
        { "hys_Enemy_Sword_sword_diagonal_slash.anim", "8F3298B542EDDE5FC14822EDCD716A6EBE066D5FE961F05448D7F9DA8B410C9E" },
        { "hys_Enemy_Sword_sword_up_diagonal_slash.anim", "AEB08B8A5A261BEDA766CBFB4661F208EF26A6C2F7870439543918967D2983D4" },
        { "hys_Enemy_Sword_Idle.anim", "6F2956DEDF055CFF12E4562C8FA225CD992FAAF81E861F8D5137E54D6115A4EF" }
    };

    static hys_SwordMonsterAnimationValidator()
    {
        EditorApplication.delayCall += RunAutomaticallyOnce;
    }

    [MenuItem("Tools/HYS/Animation/Sword 몬스터 애니메이션 검증")]
    public static void Validate()
    {
        List<string> results = new List<string>
        {
            "hys Sword Monster Animation Validation - 2026-07-27",
            ""
        };

        ValidateProtectedHashes(results);
        AnimatorController controller = RequireAsset<AnimatorController>(ControllerPath);
        ValidateParameters(controller, results);
        ValidateStatesAndClips(controller, results);
        ValidateGeneratedSprites(results);

        results.Add("");
        results.Add("RESULT: PASS");
        string absoluteReportPath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            ReportPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteReportPath));
        File.WriteAllLines(absoluteReportPath, results);
        AssetDatabase.Refresh();
        Debug.Log("[hys Sword Monster Validation] PASS - " + ReportPath);
    }

    public static void ValidateFromCommandLine()
    {
        Validate();
    }

    private static void RunAutomaticallyOnce()
    {
        if (SessionState.GetBool(SessionKey, false)
            || EditorApplication.isCompiling
            || EditorApplication.isUpdating)
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        try
        {
            Validate();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ValidateProtectedHashes(List<string> results)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        foreach (KeyValuePair<string, string> pair in ProtectedHashes)  
        {
            string assetPath = SwordRoot + "/Clips/" + pair.Key;
            string absolutePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
            string actual = ComputeSha256(absolutePath);
            Require(actual == pair.Value, "보호 클립 해시가 변경되었습니다: " + pair.Key);
            results.Add("PASS protected hash: " + pair.Key + " = " + actual);
        }
    }

    private static void ValidateParameters(AnimatorController controller, List<string> results)
    {
        RequireParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        RequireParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        RequireParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        RequireParameter(controller, "M_sword_1", AnimatorControllerParameterType.Trigger);
        RequireParameter(controller, "M_sword_2", AnimatorControllerParameterType.Trigger);
        RequireParameter(controller, "M_sword_3", AnimatorControllerParameterType.Trigger);
        RequireParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
        RequireParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        RequireParameter(controller, "Dash", AnimatorControllerParameterType.Trigger);
        results.Add("PASS Animator parameters: existing 6 + motion 3");
    }

    private static void ValidateStatesAndClips(AnimatorController controller, List<string> results)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        // 현재 공용 Sword Walk는 저장소 기준 6프레임·8fps 루프입니다.
        ValidateStateMotion(machine, "hys_Monster_Sword_Walk", SwordRoot + "/Clips/hys_Enemy_Sword_Walk.anim", 6, 8f, true, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_Jump_Start", MissingClipRoot + "/hys_Enemy_Sword_Jump_Start.anim", 4, 12f, false, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_Jump_Rise", MissingClipRoot + "/hys_Enemy_Sword_Jump_Rise.anim", 4, 12f, false, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_Jump_Apex", MissingClipRoot + "/hys_Enemy_Sword_Jump_Apex.anim", 4, 10f, false, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_Jump_Fall", MissingClipRoot + "/hys_Enemy_Sword_Jump_Fall.anim", 4, 10f, true, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_Landing", MissingClipRoot + "/hys_Enemy_Sword_Landing.anim", 5, 14f, false, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_Dash", MissingClipRoot + "/hys_Enemy_Sword_Dash.anim", 7, 18f, false, results);
        ValidateStateMotion(machine, "hys_Monster_Sword_sword_wave", SwordRoot + "/Clips/hys_Enemy_Sword_sword_wave.anim", 9, 12f, false, results);

        ValidateProtectedState(machine, "hys_Monster_Sword_Idle", "hys_Enemy_Sword_Idle.anim", results);
        ValidateProtectedState(machine, "hys_Monster_Sword_Hit", "hys_Enemy_Sword_Hit.anim", results);
        ValidateProtectedState(machine, "hys_Monster_Sword_Death", "hys_Enemy_Sword_Death.anim", results);
        ValidateProtectedState(machine, "hys_Monster_Sword_sword_diagonal_slash", "hys_Enemy_Sword_sword_diagonal_slash.anim", results);
        ValidateProtectedState(machine, "hys_Monster_Sword_sword_up_diagonal_slash", "hys_Enemy_Sword_sword_up_diagonal_slash.anim", results);

        RequireTransition(machine, "hys_Monster_Sword_Idle", "hys_Monster_Sword_Jump_Start", "IsGrounded");
        RequireTransition(machine, "hys_Monster_Sword_Walk", "hys_Monster_Sword_Jump_Start", "VerticalSpeed");
        RequireTransition(machine, "hys_Monster_Sword_Jump_Rise", "hys_Monster_Sword_Jump_Apex", "VerticalSpeed");
        RequireTransition(machine, "hys_Monster_Sword_Jump_Apex", "hys_Monster_Sword_Jump_Fall", "VerticalSpeed");
        RequireTransition(machine, "hys_Monster_Sword_Jump_Fall", "hys_Monster_Sword_Landing", "IsGrounded");
        RequireTransition(machine, "hys_Monster_Sword_Idle", "hys_Monster_Sword_Dash", "Dash");
        results.Add("PASS Animator transitions: jump chain, landing, dash");
    }

    private static void ValidateGeneratedSprites(List<string> results)
    {
        string[] folders =
        {
            "Walk_HandDrawn", "Jump_Start", "Jump_Rise", "Jump_Apex",
            "Jump_Fall", "Landing", "Dash", "Pattern3_SwordWave"
        };
        int total = 0;
        HashSet<string> sourceHashes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> handDrawnHashes = new HashSet<string>(StringComparer.Ordinal);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string sourceFolder = Path.Combine(projectRoot, RedQueenRoot.Replace('/', Path.DirectorySeparatorChar));
        foreach (string sourcePath in Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly))
        {
            sourceHashes.Add(ComputeSha256(sourcePath));
        }

        foreach (string folder in folders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { SwordRoot + "/Sprites/" + folder });
            Require(guids.Length > 0, "생성 Sprite가 없습니다: " + folder);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = RequireAsset<Sprite>(path);
                Require(Mathf.Approximately(sprite.rect.width, 50f) && Mathf.Approximately(sprite.rect.height, 50f), "Sprite 크기가 50x50이 아닙니다: " + path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null && importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed, "픽셀 Import 설정이 올바르지 않습니다: " + path);
                string absolutePath = Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
                string handDrawnHash = ComputeSha256(absolutePath);
                Require(!sourceHashes.Contains(handDrawnHash), "정상 원본 PNG를 그대로 복사한 프레임입니다: " + path);
                Require(handDrawnHashes.Add(handDrawnHash), "직접 도트 프레임끼리 중복되었습니다: " + path);
                total++;
            }
        }
        Require(total == 36, "생성 Sprite 수가 예상과 다릅니다: " + total);
        results.Add("PASS hand-drawn sprites: 36 unique files, no source PNG copies, 50x50, Point, no mipmap, uncompressed");
    }

    private static void ValidateStateMotion(
        AnimatorStateMachine machine,
        string stateName,
        string clipPath,
        int expectedKeyframes,
        float expectedFrameRate,
        bool expectedLoop,
        List<string> results)
    {
        AnimatorState state = RequireState(machine, stateName);
        AnimationClip clip = RequireAsset<AnimationClip>(clipPath);
        Require(state.motion == clip, "상태에 전용 클립이 연결되지 않았습니다: " + stateName);
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        Require(frames != null && frames.Length == expectedKeyframes, "클립 프레임 수가 다릅니다: " + clip.name);
        Require(Mathf.Approximately(clip.frameRate, expectedFrameRate), "클립 frameRate가 다릅니다: " + clip.name);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        Require(settings.loopTime == expectedLoop, "클립 Loop 설정이 다릅니다: " + clip.name);

        // Edit Mode의 SampleAnimation은 Sprite PPtr를 적용하지 않는 경우가 있어 실제 곡선 값을 직접 샘플링합니다.
        foreach (ObjectReferenceKeyframe frame in frames)
        {
            Require(frame.value is Sprite, "클립 프레임의 Sprite 참조가 비었습니다: " + clip.name);
        }
        results.Add("PASS state/clip/sample: " + stateName + " -> " + clip.name);
    }

    private static void ValidateProtectedState(
        AnimatorStateMachine machine,
        string stateName,
        string fileName,
        List<string> results)
    {
        AnimationClip expected = RequireAsset<AnimationClip>(SwordRoot + "/Clips/" + fileName);
        Require(RequireState(machine, stateName).motion == expected, "보호 상태의 클립 연결이 바뀌었습니다: " + stateName);
        results.Add("PASS protected state motion: " + stateName);
    }

    private static void RequireTransition(
        AnimatorStateMachine machine,
        string fromName,
        string toName,
        string parameter)
    {
        AnimatorState from = RequireState(machine, fromName);
        AnimatorState to = RequireState(machine, toName);
        foreach (AnimatorStateTransition transition in from.transitions)
        {
            if (transition.destinationState != to) continue;
            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == parameter) return;
            }
        }
        throw new InvalidOperationException("필수 전이가 없습니다: " + fromName + " -> " + toName + " / " + parameter);
    }

    private static AnimatorState RequireState(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == name) return child.state;
        }
        throw new InvalidOperationException("Animator 상태가 없습니다: " + name);
    }

    private static void RequireParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name && parameter.type == type) return;
        }
        throw new InvalidOperationException("Animator 파라미터가 없거나 형식이 다릅니다: " + name);
    }

    private static T RequireAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Require(asset != null, "Asset을 찾지 못했습니다: " + path);
        return asset;
    }

    private static string ComputeSha256(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
