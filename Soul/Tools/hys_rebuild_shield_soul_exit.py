from pathlib import Path

from PIL import Image, ImageDraw


# Shield 영혼 이탈 연출에 평상시 유령 스프라이트를 그대로 합성해 외형과 크기를 통일합니다.
PROJECT_ROOT = Path(__file__).resolve().parents[1]
SHIELD_SPRITES = PROJECT_ROOT / "Assets" / "05Anims" / "hys_Player_Anims" / "Shield" / "Sprites"
GHOST_SHEET = PROJECT_ROOT / "Assets" / "06Sprites" / "hys_Ghost_Standing.png"

FRAME_COUNT = 8
GHOST_CELL_SIZE = 32
GHOST_PPU = 16.0
SHIELD_PPU = 13.333333

# 앞의 두 프레임은 갑옷만 유지하고, 이후 프레임에서 같은 유령이 점점 빠져나오게 합니다.
GHOST_SIZES = (0, 0, 10, 14, 18, 21, 24, 27)
GHOST_TOPS = (0, 0, 31, 24, 18, 12, 7, 3)
GHOST_VARIANTS = (0, 0, 0, 1, 2, 3, 4, 5)


def load_ghost_frame(sheet: Image.Image, index: int, target_size: int) -> Image.Image:
    cell = sheet.crop((index * GHOST_CELL_SIZE, 0, (index + 1) * GHOST_CELL_SIZE, GHOST_CELL_SIZE))
    # Shield 프레임의 PPU가 다르므로 최종 월드 크기가 평상시 유령과 같도록 27픽셀로 맞춥니다.
    expected_final_size = round(GHOST_CELL_SIZE * SHIELD_PPU / GHOST_PPU)
    if target_size == GHOST_SIZES[-1] and target_size != expected_final_size:
        raise RuntimeError(f"최종 유령 크기가 맞지 않습니다: {target_size} != {expected_final_size}")
    return cell.resize((target_size, target_size), Image.Resampling.NEAREST)


def main() -> None:
    sheet = Image.open(GHOST_SHEET).convert("RGBA")
    frames: list[Image.Image] = []

    for index in range(FRAME_COUNT):
        path = SHIELD_SPRITES / f"hys_RedRook_SoulExit_{index:02d}.png"
        frame = Image.open(path).convert("RGBA")

        if index >= 2:
            # 기존에 들어 있던 작고 다른 모양의 유령만 지우고 갑옷 부분은 유지합니다.
            clear = Image.new("RGBA", (22, 40), (0, 0, 0, 0))
            frame.paste(clear, (14, 0))

            size = GHOST_SIZES[index]
            ghost = load_ghost_frame(sheet, GHOST_VARIANTS[index], size)
            x = (frame.width - size) // 2
            frame.alpha_composite(ghost, (x, GHOST_TOPS[index]))

        frame.save(path, optimize=True)
        frames.append(frame)

    scale = 4
    preview = Image.new("RGBA", (FRAME_COUNT * 50 * scale, 75 * scale + 28), (29, 33, 41, 255))
    draw = ImageDraw.Draw(preview)
    draw.text((8, 6), "Shield Soul Exit - Standard Ghost", fill=(255, 255, 255, 255))
    for index, frame in enumerate(frames):
        enlarged = frame.resize((frame.width * scale, frame.height * scale), Image.Resampling.NEAREST)
        preview.alpha_composite(enlarged, (index * 50 * scale, 28))
    preview.save(SHIELD_SPRITES / "hys_RedRook_Soul_Preview.png", optimize=True)
    print("Shield SoulExit 8프레임의 유령 외형과 최종 월드 크기를 평상시 유령에 맞췄습니다.")


if __name__ == "__main__":
    main()
