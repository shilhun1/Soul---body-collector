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
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private HWJ_LevelUpSystem levelUpSystem;
    private bool temp = true;

    private void Start()
    {
        // 만약 인스펙터에서 슬라이더를 넣지 않았다면, 자신에게 붙어있는 Slider 컴포넌트를 가져옴
        if (barSlider == null)
        {
            barSlider = GetComponent<Slider>();
        }

        FindPlayerSystems();
        
        UpdateColor();
        UpdateSlider();
    }

    private void FindPlayerSystems()
    {
        if (statusSystem == null || levelUpSystem == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (statusSystem == null) statusSystem = player.GetComponent<HWJ_RuntimeStatusSystem>();
                if (soulSystem == null) soulSystem = player.GetComponent<HWJ_SoulSystem>();
                if (levelUpSystem == null) levelUpSystem = player.GetComponent<HWJ_LevelUpSystem>();
            }
        }

        if (soulSystem == null && statusSystem != null)
        {
            soulSystem = statusSystem.GetComponent<HWJ_SoulSystem>();
        }

        if (levelUpSystem == null && statusSystem != null)
        {
            levelUpSystem = statusSystem.GetComponent<HWJ_LevelUpSystem>();
        }

        if (levelUpSystem == null)
        {
            if (HWJ_GameManager.Instance != null && HWJ_GameManager.Instance.PlayerLevel != null)
            {
                levelUpSystem = HWJ_GameManager.Instance.PlayerLevel;
            }
            else
            {
                levelUpSystem = Object.FindAnyObjectByType<HWJ_LevelUpSystem>();
            }
        }

        if (gameOverUI == null)
        {
            gameOverUI = FindFirstObjectByType<HSH_GameOverUI>();
        }
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
        FindPlayerSystems();

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
        if (currentType == BarType.Exp && levelUpSystem != null)
        {
            levelUpSystem.AddExperience((int)amount);
        }
        else
        {
            currentValue += amount;
            if (currentValue > maxValue)
            {
                currentValue = maxValue;
            }
        }
        CheckState();
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
            // 고스트 체력(GhostHP)은 함정 데미지 등 외부 요인으로 감소시키지 않음
        }
        else
        {
            if (currentType != BarType.GhostHP)
            {
                currentValue -= amount;
            }
        }
        

        CheckState();
        UpdateSlider();
    }

    private bool hasTriggeredGameOver = false;
    private bool hasTimerStarted = false;

    /// <summary>
    /// 매 프레임 호출되어 바의 현재값(currentValue)과 최대값(maxValue)을 갱신하고,
    /// 값에 따른 상태 변화(게임오버, 바 타입 전환, 레벨업 등)를 처리합니다.
    /// 
    /// [데이터 흐름 정리]
    /// - BarType.HP (빙의 상태의 체력):
    ///   → HWJ_RuntimeStatusSystem.CurrentHp / MaxHp 에서 직접 가져옵니다.
    ///   → 빙의체가 데미지를 받으면 내부적으로 부패도(Decay)가 깎이지만,
    ///     CurrentHp 프로퍼티가 현재 활성 상태의 체력을 자동으로 반환하므로 그대로 사용합니다.
    ///
    /// - BarType.GhostHP (영혼 상태의 잔여 시간):
    ///   → HWJ_SoulSystem.SoulDeadlineTimer 에서 남은 시간(초)을 가져옵니다.
    ///   → 최대 시간은 HWJ_PlayerTypeDataSO.SoulState.possessionDeadlineSeconds 이며,
    ///     HWJ_SoulSystem의 private 필드인 dataResolver를 리플렉션으로 접근하여 가져옵니다.
    ///   → 영혼 상태에서 시간이 0이 되면 SoulSystem이 Dead 상태로 전환하고,
    ///     이를 감지하여 게임오버 UI를 표시합니다.
    ///
    /// - BarType.Exp (경험치):
    ///   → HWJ_LevelUpSystem.CurrentExperience 및 TryGetRequiredExperienceForCurrentLevel 에서 경험치 정보와 요구 경험치를 가져옵니다.
    ///   → 레벨 정보는 HWJ_LevelUpSystem.CurrentLevel 에서 읽어와 levelTextUI 에 적용합니다.
    /// </summary>
    private void CheckState()
    {
        // ===== [1단계] 현재 바 타입에 맞는 실시간 데이터를 가져옵니다 =====
        if (statusSystem != null || levelUpSystem != null)
        {
            // [HP 바] 빙의 상태일 때의 체력
            if (currentType == BarType.HP && statusSystem != null)
            {
                currentValue = statusSystem.CurrentHp;
                maxValue = statusSystem.MaxHp;
            }
            // [GhostHP 바] 영혼 상태일 때의 시간 카운트다운
            else if (currentType == BarType.GhostHP && soulSystem != null)
            {
                Debug.Log("실행");
                Debug.Log("실행2");
                // 유저님의 요청대로 가장 심플하게 값만 대입합니다. (리플렉션 및 조건문 제거)
                maxValue = 10f;
                currentValue = soulSystem.SoulDeadlineTimer;
            }
            // [Exp 바] 경험치 (HWJ_LevelUpSystem 연동)
            else if (currentType == BarType.Exp && levelUpSystem != null)
            {
                currentValue = levelUpSystem.CurrentExperience;
                if (levelUpSystem.TryGetRequiredExperienceForCurrentLevel(out int requiredExp) && requiredExp > 0)
                {
                    maxValue = requiredExp;
                }
                else
                {
                    maxValue = Mathf.Max(1f, currentValue);
                }

                if (levelTextUI != null)
                {
                    levelTextUI.SetLevel(levelUpSystem.CurrentLevel);
                }
            }
        }
        
        // ===== [2단계] 게임오버 조건을 확인합니다 =====
        bool isGameOver = false;

        // 조건 1: SoulSystem이 Dead 상태 
        if (soulSystem != null && soulSystem.CurrentState == HWJ_SoulRuntimeState.Dead)
        {
            isGameOver = true;
        }

        // 조건 2: UI에서 카운트다운한 영혼 시간이 다 됨
        if (currentType == BarType.GhostHP && hasTimerStarted && currentValue <= 0)
        {
            isGameOver = true;
            if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Dead)
            {
                // UI 타이머가 끝났을 때 HWJ 시스템에도 Dead 상태를 강제로 알리기 위해 리플렉션 호출
                var method = typeof(HWJ_SoulSystem).GetMethod("EnterDeadState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(soulSystem, null);
                }
            }
        }
        else if (currentType != BarType.GhostHP)
        {
            // 영혼 상태가 아닐 때는 타이머 시작 플래그를 초기화
            hasTimerStarted = false;
        }

        // ===== [3단계] 게임오버 처리 또는 상태 전환을 수행합니다 =====
        if (isGameOver)
        {
            // hasTriggeredGameOver 플래그로 게임오버 UI가 중복 호출되는 것을 방지합니다.
            if (!hasTriggeredGameOver)
            {
                hasTriggeredGameOver = true;
                Debug.Log("GhostHP가 모두 닳았습니다! 게임 오버!");

                if (gameOverUI == null)
                {
                    gameOverUI = FindFirstObjectByType<HSH_GameOverUI>();
                }

                if (gameOverUI != null)
                {
                    gameOverUI.ShowGameOver();
                }
                else
                {
                    Debug.LogError("GameOverUI를 찾을 수 없습니다!");
                }
            }
        }
        else
        {
            // 게임오버가 아닌 상태에서는 플래그를 초기화
            hasTriggeredGameOver = false;
        }

        // 최종 보정: 값이 음수가 되지 않도록 0으로 클램핑합니다.
        if (currentValue < 0)
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
