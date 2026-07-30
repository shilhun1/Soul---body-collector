using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class HWJ_BossDialogueBubbleSystem : MonoBehaviour
{
    [Serializable]
    private struct DialogueLine
    {
        [TextArea(2, 4)]
        public string text;

        [Min(0f)]
        public float holdSeconds;
    }

    [Header("References")]
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Canvas bubbleCanvas;

    [Header("Position")]
    [SerializeField]
    private Vector3 bubbleOffset =
        new Vector3(0f, 0.2f, 0f);

    [Header("Bubble Size")]
    [SerializeField] private float minWidth = 180f;
    [SerializeField] private float maxWidth = 420f;
    [SerializeField] private float minHeight = 75f;
    [SerializeField] private float maxHeight = 220f;
    [SerializeField]
    private Vector2 padding =
        new Vector2(28f, 18f);

    [Header("Text")]
    [SerializeField] private float typeSecondsPerCharacter = 0.025f;
    [SerializeField] private float gapBetweenLines = 0.25f;

    [Header("Phase 1 Intro")]
    [SerializeField]
    private DialogueLine[] phaseOneIntroLines =
    {
        new DialogueLine
        {
            text = "여기까지 살아서 왔다는 건 인정하지.",
            holdSeconds = 1.5f
        },
        new DialogueLine
        {
            text = "하지만 살아남은 것과 강한 것은 다르다.",
            holdSeconds = 1.7f
        },
        new DialogueLine
        {
            text = "나는 신도, 왕도, 동료도 믿지 않는다.",
            holdSeconds = 1.7f
        },
        new DialogueLine
        {
            text = "내가 믿는 것은 오직 이 두 주먹뿐이다.",
            holdSeconds = 1.8f
        },
        new DialogueLine
        {
            text = "약자는 강자의 앞에서 쓰러지는 것이 당연하다.",
            holdSeconds = 1.8f
        },
        new DialogueLine
        {
            text = "네가 어느 쪽인지, 지금 증명해 봐라.",
            holdSeconds = 1.6f
        },
        new DialogueLine
        {
            text = "자세를 잡아라. 변명할 시간은 끝났다.",
            holdSeconds = 1.8f
        }
    };

    [Header("Phase 2")]
    [SerializeField]
    private DialogueLine[] phaseTwoLines =
    {
        new DialogueLine
        {
            text = "내가 약자에게 밀렸다고?",
            holdSeconds = 1.5f
        },
        new DialogueLine
        {
            text = "아니. 부족했던 것은 힘뿐이다.",
            holdSeconds = 1.6f
        },
        new DialogueLine
        {
            text = "주인이여, 네 힘을 내게 넘겨라.",
            holdSeconds = 1.8f
        },
        new DialogueLine
        {
            text = "그 힘조차 내 주먹 아래 굴복시키겠다!",
            holdSeconds = 2f
        },
        new DialogueLine
        {
            text = "두 번째 종이 울렸다.",
            holdSeconds = 1.5f
        }
    };

    [Header("Soul Lost")]
    [SerializeField]
    private DialogueLine[] soulLostLines =
    {
        new DialogueLine
        {
            text = "도망칠 생각인가.",
            holdSeconds = 1.5f
        },
        new DialogueLine
        {
            text = "약자는 언제나 등을 보이지.",
            holdSeconds = 1.7f
        }
    };

    [Header("Events")]
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onSequenceFinished;

    private Coroutine sequenceRoutine;

    public bool IsShowing { get; private set; }

    private void Awake()
    {
        if (bubbleCanvas != null)
        {
            bubbleCanvas.overrideSorting = true;
            bubbleCanvas.sortingOrder = 220;
        }

        if (dialogueText != null)
        {
            dialogueText.enableWordWrapping = true;
            dialogueText.fontSize = 24f;
            dialogueText.enableAutoSizing = true;
            dialogueText.fontSizeMin = 16f;
            dialogueText.fontSizeMax = 24f;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        if (bubbleRoot != null)
        {
            bubbleRoot.SetActive(false);
        }
    }

    public void ShowIntroDialogue()
    {
        PlaySequence(phaseOneIntroLines);
    }

    public void ShowPhaseTwoDialogue()
    {
        PlaySequence(phaseTwoLines);
    }

    public void ShowSoulLostDialogue()
    {
        PlaySequence(soulLostLines);
    }

    public void ShowLine(string line, float durationSeconds)
    {
        DialogueLine[] singleLine =
        {
            new DialogueLine
            {
                text = line,
                holdSeconds = durationSeconds
            }
        };

        PlaySequence(singleLine);
    }

    public void StopDialogue()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        IsShowing = false;

        if (bubbleRoot != null)
        {
            bubbleRoot.SetActive(false);
        }
    }

    private void PlaySequence(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            return;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(PlaySequenceRoutine(lines));
    }

    private IEnumerator PlaySequenceRoutine(DialogueLine[] lines)
    {
        IsShowing = true;
        onSequenceStarted?.Invoke();

        if (bubbleRoot != null)
        {
            bubbleRoot.SetActive(true);
        }

        UpdateBubblePosition();

        foreach (DialogueLine dialogueLine in lines)
        {
            if (string.IsNullOrWhiteSpace(dialogueLine.text))
            {
                continue;
            }

            ResizeBubble(dialogueLine.text);

            yield return TypeLine(dialogueLine.text);

            float holdTime = Mathf.Max(0f, dialogueLine.holdSeconds);
            if (holdTime > 0f)
            {
                yield return new WaitForSecondsRealtime(holdTime);
            }

            if (gapBetweenLines > 0f)
            {
                if (bubbleRoot != null)
                {
                    bubbleRoot.SetActive(false);
                }

                yield return new WaitForSecondsRealtime(gapBetweenLines);

                if (bubbleRoot != null)
                {
                    bubbleRoot.SetActive(true);
                }
            }
        }

        if (bubbleRoot != null)
        {
            bubbleRoot.SetActive(false);
        }

        IsShowing = false;
        sequenceRoutine = null;
        onSequenceFinished?.Invoke();
    }

    private IEnumerator TypeLine(string line)
    {
        if (dialogueText == null)
        {
            yield break;
        }

        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int characterCount = dialogueText.textInfo.characterCount;

        if (typeSecondsPerCharacter <= 0f)
        {
            dialogueText.maxVisibleCharacters = characterCount;
            yield break;
        }

        for (int i = 1; i <= characterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(
                typeSecondsPerCharacter
            );
        }
    }

    private void ResizeBubble(string line)
    {
        if (dialogueText == null ||
            bubbleRect == null ||
            textRect == null)
        {
            return;
        }

        float innerMinWidth =
            Mathf.Max(1f, minWidth - padding.x * 2f);

        float innerMaxWidth =
            Mathf.Max(innerMinWidth, maxWidth - padding.x * 2f);

        // 먼저 줄바꿈을 하지 않았을 때의 너비를 구한다.
        Vector2 unwrappedSize =
            dialogueText.GetPreferredValues(line, 0f, 0f);

        float innerWidth = Mathf.Clamp(
            unwrappedSize.x,
            innerMinWidth,
            innerMaxWidth
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            innerWidth
        );

        dialogueText.text = line;
        dialogueText.ForceMeshUpdate();

        float textHeight = dialogueText.preferredHeight;

        float bubbleWidth = Mathf.Clamp(
            innerWidth + padding.x * 2f,
            minWidth,
            maxWidth
        );

        float bubbleHeight = Mathf.Clamp(
            textHeight + padding.y * 2f,
            minHeight,
            maxHeight
        );

        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            bubbleWidth
        );

        bubbleRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            bubbleHeight
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            bubbleWidth - padding.x * 2f
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            bubbleHeight - padding.y * 2f
        );
    }

    private void LateUpdate()
    {
        if (IsShowing)
        {
            UpdateBubblePosition();
        }
    }

    private void UpdateBubblePosition()
    {
        if (bubbleRoot == null)
        {
            return;
        }

        Transform anchor =
            bubbleAnchor != null ? bubbleAnchor : transform;

        bubbleRoot.transform.position =
            anchor.position + bubbleOffset;

        // 보스가 좌우 반전되어도 말풍선 글씨는 뒤집히지 않게 한다.
        Vector3 scale = bubbleRoot.transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        bubbleRoot.transform.localScale = scale;
    }

    private void OnDisable()
    {
        StopDialogue();
    }
}