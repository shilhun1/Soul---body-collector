using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// 픽셀 퍼펙트 카메라의 저해상도 렌더를 우회하도록 보스 글씨를 화면 공간 TMP UI로 표시합니다.
[DefaultExecutionOrder(250)]
[DisallowMultipleComponent]
public class hys_SecondBossDialogueStyler : MonoBehaviour
{
    private const string BubbleRootName = "HWJ_BossDialogueBubble";
    private const string BackgroundName = "BubbleBackground";
    private const string TextName = "BubbleText";

    [Header("TMP 폰트")]
    [SerializeField] private Font dialogueFont;
    [SerializeField] private TMP_FontAsset accentFontAsset;
    [SerializeField, Range(24f, 64f)] private float dialogueTmpFontSize = 46f;
    [SerializeField, Range(20f, 56f)] private float titleTmpFontSize = 32f;
    [SerializeField, Range(20f, 56f)] private float bossNameTmpFontSize = 34f;
    [SerializeField] private Color dialogueColor = new Color(1f, 0.93f, 0.76f, 1f);

    [Header("기사단장 대사창")]
    [SerializeField] private string title = "왕실 기사단장  |  바르칸";
    [SerializeField] private Color panelColor = new Color(0.035f, 0.045f, 0.075f, 0.96f);
    [SerializeField] private Color borderColor = new Color(0.78f, 0.5f, 0.16f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.72f, 0.28f, 1f);
    [SerializeField, Min(4f)] private float minimumPanelWidth = 7.6f;
    [SerializeField, Min(1.2f)] private float minimumPanelHeight = 1.95f;

    private static TMP_FontAsset runtimeDialogueFontAsset;

    private Transform bubbleRoot;
    private SpriteRenderer legacyBackground;
    private TextMesh sourceDialogueText;
    private TextMesh sourceBossNameText;

    private GameObject overlayRoot;
    private Canvas overlayCanvas;
    private RectTransform panelRect;
    private Image panelImage;
    private Image accentLine;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI dialogueText;
    private TextMeshProUGUI bossNameText;
    private RectTransform bossNameRect;
    private int styleApplyCount;

    public bool HasStyledBubble => panelRect != null
        && titleText != null
        && dialogueText != null
        && sourceDialogueText != null;
    public int StyleApplyCount => styleApplyCount;
    public float DialogueCharacterSize => dialogueTmpFontSize;
    public Color PanelColor => panelImage != null ? panelImage.color : Color.clear;
    public bool IsDialogueInsidePanel => IsTextInsidePanel();

    private void OnEnable()
    {
        if (overlayRoot != null) overlayRoot.SetActive(true);
    }

    private void LateUpdate()
    {
        CacheBossNameSource();
        CacheBubbleSource();
        EnsureOverlay();
        UpdateDialogueOverlay();
        UpdateBossNameOverlay();
    }

    private void OnDisable()
    {
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlayRoot != null) Destroy(overlayRoot);
    }

    private void CacheBubbleSource()
    {
        Transform foundBubble = transform.Find(BubbleRootName);
        if (foundBubble == null) return;
        if (bubbleRoot != foundBubble)
        {
            bubbleRoot = foundBubble;
            styleApplyCount++;
        }

        Transform backgroundTransform = bubbleRoot.Find(BackgroundName);
        Transform textTransform = bubbleRoot.Find(TextName);
        legacyBackground = backgroundTransform != null
            ? backgroundTransform.GetComponent<SpriteRenderer>()
            : null;
        sourceDialogueText = textTransform != null ? textTransform.GetComponent<TextMesh>() : null;

        // HWJ 오브젝트는 문자열 갱신용으로 유지하고 저해상도 월드 렌더링만 끕니다.
        if (legacyBackground != null) legacyBackground.enabled = false;
        if (sourceDialogueText != null)
        {
            MeshRenderer renderer = sourceDialogueText.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

        DisableLegacyDecoration("hys_DialogueGoldBorder");
        DisableLegacyDecoration("hys_DialogueHeaderAccent");
        DisableLegacyDecoration("hys_DialogueBossTitle");
    }

    private void CacheBossNameSource()
    {
        if (sourceBossNameText != null) return;
        TextMesh[] textMeshes = GetComponentsInChildren<TextMesh>(true);
        for (int i = 0; i < textMeshes.Length; i++)
        {
            if (textMeshes[i] == null || textMeshes[i].name != "BossName") continue;
            sourceBossNameText = textMeshes[i];
            break;
        }
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null) return;

        overlayRoot = new GameObject(
            "hys_MidBoss2_ScreenSpaceDialogue_" + GetInstanceID(),
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        overlayRoot.hideFlags = HideFlags.DontSave;

        overlayCanvas = overlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 500;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        // 화면 해상도가 커져도 글자가 과도하게 확대되지 않도록 1280x720 기준으로 맞춥니다.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateDialoguePanel();
        CreateBossNameText();
    }

    private void CreateDialoguePanel()
    {
        GameObject panel = new GameObject(
            "hys_DialoguePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        panel.transform.SetParent(overlayRoot.transform, false);
        panelRect = panel.GetComponent<RectTransform>();
        panelImage = panel.GetComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        accentLine = CreateImage(panelRect, "hys_DialogueHeaderLine", borderColor);
        RectTransform accentRect = accentLine.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = new Vector2(0f, -35f);
        accentRect.sizeDelta = new Vector2(-20f, 2f);

        titleText = CreateText(panelRect, "hys_DialogueTitle", accentFontAsset);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -5f);
        titleRect.sizeDelta = new Vector2(-24f, 28f);
        titleText.alignment = TextAlignmentOptions.Left;

        dialogueText = CreateText(panelRect, "hys_DialogueBody", GetRuntimeDialogueFont());
        RectTransform bodyRect = dialogueText.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(18f, 10f);
        bodyRect.offsetMax = new Vector2(-18f, -42f);
        dialogueText.alignment = TextAlignmentOptions.Center;
        dialogueText.enableWordWrapping = true;
        dialogueText.outlineWidth = 0.06f;
        dialogueText.outlineColor = new Color32(0, 0, 0, 220);
    }

    private void CreateBossNameText()
    {
        GameObject target = new GameObject(
            "hys_BossNameOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        target.transform.SetParent(overlayRoot.transform, false);
        bossNameRect = target.GetComponent<RectTransform>();
        bossNameText = target.GetComponent<TextMeshProUGUI>();
        bossNameText.font = accentFontAsset;
        bossNameText.alignment = TextAlignmentOptions.Center;
        bossNameText.raycastTarget = false;
        bossNameText.enableWordWrapping = false;
        bossNameText.overflowMode = TextOverflowModes.Overflow;
        bossNameText.outlineWidth = 0.16f;
        bossNameText.outlineColor = new Color32(0, 0, 0, 255);
    }

    private void UpdateDialogueOverlay()
    {
        if (panelRect == null) return;
        bool visible = bubbleRoot != null
            && bubbleRoot.gameObject.activeInHierarchy
            && sourceDialogueText != null
            && !string.IsNullOrEmpty(sourceDialogueText.text);
        panelRect.gameObject.SetActive(visible);
        if (!visible) return;

        float panelWidth = Mathf.Clamp(minimumPanelWidth * 72f, 460f, 580f);
        float panelHeight = Mathf.Clamp(minimumPanelHeight * 62f, 105f, 130f);
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
        panelRect.position = ClampToScreen(WorldToScreen(bubbleRoot.position), panelRect.rect.width, panelRect.rect.height);

        titleText.text = title;
        titleText.fontSize = Mathf.Clamp(titleTmpFontSize * 0.62f, 16f, 21f);
        titleText.color = titleColor;

        dialogueText.text = sourceDialogueText.text;
        dialogueText.fontSize = Mathf.Clamp(dialogueTmpFontSize * 0.65f, 24f, 31f);
        dialogueText.color = dialogueColor;
    }

    private void UpdateBossNameOverlay()
    {
        if (bossNameText == null) return;
        bool visible = sourceBossNameText != null && sourceBossNameText.gameObject.activeInHierarchy;
        bossNameText.gameObject.SetActive(visible);
        if (!visible) return;

        MeshRenderer sourceRenderer = sourceBossNameText.GetComponent<MeshRenderer>();
        if (sourceRenderer != null) sourceRenderer.enabled = false;
        bossNameText.text = sourceBossNameText.text;
        bossNameText.color = sourceBossNameText.color;
        bossNameText.fontSize = Mathf.Clamp(bossNameTmpFontSize * 0.65f, 18f, 24f);
        bossNameRect.sizeDelta = new Vector2(460f, 36f);
        bossNameRect.position = WorldToScreen(sourceBossNameText.transform.position);
    }

    private Vector3 WorldToScreen(Vector3 worldPosition)
    {
        Camera targetCamera = Camera.main;
        return targetCamera != null
            ? targetCamera.WorldToScreenPoint(worldPosition)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.75f, 0f);
    }

    private static Vector3 ClampToScreen(Vector3 position, float width, float height)
    {
        float margin = 8f;
        position.x = Mathf.Clamp(position.x, width * 0.5f + margin, Screen.width - width * 0.5f - margin);
        position.y = Mathf.Clamp(position.y, height * 0.5f + margin, Screen.height - height * 0.5f - margin);
        position.z = 0f;
        return position;
    }

    private TMP_FontAsset GetRuntimeDialogueFont()
    {
        if (runtimeDialogueFontAsset != null) return runtimeDialogueFontAsset;
        if (dialogueFont == null) return accentFontAsset;

        // Light 원본에서 2048 SDF를 생성해 화면 해상도에서 직접 선명하게 렌더링합니다.
        runtimeDialogueFontAsset = TMP_FontAsset.CreateFontAsset(
            dialogueFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);
        if (runtimeDialogueFontAsset != null)
        {
            runtimeDialogueFontAsset.name = "hys_MidBoss2_ScreenLightSDF";
            runtimeDialogueFontAsset.hideFlags = HideFlags.DontSave;
        }

        return runtimeDialogueFontAsset != null ? runtimeDialogueFontAsset : accentFontAsset;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, TMP_FontAsset font)
    {
        GameObject target = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        target.transform.SetParent(parent, false);
        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        return text;
    }

    private static Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject target = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        target.transform.SetParent(parent, false);
        Image image = target.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void DisableLegacyDecoration(string objectName)
    {
        if (bubbleRoot == null) return;
        Transform child = bubbleRoot.Find(objectName);
        if (child != null) child.gameObject.SetActive(false);
    }

    private bool IsTextInsidePanel()
    {
        if (panelRect == null || dialogueText == null || string.IsNullOrEmpty(dialogueText.text)) return true;
        return dialogueText.preferredWidth <= panelRect.rect.width - 36f
            && dialogueText.preferredHeight <= panelRect.rect.height - 52f;
    }
}
