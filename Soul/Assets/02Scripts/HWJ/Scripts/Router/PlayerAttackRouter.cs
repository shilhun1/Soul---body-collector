using UnityEngine;
using UnityEngine.InputSystem;

namespace HWJ
{
    [DefaultExecutionOrder(-50)]
    public class PlayerAttackRouter : MonoBehaviour
    {
        public enum AttackSystemType
        {
            Hys,
            HWJ
        }

        [Header("Input")]
        [SerializeField] private HWJ_PlayerInputSystem playerInput;

        [Header("Attack Comonents")]
        [Tooltip("hys_player_attack 컴포넌트를 연결함")]
        [SerializeField] private MonoBehaviour hysAttackComponent;

        [Tooltip("HWJ_PlayerAttackSystem 컴포넌트를 연결함")]
        [SerializeField] private MonoBehaviour hwjAttackComponent;

        [Header("Current Attack System")]
        [SerializeField] private AttackSystemType currentAttackSystem = AttackSystemType.HWJ;

        [Header("Debug")]
        [SerializeField] private bool useAttackDebugLog = true;

        private IPlayerAttackHandler hysAttack;
        private IPlayerAttackHandler hwjAttack;
        private IPlayerAttackHandler currentAttack;

        private void Awake()
        {
            if(playerInput == null)
            {
                playerInput = GetComponent<HWJ_PlayerInputSystem>();
            }

            hysAttack = hysAttackComponent as IPlayerAttackHandler;
            hwjAttack = hwjAttackComponent as IPlayerAttackHandler;

            ValidateAttackComponent(
                hysAttackComponent,
                hysAttack,
                nameof(hysAttackComponent)
            );

            ValidateAttackComponent(
                hwjAttackComponent,
                hwjAttack,
                nameof(hwjAttackComponent)
             );
            SetAttackSystem(currentAttackSystem);

        }

        private void Update()
        {
            if (currentAttack == null)
            {
                if(useAttackDebugLog)
                {
                    Debug.LogWarning("[공격 실패] 현재 선택된 공격 시스템이 없음.",
                        this);
                }
                return;
            }

            if (playerInput != null)
            {
                if (playerInput.AttackPressedThisFrame)
                {
                    TryAttackWithDebugLog();
                }

                if (playerInput.AttackReleasedThisFrame)
                {
                    currentAttack.OnAttackReleased();

                    if(useAttackDebugLog)
                    {
                        Debug.Log(
                            $"[공격 버튼 해제] {currentAttack.GetType().Name}",
                            this);
                    }
                }
                return;
            }

            if(Keyboard.current == null)
            {
                return;
            }
            if(Keyboard.current.xKey.wasPressedThisFrame)
            {
                TryAttackWithDebugLog();
            }
            if(Keyboard.current.xKey.wasReleasedThisFrame)
            {
                currentAttack.OnAttackReleased();

                if (useAttackDebugLog)
                {
                    Debug.Log(
                        $"[공격 버튼 해제] {currentAttack.GetType().Name}",
                        this);
                }

            }
        }
        
        public void SetAttackSystem(AttackSystemType newAttackSystem)
        {
            currentAttack?.CancelAttack();

            currentAttackSystem = newAttackSystem;

            switch (currentAttackSystem)
            {
                case AttackSystemType.Hys:
                    currentAttack = hysAttack;
                    break;
                case AttackSystemType.HWJ:
                    currentAttack = hwjAttack;
                    break;
                default:
                    currentAttack = null;
                    break;
            }
        }

        public void UseHysAttack()
        {
            SetAttackSystem(AttackSystemType.Hys);
        }

        public void UseHwjAttack()
        {
            SetAttackSystem(AttackSystemType.HWJ);
        }

        public void CancelCurrentAttack()
        {
            currentAttack?.CancelAttack();
        }

        private void ValidateAttackComponent(
            MonoBehaviour component,
            IPlayerAttackHandler handler,
            string fieldName)
        {
            if(component == null)
            {
                Debug.LogWarning(
                    $"[PlayerAttackRouter] {fieldName}이 연결되지 않았습니다.",
                    this);

                return;
            }

            if(handler == null)
            {
                Debug.LogError(
                    $"[PlayerAttackRouter] {component.GetType().Name}은 " +
                    $"{nameof(IPlayerAttackHandler)}를 구현하지 않았습니다.",
                    component);
            }
        }

        private void TryAttackWithDebugLog()
        {
            if (currentAttack == null)
            {
                Debug.LogError(
                    "[공격 실패] currentAttack이 null입니다.",
                    this
                );

                return;
            }

            bool attackUsed = currentAttack.OnAttackPressed();
            string systemName = currentAttack.GetType().Name;

            if (!useAttackDebugLog)
            {
                return;
            }

            if (attackUsed)
            {
                string successResult = GetCurrentAttackResult();

                Debug.Log(
                    $"[공격 사용 성공] 시스템: {systemName}, " +
                    $"시간: {Time.time:F2}, 결과: {successResult}",
                    this
                );

                return;
            }

            string failureResult = GetCurrentAttackResult();

            Debug.LogWarning(
                $"[공격 사용 실패] 시스템: {systemName}, " +
                $"시간: {Time.time:F2}, 이유: {failureResult}",
                this
            );
        }
        private string GetCurrentAttackResult()
        {
            if (currentAttack is HWJ_PlayerAttackSystem hwjAttack)
            {
                if (!string.IsNullOrWhiteSpace(hwjAttack.LastAttackResult))
                {
                    return hwjAttack.LastAttackResult;
                }

                return "HWJ 공격 시스템이 실패 이유를 기록하지 않았습니다.";
            }

            return "상세 결과를 지원하지 않는 공격 시스템입니다.";
        }

    }
}
