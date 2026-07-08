using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(hys_Player_State))]
public class hys_Player_Health : MonoBehaviour
{
    [Header("Health")]
    // 플레이어 최대 체력입니다.
    [SerializeField] private float maxHp = 10f;

    // 피격 상태가 유지되는 시간입니다.
    [SerializeField] private float hitStateTime = 0.2f;

    // 피격 직후 연속 피해를 막는 무적 시간입니다.
    [SerializeField] private float invincibleTimeAfterHit = 0.5f;

    [Header("Death To Soul")]
    // 사망 애니메이션이 재생될 시간을 기다린 뒤 소울 상태로 전환합니다.
    [SerializeField] private HWJ_SoulSystem soulSystem;
    [SerializeField] private bool enterSoulStateAfterDeath = true;
    [SerializeField] private float deathAnimationSeconds = 1f;
    [SerializeField] private bool refillSoulHpAfterDeath = true;
    [SerializeField] private bool resetHysHealthAfterSoulStateStart = true;

    [Header("Debug")]
    // 체력 변화와 사망 로그를 출력할지 정합니다.
    [SerializeField] private bool useDebugLog = true;

    private hys_Player_State playerState;
    private Coroutine hitRoutine;
    private Coroutine deathRoutine;
    private float currentHp;
    private float invincibleEndTime;

    // UI, 이펙트, 게임오버 시스템이 나중에 연결할 수 있게 열어둔 이벤트입니다.
    public event Action<float> Damaged;
    public event Action Dead;

    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => playerState != null && playerState.CurrentState == hys_PlayerState.Dead;
    public bool IsInvincible => Time.time < invincibleEndTime;

    private void Awake()
    {
        // 시작 시 최대 체력으로 초기화합니다.
        playerState = GetComponent<hys_Player_State>();
        soulSystem = soulSystem != null ? soulSystem : GetComponent<HWJ_SoulSystem>();
        currentHp = maxHp;
    }

    // 적 공격 스크립트가 SendMessageUpwards로 호출하는 데미지 함수입니다.
    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvincible || damage <= 0f)
        {
            return;
        }

        currentHp = Mathf.Max(0f, currentHp - damage);
        invincibleEndTime = Time.time + invincibleTimeAfterHit;
        Damaged?.Invoke(damage);

        if (useDebugLog)
        {
            Debug.Log($"[Player Health] Damage: {damage} / HP: {currentHp}/{maxHp}", this);
        }

        if (currentHp <= 0f)
        {
            Die();
            return;
        }

        StartHitState();
    }

    // 회복 아이템이나 시스템이 생기면 사용할 수 있는 함수입니다.
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    // 테스트나 리스폰용으로 체력을 다시 채우고 상태를 Idle로 돌립니다.
    public void ResetHealth()
    {
        currentHp = maxHp;
        invincibleEndTime = 0f;

        if (playerState != null && playerState.CurrentState == hys_PlayerState.Dead)
        {
            playerState.ResetFromDead(hys_PlayerState.Idle);
        }
    }

    private void StartHitState()
    {
        if (playerState == null)
        {
            return;
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        hitRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        playerState.SetState(hys_PlayerState.Hit);

        yield return new WaitForSeconds(hitStateTime);

        if (!IsDead && playerState.CurrentState == hys_PlayerState.Hit)
        {
            playerState.SetState(hys_PlayerState.Idle);
        }

        hitRoutine = null;
    }

    private void Die()
    {
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        if (playerState != null)
        {
            playerState.SetDead();
        }

        Dead?.Invoke();

        if (useDebugLog)
        {
            Debug.Log("[Player Health] Dead", this);
        }

        if (enterSoulStateAfterDeath && deathRoutine == null)
        {
            deathRoutine = StartCoroutine(DeathToSoulRoutine());
        }
    }

    private IEnumerator DeathToSoulRoutine()
    {
        yield return new WaitForSeconds(deathAnimationSeconds);

        if (soulSystem != null)
        {
            soulSystem.EnterSoulState(refillSoulHpAfterDeath);
        }

        if (resetHysHealthAfterSoulStateStart)
        {
            ResetHealth();
        }

        deathRoutine = null;
    }
}
