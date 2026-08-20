from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


# 신규 몬스터 스킬 도트를 그리기 전에 실제 플레이어의 공격·돌진 자세를 한 화면에서 비교합니다.
ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "hys_Reports/MonsterSkillPlayerBase/hys_PlayerMotionReference.png"

GROUPS = [
    ("Sword / ForwardAttack1", ROOT / "Assets/05Anims/hys_Player_Anims/Sword/Sprites/hys_RedQueen_RequestedMotions", "hys_RedQueen_ForwardAttack1_*.png"),
    ("Sword / ForwardAttack2", ROOT / "Assets/05Anims/hys_Player_Anims/Sword/Sprites/hys_RedQueen_RequestedMotions", "hys_RedQueen_ForwardAttack2_*.png"),
    ("Sword / Dash", ROOT / "Assets/05Anims/hys_Player_Anims/Sword/Sprites/hys_RedQueen_RequestedMotions", "hys_RedQueen_Dash_*.png"),
    ("Axe / Attack1", ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions", "hys_RedKing_Attack1_*.png"),
    ("Axe / Attack2", ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions", "hys_RedKing_Attack2_*.png"),
    ("Axe / Dash", ROOT / "Assets/05Anims/hys_Player_Anims/Axe/Sprites/hys_RedKing_PlayerMotions", "hys_RedKing_Dash_*.png"),
    ("Bow / Attack1", ROOT / "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions", "hys_RedBishop_Bow_Attack1_*.png"),
    ("Bow / Attack2", ROOT / "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions", "hys_RedBishop_Bow_Attack2_*.png"),
    ("Bow / Dash", ROOT / "Assets/05Anims/hys_Player_Anims/Bow/Sprites/hys_RedBishop_Bow_PlayerMotions", "hys_RedBishop_Bow_Dash_*.png"),
    ("Lance / Attack1", ROOT / "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions", "hys_RedBishop_Attack1_*.png"),
    ("Lance / Attack2", ROOT / "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions", "hys_RedBishop_Attack2_*.png"),
    ("Lance / Dash", ROOT / "Assets/05Anims/hys_Player_Anims/Lance/Sprites/hys_RedBishop_PlayerMotions", "hys_RedBishop_Dash_*.png"),
    ("Shield / Attack", ROOT / "Assets/05Anims/hys_Player_Anims/Shield/Sprites", "hys_RedRook_Attack_*.png"),
    ("Shield / Dash", ROOT / "Assets/05Anims/hys_Player_Anims/Shield/Sprites", "hys_RedRook_Dash_*.png"),
    ("Shield / PlungeLand", ROOT / "Assets/05Anims/hys_Player_Anims/Shield/Sprites", "hys_RedRook_PlungeLand_*.png"),
]


def main() -> None:
    cell_width = 210
    row_height = 180
    label_width = 230
    sheet = Image.new("RGB", (label_width + cell_width * 8, row_height * len(GROUPS)), "#1c2029")
    draw = ImageDraw.Draw(sheet)

    for row, (label, folder, pattern) in enumerate(GROUPS):
        paths = sorted(folder.glob(pattern))
        draw.text((10, row * row_height + 12), label, fill="white")
        for index, path in enumerate(paths[:8]):
            source = Image.open(path).convert("RGBA")
            box = source.getchannel("A").getbbox()
            if box:
                source = source.crop(box)
            scale = min(4, max(1, min((cell_width - 20) // source.width, (row_height - 32) // source.height)))
            source = source.resize((source.width * scale, source.height * scale), Image.Resampling.NEAREST)
            x = label_width + index * cell_width + (cell_width - source.width) // 2
            y = row * row_height + 24 + (row_height - 24 - source.height) // 2
            layer = Image.new("RGBA", source.size, "#1c2029")
            layer.alpha_composite(source)
            sheet.paste(layer.convert("RGB"), (x, y))
            draw.text((label_width + index * cell_width + 6, row * row_height + 5), str(index), fill="#f2c45b")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
