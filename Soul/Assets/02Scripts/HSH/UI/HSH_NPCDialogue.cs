using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 대사 화자 구분 (NPC 또는 플레이어)
/// </summary>
public enum DialogueSpeaker
{
    NPC,
    Player
}

/// <summary>
/// 단일 대사 데이터 (화자 + 대사 내용)
/// </summary>
[System.Serializable]
public class DialogueData
{
    [Tooltip("대사 화자 (NPC 또는 Player)")]
    public DialogueSpeaker speaker = DialogueSpeaker.NPC;

    [TextArea(2, 5)]
    public string sentence;
}

public class HSH_NPCDialogue : MonoBehaviour
{
    [Header("--- NPC 말풍선 UI ---")]
    public GameObject npcBubbleObject;
    public RectTransform npcBubbleBackground;
    public TextMeshProUGUI npcDialogueText;
    public Vector3 npcBubbleOffset = new Vector3(0, 2.5f, 0);
    public Transform npcTransform;

    [Header("--- 플레이어 말풍선 UI ---")]
    public GameObject playerBubbleObject;
    public RectTransform playerBubbleBackground;
    public TextMeshProUGUI playerDialogueText;
    public Vector3 playerBubbleOffset = new Vector3(0, 2.5f, 0);
    [Tooltip("플레이어 위치 참조 (비워두면 코드에서 자동으로 'Player' 태그 또는 플레이어 오브젝트를 찾습니다)")]
    public Transform playerTransform;

    [Header("--- 말풍선 공통 설정 ---")]
    public float typingSpeed = 0.05f;
    public Vector2 padding = new Vector2(50f, 50f);
    public bool isScreenSpaceUI = true;

    [Header("--- 순서 지정 대사 데이터 목록 ---")]
    [Tooltip("인스펙터의 리스트 순서(0, 1, 2...)대로 대사가 진행되며 화자(NPC/Player)를 지정할 수 있습니다.")]
    public List<DialogueData> dialogueList = new List<DialogueData>();

    [Header("--- [하위 호환 전용] 기존 단일 UI 및 대사 목록 ---")]
    public GameObject bubbleObject;
    public RectTransform bubbleBackground;
    public TextMeshProUGUI dialogueText;
    public Vector3 bubbleOffset = new Vector3(0, 2.5f, 0);

    [TextArea(3, 5)]
    public string[] dialogues;

    private Coroutine dialogueCoroutine;
    private bool isPlayerInRange = false;
    private bool isTyping = false;
    private int currentDialogueIndex = 0;

    // 현재 활성화된 화자 및 UI 참조
    private DialogueSpeaker currentSpeaker = DialogueSpeaker.NPC;
    private TextMeshProUGUI currentActiveText;
    private RectTransform currentActiveBackground;

    private void Awake()
    {
        // 기존(구버전) Inspector 세팅 하위 호환 처리
        if (npcBubbleObject == null && bubbleObject != null)
        {
            npcBubbleObject = bubbleObject;
            npcBubbleBackground = bubbleBackground;
            npcDialogueText = dialogueText;
            npcBubbleOffset = bubbleOffset;
        }

        // 기존 dialogues (string[]) 목록을 dialogueList로 자동 변환
        if ((dialogueList == null || dialogueList.Count == 0) && (dialogues != null && dialogues.Length > 0))
        {
            dialogueList = new List<DialogueData>();
            foreach (string line in dialogues)
            {
                dialogueList.Add(new DialogueData { speaker = DialogueSpeaker.NPC, sentence = line });
            }
        }
    }

    private void Start()
    {
        // 시작 시 모든 말풍선 숨김 및 플레이어 자동 탐색 시도
        HideAllBubbles();
        EnsurePlayerTransform();
    }

    private void Update()
    {
        // 플레이어가 근처에 있고 'E' 키 입력 시 대화 상호작용
        if (isPlayerInRange && UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                Interact();
            }
        }
    }

    private void LateUpdate()
    {
        // 1. NPC 말풍선 위치 갱신
        if (npcBubbleObject != null && npcBubbleObject.activeSelf)
        {
            Transform target = npcTransform != null ? npcTransform : transform;
            UpdateBubblePosition(npcBubbleObject, target.position + npcBubbleOffset);
        }

        // 2. 플레이어 말풍선 위치 갱신 (플레이어 위치 자동 수급)
        if (playerBubbleObject != null && playerBubbleObject.activeSelf)
        {
            EnsurePlayerTransform();
            if (playerTransform != null)
            {
                UpdateBubblePosition(playerBubbleObject, playerTransform.position + playerBubbleOffset);
            }
        }
    }

    /// <summary>
    /// 여러 NPC가 있는 상황에서도 코드 단에서 플레이어의 Transform을 자동으로 찾아 보장하는 메쏘드입니다.
    /// </summary>
    public Transform EnsurePlayerTransform()
    {
        if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
        {
            return playerTransform;
        }

        // 1순위: "Player" 태그를 가진 오브젝트 탐색
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            return playerTransform;
        }

        // 2순위: 씬 내 플레이어 이동/상태 관련 대표 스크립트 기반 탐색
        var movementScript = FindFirstObjectByType<hys_Player_Movement>();
        if (movementScript != null)
        {
            playerTransform = movementScript.transform;
            return playerTransform;
        }

        return playerTransform;
    }

    private void UpdateBubblePosition(GameObject bubbleObj, Vector3 worldPosition)
    {
        if (isScreenSpaceUI && Camera.main != null)
        {
            bubbleObj.transform.position = Camera.main.WorldToScreenPoint(worldPosition);
        }
        else
        {
            bubbleObj.transform.position = worldPosition;
        }
    }

    /// <summary>
    /// 대화 진행 (E 키 눌렀을 때 호출) - 순서대로 대사 재생
    /// </summary>
    private void Interact()
    {
        if (dialogueList == null || dialogueList.Count == 0) return;

        EnsurePlayerTransform(); // 플레이어 위치 최신화

        bool anyBubbleActive = (npcBubbleObject != null && npcBubbleObject.activeSelf) ||
                               (playerBubbleObject != null && playerBubbleObject.activeSelf);

        // 첫 번째 대사 시작
        if (!anyBubbleActive)
        {
            currentDialogueIndex = 0;
            PlayDialogue(currentDialogueIndex);
        }
        else
        {
            // 타이핑 중이면 즉시 전체 문장 출력 (스킵)
            if (isTyping)
            {
                if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);

                DialogueData currentData = dialogueList[currentDialogueIndex];
                if (currentActiveText != null)
                {
                    currentActiveText.text = currentData.sentence;
                    UpdateBubbleSize();
                }
                isTyping = false;
            }
            else
            {
                // 다음 대사 순서로 넘어가기 (0 -> 1 -> 2 -> ...)
                currentDialogueIndex++;

                if (currentDialogueIndex < dialogueList.Count)
                {
                    PlayDialogue(currentDialogueIndex);
                }
                else
                {
                    // 모든 대사 완료 시 종료
                    EndDialogue();
                }
            }
        }
    }

    private void PlayDialogue(int index)
    {
        if (index < 0 || index >= dialogueList.Count) return;

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        DialogueData data = dialogueList[index];
        currentSpeaker = data.speaker;

        // 화자(NPC / Player)에 따라 말풍선 교체
        if (currentSpeaker == DialogueSpeaker.NPC)
        {
            if (playerBubbleObject != null) playerBubbleObject.SetActive(false);
            if (npcBubbleObject != null) npcBubbleObject.SetActive(true);

            currentActiveBackground = npcBubbleBackground;
            currentActiveText = npcDialogueText;
        }
        else // Player
        {
            EnsurePlayerTransform();

            if (npcBubbleObject != null) npcBubbleObject.SetActive(false);
            if (playerBubbleObject != null) playerBubbleObject.SetActive(true);

            currentActiveBackground = playerBubbleBackground;
            currentActiveText = playerDialogueText;
        }

        if (currentActiveText != null)
        {
            dialogueCoroutine = StartCoroutine(TypeSentence(data.sentence));
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        currentActiveText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            currentActiveText.text += letter;
            UpdateBubbleSize();
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void UpdateBubbleSize()
    {
        if (currentActiveBackground != null && currentActiveText != null)
        {
            currentActiveText.ForceMeshUpdate();
            Vector2 textSize = currentActiveText.GetRenderedValues(false);
            currentActiveBackground.sizeDelta = textSize + padding;
        }
    }

    public void EndDialogue()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        if (currentActiveText != null)
        {
            currentActiveText.text = "";
        }

        isTyping = false;
        HideAllBubbles();
    }

    private void HideAllBubbles()
    {
        if (npcBubbleObject != null) npcBubbleObject.SetActive(false);
        if (playerBubbleObject != null) playerBubbleObject.SetActive(false);
        if (bubbleObject != null) bubbleObject.SetActive(false);
    }

    // --- 플레이어 감지 (Trigger) ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.name.Contains("Player") || collision.GetComponent<hys_Player_Movement>() != null)
        {
            isPlayerInRange = true;
            playerTransform = collision.transform;
        }
        else
        {
            isPlayerInRange = true;
            if (playerTransform == null)
            {
                playerTransform = collision.transform;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        isPlayerInRange = false;
        EndDialogue();
    }
}
