using UnityEngine;

// HWJ 공용 대사창의 구조는 유지하면서 중간보스2 전용 색상, 크기와 장식을 덧씌웁니다.
[DefaultExecutionOrder(250)]
[DisallowMultipleComponent]
public class hys_SecondBossDialogueStyler : MonoBehaviour
{
    private const string BubbleRootName = "HWJ_BossDialogueBubble";
    private const string BackgroundName = "BubbleBackground";
    private const string TextName = "BubbleText";
    private const string BorderName = "hys_DialogueGoldBorder";
    private const string AccentName = "hys_DialogueHeaderAccent";
    private const string TitleName = "hys_DialogueBossTitle";

    [Header("대사 글자")]
    [SerializeField, Range(24, 64)] private int dialogueFontSize = 48;
    [SerializeField, Range(0.04f, 0.15f)] private float dialogueCharacterSize = 0.052f;
    [SerializeField] private Color dialogueColor = new Color(1f, 0.93f, 0.76f, 1f);

    [Header("기사단장 대사창")]
    [SerializeField] private string title = "왕실 기사단장  |  바르칸";
    [SerializeField] private Color panelColor = new Color(0.035f, 0.045f, 0.075f, 0.94f);
    [SerializeField] private Color borderColor = new Color(0.78f, 0.5f, 0.16f, 1f);
    [SerializeField] private Color titleColor = new Color(1f, 0.72f, 0.28f, 1f);
    [SerializeField, Min(1.2f)] private float minimumPanelHeight = 1.65f;

    private Transform styledRoot;
    private SpriteRenderer background;
    private SpriteRenderer border;
    private SpriteRenderer accent;
    private TextMesh dialogueText;
    private TextMesh titleText;
    private int styleApplyCount;

    public bool HasStyledBubble => styledRoot != null
        && background != null
        && border != null
        && accent != null
        && dialogueText != null
        && titleText != null;
    public int StyleApplyCount => styleApplyCount;
    public float DialogueCharacterSize => dialogueText != null ? dialogueText.characterSize : 0f;
    public Color PanelColor => background != null ? background.color : Color.clear;
    public bool IsDialogueInsidePanel => IsTextInsidePanel();

    private void LateUpdate()
    {
        Transform bubble = transform.Find(BubbleRootName);
        if (bubble == null) return;

        if (styledRoot != bubble || !HasStyledBubble)
        {
            styledRoot = bubble;
            CacheAndCreateStyleObjects();
            styleApplyCount++;
        }

        ApplyStyle();
    }

    private void CacheAndCreateStyleObjects()
    {
        Transform backgroundTransform = styledRoot.Find(BackgroundName);
        Transform textTransform = styledRoot.Find(TextName);
        background = backgroundTransform != null
            ? backgroundTransform.GetComponent<SpriteRenderer>()
            : null;
        dialogueText = textTransform != null ? textTransform.GetComponent<TextMesh>() : null;
        if (background == null || dialogueText == null) return;

        border = GetOrCreateSprite(BorderName, background.sortingOrder - 1);
        accent = GetOrCreateSprite(AccentName, background.sortingOrder + 1);
        titleText = GetOrCreateText(TitleName, background.sortingOrder + 2);
    }

    private void ApplyStyle()
    {
        if (!HasStyledBubble) return;

        dialogueText.fontSize = dialogueFontSize;
        dialogueText.characterSize = dialogueCharacterSize;
        dialogueText.color = dialogueColor;
        dialogueText.anchor = TextAnchor.MiddleCenter;
        dialogueText.alignment = TextAlignment.Center;
        dialogueText.transform.localPosition = new Vector3(0f, -0.13f, -0.04f);

        Vector3 panelScale = background.transform.localScale;
        panelScale.x = Mathf.Max(5.2f, panelScale.x);
        panelScale.y = Mathf.Max(minimumPanelHeight, panelScale.y);
        panelScale.z = 1f;
        background.transform.localScale = panelScale;
        background.color = panelColor;

        border.sprite = background.sprite;
        border.color = borderColor;
        border.transform.localPosition = new Vector3(0f, 0f, 0.035f);
        border.transform.localScale = new Vector3(panelScale.x + 0.16f, panelScale.y + 0.16f, 1f);

        accent.sprite = background.sprite;
        accent.color = borderColor;
        accent.transform.localPosition = new Vector3(0f, panelScale.y * 0.5f - 0.43f, -0.025f);
        accent.transform.localScale = new Vector3(panelScale.x - 0.34f, 0.035f, 1f);

        titleText.text = title;
        titleText.fontSize = 44;
        titleText.characterSize = 0.045f;
        titleText.color = titleColor;
        titleText.anchor = TextAnchor.UpperLeft;
        titleText.alignment = TextAlignment.Left;
        titleText.transform.localPosition = new Vector3(
            -panelScale.x * 0.5f + 0.24f,
            panelScale.y * 0.5f - 0.09f,
            -0.05f);
    }

    // 실제 렌더링 경계를 비교해 대사 글씨가 패널 밖으로 튀어나왔는지 검증합니다.
    private bool IsTextInsidePanel()
    {
        if (background == null || dialogueText == null) return false;
        Renderer textRenderer = dialogueText.GetComponent<Renderer>();
        if (textRenderer == null || string.IsNullOrEmpty(dialogueText.text)) return true;

        Bounds panelBounds = background.bounds;
        Bounds textBounds = textRenderer.bounds;
        const float margin = 0.08f;
        return textBounds.min.x >= panelBounds.min.x + margin
            && textBounds.max.x <= panelBounds.max.x - margin
            && textBounds.min.y >= panelBounds.min.y + margin
            && textBounds.max.y <= panelBounds.max.y - margin;
    }

    private SpriteRenderer GetOrCreateSprite(string objectName, int sortingOrder)
    {
        Transform child = styledRoot.Find(objectName);
        GameObject target = child != null ? child.gameObject : new GameObject(objectName);
        if (child == null) target.transform.SetParent(styledRoot, false);
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = target.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private TextMesh GetOrCreateText(string objectName, int sortingOrder)
    {
        Transform child = styledRoot.Find(objectName);
        GameObject target = child != null ? child.gameObject : new GameObject(objectName);
        if (child == null) target.transform.SetParent(styledRoot, false);
        TextMesh textMesh = target.GetComponent<TextMesh>();
        if (textMesh == null) textMesh = target.AddComponent<TextMesh>();
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = sortingOrder;
        return textMesh;
    }
}
