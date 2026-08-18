# HWJ 최종보스 시스템

## 1. 구현 범위

최종보스는 평상시에 처음 배치된 위치를 유지하고, 플레이어를 향한 손짓 애니메이션으로 패턴을 시작한다.
보스 본체가 실제로 이동하는 패턴은 `2-3 검은 불꽃 돌진`뿐이다. 공중 연출이 필요한 패턴은 충돌 본체를 옮기지 않고 `HWJ_FinalBoss_VisualRoot`만 위로 이동한다.

- 1페이즈: HP 100%부터 50% 초과 구간, 패턴 5개
- 2페이즈: HP 50% 이하 구간, 패턴 5개
- 같은 패턴 연속 사용 방지 및 최근 패턴 제외는 `HWJ_BossPatternSystem`이 담당
- 영혼 상태 플레이어는 인식하거나 공격하지 않음
- 최종보스는 빙의할 수 없음
- 아트 프리팹이 비어 있으면 Game View에서 확인 가능한 색상 도형을 임시 표시

## 2. 주요 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Data/Systems/HWJ_FinalBossPatternDefinitionSO.cs` | 패턴 시간, 피해, 투사체, 장판, 방어막, 소환, 돌진과 아트 프리팹을 보관하는 정의 데이터 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossPatternSystem.cs` | 최종보스 패턴 시작·취소, 손짓, 고정 위치, 런타임 상태 관리 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossPatternSequences.cs` | 기획서의 1·2페이즈 패턴 순서 실행 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossRuntimeAttacks.cs` | 투사체·장판·예고 표시 생성과 풀링 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossAttackHitbox.cs` | HP 피해와 현재 빙의 정신력 피해 판정 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossBarrierSystem.cs` | 보스 최대 HP 기준 5%·10% 방어막과 피해 흡수 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossSummoning.cs` | 붉은 포탈과 빙의 가능 몬스터 생성 |
| `Scripts/Systems/Boss/FinalBoss/HWJ_FinalBossGestureRelay.cs` | 애니메이션 이벤트를 패턴 실행 시점에 전달 |
| `Editor/HWJ_FinalBossAssetBuilder.cs` | 최종보스 SO 21개와 런타임 프리팹 생성·검증 |

## 3. 생성된 에셋

- 최종보스 프리팹: `Prefabs/Generated/Bosses/HWJ_FinalBoss_Runtime_Prefab.prefab`
- 최상위 데이터: `ScriptableObjects/RootObjects/Bosses/HWJ_FinalBoss_RootObjectData.asset`
- 보스 유형 데이터: `ScriptableObjects/TypeData/Boss/HWJ_FinalBoss_TypeData.asset`
- 공통 실행 패턴 10개: `ScriptableObjects/BossPatterns/FinalBoss`
- 최종보스 상세 패턴 정의 10개: `ScriptableObjects/Systems/Boss/FinalBoss`

에셋을 다시 만들거나 연결을 복구할 때 Unity 메뉴에서 `Tools > HWJ > Boss > Build Final Boss`를 실행한다.
빌더는 기존 동일 경로 에셋을 삭제하지 않고 값을 갱신한 뒤 필수 조건을 검증한다.

## 4. 패턴 구성

| 구분 | 패턴 | 현재 구현 |
|---|---|---|
| 1-1 | 검은 구체 3개 | 시각 루트 공중 이동, 2초 준비 후 플레이어를 향해 2초 간격 발사 |
| 1-2 | 창과 검은 경로 | 1.5초 예고 후 창 발사, 경로 장판 3초 유지 |
| 1-3 | 보라색 방어막 | 보스 최대 HP의 5% 내구도, 파괴 전 보스 HP 피해 차단 |
| 1-4 | 붉은 포탈 | 포탈 1개에서 빙의 가능 몬스터 3마리 소환 |
| 1-5 | 바닥 불과 순차 낙뢰 | 3초 차징, 바닥 지속 피해, 왼쪽부터 순차 낙뢰와 1초 조작 잠금, 종료 후 4초 그로기 |
| 2-1 | 검·창·도끼 연속 발사 | 시각 루트 공중 이동, 검 3개→창 3개→도끼 3개, 마지막 도끼 뒤 전범위 충격파 |
| 2-2 | 강화 방어막과 구체 | 보스 최대 HP의 10% 방어막, 구체 5개를 0.8초 간격으로 4초 동안 발사 |
| 2-3 | 검은 불꽃 실제 돌진 | 플레이어 위치와 방향을 고정한 뒤 보스 Rigidbody2D가 실제 이동, 적중 시 현재 빙의 정신력 최대치의 10% 차감, 이후 원위치 복귀 |
| 2-4 | 이중 붉은 포탈 | 포탈 2개에서 각각 4마리씩 총 8마리 소환 |
| 2-5 | 8방향 검은 낙뢰 | 시각 루트를 중앙 상단으로 이동, 2초 예고 후 8방향 판정과 0.7초 조작 잠금 |

## 5. 씬에 배치하는 방법

1. 최종보스 씬을 연다.
2. `HWJ_FinalBoss_Runtime_Prefab`을 Hierarchy로 드래그한다.
3. 보스를 전투방의 고정 위치에 배치한다. 이 위치가 돌진 후 복귀 위치가 된다.
4. `HWJ_BossBrainSystem > FSM 데이터`의 보스방 크기는 `HWJ_FinalBoss_TypeData`에서 조정한다.
5. `HWJ_FinalBossPatternSystem > 장애물 레이어`에 실제 지형 레이어를 지정한다.
6. `PortalAnchor_Left`, `PortalAnchor_Right`를 소환할 위치로 옮긴다.
7. 플레이어 오브젝트에는 기존 HWJ 플레이어의 `HWJ_RootObjectDataResolver`, `HWJ_SoulSystem`, `HWJ_PossessionSystem` 연결을 유지한다.

보스방 크기와 각 패턴 사거리가 실제 맵보다 크면 판정이 벽 밖에 생성될 수 있으므로 맵 블록아웃 후 SO 수치를 맞춘다.

## 6. 애니메이션 연결

`HWJ_FinalBoss_VisualRoot`의 Animator Controller에 다음 파라미터를 추가할 수 있다.

- `FinalBossPattern` (Int): 현재 패턴 종류 1~10
- 패턴별 Trigger: `Gesture_Orb`, `Gesture_Spear`, `Gesture_Barrier`, `Gesture_Portal`, `Gesture_Lightning`, `Gesture_Weapons`, `Gesture_BarrierOrb`, `Gesture_BlackFlame`, `Gesture_DoublePortal`, `Gesture_EightWayLightning`

현재 생성 데이터는 아트 없이도 실행되도록 시간 기반 실행을 사용한다.
애니메이션의 정확한 손짓 프레임에 공격을 맞추려면 해당 패턴 Definition SO에서 `애니메이션 이벤트 우선 사용`을 켜고 Animation Clip의 발동 프레임에 아래 이벤트를 넣는다.

```text
함수 이름: HWJ_FinalBoss_CastMoment
수신 컴포넌트: HWJ_FinalBossGestureRelay
```

이벤트가 누락되어도 `애니메이션 이벤트 제한 시간`이 지나면 패턴이 실행되어 전투가 멈추지 않는다.

## 7. 아트 연결

각 `HWJ_FinalBossPatternDefinitionSO` 인스펙터에서 다음 슬롯을 사용한다.

- 기본 투사체: 구체, 창, 검 또는 돌진 불꽃
- 두 번째 투사체: 2-1 창
- 세 번째 투사체: 2-1 도끼
- 예고 표시: 공격 전 범위 표시
- 첫 번째 장판: 창 경로, 바닥 불, 충격파, 낙뢰
- 두 번째 장판: 1-5의 낙뢰처럼 같은 패턴 안의 다른 장판
- 방어막: 원형 방어막 프리팹
- 붉은 포탈: 소환 포탈 프리팹

시각 프리팹에는 데미지 스크립트를 넣지 않는다. 실제 판정은 `HWJ_FinalBossAttackHitbox`가 별도로 생성하므로 아트 교체가 전투 수치를 바꾸지 않는다.

## 8. 밸런스 수정 위치

- 보스 HP·공격력·방어력: `HWJ_FinalBoss_RootObjectData`
- 페이즈 전환 HP와 보스방 범위: `HWJ_FinalBoss_TypeData`
- 패턴 선택 쿨타임: `ScriptableObjects/BossPatterns/FinalBoss`의 각 에셋
- 패턴 준비·후딜·개수·속도·피해 배율: `ScriptableObjects/Systems/Boss/FinalBoss`의 각 Definition 에셋
- 2-3 정신력 피해: `P2_03_BlackFlameCharge_Definition > 현재 빙의 정신력 피해 비율`
- 2-4 소환 수: `P2_04_DoublePortalSummon_Definition > 포탈 수`, `포탈당 몬스터 수`

빌더를 다시 실행하면 기본 기획 수치로 돌아가므로, 최종 밸런스 확정 뒤에는 빌더 기본값도 함께 수정한다.

## 9. 테스트 순서

1. Console을 비우고 최종보스 씬을 Play한다.
2. 빙의 상태 플레이어가 보스방에 들어가면 패턴이 시작되는지 확인한다.
3. 영혼 상태로 전환했을 때 진행 중 패턴이 취소되고 새 패턴이 실행되지 않는지 확인한다.
4. 보스 HP를 50% 이하로 내려 2페이즈 패턴만 선택되는지 확인한다.
5. 2-3 적중 전후 현재 빙의 정신력이 최대치 기준 정확히 10% 감소하는지 확인한다.
6. 2-4 실행 후 포탈이 2개이고, 각 포탈에서 4마리씩 총 8마리가 생성되는지 확인한다.
7. 방어막 활성 중 보스 HP가 줄지 않고 방어막 파괴 뒤부터 HP가 줄어드는지 확인한다.
8. 보스가 2-3 외에는 처음 위치에서 움직이지 않는지 확인한다.
9. 2-3 종료·취소 뒤 보스가 처음 위치로 돌아오는지 확인한다.
10. Console에 Missing Script, NullReferenceException, 패턴 정의 누락 경고가 없는지 확인한다.

현재 자동 검증은 패턴 10개, 최종보스 등급, 고정형 이동 정책, 빙의 불가, 2-3 정신력 10%, 2-4의 2×4 소환, 필수 프리팹 컴포넌트를 검사한다.
