# HSH 스크립트 문서화 (HSH Scripts Documentation)

본 문서는 `Assets/02Scripts/HSH` 폴더 내에 작성된 모든 HSH 관련 스크립트들의 역할과 주요 기능을 요약한 문서입니다.

## 1. 상호작용 (Interact)
플레이어와 환경/오브젝트 간의 상호작용을 처리하는 스크립트들입니다.

*   **HSH_SceneMove & HSH_SceneTransfer & HSH_SpawnPoint**
    *   `HSH_SceneMove`: 지정된 씬으로 이동하며, 타겟 스폰 포인트 ID를 `HSH_SceneTransfer`에 저장합니다.
    *   `HSH_SceneTransfer`: 씬 이동 시 스폰 ID나 추가 데이터를 유지하기 위한 정적 데이터 클래스입니다.
    *   `HSH_SpawnPoint`: 씬이 로드될 때 `TargetSpawnID`와 자신의 ID가 일치하면 플레이어를 자신의 위치로 이동시킵니다.
*   **HSH_StatProgressCore**
    *   능력치 코어 오브젝트입니다. 플레이어가 접근하여 특정 키(F)를 누르면 랜덤한 스탯 종류와 수치가 증가했다는 메시지를 출력하고, 코어 오브젝트는 파괴됩니다.
*   **HSH_StealBody**
    *   적의 시체 등에 빙의(Steal Body)하는 기능입니다. 플레이어가 접근하여 상호작용 키(F) 입력 시 빙의 성공 메시지를 띄우고 시체 오브젝트를 삭제합니다.

## 2. 유령 상태 제어 (testmove)
플레이어가 유령(영혼) 상태가 되었을 때의 이동과 제약을 담당합니다.

*   **HSH_GhostMove**
    *   유령 상태 진입 시 중력(`gravityScale = 0`)과 충돌(`isTrigger = true`)을 무시하고, 상하좌우 방향키로 자유롭게 지형지물을 통과하며 비행할 수 있도록 합니다.
*   **HSH_GhostStat**
    *   유령 상태의 남은 시간을 관리합니다. 영혼 상태 유지 시간(`ghostTimeLimit`)이 지나면 설정에 따라 게임 오버 처리 또는 원래 몸으로 복귀(생존)합니다.
    *   유령 상태 동안 플레이어의 체력을 지속적으로 회복시켜 데미지를 받지 않는 무적 상태를 유지합니다.

## 3. 함정 및 기믹 (Trap)
플레이어(및 적)에게 데미지와 넉백을 주는 다양한 환경 함정들입니다. (모든 함정은 플레이어가 영혼 상태일 경우 데미지와 넉백 효과를 완전히 무시합니다.)

*   **HSH_FallingTrap** (낙석 함정) *(업데이트: 2026-07-28)*
    *   아래쪽 레이캐스트 감지 시 1회만 발동(`hasTriggered`)되는 함정입니다. 감지 시 애니메이션 트리거가 발동하고, 콜라이더 `activeOffset` 이동 및 물리 낙하(선택적)를 지원합니다.
*   **HSH_FireSparkTrap** (불똥/투사체 함정) *(업데이트: 2026-07-28)*
    *   발사기(`isSpawner`) 모드일 때 플레이어를 감지하면 발사 애니메이션 연출 후 불똥 프로젝타일을 생성합니다. 생성된 불똥 프로젝타일은 날아가며 대상과 충돌 시 데미지를 주고 소멸합니다.
*   **HSH_SpikeTrap** (가시 함정) *(업데이트: 2026-07-28)*
    *   위쪽으로 플레이어가 감지되면 Transform 직접 이동이 아닌 Animator 연출을 실행하며, 콜라이더의 `activeOffset`을 위로 이동시켜 데미지 판정을 수행합니다.
*   **HSH_SteamTrap** (수증기 함정) *(업데이트: 2026-07-28)*
    *   주기적 분출 또는 플레이어 감지 모드(`detectPlayer`)를 지원합니다. 수증기가 솟구칠 때 애니메이션과 함께 콜라이더의 `activeOffset`을 이동시켜 위로 솟구치는 데미지 및 넉백 판정을 발생시킵니다.
*   **HSH_WindTrap** (바람 함정) *(업데이트: 2026-07-28)*
    *   발사기 모드에서 전방 레이캐스트로 플레이어를 감지 시 발사 애니메이션 연출을 실행하고 바람 프로젝타일을 발사합니다. 프로젝타일 충돌 시 데미지 및 넉백을 입힙니다.
*   **HSH_Soulpass** (영혼 상태 전용 통과 벽/타일맵 기믹) *(신규 제작 및 타일맵 확장: 2026-08-07)*
    *   플레이어가 영혼(Soul/Spirit) 상태일 때만 물리 충돌을 해제(`Physics2D.IgnoreCollision`)하여 통과할 수 있고, 육체 상태일 때는 막히는 물리 기믹입니다.
    *   단일 SpriteRenderer뿐만 아니라 2D `Tilemap` 컴포넌트 및 `TilemapCollider2D`/`CompositeCollider2D`를 완벽 지원하여 타일맵 전체를 영혼 통과 레이어로 구성할 수 있습니다.
*   **HSH_ShieldArrowTrap** (방패 방어 화살 함정) *(신규 제작: 2026-08-07)*
    *   전방 플레이어 감지(`Physics2D.RaycastAll`) 또는 주기적 타이머에 따라 화살 투사체를 발사하는 함정 기믹입니다.
    *   플레이어가 **방패 폼(`HWJ_WeaponType.Shield`)** 상태일 경우 화살을 데미지 없이 차단하고 소멸시키며, 방패 미착용 상태일 경우 데미지 및 넉백을 가합니다.
*   **HSH_ProjectileSwitch** (발사체 타격 스위치) *(신규 제작: 2026-08-07)*
    *   화살, 탄환 등 발사체 공격이 충돌(`OnTriggerEnter2D` / `OnCollisionEnter2D`)하면 발사체를 소멸시키며 작동하는 스위치 컴포넌트입니다.
    *   작동 시 시각적 색상 변경, 애니메이션 Trigger 발동, `HSH_GimmickDoor` 연동 문 열림 및 디버그 로그(`Debug.Log`)를 출력합니다.
*   **HSH_GimmickDoor** (기믹 연동 문) *(신규 제작: 2026-08-07)*
    *   스위치나 기믹 연동 시 작동하는 문(Door/Gate) 컴포넌트입니다.
    *   Animator 파라미터 제어(`AnimatorOnly`), 단순 콜라이더 해제(`DisableCollider`), Y축 부드러운 위치 이동(`TransformMove`), 오브젝트 비활성화(`DisableObject`) 모드를 지원하며 작동 상태를 디버그 로그로 출력합니다.
*   **HSH_FlameWall** (불꽃벽 기믹) *(신규 제작: 2026-08-11)*
    *   육체(Body) 상태에서는 물리 벽으로 막히며 닿을 시 데미지 및 넉백을 입습니다.
    *   영혼(Soul/Spirit) 상태에서는 `HSH_Soulpass` 연동을 통해 통과가 허용되지만, 불꽃벽 내부에 있는 동안 주기적(`damageInterval`) 화염 데미지를 입도록 구현된 기믹 컴포넌트입니다.

## 4. UI 시스템 (UI)
게임 화면 내 다양한 정보와 피드백을 표시하는 스크립트들입니다.

*   **HSH_BarUI** *(업데이트: 2026-07-23)*
    *   체력바(HP), 영혼 카운트다운(GhostHP), 경험치(Exp) 상태를 표시하는 다목적 바(Bar) UI입니다.
    *   경험치(Exp) 바는 `HWJ_LevelUpSystem`의 레벨, 현재 경험치(`CurrentExperience`), 요구 경험치(`TryGetRequiredExperienceForCurrentLevel`)를 연동 받아 실시간 갱신합니다.
*   **HSH_CameraViewer**
    *   RenderTexture를 사용해 서브 카메라가 보고 있는 화면을 우측 상단 등에 Picture-in-Picture (PiP) 형태로 실시간으로 띄워줍니다.
*   **HSH_EnemyHPUI**
    *   적 머리 위(또는 발밑)에 띄우는 체력바 UI입니다. 빙의 상태이거나 체력이 가득 차 있으면 UI를 자동으로 숨기는 기능이 있습니다.
*   **HSH_GameOverUI**
    *   게임 오버 시 활성화되며 재시작, 특정 씬 이동, 게임 종료 등의 기능을 제공하는 팝업 패널을 관리합니다.
*   **HSH_LEVELTEXTUI** *(업데이트: 2026-07-23)*
    *   플레이어의 레벨 정보를 TextMeshPro 혹은 일반 Legacy Text 컴포넌트에 갱신하여 띄워줍니다. `SetLevel(int level)` 함수를 통해 `HWJ_LevelUpSystem`의 실시간 레벨을 반영합니다.
*   **HSH_NPCDialogue** *(업데이트: 2026-07-22)*
    *   NPC와 상호작용 시 타이핑 연출(글자가 한 글자씩 나옴)이 들어간 말풍선을 띄워줍니다.
    *   `Dialogue List` 배열 순서(0, 1, 2, 3...)대로 대사가 진행되며 각 대사의 화자(`DialogueSpeaker`: NPC 또는 Player)를 자유롭게 설정 가능합니다.
    *   *(동적 플레이어 탐색 지원)* 씬에 여러 명의 NPC가 세팅되어 있더라도 코드에서 `EnsurePlayerTransform()`을 수행하여 플레이어 태그/컴포넌트를 감지해 플레이어 머리 위에 말풍선이 정확히 위치하도록 보장합니다.
*   **HSH_SceneLoader**
    *   버튼의 OnClick 이벤트 등에 연결하여 지정된 씬 이름이나 인덱스로 씬을 안전하게 로드할 수 있게 도와줍니다.
*   **HSH_StatProgressUi & HSH_StealBodyUi**
    *   능력치 흡수 코어나 빙의 가능한 시체 근처에 다가갔을 때, 어떤 상호작용 키를 눌러야 하는지 월드 공간에 안내하는 팝업 UI입니다. 리플렉션을 사용하여 TextMeshPro와의 호환성을 확보했습니다.
*   **HSH_SkillResetter** *(추가일: 2026-07-20)*
    *   Smiling Eclipse (Skill Tree Maker Importer) 에셋의 저장 데이터를 안전하게 초기화하는 유틸리티 스크립트입니다. PlayerPrefs에 저장된 특정 스킬트리의 진행도(레벨, 해금 상태) 키만 찾아서 지우며, 스킬 포인트(CurrencyData)를 기본값으로 재설정하는 기능도 포함되어 있습니다. UI 초기화 버튼에 연결하여 사용합니다.
    *   *(업데이트)* 향후 메인 게임 시스템(HWJ)에도 스킬 초기화 기능이 구현될 경우를 대비해, 즉시 연동할 수 있도록 주석(TODO) 처리를 해두었습니다.
*   **HSH_SkillTreeToggleUI** *(추가일: 2026-07-22)*
    *   단축키(기본값: Tab, 인스펙터에서 임의 지정 가능)를 눌러 스킬 트리 UI 패널을 열고 닫을 수 있게 하는 컨트롤러 스크립트입니다.
    *   스킬 트리가 열리면 이전 게임의 시간 흐름 속도를 기억한 뒤 `Time.timeScale = 0f`로 정지시켜 게임이 일시 정지되도록 하고, 마우스 커서를 해제하여 노드 선택 및 스킬 해금을 자유롭게 수행할 수 있도록 지원합니다. 스킬 트리가 닫히면 게임 시간을 기존 상태로 안전하게 복원합니다.
*   **HSH_PauseUI** *(신규 제작: 2026-08-03)*
    *   `ESC` 키 입력 시 게임을 일시 정지(`Time.timeScale = 0f`)시키고 마우스 커서를 해제하며 일시정지 팝업 UI 창을 표시합니다.
    *   일시정지 창 내부에는 상단부터 **계속하기 (Resume)**, **설정 (Settings)**, **게임 종료 (Quit)** 3개의 버튼이 배치됩니다.
    *   **설정** 버튼 클릭 시 일시정지 창이 닫히고/숨겨지며 **설정 창(Settings Panel)**이 화면에 띄워집니다. (설정 창에서 `ESC` 또는 뒤로가기 버튼 입력 시 다시 일시정지 창으로 전환됩니다.)
*   **HSH_AudioManager** *(신규 제작: 2026-08-04)*
    *   마스터 음량(Master), 배경음악(BGM), 효과음(SFX)을 총괄 관리하는 싱글톤 오디오 매니저입니다.
    *   `AudioMixer` 파라미터 연동(`MasterVolume`, `BGMVolume`, `SFXVolume`)을 기본 지원하며, AudioMixer가 없는 경우 `AudioListener` 및 개별 `AudioSource` 볼륨을 자동 조절합니다.
    *   모든 음량 설정값은 `PlayerPrefs`에 자동 저장/로드됩니다.
*   **HSH_AudioSettingsUI** *(신규 제작: 2026-08-04)*
    *   설정창 내부의 마스터, BGM, SFX 슬라이더(Slider) 및 % 텍스트(TMP/Legacy Text)를 제어합니다.
    *   슬라이더 조작 시 `HSH_AudioManager`에 볼륨을 즉시 전달하고 저장합니다.
*   **HSH_KeyBindingManager** *(신규 제작: 2026-08-04)*
    *   일시정지(Pause), 스킬트리(SkillTree), 상호작용(Interact), 이동 방향키 등 커스텀 단축키 변경 및 PlayerPrefs 저장을 관리하는 싱글톤 매니저입니다.
    *   Legacy `KeyCode` 및 Unity New Input System `Key` 동시 변환을 지원하며, 키 바인딩 변경 시 이벤트(`OnKeyBindingChanged`)를 발송합니다.
*   **HSH_KeyRebindUI** *(신규 제작: 2026-08-04)*
    *   설정창 내의 키 변경 버튼 및 현재 단축키 텍스트를 제어합니다.
    *   키 변경 버튼 클릭 시 키 입력 대기 연출("키 입력 대기...") 및 실제 누른 키로 키 바인딩을 즉시 갱신합니다. 기본값 복원(Reset to Defaults) 기능을 포함합니다.

*(💡 설정창 및 매니저 오브젝트 세팅 상세 가이드는 [HSH_Settings_And_KeyBinding_Setup_Guide.md](file:///c:/Users/shong/OneDrive/%EB%B0%94%ED%83%95%20%ED%99%94%EB%A9%B4/unity/4/Soul---body-collector/Soul/Assets/99.history/HSH/HSH_Settings_And_KeyBinding_Setup_Guide.md)에서 확인하실 수 있습니다.)*

---

## 5. 스킬트리 연동 (외부 에셋 통합)
에셋 스토어의 Smiling Eclipse - Skill Tree Maker Importer 에셋을 프로젝트의 메인 게임 시스템(HWJ)과 연동하기 위해 수정한 내역입니다.

*   **SkillNodeData.cs (수정됨)**
    *   기존 노드 데이터에 `HWJ_SkillNodeDataSO hwjSkillData` 필드를 추가하여, 유니티 인스펙터에서 메인 게임의 스킬 데이터를 직접 맵핑할 수 있도록 확장했습니다.
*   **SkillTreePointsUI.cs (수정됨)**
    *   스킬트리 화면 상단에 표시되는 포인트를 기존 임시 데이터(`CurrencyData`)에서 **`HWJ_LevelUpSystem`의 실제 스킬 포인트**로 교체했습니다. 레벨업 등의 이벤트(`HWJ_GameplayEvents`) 발생 시 실시간으로 UI 텍스트가 갱신됩니다.
*   **SkillNode.cs (수정됨)**
    *   스킬 노드의 활성화(구매 가능) 상태 판단 기준을 기존 `CurrencyData`에서 **HWJ 레벨 시스템의 실제 포인트**로 변경했습니다.
    *   버튼을 눌러 스킬 구매 시 `HWJ_SkillUnlockSystem.TryUnlockSkillNode()`를 호출하여 실제 백엔드에 스킬이 해금(포인트 차감 포함)되도록 완전 연동했습니다.
    *   HWJ 시스템의 포인트 증감 이벤트(`SkillPointChanged`)를 구독하여, 하나의 스킬을 찍어 포인트가 변동될 때 모든 노드의 시각적 상태(해금 가능/불가능)가 즉각적으로 동기화됩니다.

---

## 6. 일차별 작업 내역 (Daily Development Log)

### 📅 2026-07-20 (월)
*   **[HSH_SkillResetter] 추가**: Smiling Eclipse 스킬트리 에셋의 저장 데이터를 안전하게 초기화하는 유틸리티 작성 (PlayerPrefs 내 노드 레벨 및 해금 키 개별 삭제, 스킬 포인트 기본값 초기화).

### 📅 2026-07-22 (수)
*   **[HSH_SkillTreeToggleUI] 신규 제작**: `Tab` 키로 스킬트리 UI를 토글하고, 스킬트리가 열려있는 동안 `Time.timeScale = 0f`로 게임을 일시 정지시키며 마우스 커서를 자동 해제하도록 구현.
*   **[HSH_NPCDialogue] 기능 개선**: 대사 말풍선 타이핑 연출 및 플레이어 머리 위 동적 앵커링(`EnsurePlayerTransform()`) 보완.

### 📅 2026-07-23 (목)
*   **[HSH_BarUI & HSH_LEVELTEXTUI] 연동 강화**: `HWJ_LevelUpSystem`과 연결하여 레벨 및 경험치(Exp) 바가 실시간 반응하도록 UI 업데이트.
*   **[스킬트리 일시정지 후 포인트/상호작용 미반영 이슈 해결]**:
    *   `HSH_SkillTreeToggleUI` 오픈 시 `RefreshAllNodes()`를 실행하여 트리를 열었을 때 포인트 및 노드 구매 가능 비주얼이 즉시 최신화되도록 수정.
    *   `SkillNode`: 노드 구매 시 `HWJ_LevelUpSystem` 포인트를 정상 차감하고, 포인트 부족 시 `BuyableState`에서 `UnlockedState`로 비주얼이 정상 복구되도록 개선.
    *   `SkillTreePointsUI`: `OnEnable()` 추가 및 이중 이벤트 구독을 통해 일시정지 상태에서도 포인트 숫자가 실시간 갱신되도록 보완.
    *   `HSH_EnemyHPUI`: 무작위 Canvas 참조(`FindAnyObjectByType`)로 인해 숨겨진 캔버스에 생성되던 문제를 방지하고자 메인 Overlay/Camera HUD Canvas를 탐색하는 `FindMainHUDCanvas()` 구현.
    *   씬 이동 시 이전 씬의 Canvas나 메인 카메라가 파괴되더라도, `Update()` / `LateUpdate()`에서 새 씬의 주 캔버스와 카메라를 자동 재탐색하고 HP UI를 재생성하는 복구 로직 구축.

### 📅 2026-07-28 (화)
*   **[함정(Trap) 시스템 애니메이션 연출 및 Collider Offset 데미지 판정 개편]**:
    *   Transform 직접 이동 방식을 제거하고 Animator 연출 방식을 함정 시스템 전체에 도입.
    *   데미지 판정 방식을 Transform 이동에서 Collider2D의 `activeOffset` 이동 및 원복 구조로 교체.
    *   **`HSH_SpikeTrap`**: 플레이어 감지 시 `Animator` 공격 트리거 연출 및 Collider2D `activeOffset` 데미지 판정 적용.
    *   **`HSH_FallingTrap`**: 아래쪽 플레이어 감지 시 1회성 발동(`hasTriggered`) 구조 구현, Animator 및 `activeOffset` 데미지 영역 이동 적용.
    *   **`HSH_SteamTrap`**: 주기적 분출 외 플레이어 감지 모드(`detectPlayer`) 추가. 분출 시 Animator 및 Collider2D `activeOffset` 솟구침 영역 판정 반영.
    *   **`HSH_FireSparkTrap` & `HSH_WindTrap`**: 발사기(Spawner) 모드일 때 플레이어 감지 시 발사 애니메이션 연출(`fireTriggerName`) 실행 및 투사체 생성. 발사된 프로젝타일이 타겟 충돌 시(`OnTriggerEnter2D`) 데미지를 주고 소멸하도록 메커니즘 정제.

### 📅 2026-08-03 (월)
*   **[HSH_PauseUI] 신규 제작**: `ESC` 키 입력 시 게임을 일시정지(`Time.timeScale = 0f`)하고 일시정지 UI 창을 토글 표시하도록 구현.
    *   일시정지 창에 위에서부터 **계속하기**, **설정**, **게임 종료** 3개 버튼 기능 반영.
    *   **설정** 버튼 클릭 시 일시정지 창을 숨기고 설정 창(Settings Panel)을 표시하는 화면 전환 기능 구현 및 설정 창에서 `ESC`/뒤로가기 입력 시 일시정지 창으로 복귀 연동.

### 📅 2026-08-04 (화)
*   **[설정창 오디오 조절 및 키 변경(Key Rebinding) 시스템 신규 구축]**:
    *   **`HSH_AudioManager`**: 마스터, BGM, SFX 볼륨을 동적으로 제어하고 PlayerPrefs에 저장/로드하는 싱글톤 오디오 컨트롤러 추가. AudioMixer 연동 및 AudioListener/AudioSource 직렬 조절 지원.
    *   **`HSH_AudioSettingsUI`**: 설정창 내 마스터, BGM, SFX 슬라이더 및 % 텍스트 표시 연동.
    *   **`HSH_KeyBindingManager`**: 커스텀 키 바인딩(일시정지, 스킬트리, 상호작용 등) 관리 및 Legacy KeyCode / InputSystem Key 통합 호환 레이어 작성.
    *   **`HSH_KeyRebindUI`**: 키 변경 버튼 클릭 시 키 입력 대기 모드 및 누른 키로 바인딩 즉시 변경 UI 제어. 기본값 복원 지원.
    *   **`HSH_PauseUI` & `HSH_SkillTreeToggleUI` & `HSH_StealBody` & `HSH_StatProgressCore`**: `HSH_KeyBindingManager`와 동기화하여 변경된 단축키가 즉각 인게임 조작 및 UI에 적용되도록 연동.

### 📅 2026-08-07 (금)
*   **[타일맵 영혼 통과 및 방패 함정 / 스위치 & 문 연동 기믹 신규 구현]**:
    *   **`HSH_Soulpass`**: 2D `Tilemap` 색상/투명도 조절 지원 확장 및 클래스 이름 정제.
    *   **`HSH_ShieldArrowTrap`**: 방패 폼(`HWJ_WeaponType.Shield`)일 경우 화살을 데미지 없이 막고 차단하는 방패 방어 화살 발사 함정 신규 제작 및 `RaycastAll` 감지 로직 적용.
    *   **`HSH_ProjectileSwitch`**: 화살/탄환 충돌 시 작동하는 스위치 구현, 태그 비교 예외 방지(`string.Equals`) 적용 및 작동 감지 `Debug.Log` 출력 연동.
    *   **`HSH_GimmickDoor`**: 스위치 및 기믹과 연동되어 열리는 문 컴포넌트 신규 제작 (애니메이터, 위치 이동, 콜라이더 제어 모드 지원 및 디버그 로그 출력).


