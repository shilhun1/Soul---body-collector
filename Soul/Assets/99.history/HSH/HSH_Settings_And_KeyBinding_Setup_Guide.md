# HSH 설정창(오디오 & 키 바인딩) 시스템 설치 및 가이드 문서

본 문서는 `Assets/02Scripts/HSH`에 구현된 **오디오 볼륨 제어(마스터/BGM/SFX)** 및 **단축키 변경(Key Rebinding) 시스템**의 아키텍처 설계 구조와 유니티 에디터 인스펙터 세팅 가이드를 정리한 문서입니다.

---

## 1. 시스템 개요 (System Overview)

- **오디오 제어**: 마스터 음량, 배경음악(BGM), 효과음(SFX)을 독립적으로 제어하며, `AudioMixer` 및 `AudioListener`/`AudioSource` 제어를 동시에 지원합니다.
- **키 바인딩 제어**: 게임 내 주요 액션(일시정지, 스킬트리, 상호작용, 이동 등)의 단축키를 플레이어가 직접 변경할 수 있으며, `KeyCode` 및 New Input System `Key`를 모두 지원합니다.
- **데이터 보존**: 모든 오디오 및 키 설정값은 `PlayerPrefs`에 자동 저장 및 로드됩니다.

---

## 2. 스크립트 구성 및 역할 (Scripts & Responsibilities)

| 스크립트 이름 | 분류 | 주요 역할 |
|---|---|---|
| **`HSH_AudioManager.cs`** | 매니저 (Singleton) | Master, BGM, SFX 볼륨 관리, AudioMixer/AudioListener 연동, PlayerPrefs 저장 |
| **`HSH_AudioSettingsUI.cs`** | UI 컨트롤러 | 설정창 내 3개 음량 Slider 및 수치(%) Text (TMP/Legacy) 실시간 동기화 |
| **`HSH_KeyBindingManager.cs`** | 매니저 (Singleton) | 액션별 단축키 관리, KeyCode $\leftrightarrow$ InputSystem Key 변환 호환, PlayerPrefs 저장 |
| **`HSH_KeyRebindUI.cs`** | UI 컨트롤러 | 키 변경 버튼 클릭 시 입력 대기 연출, 누른 키 적용, 기본값 복원(Reset) |
| **`HSH_PauseUI.cs`** | UI 연동 | ESC 키 조작 시 일시정지 패널 토글, 설정창 오픈 시 오디오/키 UI 자동 최신화 |
| **`HSH_SkillTreeToggleUI.cs`** | UI 연동 | Tab 키 조작 시 스킬트리 토글, HSH_KeyBindingManager와 키 동기화 |
| **`HSH_StealBody.cs` / `HSH_StatProgressCore.cs`** | 상호작용 연동 | 상호작용 키(기본: F) 입력 수신 및 UI 팝업 문구 HSH_KeyBindingManager 동기화 |

---

## 3. 유니티 에디터 인스펙터 세팅 가이드 (Inspector Setup Guide)

### 🏢 1) 매니저 (Manager) 오브젝트 세팅

1. Hierarchy 뷰에서 빈 오브젝트(Empty GameObject)를 생성하고 이름을 **`@Managers`** 로 지정합니다.
2. `@Managers` 오브젝트에 아래 두 매니저 스크립트를 컴포넌트로 추가합니다.

#### **[HSH_AudioManager] 컴포넌트**
- **Audio Mixer**: (선택) 프로젝트의 `AudioMixer` 에셋 연결. 미연결 시 `AudioListener` 및 `AudioSource` 제어로 자동 전환됩니다.
- **Master Volume Param / BGM Volume Param / SFX Volume Param**: AudioMixer 노출 파라미터명 (기본값: `MasterVolume`, `BGMVolume`, `SFXVolume`).
- **Bgm Audio Source**: (선택) 씬 내 BGM 재생용 AudioSource 연결.
- **Sfx Audio Sources**: (선택) 효과음 재생용 AudioSource 리스트 등록.

#### **[HSH_KeyBindingManager] 컴포넌트**
- 인스펙터에 별도 드래그 연결 항목이 없으며, 부착 상태에서 실행 시 `PlayerPrefs` 저장 데이터를 자동 탐색 및 적용합니다.

---

### 🎨 2) 설정창 UI 패널 (`SettingsPanel`) 세팅

설정창 UI 패널 오브젝트(**`SettingsPanel`**)에 아래 UI 컨트롤러 스크립트 2개를 추가합니다.

#### **[HSH_AudioSettingsUI] 컴포넌트**
- **Master Slider / BGM Slider / SFX Slider**: 설정창 안의 Volume Slider 컴포넌트 드래그 연결.
- **Master / BGM / SFX Value Text (TMP 또는 Legacy)**: 수치(%)를 나타낼 Text 컴포넌트 드래그 연결.

#### **[HSH_KeyRebindUI] 컴포넌트**
- **Rebind Elements** (리스트):
  - `+` 버튼을 눌러 변경하려는 액션 항목을 추가합니다.
  - **Key Action**: 대상 액션 선택 (`Pause`, `SkillTree`, `Interact`, `MoveUp`, `MoveDown`, `MoveLeft`, `MoveRight`).
  - **Rebind Button**: 해당 액션의 키 변경 버튼 UI 연결.
  - **Key Text (TMP/Legacy)**: 키 이름(예: `Escape`, `Tab`, `F` 등)이 출력될 Text 연결.
- **Waiting Overlay Panel**: (선택) 키 입력 대기 중 화면을 어둡게 덮을 팝업/패널 연결.
- **Waiting Message Text**: (선택) `"변경할 키를 눌러주세요..."` 메세지를 표시할 Text 연결.
- **Reset Defaults Button**: (선택) 전체 기본값 복원 버튼 연결.

---

## 4. 핵심 설계 아키텍처 (Architecture Highlights)

### 1) 단독 동작 보장 구조 (Fallback Architecture)
- 매니저가 씬에 없거나 연결되지 않은 테스트 환경에서도 에러(`NullReferenceException`)가 발생하지 않도록 설계되었습니다.
- 모든 스크립트는 `HSH_KeyBindingManager.Instance` 활성화 여부를 확인한 뒤, 매니저가 있으면 저장된 커스텀 키를 1순위로 읽고, 매니저가 없으면 인스펙터 기본 설정값으로 안전하게 가동됩니다.

### 2) 입력 시스템 호환 레이어 (Legacy KeyCode $\leftrightarrow$ InputSystem Key)
- 프로젝트 내부에서 유니티 기본 `Input.GetKeyDown(KeyCode)` 방식과 New Input System `Keyboard.current[Key]` 방식이 혼용되고 있는 점을 고려하여 작성되었습니다.
- `HSH_KeyBindingManager` 내부에서는 기준이 되는 `KeyCode`를 저장을 하되, New Input System을 쓰는 스크립트들을 위해 `GetInputSystemKey()` 변환 메커니즘을 내장하여 기존 코드 구조 수정 없이 완벽히 동동기화됩니다.
