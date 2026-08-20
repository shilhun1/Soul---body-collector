#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 허리와 하체가 이어지도록 보정한 도끼 Walk 프레임을 클립에 연결합니다.
/// </summary>
public static class hys_MonsterLocomotionFrameFixer
{
    private const string SourceSpritePath =
        "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions/hys_RedKing_Run_00.png";

    private const string FixedWalkFolder =
        "Assets/05Anims/hys_Enemy_Anims/Axe/Sprites/hys_Enemy_Axe_WalkFixed";

    private const string WalkClipPath =
        "Assets/05Anims/hys_Enemy_Anims/Axe/Clips/hys_Enemy_Axe_Walk.anim";

    private const int FrameCount = 8;
    private const float FrameInterval = 0.1f;

    [MenuItem("Tools/hys/Animation/도끼 Walk 허리 연결 프레임 적용")]
    public static void Fix()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        TextureImporter sourceImporter = AssetImporter.GetAtPath(SourceSpritePath) as TextureImporter;
        if (sourceImporter == null)
        {
            throw new InvalidOperationException($"기준 스프라이트 Import 설정을 찾을 수 없습니다: {SourceSpritePath}");
        }

        Sprite[] sprites = new Sprite[FrameCount];
        for (int index = 0; index < FrameCount; index++)
        {
            string spritePath = $"{FixedWalkFolder}/hys_Enemy_Axe_WalkFixed_{index:00}.png";
            ApplySpriteImportSettings(sourceImporter, spritePath);

            sprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[index] == null)
            {
                throw new InvalidOperationException($"보정된 도끼 Walk 스프라이트를 찾을 수 없습니다: {spritePath}");
            }
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
        if (clip == null)
        {
            throw new InvalidOperationException($"도끼 Walk 클립을 찾을 수 없습니다: {WalkClipPath}");
        }

        ObjectReferenceKeyframe[] spriteKeys = new ObjectReferenceKeyframe[FrameCount];
        for (int index = 0; index < FrameCount; index++)
        {
            spriteKeys[index] = new ObjectReferenceKeyframe
            {
                time = index * FrameInterval,
                value = sprites[index]
            };
        }

        clip.ClearCurves();
        clip.frameRate = 10f;
        clip.wrapMode = WrapMode.Loop;

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeys);

        // 마지막 프레임도 0.8초까지 유지하여 반복 경계에서 프레임이 튀지 않게 합니다.
        EditorCurveBinding alphaBinding = EditorCurveBinding.FloatCurve(
            string.Empty, typeof(SpriteRenderer), "m_Color.a");
        AnimationUtility.SetEditorCurve(
            clip,
            alphaBinding,
            AnimationCurve.Constant(0f, FrameCount * FrameInterval, 1f));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[hys Locomotion] 허리와 하체가 연결된 도끼 Walk 8프레임 적용 완료");
    }

    private static void ApplySpriteImportSettings(TextureImporter sourceImporter, string spritePath)
    {
        TextureImporter targetImporter = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (targetImporter == null)
        {
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport);
            targetImporter = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        }

        if (targetImporter == null)
        {
            throw new InvalidOperationException($"도끼 Walk Import 설정을 만들 수 없습니다: {spritePath}");
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        sourceImporter.ReadTextureSettings(settings);
        targetImporter.SetTextureSettings(settings);
        targetImporter.textureType = TextureImporterType.Sprite;
        targetImporter.spriteImportMode = SpriteImportMode.Single;
        targetImporter.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;
        targetImporter.alphaIsTransparency = true;
        targetImporter.mipmapEnabled = false;
        targetImporter.filterMode = FilterMode.Point;
        targetImporter.textureCompression = TextureImporterCompression.Uncompressed;
        targetImporter.SaveAndReimport();
    }
}
#endif
