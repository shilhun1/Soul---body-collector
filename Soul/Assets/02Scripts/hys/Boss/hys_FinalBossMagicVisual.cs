using UnityEngine;

public enum hys_FinalBossWeaponVisualType { Sword, Spear, Axe }

/// <summary>
/// 최종보스 그래픽이 준비되기 전 패턴의 위치와 범위를 보여주는 임시 마법 이펙트입니다.
/// </summary>
public class hys_FinalBossMagicVisual : MonoBehaviour
{
    private static Sprite whiteSprite;
    private Vector3 velocity;
    private float endTime;
    private Transform followTarget;
    private SpriteRenderer[] renderers;
    private bool animateScale;
    private float scaleStartTime;
    private float scaleEndTime;
    private Vector3 scaleFrom = Vector3.one;
    private Vector3 scaleTo = Vector3.one;

    public static GameObject SpawnOrb(Vector3 position, float radius, Color color, float lifetime)
    {
        GameObject root = CreateRoot("hys_FinalBossBlackOrb", position, lifetime);
        CreatePart(root.transform, "Orb", Vector3.zero,
            new Vector3(radius * 2f, radius * 2f, 1f), color, 0f, 130);
        root.GetComponent<hys_FinalBossMagicVisual>().renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    public static GameObject SpawnSpear(Vector3 position, Vector2 direction, float length, Color color, float lifetime)
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        GameObject root = CreateRoot("hys_FinalBossDarkSpear", position, lifetime);
        root.transform.rotation = Quaternion.FromToRotation(Vector3.up, safeDirection);
        CreatePart(root.transform, "Shaft", Vector3.zero, new Vector3(0.22f, length, 1f), color, 0f, 130);
        CreatePart(root.transform, "Tip", new Vector3(0f, length * 0.55f, 0f),
            new Vector3(0.65f, 0.9f, 1f), color * 1.2f, 45f, 131);
        root.GetComponent<hys_FinalBossMagicVisual>().renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    public static GameObject SpawnWeapon(Vector3 position, Vector2 direction,
        hys_FinalBossWeaponVisualType weaponType, Color color, float lifetime)
    {
        Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        GameObject root = CreateRoot($"hys_FinalBoss{weaponType}", position, lifetime);
        root.transform.rotation = Quaternion.FromToRotation(Vector3.up, safeDirection);

        if (weaponType == hys_FinalBossWeaponVisualType.Sword)
        {
            CreatePart(root.transform, "Blade", new Vector3(0f, 0.35f, 0f),
                new Vector3(0.34f, 2.8f, 1f), color, 0f, 130);
            CreatePart(root.transform, "Guard", new Vector3(0f, -1f, 0f),
                new Vector3(1.15f, 0.2f, 1f), color, 0f, 131);
        }
        else if (weaponType == hys_FinalBossWeaponVisualType.Spear)
        {
            CreatePart(root.transform, "Shaft", Vector3.zero,
                new Vector3(0.18f, 3.8f, 1f), color, 0f, 130);
            CreatePart(root.transform, "SpearHead", new Vector3(0f, 2f, 0f),
                new Vector3(0.62f, 1.1f, 1f), color, 45f, 131);
        }
        else
        {
            CreatePart(root.transform, "Handle", new Vector3(0f, -0.25f, 0f),
                new Vector3(0.22f, 3.1f, 1f), color, 0f, 130);
            CreatePart(root.transform, "AxeHead", new Vector3(0.55f, 1.15f, 0f),
                new Vector3(1.4f, 1.05f, 1f), color, -18f, 131);
        }

        root.GetComponent<hys_FinalBossMagicVisual>().renderers =
            root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    public static GameObject SpawnRectangle(Vector3 center, Vector2 size, Color color, float lifetime, string objectName)
    {
        GameObject root = CreateRoot(objectName, center, lifetime);
        CreatePart(root.transform, "Area", Vector3.zero,
            new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 1f), color, 0f, 120);
        root.GetComponent<hys_FinalBossMagicVisual>().renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    public static GameObject SpawnLine(Vector3 start, Vector3 end, float width, Color color, float lifetime, string objectName)
    {
        Vector3 delta = end - start;
        GameObject root = CreateRoot(objectName, (start + end) * 0.5f, lifetime);
        root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        CreatePart(root.transform, "Line", Vector3.zero,
            new Vector3(Mathf.Max(0.1f, delta.magnitude), Mathf.Max(0.05f, width), 1f), color, 0f, 125);
        root.GetComponent<hys_FinalBossMagicVisual>().renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    public static GameObject SpawnRing(Transform target, float radius, Color color, float lifetime, string objectName)
    {
        GameObject root = CreateRoot(objectName, target != null ? target.position : Vector3.zero, lifetime);
        hys_FinalBossMagicVisual visual = root.GetComponent<hys_FinalBossMagicVisual>();
        visual.followTarget = target;
        int count = 20;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            CreatePart(root.transform, "RingPart", position,
                new Vector3(radius * 0.38f, 0.16f, 1f), color, angle * Mathf.Rad2Deg + 90f, 132);
        }
        visual.renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return root;
    }

    public static GameObject SpawnExpandingRing(
        Vector3 position,
        float radius,
        Color color,
        float lifetime,
        string objectName,
        float startScaleRatio = 0.08f)
    {
        GameObject root = SpawnRing(null, radius, color, lifetime, objectName);
        root.transform.position = position;
        hys_FinalBossMagicVisual visual = root.GetComponent<hys_FinalBossMagicVisual>();
        visual.animateScale = true;
        visual.scaleStartTime = Time.time;
        visual.scaleEndTime = Time.time + Mathf.Max(0.05f, lifetime);
        visual.scaleFrom = Vector3.one * Mathf.Clamp(startScaleRatio, 0.01f, 1f);
        visual.scaleTo = Vector3.one;
        root.transform.localScale = visual.scaleFrom;
        return root;
    }

    public static void SetVelocity(GameObject visualObject, Vector2 moveVelocity)
    {
        if (visualObject != null && visualObject.TryGetComponent(out hys_FinalBossMagicVisual visual))
            visual.velocity = moveVelocity;
    }

    private static GameObject CreateRoot(string objectName, Vector3 position, float lifetime)
    {
        GameObject root = new GameObject(objectName);
        root.transform.position = position;
        hys_FinalBossMagicVisual visual = root.AddComponent<hys_FinalBossMagicVisual>();
        visual.endTime = lifetime < 0f ? float.PositiveInfinity : Time.time + Mathf.Max(0.05f, lifetime);
        return root;
    }

    private static void CreatePart(Transform parent, string objectName, Vector3 localPosition,
        Vector3 localScale, Color color, float rotationZ, int sortingOrder)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "hys_FinalBossWhiteTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    private void Update()
    {
        if (followTarget != null) transform.position = followTarget.position;
        transform.position += velocity * Time.deltaTime;

        if (animateScale)
        {
            float t = Mathf.InverseLerp(scaleStartTime, scaleEndTime, Time.time);
            transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, Mathf.SmoothStep(0f, 1f, t));
        }

        if (!float.IsPositiveInfinity(endTime) && Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        if (renderers == null || float.IsPositiveInfinity(endTime)) return;
        float remaining = endTime - Time.time;
        if (remaining > 0.35f) return;
        float alpha = Mathf.Clamp01(remaining / 0.35f);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color color = renderers[i].color;
            color.a = Mathf.Min(color.a, alpha);
            renderers[i].color = color;
        }
    }
}
