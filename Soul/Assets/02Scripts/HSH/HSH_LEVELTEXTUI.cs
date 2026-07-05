using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HSH_LEVELTEXTUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelTextTMP;
    public Text levelTextLegacy;

    [Header("Level Data")]
    public int currentLevel = 1;

    private void Start()
    {
        // 텍스트 컴포넌트가 연결되지 않았다면 자신에게서 찾기
        if (levelTextTMP == null) levelTextTMP = GetComponent<TMP_Text>();
        if (levelTextLegacy == null) levelTextLegacy = GetComponent<Text>();
        
        UpdateText();
    }

    // 레벨을 1 증가시키고 텍스트를 업데이트하는 함수
    public void LevelUp()
    {
        currentLevel++;
        UpdateText();
        Debug.Log("현재 레벨: " + currentLevel);
    }

    // 텍스트에 레벨을 반영
    private void UpdateText()
    {
        string levelString = "Lv. " + currentLevel.ToString();

        if (levelTextTMP != null)
        {
            levelTextTMP.text = levelString;
        }
        else if (levelTextLegacy != null)
        {
            levelTextLegacy.text = levelString;
        }
    }
}
