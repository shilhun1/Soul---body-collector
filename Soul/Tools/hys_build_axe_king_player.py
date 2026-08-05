from __future__ import annotations

import hashlib
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
KING_DIR = ROOT / "Assets/Pixel art Chess Knights pack/06_King/Red_King"
GHOST_ATLAS = ROOT / "Assets/06Sprites/hys_Ghost_Standing.png"
OUTPUT = ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions"
CLIP_DIR = ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Clips"
REPORT_DIR = ROOT / "Reports"
PREVIEW = REPORT_DIR / "hys_Axe_RedKing_PlayerAnimations.png"
GIF_PREVIEW = REPORT_DIR / "hys_Axe_RedKing_PlayerAnimations.gif"
VALIDATION = REPORT_DIR / "hys_Axe_RedKing_AnimationValidation.txt"

CANVAS = (70, 70)
TRANSPARENT = (0, 0, 0, 0)


def load_king(kind: str, index: int) -> Image.Image:
    return Image.open(KING_DIR / f"R_King_{kind}{index:02d}.png").convert("RGBA")


def blank() -> Image.Image:
    return Image.new("RGBA", CANVAS, TRANSPARENT)


def shift(image: Image.Image, x: int = 0, y: int = 0) -> Image.Image:
    result = blank()
    result.alpha_composite(image, (x, y))
    return result


def band_pose(image: Image.Image, upper_x: int = 0, middle_x: int = 0, lower_x: int = 0,
              upper_y: int = 0, middle_y: int = 0, lower_y: int = 0) -> Image.Image:
    """원본 픽셀을 재샘플링하지 않고 상체·허리·다리 구간만 옮겨 새 자세를 만듭니다."""
    result = blank()
    bands = (
        (0, 39, upper_x, upper_y),
        (37, 53, middle_x, middle_y),
        (51, 70, lower_x, lower_y),
    )
    for top, bottom, dx, dy in bands:
        part = image.crop((0, top, 70, bottom))
        result.alpha_composite(part, (dx, top + dy))
    return result


def add_motion_lines(image: Image.Image, strength: int) -> Image.Image:
    """King 원본의 적색·주황 계열만 사용해 대시 잔상을 도트로 추가합니다."""
    result = image.copy()
    draw = ImageDraw.Draw(result)
    colors = ((92, 8, 19, 255), (181, 29, 39, 255), (255, 101, 48, 255))
    for i in range(strength):
        y = 30 + i * 8
        length = 8 + i * 3
        draw.rectangle((1, y, length, y + 1), fill=colors[i % len(colors)])
        draw.point((length + 3, y), fill=colors[(i + 1) % len(colors)])
    return result


def add_landing_dust(image: Image.Image, stage: int) -> Image.Image:
    """착지 충격은 캐릭터 팔레트의 불꽃색을 사용한 작은 픽셀 파편으로 표현합니다."""
    result = image.copy()
    draw = ImageDraw.Draw(result)
    dark = (92, 8, 19, 255)
    red = (181, 29, 39, 255)
    orange = (255, 101, 48, 255)
    spreads = ((31, 66), (24, 64), (16, 61), (9, 58))
    x, y = spreads[min(stage, len(spreads) - 1)]
    draw.rectangle((x, y, 69 - x, y + 1), fill=dark)
    draw.rectangle((x + 3, y - 2, 66 - x, y - 1), fill=red)
    draw.point((x - 2, y - 4), fill=orange)
    draw.point((71 - x, y - 5), fill=orange)
    return result


def add_vertical_plunge_motion(image: Image.Image, stage: int, impact: bool = False) -> Image.Image:
    """도끼가 수평으로 휘둘리지 않고 아래로 꽂히는 낙공 방향선을 추가합니다."""
    result = image.copy()
    draw = ImageDraw.Draw(result)
    dark = (92, 8, 19, 255)
    red = (181, 29, 39, 255)
    orange = (255, 101, 48, 255)
    line_top = max(1, 17 - stage * 4)
    line_bottom = min(64, 39 + stage * 8)
    for x, color, offset in ((24, dark, 0), (29, red, 5), (43, orange, 2), (48, dark, 8)):
        draw.rectangle((x, line_top + offset, x + 1, line_bottom), fill=color)
        draw.point((x, min(68, line_bottom + 3)), fill=color)

    if impact:
        # 지면 중앙을 향하는 V자 파편으로 도끼날이 박힌 순간을 강조합니다.
        draw.rectangle((34, 48, 36, 66), fill=dark)
        draw.rectangle((37, 50, 38, 65), fill=red)
        draw.line((35, 64, 20, 68), fill=red, width=2)
        draw.line((37, 64, 54, 68), fill=orange, width=2)
        for x, y in ((17, 63), (23, 59), (49, 60), (57, 64), (29, 67), (44, 67)):
            draw.rectangle((x, y, x + 2, y + 1), fill=orange if x % 2 else red)
    return result


def make_plunge_impact_pose(image: Image.Image) -> Image.Image:
    """기존 옆 베기 도끼를 지우고 도끼날이 지면을 향한 착지 실루엣으로 교체합니다."""
    result = band_pose(image, upper_y=3, middle_y=2, lower_y=1)
    draw = ImageDraw.Draw(result)
    dark = (63, 5, 17, 255)
    red = (181, 29, 39, 255)
    orange = (255, 101, 48, 255)
    light = (255, 171, 83, 255)

    # 오른쪽에 들고 있던 도끼날 영역을 비운 뒤 손 앞에서 지면까지 수직 손잡이를 그립니다.
    draw.rectangle((50, 8, 69, 55), fill=TRANSPARENT)
    draw.rectangle((45, 26, 48, 63), fill=dark)
    draw.rectangle((47, 27, 49, 62), fill=red)
    draw.rectangle((48, 29, 49, 60), fill=light)

    # 양날 도끼의 머리가 바닥에 박혀 양쪽으로 벌어진 모양입니다.
    draw.polygon(((47, 53), (40, 51), (30, 55), (27, 62), (35, 66), (44, 63)), fill=dark)
    draw.polygon(((43, 54), (35, 55), (31, 61), (36, 64), (44, 61)), fill=red)
    draw.polygon(((48, 53), (55, 51), (65, 55), (68, 62), (60, 66), (51, 63)), fill=dark)
    draw.polygon(((52, 54), (60, 55), (64, 61), (59, 64), (51, 61)), fill=orange)
    draw.line((35, 66, 62, 66), fill=light, width=1)
    return add_vertical_plunge_motion(result, 3, impact=True)


def make_hit_frame(stage: int) -> Image.Image:
    """전방 충격을 받아 상체가 뒤로 밀리고 망토가 늦게 따라오는 피격 자세를 만듭니다."""
    source = load_king("die", 0 if stage in (0, 3) else 1)
    poses = (
        (0, 0, 0, 0),
        (-2, -1, 1, 1),
        (-5, -3, 1, 2),
        (-2, -1, 0, 0),
    )
    upper_x, middle_x, lower_x, upper_y = poses[stage]
    result = band_pose(source, upper_x=upper_x, middle_x=middle_x, lower_x=lower_x,
                       upper_y=upper_y, middle_y=max(0, upper_y - 1))
    draw = ImageDraw.Draw(result)
    if stage in (1, 2):
        # 캐릭터 오른쪽(전방)의 타격점과 왼쪽으로 밀리는 잔상을 함께 표시합니다.
        white = (232, 240, 235, 255)
        red = (181, 29, 39, 255)
        orange = (255, 101, 48, 255)
        draw.line((65, 31, 54, 35), fill=white, width=2)
        draw.line((66, 38, 55, 38), fill=orange, width=2)
        draw.line((8, 28, 1, 28), fill=red, width=2)
        draw.line((11, 43, 2, 45), fill=orange, width=2)
    return result


def grayscale_body(image: Image.Image) -> Image.Image:
    """영혼이 빠진 육체만 무채색으로 바꾸고 원본 명암 단계와 알파를 유지합니다."""
    result = blank()
    source = image.load()
    target = result.load()
    for y in range(70):
        for x in range(70):
            r, g, b, a = source[x, y]
            if a == 0:
                continue
            value = int(round((r * 0.299 + g * 0.587 + b * 0.114) / 24.0) * 24)
            value = max(0, min(240, value))
            target[x, y] = (value, value, value, a)
    return result


def make_gray_corpse(image: Image.Image) -> Image.Image:
    """Red King의 갑옷과 도끼가 보이는 옆으로 누운 회색 시체를 지면에 배치합니다."""
    corpse = grayscale_body(image).rotate(90, resample=Image.Resampling.NEAREST, expand=False)
    corpse = corpse.resize((70, 42), Image.Resampling.NEAREST)
    result = blank()
    result.alpha_composite(corpse, (0, 28))
    return result


def ghost_frame(index: int) -> Image.Image:
    atlas = Image.open(GHOST_ATLAS).convert("RGBA")
    return atlas.crop((index * 32, 0, index * 32 + 32, 32))


def make_soul_frame(stage: int) -> Image.Image:
    """유령의 꼬리를 갑옷 앞판 뒤에 가려 육신 안에서 빠져나오는 깊이를 표현합니다."""
    idle = load_king("idle", 0)
    color_strengths = (1.0, 0.82, 0.65, 0.48, 0.3, 0.12, 0.0, 0.0)
    # 유령이 완전히 빠져나온 뒤에는 비어 있는 회색 갑옷이 지면에 남습니다.
    body = make_gray_corpse(idle) if stage >= 6 else recolor_body(idle, color_strengths[stage])
    if stage == 0:
        return body

    ghost = ghost_frame(0)
    visible_heights = (0, 12, 18, 24, 29, 32, 32, 32)
    ghost_y_positions = (0, 30, 23, 16, 9, 2, 2, 0)
    armor_front_tops = (70, 38, 40, 42, 45, 48, 51, 54)
    visible_height = visible_heights[stage]
    ghost_y = ghost_y_positions[stage]

    result = body.copy()
    ghost_layer = ghost.crop((0, 0, 32, visible_height))
    result.alpha_composite(ghost_layer, (19, ghost_y))

    # 갑옷 아래쪽을 다시 앞에 얹어 유령 꼬리가 흉갑 내부에 가려지게 합니다.
    armor_top = armor_front_tops[stage]
    armor_front = body.crop((0, armor_top, 70, 70))
    result.alpha_composite(armor_front, (0, armor_top))

    if stage <= 5:
        draw = ImageDraw.Draw(result)
        glow = (238, 218, 255, 255)
        draw.point((33, 37), fill=glow)
        draw.point((38, 39), fill=glow)
    # 영혼은 몸 중심에서 시작해 프레임마다 위로 빠져나갑니다.
    return result


def recolor_body(image: Image.Image, strength: float) -> Image.Image:
    """회색 육신이 영혼을 받아 원래 Red King 색으로 돌아오는 중간 단계를 만듭니다."""
    gray = grayscale_body(image)
    result = blank()
    original_pixels = image.load()
    gray_pixels = gray.load()
    target = result.load()
    for y in range(70):
        for x in range(70):
            r, g, b, a = original_pixels[x, y]
            if a == 0:
                continue
            gr, gg, gb, _ = gray_pixels[x, y]
            target[x, y] = (
                int(gr + (r - gr) * strength),
                int(gg + (g - gg) * strength),
                int(gb + (b - gb) * strength),
                a,
            )
    return result


def make_soul_return_frame(stage: int) -> Image.Image:
    """유령이 Axe 육신의 가슴으로 들어간 뒤 본래 색이 돌아오는 전용 프레임입니다."""
    idle = load_king("idle", 0)
    color_strength = 0.0 if stage < 5 else (stage - 4) / 3.0
    result = recolor_body(idle, min(1.0, color_strength))
    if stage < 6:
        ghost = ghost_frame(min(8, 5 + stage))
        ghost_scale = max(14, 32 - stage * 3)
        ghost = ghost.resize((ghost_scale, ghost_scale), Image.Resampling.NEAREST)
        ghost_y = -1 + stage * 8
        result.alpha_composite(ghost, ((70 - ghost_scale) // 2, ghost_y))

    if stage in (5, 6):
        draw = ImageDraw.Draw(result)
        glow = (238, 218, 255, 255)
        draw.rectangle((31, 35, 39, 37), fill=glow)
        draw.point((27, 33), fill=glow)
        draw.point((43, 40), fill=glow)
    return result


def add_possession_flame(image: Image.Image, points: list[tuple[int, int]]) -> Image.Image:
    """Sword 복귀와 같은 순서로 붉은 영혼 불꽃이 Axe 육신의 가슴에 들어가게 합니다."""
    result = image.copy()
    draw = ImageDraw.Draw(result)
    dark = (92, 8, 19, 255)
    red = (181, 29, 39, 255)
    orange = (255, 101, 48, 255)
    draw.line(points, fill=dark, width=3)
    draw.line(points, fill=red, width=1)
    x, y = points[-1]
    draw.point((x, y), fill=orange)
    draw.point((x - 1, y), fill=orange)
    return result


def make_axe_possession_frames() -> list[Image.Image]:
    """Sword Possession과 같은 8프레임 속도로 유령을 보존하고 Red King 육신만 교체합니다."""
    ghost = ghost_frame(0)
    corpse = make_gray_corpse(load_king("idle", 0))
    idle = load_king("idle", 0)
    frames: list[Image.Image] = []

    # 원본 유령을 네 단계로 줄여 육신 안으로 넣되 유령 픽셀의 형태는 다시 그리지 않습니다.
    sizes = (32, 24, 16, 8)
    y_positions = (1, 9, 17, 24)
    for size, y in zip(sizes, y_positions):
        canvas = blank()
        if size > 0:
            layer = ghost.resize((size, size), Image.Resampling.NEAREST)
            canvas.alpha_composite(layer, (35 - size // 2, y))
        # 육신을 앞에 합성해 유령의 아래쪽이 몸 안으로 들어가는 깊이를 표현합니다.
        canvas.alpha_composite(corpse, (0, 0))
        frames.append(canvas)

    # 두 장의 역방향 죽음 프레임으로 기사 육신을 빠르게 복원합니다.
    frames.extend(load_king("die", index) for index in (5, 1))
    frames.extend((
        add_possession_flame(idle, [(42, 29), (38, 33), (35, 37)]),
        idle.copy(),
    ))
    return frames


def build_frames() -> dict[str, list[str]]:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    groups: dict[str, list[str]] = {}

    def save(group: str, frames: list[Image.Image]) -> None:
        # 프레임 수가 줄었을 때 이전 PNG와 Meta가 남지 않도록 해당 동작만 먼저 정리합니다.
        for stale_path in OUTPUT.glob(f"hys_RedKing_{group}_*.png*"):
            stale_path.unlink()
        names: list[str] = []
        for index, frame in enumerate(frames):
            name = f"hys_RedKing_{group}_{index:02d}.png"
            frame.save(OUTPUT / name)
            names.append(name)
        groups[group] = names

    idle = [load_king("idle", i) for i in range(6)]
    attack = [load_king("atk", i) for i in range(5)]
    death = [load_king("die", i) for i in range(11)]

    # 원본이 존재하는 Idle·Attack·Die는 픽셀을 변경하지 않고 전용 폴더에 복제합니다.
    save("Idle", idle)
    save("Attack1", attack)
    save("Attack2", [
        attack[0],
        band_pose(attack[1], upper_x=-1, middle_x=-1),
        band_pose(attack[2], upper_x=-1, middle_x=-1),
        band_pose(attack[3], upper_x=1, lower_x=-1),
        add_landing_dust(attack[4], 0),
    ])
    # 사망 마지막은 투명 프레임 대신 쓰러진 원본을 회색 시체로 남깁니다.
    save("Die", death[:-1] + [make_gray_corpse(idle[0])])

    # 달리기는 상체의 전진 기울기와 교차하는 하체 이동을 8프레임으로 구성합니다.
    run_sources = (3, 4, 5, 0, 1, 2, 1, 0)
    run_upper = (-1, -2, -2, -1, 0, -1, -2, -1)
    run_lower = (2, 1, -1, -2, -2, -1, 1, 2)
    run_y = (0, 1, 2, 1, 0, 1, 2, 1)
    save("Run", [
        band_pose(idle[source], upper_x=ux, middle_x=ux, lower_x=lx,
                  upper_y=dy, middle_y=dy, lower_y=0)
        for source, ux, lx, dy in zip(run_sources, run_upper, run_lower, run_y)
    ])

    save("Dash", [
        add_motion_lines(band_pose(idle[3], upper_x=-2, middle_x=-2, lower_x=1), 1),
        add_motion_lines(band_pose(attack[0], upper_x=-3, middle_x=-2, lower_x=2), 2),
        add_motion_lines(band_pose(attack[1], upper_x=-4, middle_x=-3, lower_x=2), 3),
        add_motion_lines(band_pose(attack[0], upper_x=-3, middle_x=-2, lower_x=1), 2),
        add_motion_lines(band_pose(idle[3], upper_x=-1, middle_x=-1), 1),
    ])

    save("JumpStart", [
        idle[0],
        band_pose(idle[4], upper_y=2, middle_y=1),
        band_pose(idle[5], upper_x=-1, upper_y=3, middle_y=2, lower_x=1),
    ])
    save("JumpApex", [
        band_pose(idle[1], upper_x=-1, lower_x=2, lower_y=-2),
        band_pose(idle[2], upper_x=0, lower_x=-2, lower_y=-3),
        band_pose(idle[1], upper_x=1, lower_x=1, lower_y=-2),
    ])
    save("JumpFall", [
        band_pose(idle[2], upper_x=1, lower_x=-1, lower_y=-1),
        band_pose(idle[3], upper_x=2, middle_x=1, lower_x=-2),
        band_pose(idle[4], upper_x=1, middle_x=1, lower_x=-1, lower_y=1),
    ])

    save("Hit", [make_hit_frame(i) for i in range(4)])
    save("Soul", [make_soul_frame(i) for i in range(8)])
    save("SoulReturn", [make_soul_return_frame(i) for i in range(8)])
    save("Possession", make_axe_possession_frames())

    save("PlungeStart", [
        attack[1],
        add_vertical_plunge_motion(attack[2], 0),
        add_vertical_plunge_motion(attack[3], 1),
    ])
    save("PlungeFall", [
        add_vertical_plunge_motion(shift(attack[3], 0, 1), 1),
        add_vertical_plunge_motion(shift(attack[3], 0, 3), 2),
        add_vertical_plunge_motion(shift(attack[3], 0, 5), 3),
    ])
    save("PlungeLand", [
        make_plunge_impact_pose(attack[0]),
        add_landing_dust(band_pose(idle[3], upper_y=3, middle_y=2, lower_y=1), 3),
        add_landing_dust(idle[0], 2),
        idle[0],
    ])
    return groups


def guid_for(filename: str) -> str:
    return hashlib.md5(f"hys-red-king-player/{filename}".encode("utf-8")).hexdigest()


def write_meta_files(groups: dict[str, list[str]]) -> dict[str, str]:
    template = (KING_DIR / "R_King_idle00.png.meta").read_text(encoding="utf-8")
    old_guid = next(line.split(": ", 1)[1] for line in template.splitlines() if line.startswith("guid: "))
    guids: dict[str, str] = {}
    for names in groups.values():
        for name in names:
            guid = guid_for(name)
            guids[name] = guid
            text = template.replace(old_guid, guid, 1)
            text = text.replace("    filterMode: 1", "    filterMode: 0")
            text = text.replace("  spritePixelsToUnits: 100", "  spritePixelsToUnits: 16")
            text = text.replace("    textureCompression: 1", "    textureCompression: 0")
            (OUTPUT / f"{name}.meta").write_text(text, encoding="utf-8")

    # 새 스프라이트 상위 폴더와 전용 동작 폴더의 Unity GUID도 고정합니다.
    for folder, key in ((OUTPUT.parent, "Axe/Sprites"), (OUTPUT, OUTPUT.name)):
        folder_meta = folder.parent / f"{folder.name}.meta"
        if folder_meta.exists():
            continue
        folder_meta.write_text(
            "fileFormatVersion: 2\n"
            f"guid: {guid_for(key)}\n"
            "folderAsset: yes\n"
            "DefaultImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n",
            encoding="utf-8",
        )
    return guids


def number(value: float) -> str:
    return f"{value:.9f}".rstrip("0").rstrip(".") or "0"


def clip_yaml(name: str, frame_names: list[str], guids: dict[str, str], fps: int, loop: bool) -> str:
    curve_lines: list[str] = []
    mapping_lines: list[str] = []
    for index, frame_name in enumerate(frame_names):
        curve_lines.extend((
            f"    - time: {number(index / fps)}",
            f"      value: {{fileID: 21300000, guid: {guids[frame_name]}, type: 3}}",
        ))
        mapping_lines.append(f"    - {{fileID: 21300000, guid: {guids[frame_name]}, type: 3}}")
    return "\n".join((
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!74 &7400000",
        "AnimationClip:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        f"  m_Name: {name}",
        "  serializedVersion: 7",
        "  m_Legacy: 0",
        "  m_Compressed: 0",
        "  m_UseHighQualityCurve: 1",
        "  m_RotationCurves: []",
        "  m_CompressedRotationCurves: []",
        "  m_EulerCurves: []",
        "  m_PositionCurves: []",
        "  m_ScaleCurves: []",
        "  m_FloatCurves: []",
        "  m_PPtrCurves:",
        "  - serializedVersion: 2",
        "    curve:",
        *curve_lines,
        "    attribute: m_Sprite",
        "    path: ",
        "    classID: 212",
        "    script: {fileID: 0}",
        "    flags: 2",
        f"  m_SampleRate: {fps}",
        "  m_WrapMode: 0",
        "  m_Bounds:",
        "    m_Center: {x: 0, y: 0, z: 0}",
        "    m_Extent: {x: 0, y: 0, z: 0}",
        "  m_ClipBindingConstant:",
        "    genericBindings:",
        "    - serializedVersion: 2",
        "      path: 0",
        "      attribute: 0",
        "      script: {fileID: 0}",
        "      typeID: 212",
        "      customType: 23",
        "      isPPtrCurve: 1",
        "      isIntCurve: 0",
        "      isSerializeReferenceCurve: 0",
        "    pptrCurveMapping:",
        *mapping_lines,
        "  m_AnimationClipSettings:",
        "    serializedVersion: 2",
        "    m_AdditiveReferencePoseClip: {fileID: 0}",
        "    m_AdditiveReferencePoseTime: 0",
        "    m_StartTime: 0",
        f"    m_StopTime: {number(len(frame_names) / fps)}",
        "    m_OrientationOffsetY: 0",
        "    m_Level: 0",
        "    m_CycleOffset: 0",
        "    m_HasAdditiveReferencePose: 0",
        f"    m_LoopTime: {1 if loop else 0}",
        "    m_LoopBlend: 0",
        "    m_LoopBlendOrientation: 0",
        "    m_LoopBlendPositionY: 0",
        "    m_LoopBlendPositionXZ: 0",
        "    m_KeepOriginalOrientation: 0",
        "    m_KeepOriginalPositionY: 1",
        "    m_KeepOriginalPositionXZ: 0",
        "    m_HeightFromFeet: 0",
        "    m_Mirror: 0",
        "  m_EditorCurves: []",
        "  m_EulerEditorCurves: []",
        "  m_HasGenericRootTransform: 0",
        "  m_HasMotionFloatCurves: 0",
        "  m_Events: []",
        "",
    ))


def write_clips(groups: dict[str, list[str]], guids: dict[str, str]) -> None:
    settings = {
        "Idle": ("hys_Player_Axe_Idle", 8, True),
        "Run": ("hys_Player_Axe_Run", 10, True),
        "JumpStart": ("hys_Player_Axe_JumpStart", 8, False),
        # 점프 최상단은 상태 전환용 일회성 클립이므로 반복하지 않습니다.
        "JumpApex": ("hys_Player_Axe_JumpApex", 6, False),
        "JumpFall": ("hys_Player_Axe_JumpFall", 8, True),
        "Dash": ("hys_Player_Axe_Dash", 12, False),
        "Attack1": ("hys_Player_Axe_Attack1", 7, False),
        "Attack2": ("hys_Player_Axe_Attack2", 7, False),
        "Hit": ("hys_Player_Axe_Hit", 8, False),
        "Die": ("hys_Player_Axe_Die", 8, False),
        "Soul": ("hys_Player_Axe_Soul", 8, False),
        "SoulReturn": ("hys_Player_Axe_SoulReturn", 8, False),
        "Possession": ("hys_Ghost_Possession_Axe", 8, False),
        "PlungeStart": ("hys_Player_Axe_PlungeStart", 8, False),
        "PlungeFall": ("hys_Player_Axe_PlungeFall", 6, True),
        "PlungeLand": ("hys_Player_Axe_PlungeLand", 8, False),
    }
    for group, (clip_name, fps, loop) in settings.items():
        path = CLIP_DIR / f"{clip_name}.anim"
        path.write_text(clip_yaml(clip_name, groups[group], guids, fps, loop), encoding="utf-8")
        meta_path = CLIP_DIR / f"{clip_name}.anim.meta"
        if not meta_path.exists():
            # 새 전용 클립도 다시 빌드할 때 같은 GUID를 유지하도록 고정합니다.
            meta_path.write_text(
                "fileFormatVersion: 2\n"
                f"guid: {guid_for(clip_name + '.anim')}\n"
                "NativeFormatImporter:\n"
                "  externalObjects: {}\n"
                "  mainObjectFileID: 7400000\n"
                "  userData: \n"
                "  assetBundleName: \n"
                "  assetBundleVariant: \n",
                encoding="utf-8",
            )


def build_preview(groups: dict[str, list[str]]) -> None:
    scale = 4
    cell = 70 * scale
    columns = 11
    title_height = 26
    rows = sum((len(names) + columns - 1) // columns for names in groups.values())
    sheet = Image.new("RGB", (columns * cell, len(groups) * title_height + rows * cell), (26, 31, 40))
    draw = ImageDraw.Draw(sheet)
    y = 0
    for group, names in groups.items():
        draw.text((8, y + 7), f"{group} ({len(names)} frames)", fill=(240, 240, 240))
        y += title_height
        group_rows = (len(names) + columns - 1) // columns
        for index, name in enumerate(names):
            sprite = Image.open(OUTPUT / name).convert("RGBA").resize((cell, cell), Image.Resampling.NEAREST)
            x = (index % columns) * cell
            row_y = y + (index // columns) * cell
            tile = Image.new("RGBA", (cell, cell), (43, 50, 64, 255))
            tile.alpha_composite(sprite)
            sheet.paste(tile.convert("RGB"), (x, row_y))
            draw.text((x + 5, row_y + 5), f"{index + 1:02d}", fill=(255, 199, 84))
        y += group_rows * cell
    REPORT_DIR.mkdir(parents=True, exist_ok=True)
    sheet.crop((0, 0, sheet.width, y)).save(PREVIEW)


def build_gif(groups: dict[str, list[str]]) -> None:
    """14개 동작을 한 화면에서 비교할 수 있는 반복 GIF를 만듭니다."""
    scale = 3
    sprite_size = 70 * scale
    cell_width = 300
    cell_height = 244
    columns = 4
    rows = (len(groups) + columns - 1) // columns
    group_items = list(groups.items())
    animation_frames: list[Image.Image] = []
    for tick in range(22):
        sheet = Image.new("RGB", (columns * cell_width, rows * cell_height), (26, 31, 40))
        draw = ImageDraw.Draw(sheet)
        for group_index, (group, names) in enumerate(group_items):
            column = group_index % columns
            row = group_index // columns
            x = column * cell_width
            y = row * cell_height
            frame_index = tick % len(names)
            sprite = Image.open(OUTPUT / names[frame_index]).convert("RGBA")
            sprite = sprite.resize((sprite_size, sprite_size), Image.Resampling.NEAREST)
            tile = Image.new("RGBA", (cell_width, cell_height), (43, 50, 64, 255))
            tile.alpha_composite(sprite, ((cell_width - sprite_size) // 2, 28))
            sheet.paste(tile.convert("RGB"), (x, y))
            draw.text((x + 8, y + 7), f"{group}  {frame_index + 1:02d}/{len(names):02d}", fill=(245, 245, 245))
        animation_frames.append(sheet)
    animation_frames[0].save(
        GIF_PREVIEW,
        save_all=True,
        append_images=animation_frames[1:],
        duration=110,
        loop=0,
        disposal=2,
    )


def write_validation(groups: dict[str, list[str]], guids: dict[str, str]) -> None:
    lines = [
        "Axe Red King Player Animation Validation",
        f"Canvas: {CANVAS[0]}x{CANVAS[1]}",
        "Pixels Per Unit: 16",
        "Filter Mode: Point",
        "Texture Compression: None",
        f"Groups: {len(groups)}",
        f"Frames: {sum(len(names) for names in groups.values())}",
        "",
    ]
    for group, names in groups.items():
        lines.append(f"{group}: {len(names)} frames")
        for name in names:
            image = Image.open(OUTPUT / name).convert("RGBA")
            lines.append(f"  {name} | size={image.size} | bbox={image.getchannel('A').getbbox()} | guid={guids[name]}")
    VALIDATION.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    groups = build_frames()
    guids = write_meta_files(groups)
    write_clips(groups, guids)
    build_preview(groups)
    build_gif(groups)
    write_validation(groups, guids)
    print(f"OUTPUT={OUTPUT}")
    print(f"GROUPS={len(groups)}")
    print(f"FRAMES={sum(len(names) for names in groups.values())}")
    print(f"PREVIEW={PREVIEW}")
    print(f"GIF={GIF_PREVIEW}")
    print(f"VALIDATION={VALIDATION}")


if __name__ == "__main__":
    main()
