using UnityEngine;
using UnityEngine.Events;

namespace HSH.Gimmick
{
    /// <summary>
    /// 화살, 탄환, 투사체 공격 및 플레이어 상호작용(E 키 / 터치 / 공격)으로 작동하는 스위치 기믹 컴포넌트입니다.
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

        [Tooltip("플레이어 상호작용 키(E)로도 직접 작동 허용 여부")]
        [SerializeField] private bool allowPlayerInteract = true;

        [Tooltip("플레이어 상호작용 감지 최대 거리 (0이면 Collider Trigger 진입 기준)")]
        [SerializeField] private float interactMaxDistance = 2.5f;

        [Tooltip("플레이어 직접 접촉(몸체 충돌) 시 즉시 작동 허용 여부")]
        [SerializeField] private bool allowPlayerTouchActivate = false;

        [Tooltip("감지할 투사체 및 공격 태그 목록")]
        [SerializeField] private string[] projectileTags = new string[] { "Projectile", "Arrow", "Bullet", "PlayerAttack", "Attack", "Weapon" };

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
        private bool playerInside = false;
        private HWJ_PlayerInputSystem cachedPlayerInput;

        public bool IsActivated => isActivated;
        public bool OneShot => oneShot;
        public bool DestroyProjectileOnHit => destroyProjectileOnHit;
        public bool AllowPlayerInteract {get => allowPlayerInteract; set => allowPlayerInteract = value; }

        private void Awake()
        {
            if (switchRenderer == null) switchRenderer = GetComponent<SpriteRenderer>();
            if (switchAnimator == null) switchAnimator = GetComponent<Animator>();

            // Unity 2D 물리 엔진에서 OnTriggerEnter2D가 반드시 호출되도록 Kinematic Rigidbody2D 보장
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
            }
        }

        private void Update()
        {
            if (allowPlayerInteract && (!oneShot || !isActivated))
            {
                if (IsPlayerNearby() && IsInteractInputPressed())
                {
                    Debug.Log($"[HSH_ProjectileSwitch] 플레이어 상호작용 입력(Interact)으로 스위치 '{gameObject.name}' 작동!");
                    ActivateSwitch();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || (oneShot && isActivated)) return;

            Debug.Log($"[HSH_ProjectileSwitch] [Trigger 진입] 오브젝트: '{other.name}', Tag: '{other.tag}'");

            if (IsPlayerObject(other.gameObject))
            {
                playerInside = true;
                if (allowPlayerTouchActivate)
                {
                    Debug.Log($"[HSH_ProjectileSwitch] 플레이어 직접 접촉으로 스위치 '{gameObject.name}' 작동!");
                    ActivateSwitch();
                    return;
                }
            }

            if (IsProjectileHit(other.gameObject))
            {
                Debug.Log($"[HSH_ProjectileSwitch] ★★★ 스위치 '{gameObject.name}' 투사체/공격 감지 성공! (타겟: '{other.name}', Tag: '{other.tag}')");
                ActivateSwitch();

                if (destroyProjectileOnHit)
                {
                    Destroy(other.gameObject);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null || (oneShot && isActivated)) return;

            if (IsPlayerObject(other.gameObject))
            {
                playerInside = true;
                if (allowPlayerTouchActivate && !isActivated)
                {
                    ActivateSwitch();
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null && IsPlayerObject(other.gameObject))
            {
                playerInside = false;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || (oneShot && isActivated)) return;

            Debug.Log($"[HSH_ProjectileSwitch] [Collision 진입] 오브젝트: '{collision.gameObject.name}', Tag: '{collision.gameObject.tag}'");

            if (IsPlayerObject(collision.gameObject))
            {
                playerInside = true;
                if (allowPlayerTouchActivate)
                {
                    ActivateSwitch();
                    return;
                }
            }

            if (IsProjectileHit(collision.gameObject))
            {
                Debug.Log($"[HSH_ProjectileSwitch] ★★★ 스위치 '{gameObject.name}' 물리 투사체/공격 감지 성공! (타겟: '{collision.gameObject.name}', Tag: '{collision.gameObject.tag}')");
                ActivateSwitch();

                if (destroyProjectileOnHit)
                {
                    Destroy(collision.gameObject);
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision == null || (oneShot && isActivated)) return;

            if (IsPlayerObject(collision.gameObject))
            {
                playerInside = true;
                if (allowPlayerTouchActivate && !isActivated)
                {
                    ActivateSwitch();
                }
            }
        }

        /// <summary>
        /// 스위치를 수동으로 작동시킬 수 있는 메서드입니다.
        /// </summary>
        public void ActivateSwitch()
        {
            if (oneShot && isActivated) return;

            isActivated = true;
            Debug.Log($"[HSH_ProjectileSwitch] ★★★ 스위치 '{gameObject.name}' 작동 성공!");

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

        private bool IsPlayerObject(GameObject obj)
        {
            if (obj == null) return false;

            if (obj.CompareTag("Player")) return true;

            Transform current = obj.transform;
            while (current != null)
            {
                if (current.CompareTag("Player")) return true;
                current = current.parent;
            }

            if (obj.GetComponentInParent<HWJ_SoulSystem>() != null) return true;
            if (obj.GetComponentInParent<HWJ_PlayerInputSystem>() != null) return true;
            if (obj.GetComponentInParent<HWJ_PossessionSystem>() != null) return true;

            return false;
        }

        private bool IsPlayerNearby()
        {
            if (playerInside) return true;

            Transform playerT = GetPlayerTransform();
            if (playerT == null) return false;

            float dist = Vector2.Distance(transform.position, playerT.position);
            return dist <= interactMaxDistance;
        }

        private Transform GetPlayerTransform()
        {
            if (HWJ_GameAccess.PlayerResolver != null)
            {
                return HWJ_GameAccess.PlayerResolver.transform;
            }

            var soulSystem = Object.FindFirstObjectByType<HWJ_SoulSystem>();
            if (soulSystem != null)
            {
                return soulSystem.transform;
            }

            var playerInput = GetPlayerInput();
            if (playerInput != null)
            {
                return playerInput.transform;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                return playerObj.transform;
            }

            return null;
        }

        private HWJ_PlayerInputSystem GetPlayerInput()
        {
            HWJ_PlayerInputSystem input = HWJ_GameAccess.PlayerInput;
            if (input != null) return input;

            if (cachedPlayerInput == null)
            {
                cachedPlayerInput = Object.FindFirstObjectByType<HWJ_PlayerInputSystem>();
            }

            return cachedPlayerInput;
        }

        private bool IsInteractInputPressed()
        {
            HWJ_PlayerInputSystem input = GetPlayerInput();
            if (input != null && input.InteractPressedThisFrame)
            {
                return true;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                return true;
            }

            return false;
        }

        private bool IsProjectileHit(GameObject obj)
        {
            if (obj == null) return false;

            // 1. 태그 목록 검사 (자식/부모/루트 오브젝트 태그 포함)
            for (int i = 0; i < projectileTags.Length; i++)
            {
                string t = projectileTags[i];
                if (string.IsNullOrEmpty(t)) continue;

                if (string.Equals(obj.tag, t, System.StringComparison.Ordinal))
                {
                    return true;
                }

                Transform current = obj.transform.parent;
                while (current != null)
                {
                    if (string.Equals(current.tag, t, System.StringComparison.Ordinal))
                    {
                        return true;
                    }
                    current = current.parent;
                }
            }

            // 2. HSH 화살 함정 등 전용 투사체 컴포넌트 검사
            if (obj.GetComponent<HSH_ShieldArrowTrap>() != null ||
                obj.GetComponentInParent<HSH_ShieldArrowTrap>() != null)
            {
                return true;
            }

            return false;
        }
    }
}

