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

*   **HSH_FallingTrap** (낙석 함정)
    *   아래쪽으로 레이캐스트를 쏴서 플레이어를 감지하면, 일시 정지되어 있던 중력을 켜서 아래로 떨어집니다. 부딪힌 대상에게 데미지와 넉백을 줍니다.
*   **HSH_FireSparkTrap** (불똥/투사체 함정)
    *   단일 투사체로 날아가거나, 발사기(`isSpawner`) 모드로 활성화되어 일정 주기에 맞춰 투사체를 발사합니다. 
*   **HSH_SpikeTrap** (가시 함정)
    *   위쪽으로 플레이어를 감지하면 0.7초의 딜레이 후 빠르게 가시가 솟아오릅니다.
*   **HSH_SteamTrap** (수증기 함정)
    *   간헐적으로 분출되는 수증기 함정입니다. 맞은 대상을 강제적인 Y축 조작을 통해 강력하게 위쪽으로 넉백시킵니다.
*   **HSH_WindTrap** (바람 함정)
    *   불똥 함정과 유사하게 발사기 혹은 투사체로 작동하며, 레이캐스트 감지를 통해 플레이어를 인식하면 주기적으로 바람(투사체)을 발사합니다.

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
