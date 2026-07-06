using UnityEngine;

public class HSH_GhostStat : MonoBehaviour
{
    [Header("Ghost Base Stats")]
    public StatData baseStats; // 인스펙터에서 설정할 기본 고스트 스탯
    
    [Header("Ghost Specific Settings")]
    public float ghostTimeLimit = 10f; // 영혼 상태 유지 시간
    public bool dieOnTimeout = true; // 시간 만료 시 사망(게임오버) 여부

    [Header("Ghost Timer State")]
    public float currentGhostTime = 0f;
    private bool isTimerRunning = false;
    private HSH_GhostMove ghostMove;
    private CharacterBase characterBase;
    private int originalHp;

    public StatData CurrentStats { get; private set; }

    void Awake()
    {
        InitializeStats();
        ghostMove = GetComponent<HSH_GhostMove>();
        characterBase = GetComponent<CharacterBase>();
    }

    void Update()
    {
        if (!isTimerRunning) return;

        // 무적 판정: 체력을 깎이지 않게 최대치로 유지 (완전 무적)
        if (characterBase != null && characterBase.currentStats != null)
        {
            if (characterBase.currentHp < characterBase.currentStats.maxHp)
            {
                characterBase.currentHp = characterBase.currentStats.maxHp;
            }
        }

        currentGhostTime += Time.deltaTime;
        if (currentGhostTime >= ghostTimeLimit)
        {
            isTimerRunning = false;
            HandleGhostTimeout(ghostMove);
        }
    }

    public void StartTimer()
    {
        currentGhostTime = 0f;
        isTimerRunning = true;
        
        if (characterBase != null)
        {
            originalHp = characterBase.currentHp;
            if (characterBase.currentStats != null)
            {
                characterBase.currentHp = characterBase.currentStats.maxHp;
            }
        }
    }

    public void StopTimer()
    {
        isTimerRunning = false;
        
        if (characterBase != null)
        {
            characterBase.currentHp = originalHp;
        }
    }

    // 스탯 초기화
    public void InitializeStats()
    {
        if (baseStats != null)
        {
            CurrentStats = baseStats.Clone();
        }
        else
        {
            // baseStats가 설정되지 않았을 경우의 기본값
            CurrentStats = new StatData();
            CurrentStats.moveSpeed = 5f;
            CurrentStats.maxHp = 1;
        }
    }

    // 스탯 조정 기능 (아이템, 능력치 증가 등에 활용)
    public void AddStatBonus(StatData bonus)
    {
        if (bonus != null && CurrentStats != null)
        {
            CurrentStats.Add(bonus);
        }
    }

    // 플레이어 스탯 등 다른 StatData를 통째로 덮어씌울 때 활용
    public void SetStats(StatData newStats)
    {
        if (newStats != null)
        {
            CurrentStats = newStats.Clone();
        }
    }

    // 유령 시간이 다 되었을 때 죽을지 살지 결정하여 처리하는 함수
    public void HandleGhostTimeout(HSH_GhostMove moveScript)
    {
        if (dieOnTimeout)
        {
            Debug.Log("영혼 상태 유지 시간 만료: 게임 오버");
            CharacterBase cb = GetComponent<CharacterBase>();
            
            if (cb != null)
            {
                cb.currentHp = 0;
                cb.Die();
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (moveScript != null)
            {
                moveScript.isGhost = false; // 중복 호출 방지
            }
        }
        else
        {
            Debug.Log("영혼 상태 유지 시간 만료: 영혼 상태 해제 (생존)");
            if (moveScript != null)
            {
                moveScript.ExitGhostState();
            }
        }
    }
}
