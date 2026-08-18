# 2026-08-11 HSH 창 폼(Lance) 1번/4번 스킬 돌진 파괴 오브젝트 기믹 개발 기록

## 1. 개발 목적
- 게임 내 **창(Spear/Lance) 폼 1번/4번 스킬 돌진 파괴 오브젝트** 기믹을 구현함.
- **기본 대쉬기(Shift/Space)**로 충돌할 때는 파괴되지 않고 단단한 벽으로 막힘.
- 플레이어가 **창 폼(`HWJ_WeaponType.Lance`)** 상태에서 **1번 스킬(`PiercingDrive`)** 또는 **4번 스킬(`BurstLance`)**로 돌진하며 부딪힐 때만 오브젝트가 파괴되어 통과할 수 있도록 함.

---

## 2. 주요 생성 스크립트

### 🗡️ 창 돌진 파괴 기믹 스크립트
- **`HSH_SpearDashBreakable.cs`** (`Assets/02Scripts/HSH/Gimmick/HSH_SpearDashBreakable.cs`):
  - `OnCollisionEnter2D`, `OnCollisionStay2D`, `OnTriggerEnter2D`, `OnTriggerStay2D` 접촉 및 `TryActivateFromSkillHit` 스킬 콜백을 감지함.
  - 충돌 대상 플레이어의 무기(`HWJ_WeaponGimmickActivatorUtility.ResolveSourceWeapon`)가 `HWJ_WeaponType.Lance`인지 검사.
  - 기본 대쉬기(`HWJ_PlayerMovementSystem.TryDash`)를 제외하고, **1번 스킬(`PiercingDrive`)** 또는 **4번 스킬(`BurstLance`)**의 모션키/애니메이터 스테이트/트리거가 활성화된 경우만 감지(`IsSpearSkill1Or4Active`).
  - 조건을 모두 충족하면 이펙트 및 파괴 사운드를 출력하고, 설정된 `BreakMode`에 따라 오브젝트 비활성화, 삭제, 애니메이션 연출 등을 수행하고 물리 충돌체를 해제하여 통과 가능하게 함.

---

## 3. 유니티 인스펙터 오브젝트 배치 및 설정 가이드

### 🧱 1) 창 돌진 파괴 오브젝트 설정
1. 파괴 가능한 벽 또는 기믹 오브젝트에 `HSH_SpearDashBreakable` 컴포넌트를 추가합니다.
2. `Collider2D` (BoxCollider2D 등)를 설정하여 물리 충돌 영역을 지정합니다.

### ⚙️ 2) 인스펙터 주요 파라미터 설정
- **Break Mode**:
  - `DisableObject`: 오브젝트 전체 비활성화
  - `DestroyObject`: 오브젝트 완전 삭제
  - `DisableCollider`: 콜라이더 및 스프라이트만 비활성화 (투명 통과)
  - `AnimatorTrigger`: 지정된 애니메이션 Trigger 실행 후 비활성화
- **Required Weapon Type**: 요구 무기 타입 (`Lance` 기본 지정)
- **Break Effect Prefab**: 부서질 때 튀는 파티클/이펙트 프리팹
- **Break Sfx**: 파괴 사운드 오디오 클립 (`AudioSource` 연결)
- **Respawn After Time**: 일정 시간 후 장애물 자동 복구 여부 및 복구 딜레이 (`respawnDelay`)

---

## 4. 핵심 동작 흐름
```mermaid
flowchart TD
    A[플레이어가 파괴 벽과 접촉] --> B{HWJ_PossessionSystem 현재 무기 확인}
    B -- Lance(창) 이외 무기 --> C[충돌 막힘 (통과 불가)]
    B -- Lance(창) 무기 --> D{hys_Player_Movement 돌진 상태 확인}
    D -- Is_Dashing == false --> C
    D -- Is_Dashing == true --> E[오브젝트 파괴 연출 & 콜라이더 해제]
    E --> F[플레이어 통과 성공]
```
