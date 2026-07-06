using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위한 네임스페이스

public class HSH_NPCDialogue : MonoBehaviour
{
    [Header("UI 연결 (Inspector에서 할당)")]
    public GameObject bubbleObject; // 말풍선 전체 오브젝트 (끄고 켜기 위함)
    public RectTransform bubbleBackground; // 크기가 변할 말풍선 배경 이미지의 RectTransform
    public TextMeshProUGUI dialogueText; // 대사가 출력될 TextMeshProUGUI 텍스트

    [Header("말풍선 설정")]
    public float typingSpeed = 0.05f; // 글자 출력 속도 (숫자가 작을수록 빠름)
    public Vector2 padding = new Vector2(50f, 50f); // 텍스트와 말풍선 배경 사이의 여백 (X, Y)

    [Header("위치 설정")]
    public Vector3 bubbleOffset = new Vector3(0, 2.5f, 0); // NPC 위치 기준으로 얼마나 위에 띄울지 (Y축)
    public Transform npcTransform; // 기준점 (비워두면 이 스크립트가 붙은 오브젝트 위치 사용)
    public bool isScreenSpaceUI = true; // 캔버스가 일반 화면 UI(Screen Space - Overlay)인 경우 체크

    
    [Header("대사 내용")]
    [TextArea(3, 5)]
    public string[] dialogues; // NPC가 할 대사 목록

    private Coroutine dialogueCoroutine;
    
    // 상호작용 관련 변수
    private bool isPlayerInRange = false; // 플레이어가 근처에 있는지 확인
    private bool isTyping = false; // 현재 타자치는 연출이 진행중인지
    private int currentDialogueIndex = 0; // 현재 진행중인 대사 순서

    private void Start()
    {
        // 처음 시작 시 말풍선을 숨깁니다.
        if (bubbleObject != null)
        {
            bubbleObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 플레이어가 범위 안에 있고 'E' 키를 눌렀을 때 상호작용 발생 (New Input System 기준)
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
        // 말풍선이 활성화되어 있을 때 항상 NPC 머리 위쪽 위치를 따라다니도록 강제
        if (bubbleObject != null && bubbleObject.activeSelf)
        {
            // npcTransform을 따로 연결 안했으면, 이 스크립트가 붙어있는 오브젝트를 기준으로 삼습니다.
            Transform target = npcTransform != null ? npcTransform : transform;
            Vector3 targetWorldPosition = target.position + bubbleOffset;

            if (isScreenSpaceUI && Camera.main != null)
            {
                // 캔버스가 화면(Screen Space - Overlay) 방식일 때, 월드 위치를 캔버스 화면 좌표로 변환합니다.
                bubbleObject.transform.position = Camera.main.WorldToScreenPoint(targetWorldPosition);
            }
            else
            {
                // 캔버스가 월드(World Space) 방식일 때
                bubbleObject.transform.position = targetWorldPosition;
            }
        }
    }

    /// <summary>
    /// 상호작용 키(E키)를 누르면 실행되는 로직
    /// </summary>
    private void Interact()
    {
        if (dialogues == null || dialogues.Length == 0) return;

        // 말풍선이 꺼져있다면 (처음 상호작용)
        if (!bubbleObject.activeSelf)
        {
            currentDialogueIndex = 0; // 첫 번째 대사부터
            PlayDialogue(currentDialogueIndex);
        }
        else
        {
            // 타이핑 연출 중이라면 한 번 더 눌렀을 때 즉시 전체 문장을 출력 (스킵 기능)
            if (isTyping)
            {
                if (dialogueCoroutine != null) StopCoroutine(dialogueCoroutine);
                dialogueText.text = dialogues[currentDialogueIndex];
                UpdateBubbleSize();
                isTyping = false;
            }
            else
            {
                // 타이핑이 끝난 상태라면 다음 대사로 넘어가기
                currentDialogueIndex++;
                
                if (currentDialogueIndex < dialogues.Length)
                {
                    PlayDialogue(currentDialogueIndex);
                }
                else
                {
                    // 모든 대사가 끝났다면 말풍선 끄기
                    EndDialogue();
                }
            }
        }
    }

    private void PlayDialogue(int index)
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        bubbleObject.SetActive(true);
        dialogueCoroutine = StartCoroutine(TypeSentence(dialogues[index]));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        // 문장을 한 글자씩 쪼개서 반복
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            UpdateBubbleSize(); // 글자가 추가될 때마다 크기 변경
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false; // 타이핑 종료
    }

    private void UpdateBubbleSize()
    {
        if (bubbleBackground != null && dialogueText != null)
        {
            dialogueText.ForceMeshUpdate();
            Vector2 textSize = dialogueText.GetRenderedValues(false);
            bubbleBackground.sizeDelta = textSize + padding;
        }
    }

    public void EndDialogue()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }
        
        dialogueText.text = "";
        isTyping = false;
        
        if (bubbleObject != null)
        {
            bubbleObject.SetActive(false);
        }
    }

    // --- 플레이어 감지 (Trigger) ---
    
    // 플레이어가 NPC 주변(Trigger)에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 필요하다면 collision.CompareTag("Player") 등으로 플레이어만 필터링 가능
        isPlayerInRange = true;
    }

    // 플레이어가 NPC 주변에서 멀어질 때
    private void OnTriggerExit2D(Collider2D collision)
    {
        isPlayerInRange = false;
        EndDialogue(); // 멀어지면 대사창 강제 종료
    }
}
