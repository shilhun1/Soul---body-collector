using System.Collections;
using UnityEngine;

namespace HSH.Gimmick
{
    /// <summary>
    /// 플레이어가 도끼 폼(HWJ_WeaponType.Axe) 상태일 때 공격하거나 충돌 시 부서지는 파괴 오브젝트 기믹 컴포넌트입니다.
    /// 
    /// [작동 방식]
    /// 1. 플레이어가 도끼 폼(HWJ_WeaponType.Axe)으로 일반 공격(TakeDamage), 스킬 공격(TryActivateFromSkillHit) 또는 충돌 시 오브젝트가 파괴됩니다.
    /// 2. 도끼 폼이 아니거나 조건 미충족 시 부서지지 않으며 일반 물리 장애물로 동작합니다.
    /// 3. 다양한 파괴 모드(비활성화, 완전 파괴, 콜라이더 해제, 애니메이션 연출), 이펙트/사운드 및 일정 시간 후 자동 복구를 지원합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HSH_AxeBreakableObject : MonoBehaviour
    {
        public enum BreakMode
        {
            DisableObject,   // 게임오브젝트 비활성화 (SetActive(false))
            DestroyObject,   // 게임오브젝트 완전 삭제 (Destroy)
            DisableCollider, // 콜라이더 및 스프라이트만 비활성화 (통과 허용)
            AnimatorTrigger  // 애니메이션 트리거 실행 후 비활성화
        }

        [Header("요구 조건 설정")]
        [Tooltip("파괴에 요구되는 무기 타입 (기본: Axe/도끼)")]
        [SerializeField] private HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.Axe;

        [Tooltip("공격(일반 공격/스킬)에 맞았을 때만 파괴할지 여부 (꺼두면 도끼 폼으로 단순히 비벼도/충돌해도 파괴됨)")]
        [SerializeField] private bool requireAttackToBreak = true;

        [Tooltip("도끼 낙하 내려찍기 공격(Axe Dive Attack)에 의해서만 파괴되도록 엄격히 제한할지 여부")]
        [SerializeField] private bool requireAxeDiveAttackOnly = false;

        [Header("파괴 동작 설정")]
        [Tooltip("오브젝트 파괴 시 처리 모드")]
        [SerializeField] private BreakMode breakMode = BreakMode.DisableObject;

        [Header("애니메이션 연출 (AnimatorTrigger 모드 전용)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string breakTriggerName = "Break";
        [SerializeField] private float animationDelayBeforeDisable = 0.5f;

        [Header("시각 및 사운드 연출")]
        [Tooltip("부서질 때 생성할 파괴 이펙트 프리팹")]
        [SerializeField] private GameObject breakEffectPrefab;

        [Tooltip("파괴 효과음 (AudioClip)")]
        [SerializeField] private AudioClip breakSfx;

        [Tooltip("오디오 소스 (지정하지 않으면 자동 탐색 또는 PlayClipAtPoint 사용)")]
        [SerializeField] private AudioSource audioSource;

        [Header("재생성/복구 설정")]
        [Tooltip("체크 시 일정 시간 후 파괴된 오브젝트가 자동으로 복구됩니다.")]
        [SerializeField] private bool respawnAfterTime = false;
        [SerializeField] private float respawnDelay = 5f;

        private Collider2D[] objectColliders;
        private SpriteRenderer[] spriteRenderers;
        private bool isBroken = false;

        public bool IsBroken => isBroken;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            objectColliders = GetComponentsInChildren<Collider2D>();
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        #region Unity Physics Callbacks

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!requireAttackToBreak)
            {
                CheckAndBreak(collision.gameObject, isAttackHit: false);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!requireAttackToBreak)
            {
                CheckAndBreak(collision.gameObject, isAttackHit: false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckAndBreak(other.gameObject, isAttackHit: requireAttackToBreak);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckAndBreak(other.gameObject, isAttackHit: requireAttackToBreak);
        }

        #endregion

        #region Attack & Damage Hit Handlers

        /// <summary>
        /// hys_Player_Attack 등 SendMessageUpwards("TakeDamage") 호출 시 동작하는 메시지 수신기입니다.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isBroken) return;

            // 씬 내 플레이어 객체를 탐색하여 도끼 폼 및 공격 조건 검증
            GameObject playerObj = FindPlayerGameObject();
            if (playerObj != null)
            {
                CheckAndBreak(playerObj, isAttackHit: true);
            }
        }

        /// <summary>
        /// HWJ 스킬 히트 연동 콜백입니다.
        /// </summary>
        public bool TryActivateFromSkillHit(Transform sourceTransform, HWJ_SkillActionDataSO skillAction, out bool consumeHit)
        {
            consumeHit = false;
            if (isBroken || sourceTransform == null) return false;

            GameObject sourceObj = sourceTransform.gameObject;
            if (CheckWeaponAndAttackState(sourceObj, isAttackHit: true))
            {
                consumeHit = true;
                BreakObstacle();
                return true;
            }

            return false;
        }

        #endregion

        #region Core Validation & Break Logic

        /// <summary>
        /// 대상 오브젝트가 도끼 폼 조건 및 공격 상태를 충족하는지 검사 후 파괴를 실행합니다.
        /// </summary>

        private void CheckAndBreak(GameObject hitObject, bool isAttackHit)
        {
            if (isBroken || hitObject == null) return;

            if (CheckWeaponAndAttackState(hitObject, isAttackHit))
            {
                BreakObstacle();
            }
        }

        /// <summary>
        /// 무기 타입 및 공격 조건을 종합 검증합니다.
        /// </summary>

        private bool CheckWeaponAndAttackState(GameObject hitObject, bool isAttackHit)
        {
            if (hitObject == null) return false;

            Transform targetTransform = hitObject.transform;

            // 1. HWJ 유틸리티로 무기 타입 탐색
            HWJ_WeaponType currentWeapon = HWJ_WeaponGimmickActivatorUtility.ResolveSourceWeapon(targetTransform);

            // 2. HWJ_PossessionSystem 직접 검사 fallback
            if (currentWeapon == HWJ_WeaponType.None)
            {
                HWJ_PossessionSystem possessionSystem = hitObject.GetComponentInParent<HWJ_PossessionSystem>();
                if (possessionSystem == null)
                {
                    var soulSystem = hitObject.GetComponentInParent<HWJ_SoulSystem>();
                    if (soulSystem != null)
                    {
                        possessionSystem = soulSystem.GetComponent<HWJ_PossessionSystem>();
                    }
                }

                if (possessionSystem != null)
                {
                    currentWeapon = possessionSystem.CurrentWeaponType;
                }
            }

            // 3. hys_Player_Attack 또는 Animator로 Axe 폼 감지 fallback
            if (currentWeapon == HWJ_WeaponType.None)
            {
                Animator anim = hitObject.GetComponentInChildren<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null && anim.runtimeAnimatorController.name == "hys_Player_Axe")
                {
                    currentWeapon = HWJ_WeaponType.Axe;
                }
            }

            // 요구 무기 타입(Axe) 불일치 시 거부
            if (currentWeapon != requiredWeaponType)
            {
                return false;
            }

            // 4. Axe Dive (내려찍기) 전용 파괴 조건 설정 시 추가 검증
            if (requireAxeDiveAttackOnly)
            {
                hys_Player_Attack attackHandler = hitObject.GetComponentInParent<hys_Player_Attack>();
                if (attackHandler != null && attackHandler.IsAxeDiveAttacking)
                {
                    return true;
                }

                // Animator State로 추가 확인
                Animator anim = hitObject.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.IsName("Player_Axe_Plunge_Fall") ||
                        stateInfo.IsName("Player_Axe_Plunge_Land") ||
                        stateInfo.IsName("hys_Axe_Plunge_Fall") ||
                        stateInfo.IsName("hys_Axe_Plunge_Land"))
                    {
                        return true;
                    }
                }

                return false;
            }

            // 5. 공격 조건 검증 (requireAttackToBreak가 설정된 경우)
            if (requireAttackToBreak && !isAttackHit)
            {
                hys_Player_Attack attackHandler = hitObject.GetComponentInParent<hys_Player_Attack>();
                if (attackHandler != null && attackHandler.IsAxeDiveAttacking)
                {
                    return true;
                }

                // 단순 이동 접촉이면서 공격 타격 판정이 아닌 경우 거부
                return false;
            }

            return true;
        }

        /// <summary>
        /// 도끼 폼으로 오브젝트를 파괴하고 통과 처리합니다.
        /// </summary>
        public void BreakObstacle()
        {
            if (isBroken) return;
            isBroken = true;

            Debug.Log($"[HSH_AxeBreakableObject] ★ '{gameObject.name}' 오브젝트가 도끼 폼 공격에 의해 파괴되었습니다!");

            // 1. 파괴 사운드 재생
            if (breakSfx != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(breakSfx);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(breakSfx, transform.position);
                }
            }

            // 2. 파괴 이펙트 프리팹 생성
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            // 3. 지정된 파괴 모드별 처리
            switch (breakMode)
            {
                case BreakMode.DisableObject:
                    if (respawnAfterTime)
                    {
                        StartCoroutine(RespawnRoutine());
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                    break;

                case BreakMode.DestroyObject:
                    Destroy(gameObject, 0.1f);
                    break;

                case BreakMode.DisableCollider:
                    SetVisualAndColliderActive(false);
                    if (respawnAfterTime)
                    {
                        StartCoroutine(RespawnRoutine());
                    }
                    break;

                case BreakMode.AnimatorTrigger:
                    StartCoroutine(AnimatorBreakRoutine());
                    break;
            }
        }

        private IEnumerator AnimatorBreakRoutine()
        {
            if (animator != null && !string.IsNullOrEmpty(breakTriggerName))
            {
                animator.SetTrigger(breakTriggerName);
            }

            // 물리 통과가 즉시 가능하도록 콜라이더 해제
            SetCollidersEnabled(false);

            yield return new WaitForSeconds(animationDelayBeforeDisable);

            if (respawnAfterTime)
            {
                SetVisualAndColliderActive(false);
                StartCoroutine(RespawnRoutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            isBroken = false;
            gameObject.SetActive(true);
            SetVisualAndColliderActive(true);
            Debug.Log($"[HSH_AxeBreakableObject] '{gameObject.name}' 오브젝트가 복구되었습니다.");
        }

        #endregion

        #region Helper Methods

        private GameObject FindPlayerGameObject()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                var soul = FindAnyObjectByType<HWJ_SoulSystem>();
                if (soul != null) playerObj = soul.gameObject;
            }
            return playerObj;
        }

        private void SetCollidersEnabled(bool enabledState)
        {
            if (objectColliders != null)
            {
                foreach (var col in objectColliders)
                {
                    if (col != null) col.enabled = enabledState;
                }
            }
        }

        private void SetVisualAndColliderActive(bool activeState)
        {
            SetCollidersEnabled(activeState);

            if (spriteRenderers != null)
            {
                foreach (var sr in spriteRenderers)
                {
                    if (sr != null) sr.enabled = activeState;
                }
            }
        }

        #endregion
    }
}
