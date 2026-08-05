# Fighter Boss 제작 구조와 사용 가이드

## 1. 어떻게 만들었는가

보스 하나가 모든 기능을 직접 처리하지 않도록 데이터, 판단, 실행, 표현을 분리했다.

1. `HWJ_MidBoss1_RootObjectData.asset`
   보스의 최상위 데이터다. 보스 식별 정보, 공통 능력치와 `HWJ_MidBoss1_TypeData.asset` 연결을 소유한다.
2. `HWJ_MidBoss1_TypeData.asset`
   보스 전용 FSM 설정을 가진다. 보스방 크기, 전투 시작 조건, 추적 거리, 근거리 판정, 페이즈 전환 시간 등을 관리한다.
3. `HWJ_BossPatternDataSO`
   각 패턴의 ID, 사용 페이즈, HP 조건, 쿨타임, 가중치를 개별 SO로 관리한다.
4. `HWJ_BossBrainSystem`
   플레이어 탐색, 전투 시작, 추적, 공격 가능 상태, P1/전환/P2/Death 흐름을 총괄한다.
5. `HWJ_BossPatternSystem`
   현재 페이즈, HP, 거리, 공중 상태, 쿨타임과 최근 패턴 기록을 검사해 실행할 패턴을 고른다. 최근 2개 패턴을 제외하며 네 번째 결정마다 유효 후보를 순환해 낮은 가중치 패턴이 영구적으로 제외되지 않게 한다.
6. 패턴 실행 시스템
   P1은 Combo, Charge, Uppercut, GroundSlam 시스템이 나뉘어 있다. P2 9종은 `HWJ_FighterBossPhaseTwoPatternSystem`이 프로필별로 실행한다.
7. `HWJ_FighterBossAnimatorSystem`
   `Phase`, `AttackId`, `IsAttacking`, `Speed`, `Hurt`, `Dead` 파라미터와 실제 Animator 상태를 연결한다.
8. `HWJ_FighterBossAnimationEvents`
   Animation Clip의 `Anim_*` 이벤트를 현재 실행 중인 패턴 시스템에 전달한다.
9. `HWJ_FighterBossHitboxSystem`
   공격 프레임에서만 판정 Collider를 켜고 같은 공격 창에서 중복 피해를 막는다.
10. 전환과 사망
    P1 HP가 0이면 사망하지 않고 모든 공격을 정리한 뒤 P2 HP를 최대로 복원한다. P2 HP가 0일 때만 최종 Death를 실행한다.

## 2. 한 번의 공격이 실행되는 순서

1. Brain 또는 테스트 창이 패턴 실행을 요청한다.
2. 패턴 시스템이 공격 ID와 Animator State를 지정한다.
3. 준비/예고 프레임이 재생된다.
4. `Anim_EnableHitbox`가 공격 판정을 켠다.
5. Charge나 Uppercut이면 `Anim_ApplyMovement`가 Rigidbody2D를 실제로 이동시킨다.
6. `Anim_StopMovement`와 `Anim_DisableHitbox`가 이동과 판정을 정리한다.
7. `Anim_RecoveryStart`에서 후딜레이 상태가 된다.
8. `Anim_AttackEnd`가 공격 상태를 끝내고 Idle로 복귀시킨다.
9. Animation Event가 빠져도 watchdog이 패턴과 임시 공격 오브젝트를 정리한다.

## 3. 현재 보스방에서 사용하는 방법

사용 중인 Scene은 `Assets/01Scenes/HWJ_Stage1_04_MidBossBarracks.unity`이며 다음 Prefab 인스턴스를 이미 참조한다.

`Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab`

1. Scene을 연다.
2. 보스 Prefab 인스턴스가 보스방 중앙에 있는지 확인한다.
3. 바닥과 벽에 `Collider2D`가 있고 Boss Rigidbody2D와 충돌하는 Layer인지 확인한다.
4. 플레이어에 `HWJ_RootObjectDataResolver`가 있고 `ObjectType.Player` 데이터가 연결됐는지 확인한다.
5. 전체 게임 시스템을 사용하는 Scene이면 `HWJ_GameManager`는 하나만 둔다. Manager의 `PlayerResolver`가 있으면 보스가 이 대상을 가장 먼저 사용한다.
6. Play Mode를 시작한다.
7. 빙의 몸 상태의 플레이어가 보스 중심 기준 `35 x 6.5` 범위에 들어오면 전투가 자동 시작된다.

기본 설정에서는 영혼 상태 플레이어가 보스방에 들어와도 자동 전투가 시작되지 않는다. 기능만 확인할 때는 테스트 창의 `Start Phase1`을 사용한다.

## 4. 다른 Scene에 배치하는 방법

1. Hierarchy에 production boss Prefab을 한 번만 드래그한다.
2. Prefab 위치를 보스방 중심으로 둔다. 보스방 Bounds는 보스 Transform을 기준으로 계산한다.
3. 바닥과 좌우 경계에 Collider2D를 배치한다.
4. 플레이어 Resolver 또는 GameManager의 PlayerResolver를 준비한다.
5. 필요하면 `HWJ_MidBoss1_TypeData.asset`의 `bossRoomOffset`과 `bossRoomSize`를 방 크기에 맞춘다.
6. Prefab 내부 Animator, 패턴 SO, Hitbox, Brain, Death 컴포넌트는 이미 연결돼 있으므로 다시 추가하지 않는다.

## 5. 강제 패턴 확인 방법

1. Unity 메뉴에서 `Tools > HWJ > Boss > Fighter Boss Test Window`를 연다.
2. Play Mode를 시작한다.
3. `Find Scene Boss`를 누른다. 찾지 못하면 `Play Mode Instance` 칸에 Scene의 보스를 직접 넣는다.
4. 자동 AI와 버튼 실행이 경쟁하지 않게 `AI Enabled`를 끈다.
5. P1은 `Start Phase1` 후 Combo, Charge, Uppercut, GroundSlam 버튼으로 확인한다.
6. P2는 `Force Phase2` 후 9개 패턴 버튼으로 확인한다.
7. `Set Health To 1`은 HP만 1로 만든다. 이후 실제 피해를 한 번 받아야 전환 또는 사망이 실행된다.
8. 즉시 전환만 확인하려면 `Phase Transition`을 사용한다.
9. 최종 사망만 확인하려면 `P2 Death`를 사용한다.
10. 중간에 테스트를 끊었다면 `Clear Attack Objects`를 누른 후 다음 패턴을 실행한다.
11. 초기 상태로 돌아가려면 `Reset Boss`를 사용한다.

테스트 창 상단에서 현재 Animator State, Phase, HP, AttackId, 활성 Hitbox와 AI 상태를 실시간으로 볼 수 있다.

## 6. 자동 테스트 실행 방법

- 빠른 보스 계약 테스트: `Tools > HWJ > Boss > Run Fighter Boss Attack Flow Tests`
- 전체 보스 회귀 테스트: `Tools > HWJ > Boss > Run All Fighter Boss PlayMode Tests`

전체 테스트는 P1 4종, 전환, P2 9종, 궁극기 조건, SoulBind 해제, 최종 사망, 2분/3분 AI 가속 실행과 전체 전투 3회를 포함한다.

결과 파일:

- `Assets/02Scripts/HWJ/Art/Boss/_Generated/Reports/FighterBossAttackFlowPlayMode.md`
- `C:\Docs\Generated\HWJ_FighterBossAttackFlow.xml`

## 7. 값을 수정하는 위치

### 공통 능력치와 최상위 연결

`Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss1_RootObjectData.asset`

### 보스방, 추적과 페이즈 설정

`Assets/02Scripts/HWJ/ScriptableObjects/TypeData/Boss/HWJ_MidBoss1_TypeData.asset`

현재 주요 값은 자동 시작 On, 방 크기 `35 x 6.5`, 공격 시작 거리 8, 적정 공격 거리 4.5, 근거리 기준 4, 전환 시간 10초다.

### 패턴 쿨타임, HP 조건과 가중치

`Assets/02Scripts/HWJ/ScriptableObjects/BossPatterns/HWJ_FighterBoss_*.asset`

Ultimate는 P2 시작 후 10초, HP 35% 이하, 재사용 18초 조건을 추가로 검사한다.

### P2 이동, 피해와 연출 시간

production boss Prefab의 `HWJ_FighterBossPhaseTwoPatternSystem` 프로필에서 `damageMultiplier`, `movementSpeed`, `movementDuration`, `telegraphRange` 등을 조절한다.

## 8. 애니메이션을 교체하는 방법

1. 원본 이미지는 수정하지 않는다.
2. 정리된 투명 프레임만 `_Generated/Sprites` 아래에 둔다.
3. 검은 배경, 제목, 번호, 격자 잔여 픽셀이 0인지 Validation을 통과시킨다.
4. Clip의 Sprite Keyframe을 새 프레임으로 교체한다.
5. 공격 Clip은 Loop를 끈다.
6. 공격 순서에 맞춰 `Anim_EnableHitbox`, `Anim_ApplyMovement`, `Anim_DisableHitbox`, `Anim_RecoveryStart`, `Anim_AttackEnd`를 배치한다.
7. `Tools > HWJ > Boss > Build Integrated Fighter Boss`로 Controller와 Prefab 연결을 다시 생성한다.
8. 전체 PlayMode 테스트를 다시 실행한다.

Builder를 실행하면 생성 Clip, Controller, Pattern SO의 기본값과 Prefab 연결을 다시 기록한다. 생성 결과를 직접 수정했다면 재빌드 시 덮어쓸 수 있으므로, 영구 변경은 `HWJ_FighterBossIntegrationBuilder`의 생성 규칙에도 함께 반영해야 한다.

## 9. 최종 에셋 연결 시 주의점

- 현재 Hurt와 다섯 전환 Clip은 검증된 프레임을 임시 재사용한다.
- P2 강화 4종은 P1 공격 Clip을 별도 P2 State에서 재사용한다.
- 임시 Telegraph, Hazard와 Projectile은 타이밍 검증용이다.
- 최종 VFX Prefab과 AudioClip을 연결한 뒤 카메라 흔들림, Sorting Layer, 실제 플레이어 피해, 씬 재시작을 다시 확인해야 한다.
