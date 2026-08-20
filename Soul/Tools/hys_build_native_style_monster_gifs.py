from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


# 실제 인게임 원본 픽셀을 확대·축소하지 않고 그대로 사용하는 네이티브 스킬 빌더입니다.
ROOT = Path(__file__).resolve().parents[1]
SPRITE_ROOT = ROOT / "Assets/05Anims/hys_Enemy_Anims"
GIF_ROOT = ROOT.parent / "hys_FinalVersion_15_GIFs"
REPORT_ROOT = ROOT / "hys_Reports/MonsterSkillFinal"

CANVAS = (160, 112)
GROUND_Y = 100
FRAME_MS = 90
GIF_SCALE = 4

SOURCE_ROOTS = {
    "Sword": ROOT / "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen",
    "Axe": ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions",
    "Bow": ROOT / "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions",
    "Lance": ROOT / "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions",
    "Shield": ROOT / "Assets/05Anims/hys_Player_Anims/Shield/Sprites",
}

BASE_X = {"Sword": 28, "Axe": 20, "Bow": 10, "Lance": 10, "Shield": 30}

def names(prefix: str, indices: list[int]) -> list[str]:
    return [f"{prefix}{index:02d}.png" for index in indices]


def build_specs() -> dict[str, dict[str, list[str]]]:
    """스킬마다 실제 원본 모션을 서로 다른 순서로 연결합니다."""
    return {
        "Sword": {
            # 큰 붉은 베기 궤적이 합쳐진 atk02~04는 제외합니다.
            "sword_diagonal_slash": ["R_Queen_idle00.png", "R_Queen_atk00.png", "R_Queen_atk01.png",
                                      "R_Queen_atk05.png", "R_Queen_atk06.png", "R_Queen_atk05.png",
                                      "R_Queen_atk01.png", "R_Queen_atk00.png", "R_Queen_idle01.png"],
            "sword_up_diagonal_slash": ["R_Queen_idle02.png", "R_Queen_atk00.png", "R_Queen_atk01.png",
                                         "R_Queen_atk05.png", "R_Queen_atk06.png", "R_Queen_atk05.png",
                                         "R_Queen_atk01.png", "R_Queen_atk00.png", "R_Queen_idle03.png"],
            "sword_wave": ["R_Queen_idle04.png", "R_Queen_atk00.png", "R_Queen_atk01.png",
                           "R_Queen_atk05.png", "R_Queen_atk06.png", "R_Queen_atk05.png",
                           "R_Queen_atk01.png", "R_Queen_atk00.png", "R_Queen_idle05.png"],
        },
        "Axe": {
            # 원형 도끼 궤적이 합쳐진 Attack1/2의 04번은 제외합니다.
            "axe_swing": ["hys_RedKing_Idle_02.png"] + names("hys_RedKing_Attack1_", list(range(4))) + [
                "hys_RedKing_Attack1_03.png", "hys_RedKing_Attack1_02.png",
                "hys_RedKing_Idle_03.png", "hys_RedKing_Idle_02.png"
            ],
            "axe_body_charge": ["hys_RedKing_Idle_00.png"] + names("hys_RedKing_Dash_", list(range(5))) + [
                "hys_RedKing_Attack2_03.png", "hys_RedKing_Dash_01.png", "hys_RedKing_Idle_01.png"
            ],
            "axe_spin_charge": ["hys_RedKing_Idle_04.png"] + names("hys_RedKing_Attack2_", list(range(4))) + [
                "hys_RedKing_Attack2_03.png", "hys_RedKing_Dash_03.png",
                "hys_RedKing_Attack2_01.png", "hys_RedKing_Idle_05.png"
            ],
        },
        "Bow": {
            "bow_air_arrow_shot": [
                "hys_RedBishop_Bow_Idle_00.png", "hys_RedBishop_Bow_JumpStart_00.png",
                "hys_RedBishop_Bow_JumpStart_02.png", "hys_RedBishop_Bow_JumpApex_01.png",
                "hys_RedBishop_Bow_Attack1_02.png", "hys_RedBishop_Bow_Attack1_04.png",
                "hys_RedBishop_Bow_JumpFall_01.png", "hys_RedBishop_Bow_JumpFall_02.png",
                "hys_RedBishop_Bow_Idle_01.png",
            ],
            "bow_rapid_shot": [
                "hys_RedBishop_Bow_Idle_02.png", "hys_RedBishop_Bow_Attack1_00.png",
                "hys_RedBishop_Bow_Attack1_02.png", "hys_RedBishop_Bow_Attack1_04.png",
                "hys_RedBishop_Bow_Attack2_00.png", "hys_RedBishop_Bow_Attack2_02.png",
                "hys_RedBishop_Bow_Attack2_04.png", "hys_RedBishop_Bow_Attack1_05.png",
                "hys_RedBishop_Bow_Idle_03.png",
            ],
            "bow_low_charge_shot": ["hys_RedBishop_Bow_Idle_04.png"] + names(
                "hys_RedBishop_Bow_Attack2_", list(range(6))
            ) + ["hys_RedBishop_Bow_Attack2_03.png", "hys_RedBishop_Bow_Idle_00.png"],
        },
        "Lance": {
            "lance_charge_thrust": ["hys_RedBishop_Idle_00.png"] + names(
                "hys_RedBishop_Dash_", list(range(6))
            ) + ["hys_RedBishop_Attack1_04.png", "hys_RedBishop_Idle_01.png"],
            "lance_thrust_combo": [
                "hys_RedBishop_Idle_02.png", "hys_RedBishop_Attack1_00.png",
                "hys_RedBishop_Attack1_02.png", "hys_RedBishop_Attack1_04.png",
                "hys_RedBishop_Attack2_00.png", "hys_RedBishop_Attack2_02.png",
                "hys_RedBishop_Attack2_04.png", "hys_RedBishop_Attack1_05.png",
                "hys_RedBishop_Idle_03.png",
            ],
            "lance_finisher_thrust": ["hys_RedBishop_Idle_04.png"] + names(
                "hys_RedBishop_Attack2_", list(range(6))
            ) + ["hys_RedBishop_Attack2_02.png", "hys_RedBishop_Idle_00.png"],
        },
        "Shield": {
            "shield_charge": ["hys_RedRook_Idle_00.png"] + names("hys_RedRook_Dash_", list(range(6))) + [
                "hys_RedRook_Attack_04.png", "hys_RedRook_Idle_01.png"
            ],
            "shield_slam": [
                # 속도선·착지 폭발이 합쳐진 PlungeFall/Land 프레임은 사용하지 않습니다.
                "hys_RedRook_Idle_02.png", "hys_RedRook_PlungeStart_00.png",
                "hys_RedRook_PlungeStart_01.png", "hys_RedRook_PlungeStart_02.png",
                "hys_RedRook_PlungeStart_01.png", "hys_RedRook_PlungeStart_00.png",
                "hys_RedRook_Attack_02.png", "hys_RedRook_Attack_03.png",
                "hys_RedRook_Idle_03.png",
            ],
            "shield_diagonal_knockback": ["hys_RedRook_Idle_01.png"] + names(
                "hys_RedRook_Attack_", list(range(6))
            ) + ["hys_RedRook_Attack_02.png", "hys_RedRook_Idle_02.png"],
        },
    }


X_MOTION = {
    "sword_diagonal_slash": [0, 0, 1, 2, 3, 3, 2, 1, 0],
    "sword_up_diagonal_slash": [5, 5, 6, 7, 8, 8, 7, 6, 5],
    "sword_wave": [10, 10, 11, 12, 13, 13, 12, 11, 10],
    "axe_swing": [0, 1, 2, 3, 4, 5, 6, 7, 8],
    "axe_body_charge": [-1, 1, 5, 11, 19, 27, 31, 14, -1],
    "axe_spin_charge": [1, 2, 5, 9, 14, 19, 15, 7, 1],
    "bow_air_arrow_shot": [0, 1, 3, 5, 7, 9, 8, 4, 0],
    "bow_rapid_shot": [-1, -1, 0, 0, 1, 1, 2, 1, -1],
    "bow_low_charge_shot": [5, 5, 6, 7, 8, 9, 10, 11, 5],
    "lance_charge_thrust": [0, 2, 6, 12, 20, 28, 34, 16, 0],
    "lance_thrust_combo": [-1, 2, 0, 4, 1, 6, 2, 1, -1],
    "lance_finisher_thrust": [1, 0, 2, 4, 8, 12, 8, 5, 1],
    "shield_charge": [0, 2, 6, 12, 20, 28, 34, 16, 0],
    "shield_slam": [-1, -1, 0, 0, 1, 2, 12, 1, -1],
    "shield_diagonal_knockback": [1, 1, 2, 3, 5, 6, 4, 2, 1],
}

Y_MOTION = {
    "bow_air_arrow_shot": [0, -4, -10, -16, -20, -18, -12, -5, 0],
}


def compose_frame(weapon: str, skill: str, index: int, source_name: str) -> tuple[Image.Image, dict[str, object]]:
    source_path = SOURCE_ROOTS[weapon] / source_name
    source = Image.open(source_path).convert("RGBA")
    x = BASE_X[weapon] + X_MOTION[skill][index]
    y = GROUND_Y - source.height + Y_MOTION.get(skill, [0] * 9)[index]
    frame = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    # 별도 이펙트 없이 실제 원본 캐릭터 프레임만 그대로 복사합니다.
    opaque_source_mask = source.getchannel("A").point(lambda alpha: 255 if alpha > 0 else 0)
    frame.paste(source, (x, y), opaque_source_mask)
    return frame, {
        "source": str(source_path),
        "source_size": list(source.size),
        "native_scale": 1,
        "offset": [x, y],
    }


def save_gif(frames: list[Image.Image], path: Path) -> None:
    scaled = [frame.resize((CANVAS[0] * GIF_SCALE, CANVAS[1] * GIF_SCALE), Image.Resampling.NEAREST) for frame in frames]
    scaled[0].save(
        path,
        save_all=True,
        append_images=scaled[1:],
        duration=FRAME_MS,
        loop=0,
        disposal=2,
        transparency=0,
        optimize=False,
    )


def build_all() -> list[dict[str, object]]:
    specs = build_specs()
    results: list[dict[str, object]] = []
    GIF_ROOT.mkdir(parents=True, exist_ok=True)
    for weapon, skills in specs.items():
        for skill, sources in skills.items():
            if len(sources) != 9:
                raise ValueError(f"9프레임이 아닙니다: {weapon}/{skill} = {len(sources)}")
            frames: list[Image.Image] = []
            manifest: list[dict[str, object]] = []
            for index, source_name in enumerate(sources):
                frame, record = compose_frame(weapon, skill, index, source_name)
                frames.append(frame)
                record["index"] = index
                manifest.append(record)

            frame_dir = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
            frame_dir.mkdir(parents=True, exist_ok=True)
            hashes: list[str] = []
            for index, frame in enumerate(frames):
                frame_path = frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png"
                frame.save(frame_path)
                hashes.append(hashlib.sha256(frame.tobytes()).hexdigest())

            gif_path = GIF_ROOT / f"hys_Final_Enemy_{weapon}_{skill}.gif"
            save_gif(frames, gif_path)
            results.append({
                "weapon": weapon,
                "skill": skill,
                "gif": str(gif_path),
                "unique_frames": len(set(hashes)),
                "native_character_pixels": True,
                "manifest": manifest,
            })
    return results


def build_contact(results: list[dict[str, object]]) -> None:
    scale = 3
    label_width = 220
    row_height = CANVAS[1] * scale
    sheet = Image.new("RGB", (label_width + CANVAS[0] * scale * 9, row_height * 15), "#1c2029")
    draw = ImageDraw.Draw(sheet)
    for row, result in enumerate(results):
        weapon = str(result["weapon"])
        skill = str(result["skill"])
        y = row * row_height
        draw.text((8, y + 8), f"{weapon} / {skill}", fill="white")
        draw.text((8, y + 28), "NATIVE 1:1 PIXELS", fill="#f0c75e")
        frame_dir = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
        for index in range(9):
            frame = Image.open(frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png").convert("RGBA")
            frame = frame.resize((CANVAS[0] * scale, CANVAS[1] * scale), Image.Resampling.NEAREST)
            bg = Image.new("RGBA", frame.size, "#1c2029")
            bg.alpha_composite(frame)
            sheet.paste(bg.convert("RGB"), (label_width + index * CANVAS[0] * scale, y))
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    sheet.save(REPORT_ROOT / "hys_NativeStyle_15_AllFrames.png")


def build_preview(results: list[dict[str, object]]) -> None:
    columns, rows, scale = 3, 5, 2
    cell_width = CANVAS[0] * scale
    cell_height = CANVAS[1] * scale + 22
    previews: list[Image.Image] = []
    for frame_index in range(9):
        preview = Image.new("RGB", (cell_width * columns, cell_height * rows), "#1c2029")
        draw = ImageDraw.Draw(preview)
        for index, result in enumerate(results):
            weapon = str(result["weapon"])
            skill = str(result["skill"])
            path = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill / f"hys_Enemy_{weapon}_{skill}_{frame_index:02d}.png"
            sprite = Image.open(path).convert("RGBA").resize((CANVAS[0] * scale, CANVAS[1] * scale), Image.Resampling.NEAREST)
            x = (index % columns) * cell_width
            y = (index // columns) * cell_height
            draw.text((x + 5, y + 4), f"{weapon} / {skill}", fill="white")
            bg = Image.new("RGBA", sprite.size, "#1c2029")
            bg.alpha_composite(sprite)
            preview.paste(bg.convert("RGB"), (x, y + 22))
        previews.append(preview)
    previews[0].save(
        REPORT_ROOT / "hys_NativeStyle_15_AnimatedPreview.gif",
        save_all=True,
        append_images=previews[1:],
        duration=FRAME_MS,
        loop=0,
        optimize=False,
    )


def main() -> None:
    # 무기별 다른 캐릭터를 섞지 않고 실제 주황색 Red King 통합 생성기로 위임합니다.
    from hys_generate_orange_redking_actualbody_all15 import main as build_red_king_all15

    build_red_king_all15()
    return

    results = build_all()
    build_contact(results)
    build_preview(results)
    (REPORT_ROOT / "hys_NativeStyle_SourceManifest.json").write_text(
        json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"네이티브 원본 픽셀 기반 15개 GIF 출력 완료: {GIF_ROOT}")


if __name__ == "__main__":
    main()
