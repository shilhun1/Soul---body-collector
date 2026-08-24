using UnityEngine;

/// <summary>
/// hys 몬스터와 보스가 공격하기 전에 범위를 보여주는 경고선입니다.
/// HWJ 어셈블리의 내부 전용 표시기를 직접 참조하지 않도록 hys 전용으로 분리했습니다.
/// </summary>
public class hys_SkillWarningIndicator : MonoBehaviour
{
    private const int CircleSegmentCount = 48;
    private const string WarningObjectName = "hys_SkillWarningIndicator";

    private static Material sharedLineMaterial;

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float durationSeconds = 1f;
    [SerializeField] private Color warningColor = new Color(1f, 0.15f, 0.05f, 0.85f);
    [SerializeField] private float lineWidth = 0.06f;

    private float startTime;
    private float endTime;

    /// <summary>지정한 위치에 원형 공격 예고선을 표시합니다.</summary>
    public static hys_SkillWarningIndicator ShowCircle(
        Vector3 center,
        float radius,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        hys_SkillWarningIndicator indicator = Create(center);
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawCircle(radius);
        return indicator;
    }

    /// <summary>지정한 방향으로 진행하는 화살표 모양의 공격 예고선을 표시합니다.</summary>
    public static hys_SkillWarningIndicator ShowArrowPath(
        Vector3 start,
        float direction,
        float length,
        float width,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        hys_SkillWarningIndicator indicator = Create(start);
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawArrowPath(direction, length, width);
        return indicator;
    }

    /// <summary>지정한 크기의 사각형 공격 예고선을 표시합니다.</summary>
    public static hys_SkillWarningIndicator ShowRectangle(
        Vector3 center,
        Vector2 size,
        float durationSeconds,
        Color warningColor,
        float lineWidth)
    {
        hys_SkillWarningIndicator indicator = Create(center);
        indicator.Initialize(durationSeconds, warningColor, lineWidth);
        indicator.DrawRectangle(size);
        return indicator;
    }

    private static hys_SkillWarningIndicator Create(Vector3 position)
    {
        GameObject indicatorObject = new GameObject(WarningObjectName);
        indicatorObject.transform.position = position;
        return indicatorObject.AddComponent<hys_SkillWarningIndicator>();
    }

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void Update()
    {
        UpdateWarningPulse();
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize(float duration, Color color, float width)
    {
        durationSeconds = Mathf.Max(0.01f, duration);
        warningColor = color;
        lineWidth = Mathf.Max(0.01f, width);
        startTime = Time.time;
        endTime = Time.time + durationSeconds;
        EnsureLineRenderer();
    }

    private void UpdateWarningPulse()
    {
        if (lineRenderer == null) return;
        float progress = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(0.01f, durationSeconds));
        float pulse = 0.65f + Mathf.Sin(Time.time * 22f) * 0.15f;
        Color visibleColor = warningColor;
        // 타격 시점이 가까워질수록 경고선이 진하고 굵어져 공격 범위를 쉽게 읽을 수 있습니다.
        visibleColor.a *= Mathf.Lerp(pulse, 1f, progress);
        lineRenderer.startColor = visibleColor;
        lineRenderer.endColor = visibleColor;
        lineRenderer.widthMultiplier = lineWidth * Mathf.Lerp(0.85f, 1.45f, progress);
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.startColor = warningColor;
        lineRenderer.endColor = warningColor;
        lineRenderer.material = GetSharedLineMaterial();
        lineRenderer.sortingOrder = 100;
    }

    private void DrawCircle(float radius)
    {
        float safeRadius = Mathf.Max(0.1f, radius);
        lineRenderer.loop = true;
        lineRenderer.positionCount = CircleSegmentCount;

        for (int i = 0; i < CircleSegmentCount; i++)
        {
            float angle = i / (float)CircleSegmentCount * Mathf.PI * 2f;
            lineRenderer.SetPosition(
                i,
                new Vector3(Mathf.Cos(angle) * safeRadius, Mathf.Sin(angle) * safeRadius, 0f));
        }
    }

    private void DrawArrowPath(float direction, float length, float width)
    {
        float safeDirection = direction < 0f ? -1f : 1f;
        float safeLength = Mathf.Max(0.1f, length);
        float halfWidth = Mathf.Max(0.1f, width * 0.5f);
        float arrowLength = Mathf.Clamp(safeLength * 0.25f, 0.25f, 0.75f);
        Vector3 forward = Vector3.right * safeDirection;
        Vector3 side = Vector3.up;

        lineRenderer.loop = true;
        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, side * halfWidth);
        lineRenderer.SetPosition(1, forward * safeLength + side * halfWidth);
        lineRenderer.SetPosition(2, forward * (safeLength + arrowLength));
        lineRenderer.SetPosition(3, forward * safeLength - side * halfWidth);
        lineRenderer.SetPosition(4, -side * halfWidth);
    }

    private void DrawRectangle(Vector2 size)
    {
        float halfWidth = Mathf.Max(0.1f, size.x * 0.5f);
        float halfHeight = Mathf.Max(0.1f, size.y * 0.5f);

        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(-halfWidth, -halfHeight, 0f));
        lineRenderer.SetPosition(1, new Vector3(-halfWidth, halfHeight, 0f));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, halfHeight, 0f));
        lineRenderer.SetPosition(3, new Vector3(halfWidth, -halfHeight, 0f));
    }

    private static Material GetSharedLineMaterial()
    {
        if (sharedLineMaterial != null)
        {
            return sharedLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Hidden/Internal-Colored");

        if (shader != null)
        {
            sharedLineMaterial = new Material(shader);
        }

        return sharedLineMaterial;
    }
}
