from __future__ import annotations

import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw

import hys_generate_final_monster_skill_pixels as drawlib


# 실제 주황색 Red King 몸체 픽셀을 기반으로 15개 스킬을 통일하는 생성기입니다.
ROOT = Path(__file__).resolve().parents[1]
SOURCE_ROOT = (
    ROOT
    / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions"
)
OUTPUT_ROOT = ROOT / "hys_Reports/MonsterSkillOrangeReplacementActualBody"
FRAME_ROOT = OUTPUT_ROOT / "hys_Frames"
GIF_ROOT = OUTPUT_ROOT / "hys_GIFs"
PUBLISH_FRAME_ROOT = ROOT / "Assets/05Anims/hys_Enemy_Anims"
PUBLISH_GIF_ROOT = ROOT.parent / "hys_FinalVersion_15_GIFs"
PUBLISH_REPORT_ROOT = ROOT / "hys_Reports/MonsterSkillFinal"

CANVAS = (160, 112)
GROUND_Y = 100
FRAME_COUNT = 9
FRAME_MS = 90
GIF_SCALE = 4

PALETTE = {
    "outline": "#090909",
    "dark": "#212121",
    "armor": "#777777",
    "light": "#cccccc",
    "red": "#4a0011",
    "bright": "#df1225",
    "gold": "#ff7730",
    "effect": "#ffc6a8",
}

SKILLS = {
    "Sword": ["sword_diagonal_slash", "sword_up_diagonal_slash", "sword_wave"],
    "Axe": ["axe_swing", "axe_body_charge", "axe_spin_charge"],
    "Bow": ["bow_air_arrow_shot", "bow_rapid_shot", "bow_low_charge_shot"],
    "Lance": ["lance_charge_thrust", "lance_thrust_combo", "lance_finisher_thrust"],
    "Shield": ["shield_charge", "shield_slam", "shield_diagonal_knockback"],
}

IDLE_FRAMES = [2, 3, 4, 5, 4, 3, 2, 3, 2]

X_MOTION = {
    "sword_diagonal_slash": [0, 0, 1, 2, 3, 3, 2, 1, 0],
    "sword_up_diagonal_slash": [0, 0, 1, 2, 3, 3, 2, 1, 0],
    "sword_wave": [0, 0, 1, 3, 5, 6, 4, 2, 0],
    "bow_air_arrow_shot": [0, 1, 3, 5, 7, 8, 7, 4, 0],
    "bow_rapid_shot": [0, 0, 1, 1, 2, 2, 1, 1, 0],
    "bow_low_charge_shot": [0, 0, 1, 1, 2, 2, 2, 1, 0],
    "lance_charge_thrust": [0, 2, 6, 12, 20, 28, 34, 16, 0],
    "lance_thrust_combo": [0, 4, 1, 6, 2, 8, 3, 1, 0],
    "lance_finisher_thrust": [0, 0, 2, 5, 10, 16, 20, 8, 0],
    "shield_charge": [0, 2, 6, 12, 20, 28, 34, 16, 0],
    "shield_slam": [0, 0, 0, 1, 2, 3, 2, 1, 0],
    "shield_diagonal_knockback": [0, 0, 1, 2, 4, 6, 4, 2, 0],
}

Y_MOTION = {
    "bow_air_arrow_shot": [0, -4, -10, -16, -20, -17, -11, -5, 0],
    "shield_slam": [0, -3, -7, -11, -5, 3, 2, 1, 0],
}

WEAPON_ANGLES = {
    "sword_diagonal_slash": [-70, -55, -30, -5, 25, 50, 68, 30, 0],
    "sword_up_diagonal_slash": [45, 32, 15, -5, -28, -48, -68, -26, 0],
    "sword_wave": [-45, -30, -12, 5, 18, 35, 52, 20, 0],
    "lance_charge_thrust": [5, 3, 0, 0, 0, 0, 0, 3, 7],
    "lance_thrust_combo": [8, -4, 7, -10, 8, -3, 6, 2, 5],
    "lance_finisher_thrust": [12, 8, 4, 1, 0, 0, 0, 3, 8],
    "shield_charge": [0, 0, 0, 0, 0, 0, 0, 0, 0],
    "shield_slam": [-65, -78, -92, -105, -55, 5, 35, 18, 0],
    "shield_diagonal_knockback": [25, 18, 8, -10, -28, -45, -60, -25, 0],
}

AXE_SOURCES = {
    "axe_swing": [
        "hys_RedKing_Idle_02.png",
        "hys_RedKing_Attack1_00.png",
        "hys_RedKing_Attack1_01.png",
        "hys_RedKing_Attack1_02.png",
        "hys_RedKing_Attack1_03.png",
        "hys_RedKing_PlungeLand_03.png",
        "hys_RedKing_Attack1_02.png",
        "hys_RedKing_Attack1_01.png",
        "hys_RedKing_Idle_02.png",
    ],
    "axe_body_charge": [
        "hys_RedKing_Idle_00.png",
        "hys_RedKing_Dash_00.png",
        "hys_RedKing_Dash_01.png",
        "hys_RedKing_Dash_02.png",
        "hys_RedKing_Dash_03.png",
        "hys_RedKing_Dash_04.png",
        "hys_RedKing_Attack2_03.png",
        "hys_RedKing_Dash_01.png",
        "hys_RedKing_Idle_01.png",
    ],
    "axe_spin_charge": [
        "hys_RedKing_Idle_04.png",
        "hys_RedKing_Attack2_00.png",
        "hys_RedKing_Attack2_01.png",
        "hys_RedKing_Attack2_02.png",
        "hys_RedKing_Attack2_03.png",
        "hys_RedKing_Attack1_03.png",
        "hys_RedKing_Dash_03.png",
        "hys_RedKing_Attack2_01.png",
        "hys_RedKing_Idle_05.png",
    ],
}

AXE_X = {
    "axe_swing": [0, 0, 1, 2, 3, 3, 2, 1, 0],
    "axe_body_charge": [0, 2, 6, 12, 20, 28, 31, 14, 0],
    "axe_spin_charge": [0, 2, 5, 9, 14, 18, 15, 7, 0],
}


def copy_visible_pixels(destination: Image.Image, source: Image.Image, xy: tuple[int, int]) -> None:
    """반투명 RGB를 재계산하지 않고 보이는 원본 픽셀을 그대로 복사합니다."""
    mask = source.getchannel("A").point(lambda alpha: 255 if alpha > 0 else 0)
    destination.paste(source, xy, mask)


def cleaned_red_king_body(frame_index: int) -> Image.Image:
    """원본 Idle 프레임에서 도끼 머리와 손잡이만 제거한 Red King 몸체를 만듭니다."""
    source = Image.open(
        SOURCE_ROOT / f"hys_RedKing_Idle_{IDLE_FRAMES[frame_index]:02d}.png"
    ).convert("RGBA")
    pixels = source.load()
    erase = Image.new("L", source.size, 0)
    draw = ImageDraw.Draw(erase)

    # 오른쪽의 도끼 머리와 몸 앞으로 내려오는 손잡이만 지웁니다.
    draw.rectangle((42, 14, 69, 51), fill=255)
    draw.line([(47, 43), (43, 53), (34, 69)], fill=255, width=9)
    draw.ellipse((31, 55, 43, 69), fill=255)

    erase_pixels = erase.load()
    for y in range(source.height):
        for x in range(source.width):
            if erase_pixels[x, y] > 0:
                pixels[x, y] = (0, 0, 0, 0)
    return source


def draw_replacement_weapon(
    frame: Image.Image,
    weapon: str,
    skill: str,
    frame_index: int,
    hand: tuple[int, int],
) -> None:
    draw = ImageDraw.Draw(frame)
    if weapon == "Sword":
        drawlib.draw_sword(draw, hand, WEAPON_ANGLES[skill][frame_index], PALETTE)
    elif weapon == "Bow":
        angle = 2 if skill != "bow_air_arrow_shot" else 18
        if skill == "bow_rapid_shot":
            drawn = [2, 5, 8, 3, 7, 2, 8, 4, 2][frame_index]
        elif skill == "bow_low_charge_shot":
            drawn = [2, 3, 5, 7, 9, 9, 5, 3, 2][frame_index]
        else:
            drawn = [2, 3, 5, 7, 9, 3, 2, 2, 2][frame_index]
        drawlib.draw_bow(draw, hand, angle, PALETTE, drawn)
    elif weapon == "Lance":
        drawlib.draw_lance(draw, hand, WEAPON_ANGLES[skill][frame_index], PALETTE)
    elif weapon == "Shield":
        drawlib.draw_shield(draw, hand, WEAPON_ANGLES[skill][frame_index], PALETTE)


def remove_small_detached_pixels(image: Image.Image, minimum_area: int = 24) -> None:
    """본체·무기와 떨어진 도끼 잔여 픽셀 조각만 제거합니다."""
    alpha = image.getchannel("A")
    pixels = alpha.load()
    visited: set[tuple[int, int]] = set()
    remove: list[tuple[int, int]] = []
    for y in range(alpha.height):
        for x in range(alpha.width):
            if pixels[x, y] == 0 or (x, y) in visited:
                continue
            stack = [(x, y)]
            visited.add((x, y))
            component: list[tuple[int, int]] = []
            while stack:
                current_x, current_y = stack.pop()
                component.append((current_x, current_y))
                for next_x, next_y in (
                    (current_x - 1, current_y),
                    (current_x + 1, current_y),
                    (current_x, current_y - 1),
                    (current_x, current_y + 1),
                ):
                    if (
                        0 <= next_x < alpha.width
                        and 0 <= next_y < alpha.height
                        and pixels[next_x, next_y] > 0
                        and (next_x, next_y) not in visited
                    ):
                        visited.add((next_x, next_y))
                        stack.append((next_x, next_y))
            if len(component) <= minimum_area:
                remove.extend(component)
    rgba = image.load()
    for x, y in remove:
        rgba[x, y] = (0, 0, 0, 0)


def build_non_axe_frame(weapon: str, skill: str, frame_index: int) -> Image.Image:
    frame = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    body = cleaned_red_king_body(frame_index)
    x = 26 + X_MOTION[skill][frame_index]
    y = GROUND_Y - body.height + Y_MOTION.get(skill, [0] * FRAME_COUNT)[frame_index]
    copy_visible_pixels(frame, body, (x, y))

    # 지워진 오른팔 부위를 같은 팔레트의 짧은 갑옷 팔로 연결합니다.
    hand = (x + 43, y + 42)
    shoulder = (x + 34, y + 34)
    drawlib.thick_line(
        ImageDraw.Draw(frame), [shoulder, hand], PALETTE["armor"], PALETTE["outline"], 5
    )
    draw_replacement_weapon(frame, weapon, skill, frame_index, hand)
    remove_small_detached_pixels(frame)
    return frame


def build_axe_frame(skill: str, frame_index: int) -> Image.Image:
    frame = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    source = Image.open(SOURCE_ROOT / AXE_SOURCES[skill][frame_index]).convert("RGBA")
    x = 20 + AXE_X[skill][frame_index]
    y = GROUND_Y - source.height
    copy_visible_pixels(frame, source, (x, y))
    return frame


def save_gif(frames: list[Image.Image], path: Path) -> None:
    enlarged = [
        frame.resize(
            (CANVAS[0] * GIF_SCALE, CANVAS[1] * GIF_SCALE), Image.Resampling.NEAREST
        )
        for frame in frames
    ]
    enlarged[0].save(
        path,
        save_all=True,
        append_images=enlarged[1:],
        duration=FRAME_MS,
        loop=0,
        disposal=2,
        transparency=0,
        optimize=False,
    )


def build_skill(weapon: str, skill: str) -> dict[str, object]:
    frame_dir = FRAME_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
    frame_dir.mkdir(parents=True, exist_ok=True)
    GIF_ROOT.mkdir(parents=True, exist_ok=True)
    frames: list[Image.Image] = []
    hashes: list[str] = []

    for index in range(FRAME_COUNT):
        frame = (
            build_axe_frame(skill, index)
            if weapon == "Axe"
            else build_non_axe_frame(weapon, skill, index)
        )
        frame_path = frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png"
        frame.save(frame_path)
        frames.append(frame)
        hashes.append(hashlib.sha256(frame.tobytes()).hexdigest())

    gif_path = GIF_ROOT / f"hys_Final_Enemy_{weapon}_{skill}.gif"
    save_gif(frames, gif_path)
    return {
        "weapon": weapon,
        "skill": skill,
        "frames": FRAME_COUNT,
        "unique_frames": len(set(hashes)),
        "gif": str(gif_path),
    }


def build_animated_preview(results: list[dict[str, object]]) -> Path:
    columns, rows, scale = 3, 5, 2
    cell_width = CANVAS[0] * scale
    cell_height = CANVAS[1] * scale + 22
    preview_frames: list[Image.Image] = []
    for frame_index in range(FRAME_COUNT):
        preview = Image.new(
            "RGB", (cell_width * columns, cell_height * rows), "#1c2029"
        )
        draw = ImageDraw.Draw(preview)
        for index, result in enumerate(results):
            weapon = str(result["weapon"])
            skill = str(result["skill"])
            path = (
                FRAME_ROOT
                / weapon
                / "Sprites/hys_FinalSkills"
                / skill
                / f"hys_Enemy_{weapon}_{skill}_{frame_index:02d}.png"
            )
            sprite = Image.open(path).convert("RGBA").resize(
                (CANVAS[0] * scale, CANVAS[1] * scale), Image.Resampling.NEAREST
            )
            x = (index % columns) * cell_width
            y = (index // columns) * cell_height
            draw.text((x + 5, y + 4), f"{weapon} / {skill}", fill="white")
            background = Image.new("RGBA", sprite.size, "#1c2029")
            background.alpha_composite(sprite)
            preview.paste(background.convert("RGB"), (x, y + 22))
        preview_frames.append(preview)

    path = OUTPUT_ROOT / "hys_OrangeRedKing_ActualBody_15_AnimatedPreview.gif"
    preview_frames[0].save(
        path,
        save_all=True,
        append_images=preview_frames[1:],
        duration=FRAME_MS,
        loop=0,
        optimize=False,
    )
    return path


def build_contact_sheet(results: list[dict[str, object]]) -> Path:
    scale = 2
    label_width = 230
    reference_width = 150
    row_height = CANVAS[1] * scale
    width = label_width + reference_width + CANVAS[0] * scale * FRAME_COUNT
    sheet = Image.new("RGB", (width, row_height * len(results)), "#1c2029")
    draw = ImageDraw.Draw(sheet)
    reference = Image.open(SOURCE_ROOT / "hys_RedKing_Idle_02.png").convert("RGBA")
    reference = reference.resize((128, 128), Image.Resampling.NEAREST)

    for row, result in enumerate(results):
        weapon = str(result["weapon"])
        skill = str(result["skill"])
        y = row * row_height
        draw.text((8, y + 8), f"{weapon} / {skill}", fill="white")
        draw.text((label_width + 5, y + 8), "ACTUAL RED KING", fill="#ff7730")
        background = Image.new("RGBA", reference.size, "#1c2029")
        background.alpha_composite(reference)
        sheet.paste(background.convert("RGB"), (label_width + 5, y + 34))
        for index in range(FRAME_COUNT):
            path = (
                FRAME_ROOT
                / weapon
                / "Sprites/hys_FinalSkills"
                / skill
                / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png"
            )
            frame = Image.open(path).convert("RGBA").resize(
                (CANVAS[0] * scale, CANVAS[1] * scale), Image.Resampling.NEAREST
            )
            frame_bg = Image.new("RGBA", frame.size, "#1c2029")
            frame_bg.alpha_composite(frame)
            x = label_width + reference_width + index * CANVAS[0] * scale
            sheet.paste(frame_bg.convert("RGB"), (x, y))

    path = OUTPUT_ROOT / "hys_OrangeRedKing_ActualBody_15_AllFrames.png"
    sheet.save(path)
    return path


def publish_replacement(
    results: list[dict[str, object]], preview_path: Path, contact_path: Path, report_path: Path
) -> None:
    """검증본과 동일한 135프레임·15 GIF·통합 GIF를 실제 hys 대상에 반영합니다."""
    PUBLISH_GIF_ROOT.mkdir(parents=True, exist_ok=True)
    PUBLISH_REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    for result in results:
        weapon = str(result["weapon"])
        skill = str(result["skill"])
        source_dir = FRAME_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
        target_dir = PUBLISH_FRAME_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
        target_dir.mkdir(parents=True, exist_ok=True)
        for source in sorted(source_dir.glob("hys_Enemy_*.png")):
            shutil.copy2(source, target_dir / source.name)
        source_gif = GIF_ROOT / f"hys_Final_Enemy_{weapon}_{skill}.gif"
        shutil.copy2(source_gif, PUBLISH_GIF_ROOT / source_gif.name)

    shutil.copy2(
        preview_path,
        PUBLISH_REPORT_ROOT / "hys_NativeStyle_15_AnimatedPreview.gif",
    )
    shutil.copy2(
        contact_path,
        PUBLISH_REPORT_ROOT / "hys_NativeStyle_15_AllFrames.png",
    )
    shutil.copy2(
        report_path,
        PUBLISH_REPORT_ROOT / "hys_OrangeRedKing_ActualBody_15_Report.json",
    )


def main() -> None:
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    results: list[dict[str, object]] = []
    for weapon, skills in SKILLS.items():
        for skill in skills:
            results.append(build_skill(weapon, skill))
    preview_path = build_animated_preview(results)
    contact_path = build_contact_sheet(results)
    report_path = OUTPUT_ROOT / "hys_OrangeRedKing_ActualBody_15_Report.json"
    report_path.write_text(
        json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    publish_replacement(results, preview_path, contact_path, report_path)
    print(f"PREVIEW={preview_path}")
    print(f"CONTACT={contact_path}")
    print(f"REPORT={report_path}")


if __name__ == "__main__":
    main()
