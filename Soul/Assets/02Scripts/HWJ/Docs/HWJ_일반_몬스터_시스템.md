# HWJ 일반 몬스터 시스템

## 1. 적용 범위

일반 몬스터 기획서의 공통 규칙과 슬라임, 쥐 몬스터를 데이터 중심 구조로 구현했다.
애니메이션과 시각 이펙트는 실제 에셋이 준비된 뒤 연결할 수 있도록 실행 지점만 제공한다.

- 모든 일반 몬스터는 살아 있는 상태에서 빙의할 수 있다.
- 보스 빙의 금지 규칙은 유지한다.
- 영혼 상태 플레이어는 인식하거나 공격하지 않는다.
- 몬스터는 플레이어를 인식하기 전까지 생성 방향을 유지하며 제자리에서 대기한다.
- 플레이어를 인식한 동안에만 플레이어의 X축 위치에 맞춰 방향을 전환한다.
- 추적 한계를 벗어나면 생성 위치로 복귀하지만, 복귀 방향에 맞춰 외형을 뒤집지는 않는다.
- 플레이어가 다른 높이의 발판에 있으면 아래에서 대기한다.
- 피격되면 기존 시야 판정 밖에서도 공격자를 인식한다.

## 2. 생성된 일반 몬스터

| 몬스터 | 임시 HP | 이동 속도 | 공격력 | 방어력 | 빙의 정신력 | 기본 공격 방식 |
|---|---:|---:|---:|---:|---:|---|
| 슬라임 | 60 | 2.2 | 8 | 1 | 60 | 접촉 중 1.2초 간격 피해 |
| 쥐 | 45 | 3.4 | 10 | 0 | 50 | 물기 판정 시점에 1회 피해 |

현재 수치는 임시 밸런스 값이다. 코드가 아니라 각 `Enemy TypeData` SO에서 변경한다.

## 3. 공통 FSM 흐름

| 상태 | 역할 |
|---|---|
| Idle | 생성 방향을 유지하고 제자리에서 인식 대상을 확인한다. |
| Patrol | 이전 데이터와 외부 호출 호환용 상태이며, 진입하면 방향을 바꾸지 않고 Idle로 복귀한다. |
| Detect | 플레이어가 시야 규칙을 만족하는지 판단한다. |
| Approach | 같은 발판의 플레이어에게 X축으로 접근한다. |
| WaitBelowPlatform | 플레이어가 높은 발판에 있으면 아래에서 바라보며 대기한다. |
| AttackPrepare | 공격 전 준비 시간과 예고를 처리한다. |
| Attack | 기본 공격 또는 등록된 스킬을 실행한다. |
| Recovery | 공격 후딜 동안 다음 행동을 막는다. |
| Repath | 공격 후 대상을 다시 평가한다. |
| ReturnHome | 추적 한계를 벗어나거나 대상을 잃으면 생성 위치로 복귀한다. |
| HitStun | 경직이 적용된 동안 행동을 중지한다. |
| Dead | AI와 공격을 중지한다. |

## 4. 인식 규칙

기본값은 다음과 같다.

- 전방 X축 거리: 3m
- 시야각: 180도
- 같은 발판 높이 허용 오차: 0.4m
- 지형 가림 확인: 사용
- 생성 위치 기준 최대 추적 거리: 8m
- 피격 어그로: 사용
- 영혼 상태 플레이어: 무시

조정 위치:

- `Enemy TypeData > 시야 데이터`
- `Enemy TypeData > 복귀 데이터`
- `Enemy TypeData > 기본 공격 데이터`

`Enemy TypeData > 순찰 데이터`는 이전 에셋 호환을 위해 남아 있지만 현재 고정 대기 정책에서는 실행에 사용하지 않는다.

## 5. 데이터 에셋 위치

### 슬라임

- 유형 데이터: `ScriptableObjects/TypeData/Enemy/General/HWJ_Enemy_General_Slime_TypeData.asset`
- 최상위 데이터: `ScriptableObjects/RootObjects/Enemies/HWJ_Enemy_General_Slime_RootObjectData.asset`
- 실행 프리팹: `Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_General_Slime.prefab`

### 쥐

- 유형 데이터: `ScriptableObjects/TypeData/Enemy/General/HWJ_Enemy_General_Rat_TypeData.asset`
- 최상위 데이터: `ScriptableObjects/RootObjects/Enemies/HWJ_Enemy_General_Rat_RootObjectData.asset`
- 실행 프리팹: `Prefabs/Generated/RuntimeReady/Enemies/HWJ_Runtime_Enemy_General_Rat.prefab`

### 공통 등록 데이터

- 게임 데이터베이스: `ScriptableObjects/Database/HWJ_GameplayDatabase.asset`
- 스폰 테이블: `ScriptableObjects/HWJ_SpawnTableData.asset`
- 전체 시스템 프리팹: `Prefabs/Generated/RuntimeReady/HWJ_Runtime_AllSystems_DropIn.prefab`

## 6. 씬에서 사용하는 방법

### 직접 배치

1. 실행 프리팹을 Hierarchy로 드래그한다.
2. 바닥 오브젝트가 `Ground` 레이어인지 확인한다. 현재 프로젝트처럼 `Ground` 레이어가 없으면 `Default` 레이어를 사용한다.
3. 몬스터의 발밑 지면 탐지 범위 안에 Collider2D가 있는지 확인한다.
4. 플레이어 오브젝트의 태그가 `Player`인지 확인한다.
5. Play Mode에서 인식 전 방향 고정, 플레이어 인식 후 방향 전환, 접근, 공격, 복귀를 확인한다.

### 스포너 사용

스폰 테이블 ID는 다음과 같다.

- `Enemy_General_Slime`
- `Enemy_General_Rat`

자동으로 기존 씬에 몬스터가 추가되는 것을 막기 위해 두 항목의 `Spawn On Start`는 기본적으로 꺼져 있다.
자동 생성을 원하면 스폰 테이블에서 해당 항목을 켜고, 같은 ID를 사용하는 `HWJ_SpawnPoint`를 씬에 둔다.

## 7. 빙의와 육신 종료 규칙

### 살아 있는 일반 몬스터 빙의

1. 영혼 상태에서 빙의 범위에 들어간다.
2. 생체 빙의 미니게임에 성공한다.
3. 대상의 현재 HP와 전용 데이터를 플레이어 런타임 육신 상태로 전달한다.
4. 빙의 중 대상 정신력이 시간에 따라 감소한다.

### 정신력 0

- 플레이어는 영혼 상태로 돌아간다.
- 몬스터는 남은 HP를 유지한 적대 AI 상태로 복귀한다.
- 해당 몬스터는 영구적으로 다시 빙의할 수 없다.
- AI, 공격, Rigidbody2D, Collider2D를 다시 활성화한다.

### HP 0

- 현재 육신을 제거한다.
- 해당 육신은 시체 빙의 대상으로 남지 않는다.
- 영구적으로 다시 빙의할 수 없다.
- `BodyToSoul` 전환 연출 동안 입력을 잠근 뒤 영혼 상태가 된다.

## 8. 애니메이션 연결

아트가 준비되면 실행 프리팹의 모델 자식에 Animator Controller를 등록한다.

### 쥐 물기 공격

`HWJ_EnemyAttackAnimationRelay`가 애니메이션 이벤트를 공격 시스템에 전달한다.

1. 물기 Animation Clip을 연다.
2. 실제 이빨이 닿는 프레임에 `ApplyBasicAttackHit` 이벤트를 추가한다.
3. 공격 종료 프레임에 `CompleteBasicAttack` 이벤트를 추가한다.
4. 공격이 취소되는 애니메이션에는 `CancelBasicAttack` 이벤트를 사용할 수 있다.

Animator Controller가 아직 없으면 `기본 공격 데이터 > 애니메이션 이벤트 대체 판정 시간` 뒤에 자동으로 1회 판정한다.
따라서 아트가 없어도 공격 기능을 테스트할 수 있다.

### 슬라임 접촉 공격

슬라임은 충돌 접촉을 기준으로 공격하므로 필수 공격 프레임 이벤트가 없다.
애니메이션은 접촉 공격의 시각 표현으로만 연결한다.

## 9. 주요 코드 역할

| 파일 | 역할 |
|---|---|
| `HWJ_EnemyPerceptionSystem.cs` | 시야, 같은 발판, 피격 어그로, 영혼 무시, 생성 위치를 관리한다. |
| `HWJ_MonsterAISystem.cs` | 인식 전 방향 고정, 플레이어 방향 추적, 접근, 다른 발판 대기, 공격, 복귀 상태를 실행한다. |
| `HWJ_EnemyAttackSystem.cs` | 접촉 공격과 애니메이션 이벤트 공격을 구분해 피해를 적용한다. |
| `HWJ_EnemyAttackAnimationRelay.cs` | Animation Event를 공격 시스템에 전달한다. |
| `HWJ_EnemyTypeDataSO.cs` | 일반 몬스터별 조정 가능한 AI/공격 데이터를 제공한다. |
| `HWJ_PossessionTargetValidator.cs` | 살아 있는 일반 몬스터의 빙의 가능 조건을 검증한다. |
| `HWJ_PossessionExitSystem.cs` | 정신력 0 복귀와 HP 0 제거를 서로 다르게 처리한다. |
| `HWJ_GeneralMonsterAssetBuilder.cs` | 일반 몬스터 에셋 생성과 데이터 등록 및 기존 무기 몬스터 이관을 수행한다. |

## 10. 검증 결과

- Unity 6 C# 컴파일: 통과
- HWJ 전체 게임 데이터 검증: 오류 0개
- 빙의 PlayMode 직접 검증: 8개 중 8개 통과
- 확인한 핵심 규칙: 생체 빙의, 성공 정신력 비용, 시간 감소, 정신력 0 적 복귀, HP 0 육신 제거, 영혼 정신력 0 게임오버, 전환 중 조작 잠금

실제 키보드 입력, 애니메이션 프레임, 충돌 지형 배치는 Unity Editor Play Mode에서 수동 확인해야 한다.

## 11. HWJ 범위 밖 주의사항

`Assets/02Scripts/hys/Animation/Editor/hys_RuntimeReadyPrefabAnimationBinder.cs`가 삭제된 옛 `HWJ_Runtime_Enemy_NoCorpse_*.prefab` 경로를 자동으로 찾는다.
HWJ 폴더 밖 파일이므로 이번 작업에서는 수정하지 않았다.
담당자는 해당 바인더에서 NoCorpse 경로 등록을 제거하거나, 에셋이 없을 때 건너뛰도록 null 검사를 추가해야 한다.

일반 몬스터의 지면 마스크는 `Ground` 레이어가 존재하면 해당 레이어를 사용하고, 없으면 `Default`를 사용한다.
마스크가 비었다고 모든 레이어를 지면으로 검사하지 않으므로 몬스터 자신의 Collider를 지형으로 오인하지 않는다.
