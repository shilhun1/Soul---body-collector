using System.Collections;
using UnityEngine;

// 중간보스2의 2페이즈 마력 의식, 룬 궤도, 빛기둥과 최종 섬광을 전담합니다.
[DisallowMultipleComponent]
public class hys_SecondBossPhaseTransitionVisual : MonoBehaviour
{
    private const int RingSegments = 64;
    private const int RuneCount = 14;
    private const int RingCount = 3;

    private static Sprite whiteSprite;
    private static Material lineMaterial;

    [Header("의식 연출")]
    [SerializeField, Min(1f)] private float ritualRadius = 3.8f;
    [SerializeField] private Color shadowColor = new Color(0.12f, 0.015f, 0.22f, 0.78f);
    [SerializeField] private Color runeColor = new Color(0.78f, 0.24f, 1f, 0.95f);
    [SerializeField] private Color flashColor = new Color(1f, 0.82f, 1f, 0.88f);

    [Header("실행 확인")]
    [SerializeField] private bool isPlaying;
    [SerializeField] private int playCount;
    [SerializeField] private int lastStageCount;

    private GameObject visualRoot;
    private LineRenderer[] rings;
    private SpriteRenderer[] runes;
    private SpriteRenderer[] pillars;
    private SpriteRenderer flash;
    private Coroutine playRoutine;

    public bool IsPlaying => isPlaying;
    public int PlayCount => playCount;
    public int LastStageCount => lastStageCount;

    public void Play(float durationSeconds, Color magicColor)
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        CleanupVisuals();
        BuildVisuals(magicColor);
        playRoutine = StartCoroutine(PlayRoutine(Mathf.Max(1f, durationSeconds)));
    }

    private IEnumerator PlayRoutine(float duration)
    {
        isPlaying = true;
        playCount++;
        lastStageCount = 0;
        float startTime = Time.time;
        float endTime = startTime + duration;

        while (Time.time < endTime && visualRoot != null)
        {
            float progress = Mathf.InverseLerp(startTime, endTime, Time.time);
            UpdateStageCount(progress);
            AnimateRitual(progress);
            yield return null;
        }

        lastStageCount = Mathf.Max(lastStageCount, 5);
        isPlaying = false;
        playRoutine = null;
        CleanupVisuals();
    }

    private void UpdateStageCount(float progress)
    {
        if (progress >= 0.08f) lastStageCount = Mathf.Max(lastStageCount, 1);
        if (progress >= 0.28f) lastStageCount = Mathf.Max(lastStageCount, 2);
        if (progress >= 0.5f) lastStageCount = Mathf.Max(lastStageCount, 3);
        if (progress >= 0.74f) lastStageCount = Mathf.Max(lastStageCount, 4);
        if (progress >= 0.9f) lastStageCount = Mathf.Max(lastStageCount, 5);
    }

    private void AnimateRitual(float progress)
    {
        float collapse = progress < 0.72f
            ? Mathf.Lerp(ritualRadius, ritualRadius * 0.58f, progress / 0.72f)
            : Mathf.Lerp(ritualRadius * 0.58f, 0.35f, (progress - 0.72f) / 0.28f);
        float pulse = 1f + Mathf.Sin(Time.time * 11f) * 0.07f;

        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] == null) continue;
            float ringScale = collapse * (0.62f + i * 0.19f) * pulse;
            rings[i].transform.localScale = Vector3.one * ringScale;
            Color color = Color.Lerp(shadowColor, runeColor, i / (float)(rings.Length - 1));
            color.a *= Mathf.SmoothStep(0f, 1f, Mathf.Min(progress * 5f, (1f - progress) * 8f));
            rings[i].startColor = color;
            rings[i].endColor = color;
            rings[i].widthMultiplier = 0.035f + i * 0.018f;
        }

        float orbit = Time.time * 0.9f;
        for (int i = 0; i < runes.Length; i++)
        {
            if (runes[i] == null) continue;
            float angle = i / (float)runes.Length * Mathf.PI * 2f + orbit * (i % 2 == 0 ? 1f : -0.72f);
            float stagger = 0.9f + Mathf.Sin(Time.time * 7f + i * 0.8f) * 0.1f;
            float radius = collapse * stagger;
            runes[i].transform.localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius * 0.62f + 0.45f,
                0f);
            runes[i].transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            runes[i].transform.localScale = new Vector3(0.11f, 0.48f, 1f)
                * Mathf.Lerp(0.2f, 1.25f, Mathf.SmoothStep(0f, 1f, progress * 4f));
            Color color = runeColor;
            color.a *= Mathf.Clamp01((1f - progress) * 5f);
            runes[i].color = color;
        }

        for (int i = 0; i < pillars.Length; i++)
        {
            if (pillars[i] == null) continue;
            float pillarProgress = Mathf.Clamp01((progress - 0.32f - i * 0.055f) * 5f);
            float height = Mathf.Lerp(0.05f, 6.8f - i * 0.7f, pillarProgress);
            pillars[i].transform.localPosition = new Vector3((i - 1) * 1.25f, height * 0.5f - 0.6f, 0f);
            pillars[i].transform.localScale = new Vector3(0.34f + i * 0.08f, height, 1f);
            Color color = Color.Lerp(shadowColor, runeColor, 0.45f);
            color.a *= Mathf.Sin(pillarProgress * Mathf.PI) * 0.72f;
            pillars[i].color = color;
        }

        float flashProgress = Mathf.Clamp01((progress - 0.76f) / 0.18f);
        flash.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 5.4f, flashProgress);
        Color visibleFlash = flashColor;
        visibleFlash.a *= Mathf.Sin(flashProgress * Mathf.PI) * 0.72f;
        flash.color = visibleFlash;
    }

    private void BuildVisuals(Color magicColor)
    {
        runeColor = Color.Lerp(runeColor, magicColor, 0.45f);
        visualRoot = new GameObject("hys_Phase2AwakeningRitual");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = Vector3.up * 0.35f;

        rings = new LineRenderer[RingCount];
        for (int i = 0; i < rings.Length; i++) rings[i] = CreateRing(i);

        runes = new SpriteRenderer[RuneCount];
        for (int i = 0; i < runes.Length; i++)
            runes[i] = CreateSpritePart("Rune_" + i, 108 + i % 2);

        pillars = new SpriteRenderer[3];
        for (int i = 0; i < pillars.Length; i++)
            pillars[i] = CreateSpritePart("ShadowPillar_" + i, 106 + i);

        flash = CreateSpritePart("FinalAwakeningFlash", 114);
    }

    private LineRenderer CreateRing(int index)
    {
        GameObject ringObject = new GameObject("RitualRing_" + index);
        ringObject.transform.SetParent(visualRoot.transform, false);
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = RingSegments;
        line.material = GetLineMaterial();
        line.sortingOrder = 104 + index;
        for (int point = 0; point < RingSegments; point++)
        {
            float angle = point / (float)RingSegments * Mathf.PI * 2f;
            line.SetPosition(point, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.62f, 0f));
        }
        return line;
    }

    private SpriteRenderer CreateSpritePart(string objectName, int sortingOrder)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(visualRoot.transform, false);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void CleanupVisuals()
    {
        if (visualRoot != null) Destroy(visualRoot);
        visualRoot = null;
        rings = null;
        runes = null;
        pillars = null;
        flash = null;
    }

    private void OnDisable()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = null;
        isPlaying = false;
        CleanupVisuals();
    }

    private static Material GetLineMaterial()
    {
        if (lineMaterial != null) return lineMaterial;
        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Hidden/Internal-Colored");
        if (shader != null) lineMaterial = new Material(shader);
        return lineMaterial;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "hys_PhaseTransitionWhiteTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        whiteSprite.name = "hys_PhaseTransitionWhiteSprite";
        return whiteSprite;
    }
}
