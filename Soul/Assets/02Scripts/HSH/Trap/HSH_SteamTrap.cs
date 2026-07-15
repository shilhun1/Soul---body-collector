using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HSH_SteamTrap : MonoBehaviour
{
    [Header("수증기 함정 설정")]
    public float damage = 15f; 
    public float activeDuration = 2f; // 수증기가 뿜어져 나오는 시간
    public float inactiveDuration = 3f; // 수증기가 쉬는 시간 (간헐적)
    public float damageCooldown = 1f; // 같은 대상이 다시 데미지를 입기까지 걸리는 시간
    [Tooltip("수증기에 맞았을 때 위로 튕겨 올라가는 힘입니다. 숫자가 클수록 높이 뜹니다.")]
    public float knockbackPower = 25f; 

    private bool isActive = false;
    private Collider2D steamCollider;
    private SpriteRenderer spriteRenderer; 
    
    [Header("시각 효과 (파티클)")]
    [Tooltip("수증기가 뿜어져 나오는 시각 효과(Particle System)를 연결하세요.")]
    public ParticleSystem steamParticles;

    private Dictionary<GameObject, float> nextDamageTime = new Dictionary<GameObject, float>();

    private void Start()
    {
        steamCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (steamCollider != null) steamCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.2f); 
        
        if (steamParticles != null)
        {
            steamParticles.Stop(); // 시작 시 파티클 중지
        }

        StartCoroutine(SteamRoutine());
    }

    private IEnumerator SteamRoutine()
    {
        while (true)
        {
            // 쉬는 시간
            yield return new WaitForSeconds(inactiveDuration);

            // 수증기 분출
            isActive = true;
            if (steamCollider != null) steamCollider.enabled = true;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.8f);
            
            if (steamParticles != null) 
            {
                steamParticles.Play(); // 파티클 재생 (뿜어져 나옴)
            }

            yield return new WaitForSeconds(activeDuration);

            // 다시 쉼
            isActive = false;
            if (steamCollider != null) steamCollider.enabled = false;
            if (spriteRenderer != null) spriteRenderer.color = new Color(1, 1, 1, 0.2f);
            
            if (steamParticles != null) 
            {
                steamParticles.Stop(); // 파티클 중지
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isActive && (collision.CompareTag("Player") || collision.CompareTag("Enemy")))
        {
            GameObject target = collision.gameObject;

            // 쿨타임 체크 (한 번 맞고 나서 damageCooldown 시간이 지나야 다시 맞음)
            if (!nextDamageTime.ContainsKey(target) || Time.time >= nextDamageTime[target])
            {
                ApplyDamageAndKnockback(target);
                nextDamageTime[target] = Time.time + damageCooldown; // 쿨타임 갱신
            }
        }
    }

    private void ApplyDamageAndKnockback(GameObject target)
    {
        // 영혼 상태일 경우 데미지 및 넉백 무시
        HWJ_SoulSystem soulSystem = target.GetComponentInParent<HWJ_SoulSystem>();
        if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
        {
            return;
        }

        // 1. 데미지 처리
        HWJ_RuntimeStatusSystem statusSystem = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();
        if (statusSystem != null)
        {
            statusSystem.ApplyDamage(damage);
            Debug.Log($"[{gameObject.name}] {target.name}에게 데미지: {damage}");
        }

        // 2. 넉백 처리 (수증기는 무조건 위로 솟구치도록)
        // 옆으로 밀리는 현상을 방지하기 위해 방향을 완전한 위쪽(Up)으로 고정합니다.
        Vector2 knockbackDir = Vector2.up; 

        hys_Player_Hit playerHit = target.GetComponentInParent<hys_Player_Hit>();
        if (playerHit != null)
        {
            // 플레이어는 자체 넉백 시스템에 전달
            playerHit.ApplyKnockback(knockbackDir * knockbackPower);
        }
        else
        {
            // 적의 범용 넉백 시스템은 기본적으로 '수직(Y축) 넉백 무시' 설정이 되어 있어 옆으로만 밀리게 됩니다.
            // 이를 뚫고 강제로 위로 띄우기 위해 Rigidbody의 Y축 속도를 직접 조작합니다.
            HWJ_KnockbackSystem knockbackSystem = target.GetComponentInParent<HWJ_KnockbackSystem>();
            if (knockbackSystem == null)
            {
                Rigidbody2D parentRb = target.GetComponentInParent<Rigidbody2D>();
                knockbackSystem = (parentRb != null) ? parentRb.gameObject.AddComponent<HWJ_KnockbackSystem>() : target.AddComponent<HWJ_KnockbackSystem>();
            }
            // X축 방향 넉백(0)을 전달해서 기절 상태만 유발
            knockbackSystem.PlayKnockback(knockbackDir, knockbackPower, 0.25f);
            
            // 실제 위로 띄우는 힘 적용
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, knockbackPower);
            }
        }
    }
}
