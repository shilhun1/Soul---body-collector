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
    private HWJ_SoulSystem soulSystem;
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
                soulSystem = player.GetComponent<HWJ_SoulSystem>();
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
                soulSystem = player.GetComponent<HWJ_SoulSystem>();
            }
        }

        // 플레이어의 Soul 상태에 맞춰 체력바 타입 자동 변경 (경험치 바는 제외)
        if (soulSystem != null && currentType != BarType.Exp)
        {
            if (soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul)
            {
                if (currentType != BarType.GhostHP)
                {
                    currentType = BarType.GhostHP;
                    UpdateColor();
                }
            }
            else // 영혼 상태가 아닐 때 (기본 몸 또는 빙의 중일 때)
            {
                if (currentType == BarType.GhostHP)
                {
                    currentType = BarType.HP;
                    UpdateColor();
                }
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
            if (currentType == BarType.HP)
            {
                statusSystem.ApplyDamage(amount);
            }
            else if (currentType == BarType.GhostHP)
            {
                if (soulSystem != null)
                {
                    // HWJ 스크립트를 수정할 수 없으므로 리플렉션으로 타이머를 깎습니다.
                    var field = typeof(HWJ_SoulSystem).GetField("soulDeadlineTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        float currentTimer = (float)field.GetValue(soulSystem);
                        float newTimer = currentTimer - amount;

                        if (newTimer <= 0f)
                        {
                            field.SetValue(soulSystem, 0f);
                            soulSystem.EnterDeadState();
                        }
                        else
                        {
                            field.SetValue(soulSystem, newTimer);
                        }
                    }
                }
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
        // 1. 현재 연동된 데이터(체력/경험치/영혼시간)를 가져옵니다.
        if (statusSystem != null)
        {
            if (currentType == BarType.HP)
            {
                if (HWJ_GameAccess.HasManager)
                {
                    // 게임 매니저에서 savedBodyDecayValue를 가져와서 UI의 현재 체력으로 반영합니다.
                    var decayField = typeof(HWJ_GameManager).GetField("savedBodyDecayValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (decayField != null)
                    {
                        currentValue = (float)decayField.GetValue(HWJ_GameAccess.Manager);
                    }
                    else
                    {
                        currentValue = statusSystem.CurrentHp;
                    }

                    // 최대 체력(MaxHp) 대신 최대 부패 수치(maxDecayValue)를 가져옵니다.
                    if (soulSystem != null)
                    {
                        var resolverField = typeof(HWJ_SoulSystem).GetField("dataResolver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (resolverField != null)
                        {
                            var resolver = (HWJ_RootObjectDataResolver)resolverField.GetValue(soulSystem);
                            if (resolver != null && resolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData) && playerData.BodyDecay != null)
                            {
                                maxValue = playerData.BodyDecay.maxDecayValue;
                            }
                            else
                            {
                                maxValue = statusSystem.MaxHp;
                            }
                        }
                    }
                    else
                    {
                        maxValue = statusSystem.MaxHp;
                    }
                }
                else
                {
                    currentValue = statusSystem.CurrentHp;
                    maxValue = statusSystem.MaxHp;
                }
            }
            else if (currentType == BarType.GhostHP)
            {
                if (soulSystem != null)
                {
                    currentValue = soulSystem.SoulDeadlineTimer;
                    maxValue = 10f; // 기본값
                    
                    // HWJ 코드를 수정할 수 없으므로 리플렉션으로 dataResolver에 접근하여 최대 시간을 가져옵니다.
                    var resolverField = typeof(HWJ_SoulSystem).GetField("dataResolver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (resolverField != null)
                    {
                        var resolver = (HWJ_RootObjectDataResolver)resolverField.GetValue(soulSystem);
                        if (resolver != null && resolver.TryGetTypeData(out HWJ_PlayerTypeDataSO playerData))
                        {
                            maxValue = playerData.SoulState.possessionDeadlineSeconds;
                        }
                    }
                }
            }
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
            if (statusSystem != null)
            {
                statusSystem.Heal(100);
            }
            Debug.Log("temp  true");
            temp = false;
            Debug.Log("temp  false");
            
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
