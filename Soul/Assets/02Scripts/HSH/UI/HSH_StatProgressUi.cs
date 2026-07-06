using UnityEngine;
using UnityEngine.UI;

public class HSH_StatProgressUi : MonoBehaviour
{
    [Tooltip("기본 Unity UI Text 컴포넌트 (있는 경우 드래그 앤 드롭)")]
    public Text uiText;

    [Tooltip("TextMeshPro 컴포넌트가 있다면 여기에 할당하세요.")]
    public Behaviour tmpText;

    // UI 띄우기 및 텍스트 갱신
    public void ShowUI(string keyName)
    {
        gameObject.SetActive(true);
        string message = $"{keyName}";

        // 일반 텍스트 갱신
        if (uiText != null)
        {
            uiText.text = message;
        }

        // TextMeshPro 텍스트 갱신 (컴파일 에러를 막기 위해 Reflection 사용)
        if (tmpText != null)
        {
            var textProp = tmpText.GetType().GetProperty("text");
            if (textProp != null)
            {
                textProp.SetValue(tmpText, message);
            }
        }
    }

    // UI 숨기기
    public void HideUI()
    {
        gameObject.SetActive(false);
    }
}
