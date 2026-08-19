# 영혼 피해와 게임오버 규칙

## 적용 규칙

- 플레이어가 `Soul` 상태이면 일반 공격, 몬스터 공격, 보스 공격, 최대 HP 비례 고정 피해를 포함한 모든 전투 HP 피해를 받지 않는다.
- 플레이어가 `BodyToSoul` 전환 상태일 때도 전투 HP 피해를 받지 않는다.
- 영혼 상태의 정신력은 `HWJ_RuntimeStatusSystem.TryApplySpiritMentalCost()`를 통해서만 감소한다.
- 영혼 정신력이 0보다 큰 동안에는 전투 피해나 HP 값 때문에 게임오버되지 않는다.
- 영혼 정신력이 0이 되면 `HWJ_SoulSystem.EnterDeadState()`가 호출되고 게임오버 상태로 전환된다.
- 육신 상태에서는 기존처럼 전투 HP 피해를 받으며, 육신 HP가 0이 되면 육신 붕괴 후 영혼 상태로 돌아간다.

## 사용하는 시스템

### HWJ_RuntimeStatusSystem

- `IsSpiritDamageImmune`으로 현재 영혼 또는 육신 이탈 전환 상태인지 판정한다.
- `CanReceiveHitFrom()`에서 영혼 상태의 공격 적중과 넉백을 차단한다.
- `ApplyDamage()`에서도 같은 판정을 사용하므로 중간보스의 고정 피해처럼 직접 들어오는 피해도 차단된다.
- `TryApplySpiritMentalCost()`는 영혼 정신력을 감소시키고, 0이 되면 사망 상태로 전환한다.

### HWJ_RuntimeStatusHealthDepletion

- 예외적인 코드가 영혼 상태의 HP를 0으로 만들더라도 정신력이 남아 있으면 사망시키지 않는다.
- 정신력이 실제로 0일 때만 `HWJ_SoulSystem`을 사망 상태로 전환한다.

## 확인 방법

1. 플레이어를 영혼 상태로 만든다.
2. 중간보스 패턴 범위 안에 들어간다.
3. 공격을 맞아도 영혼 정신력과 현재 HP가 감소하지 않는지 확인한다.
4. 영혼 구슬 스위치나 빙의 시도처럼 `TryApplySpiritMentalCost()`를 사용하는 행동으로 정신력을 감소시킨다.
5. 정신력이 1 이상이면 게임오버되지 않는지 확인한다.
6. 정신력이 0이 되면 영혼 상태가 `Dead`로 바뀌고 게임오버 UI가 표시되는지 확인한다.

자동 검증은 `HWJ_PossessionModesPlayModeTests.SpiritState_IgnoresCombatDamage_AndDiesOnlyWhenMentalReachesZero`에서 수행한다.
