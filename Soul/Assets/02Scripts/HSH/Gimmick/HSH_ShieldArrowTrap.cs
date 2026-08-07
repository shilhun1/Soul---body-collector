using UnityEngine;

namespace HSH.Gimmick
{
    /// <summary>
    /// 플레이어를 향해 화살/투사체를 발사하는 화살 함정 스크립트입니다.
    /// 플레이어가 방패(Shield) 폼일 경우 화살을 막아 데미지를 입지 않으며,
    /// 방패 폼이 아닐 경우에는 화살에 맞아 데미지 및 넉백을 받습니다.
    /// </summary>
    public class HSH_ShieldArrowTrap : MonoBehaviour
    {
        [Header("화살 투사체(Projectile) 설정")]
        [Tooltip("화살 이동 속도")]
        [SerializeField] private float speed = 10f;
        [Tooltip("화살 데미지 수치")]
        [SerializeField] private float damage = 15f;
        [Tooltip("화살 넉백 힘")]
        [SerializeField] private float knockbackPower = 8f;
        [Tooltip("화살 자동 파괴 시간 (초)")]
        [SerializeField] private float lifeTime = 5f;
        [Tooltip("화살이 날아가는 방향 (기본: 오른쪽)")]
        [SerializeField] private Vector2 direction = Vector2.right;

        [Header("발사기(Spawner) 설정")]
        [Tooltip("체크하면 제자리에서 주기적/감지 시 화살을 발사하는 함정이 됩니다.")]
        [SerializeField] private bool isSpawner = false;
        [Tooltip("발사할 화살 프리팹")]
        [SerializeField] private GameObject arrowPrefab;
        [Tooltip("화살 발사 간격 (초)")]
        [SerializeField] private float fireInterval = 2f;

        [Header("플레이어 감지 설정 (발사기 전용)")]
        [Tooltip("전방 레이캐스트 감지 시에만 발사 여부")]
        [SerializeField] private bool detectPlayer = true;
        [Tooltip("플레이어 감지 거리")]
        [SerializeField] private float detectDistance = 12f;

        [Header("애니메이션 연출 (발사기 전용)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string fireTriggerName = "Fire";

        private float timer = 0f;

        private void Start()
        {
            if (animator == null) animator = GetComponent<Animator>();

            if (!isSpawner)
            {
                // 화살 투사체 모드인 경우 일정 시간 후 자동 삭제
                Destroy(gameObject, lifeTime);
            }
            else
            {
                timer = fireInterval; // 첫 시작 시 즉시 발사 준비
            }
        }

        private void Update()
        {
            if (isSpawner)
            {
                UpdateSpawner();
            }
            else
            {
                // 화살 투사체 이동
                transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);
            }
        }

        private void UpdateSpawner()
        {
            bool shouldFire = false;

            if (detectPlayer)
            {
                // 자기 자신 콜라이더 충돌 방지 및 레이어 마스크 제한 해제를 위해 RaycastAll 사용
                Vector2 rayOrigin = (Vector2)transform.position + direction.normalized * 0.3f;
                RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction.normalized, detectDistance);

                foreach (var hit in hits)
                {
                    if (hit.collider == null || hit.collider.gameObject == gameObject) continue;

                    // 플레이어 태그이거나 플레이어 관련 컴포넌트를 가진 경우 감지
                    if (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<HWJ_SoulSystem>() != null)
                    {
                        shouldFire = true;
                        break;
                    }
                }
            }
            else
            {
                shouldFire = true; // 무조건 주기적 발사
            }

            if (shouldFire)
            {
                timer += Time.deltaTime;
                if (timer >= fireInterval)
                {
                    timer = 0f;
                    FireArrow();
                }
            }
        }

        private void FireArrow()
        {
            if (animator != null && !string.IsNullOrEmpty(fireTriggerName))
            {
                animator.SetTrigger(fireTriggerName);
            }

            if (arrowPrefab != null)
            {
                Vector3 spawnPos = transform.position + (Vector3)(direction.normalized * 0.5f);
                GameObject arrowInstance = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
                HSH_ShieldArrowTrap arrowScript = arrowInstance.GetComponent<HSH_ShieldArrowTrap>();
                if (arrowScript != null)
                {
                    arrowScript.isSpawner = false;
                    arrowScript.direction = direction;
                    arrowScript.speed = speed;
                    arrowScript.damage = damage;
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 발사할 화살 프리팹(Arrow Prefab)이 지정되지 않았습니다! Inspector에서 지정해주세요.");
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isSpawner || collision == null) return;

            // 플레이어 충돌 처리
            if (collision.CompareTag("Player"))
            {
                HandlePlayerHit(collision.gameObject);
                Destroy(gameObject); // 충돌 시 화살 소멸
                return;
            }

            // 장애물이나 벽에 부딪혔을 때 화살 소멸
            if (!collision.isTrigger && !collision.CompareTag("Enemy"))
            {
                Destroy(gameObject);
            }
        }

        private void HandlePlayerHit(GameObject target)
        {
            var possessionSystem = target.GetComponentInParent<HWJ_PossessionSystem>();
            var soulSystem = target.GetComponentInParent<HWJ_SoulSystem>();

            // 1. 플레이어가 방패 폼(Shield Form)인지 확인
            bool isShieldForm = (possessionSystem != null && possessionSystem.HasActivePossessedBody && possessionSystem.CurrentWeaponType == HWJ_WeaponType.Shield);

            if (isShieldForm)
            {
                // 방패로 화살을 차단함 -> 데미지 입지 않음!
                Debug.Log($"[{gameObject.name}] 플레이어가 방패 폼으로 화살을 막아냈습니다!");
                return;
            }

            // 2. 영혼 상태인 경우 데미지 무시
            if (soulSystem != null && soulSystem.CurrentState != HWJ_SoulRuntimeState.Body)
            {
                return;
            }

            // 3. 방패 폼이 아닐 경우 피해 및 넉백 적용
            var statusSystem = target.GetComponentInParent<HWJ_RuntimeStatusSystem>();
            if (statusSystem != null)
            {
                statusSystem.ApplyDamage(damage);
                Debug.Log($"[{gameObject.name}] {target.name} 화살 적중! 데미지: {damage}");
            }

            // 넉백 처리
            Vector2 knockbackDir = direction.normalized;
            knockbackDir.y += 0.3f;
            var playerHit = target.GetComponentInParent<hys_Player_Hit>();
            if (playerHit != null)
            {
                playerHit.ApplyKnockback(knockbackDir.normalized * knockbackPower);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (isSpawner)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, direction.normalized * (detectPlayer ? detectDistance : 3f));
            }
        }
    }
}
