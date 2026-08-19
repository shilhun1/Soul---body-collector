/// <summary>
/// 격투가 보스 공격이 공통 Animation Event 이름을 받기 위한 계약입니다.
/// 동시에 실행 중인 패턴 하나만 이벤트를 받으므로 패턴별 이벤트 중복 코드를 만들지 않습니다.
/// </summary>
public interface HWJ_IFighterBossAnimationEventReceiver
{
    bool IsPatternRunning { get; }

    void OnAnimationAttackStart();
    void OnAnimationTelegraphStart();
    void OnAnimationEnableHitbox(int strikeNumber);
    void OnAnimationDisableHitbox();
    void OnAnimationApplyMovement();
    void OnAnimationStopMovement();
    void OnAnimationSpawnProjectile();
    void OnAnimationSpawnGroundHazard();
    void OnAnimationTeleportOut();
    void OnAnimationTeleportIn();
    void OnAnimationCameraShakeHook();
    void OnAnimationSfxHook();
    void OnAnimationRecoveryStart();
    void OnAnimationAttackEnd();
}
