from __future__ import annotations

import hashlib
import json
import math
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw


# 원본 몬스터 한 종만 참조해 만든 3x3 포즈 시트를 게임용 9프레임으로 정규화합니다.
ROOT = Path(__file__).resolve().parents[1]
SHEET_ROOT = ROOT / "Tools/hys_imagegen_sheets"
SPRITE_ROOT = ROOT / "Assets/05Anims/hys_Enemy_Anims"
GIF_ROOT = ROOT.parent / "hys_FinalVersion_15_GIFs"
REPORT_ROOT = ROOT / "hys_Reports/MonsterSkillFinal"

CANVAS = (160, 112)
GROUND_Y = 100
FRAME_COUNT = 9
FRAME_MS = 90
GIF_SCALE = 4

SKILLS = {
    "Sword": ["sword_diagonal_slash", "sword_up_diagonal_slash", "sword_wave"],
    "Axe": ["axe_swing", "axe_body_charge", "axe_spin_charge"],
    "Bow": ["bow_air_arrow_shot", "bow_rapid_shot", "bow_low_charge_shot"],
    "Lance": ["lance_charge_thrust", "lance_thrust_combo", "lance_finisher_thrust"],
    "Shield": ["shield_charge", "shield_slam", "shield_diagonal_knockback"],
}

REFERENCE_PATHS = {
    "Sword": "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen/R_Queen_idle00.png",
    "Axe": "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions/hys_RedKing_Idle_02.png",
    "Bow": "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions/hys_RedBishop_Bow_Idle_00.png",
    "Lance": "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions/hys_RedBishop_Idle_00.png",
    "Shield": "Assets/05Anims/hys_Player_Anims/Shield/Sprites/hys_RedRook_Idle_00.png",
}

# 원본 64px 체형을 기준으로 직업별 몸 중심 높이와 배치 위치를 고정합니다.
# Lance는 20% 확대 후 뒤로 뻗은 다리와 창이 왼쪽 경계에 닿지 않도록 중심을 오른쪽으로 보정합니다.
TARGET_CORE_X = {"Sword": 50, "Axe": 54, "Bow": 50, "Lance": 58, "Shield": 54}

# 긴 창의 면적 때문에 Bishop 본체가 작아지는 현상을 실제 Idle 높이에 맞춰 보정합니다.
SIZE_MULTIPLIER = {"Sword": 1.0, "Axe": 1.0, "Bow": 1.0, "Lance": 1.20, "Shield": 1.0}

X_MOTION = {
    "sword_diagonal_slash": [0, 0, 1, 2, 4, 4, 3, 1, 0],
    "sword_up_diagonal_slash": [0, 0, 1, 2, 3, 4, 3, 1, 0],
    "sword_wave": [0, 0, 1, 2, 3, 3, 2, 1, 0],
    "axe_swing": [0, 0, 1, 2, 3, 3, 2, 1, 0],
    "axe_body_charge": [0, 2, 6, 12, 20, 28, 34, 16, 2],
    "axe_spin_charge": [0, 2, 5, 9, 14, 19, 16, 8, 1],
    "bow_air_arrow_shot": [0, 2, 4, 6, 8, 10, 9, 5, 0],
    "bow_rapid_shot": [0, 0, 1, 1, 2, 2, 2, 1, 0],
    "bow_low_charge_shot": [0, 0, 0, 1, 1, 2, 2, 1, 0],
    "lance_charge_thrust": [0, 2, 6, 12, 20, 28, 34, 16, 2],
    "lance_thrust_combo": [0, 3, 1, 5, 2, 7, 3, 1, 0],
    "lance_finisher_thrust": [0, 0, 1, 3, 8, 12, 8, 3, 0],
    "shield_charge": [0, 2, 6, 12, 20, 28, 34, 16, 2],
    "shield_slam": [0, 0, 1, 1, 2, 3, 2, 1, 0],
    "shield_diagonal_knockback": [0, 0, 1, 2, 4, 5, 3, 1, 0],
}

Y_MOTION = {
    "bow_air_arrow_shot": [0, -5, -13, -20, -24, -21, -14, -6, 0],
    # 확대된 돌진 3프레임의 뒤쪽 발이 바닥 아래로 내려가지 않도록 3px 올립니다.
    "lance_charge_thrust": [0, 0, -3, 0, 0, 0, 0, 0, 0],
}


def is_magenta(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    return alpha > 0 and red > 180 and blue > 175 and green < 105 and abs(red - blue) < 85


def remove_background(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = list(rgba.get_flattened_data())
    rgba.putdata([(r, g, b, 0) if is_magenta((r, g, b, a)) else (r, g, b, a) for r, g, b, a in pixels])
    return rgba


def source_palette(weapon: str, count: int = 64) -> Image.Image:
    reference = Image.open(ROOT / REFERENCE_PATHS[weapon]).convert("RGBA")
    colors = Counter((r, g, b) for r, g, b, a in reference.get_flattened_data() if a > 16)
    selected = [color for color, _ in colors.most_common(count)]
    if not selected:
        selected = [(0, 0, 0)]
    selected.extend([selected[-1]] * (256 - len(selected)))
    palette_image = Image.new("P", (1, 1))
    flat = [channel for color in selected[:256] for channel in color]
    palette_image.putpalette(flat)
    return palette_image


def reference_opaque_area(weapon: str) -> int:
    """실제 인게임 원본의 캐릭터 면적을 반환합니다."""
    reference = Image.open(ROOT / REFERENCE_PATHS[weapon]).convert("RGBA")
    return sum(1 for alpha in reference.getchannel("A").get_flattened_data() if alpha > 16)


def largest_component_area(alpha: Image.Image) -> int:
    """화살 같은 분리 이펙트를 제외하고 본체와 연결된 가장 큰 영역을 계산합니다."""
    mask = alpha.point(lambda value: 255 if value > 16 else 0)
    pixels = mask.load()
    visited: set[tuple[int, int]] = set()
    largest = 0
    for y in range(mask.height):
        for x in range(mask.width):
            if pixels[x, y] == 0 or (x, y) in visited:
                continue
            stack = [(x, y)]
            visited.add((x, y))
            area = 0
            while stack:
                current_x, current_y = stack.pop()
                area += 1
                for next_x, next_y in (
                    (current_x - 1, current_y),
                    (current_x + 1, current_y),
                    (current_x, current_y - 1),
                    (current_x, current_y + 1),
                ):
                    if (
                        0 <= next_x < mask.width
                        and 0 <= next_y < mask.height
                        and pixels[next_x, next_y] > 0
                        and (next_x, next_y) not in visited
                    ):
                        visited.add((next_x, next_y))
                        stack.append((next_x, next_y))
            largest = max(largest, area)
    return max(1, largest)


def quantize_to_original(image: Image.Image, palette: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    quantized = image.convert("RGB").quantize(palette=palette, dither=Image.Dither.NONE).convert("RGBA")
    quantized.putalpha(alpha.point(lambda value: 255 if value >= 96 else 0))
    return quantized


def core_bounds(alpha: Image.Image) -> tuple[int, int, int, int]:
    box = alpha.getbbox()
    if box is None:
        return (0, 0, alpha.width, alpha.height)
    pixels = alpha.load()
    row_threshold = max(12, alpha.width // 28)
    column_threshold = max(12, alpha.height // 28)
    rows = [y for y in range(alpha.height) if sum(1 for x in range(alpha.width) if pixels[x, y] > 0) >= row_threshold]
    columns = [x for x in range(alpha.width) if sum(1 for y in range(alpha.height) if pixels[x, y] > 0) >= column_threshold]
    if not rows or not columns:
        return box
    return (min(columns), min(rows), max(columns) + 1, max(rows) + 1)


def extract_cells(sheet: Image.Image) -> list[Image.Image]:
    cell_width = sheet.width // 3
    cell_height = sheet.height // 3
    cells: list[Image.Image] = []
    for row in range(3):
        for column in range(3):
            left = column * cell_width
            top = row * cell_height
            right = sheet.width if column == 2 else (column + 1) * cell_width
            bottom = sheet.height if row == 2 else (row + 1) * cell_height
            cells.append(remove_background(sheet.crop((left, top, right, bottom))))
    return cells


def normalize_frame(
    cell: Image.Image,
    weapon: str,
    skill: str,
    frame_index: int,
    palette: Image.Image,
    target_area: int,
) -> Image.Image:
    alpha = cell.getchannel("A")
    foreground = alpha.getbbox()
    core = core_bounds(alpha)
    if foreground is None:
        raise ValueError(f"비어 있는 셀: {weapon}/{skill}/{frame_index}")

    scale = math.sqrt(target_area / largest_component_area(alpha)) * SIZE_MULTIPLIER[weapon]
    cropped = cell.crop(foreground)
    resized = cropped.resize(
        (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale))),
        Image.Resampling.NEAREST,
    )
    resized = quantize_to_original(resized, palette)

    core_center_x = ((core[0] + core[2]) / 2.0 - foreground[0]) * scale
    core_bottom_y = (core[3] - foreground[1]) * scale
    target_x = TARGET_CORE_X[weapon] + X_MOTION[skill][frame_index]
    target_y = GROUND_Y + Y_MOTION.get(skill, [0] * FRAME_COUNT)[frame_index]
    paste_x = round(target_x - core_center_x)
    paste_y = round(target_y - core_bottom_y)

    output = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    output.alpha_composite(resized, (paste_x, paste_y))
    return output


def build_skill(weapon: str, skill: str) -> dict[str, object]:
    sheet_path = SHEET_ROOT / weapon / f"hys_{weapon}_{skill}_sheet.png"
    sheet = Image.open(sheet_path)
    cells = extract_cells(sheet)
    palette = source_palette(weapon)
    target_area = reference_opaque_area(weapon)
    frames = [normalize_frame(cell, weapon, skill, index, palette, target_area) for index, cell in enumerate(cells)]

    if skill == "sword_wave":
        # 생성 시트의 파동 단독 프레임에는 Queen 회복 자세를 유지하고 파동만 오른쪽으로 이동합니다.
        wave_only = frames[6]
        combined = frames[7].copy()
        combined.alpha_composite(wave_only, (75, 0))
        frames[6] = combined

    frame_dir = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
    frame_dir.mkdir(parents=True, exist_ok=True)
    hashes: list[str] = []
    for index, frame in enumerate(frames):
        frame_path = frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png"
        frame.save(frame_path)
        hashes.append(hashlib.sha256(frame.tobytes()).hexdigest())

    GIF_ROOT.mkdir(parents=True, exist_ok=True)
    gif_path = GIF_ROOT / f"hys_Final_Enemy_{weapon}_{skill}.gif"
    gif_frames = [frame.resize((CANVAS[0] * GIF_SCALE, CANVAS[1] * GIF_SCALE), Image.Resampling.NEAREST) for frame in frames]
    gif_frames[0].save(
        gif_path,
        save_all=True,
        append_images=gif_frames[1:],
        duration=FRAME_MS,
        loop=0,
        disposal=2,
        transparency=0,
        optimize=False,
    )
    return {
        "weapon": weapon,
        "skill": skill,
        "sheet": str(sheet_path),
        "gif": str(gif_path),
        "frames": len(frames),
        "unique_frames": len(set(hashes)),
    }


def build_contact_sheet(results: list[dict[str, object]]) -> None:
    scale = 2
    label_width = 210
    reference_width = 140
    row_height = CANVAS[1] * scale
    width = label_width + reference_width + CANVAS[0] * scale * FRAME_COUNT
    sheet = Image.new("RGB", (width, row_height * len(results)), "#1c2029")
    draw = ImageDraw.Draw(sheet)
    for row, result in enumerate(results):
        weapon = str(result["weapon"])
        skill = str(result["skill"])
        y = row * row_height
        draw.text((8, y + 8), f"{weapon} / {skill}", fill="white")
        draw.text((label_width + 4, y + 8), "ORIGINAL", fill="#f0c75e")
        original = Image.open(ROOT / REFERENCE_PATHS[weapon]).convert("RGBA").resize((128, 128), Image.Resampling.NEAREST)
        original_bg = Image.new("RGBA", original.size, "#1c2029")
        original_bg.alpha_composite(original)
        sheet.paste(original_bg.convert("RGB"), (label_width + 4, y + 32))
        frame_dir = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
        for index in range(FRAME_COUNT):
            frame = Image.open(frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png").convert("RGBA")
            frame = frame.resize((CANVAS[0] * scale, CANVAS[1] * scale), Image.Resampling.NEAREST)
            frame_bg = Image.new("RGBA", frame.size, "#1c2029")
            frame_bg.alpha_composite(frame)
            x = label_width + reference_width + index * CANVAS[0] * scale
            sheet.paste(frame_bg.convert("RGB"), (x, y))
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    sheet.save(REPORT_ROOT / "hys_Final_15_OriginalComparison_AllFrames.png")


def build_animated_preview(results: list[dict[str, object]]) -> None:
    columns, rows, scale = 3, 5, 2
    cell_width = CANVAS[0] * scale
    cell_height = CANVAS[1] * scale + 22
    preview_frames: list[Image.Image] = []
    for frame_index in range(FRAME_COUNT):
        preview = Image.new("RGB", (cell_width * columns, cell_height * rows), "#1c2029")
        draw = ImageDraw.Draw(preview)
        for index, result in enumerate(results):
            weapon = str(result["weapon"])
            skill = str(result["skill"])
            source = SPRITE_ROOT / weapon / "Sprites/hys_FinalSkills" / skill / f"hys_Enemy_{weapon}_{skill}_{frame_index:02d}.png"
            sprite = Image.open(source).convert("RGBA").resize((CANVAS[0] * scale, CANVAS[1] * scale), Image.Resampling.NEAREST)
            x = (index % columns) * cell_width
            y = (index // columns) * cell_height
            draw.text((x + 5, y + 4), f"{weapon} / {skill}", fill="white")
            layer = Image.new("RGBA", sprite.size, "#1c2029")
            layer.alpha_composite(sprite)
            preview.paste(layer.convert("RGB"), (x, y + 22))
        preview_frames.append(preview)
    preview_frames[0].save(
        REPORT_ROOT / "hys_Final_15_AnimatedPreview.gif",
        save_all=True,
        append_images=preview_frames[1:],
        duration=FRAME_MS,
        loop=0,
        optimize=False,
    )


def main() -> None:
    results: list[dict[str, object]] = []
    for weapon, skills in SKILLS.items():
        for skill in skills:
            results.append(build_skill(weapon, skill))
    build_contact_sheet(results)
    build_animated_preview(results)
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    (REPORT_ROOT / "hys_ImagegenFinal_ExportReport.json").write_text(
        json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"원본 단독 참조 Final Version 15 GIFs 출력 완료: {GIF_ROOT}")


if __name__ == "__main__":
    main()
