# HWJ (Main Gameplay & System Infrastructure) 아키텍처 및 시스템 종합 문서

이 문서는 프로젝트의 메인 게임 플레이 코어 로직과 핵심 제어 시스템이 포함된 **HWJ 시스템**의 구조, 각 스크립트의 역할, 및 시스템 간 데이터 흐름을 상세히 정리한 개발 및 유지보수 가이드 문서입니다.

---

## 1. 개요 (System Overview)

HWJ 모듈은 게임의 **핵심 메커니즘**(플레이어 이동/전투, 영혼 및 육신 빙의 시스템, 스탯 및 경험치/레벨업, 스킬 해금, 스테이지 흐름, 씬 전환 스냅샷, 세이브/로드, 오브젝트 풀링)을 담당하는 백엔드 메인 로직입니다.

- **독립성 및 확장성**: `HWJ_GameManager` 및 `HWJ_GameAccess`를 중심으로 시스템들이 분리되어 있으며, UI(HSH 모듈) 및 기타 보조 시스템과 이벤트 버스(`HWJ_GameplayEvents`)를 통해 느슨하게 결합(Loosely Coupled)되어 있습니다.
- **New Input System & Legacy Input 하이브리드 지원**: 유니티 신규 Input System 패키지와 기존 Input Manager 방식을 동시에 완벽하게 지원합니다.

---

## 2. 코어 관리자 및 액세스 레이어 (Core Access & Managers)

### 2.1 `HWJ_GameManager`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Core/HWJ_GameManager.cs`
- **역할**: 게임의 전역 상태, 데이터베이스, 오브젝트 풀링, 플레이어 스폰 및 씬 전환 스냅샷 관리를 총괄하는 전역 싱글톤 매니저입니다.
- **주요 기능**:
  - `SetPaused(bool paused)`: 게임 일시정지 상태 토글 및 `Time.timeScale` 조절.
  - `Spawn(GameObject prefab, ...)` / `Despawn(GameObject instance)`: `HWJ_ObjectPoolSystem`과 연동된 오브젝트 생체 주기 관리.
  - `SavePlayerRuntimeSnapshot()`: 씬 이동 시 플레이어의 스탯, 경험치, 남은 영혼 시간 등의 런타임 스냅샷 보존.
  - `Database` 필드: 스킬, 레벨업 테이블, 몬스터, 타이틀 스크린 등의 SO 데이터 조회 통로 제공.

### 2.2 `HWJ_GameAccess`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Core/HWJ_GameAccess.cs`
- **역할**: 외부 시스템(UI, 몬스터 AI, 트랩 등)이 `FindObjectOfType` 없이 정적 헬퍼 방식으로 `HWJ_GameManager` 및 핵심 하위 시스템에 안전하게 접근하도록 지원하는 Service Locator 클래스입니다.
- **주요 정적 프로퍼티/메서드**:
  - `HWJ_GameAccess.PlayerInput`: 현재 활성화된 플레이어 인풋 시스템 참조.
  - `HWJ_GameAccess.ObjectPool`: 오브젝트 풀 시스템 참조.
  - `HWJ_GameAccess.Spawn(...)` / `Despawn(...)`: 안전한 풀 생성/반환 래퍼.
  - `HWJ_GameAccess.TryGetSkillNode(nodeId, out data)`: 스킬 노드 에셋 데이터베이스 조회.

---

## 3. 입력 관리 시스템 (Input System & Rebinding)

### 3.1 `HWJ_PlayerInputSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Input/HWJ_PlayerInputSystem.cs`
- **역할**: 플레이어의 입력(`Move`, `Jump`, `Dash`, `Attack`, `Interact`, `ExitPossession`, `SkillSlot1~4`)을 감지하고 런타임 단축키 재바인딩(Rebinding)을 관리합니다.
- **주요 프로퍼티 및 헬퍼**:
  - `MoveInput`: 2D 이동 벡터 (`Vector2`).
  - `JumpPressedThisFrame` / `JumpHeld`: 점프 입력 판단.
  - `DashPressedThisFrame`: 대시 입력 판단.
  - `AttackPressedThisFrame`: 공격 입력 판단.
  - `TrySetKeyboardBinding(hwjActionId, newKey)`: 특정 액션의 키보드 단축키 동적 변경.
  - `TrySetMouseBinding(hwjActionId, mouseBtn)`: 특정 액션의 마우스 단축키 동적 변경.
  - `HSH_KeyBindingManager`와의 연동: UI에서 단축키가 변경될 때 즉시 인게임 동작 키가 갱신됩니다.

---

## 4. 캐릭터 상태 및 영혼/빙의 메커니즘 (Status, Soul & Possession)

### 4.1 `HWJ_RuntimeStatusSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Status/HWJ_RuntimeStatusSystem.cs`
- **역할**: 체력(HP), 정신력(Mental), 스턴(Stun) 수치 관리 및 피격 데미지 처리를 담당합니다.
- **주요 기능**:
  - `ApplyDamage(float damage, Component source, HWJ_DamageData sourceDamage)`: 피격 데미지 감소 계산, 무적 시간 처리 및 체력 0 도달 시 사망/영혼 분리 이벤트 발동.
  - `Heal(float amount)` / `TryApplySpiritMentalCost(float mentalCost)`: 체력 회복 및 정신력 소비 처리.

### 4.2 `HWJ_SoulSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Soul/HWJ_SoulSystem.cs`
- **역할**: 플레이어의 런타임 상태(`Body`, `BodyToSoul`, `Soul`, `Dead`) 및 존재 상태(`Possessed`, `Spirit`, `Transitioning`)를 제어합니다.
- **핵심 특징**:
  - **영혼 데드라인 타이머 (`soulDeadlineTimer`)**: 육신을 잃고 영혼(`Soul`) 상태가 되면 10초 타이머가 작동하며, 0초 도달 시 사망(`Dead`) 처리되어 게임오버 흐름으로 연결됩니다.
  - 새 육신에 빙의 성공 시 `Soul` 상태에서 다시 `Body` 상태로 복귀합니다.

### 4.3 `HWJ_PossessionSystem` & `HWJ_BodyDiscoverySystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Possession/`
- **역할**: 주위의 쓰러진 시체/육신을 탐지(`BodyDiscovery`)하고, 플레이어가 영혼 상태일 때 대상 육신에 빙의(`TryPossessBody`)하는 핵심 게임 시스템입니다.
- **주요 기능**:
  - 빙의 성공 시 해당 육신의 외형, 애니메이터, 스탯 및 고유 스킬이 플레이어에게 적용됩니다.
  - `HWJ_BodyDecaySystem`: 빙의된 육신의 지속시간 및 붕괴/부패 수치를 관리합니다.

---

## 5. 플레이어 조작 및 전투 (Player Movement & Combat)

### 5.1 `HWJ_PlayerMovementSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Player/HWJ_PlayerMovementSystem.cs`
- **역할**: `Rigidbody2D` 기반으로 플레이어의 좌우 이동, 점프, 일방통과 플랫폼(One-Way Platform) 통과, 대시 동작을 처리합니다.

### 5.2 `HWJ_PlayerAttackSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Player/HWJ_PlayerAttackSystem.cs`
- **역할**: 기본 공격 및 마우스/키보드 조합 공격, 콤보 타격 판정 및 쿨다운을 관리합니다.

### 5.3 `HWJ_PlayerCameraFollowSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Player/HWJ_PlayerCameraFollowSystem.cs`
- **역할**: 씬의 Main Camera가 플레이어 오브젝트 및 맵의 바운더리(Boundary) 영역을 매끄럽게 추적하도록 제어합니다.

---

## 6. 성장 및 스킬 시스템 (Progression & Skill Systems)

### 6.1 `HWJ_LevelUpSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Level/HWJ_LevelUpSystem.cs`
- **역할**: 적 처치 시 획득하는 경험치(`AddExperience`), 레벨업 판정, 및 레벨업 보상 스킬 포인트(`SkillPoint`) 지급을 담당합니다.
- **이벤트 전파**:
  - 경험치 변경 시 `HWJ_GameplayEvents.RaiseExperienceChanged()` 전파.
  - 레벨업 시 `HWJ_GameplayEvents.RaisePlayerLevelChanged()` 전파 -> UI(`HSH_BarUI`, `HSH_LEVELTEXTUI`)에서 실시간으로 반영됩니다.

### 6.2 `HWJ_SkillActionSystem` & `HWJ_SkillUnlockSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Skill/`
- **역할**: 스킬 노드의 해금 조건 검사, 스킬 포인트 차감 및 해금된 스킬 슬롯 액션 실행을 전담합니다.
- **주요 구성 및 기능**:
  - `HWJ_SkillActionSystem.cs`: 스킬 ID/엔트리/SO 기반 스킬 실행 진입점(`TryUseSkill`).
  - `HWJ_SkillActionAreaAndMotion.cs` & `HWJ_SkillActionProjectileAndDash.cs`: 근접/범위/투사체/대시 스킬의 판정 및 모션, 이펙트 생성 처리.
  - **스킬 이펙트 위치 및 변형 커스터마이징**:
    - **스킬 데이터 단위 (`HWJ_SkillActionDataSO`)**:
      - `전용 이펙트 오프셋 사용 (useCustomActionEffectOffset)`: 스킬별 개별 오프셋 사용 여부 플래그.
      - `액션 이펙트 오프셋 (actionEffectOffset)`: 캐릭터 중심 기준 생성 오프셋 (X, Y). 캐릭터의 바라보는 방향에 따라 X축 자동 반전.
      - `액션 이펙트 회전각 (actionEffectRotationZ)`: 추가 Z축 회전 각도.
      - `액션 이펙트 크기 배율 (actionEffectScaleMultiplier)`: 이펙트 스케일 배율.
    - **시스템 공통 단위 (`HWJ_SkillActionSystem`)**:
      - `ActionEffectSpawnOffset`: 개별 설정을 사용하지 않는 모든 스킬의 기본 이펙트 생성 위치.
      - `MirrorActionEffectByFacing`: 바라보는 방향에 따른 이펙트 X축 반전 활성화 여부.
  - `HWJ_SkillUnlockSystem.cs`: 스킬 트리 노드 해금 상태 검사 및 스킬 포인트 소모 처리.

---

## 7. 스테이지 및 씬 관리 시스템 (Stage Flow & Scene Transition)

### 7.1 `HWJ_StageProgressionSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Stage/HWJ_StageProgressionSystem.cs`
- **역할**: 현재 스테이지의 진행 상태(`Entering`, `Exploring`, `BossBattle`, `Cleared` 등)를 추적하고, 보스전 진입 조건 및 클리어 조건을 판정합니다.

### 7.2 `HWJ_SceneTransitionSystem` & `HWJ_ScenePortalSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Stage/`
- **역할**: 스테이지 내 포탈 진입 시 다음 씬으로 플레이어의 스탯, 영혼 타이머, 스킬 상태를 유지(`KeepAcrossScenes`)하면서 씬을 전환합니다.

### 7.3 `HWJ_SpawnerSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Spawner/HWJ_SpawnerSystem.cs`
- **역할**: 지정된 스폰 포인트(`HWJ_SpawnPoint`)에서 몬스터 및 오브젝트를 동적으로 생성/배치합니다.

---

## 8. 저장 및 최적화 인프라 (Save & Pooling Infrastructure)

### 8.1 `HWJ_SaveService` & `HWJ_SaveMigrationService`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Save/`
- **역할**: 게임 진행도, 해금된 스킬, 플레이어 레벨, 인벤토리/스탯 데이터를 JSON/PlayerPrefs 형태로 안전하게 직렬화하여 저장 및 로드합니다.

### 8.2 `HWJ_ObjectPoolSystem` & `HWJ_HitEffectSystem`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Pooling/`
- **역할**: 타격 이펙트, 투사체, 몬스터 시체 등 자주 생성/파괴되는 오브젝트의 GC(Garbage Collector) 오버헤드를 방지하기 위해 생성-반환 메커니즘을 제공합니다.

---

## 9. 전역 이벤트 버스 (Gameplay Events)

### `HWJ_GameplayEvents`
- **위치**: `Assets/02Scripts/HWJ/Scripts/Systems/Events/HWJ_GameplayEvents.cs`
- **역할**: 게임 내 중요한 이벤트(플레이어 사망, 레벨업, 경험치 획득, 스킬 포인트 변동, 빙의 상태 변경 등)를 발행 및 구독하는 전역 C# 이벤트 버스입니다.
- **효과**: UI나 퀘스트/몬스터 시스템이 플레이어 컴포넌트에 강하게 결합되지 않고 이벤트를 수신할 수 있도록 해줍니다.
