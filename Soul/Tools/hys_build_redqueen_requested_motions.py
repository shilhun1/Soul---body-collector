from pathlib import Path
import math

from PIL import Image


# Red Queen 원본 팔레트와 50x50 캔버스를 유지하며 요청된 전용 모션만 제작합니다.
ROOT = Path(__file__).resolve().parents[1]
QUEEN_DIR = ROOT / "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen"
GHOST_ATLAS = ROOT / "Assets/06Sprites/hys_Ghost_Standing.png"
CONCEPT = ROOT / "Temp/hys_RedQueenMotionWork/hys_MovementConcept.png"
OUTPUT = ROOT / "Assets/05Anims/hys_Player_Anims/Sword/Sprites/hys_RedQueen_RequestedMotions"

MOTIONS = {
    "Run": (0, 6),
    "JumpStart": (1, 3),
    "JumpApex": (2, 2),
    "JumpFall": (3, 3),
    "Dash": (4, 4),
}


def is_green(pixel):
    r, g, b, _ = pixel
    return g > 135 and g > r * 1.35 and g > b * 1.35


def queen_palette():
    # 기존 Idle/Attack/Die의 실제 색만 수집해 새 프레임의 색상 이탈을 막습니다.
    colors = set()
    for path in sorted(QUEEN_DIR.glob("R_Queen_*.png")):
        image = Image.open(path).convert("RGBA")
        colors.update(pixel[:3] for pixel in image.getdata() if pixel[3] > 0)
    return sorted(colors)


def nearest_palette_color(rgb, palette):
    r, g, b = rgb
    return min(palette, key=lambda color: (r - color[0]) ** 2 + (g - color[1]) ** 2 + (b - color[2]) ** 2)


def extract_concept_sprite(cell, palette):
    # 초록 배경을 제거한 뒤 원본 픽셀 팔레트로 다시 찍습니다.
    rgba = cell.convert("RGBA")
    pixels = []
    for pixel in rgba.getdata():
        if is_green(pixel):
            pixels.append((0, 0, 0, 0))
        else:
            pixels.append((*nearest_palette_color(pixel[:3], palette), 255))
    rgba.putdata(pixels)

    box = rgba.getbbox()
    if box is None:
        raise RuntimeError("모션 콘셉트 셀에서 캐릭터를 찾지 못했습니다.")
    sprite = rgba.crop(box)

    # 검과 망토가 잘리지 않도록 46x46 안에 비율을 유지해 배치합니다.
    scale = min(46 / sprite.width, 46 / sprite.height)
    width = max(1, round(sprite.width * scale))
    height = max(1, round(sprite.height * scale))
    sprite = sprite.resize((width, height), Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", (50, 50), (0, 0, 0, 0))
    x = (50 - width) // 2
    y = 49 - height
    canvas.alpha_composite(sprite, (x, y))
    return canvas


def foreground_ranges(row_image, expected_count):
    # 생성 시 격자를 조금 넘은 검과 망토도 자르지 않도록 실제 전경 열을 기준으로 분리합니다.
    rgba = row_image.convert("RGBA")
    occupied = []
    for x in range(rgba.width):
        occupied.append(any(not is_green(rgba.getpixel((x, y))) for y in range(rgba.height)))

    ranges = []
    start = None
    for x, active in enumerate(occupied + [False]):
        if active and start is None:
            start = x
        elif not active and start is not None:
            ranges.append([start, x])
            start = None

    # 20픽셀 이하의 작은 공백은 한 캐릭터 내부의 검·망토 분리로 간주합니다.
    merged = []
    for current in ranges:
        if merged and current[0] - merged[-1][1] <= 20:
            merged[-1][1] = current[1]
        else:
            merged.append(current)

    while len(merged) > expected_count:
        gaps = [merged[index + 1][0] - merged[index][1] for index in range(len(merged) - 1)]
        index = gaps.index(min(gaps))
        merged[index][1] = merged[index + 1][1]
        del merged[index + 1]

    if len(merged) != expected_count:
        raise RuntimeError(f"행 전경 개수 불일치: 예상 {expected_count}, 실제 {len(merged)}")
    return [(max(0, left - 4), min(rgba.width, right + 4)) for left, right in merged]


def build_movement_frames():
    concept = Image.open(CONCEPT).convert("RGBA")
    palette = queen_palette()
    row_edges = [round(index * concept.height / 5) for index in range(6)]

    OUTPUT.mkdir(parents=True, exist_ok=True)
    for motion, (row, count) in MOTIONS.items():
        top, bottom = row_edges[row], row_edges[row + 1]
        row_image = concept.crop((0, top, concept.width, bottom))
        ranges = foreground_ranges(row_image, count)
        for index, (left, right) in enumerate(ranges):
            cell = row_image.crop((left, 0, right, row_image.height))
            frame = extract_concept_sprite(cell, palette)
            frame.save(OUTPUT / f"hys_RedQueen_{motion}_{index:02d}.png")


def cropped_ghost():
    # 기존 Ghost 애니메이션의 첫 프레임을 그대로 사용해 유령 정체성을 보존합니다.
    atlas = Image.open(GHOST_ATLAS).convert("RGBA")
    ghost = atlas.crop((0, 0, 32, 32))
    box = ghost.getbbox()
    ghost = ghost.crop(box) if box else ghost
    return ghost.resize((18, 22), Image.Resampling.NEAREST)


def soul_frame(body, ghost, ghost_y=None, visible_height=None):
    canvas = body.copy()
    if ghost_y is None:
        return canvas

    layer = ghost
    if visible_height is not None:
        visible_height = max(1, min(layer.height, visible_height))
        layer = layer.crop((0, 0, layer.width, visible_height))
    x = (50 - layer.width) // 2 + 2
    canvas.alpha_composite(layer, (x, ghost_y))
    return canvas


def build_soul_frames():
    # Soul Exit과 Return 모두 첫 프레임은 죽은 Red Queen의 마지막 프레임과 완전히 같습니다.
    body = Image.open(QUEEN_DIR / "R_Queen_die08.png").convert("RGBA")
    ghost = cropped_ghost()

    exit_frames = [
        soul_frame(body, ghost),
        soul_frame(body, ghost, 24, 8),
        soul_frame(body, ghost, 16, 15),
        soul_frame(body, ghost, 8),
        soul_frame(body, ghost, 0),
    ]
    return_frames = [
        soul_frame(body, ghost),
        soul_frame(body, ghost, 0),
        soul_frame(body, ghost, 8),
        soul_frame(body, ghost, 16, 15),
        soul_frame(body, ghost),
    ]

    for index, frame in enumerate(exit_frames):
        frame.save(OUTPUT / f"hys_RedQueen_SoulExit_{index:02d}.png")
    for index, frame in enumerate(return_frames):
        frame.save(OUTPUT / f"hys_RedQueen_SoulReturn_{index:02d}.png")


def main():
    build_movement_frames()
    build_soul_frames()
    files = sorted(OUTPUT.glob("*.png"))
    print(f"OUTPUT={OUTPUT}")
    print(f"COUNT={len(files)}")


if __name__ == "__main__":
    main()
