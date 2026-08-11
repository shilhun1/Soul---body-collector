using System.Collections.Generic;
using UnityEngine;

namespace HSH.Gimmick
{
    /// <summary>
    /// 불꽃벽(Flame Wall) 기믹 컴포넌트입니다.
    /// 
    /// [특징]
    /// 1. 육체(Body/Possessed) 상태: 물리 벽으로 동작하여 플레이어가 통과할 수 없고 닿을 시 데미지(및 넉백)를 입습니다.
    /// 2. 영혼(Soul/Spirit) 상태: HSH_Soulpass와 연동되어 통과가 가능하지만, 불꽃벽 영역 내에 있는 동안 주기적으로 데미지를 입습니다.
    /// 
    /// [사용법]
    /// 1. 불꽃벽 오브젝트에 HSH_FlameWall 컴포넌트를 추가합니다. (HSH_Soulpass가 자동 추가됨)
    /// 2. 기본 물리 충돌용 Collider2D 또는 영역 감지용 Trigger Collider2D를 추가하여 사용합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(HSH_Soulpass))]
    public class HSH_FlameWall : MonoBehaviour
    {
        [Header("데미지 설정")]
        [Tooltip("불꽃벽에 닿거나 내부에 있을 때 피격 데미지")]
        [SerializeField] private float damage = 10f;

        [Tooltip("데미지 재적용 주기 (초)")]
        [SerializeField] private float damageInterval = 0.5f;

        [Tooltip("육체(Body) 상태일 때 데미지 적용 여부")]
        [SerializeField] private bool damageInBodyState = true;

        [Tooltip("영혼(Soul) 상태일 때 통과 중 데미지 적용 여부")]
        [SerializeField] private bool damageInSoulState = true;

        [Tooltip("육체 상태에서 부딪혔을 때 밀쳐내는 넉백 적용 여부")]
        [SerializeField] private bool applyKnockbackInBodyState = true;

        [Tooltip("넉백 세기")]
        [SerializeField] private float knockbackPower = 6f;

        [Header("시각 및 사운드 연출 (선택)")]
        [Tooltip("불꽃 이펙트 (파티클)")]
        [SerializeField] private ParticleSystem flameParticle;

        [Tooltip("피격 시 생성할 타격 이펙트 프리팹")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Tooltip("피격 사운드 (AudioClip)")]
        [SerializeField] private AudioClip damageSfx;

        [Tooltip("오디오 소스")]
        [SerializeField] private AudioSource audioSource;

        // 대상별 마지막 피격 시간 기록 (중복/주기적 데미지 제어)
        private readonly Dictionary<GameObject, float> lastDamageTimes = new Dictionary<GameObject, float>();

        private Collider2D wallCollider;
        private HSH_Soulpass soulPassComponent;

        private void Awake()
        {
            wallCollider = GetComponent<Collider2D>();
            soulPassComponent = GetComponent<HSH_Soulpass>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (flameParticle == null) flameParticle = GetComponentInChildren<ParticleSystem>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleContact(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleContact(other.gameObject);
        }

        /// <summary>
        /// 불꽃벽과 접촉한 대상에 대해 영혼/육체 상태 판단 후 데미지를 처리합니다.
        /// </summary>
        private void HandleContact(GameObject hitObject)
        {
            if (hitObject == null) return;

            // Player 태그 또는 영혼/상태 매니저가 있는지 확인
            HWJ_SoulSystem soulSystem = hitObject.GetComponentInParent<HWJ_SoulSystem>();
            HWJ_RuntimeStatusSystem statusSystem = hitObject.GetComponentInParent<HWJ_RuntimeStatusSystem>();

            // 플레이어/타겟 식별을 위해 최상위 GameObject 추출
            GameObject rootTarget = statusSystem != null ? statusSystem.gameObject : (soulSystem != null ? soulSystem.gameObject : hitObject.transform.root.gameObject);

            if (!rootTarget.CompareTag("Player") && soulSystem == null && statusSystem == null)
            {
                return;
            }

            // 플레이어의 영혼 상태 여부 확인
            bool isSoul = false;
            if (soulSystem != null)
            {
                isSoul = soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul
                      || soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul
                      || soulSystem.IsSpiritExistence;
            }

            // 상태별 데미지 허용 여부 체크
            if (isSoul && !damageInSoulState) return;
            if (!isSoul && !damageInBodyState) return;

            // 데미지 주기(Cooldown) 체크
            if (lastDamageTimes.TryGetValue(rootTarget, out float lastTime))
            {
                if (Time.time < lastTime + damageInterval)
                {
                    return;
                }
            }

            // 데미지 적용
            lastDamageTimes[rootTarget] = Time.time;
            ApplyFlameDamage(rootTarget, isSoul);
        }

        private void ApplyFlameDamage(GameObject target, bool isSoul)
        {
            // 1. 체력 감소 (HWJ_RuntimeStatusSystem)
            HWJ_RuntimeStatusSystem statusSystem = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();
            if (statusSystem != null)
            {
                statusSystem.ApplyDamage(damage);
                Debug.Log($"[HSH_FlameWall] '{gameObject.name}' -> '{target.name}'에게 불꽃 데미지 {damage} 적용! (영혼 상태: {isSoul})");
            }

            // 2. 사운드 및 이펙트 연출
            if (audioSource != null && damageSfx != null)
            {
                audioSource.PlayOneShot(damageSfx);
            }

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, target.transform.position, Quaternion.identity);
            }

            // 3. 육체 상태일 경우 넉백 연출 (영혼 상태일 때는 넉백 없이 자연스럽게 통과하면서 데미지만 적용)
            if (!isSoul && applyKnockbackInBodyState)
            {
                ApplyKnockback(target);
            }
        }

        private void ApplyKnockback(GameObject target)
        {
            Vector2 knockbackDir = (target.transform.position - transform.position).normalized;
            if (knockbackDir == Vector2.zero) knockbackDir = Vector2.up;
            knockbackDir.y = Mathf.Max(knockbackDir.y, 0.3f);
            knockbackDir.Normalize();

            hys_Player_Hit playerHit = target.GetComponentInParent<hys_Player_Hit>();
            if (playerHit != null)
            {
                playerHit.ApplyKnockback(knockbackDir * knockbackPower);
            }
            else
            {
                HWJ_KnockbackSystem knockbackSystem = target.GetComponentInParent<HWJ_KnockbackSystem>();
                if (knockbackSystem == null)
                {
                    Rigidbody2D rb = target.GetComponentInParent<Rigidbody2D>();
                    if (rb != null) knockbackSystem = rb.gameObject.AddComponent<HWJ_KnockbackSystem>();
                }

                if (knockbackSystem != null)
                {
                    knockbackSystem.PlayKnockback(knockbackDir, knockbackPower, 0.2f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f); // 붉은 주황색
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
