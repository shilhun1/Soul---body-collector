using UnityEngine;
using UnityEngine.Events;

namespace HSH.Gimmick
{
    /// <summary>
    /// 화살, 탄환, 투사체 공격이 충돌(Hit)하면 작동하는 스위치 기믹 컴포넌트입니다.
    /// 스위치가 작동하면 연동된 문(HSH_GimmickDoor)을 열거나 이벤트를 발송합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HSH_ProjectileSwitch : MonoBehaviour
    {
        [Header("스위치 설정")]
        [Tooltip("한 번 작동하면 계속 열린 상태 유지 여부")]
        [SerializeField] private bool oneShot = true;

        [Tooltip("충돌된 화살/탄환 투사체를 즉시 소멸시킬지 여부")]
        [SerializeField] private bool destroyProjectileOnHit = true;

        [Tooltip("감지할 투사체 태그 목록")]
        [SerializeField] private string[] projectileTags = new string[] { "Projectile", "Arrow", "Bullet", "PlayerAttack" };

        [Header("연동된 문 & 이벤트")]
        [Tooltip("작동 시 열게 될 문 컴포넌트 참조")]
        [SerializeField] private HSH_GimmickDoor targetDoor;

        [Tooltip("스위치 작동 시 발송되는 UnityEvent (추가 액션 지정 가능)")]
        [SerializeField] private UnityEvent onSwitchActivated;

        [Header("시각 & 애니메이션 연출")]
        [SerializeField] private SpriteRenderer switchRenderer;
        [SerializeField] private Color activatedColor = Color.green;
        [SerializeField] private Animator switchAnimator;
        [SerializeField] private string activateTriggerName = "Activate";
        [SerializeField] private string isActivatedBoolName = "IsActivated";

        private bool isActivated = false;

        public bool IsActivated => isActivated;

        private void Awake()
        {
            if (switchRenderer == null) switchRenderer = GetComponent<SpriteRenderer>();
            if (switchAnimator == null) switchAnimator = GetComponent<Animator>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || (oneShot && isActivated)) return;

            if (IsProjectileHit(other.gameObject))
            {
                Debug.Log($"[HSH_ProjectileSwitch] 스위치 '{gameObject.name}' 충돌 감지! 타겟: '{other.name}' (Tag: '{other.tag}')");
                ActivateSwitch();

                if (destroyProjectileOnHit)
                {
                    Destroy(other.gameObject);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || (oneShot && isActivated)) return;

            if (IsProjectileHit(collision.gameObject))
            {
                Debug.Log($"[HSH_ProjectileSwitch] 스위치 '{gameObject.name}' 물리 충돌 감지! 타겟: '{collision.gameObject.name}' (Tag: '{collision.gameObject.tag}')");
                ActivateSwitch();

                if (destroyProjectileOnHit)
                {
                    Destroy(collision.gameObject);
                }
            }
        }

        /// <summary>
        /// 스위치를 외부 코드나 HWJ 시스템에서 수동으로 작동시킬 수 있는 메서드입니다.
        /// </summary>
        public void ActivateSwitch()
        {
            if (oneShot && isActivated) return;

            isActivated = true;
            Debug.Log($"[HSH_ProjectileSwitch] ★ 스위치 '{gameObject.name}' 작동 성공!");

            // 1. 연결된 문 열기
            if (targetDoor != null)
            {
                targetDoor.OpenDoor();
            }

            // 2. UnityEvent 연동 호출
            onSwitchActivated?.Invoke();

            // 3. 시각 피드백 적용
            if (switchRenderer != null)
            {
                switchRenderer.color = activatedColor;
            }

            if (switchAnimator != null)
            {
                if (!string.IsNullOrEmpty(activateTriggerName)) switchAnimator.SetTrigger(activateTriggerName);
                if (!string.IsNullOrEmpty(isActivatedBoolName)) switchAnimator.SetBool(isActivatedBoolName, true);
            }
        }

        private bool IsProjectileHit(GameObject obj)
        {
            if (obj == null) return false;

            for (int i = 0; i < projectileTags.Length; i++)
            {
                if (!string.IsNullOrEmpty(projectileTags[i]) && string.Equals(obj.tag, projectileTags[i], System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
