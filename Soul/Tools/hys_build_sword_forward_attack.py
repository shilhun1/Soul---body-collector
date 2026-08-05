"""선택한 두 시트에서 Sword Attack1/2의 8프레임을 같은 캔버스로 추출한다."""

from pathlib import Path

from PIL import Image


PROJECT = Path(__file__).resolve().parents[1]
REFERENCE = PROJECT / "Assets/05Anims/hys_Player_Anims/Sword/Reference"
OUTPUT = (
    PROJECT
    / "Assets/05Anims/hys_Player_Anims/Sword/Sprites/hys_RedQueen_RequestedMotions"
)
ORIGINAL_ATTACK = (
    PROJECT / "Assets/Pixel art Chess Knights pack/05_Queen/Red_Queen"
)
PREVIEW = PROJECT / "Reports/hys_SpriteSheets/hys_Sword_ForwardAttack_Preview.png"
ATTACK_SCALE = 0.8
TARGET_FEET_Y = 75


def extract_row(
    source_name: str, row: int, output_prefix: str, frame_count: int = 6
) -> None:
    """실제 픽셀 경계를 찾아 검 궤적이 셀 분할선에서 잘리지 않게 추출한다."""
    source = Image.open(REFERENCE / source_name).convert("RGBA")
    width, height = source.size
    row_top = round(row * height / 2)
    row_bottom = round((row + 1) * height / 2)
    row_image = source.crop((0, row_top, width, row_bottom))

    # 생성 배경의 밝기 편차로 남은 초록색 픽셀과 테두리 번짐을 제거한다.
    cleaned_pixels = []
    for red, green, blue, alpha in row_image.getdata():
        if green > 60 and green > red + 24 and green > blue + 18:
            cleaned_pixels.append((0, 0, 0, 0))
        else:
            cleaned_pixels.append((red, green, blue, alpha))
    row_image.putdata(cleaned_pixels)

    # 동일 폭 8등분 대신 비어 있는 세로 열을 경계로 사용한다.
    occupied_columns = []
    for x in range(row_image.width):
        occupied_columns.append(
            any(row_image.getpixel((x, y))[3] > 0 for y in range(row_image.height))
        )

    runs = []
    run_start = None
    for x, occupied in enumerate(occupied_columns + [False]):
        if occupied and run_start is None:
            run_start = x
        elif not occupied and run_start is not None:
            runs.append((run_start, x - 1))
            run_start = None

    if len(runs) != frame_count:
        raise RuntimeError(
            f"{source_name}: {frame_count}프레임이 아닌 {len(runs)}개 영역을 찾았습니다."
        )

    OUTPUT.mkdir(parents=True, exist_ok=True)
    for index, (run_left, run_right) in enumerate(runs):
        margin = 4
        left = max(0, run_left - margin)
        right = min(row_image.width, run_right + margin + 1)
        cell = row_image.crop((left, 0, right, row_image.height))

        # 원본 Red Queen 크기에 맞게 80%로 줄이고 발 위치를 같은 높이에 고정한다.
        cell = cell.resize(
            (round(cell.width / 4), round(cell.height / 4)),
            Image.Resampling.NEAREST,
        )
        alpha_bbox = cell.getchannel("A").getbbox()
        if alpha_bbox is None:
            raise RuntimeError(f"{source_name}: {index}번 프레임이 비어 있습니다.")
        cell = cell.crop(alpha_bbox)
        cell = cell.resize(
            (
                max(1, round(cell.width * ATTACK_SCALE)),
                max(1, round(cell.height * ATTACK_SCALE)),
            ),
            Image.Resampling.NEAREST,
        )
        canvas = Image.new("RGBA", (96, 100), (0, 0, 0, 0))
        x = (canvas.width - cell.width) // 2
        y = TARGET_FEET_Y - cell.height
        canvas.alpha_composite(cell, (x, y))
        canvas.save(OUTPUT / f"{output_prefix}_{index:02d}.png")


def build_preview() -> None:
    """원본 Queen 공격을 두 번 독립 재생하는 실제 클립 구성을 표시한다."""
    scale = 4
    cell_size = 64
    preview = Image.new(
        "RGBA", (6 * cell_size * scale, 2 * cell_size * scale), (28, 31, 38, 255)
    )
    for row in range(2):
        for column in range(6):
            frame = Image.open(ORIGINAL_ATTACK / f"R_Queen_atk0{column}.png").convert(
                "RGBA"
            )
            frame = frame.resize((50 * scale, 50 * scale), Image.Resampling.NEAREST)
            x = column * cell_size * scale + (cell_size - 50) * scale // 2
            y = row * cell_size * scale + (cell_size - 50) * scale // 2
            preview.alpha_composite(frame, (x, y))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW)


build_preview()
