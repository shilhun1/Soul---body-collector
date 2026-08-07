using System.Collections;
using System.Text;
using UnityEngine;

public enum HWJ_BossDialogueSequenceType
{
    None,
    Intro,
    PhaseTransition,
    Death
}

/// <summary>
/// 보스 체력바 위에 작은 대사창을 표시하고, 전투 단계별 대사를 정해진 순서로 재생합니다.
/// 대사 내용, 표시 시간, 글자 크기는 프리팹 Inspector에서 조정할 수 있습니다.
/// </summary>
public class HWJ_BossDialogueBubbleSystem : MonoBehaviour
{
    [Header("위치")]
    [Tooltip("보스 체력바 Transform을 연결합니다. 대사는 이 기준점 위에 표시됩니다.")]
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 1.15f, 0f);

    [Header("대사 순서")]
    [SerializeField] private string[] introDialogueSequence = CreateIntroSequence();
    [SerializeField] private string[] phaseTwoDialogueSequence = CreatePhaseTwoSequence();
    [SerializeField] private string[] deathDialogueSequence = CreateDeathSequence();
    [SerializeField] private string soulLostDialogue = "도망칠 생각인가.";

    [Header("재생 시간")]
    [SerializeField, Min(0.1f)] private float sequenceLineDurationSeconds = 4.5f;
    [SerializeField, Min(0f)] private float secondsPerCharacter = 0.16f;
    [SerializeField, Min(0.1f)] private float maximumLineDurationSeconds = 7f;
    [SerializeField, Min(0f)] private float sequenceGapSeconds = 0.12f;
    [SerializeField, Min(0.1f)] private float defaultDurationSeconds = 4.5f;

    [Header("대사창 표시")]
    [SerializeField, Range(12, 64)] private int dialogueFontSize = 30;
    [SerializeField, Range(0.03f, 0.15f)] private float dialogueCharacterSize = 0.052f;
    [SerializeField, Range(12, 32)] private int maxCharactersPerLine = 16;
    [SerializeField, Min(0f)] private float horizontalBubblePadding = 1.6f;
    [SerializeField, Min(0f)] private float verticalBubblePadding = 0.85f;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.84f);
    [SerializeField] private int sortingOrder = 240;

    private static Sprite sharedBubbleSprite;

    private GameObject bubbleObject;
    private SpriteRenderer bubbleRenderer;
    private TextMesh textMesh;
    private Coroutine hideRoutine;
    private Coroutine sequenceRoutine;
    private bool blocksBossActions;

    public bool IsSequencePlaying => sequenceRoutine != null;
    public bool BlocksBossActions => blocksBossActions && IsSequencePlaying;
    public bool IsPhaseTransitionSequencePlaying =>
        IsSequencePlaying && ActiveSequenceType == HWJ_BossDialogueSequenceType.PhaseTransition;
    public bool IsDeathSequencePlaying =>
        IsSequencePlaying && ActiveSequenceType == HWJ_BossDialogueSequenceType.Death;
    public HWJ_BossDialogueSequenceType ActiveSequenceType { get; private set; }
    public int CurrentLineIndex { get; private set; } = -1;
    public string CurrentLineText { get; private set; } = string.Empty;
    public Transform BubbleAnchor => bubbleAnchor;
    public Vector3 BubbleWorldPosition => bubbleObject != null
        ? bubbleObject.transform.position
        : GetBubblePosition();
    public float DialogueCharacterSize => dialogueCharacterSize;
    public Vector2 CurrentRenderedTextSize { get; private set; }
    public Vector2 CurrentBubbleSize => bubbleRenderer != null
        ? bubbleRenderer.transform.localScale
        : Vector2.zero;

    private void OnDisable()
    {
        CancelDialogueSequence(true);
    }

    /// <summary>
    /// 통합 빌더가 생성 프리팹의 체력바와 표시 기본값을 한 번에 연결할 때 사용합니다.
    /// </summary>
    public void ConfigureFighterBossDefaults(Transform healthBarAnchor)
    {
        bubbleAnchor = healthBarAnchor;
        bubbleOffset = new Vector3(0f, 1.35f, 0f);
        introDialogueSequence = CreateIntroSequence();
        phaseTwoDialogueSequence = CreatePhaseTwoSequence();
        deathDialogueSequence = CreateDeathSequence();
        sequenceLineDurationSeconds = 4.5f;
        secondsPerCharacter = 0.16f;
        maximumLineDurationSeconds = 7f;
        sequenceGapSeconds = 0.12f;
        defaultDurationSeconds = 4.5f;
        dialogueFontSize = 30;
        dialogueCharacterSize = 0.052f;
        maxCharactersPerLine = 16;
        horizontalBubblePadding = 1.6f;
        verticalBubblePadding = 0.85f;
        bubbleColor = new Color(1f, 1f, 1f, 0.84f);
        sortingOrder = 240;
    }

    public void ShowIntroDialogue()
    {
        PlaySequence(
            HWJ_BossDialogueSequenceType.Intro,
            introDialogueSequence,
            true);
    }

    public void ShowPhaseTwoDialogue()
    {
        PlaySequence(
            HWJ_BossDialogueSequenceType.PhaseTransition,
            phaseTwoDialogueSequence,
            true);
    }

    public void ShowDeathDialogue()
    {
        PlaySequence(
            HWJ_BossDialogueSequenceType.Death,
            deathDialogueSequence,
            true);
    }

    public void ShowSoulLostDialogue()
    {
        ShowLine(soulLostDialogue, defaultDurationSeconds);
    }

    /// <summary>
    /// 짧은 문장은 최소 표시 시간을 보장하고, 긴 문장은 글자 수에 맞춰 더 오래 보여줍니다.
    /// 카메라 포커스도 같은 값을 사용하므로 대사보다 먼저 줌이 풀리지 않습니다.
    /// </summary>
    public float GetReadableLineDuration(string line)
    {
        int visibleCharacterCount = 0;

        if (!string.IsNullOrEmpty(line))
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (!char.IsWhiteSpace(line[i]))
                {
                    visibleCharacterCount++;
                }
            }
        }

        float minimumDuration = Mathf.Max(0.01f, sequenceLineDurationSeconds);
        float maximumDuration = Mathf.Max(minimumDuration, maximumLineDurationSeconds);
        float readingDuration = visibleCharacterCount * Mathf.Max(0f, secondsPerCharacter);
        return Mathf.Clamp(readingDuration, minimumDuration, maximumDuration);
    }

    public float GetSoulLostDialogueDuration()
    {
        return Mathf.Max(defaultDurationSeconds, GetReadableLineDuration(soulLostDialogue));
    }

    /// <summary>
    /// 단발성 대사를 표시합니다. 진행 중인 단계 대사가 있으면 단발성 대사로 교체합니다.
    /// </summary>
    public void ShowLine(string line, float durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        CancelDialogueSequence(false);
        DisplayLine(line);
        hideRoutine = StartCoroutine(HideAfterSeconds(
            Mathf.Max(durationSeconds, GetReadableLineDuration(line))));
    }

    public void CancelDialogueSequence(bool hideBubble)
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        blocksBossActions = false;
        ActiveSequenceType = HWJ_BossDialogueSequenceType.None;
        CurrentLineIndex = -1;

        if (hideBubble && bubbleObject != null)
        {
            bubbleObject.SetActive(false);
        }
    }

    public int GetSequenceLineCount(HWJ_BossDialogueSequenceType sequenceType)
    {
        string[] sequence = GetSequence(sequenceType);
        return sequence != null ? sequence.Length : 0;
    }

    public string GetSequenceLine(HWJ_BossDialogueSequenceType sequenceType, int index)
    {
        string[] sequence = GetSequence(sequenceType);
        return sequence != null && index >= 0 && index < sequence.Length
            ? sequence[index]
            : string.Empty;
    }

    /// <summary>
    /// 카메라가 대사 전체를 놓치지 않도록 현재 Inspector 시간값으로 총 재생 시간을 계산합니다.
    /// </summary>
    public float GetSequenceDuration(HWJ_BossDialogueSequenceType sequenceType)
    {
        string[] sequence = GetSequence(sequenceType);

        if (sequence == null || sequence.Length == 0)
        {
            return 0f;
        }

        int visibleLineCount = 0;

        for (int i = 0; i < sequence.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(sequence[i]))
            {
                visibleLineCount++;
            }
        }

        float duration = 0f;

        for (int i = 0; i < sequence.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(sequence[i]))
            {
                duration += GetReadableLineDuration(sequence[i]);
            }
        }

        return duration
            + Mathf.Max(0, visibleLineCount - 1) * Mathf.Max(0f, sequenceGapSeconds);
    }

    private void PlaySequence(
        HWJ_BossDialogueSequenceType sequenceType,
        string[] sequence,
        bool blockBossActions)
    {
        CancelDialogueSequence(false);

        if (sequence == null || sequence.Length == 0)
        {
            if (bubbleObject != null)
            {
                bubbleObject.SetActive(false);
            }

            return;
        }

        blocksBossActions = blockBossActions;
        ActiveSequenceType = sequenceType;
        sequenceRoutine = StartCoroutine(SequenceRoutine(sequence));
    }

    private IEnumerator SequenceRoutine(string[] sequence)
    {
        for (int i = 0; i < sequence.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(sequence[i]))
            {
                continue;
            }

            CurrentLineIndex = i;
            CurrentLineText = sequence[i];
            DisplayLine(sequence[i]);
            yield return new WaitForSeconds(GetReadableLineDuration(sequence[i]));

            if (sequenceGapSeconds > 0f && i < sequence.Length - 1)
            {
                bubbleObject.SetActive(false);
                yield return new WaitForSeconds(sequenceGapSeconds);
            }
        }

        if (bubbleObject != null)
        {
            bubbleObject.SetActive(false);
        }

        sequenceRoutine = null;
        blocksBossActions = false;
        ActiveSequenceType = HWJ_BossDialogueSequenceType.None;
        CurrentLineIndex = -1;
    }

    private void DisplayLine(string line)
    {
        EnsureBubble();

        string wrappedLine = WrapLine(line, out int longestLineLength, out int lineCount);
        bubbleObject.SetActive(true);
        bubbleObject.transform.position = GetBubblePosition();

        textMesh.text = wrappedLine;
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = dialogueFontSize;
        textMesh.characterSize = dialogueCharacterSize;
        textMesh.lineSpacing = 0.9f;

        // 실제 TextMesh 경계를 기준으로 배경을 잡아 한글 글자가 흰 영역 밖으로 잘리지 않게 합니다.
        Renderer renderedText = textMesh.GetComponent<Renderer>();
        Vector3 renderedSize = renderedText != null
            ? renderedText.bounds.size
            : Vector3.zero;
        CurrentRenderedTextSize = new Vector2(renderedSize.x, renderedSize.y);

        float estimatedWidth = longestLineLength * 0.14f + horizontalBubblePadding;
        float estimatedHeight = lineCount * 0.4f + verticalBubblePadding;
        float width = Mathf.Max(5.2f, estimatedWidth, renderedSize.x + horizontalBubblePadding);
        float height = Mathf.Max(1.4f, estimatedHeight, renderedSize.y + verticalBubblePadding);
        bubbleRenderer.transform.localScale = new Vector3(width, height, 1f);
    }

    private void LateUpdate()
    {
        if (bubbleObject != null && bubbleObject.activeSelf)
        {
            bubbleObject.transform.position = GetBubblePosition();
        }
    }

    private void EnsureBubble()
    {
        if (bubbleObject != null)
        {
            return;
        }

        bubbleObject = new GameObject("HWJ_BossDialogueBubble");
        bubbleObject.transform.SetParent(transform, false);

        GameObject background = new GameObject("BubbleBackground");
        background.transform.SetParent(bubbleObject.transform, false);

        bubbleRenderer = background.AddComponent<SpriteRenderer>();
        bubbleRenderer.sprite = GetBubbleSprite();
        bubbleRenderer.color = bubbleColor;
        bubbleRenderer.sortingOrder = sortingOrder;

        GameObject textObject = new GameObject("BubbleText");
        textObject.transform.SetParent(bubbleObject.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = textColor;
        textMesh.fontSize = dialogueFontSize;
        textMesh.characterSize = dialogueCharacterSize;
        textMesh.lineSpacing = 0.9f;

        MeshRenderer textRenderer = textMesh.GetComponent<MeshRenderer>();
        textRenderer.sortingOrder = sortingOrder + 1;
    }

    private Vector3 GetBubblePosition()
    {
        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        return anchor.position + bubbleOffset;
    }

    private IEnumerator HideAfterSeconds(float durationSeconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, durationSeconds));

        if (bubbleObject != null)
        {
            bubbleObject.SetActive(false);
        }

        hideRoutine = null;
        CurrentLineText = string.Empty;
    }

    private string WrapLine(string line, out int longestLineLength, out int lineCount)
    {
        string[] words = line.Trim().Split(' ');
        StringBuilder result = new StringBuilder();
        StringBuilder currentLine = new StringBuilder();
        longestLineLength = 0;
        lineCount = 0;
        int maximumCharacters = Mathf.Max(8, maxCharactersPerLine);

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            int requiredLength = currentLine.Length == 0
                ? word.Length
                : currentLine.Length + 1 + word.Length;

            if (currentLine.Length > 0 && requiredLength > maximumCharacters)
            {
                AppendWrappedLine(result, currentLine, ref longestLineLength, ref lineCount);
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(word);
        }

        AppendWrappedLine(result, currentLine, ref longestLineLength, ref lineCount);
        return result.ToString();
    }

    private static void AppendWrappedLine(
        StringBuilder destination,
        StringBuilder line,
        ref int longestLineLength,
        ref int lineCount)
    {
        if (line.Length == 0)
        {
            return;
        }

        if (destination.Length > 0)
        {
            destination.Append('\n');
        }

        destination.Append(line);
        longestLineLength = Mathf.Max(longestLineLength, line.Length);
        lineCount++;
        line.Clear();
    }

    private string[] GetSequence(HWJ_BossDialogueSequenceType sequenceType)
    {
        return sequenceType switch
        {
            HWJ_BossDialogueSequenceType.Intro => introDialogueSequence,
            HWJ_BossDialogueSequenceType.PhaseTransition => phaseTwoDialogueSequence,
            HWJ_BossDialogueSequenceType.Death => deathDialogueSequence,
            _ => null
        };
    }

    private static string[] CreateIntroSequence()
    {
        return new[]
        {
            "여기까지 살아서 왔다는 건 인정하지.",
            "하지만 운 좋게 살아남은 것과 강한 것은 전혀 다르다.",
            "나는 신도, 왕도, 동료도 믿지 않는다.",
            "내가 믿는 것은 오직 이 두 주먹뿐이다.",
            "약자는 강자의 앞에서 쓰러지는 것이 당연하다.",
            "네가 어느 쪽인지, 지금 증명해 봐라.",
            "자세를 잡아라. 변명할 시간은 끝났다."
        };
    }

    private static string[] CreatePhaseTwoSequence()
    {
        return new[]
        {
            "말도 안 돼......",
            "내 주먹이...... 약자에게 밀렸다고?",
            "아니.",
            "아직 내가 약해진 것이 아니다.",
            "내 육체가...... 내 힘을 따라오지 못했을 뿐이다.",
            "듣고 있나, 주인이여.",
            "네 힘을 내게 넘겨라.",
            "복종하려는 것이 아니다.",
            "그 힘조차 내 주먹 아래 굴복시키겠다.",
            "그래...... 이게 진정한 힘인가.",
            "이제 알겠군.",
            "네가 강한 것이 아니다.",
            "내가 지금까지 스스로를 억누르고 있었을 뿐이다.",
            "두 번째 종이 울렸다.",
            "이번에는 뼈 하나 남기지 않겠다."
        };
    }

    private static string[] CreateDeathSequence()
    {
        return new[]
        {
            "내가...... 패배했다고?",
            "힘이 전부라면......",
            "지금 강한 쪽은...... 너인가......",
            "동정하지 마라.",
            "승자가 패자에게 내미는 손만큼...... 역겨운 것도 없으니까.",
            "가라......",
            "그리고 끝까지 이겨라.",
            "네가 한 번이라도 무릎 꿇는다면......",
            "오늘의 승리도 전부 거짓이 될 테니......"
        };
    }

    private static Sprite GetBubbleSprite()
    {
        if (sharedBubbleSprite != null)
        {
            return sharedBubbleSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        sharedBubbleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        return sharedBubbleSprite;
    }
}
