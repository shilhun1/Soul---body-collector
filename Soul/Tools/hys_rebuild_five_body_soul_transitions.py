from collections import deque
from pathlib import Path

from PIL import Image


# 다섯 육체의 영혼 전환에 같은 64x64 보라색 유령을 크기 변형 없이 합성합니다.
ROOT = Path(__file__).resolve().parents[1]
PLAYER_ANIMS = ROOT / "Assets" / "05Anims" / "hys_Player_Anims"
GHOST_SHEET = ROOT / "Assets" / "06Sprites" / "hys_Ghost_Matte" / "hys_Ghost_Matte_Standing.png"
GHOST_SIZE = 64


def load_original_ghost() -> tuple[list[Image.Image], set[tuple[int, int, int, int]]]:
    sheet = Image.open(GHOST_SHEET).convert("RGBA")
    ghosts = [
        sheet.crop((index * GHOST_SIZE, 0, (index + 1) * GHOST_SIZE, GHOST_SIZE))
        for index in range(sheet.width // GHOST_SIZE)
    ]
    palette = {
        pixel
        for ghost in ghosts
        for pixel in ghost.getdata()
        if pixel[3] > 0
    }
    return ghosts, palette


def remove_previous_ghost(frame: Image.Image, palette: set[tuple[int, int, int, int]]) -> Image.Image:
    """기존 보라색 유령 덩어리만 지우고 갑옷과 육체 픽셀은 그대로 보존합니다."""
    image = frame.copy().convert("RGBA")
    pixels = image.load()
    width, height = image.size
    candidates = {
        (x, y)
        for y in range(height)
        for x in range(width)
        if pixels[x, y] in palette
    }
    purple_seeds = {
        point
        for point in candidates
        if pixels[point[0], point[1]][2] > pixels[point[0], point[1]][0] * 1.12
        and pixels[point[0], point[1]][2] > pixels[point[0], point[1]][1] * 1.05
    }

    removable: set[tuple[int, int]] = set()
    queue = deque(purple_seeds)
    while queue:
        point = queue.popleft()
        if point in removable or point not in candidates:
            continue
        removable.add(point)
        x, y = point
        for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
                         (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1)):
            if neighbor in candidates and neighbor not in removable:
                queue.append(neighbor)

    for x, y in removable:
        pixels[x, y] = (0, 0, 0, 0)
    return image


def rebuild_sequence(paths: list[Path], steps: list[tuple[int, float] | None], ghosts: list[Image.Image],
                     palette: set[tuple[int, int, int, int]]) -> None:
    if len(paths) != len(steps):
        raise RuntimeError(f"프레임 수와 흡입 단계가 다릅니다: {len(paths)} != {len(steps)}")

    for frame_index, (path, step) in enumerate(zip(paths, steps)):
        source_body = remove_previous_ghost(Image.open(path).convert("RGBA"), palette)
        target_size = (max(source_body.width, GHOST_SIZE), max(source_body.height, GHOST_SIZE))
        body = Image.new("RGBA", target_size, (0, 0, 0, 0))
        body_offset = ((target_size[0] - source_body.width) // 2,
                       (target_size[1] - source_body.height) // 2)
        body.alpha_composite(source_body, body_offset)
        result = Image.new("RGBA", target_size, (0, 0, 0, 0))
        result.alpha_composite(body, (0, 0))
        if step is not None:
            ghost_size, suction = step
            ghost = ghosts[frame_index % len(ghosts)]
            ghost = ghost.resize((ghost_size, ghost_size), Image.Resampling.NEAREST)
            armor_center_y = round(body.height * 0.58)
            free_center_y = GHOST_SIZE // 2 - 4
            ghost_center_y = round(free_center_y + (armor_center_y - free_center_y) * suction)
            ghost_x = (body.width - ghost_size) // 2
            ghost_y = ghost_center_y - ghost_size // 2
            # 원본 비율을 유지한 채 가슴 중심으로 작아져 들어가도록 해 흡입감을 만듭니다.
            result.alpha_composite(ghost, (ghost_x, ghost_y))
        result.save(path, optimize=True)


def numbered(directory: Path, prefix: str) -> list[Path]:
    return sorted(directory.glob(f"{prefix}_*.png"))


def main() -> None:
    ghosts, palette = load_original_ghost()
    enter_eight = [(64, 0.0), (58, 0.15), (50, 0.3), (42, 0.5),
                   (32, 0.68), (22, 0.85), (12, 1.0), None]
    exit_eight = list(reversed(enter_eight))

    axe = PLAYER_ANIMS / "Axe" / "Sprites" / "hys_RedKing_PlayerMotions"
    rebuild_sequence(numbered(axe, "hys_RedKing_Soul"), exit_eight, ghosts, palette)
    rebuild_sequence(numbered(axe, "hys_RedKing_SoulReturn"), enter_eight, ghosts, palette)
    rebuild_sequence(numbered(axe, "hys_RedKing_Possession"), enter_eight, ghosts, palette)

    bow = PLAYER_ANIMS / "Bow" / "Sprites" / "hys_RedBishop_Bow_PlayerMotions"
    rebuild_sequence(numbered(bow, "hys_RedBishop_Bow_SoulExit"), exit_eight, ghosts, palette)
    rebuild_sequence(numbered(bow, "hys_RedBishop_Bow_SoulReturn"), enter_eight, ghosts, palette)

    lance = PLAYER_ANIMS / "Lance" / "Sprites" / "hys_RedBishop_PlayerMotions"
    rebuild_sequence(numbered(lance, "hys_RedBishop_SoulExit"), exit_eight, ghosts, palette)
    rebuild_sequence(numbered(lance, "hys_RedBishop_SoulReturn"), enter_eight, ghosts, palette)

    shield = PLAYER_ANIMS / "Shield" / "Sprites"
    rebuild_sequence(numbered(shield, "hys_RedRook_SoulExit"), exit_eight, ghosts, palette)
    rebuild_sequence(numbered(shield, "hys_RedRook_SoulReturn"), enter_eight, ghosts, palette)

    sword = PLAYER_ANIMS / "Sword" / "Sprites" / "hys_RedQueen_RequestedMotions"
    sword_enter_five = [(64, 0.0), (52, 0.25), (36, 0.6), (20, 1.0), None]
    rebuild_sequence(numbered(sword, "hys_RedQueen_SoulExit"), list(reversed(sword_enter_five)), ghosts, palette)
    sword_return = numbered(sword, "hys_RedQueen_SoulReturn")
    sword_enter = sword_enter_five + [None] * (len(sword_return) - 5)
    rebuild_sequence(sword_return, sword_enter, ghosts, palette)

    print("검·도끼·활·창·방패의 영혼이 원본 비율을 유지하며 갑옷 중심으로 흡입되게 만들었습니다.")


if __name__ == "__main__":
    main()
