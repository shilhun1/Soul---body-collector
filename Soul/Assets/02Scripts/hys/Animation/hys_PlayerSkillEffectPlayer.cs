using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 애니메이션 이벤트를 받아 캐릭터 스프라이트와 분리된 이펙트를 재생합니다.
/// 명령 형식: 시트이름|크기|X오프셋|Y오프셋|회전|초당프레임
/// </summary>
[DisallowMultipleComponent]
public sealed class hys_PlayerSkillEffectPlayer : MonoBehaviour
{
    private const string ResourceRoot = "hys/PlayerSkillEffects/";
    private const int SheetColumns = 4;
    private const int SheetRows = 4;
    private const float PixelsPerUnit = 512f;
    private const int SortingOrderOffset = 10;

    private readonly Dictionary<string, Sprite[]> cachedFrames = new Dictionary<string, Sprite[]>();
    private readonly List<GameObject> activeEffects = new List<GameObject>();
    private SpriteRenderer bodyRenderer;

    /// <summary>
    /// Animation Event에서 호출하며, 한 스킬 안의 연속 타격도 서로 끊지 않고 겹쳐 재생합니다.
    /// </summary>
    public void hys_PlaySkillEffect(string command)
    {
        if (!TryParseCommand(command, out string sheetName, out float scale,
                out float offsetX, out float offsetY, out float rotation, out float frameRate))
        {
            Debug.LogWarning($"[hys Skill Effect] 잘못된 이펙트 명령입니다: {command}", this);
            return;
        }

        Sprite[] frames = GetFrames(sheetName);
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[hys Skill Effect] Resources에서 시트를 찾지 못했습니다: {sheetName}", this);
            return;
        }

        EnsureBodyRenderer();
        bool isFacingLeft = bodyRenderer != null && bodyRenderer.flipX;

        GameObject effectObject = new GameObject($"hys_SkillEffect_{sheetName}");
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = new Vector3(
            isFacingLeft ? -offsetX : offsetX,
            offsetY,
            0f);
        effectObject.transform.localRotation = Quaternion.Euler(0f, 0f, isFacingLeft ? -rotation : rotation);
        effectObject.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = frames[0];
        effectRenderer.flipX = isFacingLeft;
        if (bodyRenderer != null)
        {
            effectRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            effectRenderer.sortingOrder = bodyRenderer.sortingOrder + SortingOrderOffset;
        }

        activeEffects.Add(effectObject);
        StartCoroutine(PlayFrames(effectObject, effectRenderer, frames, frameRate));
    }

    private IEnumerator PlayFrames(
        GameObject effectObject,
        SpriteRenderer effectRenderer,
        Sprite[] frames,
        float frameRate)
    {
        float frameSeconds = 1f / Mathf.Max(1f, frameRate);
        for (int i = 0; i < frames.Length; i++)
        {
            if (effectRenderer == null)
            {
                yield break;
            }

            effectRenderer.sprite = frames[i];
            yield return new WaitForSeconds(frameSeconds);
        }

        activeEffects.Remove(effectObject);
        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }

    private Sprite[] GetFrames(string sheetName)
    {
        if (cachedFrames.TryGetValue(sheetName, out Sprite[] cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + "hys_SkillEffect_" + sheetName);
        if (texture == null)
        {
            cachedFrames[sheetName] = null;
            return null;
        }

        int frameWidth = texture.width / SheetColumns;
        int frameHeight = texture.height / SheetRows;
        Sprite[] frames = new Sprite[SheetColumns * SheetRows];
        for (int row = 0; row < SheetRows; row++)
        {
            for (int column = 0; column < SheetColumns; column++)
            {
                int index = row * SheetColumns + column;
                Rect rect = new Rect(
                    column * frameWidth,
                    texture.height - ((row + 1) * frameHeight),
                    frameWidth,
                    frameHeight);
                frames[index] = Sprite.Create(
                    texture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                frames[index].name = $"hys_SkillEffect_{sheetName}_{index:00}";
            }
        }

        cachedFrames[sheetName] = frames;
        return frames;
    }

    private void EnsureBodyRenderer()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private static bool TryParseCommand(
        string command,
        out string sheetName,
        out float scale,
        out float offsetX,
        out float offsetY,
        out float rotation,
        out float frameRate)
    {
        sheetName = string.Empty;
        scale = 1f;
        offsetX = 0f;
        offsetY = 0f;
        rotation = 0f;
        frameRate = 18f;

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        string[] parts = command.Split('|');
        sheetName = parts[0].Trim();
        return !string.IsNullOrEmpty(sheetName)
            && TryReadFloat(parts, 1, ref scale)
            && TryReadFloat(parts, 2, ref offsetX)
            && TryReadFloat(parts, 3, ref offsetY)
            && TryReadFloat(parts, 4, ref rotation)
            && TryReadFloat(parts, 5, ref frameRate);
    }

    private static bool TryReadFloat(string[] parts, int index, ref float value)
    {
        if (index >= parts.Length)
        {
            return true;
        }

        return float.TryParse(
            parts[index],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i] != null)
            {
                Destroy(activeEffects[i]);
            }
        }

        activeEffects.Clear();
    }
}
