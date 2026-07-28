using UnityEngine;

/// <summary>
/// 두 번째 보스의 실제 그래픽이 준비되기 전까지 사용하는 임시 마력 검 오브젝트입니다.
/// 충돌체 없이 보이기만 하므로 기존 공격 판정에는 영향을 주지 않습니다.
/// </summary>
public class hys_SecondBossMagicVisual : MonoBehaviour
{
    private static Sprite whiteSprite;

    private Vector3 velocity;
    private float spawnTime;
    private float endTime;
    private SpriteRenderer[] renderers;
    private Transform followTarget;
    private Transform homingTarget;
    private float homingSpeed;

    public static GameObject SpawnSword(
        Vector3 position,
        Vector2 direction,
        float lifetime,
        float length,
        float width,
        float moveSpeed,
        Color color)
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        GameObject root = new GameObject("hys_Phase2MagicSword");
        root.transform.position = position;
        root.transform.rotation = Quaternion.FromToRotation(Vector3.up, safeDirection);

        float safeLength = Mathf.Max(0.5f, length);
        float safeWidth = Mathf.Max(0.08f, width);
        CreatePart(root.transform, "Blade", new Vector3(0f, safeLength * 0.2f, 0f),
            new Vector3(safeWidth, safeLength * 0.72f, 1f), color);
        CreatePart(root.transform, "Guard", new Vector3(0f, -safeLength * 0.18f, 0f),
            new Vector3(safeWidth * 3.2f, safeWidth * 0.45f, 1f), color * new Color(0.75f, 0.75f, 1f, 1f));
        CreatePart(root.transform, "Hilt", new Vector3(0f, -safeLength * 0.31f, 0f),
            new Vector3(safeWidth * 0.55f, safeLength * 0.25f, 1f), color * new Color(0.55f, 0.45f, 0.8f, 1f));

        hys_SecondBossMagicVisual visual = root.AddComponent<hys_SecondBossMagicVisual>();
        visual.velocity = safeDirection * Mathf.Max(0f, moveSpeed);
        visual.spawnTime = Time.time;
        visual.endTime = Time.time + Mathf.Max(0.05f, lifetime);
        visual.renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    /// <summary>현재 대상의 공중 위치까지 계속 방향을 보정하는 마력 검을 생성합니다.</summary>
    public static GameObject SpawnHomingSword(
        Vector3 position,
        Transform target,
        float lifetime,
        float length,
        float width,
        float moveSpeed,
        Color color)
    {
        Vector2 initialDirection = target != null
            ? (Vector2)(target.position - position)
            : Vector2.down;
        GameObject sword = SpawnSword(
            position,
            initialDirection,
            lifetime,
            length,
            width,
            0f,
            color);
        hys_SecondBossMagicVisual visual = sword.GetComponent<hys_SecondBossMagicVisual>();
        visual.homingTarget = target;
        visual.homingSpeed = Mathf.Max(0.1f, moveSpeed);
        return sword;
    }

    /// <summary>2초 동안 플레이어의 위치를 따라다니는 임시 마법 표식을 생성합니다.</summary>
    public static GameObject SpawnTrackingMark(
        Transform target,
        float lifetime,
        float radius,
        Color color)
    {
        GameObject root = new GameObject("hys_Phase2TrackingMark");
        root.transform.position = target != null ? target.position : Vector3.zero;
        float safeRadius = Mathf.Max(0.25f, radius);
        float partSize = Mathf.Max(0.12f, safeRadius * 0.22f);

        CreatePart(root.transform, "MarkTop", new Vector3(0f, safeRadius, 0f),
            new Vector3(partSize, partSize, 1f), color, 45f);
        CreatePart(root.transform, "MarkBottom", new Vector3(0f, -safeRadius, 0f),
            new Vector3(partSize, partSize, 1f), color, 45f);
        CreatePart(root.transform, "MarkLeft", new Vector3(-safeRadius, 0f, 0f),
            new Vector3(partSize, partSize, 1f), color, 45f);
        CreatePart(root.transform, "MarkRight", new Vector3(safeRadius, 0f, 0f),
            new Vector3(partSize, partSize, 1f), color, 45f);

        hys_SecondBossMagicVisual visual = root.AddComponent<hys_SecondBossMagicVisual>();
        visual.velocity = Vector3.zero;
        visual.followTarget = target;
        visual.spawnTime = Time.time;
        visual.endTime = Time.time + Mathf.Max(0.05f, lifetime);
        visual.renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    /// <summary>검 모양이 아닌 지면 파동과 파편 형태의 임시 충격파를 생성합니다.</summary>
    public static GameObject SpawnShockwave(
        Vector3 position,
        float lifetime,
        float width,
        float height,
        Color color)
    {
        GameObject root = new GameObject("hys_Phase2GroundShockwave");
        root.transform.position = position;

        float safeWidth = Mathf.Max(0.2f, width);
        float safeHeight = Mathf.Max(0.2f, height);
        CreatePart(root.transform, "WaveCore", new Vector3(0f, safeHeight * 0.08f, 0f),
            new Vector3(safeWidth, safeHeight * 0.18f, 1f), color);
        CreatePart(root.transform, "WaveLeft", new Vector3(-safeWidth * 0.26f, safeHeight * 0.28f, 0f),
            new Vector3(safeWidth * 0.24f, safeHeight * 0.62f, 1f), color, 22f);
        CreatePart(root.transform, "WaveCenter", new Vector3(0f, safeHeight * 0.38f, 0f),
            new Vector3(safeWidth * 0.2f, safeHeight * 0.82f, 1f), color);
        CreatePart(root.transform, "WaveRight", new Vector3(safeWidth * 0.26f, safeHeight * 0.28f, 0f),
            new Vector3(safeWidth * 0.24f, safeHeight * 0.62f, 1f), color, -22f);

        hys_SecondBossMagicVisual visual = root.AddComponent<hys_SecondBossMagicVisual>();
        visual.velocity = Vector3.zero;
        visual.spawnTime = Time.time;
        visual.endTime = Time.time + Mathf.Max(0.05f, lifetime);
        visual.renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    private static void CreatePart(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float localRotationZ = 0f)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = 110;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "hys_Phase2MagicWhiteTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        whiteSprite.name = "hys_Phase2MagicWhiteSprite";
        return whiteSprite;
    }

    private void Update()
    {
        if (followTarget != null)
            transform.position = followTarget.position;

        if (homingTarget != null)
        {
            Vector2 direction = homingTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                velocity = direction * homingSpeed;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            }
        }

        transform.position += velocity * Time.deltaTime;

        float lifetime = Mathf.Max(0.01f, endTime - spawnTime);
        float remainingRatio = Mathf.Clamp01((endTime - Time.time) / lifetime);
        if (remainingRatio < 0.3f && renderers != null)
        {
            float alphaMultiplier = remainingRatio / 0.3f;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color color = renderers[i].color;
                color.a = Mathf.Min(color.a, alphaMultiplier);
                renderers[i].color = color;
            }
        }

        if (Time.time >= endTime) Destroy(gameObject);
    }
}
