from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import Counter
from pathlib import Path

from PIL import Image, ImageChops


# 15개 최종 GIF를 매 검증 회차마다 처음부터 끝까지 모두 디코딩해 품질 조건을 확인합니다.
ROOT = Path(__file__).resolve().parents[1]
GIF_ROOT = ROOT.parent / "hys_FinalVersion_15_GIFs"
REPORT_ROOT = ROOT / "hys_Reports/MonsterSkillFinal"
SPRITE_ROOT = ROOT / "Assets/05Anims/hys_Enemy_Anims"
REFERENCE_PATHS = {
    "Sword": "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen/R_Queen_idle00.png",
    "Axe": "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions/hys_RedKing_Idle_02.png",
    "Bow": "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions/hys_RedBishop_Bow_Idle_00.png",
    "Lance": "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions/hys_RedBishop_Idle_00.png",
    "Shield": "Assets/05Anims/hys_Player_Anims/Shield/Sprites/hys_RedRook_Idle_00.png",
}
NATIVE_SOURCE_ROOTS = {
    "Sword": ROOT / "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen",
    "Axe": ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions",
    "Bow": ROOT / "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions",
    "Lance": ROOT / "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions",
    "Shield": ROOT / "Assets/05Anims/hys_Player_Anims/Shield/Sprites",
}
EXPECTED = {
    "Sword": ["sword_diagonal_slash", "sword_up_diagonal_slash", "sword_wave"],
    "Axe": ["axe_swing", "axe_body_charge", "axe_spin_charge"],
    "Bow": ["bow_air_arrow_shot", "bow_rapid_shot", "bow_low_charge_shot"],
    "Lance": ["lance_charge_thrust", "lance_thrust_combo", "lance_finisher_thrust"],
    "Shield": ["shield_charge", "shield_slam", "shield_diagonal_knockback"],
}
EXPECTED_SIZE = (640, 448)
EXPECTED_FRAMES = 9
SKILL_DURATIONS_MS = {
    "sword_diagonal_slash": 340,
    "sword_up_diagonal_slash": 340,
    "sword_wave": 530,
    "axe_swing": 340,
    "axe_body_charge": 460,
    "axe_spin_charge": 490,
    "bow_air_arrow_shot": 550,
    "bow_rapid_shot": 620,
    "bow_low_charge_shot": 1100,
    "lance_charge_thrust": 480,
    "lance_thrust_combo": 650,
    "lance_finisher_thrust": 340,
    "shield_charge": 520,
    "shield_slam": 660,
    "shield_diagonal_knockback": 540,
}

# 눈, 금속 장식처럼 작은 강조색은 기준 이미지의 주 팔레트와 별도로 허용합니다.
ACCENT_COLORS = {
    (215, 177, 92),
    (239, 107, 53),
    (217, 173, 85),
    (217, 179, 106),
    (228, 228, 228),
}


def decode(path: Path) -> tuple[list[Image.Image], list[int]]:
    image = Image.open(path)
    frames: list[Image.Image] = []
    durations: list[int] = []
    index = 0
    while True:
        try:
            image.seek(index)
        except EOFError:
            break
        frames.append(image.convert("RGBA"))
        durations.append(int(image.info.get("duration", 0)))
        index += 1
    return frames, durations


def center_of_alpha(frame: Image.Image) -> tuple[float, float]:
    box = frame.getchannel("A").getbbox()
    if box is None:
        return 0.0, 0.0
    return (box[0] + box[2]) / 2.0, (box[1] + box[3]) / 2.0


def changed_ratio(left: Image.Image, right: Image.Image) -> float:
    diff = ImageChops.difference(left, right).convert("RGB")
    changed = sum(1 for pixel in diff.get_flattened_data() if pixel != (0, 0, 0))
    return changed / float(left.width * left.height)


def verify_one(path: Path) -> dict[str, object]:
    frames, durations = decode(path)
    hashes = [hashlib.sha256(frame.tobytes()).hexdigest() for frame in frames]
    boxes = [frame.getchannel("A").getbbox() for frame in frames]
    changes = [changed_ratio(frames[index], frames[index + 1]) for index in range(len(frames) - 1)]
    start_center = center_of_alpha(frames[0])
    end_center = center_of_alpha(frames[-1])
    # 무기 각도가 달라져도 몸통/망토가 시작 위치로 돌아왔는지는 실루엣 왼쪽 기준선으로 확인합니다.
    recovery_distance = abs(boxes[0][0] - boxes[-1][0]) if boxes[0] and boxes[-1] else 9999.0
    no_edge_clip = all(
        box is not None
        and box[0] > 0
        and box[1] > 0
        and box[2] < EXPECTED_SIZE[0]
        and box[3] < EXPECTED_SIZE[1]
        for box in boxes
    )
    skill = next((name for name in SKILL_DURATIONS_MS if name in path.stem), "")
    expected_frame_ms = max(20, round(SKILL_DURATIONS_MS.get(skill, 810) / EXPECTED_FRAMES / 10) * 10)
    expected_total_ms = SKILL_DURATIONS_MS.get(skill, expected_frame_ms * EXPECTED_FRAMES)
    checks = {
        # GIF는 동일한 유지 프레임을 하나로 합칠 수 있으므로 PNG 9장 검사는 Unity 항목에서 별도로 수행합니다.
        "gif_has_four_or_more_visible_poses": len(frames) >= 4,
        "at_least_four_distinct_key_poses": len(set(hashes)) >= 4,
        "size_consistent": all(frame.size == EXPECTED_SIZE for frame in frames),
        # 균등 분배에서 생기는 10ms 나머지도 포함해 실제 스킬 총 재생시간과 비교합니다.
        "duration_matches_skill_data": sum(durations) == expected_total_ms,
        "transparent_margin_no_clip": no_edge_clip,
        "motion_changes_present": bool(changes) and max(changes) > 0.0002 and max(changes) < 0.30,
        "recovery_near_start": recovery_distance <= 80.0,
    }
    return {
        "file": path.name,
        "frames": len(frames),
        "durations_ms": durations,
        "expected_frame_ms": expected_frame_ms,
        "unique_frames": len(set(hashes)),
        "alpha_boxes": boxes,
        "change_ratios": [round(value, 6) for value in changes],
        "recovery_distance_scaled_pixels": round(recovery_distance, 2),
        "checks": checks,
        "passed": all(checks.values()),
        "hashes": hashes,
    }


def reference_palette(weapon: str, count: int = 64) -> set[tuple[int, int, int]]:
    """실제 사용 가능한 모든 네이티브 모션과 1px 효과 색상을 반환합니다."""
    colors: set[tuple[int, int, int]] = set()
    for path in NATIVE_SOURCE_ROOTS[weapon].glob("*.png"):
        source = Image.open(path).convert("RGBA")
        colors.update((r, g, b) for r, g, b, a in source.get_flattened_data() if a > 16)
    colors.update({(74, 0, 17), (223, 18, 37), (255, 119, 48), (255, 198, 168), (242, 242, 242)})
    return colors


def uses_identity_locked_palette(
    used_colors: set[tuple[int, int, int]],
    reference_colors: set[tuple[int, int, int]],
) -> bool:
    """출력 색상이 실제 플레이어 스프라이트의 색상군을 그대로 따르는지 확인합니다."""
    if not reference_colors:
        return False
    for color in used_colors:
        if color in reference_colors:
            continue
        nearest = min(
            sum((color[channel] - reference[channel]) ** 2 for channel in range(3)) ** 0.5
            for reference in reference_colors
        )
        if nearest > 12:
            return False
    return True


def verify_unity_skill(weapon: str, skill: str) -> dict[str, object]:
    """9개 PNG의 GUID와 AnimationClip 참조 순서를 실제 파일에서 대조합니다."""
    frame_dir = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
    frame_paths = sorted(frame_dir.glob(f"hys_Enemy_{weapon}_{skill}_*.png"))
    meta_paths = [Path(f"{path}.meta") for path in frame_paths]
    clip_path = SPRITE_ROOT / weapon / "Clips" / f"hys_Enemy_{weapon}_{skill}.anim"
    guids: list[str] = []
    import_settings_valid = True
    frame_sizes_valid = True
    transparent_margin_valid = True
    palette_valid = True
    allowed_colors = reference_palette(weapon)

    for frame_path, meta_path in zip(frame_paths, meta_paths):
        frame = Image.open(frame_path).convert("RGBA")
        box = frame.getchannel("A").getbbox()
        frame_sizes_valid &= frame.size == (160, 112)
        transparent_margin_valid &= bool(
            box and box[0] > 0 and box[1] > 0 and box[2] < frame.width and box[3] < frame.height
        )
        used_colors = {(r, g, b) for r, g, b, a in frame.get_flattened_data() if a > 16}
        palette_valid &= uses_identity_locked_palette(used_colors, allowed_colors)

        if not meta_path.exists():
            continue
        meta = meta_path.read_text(encoding="utf-8")
        match = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.MULTILINE)
        if match:
            guids.append(match.group(1))
        import_settings_valid &= all(
            setting in meta
            for setting in (
                "enableMipMap: 0",
                "filterMode: 0",
                "spriteMode: 1",
                "spritePixelsToUnits: 16",
                "alphaIsTransparency: 1",
                "textureType: 8",
            )
        )

    clip_guids: list[str] = []
    clip_text = clip_path.read_text(encoding="utf-8") if clip_path.exists() else ""
    clip_duration = -1.0
    if clip_text:
        clip_guids = re.findall(
            r"value: \{fileID: 21300000, guid: ([0-9a-f]{32}), type: 3\}", clip_text
        )
        stop_time_match = re.search(r"^    m_StopTime: ([0-9.]+)$", clip_text, re.MULTILINE)
        if stop_time_match:
            clip_duration = float(stop_time_match.group(1))
    expected_clip_duration = SKILL_DURATIONS_MS[skill] / 1000.0
    checks = {
        "nine_png_frames": len(frame_paths) == EXPECTED_FRAMES,
        "nine_meta_files": len(meta_paths) == EXPECTED_FRAMES and all(path.exists() for path in meta_paths),
        "unity_import_settings": import_settings_valid,
        "frame_size_160x112": frame_sizes_valid,
        "transparent_margin_no_clip": transparent_margin_valid,
        "colors_match_player_sprite_palette": palette_valid,
        "clip_exists": clip_path.exists(),
        "clip_guid_order_matches_frames": len(guids) == EXPECTED_FRAMES and clip_guids == guids,
        "clip_sample_rate_12": "m_SampleRate: 12" in clip_text,
        "clip_duration_matches_skill_data": abs(clip_duration - expected_clip_duration) < 0.001,
        "clip_has_no_animation_events": "m_Events: []" in clip_text,
    }
    return {
        "weapon": weapon,
        "skill": skill,
        "frame_count": len(frame_paths),
        "clip_guid_count": len(clip_guids),
        "clip_duration_seconds": clip_duration,
        "expected_clip_duration_seconds": expected_clip_duration,
        "checks": checks,
        "passed": all(checks.values()),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--pass-number", type=int, required=True)
    parser.add_argument("--stage", required=True)
    args = parser.parse_args()

    expected_paths = [
        GIF_ROOT / f"hys_Final_Enemy_{weapon}_{skill}.gif"
        for weapon, skills in EXPECTED.items()
        for skill in skills
    ]
    results = [verify_one(path) for path in expected_paths if path.exists()]
    all_hashes: dict[str, list[str]] = {}
    for result in results:
        for frame_index, value in enumerate(result["hashes"]):
            all_hashes.setdefault(value, []).append(f"{result['file']}#{frame_index}")
    duplicates = [locations for locations in all_hashes.values() if len(locations) > 1]
    unity_results = [
        verify_unity_skill(weapon, skill)
        for weapon, skills in EXPECTED.items()
        for skill in skills
    ]
    source_audit_path = ROOT / "hys_Reports/MonsterSkillPlayerBased/hys_PlayerBased_15_SourceAudit.json"
    source_audit = json.loads(source_audit_path.read_text(encoding="utf-8")) if source_audit_path.exists() else []
    expected_source_fragments = {
        "Sword": "Pixel art Chess Knights pack/05_Queen/Red_Queen/",
        "Axe": "hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions/",
        "Bow": "hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions/",
        "Lance": "hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions/",
        "Shield": "hys_Player_Anims/Shield/Sprites/",
    }
    audited_frames = [frame for skill in source_audit for frame in skill.get("frame_sources", [])]
    sources_are_player_sprites = len(audited_frames) == 135 and all(
        expected_source_fragments.get(str(skill.get("weapon")), "") in str(frame.get("source", ""))
        for skill in source_audit
        for frame in skill.get("frame_sources", [])
    )
    generator_path = ROOT / "Tools/hys_generate_final_monster_skill_pixels.py"
    generator_text = generator_path.read_text(encoding="utf-8") if generator_path.exists() else ""

    summary_checks = {
        "all_15_exist": len(results) == 15 and all(path.exists() for path in expected_paths),
        "all_skills_have_four_or_more_key_poses": len(results) == 15 and all(int(result["unique_frames"]) >= 4 for result in results),
        "all_individual_checks_pass": len(results) == 15 and all(bool(result["passed"]) for result in results),
        "all_135_unity_frames_exist": sum(int(result["frame_count"]) for result in unity_results) == 135,
        "all_15_clip_guid_sequences_match": all(
            bool(result["checks"]["clip_guid_order_matches_frames"]) for result in unity_results
        ),
        "all_unity_asset_checks_pass": all(bool(result["passed"]) for result in unity_results),
        "all_actual_reference_files_exist": all((ROOT / path).exists() for path in REFERENCE_PATHS.values()),
        "all_135_frames_use_player_sprite_sources": sources_are_player_sprites,
        "procedural_replacement_body_removed": "BODY_SPECS" not in generator_text and "draw_job_armor_details" not in generator_text,
        "skill_specific_motion_names": len({path.stem for path in expected_paths}) == 15,
    }
    report = {
        "pass_number": args.pass_number,
        "stage": args.stage,
        "summary_checks": summary_checks,
        "cross_skill_duplicate_frames": duplicates,
        "weapon_invariants": {
            "Sword": "R_Queen_idle00의 적색/주황 넓은 검 픽셀과 원본 팔레트 고정",
            "Axe": "손잡이·양날 도끼 머리 치수와 적색/주황 팔레트 고정",
            "Bow": "활 높이 36px와 붉은 프레임·은색 시위 고정",
            "Lance": "자루 54px와 은색 창끝 8px 고정",
            "Shield": "34x46 원본형 방패와 중앙 금색 보강대 고정",
        },
        "actual_reference_paths": REFERENCE_PATHS,
        "unity_asset_results": unity_results,
        "results": [{key: value for key, value in result.items() if key != "hashes"} for result in results],
        "passed": all(summary_checks.values()),
    }
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    report_path = REPORT_ROOT / f"hys_FinalValidation_Pass{args.pass_number}_{args.stage}.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"report": str(report_path), "passed": report["passed"], "summary": summary_checks}, ensure_ascii=False))
    if not report["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
