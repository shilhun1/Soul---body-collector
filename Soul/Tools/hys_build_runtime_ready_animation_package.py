#!/usr/bin/env python3
"""hys 애니메이션 런타임 패키지와 포함 목록을 생성한다."""

from __future__ import annotations

import argparse
import hashlib
import io
import re
import tarfile
from collections import Counter, deque
from pathlib import Path


GUID_RE = re.compile(rb"guid:\s*([0-9a-fA-F]{32})")
WEAPON_NAMES = ("Sword", "Axe", "Bow", "Lance", "Shield")
TEXT_EXTENSIONS = {
    ".anim",
    ".controller",
    ".overridecontroller",
    ".prefab",
    ".asset",
    ".mat",
    ".meta",
}


def stable_guid(key: str) -> str:
    """새 RuntimeReady 파일에는 경로 기반의 고정 GUID를 사용한다."""
    return hashlib.md5(f"hys-runtime-ready:{key}".encode("utf-8")).hexdigest()


def write_meta(path: Path, *, folder: bool = False) -> None:
    """Unity가 새 결과물을 안정적으로 추적할 수 있도록 meta를 생성한다."""
    guid = stable_guid(path.as_posix())
    lines = ["fileFormatVersion: 2", f"guid: {guid}"]
    if folder:
        lines.append("folderAsset: yes")
    lines.extend(
        [
            "DefaultImporter:",
            "  externalObjects: {}",
            "  userData:",
            "  assetBundleName:",
            "  assetBundleVariant:",
            "",
        ]
    )
    path.with_name(path.name + ".meta").write_text("\n".join(lines), encoding="utf-8")


def unity_path(project: Path, path: Path) -> str:
    return path.relative_to(project).as_posix()


def add_tree(selected: set[Path], root: Path) -> None:
    if not root.exists():
        raise FileNotFoundError(root)
    selected.add(root)
    selected.update(p for p in root.rglob("*") if not p.name.endswith(".meta"))


def add_asset(selected: set[Path], path: Path) -> None:
    if not path.exists():
        raise FileNotFoundError(path)
    selected.add(path)


def collect_initial_assets(project: Path, output_dir: Path) -> set[Path]:
    """플레이어·무기 몬스터·영혼 애니메이션과 관련 hys 스크립트를 고른다."""
    assets = project / "Assets"
    selected: set[Path] = set()

    add_tree(selected, assets / "05Anims" / "hys_Player_Anims")
    enemy_root = assets / "05Anims" / "hys_Enemy_Anims"
    selected.add(enemy_root)
    for weapon in WEAPON_NAMES:
        add_tree(selected, enemy_root / weapon)
    add_tree(selected, assets / "05Anims" / "Soul_Anims")

    legacy_sword = assets / "05Anims" / "Player_Anims"
    add_asset(selected, legacy_sword / "hys_Player_Sword.controller")
    for clip in legacy_sword.glob("hys_Sword_*.anim"):
        selected.add(clip)

    hys_scripts = assets / "02Scripts" / "hys"
    add_tree(selected, hys_scripts / "Animation")
    add_tree(selected, hys_scripts / "Player")

    # 보스/몬스터를 포함해 이름으로 명확히 드러나는 애니메이션 연동 hys 스크립트도 포함한다.
    for script in hys_scripts.rglob("*.cs"):
        lowered = script.name.lower()
        if "animator" in lowered or "animation" in lowered:
            selected.add(script)

    return {p.resolve() for p in selected if output_dir.resolve() not in p.resolve().parents}


def build_guid_map(assets_root: Path, output_dir: Path) -> dict[str, Path]:
    candidates: dict[str, list[Path]] = {}
    for meta in assets_root.rglob("*.meta"):
        if output_dir.resolve() in meta.resolve().parents:
            continue
        match = GUID_RE.search(meta.read_bytes()[:4096])
        if not match:
            continue
        asset = meta.with_name(meta.name[:-5])
        guid = match.group(1).decode("ascii").lower()
        candidates.setdefault(guid, []).append(asset.resolve())

    # 프로젝트에 남은 구형 복사본과 GUID가 겹치면 현재 hys 전용 폴더를 우선한다.
    def preference(path: Path) -> tuple[int, int, str]:
        normalized = path.as_posix()
        score = 0
        if "/hys_Player_Anims/" in normalized or "/hys_Enemy_Anims/" in normalized:
            score += 100
        if "/Soul_Anims/" in normalized or "/02Scripts/hys/" in normalized:
            score += 90
        if "/Player_Anims/" in normalized and path.name.startswith(("hys_Player_Sword", "hys_Sword_")):
            score += 80
        return score, -len(normalized), normalized

    return {guid: max(paths, key=preference) for guid, paths in candidates.items()}


def dependency_closure(selected: set[Path], guid_map: dict[str, Path]) -> set[Path]:
    """클립/컨트롤러가 참조하는 스프라이트 등의 GUID 의존성을 재귀적으로 포함한다."""
    queue = deque(selected)
    completed: set[Path] = set()
    while queue:
        asset = queue.popleft()
        if asset in completed:
            continue
        completed.add(asset)
        if not asset.is_file() or asset.suffix.lower() not in TEXT_EXTENSIONS:
            continue
        for raw_guid in GUID_RE.findall(asset.read_bytes()):
            dependency = guid_map.get(raw_guid.decode("ascii").lower())
            if dependency and dependency not in selected:
                selected.add(dependency)
                queue.append(dependency)
    return selected


def add_parent_folders(project: Path, selected: set[Path]) -> set[Path]:
    assets_root = (project / "Assets").resolve()
    for asset in list(selected):
        parent = asset if asset.is_dir() else asset.parent
        while parent != assets_root and assets_root in parent.parents:
            selected.add(parent)
            parent = parent.parent
    return selected


def asset_guid(asset: Path) -> str:
    meta = asset.with_name(asset.name + ".meta")
    if not meta.exists():
        raise FileNotFoundError(f"meta 누락: {asset}")
    match = GUID_RE.search(meta.read_bytes()[:4096])
    if not match:
        raise RuntimeError(f"GUID 누락: {meta}")
    return match.group(1).decode("ascii").lower()


def tar_bytes(tar: tarfile.TarFile, name: str, data: bytes) -> None:
    info = tarfile.TarInfo(name)
    info.size = len(data)
    info.mtime = 0
    info.mode = 0o644
    tar.addfile(info, io.BytesIO(data))


def build_unitypackage(project: Path, selected: set[Path], package_path: Path) -> None:
    package_path.parent.mkdir(parents=True, exist_ok=True)
    with tarfile.open(package_path, "w:gz", format=tarfile.PAX_FORMAT) as archive:
        for asset in sorted(selected, key=lambda p: unity_path(project, p).lower()):
            guid = asset_guid(asset)
            meta = asset.with_name(asset.name + ".meta")
            if asset.is_file():
                tar_bytes(archive, f"{guid}/asset", asset.read_bytes())
            tar_bytes(archive, f"{guid}/asset.meta", meta.read_bytes())
            tar_bytes(archive, f"{guid}/pathname", unity_path(project, asset).encode("utf-8"))


def write_manifest(project: Path, selected: set[Path], manifest_path: Path, package_path: Path) -> None:
    files = sorted((p for p in selected if p.is_file()), key=lambda p: unity_path(project, p).lower())
    extensions = Counter(p.suffix.lower() or "(없음)" for p in files)
    lines = [
        "# hys Animation RuntimeReady",
        "",
        "플레이어 5종과 무기 몬스터 5종의 애니메이션을 다른 Unity 프로젝트에서도 원본 GUID/경로로 가져오기 위한 패키지입니다.",
        "",
        "## 사용 방법",
        "",
        f"1. Unity에서 `{package_path.name}`를 더블 클릭하거나 `Assets > Import Package > Custom Package`로 선택합니다.",
        "2. 기존 프로젝트에 임포트할 때는 충돌 항목을 확인한 뒤 필요한 파일만 선택합니다.",
        "3. 소스 프로젝트의 파일을 RuntimeReady 아래에 직접 복제하지 않아 C# 클래스 중복 오류가 발생하지 않습니다.",
        "",
        "## 포함 범위",
        "",
        "- Sword, Axe, Bow, Lance, Shield 플레이어 애니메이션 클립/컨트롤러",
        "- Sword, Axe, Bow, Lance, Shield 무기 몬스터 애니메이션 클립/컨트롤러",
        "- 영혼 애니메이션",
        "- `hys` 애니메이션 관련 스크립트와 플레이어 스크립트(이동 포함)",
        "- 클립/컨트롤러가 참조하는 스프라이트 의존성",
        "- 일반(Normal)·파워(Power) 몬스터는 제외",
        "",
        f"총 파일: **{len(files)}개**, 패키지 크기: **{package_path.stat().st_size:,} bytes**",
        "",
        "## 확장자별 개수",
        "",
    ]
    lines.extend(f"- `{ext}`: {count}" for ext, count in sorted(extensions.items()))
    lines.extend(["", "## 포함 파일", ""])
    lines.extend(f"- `{unity_path(project, path)}`" for path in files)
    lines.append("")
    manifest_path.write_text("\n".join(lines), encoding="utf-8")


def validate_package(project: Path, selected: set[Path], package_path: Path) -> None:
    expected_guids = {asset_guid(p) for p in selected}
    with tarfile.open(package_path, "r:gz") as archive:
        names = set(archive.getnames())
        packaged_paths = {
            member.name.split("/", 1)[0]: archive.extractfile(member).read().decode("utf-8")
            for member in archive.getmembers()
            if member.name.endswith("/pathname") and archive.extractfile(member) is not None
        }
    if set(packaged_paths) != expected_guids:
        raise RuntimeError("패키지 GUID 목록이 선택한 에셋과 다릅니다.")
    for guid, pathname in packaged_paths.items():
        if f"{guid}/asset.meta" not in names:
            raise RuntimeError(f"패키지 meta 누락: {pathname}")
        source = project / pathname
        if source.is_file() and f"{guid}/asset" not in names:
            raise RuntimeError(f"패키지 asset 누락: {pathname}")

    paths = set(packaged_paths.values())
    required = {
        "Assets/02Scripts/hys/Player/hys_Player_Movement.cs",
        "Assets/02Scripts/hys/Animation/hys_Player_Animator.cs",
        "Assets/02Scripts/hys/Animation/hys_Ghost_Animator.cs",
    }
    missing = required - paths
    if missing:
        raise RuntimeError(f"필수 파일 누락: {sorted(missing)}")
    forbidden = ("Assets/05Anims/hys_Enemy_Anims/Normal", "Assets/05Anims/hys_Enemy_Anims/Power")
    if any(path.startswith(forbidden) for path in paths):
        raise RuntimeError("제외 대상인 Normal/Power 몬스터가 포함되었습니다.")
    for weapon in WEAPON_NAMES:
        player_prefix = f"Assets/05Anims/hys_Player_Anims/{weapon}/"
        enemy_prefix = f"Assets/05Anims/hys_Enemy_Anims/{weapon}/"
        if not any(path.startswith(player_prefix) and path.endswith(".controller") for path in paths):
            if weapon != "Sword" or "Assets/05Anims/Player_Anims/hys_Player_Sword.controller" not in paths:
                raise RuntimeError(f"{weapon} 플레이어 컨트롤러 누락")
        if not any(path.startswith(enemy_prefix) and path.endswith(".controller") for path in paths):
            raise RuntimeError(f"{weapon} 몬스터 컨트롤러 누락")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    project = args.project.resolve()
    output_dir = project / "Assets" / "02Scripts" / "HWJ" / "Prefabs" / "Generated" / "RuntimeReady" / "hys_Animation_RuntimeReady"
    package_path = output_dir / "hys_Animation_RuntimeReady.unitypackage"
    manifest_path = output_dir / "hys_Animation_RuntimeReady_Manifest.md"

    output_dir.mkdir(parents=True, exist_ok=True)
    write_meta(output_dir, folder=True)

    selected = collect_initial_assets(project, output_dir)
    guid_map = build_guid_map(project / "Assets", output_dir)
    selected = dependency_closure(selected, guid_map)
    selected = add_parent_folders(project, selected)
    build_unitypackage(project, selected, package_path)
    write_manifest(project, selected, manifest_path, package_path)
    write_meta(package_path)
    write_meta(manifest_path)
    validate_package(project, selected, package_path)

    print(f"패키지 생성 완료: {package_path}")
    print(f"포함 에셋: {len(selected)}개")
    print(f"패키지 크기: {package_path.stat().st_size:,} bytes")


if __name__ == "__main__":
    main()
