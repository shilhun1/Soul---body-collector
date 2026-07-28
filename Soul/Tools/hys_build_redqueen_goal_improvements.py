from pathlib import Path
import importlib.util

from PIL import Image, ImageDraw


# 기존 이동 프레임 생성기를 재사용하고 이번 요청에 해당하는 프레임만 후처리합니다.
ROOT = Path(__file__).resolve().parents[1]
BASE_SCRIPT = ROOT / "Tools/hys_build_redqueen_requested_motions.py"
QUEEN_DIR = ROOT / "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen"
GHOST_ATLAS = ROOT / "Assets/06Sprites/hys_Ghost_Standing.png"
OUTPUT = ROOT / "Assets/05Anims/hys_Player_Anims/Sword/Sprites/hys_RedQueen_RequestedMotions"
PREVIEW = ROOT / "Reports/hys_RedQueen_AttackFallSoul_Improved.png"
NEW_GUIDS = {
    **{f"hys_RedQueen_SoulReturn_{i:02d}.png": f"e72800000000000000000000000000{i + 25:x}" for i in range(5, 15)},
}


def load_base_builder():
    spec = importlib.util.spec_from_file_location("hys_redqueen_base", BASE_SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def exact_ghost():
    # 유령은 새로 그리지 않고 기존 아틀라스 첫 프레임의 32x32 픽셀을 그대로 사용합니다.
    atlas = Image.open(GHOST_ATLAS).convert("RGBA")
    return atlas.crop((0, 0, 32, 32))


def soul_frame(body=None, ghost=None, ghost_y=0, visible_from=None, body_in_front=False):
    canvas = Image.new("RGBA", (50, 50), (0, 0, 0, 0))
    ghost_layer = ghost
    ghost_x = 9

    if ghost_layer is not None and visible_from is not None:
        visible_from = max(0, min(31, visible_from))
        ghost_layer = ghost_layer.crop((0, visible_from, 32, 32))
        ghost_y += visible_from

    if body_in_front and ghost_layer is not None:
        canvas.alpha_composite(ghost_layer, (ghost_x, ghost_y))
        if body is not None:
            canvas.alpha_composite(body, (0, 0))
        return canvas

    if body is not None:
        canvas.alpha_composite(body, (0, 0))
    if ghost_layer is not None:
        canvas.alpha_composite(ghost_layer, (ghost_x, ghost_y))
    return canvas


def emerging_soul_frame(body, ghost, visible_height):
    # 유령 머리부터 가슴 위로 드러나게 하되 보이는 픽셀은 원본 유령에서 그대로 잘라 씁니다.
    visible_height = max(1, min(32, visible_height))
    canvas = body.copy()
    layer = ghost.crop((0, 0, 32, visible_height))
    canvas.alpha_composite(layer, (9, 26 - visible_height))
    return canvas


def shrinking_soul_frame(body, ghost, size, y):
    # 기존 유령을 최근접 보간으로 단계별 축소하면서 시체의 가슴 위치로 이동시킵니다.
    if size <= 0:
        return body.copy()
    layer = ghost.resize((size, size), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (50, 50), (0, 0, 0, 0))
    x = 25 - size // 2
    canvas.alpha_composite(layer, (x, y))
    canvas.alpha_composite(body, (0, 0))
    return canvas


def flame_frame(body, points):
    # 부활 완료 뒤 붉은 불꽃이 가슴으로 들어오는 궤적만 원본 Idle 위에 덧그립니다.
    frame = body.copy()
    draw = ImageDraw.Draw(frame)
    dark = (74, 0, 17, 255)
    red = (213, 62, 76, 255)
    orange = (255, 149, 92, 255)
    draw.line(points, fill=dark, width=3)
    draw.line(points, fill=red, width=1)
    x, y = points[-1]
    draw.point((x, y), fill=orange)
    draw.point((x - 1, y), fill=orange)
    return frame


def straighten_jump_fall_frames():
    # 상체는 기존 Red Queen 프레임을 유지하고 아래쪽 두 다리만 수직으로 곧게 펴 줍니다.
    dark = (74, 0, 17, 255)
    white = (239, 239, 239, 255)
    gray = (149, 149, 149, 255)
    red = (143, 35, 66, 255)
    for index in range(3):
        path = OUTPUT / f"hys_RedQueen_JumpFall_{index:02d}.png"
        frame = Image.open(path).convert("RGBA")
        pixels = frame.load()
        for y in range(34, 50):
            for x in range(23, 37):
                pixels[x, y] = (0, 0, 0, 0)

        draw = ImageDraw.Draw(frame)
        sway = (0, 1, 0)[index]
        draw.polygon([(24 + sway, 32), (29 + sway, 32), (29 + sway, 39), (28 + sway, 46),
                      (24 + sway, 46), (25 + sway, 39)], fill=dark)
        draw.polygon([(26 + sway, 33), (28 + sway, 33), (28 + sway, 39), (27 + sway, 45),
                      (25 + sway, 45), (26 + sway, 39)], fill=white)
        draw.point((25 + sway, 40), fill=gray)
        draw.line([(27 + sway, 46), (24 + sway, 48)], fill=dark, width=2)
        draw.point((25 + sway, 47), fill=white)
        draw.polygon([(30 + sway, 32), (35 + sway, 32), (34 + sway, 40), (35 + sway, 47),
                      (31 + sway, 47), (31 + sway, 40)], fill=dark)
        draw.polygon([(32 + sway, 33), (34 + sway, 33), (33 + sway, 40), (34 + sway, 46),
                      (32 + sway, 46), (32 + sway, 40)], fill=white)
        draw.point((34 + sway, 39), fill=red)
        draw.line([(32 + sway, 47), (35 + sway, 48)], fill=dark, width=2)
        draw.point((34 + sway, 47), fill=white)
        frame.save(path)


def build_soul_frames():
    # 사망한 몸에서 원본 유령이 빠져나오고 복귀 때는 같은 유령이 몸에 가려지며 흡수됩니다.
    corpse = Image.open(QUEEN_DIR / "R_Queen_die08.png").convert("RGBA")
    idle = Image.open(QUEEN_DIR / "R_Queen_idle00.png").convert("RGBA")
    ghost = exact_ghost()

    # 흡수 -> 죽음 프레임 역순 기상 -> 정확한 Idle 1번 -> 붉은 불꽃 유입 순서입니다.
    return_frames = [
        shrinking_soul_frame(corpse, ghost, 32, 0),
        shrinking_soul_frame(corpse, ghost, 24, 8),
        shrinking_soul_frame(corpse, ghost, 16, 16),
        shrinking_soul_frame(corpse, ghost, 8, 23),
        shrinking_soul_frame(corpse, ghost, 0, 0),
    ]
    for die_index in (7, 6, 5, 2, 1):
        return_frames.append(Image.open(QUEEN_DIR / f"R_Queen_die{die_index:02d}.png").convert("RGBA"))
    return_frames.extend([
        idle.copy(),
        flame_frame(idle, [(45, 8), (41, 12), (38, 16)]),
        flame_frame(idle, [(39, 14), (35, 19), (32, 23)]),
        flame_frame(idle, [(33, 22), (30, 25), (27, 28)]),
        idle.copy(),
    ])

    for index, frame in enumerate(return_frames):
        frame.save(OUTPUT / f"hys_RedQueen_SoulReturn_{index:02d}.png")


def build_preview():
    # 최종 프레임 순서를 한눈에 확인할 수 있는 확대 프리뷰를 만듭니다.
    groups = [
        ("ATTACK 1 - LEFT TO RIGHT", [QUEEN_DIR / f"R_Queen_atk{i:02d}.png" for i in range(7)]),
        ("ATTACK 2 - RIGHT TO LEFT", [QUEEN_DIR / f"R_Queen_atk{i:02d}.png" for i in (0, 5, 4, 3, 2, 1, 6)]),
        ("JUMP FALL - STRAIGHT LEGS", [OUTPUT / f"hys_RedQueen_JumpFall_{i:02d}.png" for i in range(3)]),
        ("SOUL EXIT - PREVIOUS VERSION RESTORED", [OUTPUT / f"hys_RedQueen_SoulExit_{i:02d}.png" for i in range(5)]),
        ("SOUL RETURN - SHRINK / REVIVE / RED FLAME", [OUTPUT / f"hys_RedQueen_SoulReturn_{i:02d}.png" for i in range(15)]),
    ]
    scale = 4
    cell = 50 * scale
    columns = 8
    heights = [50 + ((len(paths) + columns - 1) // columns) * cell for _, paths in groups]
    sheet = Image.new("RGB", (columns * cell, sum(heights)), (28, 33, 43))
    draw = ImageDraw.Draw(sheet)
    y = 0
    for title, paths in groups:
        draw.text((8, y + 12), title, fill=(235, 235, 235))
        y += 50
        for index, path in enumerate(paths):
            sprite = Image.open(path).convert("RGBA").resize((cell, cell), Image.Resampling.NEAREST)
            x = (index % columns) * cell
            row_y = y + (index // columns) * cell
            tile = Image.new("RGBA", (cell, cell), (42, 49, 62, 255))
            tile.alpha_composite(sprite)
            sheet.paste(tile.convert("RGB"), (x, row_y))
            draw.text((x + 6, row_y + 6), f"{index + 1:02d}", fill=(255, 196, 80))
        y += ((len(paths) + columns - 1) // columns) * cell
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(PREVIEW)


def ensure_new_sprite_meta_files():
    # 기존 전용 스프라이트 메타 형식을 복제해 새 프레임도 동일한 PPU와 Point 필터를 사용합니다.
    template_path = OUTPUT / "hys_RedQueen_SoulExit_04.png.meta"
    template = template_path.read_text(encoding="utf-8")
    old_guid = "e7280000000000000000000000000017"
    for filename, guid in NEW_GUIDS.items():
        meta_path = OUTPUT / f"{filename}.meta"
        if not meta_path.exists():
            meta_path.write_text(template.replace(old_guid, guid), encoding="utf-8")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    # 기존 완성 프레임을 기준으로 필요한 구간만 다시 만들며 원본 캐릭터 외형은 유지합니다.
    straighten_jump_fall_frames()
    build_soul_frames()
    ensure_new_sprite_meta_files()
    build_preview()
    print(f"OUTPUT={OUTPUT}")
    print("SOUL_EXIT_FRAMES=5")
    print("SOUL_RETURN_FRAMES=15")
    print(f"PREVIEW={PREVIEW}")


if __name__ == "__main__":
    main()
