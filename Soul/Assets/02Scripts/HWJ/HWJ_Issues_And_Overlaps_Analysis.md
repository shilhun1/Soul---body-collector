# HWJ 코드 중복 및 잠재적 문제점 분석 보고서 (Code Overlaps & Issues Analysis)

본 문서는 프로젝트의 **HWJ 시스템**(메인 게임 플레이 및 인프라)과 **HSH 시스템**(UI 및 단축키/오디오) 간에 존재하는 **중복 구현 코드, 구조적 충돌 위험, 성능 저하 요소 및 잠재적 버그**를 상세히 분석하고 개선안을 제시한 리포트입니다.

---

## 1. 중복 / 이중 구현 코드 분석 (Duplicated & Overlapping Code)

### 1.1 게임 오버(Game Over) 처리 이중화
- **관련 파일**:
  - `Assets/02Scripts/HWJ/Scripts/Systems/UI/HWJ_GameOverWindowSystem.cs`
  - `Assets/02Scripts/HSH/UI/HSH_GameOverUI.cs`
- **문제점**:
  - 두 클래스 모두 플레이어 사망(`Dead`) 상태 감지 시 독립적으로 게임 오버 화면을 띄우고, `Time.timeScale = 1f` 복원 및 씬 재로드(`SceneManager.LoadScene`) 로직을 개별 구현하고 있습니다.
  - 씬 내에 두 컴포넌트가 동시에 존재할 경우 씬이 중복 로드되거나 UI 이벤트 처리 시 충돌이 발생할 위험이 있습니다.
- **개선안**:
  - 씬 백엔드 로직(`HWJ_GameOverWindowSystem`)과 시각적 UI 패널(`HSH_GameOverUI`) 중 하나로 책임을 일원화하거나, 사망 이벤트를 통해 단일 패널만 활성화되도록 구조를 통합해야 합니다.

### 1.2 일시정지(Pause) 및 ESC 키 입력 독립 처리
- **관련 파일**:
  - `Assets/02Scripts/HWJ/Scripts/Core/HWJ_GameManager.cs` (`togglePauseWithEscape`)
  - `Assets/02Scripts/HSH/UI/HSH_PauseUI.cs`
- **문제점**:
  - `HWJ_GameManager.Update()`와 `HSH_PauseUI.Update()` 두 곳에서 각각 ESC 키 프레임 입력을 독자적으로 감지하고 있습니다.
  - 이로 인해 프레임 타이밍에 따라 `HWJ_GameManager`가 시간을 멈췄으나 `HSH_PauseUI` 창은 열리지 않거나, 반대로 UI 창만 켜지고 메인 게임 루프 상태(`isPaused`)가 엇갈리는 현상이 발생할 수 있습니다.
- **개선안**:
  - `HWJ_GameManager`의 `togglePauseWithEscape` 설정을 `false`로 끄고, 입력 감지 및 일시정지 UI 토글 처리를 `HSH_PauseUI` 단일 지점으로 모으는 것을 권장합니다.

### 1.3 프로젝트 폴더 구조 이중화 (Core Folder Redundancy)
- **관련 파일**:
  - `Assets/02Scripts/HWJ/Core/` (`HWJ_DamageInfo.cs`, `HWJ_StatData.cs`, `HWJ_LegacyDamageType.cs`)
  - `Assets/02Scripts/HWJ/Scripts/Core/` (`HWJ_GameManager.cs`, `HWJ_GameAccess.cs`)
- **문제점**:
  - `HWJ` 루트 아래에 `Core` 폴더와 `Scripts/Core` 폴더가 따로 분리되어 있어 데이터 구조체와 매니저 클래스의 위치 파악이 어렵고 가독성이 떨어집니다.
- **개선안**:
  - `Assets/02Scripts/HWJ/Core/` 내의 파일들을 `Assets/02Scripts/HWJ/Scripts/Core/` 하위로 이동시켜 폴더 구조를 단일화합니다.

---

## 2. 잠재적 예외 / 버그 및 성능 위험 요소 (Potential Bugs & Performance Issues)

### 2.1 UI에서 리플렉션(Reflection)을 이용한 비공개 메서드 호출
- **관련 파일**: `Assets/02Scripts/HSH/UI/HSH_BarUI.cs` (라인 260 부근)
- **원인 코드**:
  ```csharp
  var method = typeof(HWJ_SoulSystem).GetMethod("EnterDeadState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  if (method != null) { method.Invoke(soulSystem, null); }
  ```
- **문제점**:
  - C# Reflection을 런타임 UI 처리 루프 안에서 호출하면 CPU 연산 비용이 높고 런타임 오류 가능성이 커집니다.
  - 만약 HWJ 내부의 `EnterDeadState` 메서드 이름이 리팩토링되거나 억세스 지정자가 변경되면 컴파일 에러 없이 런타임에서 조용히 동작하지 않는 치명적 버그가 됩니다.
- **개선안**:
  - `HWJ_SoulSystem`에 `public void ForceDeadState()`와 같은 퍼블릭 API를 제공하거나, 이벤트를 통해 안전하게 상태 전환을 알리도록 수정해야 합니다.

### 2.2 매 프레임 `FindGameObjectWithTag` 및 컴포넌트 탐색 (Polling 성능 낭비)
- **관련 파일**: `Assets/02Scripts/HSH/UI/HSH_BarUI.cs`, `Assets/02Scripts/HSH/UI/HSH_LEVELTEXTUI.cs`
- **문제점**:
  - `Update()` 루프에서 `FindPlayerSystems()`가 매 프레임 호출되며 `GameObject.FindGameObjectWithTag("Player")` 및 `GetComponent` 연산을 지속적으로 수행합니다.
  - 플레이어가 씬에 없거나 씬 전환 중일 때 매 프레임 검색 연산이 낭비됩니다.
- **개선안**:
  - `Start()`나 `Awake()` 시점에 1회 탐색하여 캐싱하고, `HWJ_GameplayEvents.OnPlayerLevelChanged`, `OnExperienceChanged` 등 이벤트 구독(Event-Driven) 방식으로 전환하여 `Update()` 연산을 없애야 합니다.

### 2.3 씬 재시작 시 런타임 스냅샷 잔존 및 사망 반복 위험
- **관련 파일**: `Assets/02Scripts/HWJ/Scripts/Core/HWJ_GameManager.cs`, `Assets/02Scripts/HWJ/Scripts/Systems/Stage/HWJ_SceneTransitionSystem.cs`
- **문제점**:
  - `HWJ_GameManager`는 `DontDestroyOnLoad`로 씬 간 유지되며, 플레이어가 사망하기 직전 스탯 및 영혼 시간이 스냅샷으로 보존됩니다.
  - 플레이어가 사망 후 "다시 시작(Restart)"을 눌러 동일 씬을 다시 로드할 때, 이전 스냅샷이 초기화되지 않으면 체력 0 / 영혼 시간 0 상태가 그대로 불러와져 **로드되자마자 즉시 다시 게임 오버되는 부작용**이 발생할 위험이 있습니다.
- **개선안**:
  - "씬 재시작(Restart)" 시에는 `ClearRuntimeSnapshot()`을 호출하여 스냅샷을 파기하고 초기 스테이지 설정 데이터(SO)로 신규 생성이 이루어지도록 보장해야 합니다.

### 2.4 New Input System & Legacy Input 이중 매핑 유지보수 위험
- **관련 파일**:
  - `Assets/02Scripts/HWJ/Scripts/Systems/Input/HWJ_PlayerInputSystem.cs`
  - `Assets/02Scripts/HSH/UI/HSH_KeyBindingManager.cs`
- **문제점**:
  - 두 시스템 모두 `Key` (New Input)와 `KeyCode` (Legacy Input) 간의 수동 매핑 테이블(`KeyCodeToInputKey`, `InputKeyToKeyCode`)을 갖고 있습니다.
  - 신규 조작키나 마우스 버튼 액션이 추가될 때 한쪽 시스템의 매핑 switch 문이 누락되면 단축키 바인딩이 엇갈리거나 인게임 동작이 작동하지 않을 수 있습니다.
- **개선안**:
  - 키 변환 매핑 메서드를 공통 헬퍼 클래스로 일원화하여 관리를 상속받도록 정리합니다.

---

## 3. 리팩토링 제안 순서 (Recommended Action Plan)

| 우선순위 | 항목 | 내용 |
| :--- | :--- | :--- |
| **P0 (긴급)** | **리플렉션 제거** | `HSH_BarUI.cs`의 `EnterDeadState` 리플렉션 호출을 `HWJ_SoulSystem` 공식 API로 교체 |
| **P1 (높음)** | **일시정지/게임오버 단일화** | `HWJ_GameManager.togglePauseWithEscape`를 비활성화하고 `HSH_PauseUI` 및 `HSH_GameOverUI`로 컨트롤 단일화 |
| **P1 (높음)** | **씬 재시작 스냅샷 리셋** | `RestartGame()` 호출 시 `HWJ_GameManager` 런타임 스냅샷 초기화 보장 |
| **P2 (보통)** | **UI 이벤트 기반 전환** | `HSH_BarUI` 및 `HSH_LEVELTEXTUI`의 매 프레임 `FindGameObjectWithTag` 탐색을 `HWJ_GameplayEvents` 이벤트 구독으로 교체 |
| **P3 (개선)** | **폴더 구조 통합** | `Assets/02Scripts/HWJ/Core` 내의 파일들을 `Assets/02Scripts/HWJ/Scripts/Core`로 이동 및 정리 |
