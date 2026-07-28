from __future__ import annotations

import re
from pathlib import Path


# 플레이어의 공통 모션을 무기 몬스터 5종 전용 클립에 복사한다.
PROJECT_ROOT = Path(__file__).resolve().parents[1]
PLAYER_ROOT = PROJECT_ROOT / "Assets" / "05Anims" / "hys_Player_Anims"
LEGACY_PLAYER_ROOT = PROJECT_ROOT / "Assets" / "05Anims" / "Player_Anims"
ENEMY_ROOT = PROJECT_ROOT / "Assets" / "05Anims" / "hys_Enemy_Anims"


CONFIG = {
    "Sword": {
        "sources": {
            "Idle": LEGACY_PLAYER_ROOT / "hys_Sword_Idle.anim",
            "Walk": LEGACY_PLAYER_ROOT / "hys_Sword_Run.anim",
            "Hit": LEGACY_PLAYER_ROOT / "hys_Sword_Hit.anim",
            "Death": LEGACY_PLAYER_ROOT / "hys_Sword_Die.anim",
        },
        "attacks": (
            "hys_Enemy_Sword_sword_diagonal_slash.anim",
            "hys_Enemy_Sword_sword_up_diagonal_slash.anim",
            "hys_Enemy_Sword_sword_wave.anim",
        ),
    },
    "Axe": {
        "sources": {
            key: PLAYER_ROOT / "Axe" / "Clips" / f"hys_Player_Axe_{source}.anim"
            for key, source in {"Idle": "Idle", "Walk": "Run", "Hit": "Hit", "Death": "Die"}.items()
        },
        "attacks": (
            "hys_Enemy_Axe_axe_swing.anim",
            "hys_Enemy_Axe_axe_spin_charge.anim",
            "hys_Enemy_Axe_axe_body_charge.anim",
        ),
    },
    "Bow": {
        "sources": {
            key: PLAYER_ROOT / "Bow" / "Clips" / f"hys_Player_Bow_Bishop_{source}.anim"
            for key, source in {"Idle": "Idle", "Walk": "Run", "Hit": "Hit", "Death": "Die"}.items()
        },
        "attacks": (
            "hys_Enemy_Bow_bow_rapid_shot.anim",
            "hys_Enemy_Bow_bow_low_charge_shot.anim",
            "hys_Enemy_Bow_bow_air_arrow_shot.anim",
        ),
    },
    "Lance": {
        "sources": {
            key: PLAYER_ROOT / "Lance" / "Clips" / f"hys_Player_Lance_{source}.anim"
            for key, source in {"Idle": "Idle", "Walk": "Run", "Hit": "Hit", "Death": "Die"}.items()
        },
        "attacks": (
            "hys_Enemy_Lance_lance_thrust_combo.anim",
            "hys_Enemy_Lance_lance_charge_thrust.anim",
            "hys_Enemy_Lance_lance_finisher_thrust.anim",
        ),
    },
    "Shield": {
        "sources": {
            key: PLAYER_ROOT / "Shield" / "Clips" / f"hys_Player_Shield_{source}.anim"
            for key, source in {"Idle": "Idle", "Walk": "Run", "Hit": "Hit", "Death": "Die"}.items()
        },
        "attacks": (
            "hys_Enemy_Shield_shield_charge.anim",
            "hys_Enemy_Shield_shield_diagonal_knockback.anim",
            "hys_Enemy_Shield_shield_slam.anim",
        ),
    },
}


def rename_clip(text: str, clip_name: str) -> str:
    """Unity 애니메이션의 첫 번째 이름만 몬스터 전용 이름으로 변경한다."""
    changed, count = re.subn(r"(?m)^  m_Name: .*$", f"  m_Name: {clip_name}", text, count=1)
    if count != 1:
        raise ValueError(f"클립 이름을 찾지 못했습니다: {clip_name}")
    return changed


def first_sprite_reference(idle_text: str) -> str:
    """Idle 클립의 첫 번째 스프라이트 참조를 추출한다."""
    match = re.search(r"(?m)^      value: (\{fileID: .+\})$", idle_text)
    if not match:
        raise ValueError("Idle 클립에서 첫 번째 스프라이트를 찾지 못했습니다.")
    return match.group(1)


def make_attack_placeholder(clip_name: str, sprite_reference: str) -> str:
    """전용 공격 제작 전까지 Idle 첫 프레임만 보여주는 비루프 클립을 만든다."""
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {clip_name}
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
    - time: 0
      value: {sprite_reference}
    attribute: m_Sprite
    path: 
    classID: 212
    script: {{fileID: 0}}
    flags: 2
  m_SampleRate: 8
  m_WrapMode: 0
  m_Bounds:
    m_Center: {{x: 0, y: 0, z: 0}}
    m_Extent: {{x: 0, y: 0, z: 0}}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {{fileID: 0}}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
    - {sprite_reference}
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {{fileID: 0}}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: 0.125
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 0
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
"""


def sync_monster(monster_type: str, config: dict) -> None:
    clip_dir = ENEMY_ROOT / monster_type / "Clips"
    source_texts = {
        motion: path.read_text(encoding="utf-8-sig")
        for motion, path in config["sources"].items()
    }

    for motion, source_text in source_texts.items():
        target = clip_dir / f"hys_Enemy_{monster_type}_{motion}.anim"
        target.write_text(rename_clip(source_text, target.stem), encoding="utf-8", newline="\n")

    idle_sprite = first_sprite_reference(source_texts["Idle"])
    for attack_name in config["attacks"]:
        target = clip_dir / attack_name
        target.write_text(
            make_attack_placeholder(target.stem, idle_sprite),
            encoding="utf-8",
            newline="\n",
        )


def main() -> None:
    for monster_type, config in CONFIG.items():
        sync_monster(monster_type, config)
        print(f"완료: {monster_type} 공통 4개 + 공격 임시 3개")


if __name__ == "__main__":
    main()
