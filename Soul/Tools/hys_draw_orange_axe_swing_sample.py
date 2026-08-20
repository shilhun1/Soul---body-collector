from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


# 주황색 Red King 원본 픽셀을 다시 그리지 않고 그대로 보존하는 Axe Swing 샘플 생성기입니다.
ROOT = Path(__file__).resolve().parents[1]
SOURCE = (
    ROOT
    / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions"
)
OUTPUT = ROOT / "hys_Reports/MonsterSkillSample/hys_OrangeMonster_AxeSwing_Sample"

CANVAS = (160, 112)
GROUND_Y = 100
FRAME_MS = 90
PREVIEW_SCALE = 4


# 기존 swing에서 빠졌던 충돌 자세를 같은 몬스터의 PlungeLand 자세로 보완합니다.
FRAME_SOURCES = [
    "hys_RedKing_Idle_02.png",
    "hys_RedKing_Attack1_00.png",
    "hys_RedKing_Attack1_01.png",
    "hys_RedKing_Attack1_02.png",
    "hys_RedKing_Attack1_03.png",
    "hys_RedKing_PlungeLand_03.png",
    "hys_RedKing_Attack1_02.png",
    "hys_RedKing_Attack1_01.png",
    "hys_RedKing_Idle_02.png",
]

# 이동 스킬이 아니므로 발 위치가 크게 흔들리지 않도록 3픽셀 안에서만 무게중심을 옮깁니다.
X_OFFSETS = [0, 0, 1, 2, 3, 3, 2, 1, 0]


def compose_frame(source_name: str, x_offset: int) -> Image.Image:
    source = Image.open(SOURCE / source_name).convert("RGBA")
    frame = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    x = 20 + x_offset
    y = GROUND_Y - source.height
    # 반투명 경계의 RGB가 재계산되지 않도록 알파 유무만 마스크로 사용해 원본 바이트를 복사합니다.
    opaque_mask = source.getchannel("A").point(lambda alpha: 255 if alpha > 0 else 0)
    frame.paste(source, (x, y), opaque_mask)
    return frame


def save_gif(frames: list[Image.Image]) -> Path:
    path = OUTPUT / "hys_OrangeMonster_AxeSwing_Sample.gif"
    scaled = [
        frame.resize(
            (CANVAS[0] * PREVIEW_SCALE, CANVAS[1] * PREVIEW_SCALE),
            Image.Resampling.NEAREST,
        )
        for frame in frames
    ]
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
    return path


def save_contact_sheet(frames: list[Image.Image]) -> Path:
    cell_width = CANVAS[0] * 3
    cell_height = CANVAS[1] * 3 + 24
    sheet = Image.new("RGB", (cell_width * 3, cell_height * 3), "#1c2029")
    draw = ImageDraw.Draw(sheet)
    for index, frame in enumerate(frames):
        x = (index % 3) * cell_width
        y = (index // 3) * cell_height
        draw.text((x + 6, y + 5), f"frame {index}", fill="white")
        enlarged = frame.resize(
            (CANVAS[0] * 3, CANVAS[1] * 3), Image.Resampling.NEAREST
        )
        background = Image.new("RGBA", enlarged.size, "#1c2029")
        background.alpha_composite(enlarged)
        sheet.paste(background.convert("RGB"), (x, y + 24))
    path = OUTPUT / "hys_OrangeMonster_AxeSwing_ContactSheet.png"
    sheet.save(path)
    return path


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    frames = [
        compose_frame(source_name, x_offset)
        for source_name, x_offset in zip(FRAME_SOURCES, X_OFFSETS, strict=True)
    ]
    for index, frame in enumerate(frames):
        frame.save(OUTPUT / f"hys_OrangeMonster_AxeSwing_{index:02d}.png")
    gif_path = save_gif(frames)
    sheet_path = save_contact_sheet(frames)
    print(f"GIF={gif_path}")
    print(f"CONTACT={sheet_path}")


if __name__ == "__main__":
    main()
