using System.Collections;
using UnityEngine;

public class HWJ_BossDialogueBubbleSystem : MonoBehaviour
{
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private string introDialogue = "왔군.";
    [SerializeField] private string phaseTwoDialogue = "여기서 끝내겠다.";
    [SerializeField] private string soulLostDialogue = "도망칠 생각인가.";
    [SerializeField] private float defaultDurationSeconds = 2.2f;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private int sortingOrder = 220;

    private static Sprite sharedBubbleSprite;

    private GameObject bubbleObject;
    private SpriteRenderer bubbleRenderer;
    private TextMesh textMesh;
    private Coroutine hideRoutine;

    public void ShowIntroDialogue()
    {
        ShowLine(introDialogue, defaultDurationSeconds);
    }

    public void ShowPhaseTwoDialogue()
    {
        ShowLine(phaseTwoDialogue, defaultDurationSeconds);
    }

    public void ShowSoulLostDialogue()
    {
        ShowLine(soulLostDialogue, defaultDurationSeconds);
    }

    public void ShowLine(string line, float durationSeconds)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        EnsureBubble();
        bubbleObject.SetActive(true);
        bubbleObject.transform.position = GetBubblePosition();

        textMesh.text = line;
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.16f;
        textMesh.fontSize = 48;

        float width = Mathf.Clamp(line.Length * 0.18f + 0.8f, 1.8f, 4.6f);
        float height = Mathf.Clamp(Mathf.Ceil(line.Length / 14f) * 0.38f + 0.55f, 0.8f, 2.2f);
        bubbleRenderer.transform.localScale = new Vector3(width, height, 1f);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterSeconds(durationSeconds));
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
        textMesh.fontSize = 48;
        textMesh.characterSize = 0.16f;
        textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 1;
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
        sharedBubbleSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return sharedBubbleSprite;
    }
}
