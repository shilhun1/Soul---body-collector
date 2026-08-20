from __future__ import annotations

import argparse
import hashlib
import json
import math
import shutil
from collections import deque
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


# 실제 플레이어 스프라이트를 캐릭터 베이스로 사용하고 스킬 데이터에 필요한 신규 자세만 보강합니다.
ROOT = Path(__file__).resolve().parents[1]
ASSET_ROOT = ROOT / "Assets/05Anims/hys_Enemy_Anims"
GIF_ROOT = ROOT.parent / "hys_FinalVersion_15_GIFs"
REPORT_ROOT = ROOT / "hys_Reports/MonsterSkillPlayerBased"
CANVAS = (160, 112)
GROUND_Y = 98
FRAME_COUNT = 9
PREVIEW_SCALE = 2
GIF_SCALE = 4


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
    "lance_charge_thrust": 300,
    "lance_thrust_combo": 650,
    "lance_finisher_thrust": 350,
    "shield_charge": 520,
    "shield_slam": 660,
    "shield_diagonal_knockback": 540,
}


# 검기 발사 자세가 런타임 투사체 생성 시점인 0.35초에 정확히 시작하도록 맞춥니다.
CUSTOM_GIF_FRAME_DURATIONS_MS = {
    "sword_wave": [60, 60, 60, 60, 60, 50, 60, 60, 60],
    # 돌진 접촉, 4연타, 마무리 타격의 실제 데이터 시점에 공격 자세를 맞춥니다.
    "lance_charge_thrust": [30, 30, 20, 40, 40, 40, 40, 30, 30],
    "lance_thrust_combo": [70, 60, 60, 60, 60, 60, 60, 70, 150],
    "lance_finisher_thrust": [30, 30, 20, 50, 50, 50, 40, 40, 40],
}


SOURCE_ROOTS = {
    "Sword": ROOT / "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen",
    "Axe": ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions",
    "Bow": ROOT / "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions",
    "Lance": ROOT / "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions",
    "Shield": ROOT / "Assets/05Anims/hys_Player_Anims/Shield/Sprites",
}

SWORD_SKILL_SHEET_ROOT = ROOT / "Tools/hys_imagegen_sheets/Sword"
SWORD_IDLE_WEAPON_ANGLES = {
    # 각 신규 자세에서 손에서 칼끝으로 향하는 화면 좌표 기준 각도입니다.
    "sword_diagonal_slash": [45, -110, -160, -165, 45, 25, 45, 45, 45],
    "sword_up_diagonal_slash": [25, 160, -160, -55, -55, -90, 180, 45, 45],
    # 검기는 별도 이펙트가 담당하므로 뒤로 당긴 칼이 전방 수평으로 빠져나가는 모션만 만듭니다.
    "sword_wave": [45, -145, -120, -95, -55, -15, 0, 25, 45],
}
# 검기 모션은 낮게 힘을 모은 뒤 상체를 열고 수평으로 베도록 몸 자세 순서를 고정합니다.
SWORD_BODY_FRAME_ORDER = {
    "sword_diagonal_slash": list(range(FRAME_COUNT)),
    "sword_up_diagonal_slash": list(range(FRAME_COUNT)),
    "sword_wave": [0, 2, 3, 4, 5, 6, 6, 7, 8],
}
SWORD_BODY_GUIDE = (
    ROOT
    / "Assets/05Anims/hys_Player_Anims/Sword/Reference/hys_Sword_24BodyPose_GeneratedGuide.png"
)


SOURCE_PREFIXES = {
    "Sword": "R_Queen_",
    "Axe": "hys_RedKing_",
    "Bow": "hys_RedBishop_Bow_",
    "Lance": "hys_RedBishop_",
    "Shield": "hys_RedRook_",
}


# 각 원본 시트에서 발 위치가 아니라 몸 중심으로 사용되는 X 좌표입니다.
SOURCE_ANCHOR_X = {
    "Sword": 25,
    "Axe": 31,
    "Bow": 38,
    "Lance": 34,
    "Shield": 20,
}


@dataclass(frozen=True)
class SourcePose:
    motion: str
    index: int
    x: int = 0
    y: int = 0
    remove_lines: bool = False
    remove_spin_effect: bool = False
    remove_sword_effect: bool = False
    sword_angle: int = 0
    shield_angle: int | None = None
    shield_x: int = 0
    shield_y: int = 0


def poses(motion: str, indices: list[int], **kwargs: object) -> list[SourcePose]:
    return [SourcePose(motion, index, **kwargs) for index in indices]


def sword_poses(indices: list[int], angles: list[int]) -> list[SourcePose]:
    """원본 Red Queen 공격 자세를 그대로 사용해 몸과 칼 픽셀이 잘리지 않게 합니다."""
    # 각도 목록 길이도 함께 검사해 기존 스킬 정의의 9프레임 구성을 보호합니다.
    if len(indices) != len(angles):
        raise ValueError("Sword 자세 인덱스와 각도 목록 길이가 다릅니다.")
    return [SourcePose("Attack", index) for index in indices]


SKILLS: dict[str, dict[str, list[SourcePose]]] = {
    "Sword": {
        # 실제 R_Queen의 이펙트 없는 공격 프레임을 통째로 사용해 중간 자세가 잘리지 않게 합니다.
        "sword_diagonal_slash": sword_poses(
            [0, 1, 1, 5, 5, 6, 6, 1, 0],
            [0, -12, -4, -8, 0, 8, 4, 0, 0],
        ),
        # 올려베기는 같은 검 규격을 유지한 채 낮은 자세에서 높은 자세로 역재생합니다.
        "sword_up_diagonal_slash": sword_poses(
            [0, 5, 5, 1, 1, 6, 6, 5, 0],
            [0, 12, 4, 8, 0, -8, -4, 0, 0],
        ),
        # 검기는 런타임 투사체가 담당하고 캐릭터는 힘을 모은 뒤 크게 방출하는 자세만 사용합니다.
        # 검기는 0.35초까지 준비 자세를 유지하고 7번째 프레임에서 투사체를 방출합니다.
        "sword_wave": sword_poses(
            [0, 1, 1, 1, 5, 5, 6, 6, 0],
            [0, -4, 0, 4, 0, -4, 0, 4, 0],
        ),
    },
    "Axe": {
        "axe_swing": [
            SourcePose("Attack1", index, remove_spin_effect=index == 4)
            for index in [0, 1, 2, 3, 4, 3, 2, 1, 0]
        ],
        # 실제 이동은 DashAction이 담당하므로 모든 프레임은 동일 피벗에 고정합니다.
        "axe_body_charge": poses("Attack1", [0, 1, 2, 3, 2, 1, 0, 1, 0]),
        # 4번 프레임에서는 원호를 제거하고 중앙의 웅크린 회전 본체만 사용합니다.
        "axe_spin_charge": [
            SourcePose("Attack2", index, remove_spin_effect=index == 4)
            for index in [2, 3, 4, 0, 1, 2, 3, 4, 0]
        ],
    },
    "Bow": {
        "bow_air_arrow_shot": [
            SourcePose("JumpStart", 0, y=0),
            SourcePose("JumpStart", 1, y=-5),
            SourcePose("JumpStart", 2, y=-11),
            SourcePose("JumpApex", 0, y=-17),
            SourcePose("Attack1", 2, y=-18),
            SourcePose("Attack1", 3, y=-16),
            SourcePose("Attack1", 4, y=-11),
            SourcePose("JumpFall", 1, y=-5),
            SourcePose("JumpFall", 2, y=0),
        ],
        # 0.11초 간격의 세 발을 당김/발사 자세 세 쌍으로 표시합니다.
        "bow_rapid_shot": poses("Attack1", [0, 2, 3, 2, 3, 2, 3, 4, 5]),
        # 0.75초 충전 구간은 동일한 최대 장력 자세를 의도적으로 유지합니다.
        "bow_low_charge_shot": poses("Attack2", [0, 1, 2, 2, 2, 2, 4, 5, 0]),
    },
    "Lance": {
        # 돌진 이동은 런타임이 담당하고 캐릭터는 속도선을 지운 실제 Dash 자세로 창을 뻗어 버팁니다.
        "lance_charge_thrust": [
            SourcePose("Attack1", 0),
            SourcePose("Dash", 0, remove_lines=True),
            SourcePose("Dash", 1, remove_lines=True),
            SourcePose("Dash", 2, remove_lines=True),
            SourcePose("Dash", 3, remove_lines=True),
            SourcePose("Dash", 3, x=1, remove_lines=True),
            SourcePose("Dash", 4, remove_lines=True),
            SourcePose("Dash", 5, remove_lines=True),
            SourcePose("Idle", 0),
        ],
        # 홀수 프레임 1/3/5/7의 네 타격 사이에 실제 회수 자세를 넣어 연속 찌르기를 분리합니다.
        "lance_thrust_combo": [
            SourcePose("Attack1", 0), SourcePose("Attack1", 3),
            SourcePose("Attack1", 5), SourcePose("Attack2", 3),
            SourcePose("Attack2", 5), SourcePose("Attack1", 3),
            SourcePose("Attack1", 5), SourcePose("Attack2", 3),
            SourcePose("Idle", 0),
        ],
        # 마무리는 Attack2 중간 자세를 빠짐없이 사용하고 타격 순간의 깊은 발 디딤만 더합니다.
        "lance_finisher_thrust": [
            SourcePose("Attack2", 0),
            SourcePose("Attack2", 1),
            SourcePose("Attack2", 2, x=1),
            SourcePose("Attack2", 3, x=3),
            SourcePose("Attack2", 4, x=5, y=1),
            SourcePose("Attack2", 5, x=6, y=1),
            SourcePose("Attack2", 4, x=4),
            SourcePose("Attack2", 1, x=1),
            SourcePose("Idle", 0),
        ],
    },
    "Shield": {
        "shield_charge": poses("Dash", [0, 1, 2, 3, 4, 5], remove_lines=True)
        + poses("Attack", [3, 4, 5]),
        # 원본 몸체와 방패를 분리한 뒤 방패와 팔의 신규 픽셀을 머리 위로 이동해 내려찍기를 만듭니다.
        "shield_slam": [
            SourcePose("Attack", 0, shield_angle=0),
            SourcePose("Attack", 0, y=-1, shield_angle=15, shield_x=-1, shield_y=-4),
            SourcePose("Attack", 0, y=-2, shield_angle=40, shield_x=-4, shield_y=-11),
            SourcePose("Attack", 0, y=-3, shield_angle=70, shield_x=-7, shield_y=-17),
            SourcePose("Attack", 0, y=1, shield_angle=35, shield_x=-2, shield_y=-9),
            SourcePose("Attack", 0, y=4, shield_angle=-8, shield_x=4, shield_y=2),
            SourcePose("Attack", 0, y=3, shield_angle=-18, shield_x=5, shield_y=4),
            SourcePose("Attack", 0, y=1, shield_angle=-5, shield_x=2, shield_y=1),
            SourcePose("Attack", 0, shield_angle=0),
        ],
        "shield_diagonal_knockback": [
            SourcePose("Attack", 0, shield_angle=0),
            SourcePose("Attack", 1, shield_angle=4, shield_x=-1),
            SourcePose("Attack", 1, shield_angle=15, shield_x=1, shield_y=-2),
            SourcePose("Attack", 2, shield_angle=28, shield_x=3, shield_y=-5),
            SourcePose("Attack", 3, shield_angle=42, shield_x=6, shield_y=-8),
            SourcePose("Attack", 4, shield_angle=55, shield_x=8, shield_y=-10),
            SourcePose("Attack", 5, shield_angle=32, shield_x=5, shield_y=-6),
            SourcePose("Attack", 1, shield_angle=12, shield_x=2, shield_y=-2),
            SourcePose("Attack", 0, shield_angle=0),
        ],
    },
}


def source_path(weapon: str, pose: SourcePose) -> Path:
    if weapon == "Sword":
        return SOURCE_ROOTS[weapon] / f"R_Queen_atk{pose.index:02d}.png"
    return SOURCE_ROOTS[weapon] / f"{SOURCE_PREFIXES[weapon]}{pose.motion}_{pose.index:02d}.png"


def remove_sword_sheet_background(cell: Image.Image) -> Image.Image:
    """시트 가장자리와 연결된 밝은 체크무늬만 지워 흰 갑옷과 검날은 보존합니다."""
    rgba = cell.convert("RGBA")
    pixels = rgba.load()
    background: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def is_background(x: int, y: int) -> bool:
        red, green, blue, _ = pixels[x, y]
        return min(red, green, blue) >= 218 and max(red, green, blue) - min(red, green, blue) <= 18

    for x in range(rgba.width):
        for y in (0, rgba.height - 1):
            if is_background(x, y) and (x, y) not in background:
                background.add((x, y))
                queue.append((x, y))
    for y in range(rgba.height):
        for x in (0, rgba.width - 1):
            if is_background(x, y) and (x, y) not in background:
                background.add((x, y))
                queue.append((x, y))

    while queue:
        point_x, point_y = queue.popleft()
        for next_x, next_y in (
            (point_x - 1, point_y),
            (point_x + 1, point_y),
            (point_x, point_y - 1),
            (point_x, point_y + 1),
        ):
            if not (0 <= next_x < rgba.width and 0 <= next_y < rgba.height):
                continue
            point = (next_x, next_y)
            if point not in background and is_background(next_x, next_y):
                background.add(point)
                queue.append(point)

    for x, y in background:
        red, green, blue, _ = pixels[x, y]
        pixels[x, y] = (red, green, blue, 0)
    return rgba


def sword_source_palette() -> Image.Image:
    """신규 자세의 색을 실제 Red Queen 원본에 존재하는 색으로만 제한합니다."""
    colors: set[tuple[int, int, int]] = set()
    for pattern in ("R_Queen_idle*.png", "R_Queen_atk*.png"):
        for path in SOURCE_ROOTS["Sword"].glob(pattern):
            colors.update(
                (red, green, blue)
                for red, green, blue, alpha in Image.open(path).convert("RGBA").get_flattened_data()
                if alpha > 0
            )
    ordered = sorted(colors)
    ordered.extend([ordered[-1]] * (256 - len(ordered)))
    palette = Image.new("P", (1, 1))
    palette.putpalette([channel for color in ordered[:256] for channel in color])
    return palette


def extract_idle_sword() -> tuple[Image.Image, tuple[int, int]]:
    """Idle 원본의 왼쪽 아래 은색 날·적색 검신·손잡이 픽셀만 분리합니다."""
    idle = Image.open(SOURCE_ROOTS["Sword"] / "R_Queen_idle00.png").convert("RGBA")
    mask = Image.new("L", idle.size, 0)
    draw = ImageDraw.Draw(mask)
    draw.polygon(
        [(4, 49), (4, 44), (18, 30), (22, 30), (24, 33), (22, 36), (10, 49)],
        fill=255,
    )
    isolated = Image.composite(idle, Image.new("RGBA", idle.size), mask)
    # 겹치는 손 픽셀과 합성될 때 중간색이 생기지 않도록 원본 도트를 완전 불투명으로 고정합니다.
    isolated.putalpha(isolated.getchannel("A").point(lambda value: 255 if value > 0 else 0))
    return isolated, (23, 32)


def replace_with_idle_sword(frame: Image.Image, target_angle: float) -> Image.Image:
    """무기가 제거된 자세의 주먹 위치에 Idle 원본 검 픽셀만 붙입니다."""
    result = frame.copy()
    pixels = result.load()
    body_center = (50.0, 80.0)
    target_radians = math.radians(target_angle)
    unit_x = math.cos(target_radians)
    unit_y = math.sin(target_radians)
    hand_candidates: list[tuple[float, int, int]] = []
    for y in range(result.height):
        for x in range(result.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                continue
            distance = math.hypot(x - body_center[0], y - body_center[1])
            is_hand_color = red >= 85 and red > green * 1.28 and red > blue * 1.08
            if is_hand_color and 4 <= distance <= 28 and y <= 94:
                projection = (x - body_center[0]) * unit_x + (y - body_center[1]) * unit_y
                hand_candidates.append((projection, x, y))

    if hand_candidates:
        _, outer_x, outer_y = max(hand_candidates)
        hand_x = round(outer_x - unit_x * 2)
        hand_y = round(outer_y - unit_y * 2)
    else:
        hand_x = round(body_center[0] + unit_x * 7)
        hand_y = round(body_center[1] + unit_y * 7)

    idle_sword, idle_hand = extract_idle_sword()
    weapon_layer = Image.new("RGBA", (100, 100), (0, 0, 0, 0))
    weapon_layer.alpha_composite(idle_sword, (50 - idle_hand[0], 50 - idle_hand[1]))
    idle_angle = math.degrees(math.atan2(49 - idle_hand[1], 5 - idle_hand[0]))
    weapon_layer = weapon_layer.rotate(
        idle_angle - target_angle,
        resample=Image.Resampling.NEAREST,
        center=(50, 50),
    )
    result.alpha_composite(weapon_layer, (hand_x - 50, hand_y - 50))
    return result


def render_sword_skill_frames(skill: str) -> list[Image.Image]:
    """스킬별 3x3 신규 포즈 시트를 원본 픽셀 크기와 팔레트로 정규화합니다."""
    sheet_path = SWORD_SKILL_SHEET_ROOT / f"hys_Sword_{skill}_body_sheet.png"
    sheet = Image.open(sheet_path).convert("RGBA")
    cell_width = sheet.width // 3
    cell_height = sheet.height // 3
    cells: list[Image.Image] = []
    for row in range(3):
        for column in range(3):
            right = sheet.width if column == 2 else (column + 1) * cell_width
            bottom = sheet.height if row == 2 else (row + 1) * cell_height
            cell = sheet.crop((column * cell_width, row * cell_height, right, bottom))
            cleaned = remove_sword_sheet_background(cell)
            box = cleaned.getchannel("A").getbbox()
            if box is None:
                raise ValueError(f"{skill}: {len(cells)}번 신규 자세가 비어 있습니다.")
            cells.append(cleaned.crop(box))

    original_idle = Image.open(SOURCE_ROOTS["Sword"] / "R_Queen_idle00.png").convert("RGBA")
    original_box = original_idle.getchannel("A").getbbox()
    if original_box is None:
        raise ValueError("Sword 원본 Idle이 비어 있습니다.")
    # 마지막 회복 자세의 키를 기준으로 모든 프레임에 같은 배율을 적용해 체형 떨림을 막습니다.
    scale = (original_box[3] - original_box[1]) / float(cells[-1].height)
    palette = sword_source_palette()
    frames: list[Image.Image] = []
    target_angles = SWORD_IDLE_WEAPON_ANGLES[skill]
    ordered_cells = [cells[index] for index in SWORD_BODY_FRAME_ORDER[skill]]
    for frame_index, cell in enumerate(ordered_cells):
        resized = cell.resize(
            (max(1, round(cell.width * scale)), max(1, round(cell.height * scale))),
            Image.Resampling.NEAREST,
        )
        alpha = resized.getchannel("A").point(lambda value: 255 if value >= 96 else 0)
        quantized = resized.convert("RGB").quantize(
            palette=palette,
            dither=Image.Dither.NONE,
        ).convert("RGBA")
        quantized.putalpha(alpha)
        normalized = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        normalized.alpha_composite(
            quantized,
            (round(50 - quantized.width / 2), GROUND_Y - quantized.height),
        )
        quantized = replace_with_idle_sword(normalized, target_angles[frame_index])
        # 몸체에서 멀리 떨어진 1~2픽셀 생성 잡티만 제거하고 내부 강조 픽셀은 유지합니다.
        components = alpha_components(quantized)
        if components:
            body_component = max(components, key=len)
            body_x = [point[0] for point in body_component]
            body_y = [point[1] for point in body_component]
            body_box = (min(body_x), min(body_y), max(body_x), max(body_y))
            pixels = quantized.load()
            for component in components:
                if component is body_component or len(component) > 32:
                    continue
                component_x = [point[0] for point in component]
                component_y = [point[1] for point in component]
                separated = (
                    max(component_x) < body_box[0] - 3
                    or min(component_x) > body_box[2] + 3
                    or max(component_y) < body_box[1] - 3
                    or min(component_y) > body_box[3] + 3
                )
                # 검기 모션에서 회전·축소 후 본체와 떨어진 붉은 누끼 잔여점만 제거합니다.
                tiny_sword_wave_fragment = skill == "sword_wave" and len(component) <= 8
                if separated or tiny_sword_wave_fragment:
                    for point_x, point_y in component:
                        pixels[point_x, point_y] = (0, 0, 0, 0)
        frames.append(quantized)
    # 시작과 회복은 칼까지 포함된 실제 Idle 원본 전체를 그대로 사용합니다.
    idle_source = Image.open(SOURCE_ROOTS["Sword"] / "R_Queen_idle00.png").convert("RGBA")
    idle_frame = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    paste_player_base(idle_frame, "Sword", idle_source, 0)
    frames[0] = idle_frame
    frames[-1] = idle_frame.copy()
    return frames


def load_sword_created_body(pose_index: int) -> Image.Image:
    """24칸 동작 시트에서 한 자세를 꺼내 원본 Red Queen 팔레트의 50x50 도트로 정리합니다."""
    guide = Image.open(SWORD_BODY_GUIDE).convert("RGB")
    column = pose_index % 8
    row = pose_index // 8
    left = round(column * guide.width / 8)
    right = round((column + 1) * guide.width / 8)
    top = round(row * guide.height / 3)
    bottom = round((row + 1) * guide.height / 3)
    cell = guide.crop((left, top, right, bottom)).convert("RGBA")

    # 가장자리에서 연결되는 밝은 체크무늬만 배경으로 판정해 흰 갑옷 내부는 보존합니다.
    pixels = cell.load()
    background: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def is_background(x: int, y: int) -> bool:
        red, green, blue, _ = pixels[x, y]
        return max(red, green, blue) - min(red, green, blue) <= 10 and min(red, green, blue) >= 218

    for x in range(cell.width):
        for y in (0, cell.height - 1):
            if is_background(x, y) and (x, y) not in background:
                background.add((x, y))
                queue.append((x, y))
    for y in range(cell.height):
        for x in (0, cell.width - 1):
            if is_background(x, y) and (x, y) not in background:
                background.add((x, y))
                queue.append((x, y))
    while queue:
        point_x, point_y = queue.popleft()
        for neighbor_x, neighbor_y in (
            (point_x - 1, point_y),
            (point_x + 1, point_y),
            (point_x, point_y - 1),
            (point_x, point_y + 1),
        ):
            if not (0 <= neighbor_x < cell.width and 0 <= neighbor_y < cell.height):
                continue
            point = (neighbor_x, neighbor_y)
            if point not in background and is_background(neighbor_x, neighbor_y):
                background.add(point)
                queue.append(point)
    for x, y in background:
        red, green, blue, _ = pixels[x, y]
        pixels[x, y] = (red, green, blue, 0)

    box = cell.getchannel("A").getbbox()
    if box is None:
        raise ValueError(f"Sword 생성 몸체가 비어 있습니다: {pose_index}")
    cell = cell.crop(box)
    scale = min(46 / cell.width, 42 / cell.height)
    cell = cell.resize(
        (max(1, round(cell.width * scale)), max(1, round(cell.height * scale))),
        Image.Resampling.NEAREST,
    )

    # 생성 시트의 중간색을 실제 Red Queen PNG에 존재하는 가장 가까운 색으로 치환합니다.
    palette: set[tuple[int, int, int]] = set()
    for pattern in ("R_Queen_idle*.png", "R_Queen_atk*.png"):
        for path in SOURCE_ROOTS["Sword"].glob(pattern):
            palette.update(
                (red, green, blue)
                for red, green, blue, alpha in Image.open(path).convert("RGBA").get_flattened_data()
                if alpha > 0
            )
    palette_colors = tuple(palette)
    cell_pixels = cell.load()
    for y in range(cell.height):
        for x in range(cell.width):
            red, green, blue, alpha = cell_pixels[x, y]
            if alpha == 0:
                continue
            nearest = min(
                palette_colors,
                key=lambda color: (red - color[0]) ** 2 + (green - color[1]) ** 2 + (blue - color[2]) ** 2,
            )
            cell_pixels[x, y] = (*nearest, 255)

    canvas = Image.new("RGBA", (50, 50), (0, 0, 0, 0))
    canvas.alpha_composite(cell, ((50 - cell.width) // 2, 48 - cell.height))
    # 생성 시트 배경에서 떨어져 나온 1~2픽셀 잡티만 제거하고 몸체 연결 성분은 유지합니다.
    components = alpha_components(canvas)
    if components:
        largest = max(components, key=len)
        canvas_pixels = canvas.load()
        for component in components:
            if component is largest or len(component) >= 3:
                continue
            for x, y in component:
                canvas_pixels[x, y] = (0, 0, 0, 0)
    return canvas


def remove_sword_slash_effect(image: Image.Image, frame_index: int) -> Image.Image:
    """2~4번의 큰 원호는 갑옷 중성색 주변의 실제 몸체 픽셀만 남겨 제거합니다."""
    if frame_index not in (2, 3, 4):
        return image

    result = image.copy()
    pixels = result.load()
    neutral: set[tuple[int, int]] = set()
    for y in range(8, result.height):
        for x in range(8, min(42, result.width)):
            red, green, blue, alpha = pixels[x, y]
            if alpha > 0 and max(red, green, blue) - min(red, green, blue) <= 58:
                neutral.add((x, y))

    components: list[list[tuple[int, int]]] = []
    visited: set[tuple[int, int]] = set()
    for start in neutral:
        if start in visited:
            continue
        queue: deque[tuple[int, int]] = deque([start])
        visited.add(start)
        component: list[tuple[int, int]] = []
        while queue:
            point_x, point_y = queue.popleft()
            component.append((point_x, point_y))
            for neighbor in (
                (point_x - 1, point_y),
                (point_x + 1, point_y),
                (point_x, point_y - 1),
                (point_x, point_y + 1),
            ):
                if neighbor in neutral and neighbor not in visited:
                    visited.add(neighbor)
                    queue.append(neighbor)

        components.append(component)

    if not components:
        return result

    # 몸 중앙과 다리 쪽에 있는 2픽셀 이상의 갑옷 조각을 함께 선택해 자세 전체를 보존합니다.
    armor: list[tuple[int, int]] = []
    for component in components:
        center_x = sum(point[0] for point in component) / len(component)
        if len(component) >= 2 and 10 <= center_x <= 38:
            armor.extend(component)

    keep: set[tuple[int, int]] = set()
    for seed_x, seed_y in armor:
        for offset_y in range(-4, 5):
            for offset_x in range(-4, 5):
                target_x = seed_x + offset_x
                target_y = seed_y + offset_y
                if 0 <= target_x < result.width and 0 <= target_y < result.height:
                    keep.add((target_x, target_y))

    for y in range(result.height):
        for x in range(result.width):
            if (x, y) not in keep:
                pixels[x, y] = (0, 0, 0, 0)
    return result


SWORD_HAND_POSITIONS = {
    # 실제 R_Queen 공격 프레임의 손 위치입니다.
    0: (27, 29),
    1: (20, 28),
    5: (24, 28),
}

SWORD_BASE_ROTATIONS = {
    0: 0,
    1: 0,
    5: 180,
}

SWORD_ERASE_SEGMENTS = {
    # 원본 공격 프레임에 포함된 기존 날만 손 바깥쪽에서 지웁니다.
    0: [((27, 29), (14, 49))],
    1: [((20, 28), (31, 9)), ((20, 28), (0, 46))],
    5: [((24, 28), (32, 11)), ((24, 28), (0, 45))],
}


def redraw_player_sword(image: Image.Image, frame_index: int, angle_offset: int = 0) -> Image.Image:
    """기존 공격 무기를 지우고 Idle 원본의 검 픽셀을 그대로 회전해 같은 손에 붙입니다."""
    if frame_index not in SWORD_HAND_POSITIONS:
        return image

    result = image.copy()
    draw = ImageDraw.Draw(result)
    hand = SWORD_HAND_POSITIONS[frame_index]
    hx, hy = hand

    # 손에서 3픽셀 떨어진 원본 날을 먼저 지우고 몸체는 보존합니다.
    for segment_start, segment_end in SWORD_ERASE_SEGMENTS[frame_index]:
        sx, sy = segment_start
        ex, ey = segment_end
        segment_dx = ex - sx
        segment_dy = ey - sy
        segment_length = max(1, round((segment_dx * segment_dx + segment_dy * segment_dy) ** 0.5))
        clear_start = (
            sx + round(segment_dx / segment_length * 3),
            sy + round(segment_dy / segment_length * 3),
        )
        draw.line((clear_start, segment_end), fill=(0, 0, 0, 0), width=9)

    idle = Image.open(SOURCE_ROOTS["Sword"] / "R_Queen_idle00.png").convert("RGBA")
    mask = Image.new("L", idle.size, 0)
    mask_draw = ImageDraw.Draw(mask)
    # 아래 왼쪽의 넓은 적색/주황 칼날과 손까지 이어지는 검은 손잡이만 분리합니다.
    mask_draw.polygon(
        [(5, 48), (5, 44), (9, 38), (15, 38), (18, 41), (18, 47), (11, 48)],
        fill=255,
    )
    mask_draw.line(((15, 42), (24, 30)), fill=255, width=5)
    isolated = Image.composite(idle, Image.new("RGBA", idle.size), mask)

    # 원본 손 좌표를 100x100 작업 캔버스 중앙에 둔 뒤 NEAREST로만 회전합니다.
    idle_hand = (24, 30)
    weapon_layer = Image.new("RGBA", (100, 100), (0, 0, 0, 0))
    weapon_layer.alpha_composite(isolated, (50 - idle_hand[0], 50 - idle_hand[1]))
    rotation = SWORD_BASE_ROTATIONS[frame_index] + angle_offset
    weapon_layer = weapon_layer.rotate(
        rotation,
        resample=Image.Resampling.NEAREST,
        center=(50, 50),
    )
    result.alpha_composite(weapon_layer, (hx - 50, hy - 50))
    # 반투명 경계 합성으로 생긴 중간색을 다시 실제 원본 팔레트의 불투명 픽셀로 고정합니다.
    palette: set[tuple[int, int, int]] = set()
    for pattern in ("R_Queen_idle*.png", "R_Queen_atk*.png"):
        for path in SOURCE_ROOTS["Sword"].glob(pattern):
            palette.update(
                (red, green, blue)
                for red, green, blue, alpha in Image.open(path).convert("RGBA").get_flattened_data()
                if alpha > 0
            )
    palette_colors = tuple(palette)
    result_pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            red, green, blue, alpha = result_pixels[x, y]
            if alpha == 0:
                continue
            nearest = min(
                palette_colors,
                key=lambda color: (red - color[0]) ** 2 + (green - color[1]) ** 2 + (blue - color[2]) ** 2,
            )
            result_pixels[x, y] = (*nearest, 255)
    return result


def alpha_components(image: Image.Image) -> list[list[tuple[int, int]]]:
    alpha = image.getchannel("A")
    pixels = alpha.load()
    visited: set[tuple[int, int]] = set()
    components: list[list[tuple[int, int]]] = []

    for y in range(image.height):
        for x in range(image.width):
            if pixels[x, y] == 0 or (x, y) in visited:
                continue
            queue: deque[tuple[int, int]] = deque([(x, y)])
            visited.add((x, y))
            component: list[tuple[int, int]] = []
            while queue:
                px, py = queue.popleft()
                component.append((px, py))
                for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    if 0 <= nx < image.width and 0 <= ny < image.height and pixels[nx, ny] > 0 and (nx, ny) not in visited:
                        visited.add((nx, ny))
                        queue.append((nx, ny))
            components.append(component)
    return components


def remove_speed_lines(image: Image.Image, weapon: str, anchor_x: int) -> Image.Image:
    """캐릭터 왼쪽에 분리된 가는 속도선만 지우고 본체 픽셀은 보존합니다."""
    result = image.copy()
    pixels = result.load()
    # Red Rook 대시의 속도선은 본체보다 완전히 왼쪽에 있으므로 해당 영역을 먼저 제거합니다.
    if weapon == "Shield":
        for y in range(result.height):
            for x in range(min(18, result.width)):
                pixels[x, y] = (0, 0, 0, 0)
    elif weapon == "Axe":
        # Red King 왼쪽 끝의 선은 완전히 지우고 본체 부근에서는 세로 이웃이 없는 가는 선만 제거합니다.
        original_alpha = image.getchannel("A").load()
        for y in range(result.height):
            for x in range(min(anchor_x, result.width)):
                if x < 11:
                    pixels[x, y] = (0, 0, 0, 0)
                    continue
                vertical_neighbors = 0
                for ny in range(max(0, y - 2), min(result.height, y + 3)):
                    if ny != y and original_alpha[x, ny] > 0:
                        vertical_neighbors += 1
                if vertical_neighbors == 0:
                    pixels[x, y] = (0, 0, 0, 0)
    elif weapon == "Lance":
        # Lance Dash는 본체와 창이 한 덩어리이므로 그 밖의 분리된 속도 점을 전부 제거합니다.
        components = alpha_components(result)
        if components:
            body_and_lance = max(components, key=len)
            keep = set(body_and_lance)
            for y in range(result.height):
                for x in range(result.width):
                    if (x, y) not in keep:
                        pixels[x, y] = (0, 0, 0, 0)
    for component in alpha_components(result):
        xs = [point[0] for point in component]
        ys = [point[1] for point in component]
        width = max(xs) - min(xs) + 1
        height = max(ys) - min(ys) + 1
        if max(xs) < anchor_x and width >= 6 and height <= 3 and len(component) <= 80:
            for x, y in component:
                pixels[x, y] = (0, 0, 0, 0)
    return result


def remove_large_spin_arc(image: Image.Image) -> Image.Image:
    """Axe 회전 원본에서 몸체보다 큰 주황/적색 원호만 제거합니다."""
    result = image.copy()
    rgba = result.load()
    neutral: set[tuple[int, int]] = set()
    for y in range(result.height):
        for x in range(result.width):
            r, g, b, a = rgba[x, y]
            if a > 0 and max(r, g, b) - min(r, g, b) < 58:
                neutral.add((x, y))
    visited: set[tuple[int, int]] = set()
    neutral_components: list[list[tuple[int, int]]] = []
    for start in neutral:
        if start in visited:
            continue
        queue: deque[tuple[int, int]] = deque([start])
        visited.add(start)
        component: list[tuple[int, int]] = []
        while queue:
            x, y = queue.popleft()
            component.append((x, y))
            for point in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if point in neutral and point not in visited:
                    visited.add(point)
                    queue.append(point)
        neutral_components.append(component)

    if not neutral_components:
        return result

    def body_score(component: list[tuple[int, int]]) -> float:
        xs = [point[0] for point in component]
        ys = [point[1] for point in component]
        width = max(xs) - min(xs) + 1
        height = max(ys) - min(ys) + 1
        density = len(component) / float(width * height)
        center_x = (min(xs) + max(xs)) / 2.0
        center_y = (min(ys) + max(ys)) / 2.0
        distance = abs(center_x - 32) + abs(center_y - 34)
        return len(component) * density / (1.0 + distance / 24.0)

    body_core = max(neutral_components, key=body_score)
    keep: set[tuple[int, int]] = set()
    for x, y in body_core:
        for dy in range(-5, 6):
            for dx in range(-5, 6):
                nx, ny = x + dx, y + dy
                if 0 <= nx < result.width and 0 <= ny < result.height:
                    keep.add((nx, ny))
    for y in range(result.height):
        for x in range(result.width):
            if (x, y) not in keep:
                rgba[x, y] = (0, 0, 0, 0)
                continue
            r, g, b, a = rgba[x, y]
            # 웅크린 본체 아래에 붙은 마지막 붉은 원호 조각을 제거합니다.
            if y > 43 and a > 0 and r > 45 and r > g * 1.22 and r > b * 1.08:
                rgba[x, y] = (0, 0, 0, 0)
    return result


def paste_player_base(
    canvas: Image.Image,
    weapon: str,
    source: Image.Image,
    x_offset: int = 0,
    y_offset: int = 0,
) -> tuple[int, int]:
    """플레이어 원본 피벗을 유지하고 스킬 전용 발 디딤만 픽셀 단위로 더합니다."""
    box = source.getchannel("A").getbbox()
    if box is None:
        raise ValueError(f"투명한 원본 스프라이트입니다: {weapon}")
    destination_x = 50 - SOURCE_ANCHOR_X[weapon] + x_offset
    destination_y = GROUND_Y - (box[3] - 1) + y_offset
    canvas.alpha_composite(source, (destination_x, destination_y))
    return destination_x, destination_y


def redraw_shield_pose(canvas: Image.Image, source: Image.Image, destination: tuple[int, int], pose: SourcePose) -> None:
    """Red Rook 본체는 유지하고 방패와 연결 팔만 신규 각도로 다시 배치합니다."""
    dx, dy = destination
    shield_box = (24, 8, 49, 50)
    shield = source.crop(shield_box)
    clear = Image.new("RGBA", (shield_box[2] - shield_box[0], shield_box[3] - shield_box[1]), (0, 0, 0, 0))
    canvas.paste(clear, (dx + shield_box[0], dy + shield_box[1]))

    rotated = shield.rotate(pose.shield_angle or 0, resample=Image.Resampling.NEAREST, expand=True)
    target_x = dx + 24 + pose.shield_x - (rotated.width - shield.width) // 2
    target_y = dy + 8 + pose.shield_y - (rotated.height - shield.height) // 2

    draw = ImageDraw.Draw(canvas)
    shoulder = (dx + 21, dy + 28)
    shield_center = (target_x + rotated.width // 2, target_y + rotated.height // 2)
    draw.line((shoulder, shield_center), fill=(26, 27, 29, 255), width=4)
    draw.line((shoulder, shield_center), fill=(126, 126, 126, 255), width=2)
    canvas.alpha_composite(rotated, (target_x, target_y))


def render_frame(weapon: str, pose: SourcePose) -> tuple[Image.Image, str, list[str]]:
    path = source_path(weapon, pose)
    if not path.exists():
        raise FileNotFoundError(path)
    source = Image.open(path).convert("RGBA")
    edits: list[str] = []

    if pose.remove_lines:
        source = remove_speed_lines(source, weapon, SOURCE_ANCHOR_X[weapon])
        edits.append("remove_baked_speed_lines")
    if pose.remove_spin_effect:
        source = remove_large_spin_arc(source)
        edits.append("remove_baked_spin_arc")
    if pose.remove_sword_effect:
        source = redraw_player_sword(source, pose.index, pose.sword_angle)
        edits.append("reuse_exact_player_attack_body_and_idle_sword_pixels")

    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    destination = paste_player_base(canvas, weapon, source, pose.x, pose.y)
    if pose.x != 0 or pose.y != 0:
        edits.append("player_source_pose_pixel_offset")
    if pose.shield_angle is not None:
        # 먼저 붙인 원본 방패를 지운 뒤 신규 각도로 다시 그립니다.
        redraw_shield_pose(canvas, source, destination, pose)
        edits.append("redraw_shield_and_arm_pixels")

    return canvas, str(path.relative_to(ROOT)).replace("\\", "/"), edits


def build_skill(weapon: str, skill: str, sequence: list[SourcePose]) -> dict[str, object]:
    if len(sequence) != FRAME_COUNT:
        raise ValueError(f"{skill}: {FRAME_COUNT}프레임이 아닙니다")
    frame_dir = ASSET_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
    frame_dir.mkdir(parents=True, exist_ok=True)
    frames: list[Image.Image] = []
    frame_sources: list[dict[str, object]] = []
    sword_frames = render_sword_skill_frames(skill) if weapon == "Sword" else None

    for index, pose in enumerate(sequence):
        if sword_frames is not None:
            frame = sword_frames[index]
            source = str(
                (SWORD_SKILL_SHEET_ROOT / f"hys_Sword_{skill}_body_sheet.png").relative_to(ROOT)
            ).replace("\\", "/")
            edits = [
                "skill_specific_body_pose_sheet",
                "reuse_exact_idle_sword_pixels",
                "identity_palette_and_scale_lock",
            ]
        else:
            frame, source, edits = render_frame(weapon, pose)
        output = frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png"
        frame.save(output)
        frames.append(frame)
        frame_sources.append({"frame": index, "source": source, "edits": edits})

    gif_frames = [frame.resize((CANVAS[0] * GIF_SCALE, CANVAS[1] * GIF_SCALE), Image.Resampling.NEAREST) for frame in frames]
    total_units = SKILL_DURATIONS_MS[skill] // 10
    base_units, extra_units = divmod(total_units, FRAME_COUNT)
    frame_durations_ms = CUSTOM_GIF_FRAME_DURATIONS_MS.get(
        skill,
        [(base_units + (1 if index < extra_units else 0)) * 10 for index in range(FRAME_COUNT)],
    )
    GIF_ROOT.mkdir(parents=True, exist_ok=True)
    gif_path = GIF_ROOT / f"hys_Final_Enemy_{weapon}_{skill}.gif"
    gif_frames[0].save(
        gif_path,
        save_all=True,
        append_images=gif_frames[1:],
        duration=frame_durations_ms,
        loop=0,
        disposal=2,
        optimize=False,
    )
    return {
        "weapon": weapon,
        "skill": skill,
        "duration_ms": SKILL_DURATIONS_MS[skill],
        "gif_frame_durations_ms": frame_durations_ms,
        "frame_sources": frame_sources,
        "hashes": [hashlib.sha256(frame.tobytes()).hexdigest() for frame in frames],
        "gif": str(gif_path),
    }


def build_contact_sheet(results: list[dict[str, object]]) -> None:
    row_height = CANVAS[1] * PREVIEW_SCALE
    label_width = 310
    sheet = Image.new("RGB", (label_width + CANVAS[0] * PREVIEW_SCALE * FRAME_COUNT, row_height * len(results)), "#1c2029")
    draw = ImageDraw.Draw(sheet)
    for row, result in enumerate(results):
        weapon = str(result["weapon"])
        skill = str(result["skill"])
        draw.text((8, row * row_height + 8), f"{weapon} / {skill}", fill="white")
        draw.text((8, row * row_height + 28), "PLAYER SPRITE BASED", fill="#f2c45b")
        frame_dir = ASSET_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
        for index in range(FRAME_COUNT):
            frame = Image.open(frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png").convert("RGBA")
            layer = Image.new("RGBA", CANVAS, "#1c2029")
            layer.alpha_composite(frame)
            layer = layer.resize((CANVAS[0] * PREVIEW_SCALE, row_height), Image.Resampling.NEAREST)
            sheet.paste(layer.convert("RGB"), (label_width + index * CANVAS[0] * PREVIEW_SCALE, row * row_height))
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    sheet.save(REPORT_ROOT / "hys_PlayerBased_15_AllFrames.png")


def build_animated_preview(results: list[dict[str, object]]) -> None:
    columns = 3
    rows = 5
    cell_width = CANVAS[0] * PREVIEW_SCALE
    cell_height = CANVAS[1] * PREVIEW_SCALE + 24
    preview_frames: list[Image.Image] = []
    for frame_index in range(FRAME_COUNT):
        preview = Image.new("RGB", (cell_width * columns, cell_height * rows), "#1c2029")
        draw = ImageDraw.Draw(preview)
        for item_index, result in enumerate(results):
            weapon = str(result["weapon"])
            skill = str(result["skill"])
            source = ASSET_ROOT / weapon / "Sprites/hys_FinalSkills" / skill / f"hys_Enemy_{weapon}_{skill}_{frame_index:02d}.png"
            sprite = Image.open(source).convert("RGBA").resize((CANVAS[0] * PREVIEW_SCALE, CANVAS[1] * PREVIEW_SCALE), Image.Resampling.NEAREST)
            cell_x = item_index % columns * cell_width
            cell_y = item_index // columns * cell_height
            draw.text((cell_x + 5, cell_y + 5), f"{weapon} / {skill}", fill="white")
            layer = Image.new("RGBA", sprite.size, "#1c2029")
            layer.alpha_composite(sprite)
            preview.paste(layer.convert("RGB"), (cell_x, cell_y + 24))
        preview_frames.append(preview)
    preview_frames[0].save(
        REPORT_ROOT / "hys_PlayerBased_15_AnimatedPreview.gif",
        save_all=True,
        append_images=preview_frames[1:],
        duration=90,
        loop=0,
        optimize=False,
    )


def build_single_weapon_preview(results: list[dict[str, object]], weapon: str) -> None:
    """검수용으로 한 무기의 세 스킬을 불투명 배경 위에 나란히 재생합니다."""
    cell_width = CANVAS[0] * PREVIEW_SCALE
    cell_height = CANVAS[1] * PREVIEW_SCALE + 24
    preview_frames: list[Image.Image] = []
    for frame_index in range(FRAME_COUNT):
        preview = Image.new("RGB", (cell_width * len(results), cell_height), "#1c2029")
        draw = ImageDraw.Draw(preview)
        for item_index, result in enumerate(results):
            skill = str(result["skill"])
            source = ASSET_ROOT / weapon / "Sprites/hys_FinalSkills" / skill / f"hys_Enemy_{weapon}_{skill}_{frame_index:02d}.png"
            sprite = Image.open(source).convert("RGBA").resize(
                (CANVAS[0] * PREVIEW_SCALE, CANVAS[1] * PREVIEW_SCALE), Image.Resampling.NEAREST
            )
            cell_x = item_index * cell_width
            draw.text((cell_x + 5, 5), f"{weapon} / {skill}", fill="white")
            layer = Image.new("RGBA", sprite.size, "#1c2029")
            layer.alpha_composite(sprite)
            preview.paste(layer.convert("RGB"), (cell_x, 24))
        preview_frames.append(preview)
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    preview_frames[0].save(
        REPORT_ROOT / f"hys_{weapon}_3_AnimatedPreview.gif",
        save_all=True,
        append_images=preview_frames[1:],
        duration=90,
        loop=0,
        optimize=False,
    )


def build_single_weapon_contact_sheet(results: list[dict[str, object]], weapon: str) -> None:
    """세 스킬의 27개 프레임을 한 장에서 잘림과 자세 순서까지 확인합니다."""
    row_height = CANVAS[1] * PREVIEW_SCALE
    label_width = 310
    sheet = Image.new(
        "RGB",
        (label_width + CANVAS[0] * PREVIEW_SCALE * FRAME_COUNT, row_height * len(results)),
        "#1c2029",
    )
    draw = ImageDraw.Draw(sheet)
    for row, result in enumerate(results):
        skill = str(result["skill"])
        draw.text((8, row * row_height + 8), f"{weapon} / {skill}", fill="white")
        frame_dir = ASSET_ROOT / weapon / "Sprites/hys_FinalSkills" / skill
        for index in range(FRAME_COUNT):
            frame = Image.open(frame_dir / f"hys_Enemy_{weapon}_{skill}_{index:02d}.png").convert("RGBA")
            layer = Image.new("RGBA", CANVAS, "#1c2029")
            layer.alpha_composite(frame)
            layer = layer.resize((CANVAS[0] * PREVIEW_SCALE, row_height), Image.Resampling.NEAREST)
            sheet.paste(layer.convert("RGB"), (label_width + index * CANVAS[0] * PREVIEW_SCALE, row * row_height))
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    sheet.save(REPORT_ROOT / f"hys_{weapon}_3_AllFrames.png")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--weapon", choices=sorted(SKILLS), help="지정한 무기만 다시 생성합니다.")
    args = parser.parse_args()

    if args.weapon is None and GIF_ROOT.exists():
        shutil.rmtree(GIF_ROOT)
    results: list[dict[str, object]] = []
    for weapon, skill_map in SKILLS.items():
        if args.weapon is not None and weapon != args.weapon:
            continue
        for skill, sequence in skill_map.items():
            results.append(build_skill(weapon, skill, sequence))
    if args.weapon is not None:
        REPORT_ROOT.mkdir(parents=True, exist_ok=True)
        build_single_weapon_preview(results, args.weapon)
        build_single_weapon_contact_sheet(results, args.weapon)
        report = [{key: value for key, value in result.items() if key != "hashes"} for result in results]
        (REPORT_ROOT / f"hys_{args.weapon}_3_SourceAudit.json").write_text(
            json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
        )
        print(f"Player-based {args.weapon} skills exported: {GIF_ROOT}")
        return
    build_contact_sheet(results)
    build_animated_preview(results)
    REPORT_ROOT.mkdir(parents=True, exist_ok=True)
    report = [{key: value for key, value in result.items() if key != "hashes"} for result in results]
    (REPORT_ROOT / "hys_PlayerBased_15_SourceAudit.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Player-based monster skills exported: {GIF_ROOT}")


if __name__ == "__main__":
    main()
