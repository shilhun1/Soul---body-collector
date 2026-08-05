# Boss Sprite Audit

- 생성 시각: 2026-07-29T23:18:03+09:00
- 검사 범위: `Assets/02Scripts/HWJ/Art/Boss`의 PNG 8개와 해당 TextureImporter 메타
- 이번 작업에서 생성한 파일: 이 보고서와 `BossSpriteAudit.json`
- 이번 작업에서 하지 않은 작업: 원본 이미지/메타 수정, Sprite 분리, Animation Clip 생성, Animator Controller 수정

## 판정 기준

- `GAME_READY_SEQUENCE`: 투명 배경의 개별 프레임 시퀀스로 바로 사용할 수 있음
- `GAME_READY_SHEET`: 제목, 번호, 격자선 없이 규칙적인 셀로 구성된 투명 스프라이트 시트
- `CLEANABLE_REFERENCE_SHEET`: 어두운 불투명 배경이나 제목, 번호, 격자선이 있지만 캐릭터와 분리되어 있어 정제 가능
- `REFERENCE_ONLY_REJECTED`: 보스 이미지라는 근거가 없거나 안전한 분리를 보장할 수 없어 자동 처리하면 안 됨

`안전 자동 분리 후보`는 현재 이미지를 수정한다는 뜻이 아니다. 추후 별도 생성본을 만들 때 셀 경계가 명확하다는 뜻이며, 이번 감사에서는 어떤 추출도 수행하지 않았다.

## 요약

| 구분 | 개수 | 파일 |
|---|---:|---|
| GAME_READY_SEQUENCE | 0 | 없음 |
| GAME_READY_SHEET | 0 | 없음 |
| CLEANABLE_REFERENCE_SHEET | 7 | `boss2_patten.png`, `death.png`, `idle.png`, `patten_1.png`~`patten_4.png` |
| REFERENCE_ONLY_REJECTED | 1 | `move.png` |
| 안전 자동 분리 후보 | 4 | `patten_1.png`~`patten_4.png` |
| 수동 확인이 필요한 조건부 후보 | 3 | `boss2_patten.png`, `death.png`, `idle.png` |

모든 PNG는 RGB이며 알파 채널이 없다. 현재 `.meta`도 `textureType: Default`, `spriteMode: Single`, `filterMode: Bilinear`, `alphaIsTransparency: false`, `spritePixelsToUnits: 100`, 중앙 Pivot 상태다. 따라서 현재 상태 그대로 게임용 픽셀 스프라이트라고 볼 수 있는 파일은 없다.

## 이미지별 검사

| 파일 | 크기 | 어두운/검은 배경 | 제목 | 번호 | 격자선 | 선언된 구성 | 판정 | 자동 분리 |
|---|---:|---|---|---|---|---|---|---|
| `boss2_patten.png` | 1536x1024 | 있음, 불투명 | 있음 | 있음 | 있음 | 5행, 행마다 14 Frames | CLEANABLE_REFERENCE_SHEET | 조건부 |
| `death.png` | 1448x1086 | 있음, 불투명 | 없음 | 있음 | 없음 | 14프레임으로 보임 | CLEANABLE_REFERENCE_SHEET | 수동 확인 |
| `idle.png` | 1448x1086 | 있음, 불투명 | `P1_Idle`, `8 frames` | 있음 | 있음 | 8열 1행 | CLEANABLE_REFERENCE_SHEET | 조건부 |
| `move.png` | 1536x1024 | 있음, 불투명 | `P1_Move (Sword Player)` | 있음 | 있음 | `8 Frames | 64x64` | REFERENCE_ONLY_REJECTED | 금지 |
| `patten_1.png` | 1857x847 | 있음, 불투명 | `P1_Attack_Combo (Boss)` | 있음 | 있음 | `14 Frames | 64x64`, 7x2 | CLEANABLE_REFERENCE_SHEET | 안전 후보 |
| `patten_2.png` | 1672x941 | 있음, 불투명 | `P1_Attack_Charge (Boss)` | 있음 | 있음 | `14 Frames | 64x64`, 7x2 | CLEANABLE_REFERENCE_SHEET | 안전 후보 |
| `patten_3.png` | 1448x1086 | 있음, 불투명 | `P1_Attack_Uppercut (Boss)` | 있음 | 있음 | `14 Frames | 64x64`, 7x2 | CLEANABLE_REFERENCE_SHEET | 안전 후보 |
| `patten_4.png` | 1448x1086 | 있음, 불투명 | `P1_Attack_GroundSlam (Boss)` | 있음 | 있음 | `14 Frames | 64x64`, 5x3(마지막 행 4개) | CLEANABLE_REFERENCE_SHEET | 안전 후보 |

### `boss2_patten.png`

- 한 이미지에 서로 다른 애니메이션 5개가 들어 있다.
- 확인된 행 제목은 `암영 연무`, `흑뢰 낙인`, `마력 붕권`, `마령 포박`, `종말의 마투`이며 각 행에 1~14 번호와 격자가 있다.
- 행별 캐릭터와 장식 텍스트는 시각적으로 분리되어 있지만 논리 프레임 크기가 명시되지 않았다.
- 결론: 정제 가능한 참고 시트지만, 행 범위와 실제 셀 크기를 수동 확정하기 전에는 자동 추출 금지.

### `death.png`

- 동일한 파란 기사 한 명의 사망 진행으로 보이는 14개 프레임이 있다.
- 번호는 있지만 제목, 격자선, 논리 프레임 크기 표기가 없다.
- 결론: 파일명으로 `P2_Death` 후보는 될 수 있으나 경계 추정만으로 자동 추출하면 안 된다. 수동 셀 경계 확인 필요.

### `idle.png`

- `P1_Idle`, `8 frames`, 1~8 번호와 8칸 격자가 확인된다.
- 큰 검은 외곽 영역이 있고 `64x64` 같은 논리 프레임 크기는 명시되지 않았다.
- 결론: 정제 가능한 참고 시트지만 실제 셀 크기를 확인한 뒤 처리해야 한다.

### `move.png`

- 이미지 자체 제목이 `P1_Move (Sword Player)`다.
- 보스와 닮았다는 시각적 추측만으로 보스 애니메이션에 연결하면 서로 다른 개체 유형을 섞게 된다.
- 결론: 보스 원본이라는 확인 또는 올바른 보스 Move 이미지가 제공되기 전까지 `REFERENCE_ONLY_REJECTED`.

### `patten_1.png` ~ `patten_4.png`

- 모두 제목에 `(Boss)`가 있고 `14 Frames | 64x64`가 명시되어 있다.
- 프레임 번호와 격자는 캐릭터 영역과 분리되어 있으며 셀마다 캐릭터 한 명이 들어 있다.
- 결론: 현재 8개 중 가장 안전한 자동 분리 후보지만, 원본을 직접 변경하지 않고 별도 생성 경로로 출력해야 한다.

## 기존 보스 PPU와 Pivot 확인

현재 런타임 보스 프리팹은 다음 Sprite GUID를 참조한다.

- 프리팹: `Assets/02Scripts/HWJ/Prefabs/Generated/Bosses/HWJ_MidBoss1_Runtime_Prefab.prefab`
- 참조 GUID: `fa57d6926be293d4aa1331f481472c51`

그러나 현재 `Assets` 전체에서 이 GUID를 가진 `.meta` 파일을 찾을 수 없다. 데이터 모델이 가리키는 `HWJ_Model_MidBoss1_Placeholder.prefab`에도 실제 Sprite가 비어 있다. 따라서 현재 작업 트리만으로 기존 보스 Sprite의 권위 있는 PPU와 Importer Pivot을 확인할 수 없다.

Boss 폴더 이미지 메타에 적힌 PPU 100과 중앙 Pivot `(0.5, 0.5)`은 `textureType: Default` 상태의 값이며, 현재 보스용 Sprite 설정의 증거로 사용할 수 없다. 프리팹 내부에 보이는 `(0.5, 0)` 값도 SpriteImporter Pivot이 아니라 직렬화된 렌더러 데이터이므로 권위 있는 Importer 설정으로 간주하지 않았다.

추후 실제 Sprite를 생성할 때 기존 보스 원본 설정을 복구하지 못하면 기획 기준의 보류 권장값은 다음과 같다.

- PPU: 32
- Pivot: Bottom Center `(0.5, 0)`
- Filter Mode: Point
- Compression: None
- Mip Maps: Off
- Alpha Is Transparency: On

이 권장값은 이번 감사에서 적용하지 않았다.

## 다음 작업 전 확인 사항

1. `move.png`가 보스용인지 제작자에게 확인하거나 보스 전용 Move 원본으로 교체한다.
2. `boss2_patten.png`의 행별 논리 셀 크기와 추출 범위를 확정한다.
3. `death.png`와 `idle.png`의 실제 프레임 경계를 수동 확정한다.
4. 누락된 GUID `fa57d6926be293d4aa1331f481472c51`의 원본 Sprite 또는 올바른 메타를 복구해 기존 PPU/Pivot을 확인한다.
5. 승인 후에도 원본은 그대로 두고 `_Generated` 아래에만 정제본과 애니메이션을 생성한다.
