using System.Collections;
using UnityEngine;

namespace HSH.Gimmick
{
    /// <summary>
    /// 창(Lance/Spear) 폼 상태에서 특정 돌진 스킬(1번 스킬: PiercingDrive, 4번 스킬: BurstLance)로 부술 수 있는 파괴 벽/오브젝트 기믹 컴포넌트입니다.
    /// 
    /// [작동 방식]
    /// 1. 창 폼(HWJ_WeaponType.Lance)을 보유한 플레이어가 1번 스킬(PiercingDrive) 또는 4번 스킬(BurstLance)로 돌진하며 충돌 시 오브젝트가 파괴되어 통과 가능해집니다.
    /// 2. 기본 대쉬기(Shift/Space) 또는 조건 미충족 시 일반 물리 벽으로 작동하여 통과할 수 없습니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HSH_SpearDashBreakable : MonoBehaviour
    {
        public enum BreakMode
        {
            DisableObject,   // 게임오브젝트 비활성화
            DestroyObject,   // 게임오브젝트 완전 파괴
            DisableCollider, // 콜라이더 및 스프라이트만 비활성화 (통과 허용)
            AnimatorTrigger  // 애니메이션 파라미터 실행 후 비활성화
        }

        [Header("파괴 모드 설정")]
        [Tooltip("오브젝트가 부서질 때 동작 방식")]
        [SerializeField] private BreakMode breakMode = BreakMode.DisableObject;

        [Tooltip("요구 무기 타입 (기본: Lance/Spear 창)")]
        [SerializeField] private HWJ_WeaponType requiredWeaponType = HWJ_WeaponType.Lance;

        [Header("스킬 돌진 파괴 조건 설정")]
        [Tooltip("기본 대쉬기가 아닌 1번 스킬(PiercingDrive) 또는 4번 스킬(BurstLance) 돌진 시에만 파괴되도록 제한할지 여부")]
        [SerializeField] private bool requireSkill1Or4Only = true;

        [Tooltip("스킬 1 식별 키워드 (예: PiercingDrive, 1202)")]
        [SerializeField] private string[] skill1Keywords = new string[] { "piercingdrive", "piercing_drive", "1202", "chargethrust" };

        [Tooltip("스킬 4 식별 키워드 (예: BurstLance, 1205)")]
        [SerializeField] private string[] skill4Keywords = new string[] { "burstlance", "burst_lance", "1205" };

        [Header("애니메이션 설정 (AnimatorTrigger 모드 전용)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string breakTriggerName = "Break";
        [SerializeField] private float animationDelayBeforeDisable = 0.5f;

        [Header("시각 및 사운드 연출")]
        [Tooltip("부서질 때 생성할 파괴 이펙트 프리팹")]
        [SerializeField] private GameObject breakEffectPrefab;

        [Tooltip("파괴 음향 효과 (AudioClip)")]
        [SerializeField] private AudioClip breakSfx;

        [Tooltip("오디오 소스")]
        [SerializeField] private AudioSource audioSource;

        [Header("재생성 설정 (선택)")]
        [Tooltip("체크 시 일정 시간 후 파괴된 오브젝트가 복구됩니다.")]
        [SerializeField] private bool respawnAfterTime = false;
        [SerializeField] private float respawnDelay = 5f;

        private Collider2D[] objectColliders;
        private SpriteRenderer[] spriteRenderers;
        private bool isBroken = false;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            objectColliders = GetComponentsInChildren<Collider2D>();
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckAndBreak(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            CheckAndBreak(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckAndBreak(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckAndBreak(other.gameObject);
        }

        /// <summary>
        /// HWJ 스킬 히트 콜백 지원
        /// </summary>
        public bool TryActivateFromSkillHit(Transform sourceTransform, HWJ_SkillActionDataSO skillAction, out bool consumeHit)
        {
            consumeHit = false;
            if (isBroken || sourceTransform == null) return false;

            GameObject hitObject = sourceTransform.gameObject;
            HWJ_WeaponType currentWeapon = HWJ_WeaponGimmickActivatorUtility.ResolveSourceWeapon(sourceTransform);
            if (currentWeapon != requiredWeaponType) return false;

            if (requireSkill1Or4Only && !IsSpearSkill1Or4Active(hitObject, skillAction))
            {
                return false;
            }

            consumeHit = true;
            BreakObstacle();
            return true;
        }

        /// <summary>
        /// 충돌 대상이 창 폼 및 1번/4번 스킬 돌진 중인지 확인하고 파괴를 실행합니다.
        /// </summary>
        private void CheckAndBreak(GameObject hitObject)
        {
            if (isBroken || hitObject == null) return;

            // 1. HWJ 무기 타입 검사 (HWJ_WeaponGimmickActivatorUtility / HWJ_PossessionSystem)
            HWJ_WeaponType currentWeapon = HWJ_WeaponGimmickActivatorUtility.ResolveSourceWeapon(hitObject.transform);

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

            if (currentWeapon != requiredWeaponType)
            {
                return;
            }

            // 2. 1번 또는 4번 스킬 돌진 상태 확인 (기본 대쉬기로는 파괴 안 됨)
            if (requireSkill1Or4Only)
            {
                if (!IsSpearSkill1Or4Active(hitObject))
                {
                    return;
                }
            }

            // 3. 창 폼 + 1번/4번 스킬 돌진 조건 충족! 파괴 실행
            BreakObstacle();
        }

        /// <summary>
        /// 대상 플레이어 또는 스킬 데이터가 1번 스킬(PiercingDrive) 또는 4번 스킬(BurstLance)인지 감지합니다.
        /// </summary>
        private bool IsSpearSkill1Or4Active(GameObject playerObject, HWJ_SkillActionDataSO skillAction = null)
        {
            // 1. 전달된 SkillActionDataSO 직관적 확인
            if (skillAction != null)
            {
                if (MatchesKeyword(skillAction.SkillActionId) || MatchesKeyword(skillAction.MotionKey))
                {
                    return true;
                }
            }

            if (playerObject == null) return false;

            // 2. 플레이어 Animator의 현재 재생 중인 스테이트, 클립 및 트리거 상태 검사
            Animator[] animators = playerObject.GetComponentsInChildren<Animator>();
            foreach (var anim in animators)
            {
                if (anim == null || !anim.enabled) continue;

                // (1) 현재 재생 중인 State Name 및 Clip Info 체크
                for (int layer = 0; layer < anim.layerCount; layer++)
                {
                    AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(layer);
                    if (IsStateMatchingSkill1Or4(stateInfo))
                    {
                        return true;
                    }

                    AnimatorClipInfo[] clipInfos = anim.GetCurrentAnimatorClipInfo(layer);
                    foreach (var clipInfo in clipInfos)
                    {
                        if (clipInfo.clip != null && MatchesKeyword(clipInfo.clip.name))
                        {
                            return true;
                        }
                    }
                }

                // (2) Animator 파라미터 Trigger 체크
                foreach (var param in anim.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        if (MatchesKeyword(param.name))
                        {
                            if (anim.GetBool(param.nameHash))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // 3. HWJ_SkillActionSystem 및 HWJ_PlayerMovementSystem의 대시 스킬 실행 판정
            HWJ_PlayerMovementSystem hwjMovement = playerObject.GetComponentInParent<HWJ_PlayerMovementSystem>();
            if (hwjMovement != null && hwjMovement.IsDashing)
            {
                foreach (var anim in animators)
                {
                    if (anim == null) continue;
                    for (int layer = 0; layer < anim.layerCount; layer++)
                    {
                        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(layer);
                        if (IsStateMatchingSkill1Or4(info)) return true;

                        AnimatorClipInfo[] clipInfos = anim.GetCurrentAnimatorClipInfo(layer);
                        foreach (var clipInfo in clipInfos)
                        {
                            if (clipInfo.clip != null && MatchesKeyword(clipInfo.clip.name)) return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsStateMatchingSkill1Or4(AnimatorStateInfo stateInfo)
        {
            return stateInfo.IsName("PiercingDrive") ||
                   stateInfo.IsName("BurstLance") ||
                   stateInfo.IsName("PlayerSkill_Lance_PiercingDrive") ||
                   stateInfo.IsName("PlayerSkill_Lance_BurstLance") ||
                   stateInfo.IsName("Lance_ChargeThrust") ||
                   stateInfo.IsName("hys_Lance_Attack1") ||
                   stateInfo.IsName("hys_Lance_Attack2");
        }

        private bool MatchesKeyword(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            string lower = input.ToLowerInvariant();

            if (skill1Keywords != null)
            {
                foreach (var kw in skill1Keywords)
                {
                    if (!string.IsNullOrEmpty(kw) && lower.Contains(kw.ToLowerInvariant())) return true;
                }
            }

            if (skill4Keywords != null)
            {
                foreach (var kw in skill4Keywords)
                {
                    if (!string.IsNullOrEmpty(kw) && lower.Contains(kw.ToLowerInvariant())) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 장애물을 부수고 통과 상태로 전환합니다.
        /// </summary>
        public void BreakObstacle()
        {
            if (isBroken) return;
            isBroken = true;

            Debug.Log($"[HSH_SpearDashBreakable] ★ '{gameObject.name}' 오브젝트가 창 1번/4번 돌진 스킬에 의해 파괴되었습니다!");

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

            // 2. 파괴 이펙트 생성
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            // 3. 설정된 파괴 모드별 처리
            switch (breakMode)
            {
                case BreakMode.DisableObject:
                    if (respawnAfterTime)
                    {
                        StartCoroutine(RespawnRoutine(0f));
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
                        StartCoroutine(RespawnRoutine(0f));
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

            // 콜라이더는 즉시 해제하여 플레이어가 통과할 수 있게 함
            SetCollidersEnabled(false);

            yield return new WaitForSeconds(animationDelayBeforeDisable);

            if (respawnAfterTime)
            {
                SetVisualAndColliderActive(false);
                StartCoroutine(RespawnRoutine(0f));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator RespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(respawnDelay);

            isBroken = false;
            gameObject.SetActive(true);
            SetVisualAndColliderActive(true);
            Debug.Log($"[HSH_SpearDashBreakable] '{gameObject.name}' 오브젝트가 복구되었습니다.");
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
    }
}
