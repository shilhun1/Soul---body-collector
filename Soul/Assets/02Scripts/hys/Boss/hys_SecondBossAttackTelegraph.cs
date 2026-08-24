using UnityEngine;

// 중간보스2 공격마다 다른 색·문양·카운트다운을 바닥에 표시합니다.
public enum hys_SecondBossTelegraphStyle
{
    PhysicalSlash,
    Dash,
    SummonCommand,
    DarkShockwave,
    MagicMark,
    SwordEruption
}

[DisallowMultipleComponent]
public class hys_SecondBossAttackTelegraph : MonoBehaviour
{
    private const int EllipseSegments = 48;
    private static Material sharedMaterial;

    [SerializeField] private hys_SecondBossTelegraphStyle style;
    [SerializeField] private float durationSeconds;
    [SerializeField] private bool hasCountdown;
    [SerializeField] private Color color;

    private LineRenderer[] lines;
    private TextMesh countdownText;
    private Transform followTarget;
    private Vector3 followOffset;
    private float startTime;
    private float endTime;

    public static int ActiveCount { get; private set; }
    public static int CreatedCount { get; private set; }
    public static hys_SecondBossTelegraphStyle LastCreatedStyle { get; private set; }
    public hys_SecondBossTelegraphStyle Style => style;
    public bool HasCountdown => hasCountdown;

    public hys_SecondBossAttackTelegraph Follow(Transform target, Vector3 offset)
    {
        followTarget = target;
        followOffset = offset;
        return this;
    }

    public static hys_SecondBossAttackTelegraph ShowArea(
        Vector3 center,
        Vector2 size,
        float duration,
        Color warningColor,
        hys_SecondBossTelegraphStyle warningStyle,
        bool showCountdown,
        float lineWidth)
    {
        Vector2 safeSize = new Vector2(Mathf.Max(0.2f, size.x), Mathf.Max(0.2f, size.y));
        hys_SkillWarningIndicator.ShowRectangle(
            center,
            safeSize,
            Mathf.Max(0.05f, duration),
            warningColor,
            Mathf.Max(0.02f, lineWidth));

        Vector3 floorCenter = center - Vector3.up * (safeSize.y * 0.5f - 0.12f);
        return CreateFloorGlyph(
            floorCenter,
            safeSize.x * 0.5f,
            duration,
            warningColor,
            warningStyle,
            showCountdown,
            lineWidth);
    }

    public static hys_SecondBossAttackTelegraph ShowCircle(
        Vector3 center,
        float radius,
        float duration,
        Color warningColor,
        hys_SecondBossTelegraphStyle warningStyle,
        bool showCountdown,
        float lineWidth)
    {
        float safeRadius = Mathf.Max(0.2f, radius);
        hys_SkillWarningIndicator.ShowCircle(
            center,
            safeRadius,
            Mathf.Max(0.05f, duration),
            warningColor,
            Mathf.Max(0.02f, lineWidth));
        return CreateFloorGlyph(
            center,
            safeRadius,
            duration,
            warningColor,
            warningStyle,
            showCountdown,
            lineWidth);
    }

    public static hys_SecondBossAttackTelegraph ShowDashPath(
        Vector3 start,
        Vector3 end,
        float width,
        float duration,
        Color warningColor,
        float lineWidth)
    {
        float direction = end.x >= start.x ? 1f : -1f;
        float length = Mathf.Max(0.2f, Mathf.Abs(end.x - start.x));
        hys_SkillWarningIndicator.ShowArrowPath(
            start,
            direction,
            length,
            Mathf.Max(0.3f, width),
            Mathf.Max(0.05f, duration),
            warningColor,
            Mathf.Max(0.02f, lineWidth));

        Vector3 center = new Vector3((start.x + end.x) * 0.5f, start.y, start.z);
        return CreateFloorGlyph(
            center,
            length * 0.5f,
            duration,
            warningColor,
            hys_SecondBossTelegraphStyle.Dash,
            false,
            lineWidth);
    }

    public static hys_SecondBossAttackTelegraph ShowCommandSeal(
        Vector3 center,
        float radius,
        float duration,
        Color warningColor,
        float lineWidth)
    {
        return CreateFloorGlyph(
            center,
            Mathf.Max(0.6f, radius),
            duration,
            warningColor,
            hys_SecondBossTelegraphStyle.SummonCommand,
            false,
            lineWidth);
    }

    private static hys_SecondBossAttackTelegraph CreateFloorGlyph(
        Vector3 center,
        float halfWidth,
        float duration,
        Color warningColor,
        hys_SecondBossTelegraphStyle warningStyle,
        bool showCountdown,
        float lineWidth)
    {
        GameObject root = new GameObject("hys_MidBoss2_" + warningStyle + "_Telegraph");
        root.transform.position = center;
        hys_SecondBossAttackTelegraph telegraph = root.AddComponent<hys_SecondBossAttackTelegraph>();
        telegraph.Initialize(
            Mathf.Max(0.35f, halfWidth),
            Mathf.Max(0.05f, duration),
            warningColor,
            warningStyle,
            showCountdown,
            Mathf.Max(0.02f, lineWidth));
        return telegraph;
    }

    private void Initialize(
        float halfWidth,
        float duration,
        Color warningColor,
        hys_SecondBossTelegraphStyle warningStyle,
        bool showCountdown,
        float lineWidth)
    {
        style = warningStyle;
        durationSeconds = duration;
        hasCountdown = showCountdown;
        color = warningColor;
        startTime = Time.time;
        endTime = startTime + durationSeconds;
        ActiveCount++;
        CreatedCount++;
        LastCreatedStyle = style;

        lines = new LineRenderer[2];
        lines[0] = CreateLine("OuterRune", lineWidth, true);
        DrawEllipse(lines[0], halfWidth, Mathf.Clamp(halfWidth * 0.12f, 0.12f, 0.38f));
        lines[1] = CreateLine("InnerRune", lineWidth * 0.82f, style != hys_SecondBossTelegraphStyle.SwordEruption);
        DrawStyleRune(lines[1], halfWidth);
        if (showCountdown) CreateCountdown(halfWidth);
    }

    private void Update()
    {
        if (followTarget != null) transform.position = followTarget.position + followOffset;
        float progress = Mathf.Clamp01((Time.time - startTime) / Mathf.Max(0.01f, durationSeconds));
        float pulse = Mathf.Lerp(0.58f, 1f, progress)
            * (0.88f + Mathf.Sin(Time.time * Mathf.Lerp(9f, 22f, progress)) * 0.12f);
        Color visible = color;
        visible.a *= pulse;

        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                lines[i].startColor = visible;
                lines[i].endColor = visible;
                lines[i].transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.03f, progress);
            }
        }

        if (countdownText != null)
        {
            float remaining = Mathf.Max(0f, endTime - Time.time);
            countdownText.text = Mathf.Max(1, Mathf.CeilToInt(remaining)).ToString();
            countdownText.color = new Color(visible.r, visible.g, visible.b, Mathf.Max(0.78f, visible.a));
        }

        if (Time.time >= endTime) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
    }

    private LineRenderer CreateLine(string objectName, float width, bool loop)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        LineRenderer line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = loop;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.material = GetSharedMaterial();
        line.sortingOrder = 252;
        return line;
    }

    private static void DrawEllipse(LineRenderer line, float radiusX, float radiusY)
    {
        line.positionCount = EllipseSegments;
        for (int i = 0; i < EllipseSegments; i++)
        {
            float angle = i / (float)EllipseSegments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radiusX,
                Mathf.Sin(angle) * radiusY,
                0f));
        }
    }

    private void DrawStyleRune(LineRenderer line, float halfWidth)
    {
        float y = Mathf.Clamp(halfWidth * 0.1f, 0.1f, 0.3f);
        switch (style)
        {
            case hys_SecondBossTelegraphStyle.PhysicalSlash:
                line.loop = false;
                SetPoints(line,
                    new Vector3(-halfWidth * 0.72f, -y, 0f),
                    new Vector3(halfWidth * 0.72f, y, 0f));
                break;
            case hys_SecondBossTelegraphStyle.Dash:
                SetPoints(line,
                    new Vector3(-halfWidth * 0.68f, -y, 0f),
                    new Vector3(halfWidth * 0.2f, -y, 0f),
                    new Vector3(halfWidth * 0.68f, 0f, 0f),
                    new Vector3(halfWidth * 0.2f, y, 0f),
                    new Vector3(-halfWidth * 0.68f, y, 0f));
                break;
            case hys_SecondBossTelegraphStyle.SummonCommand:
                SetPoints(line,
                    new Vector3(0f, y * 1.4f, 0f),
                    new Vector3(halfWidth * 0.62f, 0f, 0f),
                    new Vector3(0f, -y * 1.4f, 0f),
                    new Vector3(-halfWidth * 0.62f, 0f, 0f));
                break;
            case hys_SecondBossTelegraphStyle.DarkShockwave:
                DrawAlternatingStar(line, halfWidth * 0.72f, halfWidth * 0.34f, y, 6);
                break;
            case hys_SecondBossTelegraphStyle.MagicMark:
                DrawAlternatingStar(line, halfWidth * 0.76f, halfWidth * 0.3f, y, 8);
                break;
            case hys_SecondBossTelegraphStyle.SwordEruption:
                line.loop = false;
                SetPoints(line,
                    new Vector3(-halfWidth * 0.9f, 0f, 0f),
                    new Vector3(-halfWidth * 0.48f, y * 1.6f, 0f),
                    new Vector3(-halfWidth * 0.18f, 0f, 0f),
                    new Vector3(0f, y * 2.15f, 0f),
                    new Vector3(halfWidth * 0.18f, 0f, 0f),
                    new Vector3(halfWidth * 0.48f, y * 1.6f, 0f),
                    new Vector3(halfWidth * 0.9f, 0f, 0f));
                break;
        }
    }

    private static void DrawAlternatingStar(
        LineRenderer line,
        float outerRadius,
        float innerRadius,
        float verticalRadius,
        int points)
    {
        int vertexCount = Mathf.Max(3, points) * 2;
        line.positionCount = vertexCount;
        for (int i = 0; i < vertexCount; i++)
        {
            float angle = i / (float)vertexCount * Mathf.PI * 2f;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            line.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * verticalRadius,
                0f));
        }
    }

    private static void SetPoints(LineRenderer line, params Vector3[] points)
    {
        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    private void CreateCountdown(float halfWidth)
    {
        GameObject textObject = new GameObject("Countdown");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, Mathf.Clamp(halfWidth * 0.16f, 0.42f, 0.85f), 0f);
        countdownText = textObject.AddComponent<TextMesh>();
        countdownText.anchor = TextAnchor.MiddleCenter;
        countdownText.alignment = TextAlignment.Center;
        countdownText.fontSize = 72;
        countdownText.characterSize = 0.055f;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.color = color;
        countdownText.text = Mathf.Max(1, Mathf.CeilToInt(durationSeconds)).ToString();
        MeshRenderer renderer = countdownText.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = 254;
    }

    private static Material GetSharedMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;
        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Hidden/Internal-Colored");
        if (shader != null) sharedMaterial = new Material(shader);
        return sharedMaterial;
    }
}
