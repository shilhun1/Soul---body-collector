using UnityEngine;

namespace HSH.Gimmick
{
    /// <summary>
    /// 스위치(HSH_ProjectileSwitch)나 기믹 조건 완류 시 열리도록 연동하는 문(Door/Gate) 컴포넌트입니다.
    /// 애니메이션 실행, 콜라이더 해제, Transform 위치 이동 등 다양한 열림 모드를 지원합니다.
    /// </summary>
    public class HSH_GimmickDoor : MonoBehaviour
    {
        public enum DoorOpenMode
        {
            AnimatorOnly,    // 애니메이터 파라미터 제어
            DisableCollider, // 단순 충돌체(Collider2D) 해제
            TransformMove,   // 오프셋 위치로 이동
            DisableObject    // 게임오브젝트 비활성화
        }

        [Header("문 연동 설정")]
        [Tooltip("문이 열릴 때 동작할 모드")]
        [SerializeField] private DoorOpenMode openMode = DoorOpenMode.AnimatorOnly;

        [Header("Animator 모드 설정")]
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string openBoolParam = "IsOpened";
        [SerializeField] private string openTriggerParam = "Open";

        [Header("Transform Move 모드 설정")]
        [Tooltip("문이 열렸을 때 이동할 목표 로컬 위치 오프셋 (예: Y축으로 +3 만큼 상승)")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 3f, 0f);
        [SerializeField] private float moveSpeed = 3f;

        [Header("콜라이더 설정")]
        [Tooltip("열릴 때 꺼질 Collider2D (미지정 시 자식에서 자동 수집)")]
        [SerializeField] private Collider2D[] doorColliders;

        private bool isOpen = false;
        private Vector3 startLocalPosition;
        private Vector3 targetLocalPosition;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
            if (doorColliders == null || doorColliders.Length == 0)
            {
                doorColliders = GetComponentsInChildren<Collider2D>();
            }

            startLocalPosition = transform.localPosition;
            targetLocalPosition = startLocalPosition + openOffset;
        }

        private void Update()
        {
            // Transform 이동 모드일 경우 부드럽게 목표 위치로 이동
            if (isOpen && openMode == DoorOpenMode.TransformMove)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * moveSpeed);
            }
        }

        /// <summary>
        /// 문을 엽니다.
        /// </summary>
        public void OpenDoor()
        {
            if (isOpen) return;

            isOpen = true;
            Debug.Log($"[HSH_GimmickDoor] ★ 문 '{gameObject.name}'이(가) 열렸습니다! (작동 모드: {openMode})");

            // 1. 콜라이더 비활성화 (문 통과 허용)
            SetCollidersEnabled(false);

            // 2. 모드별 동작 수행
            switch (openMode)
            {
                case DoorOpenMode.AnimatorOnly:
                    if (doorAnimator != null)
                    {
                        if (!string.IsNullOrEmpty(openBoolParam)) doorAnimator.SetBool(openBoolParam, true);
                        if (!string.IsNullOrEmpty(openTriggerParam)) doorAnimator.SetTrigger(openTriggerParam);
                    }
                    break;

                case DoorOpenMode.DisableObject:
                    gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// 문을 닫습니다.
        /// </summary>
        public void CloseDoor()
        {
            if (!isOpen) return;

            isOpen = false;
            Debug.Log($"[HSH_GimmickDoor] 문 '{gameObject.name}'이(가) 닫혔습니다!");

            SetCollidersEnabled(true);

            if (openMode == DoorOpenMode.AnimatorOnly && doorAnimator != null)
            {
                if (!string.IsNullOrEmpty(openBoolParam)) doorAnimator.SetBool(openBoolParam, false);
            }

            if (openMode == DoorOpenMode.TransformMove)
            {
                transform.localPosition = startLocalPosition;
            }

            if (openMode == DoorOpenMode.DisableObject)
            {
                gameObject.SetActive(true);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (doorColliders == null) return;
            for (int i = 0; i < doorColliders.Length; i++)
            {
                if (doorColliders[i] != null)
                {
                    doorColliders[i].enabled = enabled;
                }
            }
        }
    }
}
