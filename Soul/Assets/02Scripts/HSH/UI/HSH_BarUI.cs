using UnityEngine;
using UnityEngine.UI;

public class HSH_BarUI : MonoBehaviour
{
    public enum BarType
    {
        HP,
        GhostHP,
        Exp
    }

    [Header("UI Settings")]
    public Slider barSlider;
    public BarType currentType = BarType.HP;
    
    [Header("Level UI (For Exp Type)")]
    public HSH_LEVELTEXTUI levelTextUI;

    [Header("Game Over UI")]
    public HSH_GameOverUI gameOverUI; // 체력이 다 까졌을 때 띄울 창


    [Header("Stats")]
    public float currentValue = 100f;
    public float maxValue = 100f;

    [Header("Colors")]
    public Color hpColor = Color.red;
    public Color ghostHpColor = new Color(0.6f, 0f, 1f); // 보라색
    public Color expColor = Color.yellow;

    [SerializeField] private HWJ_RuntimeStatusSystem statusSystem;
    private bool temp = true;

    private void Start()
    {
        // 만약 인스펙터에서 슬라이더를 넣지 않았다면, 자신에게 붙어있는 Slider 컴포넌트를 가져옴
        if (barSlider == null)
        {
            barSlider = GetComponent<Slider>();
        }

        // 플레이어 태그를 찾아서 자동으로 statusSystem을 연결합니다.
        if (statusSystem == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                statusSystem = player.GetComponent<HWJ_RuntimeStatusSystem>();
            }
        }
        
        UpdateColor();
        UpdateSlider();
    }

#if UNITY_EDITOR
    // 에디터에서 값을 변경할 때 즉시 반영되도록 함
    private void OnValidate()
    {
        UpdateColor();
        UpdateSlider();
    }
#endif

    private void Update()
    {

        // 플레이어 태그를 찾아서 자동으로 statusSystem을 연결합니다.
        if (statusSystem == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                statusSystem = player.GetComponent<HWJ_RuntimeStatusSystem>();
            }
        }
        // 씬 이동이나 다른 스크립트에서 체력을 변경했을 때도 실시간으로 반영되도록 Update에서 값을 확인합니다.
        CheckState();
        UpdateSlider();
    }

    // 나중에 외부에서 값을 받아올 때 사용하는 함수
    public void SetValues(float current, float max)
    {
        currentValue = current;
        maxValue = max;
        
        CheckState();
        UpdateSlider();
    }

    // 값 증가
    public void IncreaseValue(float amount)
    {
        currentValue += amount;
        CheckState();
        if (currentValue > maxValue)
        {
            currentValue = maxValue;
        }
        UpdateSlider();
    }

    // 값 감소
    public void DecreaseValue(float amount)
    {
        if (statusSystem != null)
        {
            if(currentType == BarType.HP){
                statusSystem.ApplyDamage(amount);
            }
            
        }
        else
        {
            currentValue -= amount;
        }
        

        CheckState();
        UpdateSlider();
    }

    private bool hasTriggeredGameOver = false;

    // HP가 다 달게된다면(0 이하) GhostHP로 변경하거나, Exp가 꽉 차면 레벨업
    private void CheckState()
    {
        // 1. 현재 연동된 데이터(체력/경험치)를 가져옵니다.
        if (statusSystem != null)
        {
            if (currentType == BarType.HP || currentType == BarType.GhostHP)
            {
                currentValue = statusSystem.CurrentHp;
                maxValue = statusSystem.MaxHp;
                
            }
            // TODO: 나중에 경험치(Exp) 시스템이 추가된다면 이 곳 주석을 해제하고 연동하세요.
            // else if (currentType == BarType.Exp)
            // {
            //     currentValue = ExpSystem.CurrentExp;
            //     maxValue = ExpSystem.MaxExp;
            // }
        }
        
        // 2. 값에 따른 상태 변화(게임오버, 레벨업 등)를 처리합니다.
        if (currentType == BarType.HP && currentValue <= 0 && maxValue > 0)
        {
            currentType = BarType.GhostHP;
            UpdateColor();
            Debug.Log("HP가 모두 닳아서 GhostHP 타입으로 변경되었습니다!");
        }
        else if(currentType == BarType.GhostHP && currentValue <= 0 && temp == true){
            
            currentValue = 100;
            statusSystem.Heal(100);

            temp = false;
            
        }
        else if (currentType == BarType.GhostHP && currentValue <= 0 && maxValue > 0)
        {
            
            if (!hasTriggeredGameOver)
            {
                hasTriggeredGameOver = true;
                Debug.Log("GhostHP가 모두 닳았습니다! 게임 오버!");
                
                if (gameOverUI != null)
                {
                    gameOverUI.ShowGameOver();
                }
            }
        }
        else if (currentType == BarType.Exp && currentValue >= maxValue && maxValue > 0)
        {
            // 경험치가 가득 찼으므로 레벨업!
            if (levelTextUI != null)
            {
                levelTextUI.LevelUp();
            }

            // 남은 초과 경험치를 이월
            currentValue -= maxValue;
            if (currentValue < 0) currentValue = 0;

            // TODO: 레벨업 시 다음 레벨의 요구 경험치(maxValue)를 증가시킬 수 있습니다.
            
            Debug.Log("경험치가 가득 차서 레벨업을 진행합니다!");
        }
        else if (currentValue < 0)
        {
            currentValue = 0;
        }
    }

    // 현재값과 최대값을 기준으로 나눠서 슬라이더의 값을 업데이트 (0 ~ 1)
    private void UpdateSlider()
    {
        if (barSlider != null && maxValue > 0)
        {
            barSlider.value = currentValue / maxValue;
        }
    }

    // 타입에 맞춰 슬라이더 Fill 이미지 색상 변경
    private void UpdateColor()
    {
        if (barSlider != null && barSlider.fillRect != null)
        {
            Image fillImage = barSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                switch (currentType)
                {
                    case BarType.HP:
                        fillImage.color = hpColor;
                        break;
                    case BarType.GhostHP:
                        fillImage.color = ghostHpColor;
                        break;
                    case BarType.Exp:
                        fillImage.color = expColor;
                        break;
                }
            }
        }
    }
}
