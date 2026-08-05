from pathlib import Path

from PIL import Image, ImageDraw


# Red Rook 원본 시트에서 동작별 프레임을 다시 추출해 옆 프레임 혼입과 잘림을 방지합니다.
PROJECT_ROOT = Path(__file__).resolve().parents[1]
SHIELD_ROOT = PROJECT_ROOT / "Assets" / "05Anims" / "hys_Player_Anims" / "Shield"
SOURCE_PATH = SHIELD_ROOT / "Source" / "hys_RedRook_Movement_Transparent.png"
SPRITE_DIR = SHIELD_ROOT / "Sprites"

CANVAS_SIZE = (64, 50)
SOURCE_SCALE = 0.30
GROUND_Y = 47


# 좌표는 투명 원본의 각 프레임 알파 영역을 기준으로 분리했습니다.
FRAME_ROWS = {
    "Run": ((36, 153), [(277, 412), (453, 581), (641, 775), (842, 973), (1029, 1169), (1228, 1353)]),
    "JumpStart": ((192, 307), [(278, 412), (447, 588), (627, 759)]),
    "JumpApex": ((333, 443), [(276, 396), (436, 550)]),
    "JumpFall": ((485, 600), [(274, 408), (456, 576), (630, 751)]),
    "Dash": ((635, 747), [(238, 424), (453, 637), (673, 856), (871, 1054), (1062, 1246), (1251, 1436)]),
    "Hit": ((786, 924), [(274, 403), (468, 578), (637, 762)]),
}


def extract_frame(source: Image.Image, y_range: tuple[int, int], x_range: tuple[int, int]) -> Image.Image:
    frame = source.crop((x_range[0], y_range[0], x_range[1], y_range[1]))
    alpha_bbox = frame.getchannel("A").getbbox()
    if alpha_bbox is None:
        raise RuntimeError(f"프레임에 불투명 픽셀이 없습니다: x={x_range}, y={y_range}")

    frame = frame.crop(alpha_bbox)
    scaled_size = (
        max(1, round(frame.width * SOURCE_SCALE)),
        max(1, round(frame.height * SOURCE_SCALE)),
    )
    frame = frame.resize(scaled_size, Image.Resampling.NEAREST)

    if frame.width > CANVAS_SIZE[0] or frame.height > CANVAS_SIZE[1]:
        raise RuntimeError(f"64x50 캔버스를 넘는 프레임입니다: {frame.size}")

    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    x = (CANVAS_SIZE[0] - frame.width) // 2
    y = GROUND_Y - frame.height
    canvas.alpha_composite(frame, (x, y))
    return canvas


def build_preview(frames: dict[str, list[Image.Image]]) -> Image.Image:
    scale = 4
    row_height = CANVAS_SIZE[1] * scale + 28
    width = 64 + max(len(items) for items in frames.values()) * CANVAS_SIZE[0] * scale
    height = len(frames) * row_height + 16
    preview = Image.new("RGBA", (width, height), (29, 33, 41, 255))
    draw = ImageDraw.Draw(preview)

    for row, (motion, items) in enumerate(frames.items()):
        y = 8 + row * row_height
        draw.text((8, y + 8), motion, fill=(255, 255, 255, 255))
        for index, frame in enumerate(items):
            enlarged = frame.resize((CANVAS_SIZE[0] * scale, CANVAS_SIZE[1] * scale), Image.Resampling.NEAREST)
            preview.alpha_composite(enlarged, (64 + index * CANVAS_SIZE[0] * scale, y))
    return preview


def main() -> None:
    source = Image.open(SOURCE_PATH).convert("RGBA")
    extracted: dict[str, list[Image.Image]] = {}

    for motion, (y_range, x_ranges) in FRAME_ROWS.items():
        extracted[motion] = []
        for index, x_range in enumerate(x_ranges):
            frame = extract_frame(source, y_range, x_range)
            output = SPRITE_DIR / f"hys_RedRook_{motion}_{index:02d}.png"
            frame.save(output, optimize=True)
            extracted[motion].append(frame)

    preview = build_preview(extracted)
    preview.save(SPRITE_DIR / "hys_RedRook_Movement_Preview.png", optimize=True)
    print(f"Shield 이동 프레임 {sum(map(len, extracted.values()))}개를 64x50으로 재생성했습니다.")


if __name__ == "__main__":
    main()
