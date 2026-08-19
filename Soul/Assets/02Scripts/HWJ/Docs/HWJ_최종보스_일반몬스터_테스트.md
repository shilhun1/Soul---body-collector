# HWJ 최종보스 및 일반 몬스터 테스트

## 1. 테스트 전 오류 확인

1. Unity가 Play Mode라면 중지한다.
2. `Assets > Refresh`를 실행한다.
3. Console의 `Clear`를 누른다.
4. `Tools > HWJ > Test > 최종보스 및 일반 몬스터 구성 검증`을 실행한다.
5. 아래 메시지가 나오면 프리팹과 데이터 연결 검증을 통과한 것이다.

```text
[HWJ 전투 테스트 검증] PASS
```

검증 메뉴는 에셋을 수정하지 않는다. 다음 항목만 읽어서 검사한다.

- 최종보스·일반 몬스터 프리팹 및 현재 열린 씬의 Missing Script
- RootObjectData 연결
- 런타임 상태, 전투, AI, 빙의 필수 컴포넌트
- 최종보스 패턴 Definition 10개
- 최종보스 빙의 불가와 고정형 이동 정책
- 2-3 현재 빙의 정신력 10% 피해
- 2-4 포탈 2개, 포탈당 4마리
- 슬라임과 쥐의 생체 빙의 가능 설정

현재 열린 씬에서 Missing Script를 찾으면 Console에
`씬 에셋 경로 > Hierarchy 오브젝트 경로`가 함께 출력된다.

## 2. 공통 테스트 씬 준비

기존 `HWJ` 씬을 사용하거나 별도의 테스트용 복사본을 사용한다.

1. 바닥 GameObject에 `BoxCollider2D` 또는 `TilemapCollider2D`가 있는지 확인한다.
2. 현재 프로젝트에는 `Ground` 레이어가 없으므로 바닥 레이어는 `Default`로 둔다.
3. 기존 플레이어가 없다면 `HWJ_Runtime_Player_Soul.prefab`을 배치한다.
4. 기존 게임 시스템이 없다면 `HWJ_Runtime_AllSystems_DropIn.prefab`을 한 번만 배치한다.
5. 플레이어 오브젝트의 Tag는 `Player`인지 확인한다.
6. 플레이어, 보스, 몬스터가 바닥 Collider 위에서 시작하도록 배치한다.

같은 시스템 프리팹이나 플레이어 프리팹을 두 번 배치하면 입력·이벤트·저장 서비스가 중복될 수 있다.

## 3. 일반 몬스터 직접 배치

다음 프리팹을 Project 창에서 Hierarchy로 드래그한다.

- `Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_General_Slime.prefab`
- `Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_General_Rat.prefab`

플레이어와 각 몬스터 사이를 약 5~7 Unity 단위로 둔다.

### 확인 순서

1. Play Mode를 시작한다.
2. 플레이어가 인식 범위 밖에 있을 때 몬스터가 생성 방향을 유지하며 정지하는지 확인한다.
3. 빙의 육신 상태로 몬스터의 전방 약 3m 안에 접근한다.
4. 몬스터가 `Idle > Detect > Approach > AttackPrepare > Attack > Recovery > Repath` 순서로 움직이는지 Inspector의 `HWJ_MonsterAISystem > Current State`에서 확인한다.
5. 인식된 플레이어가 몬스터의 반대편으로 이동할 때만 몬스터 방향이 플레이어 위치에 맞춰 바뀌는지 확인한다.
6. 슬라임은 접촉 중 약 1.2초 간격으로 피해를 주는지 확인한다.
7. 쥐는 공격 준비 뒤 물기 판정이 한 번만 발생하는지 확인한다.
8. 몬스터와 플레이어 사이에 Default 레이어 벽을 놓았을 때 시야가 차단되는지 확인한다.
9. 플레이어를 높은 발판으로 이동시켰을 때 몬스터가 점프하지 않고 아래에서 대기하는지 확인한다.
10. 최대 추적 거리를 벗어나면 몬스터가 생성 위치로 복귀하며 외형 방향은 새로 뒤집히지 않는지 확인한다.

### 일반 몬스터 빙의

1. 플레이어를 영혼 상태로 만든다.
2. 살아 있는 슬라임 또는 쥐의 빙의 범위에 들어간다.
3. 빙의 키를 눌러 미니게임을 시작하고 성공시킨다.
4. 해당 몬스터 외형과 능력으로 조작되는지 확인한다.
5. 빙의 정신력이 시간에 따라 감소하는지 확인한다.
6. 정신력 0이면 플레이어가 영혼으로 나오고 몬스터가 남은 HP로 적 AI 상태에 복귀하는지 확인한다.
7. 복귀한 동일 몬스터에는 다시 빙의할 수 없는지 확인한다.
8. 빙의체 HP를 0으로 만들면 몬스터가 제거되고 재빙의할 수 없는지 확인한다.

## 4. 일반 몬스터 스포너 테스트

스폰 테이블의 일반 몬스터 ID는 다음과 같다.

```text
Enemy_General_Slime
Enemy_General_Rat
```

1. 씬에 `HWJ_SpawnPoint`를 만든다.
2. Spawn ID에 위 ID 중 하나를 정확히 입력한다.
3. `HWJ_SpawnTableData`의 같은 ID 항목에서 `Spawn On Start`를 켠다.
4. Play Mode에서 지정 위치에 몬스터가 한 번 생성되는지 확인한다.
5. 여러 마리 테스트 시 SpawnPoint를 복제하고 Stable Instance ID가 중복되지 않게 한다.

## 5. 최종보스 배치

다음 프리팹을 Hierarchy로 드래그한다.

```text
Prefabs/Generated/Bosses/HWJ_FinalBoss_Runtime_Prefab.prefab
```

1. 보스를 전투방 중앙 또는 기획상 고정 위치에 둔다.
2. 이 위치가 2-3 돌진 후 복귀 위치가 된다.
3. `PortalAnchor_Left`, `PortalAnchor_Right`를 소환 위치로 옮긴다.
4. `HWJ_FinalBossPatternSystem > 장애물 레이어`는 현재 지형 레이어인 `Default`로 지정한다.
5. `HWJ_FinalBoss_TypeData > 보스방 크기`가 실제 전투방을 포함하도록 조정한다.
6. 플레이어를 보스방 안, 보스와 6~10 Unity 단위 떨어진 위치에 둔다.

## 6. 최종보스 기본 테스트

1. 빙의 육신 상태로 보스방에 들어간다.
2. `HWJ_BossBrainSystem > Current State`가 `Attack`으로 전환되는지 확인한다.
3. `HWJ_BossPatternSystem > Last Pattern Execution Result`에서 실행 성공을 확인한다.
4. `HWJ_FinalBossPatternSystem > Active Pattern Id`로 현재 패턴을 확인한다.
5. 1페이즈에서 1-1부터 1-5 중 하나가 실행되는지 확인한다.
6. 보스 HP를 최대 HP의 50% 이하로 줄여 2페이즈에 진입시킨다.
7. 이후 2-1부터 2-5만 선택되는지 확인한다.
8. 2-3 외에는 보스 루트가 처음 배치 위치를 유지하는지 확인한다.
9. 영혼 상태로 전환하면 진행 중 패턴이 취소되고 새 패턴이 시작되지 않는지 확인한다.

## 7. 최종보스 핵심 패턴 테스트

### 2-3 실제 돌진과 정신력

1. `HWJ_FinalBoss_P2_03_BlackFlameCharge`의 쿨타임을 임시로 0으로 낮춘다.
2. 다른 2페이즈 패턴은 임시로 비활성화하거나 쿨타임을 크게 높인다.
3. 플레이어의 현재 빙의 정신력 최대치와 현재값을 기록한다.
4. 돌진 예고 후 보스 루트 Rigidbody2D가 플레이어 위치로 실제 이동하는지 확인한다.
5. 적중 시 HP가 아닌 현재 빙의 정신력이 최대치의 10%만 감소하는지 확인한다.
6. 돌진 종료 또는 실패 뒤 보스가 처음 위치로 복귀하는지 확인한다.

### 2-4 총 8마리 소환

1. `HWJ_FinalBoss_P2_04_DoublePortalSummon`만 선택되기 쉽게 쿨타임을 조정한다.
2. Hierarchy에서 붉은 포탈 표시가 2개 생성되는지 확인한다.
3. 왼쪽 포탈에서 4마리, 오른쪽 포탈에서 4마리가 생성되는지 센다.
4. 생성된 8마리가 플레이어를 추적하고 살아 있는 상태에서 빙의 가능한지 확인한다.

### 방어막

1. 1-3 또는 2-2가 실행될 때 `HWJ_FinalBossBarrierSystem > Barrier Active`를 확인한다.
2. 방어막 공격 중 보스 HP가 감소하지 않는지 확인한다.
3. 1페이즈 방어막은 최대 HP 5%, 2페이즈 방어막은 최대 HP 10% 피해로 파괴되는지 확인한다.
4. 방어막 파괴 후 다음 공격부터 보스 HP가 감소하는지 확인한다.

## 8. 현재 확인된 Console 오류

### 삭제된 NoCorpse 프리팹 오류

```text
ArgumentException: HWJ_Runtime_Enemy_NoCorpse_Sword.prefab does not exist
```

원인은 HWJ 일반 몬스터가 아니라 다음 HWJ 밖 자동 바인더가 삭제된 옛 프리팹을 계속 열기 때문이다.

```text
Assets/02Scripts/hys/Animation/Editor/hys_RuntimeReadyPrefabAnimationBinder.cs
```

담당자가 `WireEnemyPrefab`과 `WireCorpsePrefab` 실행 전에 `AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null`이면 건너뛰게 수정해야 한다.
옛 NoCorpse 프리팹을 다시 생성하는 것은 현재 생체 빙의 기획과 충돌하므로 해결 방법으로 사용하지 않는다.

### Sword Death 애니메이션 해시 오류

```text
InvalidOperationException: 보호 클립 해시가 변경되었습니다: hys_Enemy_Sword_Death.anim
```

오류 발생 파일은 다음과 같다.

```text
Assets/02Scripts/hys/Animation/Editor/hys_SwordMonsterAnimationValidator.cs
```

애니메이션 담당자가 변경된 Death 클립이 의도한 최종본인지 확인해야 한다.
의도한 변경이면 검증기의 `ProtectedHashes`에 현재 SHA-256을 갱신하고, 의도하지 않은 변경이면 해당 클립을 원본으로 복구한다.
HWJ 시스템에서 검증기를 우회하거나 예외를 숨기면 실제 애니메이션 손상을 감지할 수 없으므로 그렇게 처리하지 않는다.

현재 파일에서 확인한 SHA-256은 다음과 같다.

```text
FF4F62AFA546A1A6FC3EFF173B0D7C3ABEDE16971E19D39991C2AA865C2288C0
```

### 영혼 상태 공격 경고

```text
Attack requires a possessed body.
```

영혼 상태에서는 공격할 수 없다는 현재 기획을 알리는 정상 경고다. 빙의 후 공격하면 나타나지 않아야 한다.

## 9. 테스트 종료 기준

- 구성 검증 메뉴 PASS
- HWJ C# 컴파일 오류 0개
- Missing Script 0개
- 슬라임·쥐 인식 전 방향 고정, 인식 후 플레이어 방향 전환, 공격, 복귀 확인
- 살아 있는 일반 몬스터 빙의와 정신력 0 적 복귀 확인
- 최종보스 1·2페이즈 패턴 분리 확인
- 2-3 실제 돌진과 현재 빙의 정신력 10% 감소 확인
- 2-4 총 8마리 소환 확인
- 방어막 중 보스 HP 차단 확인
- 영혼 상태에서 보스와 일반 몬스터가 공격하지 않음 확인
