# 중간보스2 설정 및 사용 가이드

## 현재 작업 범위

애니메이션, 이펙트, 사운드는 추후 연결 대상으로 남겨두고, 중간보스2가 게임 데이터와 보스 패턴 시스템에서 독립적으로 사용될 수 있도록 데이터와 프리팹을 분리했다.

## 추가된 파일

1. `Assets/02Scripts/HWJ/ScriptableObjects/RootObjects/Bosses/HWJ_MidBoss2_RootObjectData.asset`
   - 중간보스2의 최상위 데이터다.
   - ID는 `boss.mid.02.captain`이다.
   - 체력, 공격력, 방어력, 이동속도, 보상, 모델 참조를 가진다.

2. `Assets/02Scripts/HWJ/ScriptableObjects/TypeData/Boss/HWJ_MidBoss2_TypeData.asset`
   - 중간보스2의 보스 전용 데이터다.
   - 보스방 범위, 인식 조건, 50% 페이즈 전환, 그로기, 보스전 진입 조건을 가진다.

3. `Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss2_Runtime_Prefab.prefab`
   - 씬에 배치해서 테스트하는 중간보스2 런타임 프리팹이다.
   - `HWJ_BossBrainSystem`, `HWJ_BossPatternSystem`, `HWJ_MidBossPatternSystem`, 전투/피격/상태 컴포넌트를 포함한다.
   - `HWJ_RuntimeSaveIdentity`를 포함한다. 저장 서비스가 켜진 상태에서도 보스 처치 보상이 한 번만 지급되도록 보상 Claim ID를 만든다.

## 중간보스1과 다른 점

- 중간보스1은 기존 격투가형 보스 구조를 사용한다.
  - `Use Two Bar Phase Health`가 켜져 있다.
  - 격투가 보스 전용 Combo, Charge, Uppercut, GroundSlam, Phase2 Pattern, Death, Animator 보조 시스템을 사용한다.
- 중간보스2는 `boss.mid.02.captain` ID를 사용한다.
- 중간보스2 RootObjectData는 현재 중간보스 기획 이미지의 기본 능력치를 따른다.
  - HP: 7500
  - 공격력: 100
  - 방어력: 40
  - 이동속도: 15
  - 경험치 보상: 350
- 중간보스2 프리팹은 `HWJ_MidBossPhysical_Pattern1~7`을 사용한다.
- 기존 FighterBoss 전용 두 줄 체력 방식은 사용하지 않는다.
- 체력 50% 이하가 되면 `HWJ_BossTypeDataSO`의 `phaseTwoHpRatio` 기준으로 2페이즈 전환을 진행한다.

## 사용하는 패턴

중간보스2 프리팹의 `HWJ_BossPatternSystem`에는 아래 패턴이 연결되어 있다.

1. `HWJ_MidBossPhysical_Pattern1`
   - HP가 15%씩 깎일 때마다 사용 기회를 얻는다.
   - 보스방 안에 빙의 가능한 시체가 없을 때만 소환 패턴을 사용할 수 있다.

2. `HWJ_MidBossPhysical_Pattern2`
   - 1페이즈 근거리 패턴이다.
   - 대쉬 후 가로 베기와 세로 베기를 실행한다.

3. `HWJ_MidBossPhysical_Pattern3`
   - 1페이즈 패턴이다.
   - 플레이어를 베고 먼 쪽으로 이동한 뒤 맵 가로 검기를 사용한다.

4. `HWJ_MidBossPhysical_Pattern4`
   - 1페이즈 패턴이다.
   - 조준 후 돌진하고 플레이어 최대 체력 기준 고정 피해를 준 뒤 그로기에 들어간다.
   - 조준이 시작된 순간의 플레이어 위치를 기준으로 돌진하기 때문에, 플레이어가 예고를 보고 벗어나면 피할 수 있다.
   - 돌진 판정과 패턴 물리 복구를 먼저 끝낸 뒤 그로기에 들어가므로, 그로기 진입 중 현재 패턴 코루틴을 다시 취소하지 않는다.

5. `HWJ_MidBossPhysical_Pattern5`
   - 2페이즈 근거리 패턴이다.
   - 붉은 검 연출 대기 후 연속 공격과 충격파를 실행한다.

6. `HWJ_MidBossPhysical_Pattern6`
   - 2페이즈 패턴이다.
   - 은신, 배후 순간이동, 베기, 복귀 후 검기를 실행한다.

7. `HWJ_MidBossPhysical_Pattern7`
   - 2페이즈 패턴이다.
   - 공중으로 올라간 뒤 플레이어 위치로 낙하하고 충격파를 발생시킨다.

패턴 방향 고정:

- 가로 베기, 고정 피해 돌진, 반방 충격파는 예고 표시가 뜬 순간의 방향을 실제 데미지 판정까지 유지한다.
- 패턴이 시작된 뒤 플레이어가 보스 뒤로 넘어가도 예고와 다른 방향으로 판정이 따라가지 않는다.
- 애니메이션을 붙일 때도 같은 기준으로 공격 시작 프레임에서 방향을 잠그면 된다.

패턴 중 물리 처리:

- 중간보스2 프리팹은 Dynamic Rigidbody2D와 Gravity Scale 1을 사용한다.
- 대쉬, 순간이동, 공중 대기 패턴이 중력 때문에 밀리지 않도록, `HWJ_MidBossPatternSystem`은 특수 패턴 실행 중 Rigidbody2D의 Gravity Scale을 0으로 잠시 바꾼다.
- 패턴이 정상 종료되거나 취소되면 원래 Gravity Scale로 복구하고 속도를 0으로 정리한다.

## 씬에서 사용하는 방법

1. Project 창에서 `Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss2_Runtime_Prefab.prefab`을 찾는다.
2. 중간보스2를 테스트할 보스방 씬의 Hierarchy에 드래그해서 배치한다.
3. 보스방 중앙 위치에 프리팹을 둔다.
4. `HWJ_MidBoss2_TypeData.asset`의 `bossRoomSize`와 `bossRoomOffset`을 보스방 크기에 맞춘다.
5. 플레이어가 보스방 안에 있고 영혼 상태가 아니면 보스가 자동으로 전투를 시작한다.
6. `HWJ_MidBossPatternSystem`에서 소환 몬스터 프리팹을 직접 지정하면 그 프리팹을 사용한다.
7. 중간보스2 프리팹에는 기본적으로 검, 방패, 창, 활 빙의 가능 몬스터 런타임 프리팹이 연결되어 있다.
8. 소환 몬스터 프리팹이 비어 있으면 연결된 RootObjectData의 모델 프리팹을 기준으로 런타임 컴포넌트를 보강하지만, 실제 테스트에서는 `소환 몬스터 프리팹 목록`을 채운 상태를 기준으로 사용한다.

## 인스펙터에서 조정하는 값

중간보스2 프리팹의 `HWJ_MidBossPatternSystem`은 기획자가 직접 조정할 수 있도록 한글 Header, Tooltip, InspectorName을 사용한다.

- `패턴 실행 키`
  - 물리형 중간보스 패턴 데이터와 연결되는 값이다.
  - 기본값은 `mid_boss_physical`이다.
  - BossPatternData의 Custom Executor Key와 다르면 패턴이 실행되지 않는다.
- `공통 타이밍`
  - 대쉬 시간, 베기 예고 시간, 후딜 시간, 가로 베기 사거리, 충격파 높이, 예고 선 두께와 색상을 조정한다.
  - 애니메이션을 붙이기 전에는 이 값으로 패턴 템포와 판정 감각을 먼저 맞춘다.
- `패턴 1 - 몬스터 소환`
  - HP가 몇 퍼센트씩 깎일 때 소환 기회가 쌓이는지, 몇 마리를 소환하는지, 호루라기 대기 시간과 소환 간격을 조정한다.
  - `소환 몬스터 프리팹 목록`과 `소환 몬스터 RootObjectData 목록`을 같은 순서로 맞추면 패턴 1에서 원하는 빙의 가능 몬스터를 순서대로 소환할 수 있다.
- `패턴 3 - 검기`
  - 검기 차징 시간, 판정 높이, 데미지 배율을 조정한다.
- `패턴 4 - 고정 피해 돌진`
  - 조준 시간, 최대 체력 기준 고정 피해 비율, 그로기 시간을 조정한다.
- `패턴 5 - 2페이즈 연속 공격`
  - 붉은 검 연출 시간, 연속 공격 간격, 마지막 충격파 가로 비율을 조정한다.
- `패턴 6 - 은신 기습`
  - 은신 전 대기 시간, 플레이어 뒤로 이동하는 거리, 복귀 전 대기 시간을 조정한다.
- `패턴 7 - 점프 내려찍기`
  - 점프 높이, 공중 대기 시간, 내려찍기 반지름, 착지 충격파 가로 비율을 조정한다.

## 테스트 창 사용 방법

1. Unity 상단 메뉴에서 `Tools/HWJ/Boss/Mid Boss Physical Test Window`를 연다.
2. Play Mode를 시작한다.
3. 테스트 창 상단의 `권장 테스트 순서` 안내를 보고 순서대로 진행한다.
4. 중간보스2를 테스트할 때는 `씬의 중간보스2 찾기`를 눌러 `boss.mid.02.captain` ID를 가진 보스를 찾는다.
5. `씬의 물리 중간보스 찾기`는 씬 안의 첫 번째 물리 중간보스 패턴 컴포넌트를 찾는 일반 버튼이다. 중간보스1과 중간보스2가 같이 있으면 중간보스2 전용 버튼을 우선 사용한다.
6. 빠르게 수동 패턴 테스트를 시작하려면 `중간보스2 Play Mode 빠른 테스트 준비`를 누른다.
   - 이 버튼은 현재 씬에서 중간보스2를 찾고, 현재 패턴을 취소하고, HP를 풀회복하고, AI를 끄고, 패턴 기록을 초기화하고, 타겟을 자동 연결한 뒤 전투를 시작한다.
   - 버튼 실행 후 자동으로 `중간보스2 Play Mode 준비 상태 검사`도 실행한다.
7. 수동으로 하나씩 준비하려면 `전투 시작`을 눌러 보스 전투 상태로 진입시킨다.
8. AI 자동 패턴을 확인할 때는 `AI 활성화`를 켠다. 보스가 플레이어 거리와 페이즈 조건에 따라 자동으로 패턴을 고른다.
9. `타겟 자동 연결`은 씬의 Player 타입 오브젝트를 찾아 보스 타겟으로 연결한다.
10. `AI 패턴 1회 실행`은 현재 거리, 페이즈, 쿨타임, Rule/Condition을 기준으로 보스 공통 패턴 선택을 한 번 실행한다.
11. 특정 패턴 루틴만 확인하고 싶으면 `패턴 2~7` 버튼을 눌러 강제 실행한다. 이 버튼은 자동 선택 조건 검증이 아니라 패턴 연출/판정 루틴 확인용이다.
12. `패턴 1`은 HP가 15%씩 감소해 사용 기회를 얻고, 보스방 안에 빙의 가능한 시체가 없어야 성공한다.
13. `HP 50%로 설정`을 누르면 2페이즈 전환 조건을 빠르게 확인할 수 있다.
14. `50% 페이즈 전환 테스트 준비`는 현재 패턴을 취소하고, 타겟을 연결하고, 전투를 시작한 뒤 HP를 49%로 낮춘다.
   - 이 버튼은 실제 중간보스2 페이즈 전환 루트를 타게 만드는 용도다.
   - 누른 뒤 보스가 중앙으로 이동하고, 대사를 출력하고, 무적 상태로 10초 전환을 진행하는지 확인한다.
15. `2페이즈 AI 패턴 1회 실행`은 현재 페이즈가 2일 때만 AI 패턴 선택을 한 번 실행한다.
   - 이 버튼으로 2페이즈 패턴 5, 6, 7이 실제 조건에서 선택되는지 확인한다.
16. `HP 0 사망 이벤트 테스트`는 보스 HP를 1로 만든 뒤 실제 `ApplyDamage()`를 호출해 `DamageApplied -> ActorDied -> BossFlowSystem` 경로가 작동하는지 확인한다.
   - 테스트 시작 전에 보스 HP, 조작 잠금, 히트스턴, 임시 무적, 피격 쿨타임을 초기화한다.
   - 성공 로그에서 `보스 사망=Yes`, `BossFlow 메시지`, `마지막 플로우`를 확인한다.
17. `BossFlow 직접 완료 테스트`는 전투 데미지 없이 현재 씬의 중간보스2 BossFlowSystem에 `TryMarkBossDefeated()`를 직접 호출한다.
   - 사망 이벤트 문제가 아니라 스테이지 진행 연결 자체가 되는지 따로 확인할 때 사용한다.
18. `보상 지급 준비 검사`는 실제 보상을 지급하지 않고 중간보스2 보상 데이터, 저장 Claim ID, 플레이어 Resolver, 플레이어 LevelUpSystem 연결을 검사한다.
   - 이 버튼은 안전한 사전 검사다.
19. `보상 직접 지급 테스트`는 `HWJ_RewardUtility.TryGrantKillRewardDetailed()`를 직접 호출해 실제 보상 지급 경로를 테스트한다.
   - 이 버튼은 XP/SP 지급과 보상 Claim 상태를 실제로 변경할 수 있다.
   - 한 번 성공한 뒤 다시 누르면 중복 지급 차단 때문에 `AlreadyClaimed`가 정상적으로 뜰 수 있다.
20. `현재 패턴 취소`는 임시 경고선과 히트박스를 정리하고 다음 패턴 테스트를 준비하는 버튼이다.
21. `최근 실행 패턴`은 마지막으로 실제 실행에 성공한 BossPatternData의 ID를 보여준다.
22. `최근 패턴 기록`은 반복 방지 로직이 기억하고 있는 최근 패턴 순서를 보여준다. 같은 패턴이 연속으로 나오지 않는지 확인할 때 사용한다.
23. `패턴 선택 결과`는 보스 패턴 후보를 고를 때 어디서 막혔는지 보여준다. 예를 들어 페이즈, 거리, 쿨타임, 반복 방지 중 어느 조건이 후보를 제외했는지 확인할 수 있다.
24. `패턴 실행 결과`는 선택된 패턴이 실제 실행기 또는 스킬 액션까지 도달했는지 보여준다.
25. `패턴 규칙 결과`는 공통 Rule/Condition 검사를 통과했는지, 실패했다면 어떤 규칙에서 막혔는지 확인하는 값이다.
26. `특수 패턴 실행 중`이 `Yes`이면 `HWJ_MidBossPatternSystem` 같은 커스텀 패턴 실행기가 현재 코루틴을 돌리고 있는 상태다.
27. `패턴 1 충전`은 HP가 15%씩 깎여 소환 패턴 사용 기회가 몇 번 쌓였는지 보여준다. HP를 테스트 버튼으로 바꾸면 이 값도 바로 갱신된다.
28. `패턴 1 다음 HP 기준`은 다음 소환 기회가 쌓이는 HP 비율을 보여준다. HP 상태를 기준으로 갱신된다.
29. `빙의 가능 시체 감지`가 `Yes`이면 패턴 1 소환이 막힌다. 보스방 안에 이미 빙의 가능한 시체가 있기 때문이다. 이 값은 테스트 창에서 짧은 간격으로 갱신된다.
30. `패턴 물리 오버라이드`, `현재 중력`, `복구 예정 중력`은 특수 패턴 중 Gravity Scale이 0으로 바뀌고 패턴 종료 후 원래 값으로 돌아오는지 확인할 때 사용한다.
31. `패턴 기록과 쿨타임 초기화`는 자동 패턴 선택 이력과 쿨타임을 비우는 버튼이다. 같은 상황에서 다른 패턴 선택을 다시 확인할 때 사용한다.
32. `중간보스2 Play Mode 준비 상태 검사`는 현재 Play Mode 씬에서 중간보스2 ID, Boss TypeData, HP 상태, 타겟, 패턴 시스템, Rigidbody2D, BossFlow 연결을 한 번에 검사한다. 실패가 뜨면 Console의 항목을 먼저 고친 뒤 패턴 테스트를 진행한다.
33. `최종 확인 리포트 출력`은 에셋 검증, 현재 씬 BossFlow 검증, Play Mode 체크리스트, 패턴 수동 확인 기록을 한 번에 Console로 출력한다.
   - 실패나 미확인 항목이 남아 있으면 완료 확정 불가로 출력된다.
   - 모든 항목이 통과하고 체크되면 애니메이션, 이펙트, 사운드를 제외한 중간보스2 검증 완료로 출력된다.
34. `패턴 1~7 수동 확인 기록`은 패턴 버튼을 눌러 직접 확인한 항목을 체크하는 영역이다.
   - 패턴 버튼을 눌러 실제 동작을 본 뒤 문제가 없으면 `최근 수동 패턴 확인 처리`를 누른다.
   - 이 기록은 자동 성공 판정이 아니라 수동 확인 누락을 막기 위한 EditorWindow 전용 체크다.
   - 다른 패턴을 다시 확인하려면 `패턴 확인 기록 초기화`로 체크 상태를 비운다.
35. `패턴 1~7 순차 실행 시작`은 패턴 1부터 7까지 강제 실행 버튼을 자동으로 순서대로 눌러주는 보조 기능이다.
   - 이 기능은 Play Mode에서만 동작한다.
   - 시작 시 중간보스2 빠른 테스트 준비를 먼저 실행한다.
   - 각 패턴 코루틴이 끝나면 해당 패턴 확인 기록을 자동으로 체크한다.
   - 화면에서 실제 동작이 이상한지는 사람이 직접 봐야 한다.
   - 중간에 문제가 있으면 `패턴 순차 실행 중지`를 누른다.

### Play Mode 최종 확인 체크리스트

테스트 창에는 `중간보스2 Play Mode 최종 확인 체크리스트`가 있다.
이 체크리스트는 자동 검증 결과가 아니라, 실제 Play Mode에서 사람이 확인한 항목을 표시하기 위한 EditorWindow 전용 기록이다.
Unity를 다시 열거나 창 상태가 초기화되면 체크 값은 보존되지 않는다.

최종 완료 판정 전에는 최소한 아래 흐름을 직접 확인한다.

- 통합 검증 메뉴 또는 버튼에서 성공 메시지가 뜨는지 확인한다.
- Play Mode에서 `boss.mid.02.captain` 보스를 찾고 타겟 자동 연결이 되는지 확인한다.
- 패턴 1로 빙의 가능 몬스터 4마리가 생성되는지 확인한다.
- 1페이즈 패턴 2, 3, 4의 예고 방향과 피해 판정이 시전 시작 후 플레이어를 따라가지 않는지 확인한다.
- HP 50% 조건에서 10초 전환 후 2페이즈로 넘어가는지 확인한다.
- 2페이즈 패턴 5, 6, 7이 실행되는지 확인한다.
- 특수 패턴 중 Gravity Scale이 0으로 바뀌고 패턴 종료 후 원래 값으로 돌아오는지 확인한다.
- HP를 1로 만든 뒤 처치했을 때 BossFlowSystem을 통해 보상/스테이지 진행 흐름으로 넘어가는지 확인한다.

패턴 코드 점검 기준:

- 패턴 5, 6, 7은 실제 경고 표시와 피해 판정이 끝날 때까지 이동 잠금 시간이 유지되도록 설정되어 있다.
- 패턴 7의 착지 충격파 방향은 착지 후 다시 계산하지 않고, 착지 직전에 플레이어 방향을 기준으로 고정한다.

자동 패턴이 나오지 않을 때 확인할 값:

- `타겟`이 `-`이면 보스가 플레이어를 찾지 못한 상태다. 먼저 플레이어 오브젝트에 RootObjectDataResolver가 있고 ObjectType이 Player인지 확인한다.
- `타겟 X 거리`가 `공격 시작 거리`보다 크면 보스는 패턴보다 추적을 우선한다.
- `근거리 판정`이 `No`이면 패턴 2, 패턴 5처럼 근거리 조건을 가진 패턴은 자동 선택되지 않는다.
- `최적 거리`보다 멀면 보스는 플레이어 쪽으로 이동하려고 한다.
- `타겟 보스방 내부`가 `No`이면 보스방 범위 설정이 맞지 않아 자동 전투가 시작되지 않거나 유지되지 않을 수 있다.
- `패턴 선택 결과`에서 `페이즈` 카운트가 높으면 현재 페이즈에서 사용 가능한 패턴이 아니어서 제외된 것이다.
- `패턴 선택 결과`에서 `거리` 카운트가 높으면 근거리/원거리 조건이 맞지 않아서 제외된 것이다.
- `패턴 선택 결과`에서 `쿨타임` 카운트가 높으면 패턴 데이터의 쿨타임이 아직 끝나지 않은 것이다.
- `패턴 선택 결과`에서 `직전반복`, `최근기록`, `분류반복` 카운트가 높으면 같은 패턴 반복 방지 규칙 때문에 제외된 것이다.
- `패턴 실행 결과`에서 `특수 실행기가 실행을 거부`라고 나오면 `Special Pattern Executors` 연결 또는 해당 패턴의 Custom Executor Key를 확인한다.
- `패턴 실행 결과`에서 `SkillActionSystem이 없어`라고 나오면 보스 프리팹의 `HWJ_SkillActionSystem` 연결을 확인한다.
- `패턴 실행 결과`에서 `실행할 특수 실행기나 스킬 액션이 없습니다`라고 나오면 BossPatternData의 실행 방식이 비어 있는 것이다.
- `패턴 규칙 결과`가 실패 메시지를 보여주면 거리 문제가 아니라 Rule/Condition 검사가 막은 것이다.

## 중간보스2 데이터 검증 방법

1. Unity 상단 메뉴에서 `Tools/HWJ/Boss/Validate Mid Boss 2 Assets`를 실행한다.
2. Console에 `중간보스2 에셋 검증 성공`이 뜨면 RootObjectData, TypeData, GameplayDatabase, 프리팹, 물리 패턴 1~7, 소환 몬스터 프리팹/RootObjectData 연결이 정상이다.
3. 검증 실패가 뜨면 Console의 항목을 보고 비어 있는 프리팹, 잘못 연결된 RootObjectData, 꺼진 패턴 시스템을 수정한다.
4. 같은 검증은 `Mid Boss Physical Test Window` 안의 `중간보스2 에셋 검증` 버튼으로도 실행할 수 있다.
5. Unity를 닫은 상태에서 배치 검증을 돌릴 때는 `-executeMethod HWJ_MidBossPhysicalTestWindow.RunMidBoss2AssetValidationBatch`를 사용한다.
6. 중간보스1과 중간보스2를 같이 검증할 때는 `-executeMethod HWJ_MidBossPhysicalTestWindow.RunAllMidBossAssetValidationBatch`를 사용한다.
7. 배치 검증은 실패 항목이 하나라도 있으면 예외를 발생시켜 검증 실패로 종료한다.

### Console에 이전 컴파일 오류가 남아 있을 때

Unity가 스크립트 변경을 아직 다시 Refresh하지 않았으면 Console 또는 `Editor.log`에 이전 컴파일 오류가 남아 있을 수 있다.
이 경우 아래 순서로 확인한다.

1. `HWJ_MidBossPhysicalTestWindow.cs`의 357번째 줄이 `TickPatternSequenceRunner();`인지 확인한다.
2. `UpdatePatternSequence`라는 호출이 파일 안에 남아 있지 않은지 확인한다.
3. Unity에서 `Assets > Refresh`를 실행하거나 Play Mode를 끄고 다시 스크립트 리컴파일을 기다린다.
4. 새로 뜨는 Console 오류가 없으면 이전 로그에 남은 오류는 과거 컴파일 결과다.
5. 현재 코드 기준으로는 Unity가 사용하는 `HWJ.Editor.rsp` 기반 별도 컴파일 검사를 통과했다.

## 현재 씬 중간보스2 플로우 검증 방법

중간보스2는 보스 체력이 0이 되었을 때 단순히 죽는 것에서 끝나면 안 된다.
보스 사망 이벤트가 `HWJ_BossFlowSystem`을 통해 스테이지 클리어, 보스 처치 이벤트, 저장 진행 데이터로 이어져야 한다.

검증 방법:

1. 중간보스2가 배치된 씬을 연다.
2. Unity 상단 메뉴에서 `Tools/HWJ/Boss/Validate Active Scene Mid Boss 2 Flow`를 실행한다.
3. `Mid Boss Physical Test Window` 안의 `현재 씬 중간보스2 플로우 검증` 버튼으로도 실행할 수 있다.
4. 성공 메시지가 뜨면 현재 씬에는 중간보스2를 참조하는 `HWJ_BossFlowSystem`이 있고, 보스 사망을 스테이지 진행으로 넘길 수 있는 상태다.

에셋과 현재 씬을 한 번에 확인하는 방법:

1. 중간보스2가 배치된 씬을 연다.
2. Unity 상단 메뉴에서 `Tools/HWJ/Boss/Validate Mid Boss 2 Setup And Active Scene`를 실행한다.
3. `Mid Boss Physical Test Window` 안의 `중간보스2 에셋 + 현재 씬 플로우 검증` 버튼으로도 실행할 수 있다.
4. 이 검증은 `Validate Mid Boss 2 Assets`와 `Validate Active Scene Mid Boss 2 Flow`를 한 번에 실행한다.
5. 성공 메시지가 뜨면 Play Mode 패턴 테스트 전 단계의 데이터, 프리팹, 현재 씬 보스 사망 플로우 연결이 정상이다.

이 검증에서 확인하는 것:

- 현재 씬에 `boss.mid.02.captain` ID를 가진 중간보스2가 있는지 확인한다.
- 현재 씬에 `HWJ_BossFlowSystem`이 있는지 확인한다.
- `HWJ_BossFlowSystem`의 `Boss Resolver`가 중간보스2를 참조하는지 확인한다.
- `HWJ_BossFlowSystem`의 `Boss Brain`이 중간보스2를 참조하는지 확인한다.
- `StageProgressionSystem` 참조가 비어 있지 않은지 확인한다.
- `Auto Complete Boss Flow On Combat Death`가 켜져 있는지 확인한다.
- 중간보스2가 보스 브레인 자동 시작을 사용한다면 `Require Boss Battle State For Combat Death`가 꺼져 있는지 확인한다.
- 보스 입장 조건에서 Entry Trigger를 요구한다면 `Boss Entry Trigger Active`가 켜져 있는지 확인한다.

실패했을 때 의미:

- `HWJ_BossFlowSystem`이 없으면 보스를 죽여도 스테이지 클리어로 이어지지 않는다.
- Boss Resolver 또는 Boss Brain 참조가 다른 보스를 보고 있으면 중간보스2 사망 이벤트를 무시한다.
- `Require Boss Battle State For Combat Death`가 켜져 있는데 보스 브레인이 직접 전투를 시작하면, 보스를 죽여도 BossBattle 상태가 아니라는 이유로 클리어 처리가 막힐 수 있다.

검증에서 확인하는 중간보스2 거리/전투 설정:

- HP는 7500이어야 한다.
- 공격력과 기본 데미지는 100이어야 한다.
- 방어력은 40이어야 한다.
- 이동속도는 15여야 한다.
- Model Prefab은 비어 있으면 안 된다. 최종 아트가 아니어도 임시 모델 프리팹은 필요하다.
- Damage Type은 물리 공격 컨셉에 맞게 `Physical`이어야 한다.
- 보스는 플레이어 공격 대상이므로 `Can Be Targeted`가 켜져 있어야 한다.
- 보스는 빙의/상호작용 대상이 아니므로 `Can Interact`가 꺼져 있어야 한다.
- 경험치 보상과 스킬 포인트 보상은 음수가 아니어야 한다.
- 보스방 크기가 0보다 커야 한다.
- 공격 시작 거리가 0보다 커야 한다.
- 근거리 스킬 거리가 0보다 커야 한다.
- 공격 시작 거리는 근거리 스킬 거리보다 작으면 안 된다.
- 최적 공격 거리가 0보다 커야 한다.
- 2페이즈 HP 비율은 0보다 크고 1보다 작아야 한다.
- 페이즈 전환 시간은 현재 기획 기준상 10초 정도여야 한다.
- Phase Transform의 Can Transform은 켜져 있어야 한다.
- Phase Transform HP 비율은 0.5여야 한다.
- 2페이즈 변신 모델 ID는 `mid_boss_02_phase_2`여야 한다.
- `HWJ_BossPatternSystem`의 `Auto Use Patterns`는 꺼져 있어야 한다.
- `HWJ_BossPatternSystem`의 `Special Pattern Executors`에는 `HWJ_MidBossPatternSystem`이 연결되어 있어야 한다.
- 중간보스2는 활 보스 전용 `Stage One Pattern System`을 사용하지 않으므로 해당 참조는 비어 있어야 한다.
- `Use Gameplay Pattern Rule`은 켜져 있어야 한다.
- 중간보스2 프리팹에는 Rigidbody2D, Collider2D, SpriteRenderer, Animator가 있어야 한다.
- Rigidbody2D는 Dynamic, Simulated On, Gravity Scale 0보다 큰 값, Freeze Rotation On이어야 한다.
- 특수 패턴 실행 중에는 코드가 Gravity Scale을 임시로 0으로 바꾸고, 패턴 종료/취소 시 원래 값으로 복구한다.
- 중간보스2 프리팹에는 RootObjectDataResolver, RuntimeStatusSystem, CombatSystem, CombatExecutionSystem, SkillActionSystem이 있어야 한다.
- 중간보스2 프리팹에는 RuntimeSaveIdentity가 있어야 한다.
- RuntimeSaveIdentity의 Stable Instance Id는 `boss_mid_02_captain_scene`, Root Object Id는 `boss.mid.02.captain`이어야 한다.
- 이 값으로 생성되는 보상 Claim ID는 `boss.mid.02.captain:boss_mid_02_captain_scene`이다.
- 중간보스2 프리팹에는 BossBrainSystem, BossPatternSystem, MidBossPatternSystem, CharacterMotionSystem이 있어야 한다.
- 중간보스2 프리팹에는 보스 대사, 카메라 포커스, 중복 생성 방지 컴포넌트가 있어야 한다.
- 중간보스2는 몬스터가 아니므로 EnemyAttackSystem, MonsterAISystem, EnemyNavigationSystem이 붙어 있으면 안 된다.
- 중간보스2 대사는 검/기사단장 컨셉을 사용해야 하며, 중간보스1 격투가용 주먹/격투 대사가 남아 있으면 검증 실패로 처리한다.

검증에서 확인하는 물리 패턴 조건:

- 패턴 1은 1페이즈와 2페이즈에서 모두 사용 가능해야 한다.
- 패턴 2, 3, 4는 1페이즈에서만 사용 가능해야 한다.
- 패턴 5, 6, 7은 2페이즈에서만 사용 가능해야 한다.
- 모든 물리 패턴은 `mid_boss_physical` 실행 키를 사용해야 한다.
- 모든 물리 패턴의 유효 쿨타임은 5초여야 한다.
- 패턴 2와 5는 근거리 조건, 나머지는 전체 거리 조건이어야 한다.
- `HWJ_MidBossPatternSystem`의 실행 키는 `mid_boss_physical`이어야 한다.
- `HWJ_BossPatternSystem`의 RuleExecutionCore ID는 `boss_pattern_execution`이어야 한다.
- `HWJ_BossPatternSystem`의 GameplayRule ID는 `boss_can_use_combat_pattern`이어야 한다.
- 위 RuleExecutionCore와 GameplayRule은 `HWJ_GameplayDatabase.asset`에서 ID로 조회 가능해야 한다.
- 패턴 1은 HP 15% 감소마다 소환 기회가 쌓이고, 소환 몬스터 수는 4마리여야 한다.
- 패턴 1 호루라기 대기 시간은 현재 기획 기준상 2초여야 한다.
- 패턴 1 소환 몬스터는 `canBePossessed`가 켜져 있고, 보스전 중 바로 빙의할 수 있도록 `Requires Defeated State`가 꺼져 있어야 한다.
- 패턴 4의 고정 피해 비율은 플레이어 최대 체력의 40%여야 한다.
- 패턴 4 종료 후 그로기 시간은 3초여야 한다.
- 패턴 6 은신 전 대기 시간은 2초여야 한다.
- 각 패턴의 ID와 AnimationId가 중간보스2 기획 기준과 일치해야 한다.

## 중간보스2 FighterBoss 잔여물 정리

중간보스2는 물리형 보스이므로 중간보스1의 격투가 전용 컴포넌트가 남아 있으면 안 된다.

현재 `HWJ_MidBoss2_Runtime_Prefab.prefab`에서는 `AttackRoot`, FighterBoss 전용 히트박스, FighterBoss 전용 Telegraph 오브젝트, FighterBoss 전용 패턴 실행 컴포넌트를 제거한 상태다.

체력바도 `HWJ_FighterBossHealthBarSystem`이 아니라 범용 `HWJ_BossHealthBarSystem`을 사용한다.

아래 메뉴는 나중에 프리팹을 다시 복사하거나 작업 중 잔여물이 생겼을 때 재정리용으로 사용한다.

정리 방법:

1. Unity 상단 메뉴에서 `Tools/HWJ/Boss/Clean Mid Boss 2 Fighter Residue`를 실행한다.
2. Console에 제거된 컴포넌트와 오브젝트 목록이 출력된다.
3. 그 뒤 `Tools/HWJ/Boss/Validate Mid Boss 2 Assets`를 다시 실행한다.
4. 검증 성공이 뜨면 중간보스2 프리팹에는 물리형 패턴 실행에 필요한 구성만 남아 있는 상태다.

이 메뉴가 제거하는 것:

- `HWJ_FighterBossComboSystem`
- `HWJ_FighterBossChargeSystem`
- `HWJ_FighterBossUppercutSystem`
- `HWJ_FighterBossGroundSlamSystem`
- `HWJ_FighterBossPhaseTwoPatternSystem`
- `HWJ_FighterBossDeathSystem`
- `HWJ_FighterBossAnimatorSystem`
- `HWJ_FighterBossAnimationEvents`
- `HWJ_FighterBossHealthBarSystem`
- FighterBoss 전용 Hitbox 오브젝트
- FighterBoss 전용 임시 Telegraph 오브젝트

이 메뉴가 유지하는 것:

- `HWJ_BossBrainSystem`
- `HWJ_BossPatternSystem`
- `HWJ_MidBossPatternSystem`
- `HWJ_RuntimeStatusSystem`
- `HWJ_CombatSystem`
- `HWJ_SkillActionSystem`
- `HWJ_BossHealthBarSystem`
- 보스 카메라, 대사, 공통 상태/전투 컴포넌트

## 중간보스 전체 검증 방법

1. Unity 상단 메뉴에서 `Tools/HWJ/Boss/Validate All Mid Boss Assets`를 실행한다.
2. 이 검증은 중간보스1과 중간보스2를 서로 다른 기준으로 검사한다.
3. 중간보스1은 격투가형 보스 기준으로 검사한다.
   - RootObjectData와 TypeData ID
   - GameplayDatabase 등록 여부
   - `Use Two Bar Phase Health` 켜짐
   - 격투가 보스 전용 패턴 실행 컴포넌트 존재 여부
   - BossPatternSystem의 Patterns와 Special Pattern Executors 존재 여부
4. 중간보스2는 물리형 보스 기준으로 검사한다.
   - RootObjectData와 TypeData ID
   - GameplayDatabase 등록 여부
   - `Use Two Bar Phase Health` 꺼짐
   - 모델 프리팹, 물리 데미지 타입, 공격 대상 설정
   - 보스 런타임 필수 컴포넌트
   - 몬스터 AI/기본 공격 컴포넌트 미사용 여부
   - `HWJ_MidBossPhysical_Pattern1~7` 연결 여부
   - 패턴 1 소환 몬스터 프리팹과 RootObjectData 연결 여부

## 현재 완료 판단

현재 기준으로 중간보스2는 애니메이션, 이펙트, 사운드를 제외한 데이터/프리팹/패턴 실행 구조가 준비된 상태다.

완료로 볼 수 있는 항목:

- 중간보스2 RootObjectData가 있고 기획 수치가 반영되어 있다.
- 중간보스2 TypeData가 있고 보스방, 추적, 공격 거리, 50% 페이즈 전환, 10초 전환 시간을 가진다.
- 중간보스2 Runtime Prefab이 있고 공통 보스 브레인과 물리 중간보스 패턴 실행기를 사용한다.
- 중간보스2는 기본 몬스터 공격을 사용하지 않고 보스 패턴만 사용한다.
- 중간보스2 Runtime Prefab에는 저장 ID가 있어 보스 처치 보상 중복 지급을 막을 수 있다.
- 물리 패턴 1~7이 데이터로 분리되어 있고 페이즈/거리/쿨타임/실행 키 조건을 가진다.
- 패턴 1은 HP가 15%씩 깎였고 보스방 안에 빙의 가능한 시체가 없을 때 소환 패턴을 사용할 수 있다.
- 패턴 5, 6, 7은 실제 경고 표시와 피해 판정이 끝날 때까지 이동 잠금이 유지되도록 코드 시간이 맞춰져 있다.
- 패턴 7의 착지 충격파 방향은 착지 직전 플레이어 방향으로 고정된다.
- 중간보스2 프리팹 대사는 검/기사단장 컨셉으로 정리되어 있고, 중간보스1의 주먹/격투가 대사가 남아 있지 않다.
- 검증 도구가 중간보스2 데이터, 패턴, 소환 몬스터, 프리팹 필수 연결을 검사한다.
- 통합 검증 메뉴가 에셋 검증과 현재 씬 보스 플로우 검증을 한 번에 실행할 수 있다.
- 테스트 창에서 Play Mode 빠른 준비, 준비 상태 검사, 패턴 1~7 순차 실행, 패턴별 수동 확인 기록을 사용할 수 있다.
- 테스트 창에서 HP 0 사망 이벤트 브리지와 BossFlow 직접 완료 경로를 따로 검증할 수 있다.
- 테스트 창에서 중간보스2 보상 지급 준비 상태와 실제 RewardUtility 지급 경로를 따로 검증할 수 있다.
- 테스트 창에서 최종 확인 리포트를 출력해 남은 미확인 항목을 한 번에 확인할 수 있다.
- 현재 소스 기준 Editor 어셈블리와 Runtime 어셈블리의 별도 컴파일 검사를 통과했다.

아직 완료로 확정할 수 없는 항목:

- 실제 보스방 씬에 배치한 상태의 Play Mode 검증
- 패턴 1~7의 실시간 판정 위치와 피해 적용 검증
- 50% 페이즈 전환이 실제 플레이 중 한 번만 실행되는지 검증
- 보스 사망, 보상 지급, 다음 진행 연결 검증

이번 목표에서 후속 작업으로 남긴 항목:

- 최종 애니메이션 연결
- 최종 이펙트 연결
- 최종 사운드 연결

## 애니메이션 담당자가 연결할 위치

1. 보스 본체 Animator
   - 프리팹의 `Animator` 컴포넌트에 최종 컨트롤러를 넣는다.

2. 보스 이동/피격/사망 모션
   - `HWJ_CharacterMotionSystem`에서 Animator 파라미터 이름을 확인하고 컨트롤러와 맞춘다.

3. 패턴별 공격 타이밍
   - 현재 물리 중간보스 패턴은 코드 타이머로 임시 판정을 실행한다.
   - 최종 애니메이션이 들어오면 각 패턴의 경고 시간, 차징 시간, 후딜 시간을 Inspector에서 애니메이션 길이에 맞춘다.

4. 이펙트와 사운드
   - 현재는 임시 경고선과 판정으로만 확인한다.
   - 최종 VFX/SFX 프리팹과 AudioClip은 각 패턴 시스템 또는 애니메이션 이벤트에 연결한다.

## 직접 수정하는 주요 값

- `HWJ_MidBoss2_RootObjectData.asset`
  - HP, 공격력, 방어력, 이동속도, 보상 수치

- `HWJ_MidBoss2_TypeData.asset`
  - 보스방 크기
  - 공격 시작 거리
  - 근거리 판정 거리
  - 2페이즈 전환 체력 비율
  - 페이즈 전환 시간
  - 그로기 조건

- `HWJ_MidBoss2_Runtime_Prefab.prefab`
  - 소환 몬스터 프리팹
  - 소환 몬스터 RootObjectData
  - 패턴 경고 시간
  - 패턴별 차징 시간
  - 패턴별 피해 배율
  - 충격파 범위

## 확인해야 하는 항목

- 중간보스2 프리팹의 `Root Object Data`가 `HWJ_MidBoss2_RootObjectData`인지 확인한다.
- `HWJ_BossPatternSystem`의 Patterns가 `HWJ_MidBossPhysical_Pattern1~7`인지 확인한다.
- `HWJ_MidBossPatternSystem`이 켜져 있는지 확인한다.
- `HWJ_MidBossPatternSystem`의 `소환 몬스터 프리팹 목록`이 검, 방패, 창, 활 빙의 가능 몬스터 프리팹으로 채워져 있는지 확인한다.
- `HWJ_MidBossPatternSystem`의 `소환 몬스터 RootObjectData 목록`이 위 프리팹 순서와 같은 검, 방패, 창, 활 데이터인지 확인한다.
- `Use Two Bar Phase Health`가 꺼져 있는지 확인한다.
- 보스는 빙의 대상이 아니므로 `canBePossessed`가 꺼져 있어야 한다.

## 남은 작업

- 중간보스2 전용 외형 아트 교체
- 중간보스2 전용 Animator Controller 연결
- 패턴별 최종 이펙트 프리팹 연결
- 패턴별 사운드 연결
- 실제 보스방 씬 배치 후 수동 플레이 검증
