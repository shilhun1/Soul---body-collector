#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HWJ의 몬스터 패턴 시간에 맞춰 검증된 플레이어 도트 프레임으로 5종 패턴 클립을 구성합니다.
/// 별도 이펙트 프레임과 이벤트는 넣지 않고 캐릭터 본체 동작만 사용합니다.
/// </summary>
public static class hys_MonsterPatternAnimationGenerator
{
    private const float FrameRate = 12f;

    private sealed class PatternSpec
    {
        public string Weapon;
        public string SkillId;
        public float Duration;
        public string[] Motions;
    }

    private sealed class SpriteFrame
    {
        public int Index;
        public string Path;
        public Sprite Sprite;
    }

    private static readonly PatternSpec[] Patterns =
    {
        Pattern("Sword", "sword_diagonal_slash", 0.65f, "ForwardAttack1:0,1,5-7"),
        Pattern("Sword", "sword_up_diagonal_slash", 0.80f, "ForwardAttack2:0,1,4-7"),
        Pattern("Sword", "sword_wave", 1.00f, "ForwardAttack1:0,1,5-7", "ForwardAttack2:0,1,4-7"),

        Pattern("Axe", "axe_swing", 0.85f, "Attack1:0-3"),
        // 달리기 반복 대신 한 번 이어지는 대시 자세를 사용해 돌진 중 상하 흔들림을 없앱니다.
        Pattern("Axe", "axe_body_charge", 1.00f, "Dash:0-3", "Attack2:0-4"),
        Pattern("Axe", "axe_spin_charge", 1.20f, "Attack1:0-3", "Attack2:0-3", "Attack1:0-3"),

        // 점프 정점을 거친 뒤 공중에 어울리는 낮은 사격 자세로 연결합니다.
        Pattern("Bow", "bow_air_arrow_shot", 0.90f,
            "Bow_JumpStart:0-2", "Bow_JumpApex:0-2", "Bow_Attack2:0-3", "Bow_JumpFall:0-2"),
        Pattern("Bow", "bow_rapid_shot", 1.00f, "Bow_Attack1", "Bow_Attack1", "Bow_Attack1"),
        Pattern("Bow", "bow_low_charge_shot", 1.20f, "Bow_Attack2:0-2,5", "Bow_Attack2:0-2,5"),

        // 준비 자세로 되튀는 구간을 줄이고 대시에서 찌르기로 한 방향으로 이어지게 구성합니다.
        Pattern("Lance", "lance_charge_thrust", 0.90f, "Dash:0-3", "Attack1:1-5"),
        Pattern("Lance", "lance_thrust_combo", 0.90f, "Attack1:0-5", "Attack2:0-5"),
        Pattern("Lance", "lance_finisher_thrust", 1.00f, "Dash:0-3", "Attack2:1-5"),

        Pattern("Shield", "shield_charge", 1.00f, "Run", "Attack"),
        Pattern("Shield", "shield_slam", 1.10f, "JumpStart", "JumpFall", "PlungeLand:3", "Attack:0-2"),
        Pattern("Shield", "shield_diagonal_knockback", 0.90f, "Attack:0-5")
    };

    [MenuItem("Tools/hys/Animation/몬스터 5종 패턴 애니메이션 갱신")]
    public static void Generate()
    {
        int updated = 0;

        foreach (PatternSpec pattern in Patterns)
        {
            string clipPath = $"Assets/05Anims/hys_Enemy_Anims/{pattern.Weapon}/Clips/" +
                $"hys_Enemy_{pattern.Weapon}_{pattern.SkillId}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            if (clip == null)
            {
                Debug.LogError($"[hys Monster Pattern] 클립을 찾을 수 없습니다: {clipPath}");
                continue;
            }

            List<Sprite> sprites = new List<Sprite>();
            foreach (string motion in pattern.Motions)
            {
                sprites.AddRange(LoadMotionSprites(pattern.Weapon, motion));
            }

            if (sprites.Count < 2)
            {
                Debug.LogError($"[hys Monster Pattern] 동작 프레임이 부족합니다: {pattern.SkillId}");
                continue;
            }

            RebuildClip(clip, sprites, pattern.Duration);
            updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[hys Monster Pattern] 5종 패턴 {updated}/{Patterns.Length}개 갱신 완료");
    }

    private static void RebuildClip(AnimationClip clip, List<Sprite> sprites, float duration)
    {
        clip.ClearCurves();
        clip.frameRate = FrameRate;
        clip.wrapMode = WrapMode.Once;

        float frameDuration = 1f / FrameRate;
        float lastFrameTime = Mathf.Max(0f, duration - frameDuration);
        float interval = lastFrameTime / (sprites.Count - 1);
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count];

        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i * interval, value = sprites[i] };
        }

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keys);

        // 마지막 도트가 HWJ 패턴 종료 시각까지 유지되도록 길이 전용 곡선을 둡니다.
        EditorCurveBinding durationBinding = EditorCurveBinding.FloatCurve(
            string.Empty, typeof(SpriteRenderer), "m_Color.a");
        AnimationUtility.SetEditorCurve(
            clip, durationBinding, AnimationCurve.Constant(0f, duration, 1f));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        EditorUtility.SetDirty(clip);
    }

    private static List<Sprite> LoadMotionSprites(string weapon, string motionSpec)
    {
        string[] parts = motionSpec.Split(':');
        string motionName = parts[0];
        HashSet<int> selection = parts.Length > 1 ? ParseSelection(parts[1]) : null;
        string folder = $"Assets/05Anims/hys_Player_Anims/{weapon}/Sprites";
        string marker = $"_{motionName}_";
        List<SpriteFrame> frames = new List<SpriteFrame>();

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            int markerIndex = name.LastIndexOf(marker, StringComparison.Ordinal);

            if (markerIndex < 0 ||
                !int.TryParse(name.Substring(markerIndex + marker.Length), out int index) ||
                (selection != null && !selection.Contains(index)))
            {
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                frames.Add(new SpriteFrame { Index = index, Path = path, Sprite = sprite });
            }
        }

        frames.Sort((left, right) =>
        {
            int indexCompare = left.Index.CompareTo(right.Index);
            return indexCompare != 0 ? indexCompare : string.CompareOrdinal(left.Path, right.Path);
        });

        List<Sprite> result = new List<Sprite>(frames.Count);
        foreach (SpriteFrame frame in frames)
        {
            result.Add(frame.Sprite);
        }

        return result;
    }

    private static HashSet<int> ParseSelection(string value)
    {
        HashSet<int> result = new HashSet<int>();

        foreach (string segment in value.Split(','))
        {
            string[] range = segment.Split('-');
            if (!int.TryParse(range[0], out int start))
            {
                continue;
            }

            int end = start;
            if (range.Length > 1 && !int.TryParse(range[1], out end))
            {
                end = start;
            }

            for (int index = Mathf.Min(start, end); index <= Mathf.Max(start, end); index++)
            {
                result.Add(index);
            }
        }

        return result;
    }

    private static PatternSpec Pattern(
        string weapon, string skillId, float duration, params string[] motions)
    {
        return new PatternSpec
        {
            Weapon = weapon,
            SkillId = skillId,
            Duration = duration,
            Motions = motions
        };
    }
}
#endif
