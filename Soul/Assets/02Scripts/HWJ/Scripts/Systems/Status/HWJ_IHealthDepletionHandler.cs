/// <summary>
/// 공통 HP가 0이 되었을 때 즉시 사망시키지 않고 별도 흐름으로 처리해야 하는 오브젝트의 계약입니다.
/// 격투가 보스는 1페이즈 HP 소진을 페이즈 전환으로 바꾸기 위해 이 계약을 구현합니다.
/// </summary>
public interface HWJ_IHealthDepletionHandler
{
    /// <summary>
    /// HP가 0이어도 공통 사망 판정을 잠시 보류해야 하는 동안 true를 반환합니다.
    /// </summary>
    bool IsHealthDepletionHandled { get; }

    /// <summary>
    /// HP 소진을 처리했으면 true를 반환합니다. false이면 RuntimeStatusSystem이 기존 사망 처리를 계속합니다.
    /// </summary>
    bool TryHandleHealthDepleted(HWJ_RuntimeStatusSystem status);
}
