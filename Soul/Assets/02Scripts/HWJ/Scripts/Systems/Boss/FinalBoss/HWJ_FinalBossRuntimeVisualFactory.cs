using UnityEngine;

/// <summary>
/// 최종보스 아트가 연결되기 전에도 Game View에서 패턴을 확인할 수 있는 표시 오브젝트를 만듭니다.
/// 실제 프리팹이 지정되면 임시 사각형 대신 해당 프리팹을 사용합니다.
/// </summary>
public static class HWJ_FinalBossRuntimeVisualFactory
{
    private static Sprite whiteSprite;

    public static GameObject Create(
        GameObject visualPrefab,
        string objectName,
        Vector3 worldPosition,
        Transform parent,
        Vector2 placeholderSize,
        Color placeholderColor,
        int sortingOrder)
    {
        GameObject instance;

        if (visualPrefab != null)
        {
            instance = Object.Instantiate(visualPrefab, worldPosition, Quaternion.identity, parent);
            instance.name = objectName;
            return instance;
        }

        instance = new GameObject(objectName);
        instance.transform.SetParent(parent, true);
        instance.transform.position = worldPosition;
        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = placeholderColor;
        renderer.sortingOrder = sortingOrder;
        instance.transform.localScale = new Vector3(
            Mathf.Max(0.05f, placeholderSize.x),
            Mathf.Max(0.05f, placeholderSize.y),
            1f);
        return instance;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "HWJ_FinalBoss_RuntimeWhiteTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        whiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        whiteSprite.name = "HWJ_FinalBoss_RuntimeWhiteSprite";
        whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return whiteSprite;
    }
}
