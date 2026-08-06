using UnityEngine;

namespace HSH.Gimmick
{
    /// <summary>
    /// 플레이어가 영혼(Soul/Spirit) 상태일 때만 통과할 수 있고,
    /// 육체(Body/Possessed) 상태일 때는 막히는 물리 벽 기믹 컴포넌트입니다.
    /// 2D Collider2D가 부착된 벽 오브젝트에 넣어서 사용합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HSH_SoulPassableWall : MonoBehaviour
    {
        [Header("벽 설정")]
        [Tooltip("영혼 상태일 때 충돌을 해제하여 통과를 허용할지 여부")]
        [SerializeField] private bool allowSoulPass = true;

        [Tooltip("영혼 상태일 때 벽의 시각적 투명도 비율 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float soulPassableAlpha = 0.4f;

        [Tooltip("상태 변화 시 시각적 연출 전환 속도")]
        [SerializeField] private float fadeSpeed = 5f;

        [Header("시각 연출 참조 (선택)")]
        [SerializeField] private SpriteRenderer wallRenderer;

        private Collider2D wallCollider;
        private Color defaultColor = Color.white;
        private bool isCurrentlyIgnored = false;

        private void Awake()
        {
            wallCollider = GetComponent<Collider2D>();
            if (wallRenderer == null)
            {
                wallRenderer = GetComponent<SpriteRenderer>();
            }

            if (wallRenderer != null)
            {
                defaultColor = wallRenderer.color;
            }
        }

        private void Update()
        {
            UpdatePassableState();
        }

        private void UpdatePassableState()
        {
            var soulSystem = Object.FindFirstObjectByType<HWJ_SoulSystem>();
            if (soulSystem == null) return;

            // 플레이어가 영혼 상태인지 확인 (Soul 상태이거나 Spirit 형태일 때)
            bool isSoul = soulSystem.CurrentState == HWJ_SoulRuntimeState.Soul 
                       || soulSystem.CurrentState == HWJ_SoulRuntimeState.BodyToSoul
                       || soulSystem.IsSpiritExistence;

            bool shouldIgnore = allowSoulPass && isSoul;

            // 플레이어 Collider2D 찾기
            Collider2D playerCollider = soulSystem.GetComponent<Collider2D>();
            if (playerCollider != null && wallCollider != null)
            {
                if (isCurrentlyIgnored != shouldIgnore)
                {
                    isCurrentlyIgnored = shouldIgnore;
                    Physics2D.IgnoreCollision(wallCollider, playerCollider, shouldIgnore);
                }
            }

            // 시각적 피드백: 영혼 상태일 때 벽의 투명도 조절
            if (wallRenderer != null)
            {
                Color targetColor = defaultColor;
                if (shouldIgnore)
                {
                    targetColor.a = defaultColor.a * soulPassableAlpha;
                }
                wallRenderer.color = Color.Lerp(wallRenderer.color, targetColor, Time.deltaTime * fadeSpeed);
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 충돌 무시 상태 복구
            if (isCurrentlyIgnored && wallCollider != null)
            {
                var soulSystem = Object.FindFirstObjectByType<HWJ_SoulSystem>();
                if (soulSystem != null)
                {
                    Collider2D playerCollider = soulSystem.GetComponent<Collider2D>();
                    if (playerCollider != null)
                    {
                        Physics2D.IgnoreCollision(wallCollider, playerCollider, false);
                    }
                }
                isCurrentlyIgnored = false;
            }
        }
    }
}
