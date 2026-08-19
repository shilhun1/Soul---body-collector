# 2026-08-04 HSH 설정창 오디오 조절 및 키 변경 (Key Rebinding) 개발 기록

## 1. 개발 목적
설정창(Settings Panel)에서 게임의 **마스터 볼륨, BGM, SFX 음량 조절** 및 **인게임 단축키(일시정지, 스킬트리, 상호작용 등) 변경 기능**을 손쉽게 설정하고 PlayerPrefs에 자동 저장/로드할 수 있도록 HSH 시스템을 구축함.

---

## 2. 주요 생성 스크립트

### 🔊 오디오 조절 시스템
- **`HSH_AudioManager.cs`**:
  - 마스터(Master), BGM(노래), SFX(효과음) 음량 조절을 총괄하는 싱글톤 오디오 컨트롤러.
  - `AudioMixer` 파라미터 연동(`MasterVolume`, `BGMVolume`, `SFXVolume`)을 지원하며 Mixer 미사용 시 `AudioListener` 및 `AudioSource` 볼륨을 직접 제어.
  - `PlayerPrefs` 키: `"HSH_MasterVolume"`, `"HSH_BGMVolume"`, `"HSH_SFXVolume"`.
- **`HSH_AudioSettingsUI.cs`**:
  - 설정창 패널 내 3개의 음량 Slider 및 TextMeshPro/Legacy Text (%) 연결 제어.

### ⌨️ 키 바인딩 (Key Rebinding) 시스템
- **`HSH_KeyBindingManager.cs`**:
  - `Pause`, `SkillTree`, `Interact`, `MoveUp`, `MoveDown`, `MoveLeft`, `MoveRight` 등의 키 바인딩 저장/로드 관리.
  - Legacy `KeyCode`와 Unity New Input System `Key` 간 통합 변환 레이어 제공.
  - `PlayerPrefs` 키: `"HSH_KeyBinding_[ActionName]"`.
- **`HSH_KeyRebindUI.cs`**:
  - 설정창 내 각 키 변경 버튼 클릭 시 `"[ 입력 대기... ]"` 수신 연출 및 누른 키로 즉시 변경 처리.
  - 기본값 복원 (`ResetToDefaults`) 제공.

---

## 3. 유니티 인스펙터 오브젝트 배치 및 연결 가이드

### 🏢 1) 매니저 (Manager) 스크립트
씬 내에 **`@Managers`** 라는 이름의 빈 오브젝트(Empty GameObject)를 하나 만들고 아래 2개의 매니저 컴포넌트를 붙여줍니다.

- **`HSH_AudioManager`** (`@Managers` 오브젝트에 부착)
  - `Audio Mixer`: 프로젝트의 AudioMixer 연결 (없으면 비워두어도 자동 Fallback 동작).
  - `Bgm Audio Source` / `Sfx Audio Sources`: 필요 시 씬 내 AudioSource 컴포넌트 연결.
- **`HSH_KeyBindingManager`** (`@Managers` 오브젝트에 부착)
  - 인스펙터에 별도 드래그 연결 없이 부착만 해두면 `Awake` 시 자동 동작.

---

### 🎨 2) UI 스크립트
설정창 UI 패널 오브젝트(**`SettingsPanel`**)에 아래 컴포넌트들을 붙여줍니다.

- **`HSH_AudioSettingsUI`** (`SettingsPanel` 오브젝트에 부착)
  - `Master Slider`, `Bgm Slider`, `Sfx Slider`: 설정창 내의 Slider UI 연결.
  - `Value Text`: 음량 수치(%)를 표시할 TMP_Text 또는 Legacy Text 연결.
- **`HSH_KeyRebindUI`** (`SettingsPanel` 오브젝트에 부착)
  - `Rebind Elements`: `+` 버튼을 눌러 변경하려는 액션(Pause, SkillTree, Interact 등) 지정 후 해당 액션의 `Button`과 `Text` 연결.
  - `Reset Defaults Button`: 기본값 복원 버튼 연결 (선택).
- **`HSH_PauseUI`** (`PauseUI` 오브젝트에 부착)
  - `Settings Panel` 필드에 위 `SettingsPanel` 오브젝트가 연결되어 있는지 확인.

---

## 4. 질문 답변: 각 스크립트에 `KeyCode`가 기본 등록된 이유

> **Q. 키바인딩 매니저에서 키코드가 이미 관리되는데, 각 스크립트에 `KeyCode` / `Key` 변수를 따로 적용시킨 이유가 있나요?**

1. **독립성 및 안전장치 (Inspector Fallback)**:
   - 개발 시 씬 내에 `HSH_KeyBindingManager`를 실수로 생성하지 않았거나 단독 스크립트만 테스트할 때 발생할 수 있는 NullReferenceException을 방지하기 위함입니다.
   - 스크립트는 `HSH_KeyBindingManager.Instance`가 활성화되어 있으면 매니저의 최신 바인딩 키를 1순위로 읽고, 매니저가 없을 때만 인스펙터 기본값을 사용하는 안전 구조입니다.
2. **Legacy Input (`KeyCode`) vs New Input System (`Key`) 분리 호환**:
   - 기존 HSH 코드 중 `HSH_PauseUI`, `HSH_SkillTreeToggleUI`는 유니티 기본 `Input.GetKeyDown(KeyCode)` 방식을 사용하고 있었고, `HSH_StealBody`, `HSH_StatProgressCore` 등은 New Input System(`Keyboard.current[Key]`) 방식을 사용하고 있었습니다.
   - `HSH_KeyBindingManager`는 기준 키(`KeyCode`)로 관리하면서 New Input System 컴포넌트를 위해 `GetInputSystemKey()` 변환을 지원하므로, 기존 작성된 스크립트 구조를 수정 없이 안정적으로 연동할 수 있습니다.
