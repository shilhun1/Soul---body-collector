using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// 화면을 가리는 하단 안내 없이 보스 이름, 섬광과 왕실 인장만 간결하게 표시합니다.
[DisallowMultipleComponent]
public class hys_SecondBossCinematicOverlay : MonoBehaviour
{
    private const int SealSegments = 64;
    private const int LightningSegments = 18;
    private static TMP_FontAsset runtimeKoreanFont;
    private static Material lineMaterial;

    private GameObject overlayRoot;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI epithetText;
    private TextMeshProUGUI hintText;
    private Image skipFill;
    private Image flashImage;
    private GameObject sealRoot;
    private Transform sealTarget;
    private LineRenderer[] sealRings;
    private LineRenderer[] sealTicks;
    private LineRenderer[] sealLightning;
    private Coroutine flashRoutine;

    public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;
    public bool HasRoyalSeal => sealRoot != null;
    public int RoyalSealRingCount => sealRings != null ? sealRings.Length : 0;
    public int RoyalSealLightningCount => sealLightning != null ? sealLightning.Length : 0;
    public bool HasBottomControls => hintText != null || skipFill != null;
    public int TitleShowCount { get; private set; }
    public int ReleaseFlashCount { get; private set; }

    public void BeginIntro(Transform player, Color accentColor)
    {
        BeginDialogueControls("보스 등장", accentColor);
        // 사용자가 선호한 전기처럼 회전하는 왕실 처형 인장을 플레이어 주위에 다시 표시합니다.
        BuildRoyalSeal(player, accentColor);
    }

    public void BeginDialogueControls(string contextLabel, Color accentColor)
    {
        EnsureOverlay();
        CleanupRoyalSeal();
        overlayRoot.SetActive(true);
        canvasGroup.alpha = 1f;
        titleText.gameObject.SetActive(false);
        epithetText.gameObject.SetActive(false);
        // 조작 기능은 유지하되 화면 아래 진행바와 키 안내는 표시하지 않습니다.
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (skipFill != null) skipFill.color = accentColor;
        SetSkipProgress(0f);
    }

    public void ShowBossTitle(string bossName, string epithet)
    {
        EnsureOverlay();
        TitleShowCount++;
        titleText.text = bossName;
        epithetText.text = epithet;
        titleText.gameObject.SetActive(true);
        epithetText.gameObject.SetActive(true);
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    public void SetSkipProgress(float progress)
    {
        if (skipFill != null) skipFill.fillAmount = Mathf.Clamp01(progress);
    }

    public void PlayReleaseFlash(float duration)
    {
        EnsureOverlay();
        overlayRoot.SetActive(true);
        canvasGroup.alpha = 1f;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(ReleaseFlashRoutine(Mathf.Max(0.08f, duration)));
    }

    public void EndIntro()
    {
        CleanupRoyalSeal();
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void Update()
    {
        if (sealRoot == null) return;
        if (sealTarget != null) sealRoot.transform.position = ResolveSealPosition(sealTarget);
        if (sealRings == null) return;

        float time = Time.unscaledTime;
        for (int i = 0; i < sealRings.Length; i++)
        {
            if (sealRings[i] == null) continue;
            float direction = i % 2 == 0 ? 1f : -1f;
            float pulse = 1f + Mathf.Sin(time * (4.2f + i * 0.35f) + i) * 0.045f;
            sealRings[i].transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                time * (24f + i * 8f) * direction);
            sealRings[i].transform.localScale = new Vector3(pulse, pulse, 1f);
            sealRings[i].widthMultiplier = (0.022f + i * 0.005f)
                * (1f + Mathf.Sin(time * 7f + i * 1.4f) * 0.25f);
        }

        if (sealTicks != null)
        {
            for (int i = 0; i < sealTicks.Length; i++)
            {
                if (sealTicks[i] == null) continue;
                float direction = i % 2 == 0 ? 1f : -1f;
                sealTicks[i].transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    time * (42f + i * 1.5f) * direction);
                sealTicks[i].widthMultiplier = 0.026f
                    * (1f + Mathf.Sin(time * 11f + i) * 0.35f);
            }
        }

        if (sealLightning != null)
        {
            for (int i = 0; i < sealLightning.Length; i++)
                UpdateLightningArc(sealLightning[i], i, time);
        }
    }

    private void OnDestroy()
    {
        CleanupRoyalSeal();
        if (overlayRoot != null) Destroy(overlayRoot);
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null) return;

        overlayRoot = new GameObject(
            "hys_MidBoss2_CinematicOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CanvasGroup));
        overlayRoot.hideFlags = HideFlags.DontSave;

        Canvas canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 620;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        TMP_FontAsset font = GetKoreanFont();
        titleText = CreateText("hys_CinematicBossTitle", font, 42f, new Vector2(0f, 35f));
        titleText.color = new Color(1f, 0.72f, 0.25f, 1f);
        titleText.fontStyle = FontStyles.Bold;
        epithetText = CreateText("hys_CinematicBossEpithet", font, 21f, new Vector2(0f, -18f));
        epithetText.color = new Color(1f, 0.9f, 0.7f, 1f);

        flashImage = CreateImage(
            "hys_CinematicReleaseFlash",
            Color.clear,
            Vector2.zero,
            Vector2.zero);
        RectTransform flashRect = flashImage.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashRect.SetAsLastSibling();
        overlayRoot.SetActive(false);
    }

    private void CreateLetterbox(string objectName, bool top)
    {
        Image bar = CreateImage(objectName, new Color(0f, 0f, 0f, 0.94f), new Vector2(0f, 72f), Vector2.zero);
        RectTransform rect = bar.rectTransform;
        rect.anchorMin = top ? new Vector2(0f, 1f) : Vector2.zero;
        rect.anchorMax = top ? Vector2.one : new Vector2(1f, 0f);
        rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 72f);
    }

    private TextMeshProUGUI CreateText(string objectName, TMP_FontAsset font, float fontSize, Vector2 position)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        target.transform.SetParent(overlayRoot.transform, false);
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 58f);
        rect.anchoredPosition = position;
        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth = 0.12f;
        text.outlineColor = new Color32(0, 0, 0, 240);
        return text;
    }

    private Image CreateImage(string objectName, Color color, Vector2 size, Vector2 position)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        target.transform.SetParent(overlayRoot.transform, false);
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = target.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void BuildRoyalSeal(Transform player, Color color)
    {
        CleanupRoyalSeal();
        sealTarget = player;
        sealRoot = new GameObject("hys_RoyalExecutionSeal");
        sealRoot.transform.position = player != null ? ResolveSealPosition(player) : transform.position;

        // 서로 반대로 도는 다섯 겹의 얇은 인장으로 깊이감과 속도감을 만듭니다.
        sealRings = new LineRenderer[5];
        for (int i = 0; i < sealRings.Length; i++)
        {
            GameObject ringObject = new GameObject("RoyalSealRing_" + i);
            ringObject.transform.SetParent(sealRoot.transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = SealSegments;
            ring.material = GetLineMaterial();
            Color ringColor = i % 2 == 0 ? color : Color.Lerp(color, Color.white, 0.48f);
            ring.startColor = ring.endColor = ringColor;
            ring.widthMultiplier = 0.022f + i * 0.005f;
            ring.sortingOrder = 245 + i;
            float radius = 0.7f + i * 0.11f;
            for (int point = 0; point < SealSegments; point++)
            {
                float angle = point / (float)SealSegments * Mathf.PI * 2f;
                ring.SetPosition(point, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.24f, 0f));
            }
            sealRings[i] = ring;
        }

        // 긴 룬과 짧은 룬을 교차시켜 왕실 처형 인장처럼 보이게 합니다.
        sealTicks = new LineRenderer[12];
        for (int i = 0; i < sealTicks.Length; i++)
        {
            float angle = i / (float)sealTicks.Length * Mathf.PI * 2f;
            GameObject tickObject = new GameObject("RoyalSealTick_" + i);
            tickObject.transform.SetParent(sealRoot.transform, false);
            LineRenderer tick = tickObject.AddComponent<LineRenderer>();
            tick.useWorldSpace = false;
            tick.positionCount = 2;
            tick.material = GetLineMaterial();
            Color tickColor = i % 3 == 0 ? Color.white : color;
            tick.startColor = tick.endColor = tickColor;
            tick.widthMultiplier = 0.026f;
            tick.sortingOrder = 252;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.24f, 0f);
            float inner = i % 2 == 0 ? 0.48f : 0.66f;
            float outer = i % 3 == 0 ? 1.32f : 1.16f;
            tick.SetPosition(0, direction * inner);
            tick.SetPosition(1, direction * outer);
            sealTicks[i] = tick;
        }

        // 인장 표면을 타고 흐르는 불규칙한 전기 아크를 추가합니다.
        sealLightning = new LineRenderer[5];
        for (int i = 0; i < sealLightning.Length; i++)
        {
            GameObject arcObject = new GameObject("RoyalSealLightning_" + i);
            arcObject.transform.SetParent(sealRoot.transform, false);
            LineRenderer arc = arcObject.AddComponent<LineRenderer>();
            arc.useWorldSpace = false;
            arc.loop = false;
            arc.positionCount = LightningSegments;
            arc.material = GetLineMaterial();
            arc.startColor = Color.Lerp(color, Color.white, 0.75f);
            arc.endColor = new Color(color.r, color.g * 0.72f, color.b * 0.5f, 0.18f);
            arc.widthMultiplier = 0.018f + i * 0.003f;
            arc.sortingOrder = 256 + i;
            sealLightning[i] = arc;
            UpdateLightningArc(arc, i, Time.unscaledTime);
        }
    }

    private static void UpdateLightningArc(LineRenderer arc, int index, float time)
    {
        if (arc == null) return;
        float direction = index % 2 == 0 ? 1f : -1f;
        float startAngle = time * (2.2f + index * 0.18f) * direction
            + index * Mathf.PI * 0.43f;
        float span = 0.9f + index * 0.11f;
        float pulse = 0.9f + Mathf.Sin(time * 8.5f + index) * 0.08f;

        for (int point = 0; point < LightningSegments; point++)
        {
            float ratio = point / (float)(LightningSegments - 1);
            float angle = startAngle + span * ratio * direction;
            float jagged = Mathf.Sin(time * 31f + point * 9.7f + index * 4.1f) * 0.055f;
            float radius = pulse + jagged;
            arc.SetPosition(
                point,
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 0.27f,
                    0f));
        }
    }

    private static Vector3 ResolveSealPosition(Transform player)
    {
        Collider2D playerCollider = player != null ? player.GetComponentInParent<Collider2D>() : null;
        if (playerCollider != null)
            return new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.12f, 0f);
        return player != null ? player.position + Vector3.down * 0.65f : Vector3.zero;
    }

    private void CleanupRoyalSeal()
    {
        if (sealRoot != null) Destroy(sealRoot);
        sealRoot = null;
        sealTarget = null;
        sealRings = null;
        sealTicks = null;
        sealLightning = null;
    }

    private IEnumerator ReleaseFlashRoutine(float duration)
    {
        ReleaseFlashCount++;
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime < startTime + duration)
        {
            float progress = Mathf.InverseLerp(startTime, startTime + duration, Time.unscaledTime);
            flashImage.color = new Color(1f, 0.76f, 0.32f, Mathf.Sin(progress * Mathf.PI) * 0.78f);
            yield return null;
        }
        flashImage.color = Color.clear;
        flashRoutine = null;
    }

    private static TMP_FontAsset GetKoreanFont()
    {
        if (runtimeKoreanFont != null) return runtimeKoreanFont;
        Font systemFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 90);
        if (systemFont != null)
        {
            runtimeKoreanFont = TMP_FontAsset.CreateFontAsset(
                systemFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (runtimeKoreanFont != null)
            {
                runtimeKoreanFont.name = "hys_MidBoss2_CinematicKoreanSDF";
                runtimeKoreanFont.hideFlags = HideFlags.DontSave;
            }
        }
        return runtimeKoreanFont != null ? runtimeKoreanFont : TMP_Settings.defaultFontAsset;
    }

    private static Material GetLineMaterial()
    {
        if (lineMaterial != null) return lineMaterial;
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.DontSave;
        }
        return lineMaterial;
    }
}
